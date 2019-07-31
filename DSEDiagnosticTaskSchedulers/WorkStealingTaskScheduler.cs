﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Schedulers.Extensions;

namespace Schedulers
{

    /// <summary>Provides a work-stealing scheduler.</summary> 
    public sealed class WorkStealingTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly int m_concurrencyLevel;
        private readonly Queue<Task> m_queue = new Queue<Task>();
        private WorkStealingQueue<Task>[] m_wsQueues = new WorkStealingQueue<Task>[Environment.ProcessorCount];
        private Lazy<Thread[]> m_threads;
        private int m_threadsWaiting;
        private bool m_shutdown;
        [ThreadStatic]
        private static WorkStealingQueue<Task> m_wsq;

        /// <summary>Initializes a new instance of the WorkStealingTaskScheduler class.</summary> 
        /// <remarks>This constructors defaults to using twice as many threads as there are processors.</remarks> 
        public WorkStealingTaskScheduler() : this(Environment.ProcessorCount * 2) { }

        /// <summary>Initializes a new instance of the WorkStealingTaskScheduler class.</summary> 
        /// <param name="concurrencyLevel">The number of threads to use in the scheduler.</param> 
        public WorkStealingTaskScheduler(int concurrencyLevel)
        {
            // Store the concurrency level 
            if (concurrencyLevel <= 0) throw new ArgumentOutOfRangeException("concurrencyLevel");
            m_concurrencyLevel = concurrencyLevel;

            // Set up threads 
            m_threads = new Lazy<Thread[]>(() =>
            {
                var threads = new Thread[m_concurrencyLevel];
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i] = new Thread(DispatchLoop) { IsBackground = true };
                    threads[i].Start();
                }
                return threads;
            });
        }

        /// <summary>Queues a task to the scheduler.</summary> 
        /// <param name="task">The task to be scheduled.</param> 
        protected override void QueueTask(Task task)
        {
            // Make sure the pool is started, e.g. that all threads have been created. 
            m_threads.Force();

            // If the task is marked as long-running, give it its own dedicated thread 
            // rather than queueing it. 
            if ((task.CreationOptions & TaskCreationOptions.LongRunning) != 0)
            {
                new Thread(state => base.TryExecuteTask((Task)state)) { IsBackground = true }.Start(task);
            }
            else
            {
                // Otherwise, insert the work item into a queue, possibly waking a thread. 
                // If there's a local queue and the task does not prefer to be in the global queue, 
                // add it to the local queue. 
                WorkStealingQueue<Task> wsq = m_wsq;
                if (wsq != null && ((task.CreationOptions & TaskCreationOptions.PreferFairness) == 0))
                {
                    // Add to the local queue and notify any waiting threads that work is available. 
                    // Races may occur which result in missed event notifications, but they're benign in that 
                    // this thread will eventually pick up the work item anyway, as will other threads when another 
                    // work item notification is received. 
                    wsq.LocalPush(task);
                    if (m_threadsWaiting > 0) // OK to read lock-free. 
                    {
                        lock (m_queue) { Monitor.Pulse(m_queue); }
                    }
                }
                // Otherwise, add the work item to the global queue 
                else
                {
                    lock (m_queue)
                    {
                        m_queue.Enqueue(task);
                        if (m_threadsWaiting > 0) Monitor.Pulse(m_queue);
                    }
                }
            }
        }

        /// <summary>Executes a task on the current thread.</summary> 
        /// <param name="task">The task to be executed.</param> 
        /// <param name="taskWasPreviouslyQueued">Ignored.</param> 
        /// <returns>Whether the task could be executed.</returns> 
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return TryExecuteTask(task);

            // // Optional replacement: Instead of always trying to execute the task (which could 
            // // benignly leave a task in the queue that's already been executed), we 
            // // can search the current work-stealing queue and remove the task, 
            // // executing it inline only if it's found. 
            // WorkStealingQueue<Task> wsq = m_wsq; 
            // return wsq != null && wsq.TryFindAndPop(task) && TryExecuteTask(task); 
        }

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary> 
        public override int MaximumConcurrencyLevel
        {
            get { return m_concurrencyLevel; }
        }

        /// <summary>Gets all of the tasks currently scheduled to this scheduler.</summary> 
        /// <returns>An enumerable containing all of the scheduled tasks.</returns> 
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // Keep track of all of the tasks we find 
            List<Task> tasks = new List<Task>();

            // Get all of the global tasks.  We use TryEnter so as not to hang 
            // a debugger if the lock is held by a frozen thread. 
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(m_queue, ref lockTaken);
                if (lockTaken) tasks.AddRange(m_queue.ToArray());
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(m_queue);
            }

            // Now get all of the tasks from the work-stealing queues 
            WorkStealingQueue<Task>[] queues = m_wsQueues;
            for (int i = 0; i < queues.Length; i++)
            {
                WorkStealingQueue<Task> wsq = queues[i];
                if (wsq != null) tasks.AddRange(wsq.ToArray());
            }

            // Return to the debugger all of the collected task instances 
            return tasks;
        }

        /// <summary>Adds a work-stealing queue to the set of queues.</summary> 
        /// <param name="wsq">The queue to be added.</param> 
        private void AddWsq(WorkStealingQueue<Task> wsq)
        {
            lock (m_wsQueues)
            {
                // Find the next open slot in the array. If we find one, 
                // store the queue and we're done. 
                int i;
                for (i = 0; i < m_wsQueues.Length; i++)
                {
                    if (m_wsQueues[i] == null)
                    {
                        m_wsQueues[i] = wsq;
                        return;
                    }
                }

                // We couldn't find an open slot, so double the length  
                // of the array by creating a new one, copying over, 
                // and storing the new one. Here, i == m_wsQueues.Length. 
                WorkStealingQueue<Task>[] queues = new WorkStealingQueue<Task>[i * 2];
                Array.Copy(m_wsQueues, queues, i);
                queues[i] = wsq;
                m_wsQueues = queues;
            }
        }

        /// <summary>Remove a work-stealing queue from the set of queues.</summary> 
        /// <param name="wsq">The work-stealing queue to remove.</param> 
        private void RemoveWsq(WorkStealingQueue<Task> wsq)
        {
            lock (m_wsQueues)
            {
                // Find the queue, and if/when we find it, null out its array slot 
                for (int i = 0; i < m_wsQueues.Length; i++)
                {
                    if (m_wsQueues[i] == wsq)
                    {
                        m_wsQueues[i] = null;
                    }
                }
            }
        }

        /// <summary> 
        /// The dispatch loop run by each thread in the scheduler. 
        /// </summary> 
        private void DispatchLoop()
        {
            // Create a new queue for this thread, store it in TLS for later retrieval, 
            // and add it to the set of queues for this scheduler. 
            WorkStealingQueue<Task> wsq = new WorkStealingQueue<Task>();
            m_wsq = wsq;
            AddWsq(wsq);

            try
            {
                // Until there's no more work to do... 
                while (true)
                {
                    Task wi = null;

                    // Search order: (1) local WSQ, (2) global Q, (3) steals from other queues. 
                    if (!wsq.LocalPop(ref wi))
                    {
                        // We weren't able to get a task from the local WSQ 
                        bool searchedForSteals = false;
                        while (true)
                        {
                            lock (m_queue)
                            {
                                // If shutdown was requested, exit the thread. 
                                if (m_shutdown)
                                    return;

                                // (2) try the global queue. 
                                if (m_queue.Count != 0)
                                {
                                    // We found a work item! Grab it ... 
                                    wi = m_queue.Dequeue();
                                    break;
                                }
                                else if (searchedForSteals)
                                {
                                    // Note that we're not waiting for work, and then wait 
                                    m_threadsWaiting++;
                                    try { Monitor.Wait(m_queue); }
                                    finally { m_threadsWaiting--; }

                                    // If we were signaled due to shutdown, exit the thread. 
                                    if (m_shutdown)
                                        return;

                                    searchedForSteals = false;
                                    continue;
                                }
                            }

                            // (3) try to steal. 
                            WorkStealingQueue<Task>[] wsQueues = m_wsQueues;
                            int i;
                            for (i = 0; i < wsQueues.Length; i++)
                            {
                                WorkStealingQueue<Task> q = wsQueues[i];
                                if (q != null && q != wsq && q.TrySteal(ref wi)) break;
                            }

                            if (i != wsQueues.Length) break;

                            searchedForSteals = true;
                        }
                    }

                    // ...and Invoke it. 
                    TryExecuteTask(wi);
                }
            }
            finally
            {
                RemoveWsq(wsq);
            }
        }

        /// <summary>Signal the scheduler to shutdown and wait for all threads to finish.</summary> 
        public void Dispose()
        {
            m_shutdown = true;
            if (m_queue != null && m_threads.IsValueCreated)
            {
                var threads = m_threads.Value;
                lock (m_queue) Monitor.PulseAll(m_queue);
                for (int i = 0; i < threads.Length; i++) threads[i].Join();
            }
        }
    }

}
