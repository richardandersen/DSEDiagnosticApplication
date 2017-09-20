﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Patterns.Tasks;

namespace DSEDiagnosticConsoleApplication
{
    class Program
    {
        public static readonly DateTime RunDateTime = DateTime.Now;
        public static string CommandLineArgsString = null;
        public static bool DebugMode = false;

        #region Console

        static public ConsoleDisplay ConsoleDeCompressFiles = null;
        static public ConsoleDisplay ConsoleNonLogReadFiles = null;
        static public ConsoleDisplay ConsoleLogReadFiles = null;
        static public ConsoleDisplay ConsoleAnalyze = null;
        static public ConsoleDisplay ConsoleParsingDataTable = null;
        static public ConsoleDisplay ConsoleExcelWorkSheet = null;
        static public ConsoleDisplay ConsoleExcelWorkbook = null;
        static public ConsoleDisplay ConsoleWarnings = null;
        static public ConsoleDisplay ConsoleErrors = null;

        #endregion

        #region Exception/Progression Handlers

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Logger.Instance.Warn("Application Aborted");
            Program.ConsoleErrors.Increment("Aborted");

            Logger.Flush();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ConsoleDisplay.End();

            GCMonitor.GetInstance().StopGCMonitoring();

            ConsoleDisplay.Console.SetReWriteToWriterPosition();
            ConsoleDisplay.Console.WriteLine();

            Logger.Instance.FatalFormat("Unhandled Exception Occurred! Exception Is \"{0}\" ({1}) Terminating Processing...",
                                            e.ExceptionObject?.GetType(),
                                            e.ExceptionObject is System.Exception ? ((System.Exception)e.ExceptionObject).Message : "<Not an Exception Object>");

            if (e.ExceptionObject is System.Exception)
            {
                Logger.Instance.Error("Unhandled Exception", ((System.Exception)e.ExceptionObject));
                ConsoleDisplay.Console.WriteLine("Unhandled Exception of \"{0}\" occurred", ((System.Exception)e.ExceptionObject).GetType().Name);
            }

            Logger.Instance.Info("DSEDiagnosticConsoleApplication Main Ended due to unhandled exception");
            Logger.Flush();

            ConsoleDisplay.Console.WriteLine();
            Common.ConsoleHelper.Prompt("Press Return to Exit (Unhandled Exception)", ConsoleColor.Gray, ConsoleColor.DarkRed);
            Environment.Exit(-1);
        }

        private static void DiagnosticFile_OnException(object sender, DSEDiagnosticFileParser.ExceptionEventArgs eventArgs)
        {
            Logger.Instance.Error("Exception within DSEDiagnosticFileParser", eventArgs.Exception);
            ConsoleErrors.Increment("Exception within DSEDiagnosticFileParser");
        }

