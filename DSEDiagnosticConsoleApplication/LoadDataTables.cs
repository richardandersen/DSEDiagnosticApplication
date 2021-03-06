﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Patterns.Tasks;

namespace DSEDiagnosticConsoleApplication
{
    partial class Program
    {
        public static Task<System.Data.DataSet> LoadDataTables(Task<IEnumerable<DSEDiagnosticFileParser.DiagnosticFile>> diagParserTask,
                                                                Task<IEnumerable<DSEDiagnosticLibrary.IAggregatedStats>>[] aggStatsTask,
                                                                System.Threading.CancellationTokenSource cancellationSource,
                                                                Guid? sessionGuid = null)
        {
            var datatableTasks = new List<Task<System.Data.DataTable>>();
            var loadAllDataTableTask = Common.Patterns.Tasks.CompletionExtensions.CompletedTask<System.Data.DataSet>();
            {
                var cluster = DSEDiagnosticLibrary.Cluster.GetCurrentOrMaster();

                if (cluster.IsMaster
                        && cluster.Nodes.HasAtLeastOneElement())
                {
                    System.Threading.Thread.Sleep(500);
                    cluster = DSEDiagnosticLibrary.Cluster.GetCurrentOrMaster();

                    if (cluster.IsMaster)
                    {
                        if (diagParserTask.IsCompleted)
                        {                          
                            Logger.Instance.Error("File Parsing tasks has completed but Cluster has not been properly defined! This usually indicates some type of issues with the diagnostic files.");
                            ConsoleErrors.Increment("File Parsing Issues detected. Cluster not properly defined!");
                        }
                        else if (diagParserTask.IsFaulted || diagParserTask.IsCanceled)
                        {
                            Logger.Instance.Warn("File Parsing tasks failed or was canceled! Cluster not properly defined.");
                            ConsoleWarnings.Increment("File Parsing tasks failed or was canceled! Cluster not properly defined.");
                        }
                        else
                        {
                            Logger.Instance.Debug("Load Data table is waiting on non-Master cluster");
                            ConsoleParsingDataTable.Increment("Load Data table is waiting on non-Master cluster");
                            Common.Patterns.Threading.LockFree.SpinWait(() =>
                            {
                                System.Threading.Thread.Sleep(500);
                                return !(cluster = DSEDiagnosticLibrary.Cluster.GetCurrentOrMaster()).IsMaster;
                            });

                            ConsoleParsingDataTable.TaskEnd("Load Data table is waiting on non-Master cluster");
                            Logger.Instance.DebugFormat("Load Data table is using Cluster \"{0}\"", cluster.Name);
                        }
                    }
                }

                var loadDataTables = new DSEDiagnosticToDataTable.IDataTable[]
                {                   
                };

                var loadDataTablesWaitLogAnalysis = new DSEDiagnosticToDataTable.IDataTable[]
                {
                    new DSEDiagnosticToDataTable.CQLDDLDataTable(cluster, cancellationSource, ParserSettings.IgnoreKeySpaces.ToArray(), sessionGuid),
                    new DSEDiagnosticToDataTable.KeyspaceDataTable(cluster,
                                                                    cancellationSource,
                                                                    ParserSettings.IgnoreKeySpaces.ToArray(),
                                                                    ParserSettings.WhiteListKeyspaceInWS.ToArray(),
                                                                    sessionGuid),
                    new DSEDiagnosticToDataTable.ConfigDataTable(cluster, cancellationSource, sessionGuid),
                    new DSEDiagnosticToDataTable.MachineDataTable(cluster, cancellationSource, sessionGuid),
                    new DSEDiagnosticToDataTable.TokenRangesDataTable(cluster, cancellationSource, sessionGuid),
                    ParserSettings.OldExcelWorksheets ? new DSEDiagnosticToDataTable.CFStatsDataTable(cluster, cancellationSource, ParserSettings.IgnoreKeySpaces.ToArray(), ParserSettings.WarnWhenKSTblIsDetected.ToArray(), sessionGuid) : null,
                    ParserSettings.OldExcelWorksheets ? new DSEDiagnosticToDataTable.TPStatsDataTable(cluster, cancellationSource, sessionGuid) : null,
                    ParserSettings.OldExcelWorksheets ? null : new DSEDiagnosticToDataTable.AggregatedStatsDataTable(cluster, cancellationSource, ParserSettings.IgnoreKeySpaces.ToArray(), ParserSettings.WarnWhenKSTblIsDetected.ToArray(), sessionGuid),
                    new DSEDiagnosticToDataTable.MultiInstanceDataTable(cluster, cancellationSource, sessionGuid),
                    new DSEDiagnosticToDataTable.NodeConfigChanges(cluster, cancellationSource, sessionGuid),
                    new DSEDiagnosticToDataTable.LogAggregationDataTable(cluster, cancellationSource, ParserSettings.IgnoreKeySpaces.ToArray(), ParserSettings.LogAggregationPeriod, sessionGuid),
                    new DSEDiagnosticToDataTable.DSEDevicesDataTable(cluster, cancellationSource, sessionGuid),
                    new DSEDiagnosticToDataTable.DataCenterDataTable(cluster, cancellationSource, sessionGuid),
                    new DSEDiagnosticToDataTable.LogInfoDataTable(cluster, null, cancellationSource, sessionGuid),
                    new DSEDiagnosticToDataTable.NodeDataTable(cluster, cancellationSource, sessionGuid),
                    new DSEDiagnosticToDataTable.NodeStateDataTable(cluster, cancellationSource, sessionGuid)
                };

                loadDataTables.ForEach(ldtInstance =>
                {
                    if (ldtInstance != null)
                    {
                        var taskDataTable = diagParserTask.ContinueWith((task, instance) =>
                                                {
                                                    ConsoleParsingDataTable.Increment(ldtInstance.Table.TableName);
                                                    return ((DSEDiagnosticToDataTable.IDataTable)instance).LoadTable();
                                                },
                                                ldtInstance,
                                                cancellationSource.Token,
                                                TaskContinuationOptions.OnlyOnRanToCompletion,
                                                TaskScheduler.Default);
                        datatableTasks.Add(taskDataTable);
                        taskDataTable.Then(result => ConsoleParsingDataTable.TaskEnd(result.TableName));
                    }
                });

                loadDataTablesWaitLogAnalysis.ForEach(ldtInstance =>
                {
                    if (ldtInstance != null)
                    {
                        var taskDataTable = aggStatsTask[0].ContinueWith((task, instance) =>
                                                {
                                                    ConsoleParsingDataTable.Increment(ldtInstance.Table.TableName);
                                                    if (instance is DSEDiagnosticToDataTable.ILogFileStats)
                                                        ((DSEDiagnosticToDataTable.ILogFileStats)instance).LogFileStats = task.Result;

                                                    return ((DSEDiagnosticToDataTable.IDataTable)instance).LoadTable();
                                                },
                                                ldtInstance,
                                                cancellationSource.Token,
                                                TaskContinuationOptions.OnlyOnRanToCompletion,
                                                TaskScheduler.Default);
                        datatableTasks.Add(taskDataTable);
                        taskDataTable.Then(result => ConsoleParsingDataTable.TaskEnd(result.TableName));
                    }
                });                

                datatableTasks.Add(aggStatsTask[1].ContinueWith((task, ignore) =>
                                                        {
                                                            var clusterInstance = DSEDiagnosticLibrary.Cluster.GetCurrentOrMaster();
                                                            var dtLoadInstance = new DSEDiagnosticToDataTable.CommonPartitionKeyDataTable(clusterInstance, task.Result, cancellationSource, sessionGuid);

                                                            ConsoleParsingDataTable.Increment(dtLoadInstance.Table.TableName);
                                                            return dtLoadInstance.LoadTable();
                                                        },
                                                        null,
                                                        cancellationSource.Token,
                                                        TaskContinuationOptions.OnlyOnRanToCompletion,
                                                        TaskScheduler.Default));
                datatableTasks.Last().Then(result => ConsoleParsingDataTable.TaskEnd(result.TableName));

                if (!ParserSettings.OldExcelWorksheets)
                {
                    datatableTasks.Add(datatableTasks.Last().ContinueWith((task, ignore) =>
                                                        {
                                                            var clusterInstance = DSEDiagnosticLibrary.Cluster.GetCurrentOrMaster();
                                                            var dtLoadInstance = new DSEDiagnosticToDataTable.TaggedItemsDataTable(clusterInstance, task.Result, cancellationSource, ParserSettings.IgnoreKeySpaces.ToArray(), sessionGuid);

                                                            ConsoleParsingDataTable.Increment(dtLoadInstance.Table.TableName);
                                                            return dtLoadInstance.LoadTable();
                                                        },
                                                       null,
                                                       cancellationSource.Token,
                                                       TaskContinuationOptions.OnlyOnRanToCompletion,
                                                       TaskScheduler.Default));
                    datatableTasks.Last().Then(result => ConsoleParsingDataTable.TaskEnd(result.TableName));                    
                }
                    


                loadAllDataTableTask = Task.Factory.ContinueWhenAll(datatableTasks.ToArray(),
                                                                    dtTasks =>
                                                                    {
                                                                        var dataSet = new System.Data.DataSet(string.Format("DSEDiagnostic Cluster {0}", cluster.Name));
                                                                        var dataTables = dtTasks.Select(t => t.Result).ToArray();
                                                                        //Work around where the default view is reset upon being added to the dataset....
                                                                        var dataViewProps = dataTables.Select(t => new Tuple<string, string, System.Data.DataViewRowState>(t.DefaultView.Sort, t.DefaultView.RowFilter, t.DefaultView.RowStateFilter)).ToArray();

                                                                        dataSet.Tables.AddRange(dataTables);

                                                                        for (int nIdx = 0; nIdx < dataTables.Count(); ++nIdx)
                                                                        {
                                                                            dataTables[nIdx].DefaultView.ApplyDefaultSort = false;
                                                                            dataTables[nIdx].DefaultView.AllowDelete = false;
                                                                            dataTables[nIdx].DefaultView.AllowEdit = false;
                                                                            dataTables[nIdx].DefaultView.AllowNew = false;
                                                                            dataTables[nIdx].DefaultView.Sort = dataViewProps[nIdx].Item1;
                                                                            dataTables[nIdx].DefaultView.RowFilter = dataViewProps[nIdx].Item2;
                                                                            dataTables[nIdx].DefaultView.RowStateFilter = dataViewProps[nIdx].Item3;                                                                            
                                                                        }

                                                                        return dataSet;
                                                                    },
                                                                    cancellationSource.Token);

                loadAllDataTableTask.Then(result => ConsoleParsingDataTable.Terminate());
                loadAllDataTableTask.ContinueWith(task => CanceledFaultProcessing(task.Exception),
                                                            TaskContinuationOptions.OnlyOnFaulted);
                loadAllDataTableTask.ContinueWith(task => CanceledFaultProcessing(null),
                                                            TaskContinuationOptions.OnlyOnCanceled);

                return loadAllDataTableTask;
            }
        }
    }
}