        private static void DiagnosticFile_OnProgression(object sender, DSEDiagnosticFileParser.ProgressionEventArgs eventArgs)
        {
            if(ConsoleDeCompressFiles != null)
            {
                if(sender is DSEDiagnosticFileParser.DiagnosticFile)
                {
                    var diagFile = (DSEDiagnosticFileParser.DiagnosticFile)sender;

                    if(diagFile.Catagory == DSEDiagnosticFileParser.DiagnosticFile.CatagoryTypes.LogFile)
                    {
                        if(eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.Start))
                        {
                            ConsoleLogReadFiles.Increment(diagFile.File);
                        }
                        else if(eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.End)
                                    || eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.Cancel))
                        {
                            ConsoleLogReadFiles.TaskEnd(diagFile.File);
                        }
                    }
                    else if (diagFile.Catagory == DSEDiagnosticFileParser.DiagnosticFile.CatagoryTypes.ZipFile)
                    {
                        if (eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.Start))
                        {
                            ConsoleDeCompressFiles.Increment(diagFile.File);
                        }
                        else if (eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.End)
                                    || eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.Cancel))
                        {
                            ConsoleDeCompressFiles.TaskEnd(diagFile.File);
                        }
                    }
                    else
                    {
                        if (eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.Start))
                        {
                            ConsoleNonLogReadFiles.Increment(diagFile.File);
                        }
                        else if (eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.End)
                                    || eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.Cancel))
                        {
                            ConsoleNonLogReadFiles.TaskEnd(diagFile.File);
                        }
                    }

                    if(eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.Cancel))
                    {
                        ConsoleWarnings.Increment(string.Format("Canceled processing for {0}", diagFile.File.FileName));
                    }

                    return;
                }

                if (eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.Process)
                      || eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.Collection))
                {
                    if (eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.Start))
                    {
                        ConsoleNonLogReadFiles.Increment(eventArgs.Message());
                    }
                    else if (eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.Cancel)
                                || eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.End))
                    {
                        ConsoleNonLogReadFiles.TaskEnd(eventArgs.Message());
                    }

                    if (eventArgs.Category.HasFlag(DSEDiagnosticFileParser.ProgressionEventArgs.Categories.Cancel))
                    {
                        ConsoleWarnings.Increment(eventArgs.Message());
                    }
                }
            }
        }

        private static void Instance_OnLoggingEvent(DSEDiagnosticLogger.Logger sender, DSEDiagnosticLogger.LoggingEventArgs eventArgs)
        {
            foreach (var item in eventArgs.LogInfo.LoggingEvents)
            {
                if(item.Level == log4net.Core.Level.Error || item.Level == log4net.Core.Level.Fatal)
                {
                    ConsoleErrors.Increment(string.Format(@"Log: {0:yyyy-MM-dd\ HH\:mm\:ss.fff}", item.TimeStamp));
                }
                else if(item.Level == log4net.Core.Level.Warn)
                {
                    ConsoleWarnings.Increment(string.Format(@"Log: {0:yyyy-MM-dd\ HH\:mm\:ss.fff}", item.TimeStamp));
                }

            }
        }

        #endregion

        static int Main(string[] args)
        {
            #region Setup Exception handling, Argument Parsering

            Common.TimeZones.Convert(DateTime.Now, "UTC");

            Logger.Instance.InfoFormat("Starting {0} ({1}) Vetsion: {2} RunAs: {3} RunTime Dir: {4}",
                                            Common.Functions.Instance.ApplicationName,
                                            Common.Functions.Instance.AssemblyFullName,
                                            Common.Functions.Instance.ApplicationVersion,
                                            Common.Functions.Instance.CurrentUserName,
                                            Common.Functions.Instance.ApplicationRunTimeDir);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            System.Console.CancelKeyPress += Console_CancelKeyPress;

            CommandLineArgsString = string.Join(" ", args);

            Logger.Instance.InfoFormat("DSEDiagnosticConsoleApplication Main Start with Args: {0}", CommandLineArgsString);
            DSEDiagnosticFileParser.DiagnosticFile.OnException += DiagnosticFile_OnException;
            DSEDiagnosticFileParser.DiagnosticFile.OnProgression += DiagnosticFile_OnProgression;
            Logger.Instance.OnLoggingEvent += Instance_OnLoggingEvent;

            #region Arguments
            {
                var consoleArgs = new ConsoleArguments();

                try
                {
                    if (!consoleArgs.ParseSetArguments(args))
                    {
                        Common.ConsoleHelper.Prompt("Press Return to Exit", ConsoleColor.Gray, ConsoleColor.DarkRed);
                        return 1;
                    }

                    if (ParserSettings.DiagnosticPath == null)
                    {
                        throw new ArgumentNullException("DiagnosticPath argument is required");
                    }

                    if (consoleArgs.Debug)
                    {
                        DebugMode = true;
                        Common.ConsoleHelper.Prompt("Attach Debugger and Press Return to Continue", ConsoleColor.Gray, ConsoleColor.DarkRed);
                        ConsoleDisplay.DisableAllConsoleWriter();
                    }
                }
                catch (CommandLineParser.Exceptions.CommandLineException e)
                {
                    ConsoleDisplay.Console.WriteLine(e.Message);
                    ConsoleDisplay.Console.WriteLine("CommandLine: '{0}'", CommandLineArgsString);

                    consoleArgs.ShowUsage();
                    Common.ConsoleHelper.Prompt("Press Return to Exit", ConsoleColor.Gray, ConsoleColor.DarkRed);
                    return 1;
                }
                catch (System.Exception e)
                {
                    ConsoleDisplay.Console.WriteLine(e.Message);
                    ConsoleDisplay.Console.WriteLine("CommandLine: '{0}'", CommandLineArgsString);

                    consoleArgs.ShowUsage();
                    Common.ConsoleHelper.Prompt("Press Return to Exit", ConsoleColor.Gray, ConsoleColor.DarkRed);
                    return 1;
                }

                if (ParserSettings.ExcelFilePath != null
                        && ParserSettings.ExcelFilePath.IsRelativePath
                        && ParserSettings.DiagnosticPath.IsAbsolutePath)
                {
                    IAbsolutePath absPath;

                    if (ParserSettings.ExcelFilePath.MakePathFrom((Common.IAbsolutePath)ParserSettings.DiagnosticPath, out absPath))
                    {
                        ParserSettings.ExcelFilePath = (Common.IFilePath)absPath;
                    }
                }

                if (ParserSettings.ExcelFilePath != null
                        && ParserSettings.ExcelFilePath.IsAbsolutePath
                        && ParserSettings.DiagnosticPath.IsRelativePath)
                {
                    IAbsolutePath absPath;

                    if (ParserSettings.ExcelFileTemplatePath.MakePathFrom((Common.IAbsolutePath)ParserSettings.ExcelFilePath, out absPath))
                    {
                        ParserSettings.ExcelFileTemplatePath = (Common.IFilePath)absPath;
                    }
                }

                Logger.Instance.InfoFormat("Settings:\r\n\t{0}", ParserSettings.SettingValues());
            }
            #endregion

            #region Console Display Setup

            ConsoleDisplay.Console.ClearScreen();
            ConsoleDisplay.Console.AdjustScreenStartBlock();

            ConsoleDisplay.Console.WriteLine(" ");
            ConsoleDisplay.Console.WriteLine("Diagnostic Folder Structure: \"{0}\"", ParserSettings.DiagFolderStruct);
            ConsoleDisplay.Console.WriteLine("Diagnostic Source Folder: \"{0}\"", ParserSettings.DiagnosticPath);
            ConsoleDisplay.Console.WriteLine("Excel Target File: \"{0}\"", ParserSettings.ExcelFilePath?.ToString()
                                                                                ?? string.Format(Properties.Settings.Default.ExcelFileNameGeneratedStringFormat,
                                                                                                    ParserSettings.DiagnosticPath,
                                                                                                    "<ClusterName>",
                                                                                                    RunDateTime,
                                                                                                    ParserSettings.ExcelFileTemplatePath?.FileExtension ?? DSEDiagtnosticToExcel.LibrarySettings.ExcelFileExtension));

            ConsoleDisplay.Console.WriteLine(" ");

            ConsoleDeCompressFiles = new ConsoleDisplay("Decompression Completed: {0} Working: {1} Task: {2}");
            ConsoleNonLogReadFiles = new ConsoleDisplay("Non-Log Completed: {0} Working: {1} Task: {2}");
            ConsoleLogReadFiles = new ConsoleDisplay("Log Completed: {0}  Working: {1} Task: {2}");
            ConsoleAnalyze = new ConsoleDisplay("Analyze Processing: {0}  Working: {1} Task: {2}");
            ConsoleParsingDataTable = new ConsoleDisplay("DataTable Processing: {0}  Working: {1} Task: {2}");
            ConsoleExcelWorkSheet = new ConsoleDisplay("Excel WorkSheet: {0}  Working: {1} Task: {2}");
            ConsoleExcelWorkbook = new ConsoleDisplay("Excel WorkBook: {0}  Working: {1} Task: {2}");
            ConsoleWarnings = new ConsoleDisplay("Warnings: {0} Last: {2}", 2, false);
            ConsoleErrors = new ConsoleDisplay("Errors: {0} Last: {2}", 2, false);

            ConsoleDisplay.Console.ReserveRwWriteConsoleSpace(ConsoleDisplay.ConsoleRunningTimerTag, 2, -1);
            ConsoleDisplay.Console.ReserveRwWriteConsoleSpace("Prompt", 2, -1);
            ConsoleDisplay.Console.AdjustScreenToFitBasedOnStartBlock();

            ConsoleDisplay.Start();

            #endregion

            GCMonitor.GetInstance().StartGCMonitoring();

            #endregion

            #region DSEDiagnosticFileParser
            string defaultCluster = null;

            if (ParserSettings.DiagFolderStruct == ParserSettings.DiagFolderStructOptions.OpsCtrDiagStruct
                    && !DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp.HasValue)
            {
                var regEx = new System.Text.RegularExpressions.Regex(Properties.Settings.Default.OpsCenterDiagFolderRegEx,
                                                                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var regexMatch = regEx.Match(ParserSettings.DiagnosticPath.Name);

                if (regexMatch.Success)
                {
                    var clusterName = regexMatch.Groups["CLUSTERNAME"].Value;
                    var diagTS = regexMatch.Groups["TS"].Value;
                    var diagTZ = regexMatch.Groups["TZ"].Value;
                    DateTime diagDateTime;

                    //defaultCluster = clusterName.Trim(); //Turns out OpsCenter translate "-" into "_" for cluster name in the folder name and there is no way of knowing if it has been translated
                    if (DateTime.TryParseExact(diagTS,
                                                Properties.Settings.Default.OpsCenterDiagFolderDateTimeFmt,
                                                System.Globalization.CultureInfo.InvariantCulture,
                                                System.Globalization.DateTimeStyles.None,
                                                out diagDateTime))
                    {
                        DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp = diagDateTime.ConvertToOffSet(diagTZ);
                    }

                    Logger.Instance.InfoFormat("Using Diagnostic Tar-Ball \"{0}\" with OpsCenter Capture Date of {1}",
                                                    clusterName,
                                                    DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp.HasValue
                                                        ? DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp.Value.ToString(@"yyyy-MM-dd HH:mm:ss zzz")
                                                        : "<Unkown>");
                }
            }

            if (ParserSettings.OnlyIncludeXHrsofLogsFromDiagCaptureTime > 0
                && DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp.HasValue)
            {
                DSEDiagnosticFileParser.file_cassandra_log4net.LogTimeRange = new DateTimeOffsetRange(DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp.Value - TimeSpan.FromHours(ParserSettings.OnlyIncludeXHrsofLogsFromDiagCaptureTime), DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp.Value);
            }

            if (DSEDiagnosticFileParser.file_cassandra_log4net.LogTimeRange != null)
            {
                Logger.Instance.WarnFormat("Using UTC Log Range of {0} to {1}",
                                                DSEDiagnosticFileParser.file_cassandra_log4net.LogTimeRange.Min == DateTimeOffset.MinValue
                                                            ? "MinValue"
                                                            : DSEDiagnosticFileParser.file_cassandra_log4net.LogTimeRange.Min.ToString(@"yyyy-MM-dd HH:mm:ss zzz"),
                                                 DSEDiagnosticFileParser.file_cassandra_log4net.LogTimeRange.Max == DateTimeOffset.MaxValue
                                                            ? "MaxValue"
                                                            : DSEDiagnosticFileParser.file_cassandra_log4net.LogTimeRange.Max.ToString(@"yyyy-MM-dd HH:mm:ss zzz"));
            }

            var cancellationSource = new System.Threading.CancellationTokenSource();
            var diagParserTask = DSEDiagnosticFileParser.DiagnosticFile.ProcessFile(ParserSettings.DiagnosticPath,
                                                                                     null,
                                                                                     defaultCluster,
                                                                                     null,
                                                                                     cancellationSource,
                                                                                     ParserSettings.AdditionalFilesForParsingClass);

            diagParserTask.Then(ignore => { ConsoleNonLogReadFiles.Terminate(); ConsoleLogReadFiles.Terminate(); ConsoleDeCompressFiles.Terminate(); });
            diagParserTask.Then(ignore =>
            {
                foreach (var uaNode in DSEDiagnosticLibrary.Cluster.GetUnAssocaitedNodes())
                {
                    if (uaNode.DataCenter == null || uaNode.DataCenter is DSEDiagnosticLibrary.PlaceholderDataCenter)
                    {
                        Logger.Instance.WarnFormat("Node \"{0}\" in DC \"{1}\" was detected as being Orphaned. This may indicated a processing error!", uaNode, uaNode.DataCenter);
                    }
                }
            });

            diagParserTask.ContinueWith(task => CanceledFaultProcessing(task.Exception),
                                            TaskContinuationOptions.OnlyOnFaulted);
            diagParserTask.ContinueWith(task => CanceledFaultProcessing(null),
                                            TaskContinuationOptions.OnlyOnCanceled);

            #endregion

            #region Analytics
            var logInfoStatsTask = diagParserTask.ContinueWith((task, ignore) =>
                                        {
                                            ConsoleAnalyze.Increment("LogFileStats");
                                            var cluster = DSEDiagnosticLibrary.Cluster.Clusters.FirstOrDefault(c => !c.IsMaster) ?? DSEDiagnosticLibrary.Cluster.Clusters.First();
                                            var analyticInstance = new DSEDiagnosticAnalytics.LogFileStats(cluster, cancellationSource.Token);

                                            return analyticInstance.ComputeStats();
                                        },
                                        null,
                                        cancellationSource.Token,
                                        TaskContinuationOptions.OnlyOnRanToCompletion,
                                        TaskScheduler.Default);
            logInfoStatsTask.Then(result => ConsoleAnalyze.TaskEnd("LogFileStats"));

            logInfoStatsTask.Then(result => ConsoleAnalyze.Terminate());
            logInfoStatsTask.ContinueWith(task => CanceledFaultProcessing(task.Exception),
                                                        TaskContinuationOptions.OnlyOnFaulted);
            logInfoStatsTask.ContinueWith(task => CanceledFaultProcessing(null),
                                                        TaskContinuationOptions.OnlyOnCanceled);
            #endregion

            #region Load DataTables
            var datatableTasks = new List<Task<System.Data.DataTable>>();
            var loadAllDataTableTask = Common.Patterns.Tasks.CompletionExtensions.CompletedTask<System.Data.DataSet>();
            {
                var cluster = DSEDiagnosticLibrary.Cluster.Clusters.FirstOrDefault(c => !c.IsMaster) ?? DSEDiagnosticLibrary.Cluster.Clusters.First();

                if (cluster.IsMaster)
                {
                    Logger.Instance.Debug("Load Data table is waiting on non-Master cluster");
                    ConsoleParsingDataTable.Increment("Load Data table is waiting on non-Master cluster");
                    Common.Patterns.Threading.LockFree.SpinWait(() =>
                    {
                        System.Threading.Thread.Sleep(500);
                        return !(cluster = DSEDiagnosticLibrary.Cluster.Clusters.FirstOrDefault(c => !c.IsMaster) ?? DSEDiagnosticLibrary.Cluster.Clusters.First()).IsMaster;
                    });

                    ConsoleParsingDataTable.TaskEnd("Load Data table is waiting on non-Master cluster");
                    Logger.Instance.DebugFormat("Load Data table is using Cluster \"{0}\"", cluster.Name);
                }

                var loadDataTables = new DSEDiagnosticToDataTable.IDataTable[]
                {
                    new DSEDiagnosticToDataTable.ConfigDataTable(cluster, cancellationSource),
                    new DSEDiagnosticToDataTable.CQLDDLDataTable(cluster, cancellationSource, ParserSettings.IgnoreKeySpaces.ToArray()),
                    new DSEDiagnosticToDataTable.KeyspaceDataTable(cluster, cancellationSource, ParserSettings.IgnoreKeySpaces.ToArray()),
                    new DSEDiagnosticToDataTable.MachineDataTable(cluster, cancellationSource),
                    new DSEDiagnosticToDataTable.NodeDataTable(cluster, cancellationSource),
                    new DSEDiagnosticToDataTable.TokenRangesDataTable(cluster, cancellationSource),
                    new DSEDiagnosticToDataTable.CFStatsDataTable(cluster, cancellationSource, ParserSettings.IgnoreKeySpaces.ToArray(), ParserSettings.WarnWhenKSTblIsDetected.ToArray()),
                    new DSEDiagnosticToDataTable.TPStatsDataTable(cluster, cancellationSource)
                };

                loadDataTables.ForEach(ldtInstance =>
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
                });

                datatableTasks.Add(logInfoStatsTask.ContinueWith((task, ignore) =>
                                                        {
                                                            var clusterInstance = DSEDiagnosticLibrary.Cluster.Clusters.FirstOrDefault(c => !c.IsMaster) ?? DSEDiagnosticLibrary.Cluster.Clusters.First();
                                                            var dtLoadInstance = new DSEDiagnosticToDataTable.LogInfoDataTable(clusterInstance, task.Result, cancellationSource);

                                                            ConsoleParsingDataTable.Increment(dtLoadInstance.Table.TableName);
                                                            return dtLoadInstance.LoadTable();
                                                        },
                                                        null,
                                                        cancellationSource.Token,
                                                        TaskContinuationOptions.OnlyOnRanToCompletion,
                                                        TaskScheduler.Default));
                datatableTasks.Last().Then(result => ConsoleParsingDataTable.TaskEnd(result.TableName));

                loadAllDataTableTask = Task.Factory.ContinueWhenAll(datatableTasks.ToArray(),
                                                                    dtTasks =>
                                                                    {
                                                                        var dataSet = new System.Data.DataSet(string.Format("DSEDiagnostic Cluster {0}", cluster.Name));
                                                                        var dataTables = dtTasks.Select(t => t.Result).ToArray();
                                                                        //Work around where the default view is reset upon being added to the dataset....
                                                                        var dataViewProps = dataTables.Select(t => new Tuple<string,string,System.Data.DataViewRowState>(t.DefaultView.Sort, t.DefaultView.RowFilter, t.DefaultView.RowStateFilter)).ToArray();

                                                                        dataSet.Tables.AddRange(dataTables);

                                                                        for(int nIdx = 0; nIdx < dataTables.Count(); ++nIdx)
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

            }
            #endregion

            #region Load Excel
            {
                if (ParserSettings.ExcelFilePath == null)
                {
                    var cluster = DSEDiagnosticLibrary.Cluster.Clusters.FirstOrDefault(c => !c.IsMaster) ?? DSEDiagnosticLibrary.Cluster.Clusters.First();

                    if (cluster.IsMaster)
                    {
                        Logger.Instance.Debug("Load Excel is waiting on non-Master cluster");
                        ConsoleExcelWorkbook.Increment("Load Excel is waiting on non-Master cluster");
                        Common.Patterns.Threading.LockFree.SpinWait(() =>
                        {
                            System.Threading.Thread.Sleep(500);
                            return !(cluster = DSEDiagnosticLibrary.Cluster.Clusters.FirstOrDefault(c => !c.IsMaster) ?? DSEDiagnosticLibrary.Cluster.Clusters.First()).IsMaster;
                        });

                        ConsoleExcelWorkbook.TaskEnd("Load Excel is waiting on non-Master cluster");
                        Logger.Instance.DebugFormat("Load Excel is using Cluster \"{0}\"", cluster.Name);
                    }

                    ParserSettings.ExcelFilePath = Common.Path.PathUtils.BuildFilePath(string.Format(Properties.Settings.Default.ExcelFileNameGeneratedStringFormat,
                                                                                                        ParserSettings.DiagnosticPath,
                                                                                                        cluster.IsMaster ? "MasterCluster" : cluster.Name,
                                                                                                        RunDateTime,
                                                                                                        ParserSettings.ExcelFileTemplatePath?.FileExtension ?? DSEDiagtnosticToExcel.LibrarySettings.ExcelFileExtension));
                }

                var loadExcel = new DSEDiagtnosticToExcel.LoadDataSet(loadAllDataTableTask,
                                                                            ParserSettings.ExcelFilePath,
                                                                            ParserSettings.ExcelFileTemplatePath,
                                                                            cancellationSource);

                loadExcel.OnAction += (DSEDiagtnosticToExcel.IExcel sender, string action) =>
                                        {
                                            if(action == "Begin Loading")
                                            {
                                                if (sender.LoadTo == DSEDiagtnosticToExcel.LoadToTypes.WorkBook)
                                                {
                                                    ConsoleExcelWorkbook.Increment(sender.ExcelTargetWorkbook);
                                                }
                                                else
                                                {
                                                    ConsoleExcelWorkSheet.Increment(sender.WorkSheetName);
                                                }
                                            }
                                            else if(action == "Loaded")
                                            {
                                                if (sender.LoadTo == DSEDiagtnosticToExcel.LoadToTypes.WorkBook)
                                                {
                                                    ConsoleExcelWorkbook.TaskEnd(sender.ExcelTargetWorkbook);
                                                }
                                                else
                                                {
                                                    ConsoleExcelWorkSheet.TaskEnd(sender.WorkSheetName);
                                                }
                                            }
                                            else if(action == "Workbook Saved")
                                            {
                                                ConsoleExcelWorkbook.Increment(sender.ExcelTargetWorkbook);
                                            }
                                        };
                var excelTask = loadExcel.Load();

                var excelUpdateAppInfoTask = Task.Factory.ContinueWhenAll(new Task[] { diagParserTask, excelTask }, tasks =>
                {
                    var parsedResults = ((Task<IEnumerable<DSEDiagnosticFileParser.DiagnosticFile>>)tasks[0]).Result;

                    var loadAppInfo = new DSEDiagtnosticToExcel.ApplicationInfoExcel(ParserSettings.ExcelFilePath);

                    loadAppInfo.ApplicationInfo.Aborted = AlreadyCanceled || tasks.Any(t => t.IsCanceled);
                    loadAppInfo.ApplicationInfo.Erroes = (int)ConsoleErrors.Counter;
                    loadAppInfo.ApplicationInfo.Warnings = (int)ConsoleWarnings.Counter;
                    loadAppInfo.ApplicationInfo.ApplicationArgs = CommandLineArgsString;
                    loadAppInfo.ApplicationInfo.ApplicationAssemblyDir = Common.Functions.Instance.AssemblyDir;
                    loadAppInfo.ApplicationInfo.ApplicationLibrarySettings = ParserSettings.SettingValues();
                    loadAppInfo.ApplicationInfo.ApplicationName = Common.Functions.Instance.ApplicationName;
                    loadAppInfo.ApplicationInfo.ApplicationStartEndTime = new DateTimeRange(RunDateTime, DateTime.Now);
                    loadAppInfo.ApplicationInfo.ApplicationVersion = Common.Functions.Instance.ApplicationVersion;
                    loadAppInfo.ApplicationInfo.DiagnosticDirectory = ParserSettings.DiagnosticPath.PathResolved;
                    loadAppInfo.ApplicationInfo.WorkingDir = Common.Functions.Instance.ApplicationRunTimeDir;

                    var resultItems = from result in parsedResults
                                      group result by new { DC = result.Node?.DataCenter?.Name, Node = result.Node?.Id.NodeName(), Class = result.GetType().Name, Category = result.Catagory, MapperId = result.MapperId } into g
                                      select new
                                      {
                                          DC = g.Key.DC,
                                          Node = g.Key.Node,
                                          Class = g.Key.Class,
                                          Category = g.Key.Category.ToString(),
                                          MapperId = g.Key.MapperId,
                                          NbrTasksCompleted = g.Count(i => i.Processed),
                                          NbrItemsParsed = g.Sum(i => i.NbrItemsParsed),
                                          NbrItemsGenerated = g.Sum(i => i.NbrItemGenerated),
                                          NbrTasksCanceled = g.Count(i => i.Canceled),
                                          NbrExceptions = g.Count(i => i.Exception != null || (i.ExceptionStrings?.HasAtLeastOneElement() ?? false))
                                      };

                    foreach (var item in resultItems)
                    {
                        loadAppInfo.ApplicationInfo.Results.Add(new DSEDiagtnosticToExcel.ApplicationInfoExcel.ApplInfo.ResultInfo()
                        {
                            Catagory = item.Category,
                            MapperClass = item.Class,
                            DataCenter = item.DC,
                            MapperId = item.MapperId,
                            NbrExceptions = item.NbrExceptions,
                            NbrItemsGenerated = item.NbrItemsGenerated,
                            NbrItemsParsed = item.NbrItemsParsed,
                            NbrTasksCanceled = item.NbrTasksCanceled,
                            NbrTasksCompleted = item.NbrTasksCompleted,
                            Node = item.Node
                        });
                    }
                    loadAppInfo.Load();
                });

                excelUpdateAppInfoTask.Then(() => { ConsoleExcelWorkbook.Terminate(); ConsoleExcelWorkSheet.Terminate(); });

                excelUpdateAppInfoTask.Wait();
            }
            #endregion

            #region Termiate

            ConsoleDisplay.End();

            GCMonitor.GetInstance().StopGCMonitoring();

            Logger.Instance.Info("DSEDiagnosticConsoleApplication Main End");

            ConsoleDisplay.Console.SetReWriteToWriterPosition();
            ConsoleDisplay.Console.WriteLine();
            Common.ConsoleHelper.Prompt("Press Return to Exit", ConsoleColor.Gray, ConsoleColor.DarkRed);

            #endregion

            return 0;
        }

        #region Canceled Processing
        static public volatile bool AlreadyCanceled = false;
        static bool _AlreadyCanceled = false;

        static void CanceledFaultProcessing(System.Exception ex)
        {
            if (!AlreadyCanceled && !Common.Patterns.Threading.LockFree.Exchange(ref _AlreadyCanceled, true))
            {
                AlreadyCanceled = true;
                ConsoleDisplay.End();

                GCMonitor.GetInstance().StopGCMonitoring();

                if(ex != null)
                {
                    Logger.Instance.Error("Fault Detected", ex);
                }

                Logger.Instance.Info("DSEDiagnosticConsoleApplication Main Ended from Fault or Canceled");

                ConsoleDisplay.Console.SetReWriteToWriterPosition();
                ConsoleDisplay.Console.WriteLine();
                Common.ConsoleHelper.Prompt(@"Press Return to Exit (Fault/Canceled)", ConsoleColor.Gray, ConsoleColor.DarkRed);
                Environment.Exit(-1);
            }
        }
        #endregion
    }
}
