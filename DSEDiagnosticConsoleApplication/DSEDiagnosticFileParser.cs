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

        static IList<Tuple<IPath,DateTimeOffset>> DetermineNodeAgentDirectories(IDirectoryPath diagPath)
        {
            var agentDirs = new List<Tuple<IPath, DateTimeOffset>>();
            var childrenDirs = diagPath.Children().Where(i => i.IsDirectoryPath);
            
            foreach (IDirectoryPath pDir in childrenDirs)
            {
                var foundCaptureInfo = DSEDiagnosticFileParser.LibrarySettings.DetermineNodeCaptureInfo(pDir.Name);
                bool matched = false;

                if (foundCaptureInfo != null)
                {
                    agentDirs.Add(new Tuple<IPath, DateTimeOffset>(pDir, foundCaptureInfo.Item1));                        
                    matched = true;
                }

                if(!matched)
                {
                    agentDirs.AddRange(DetermineNodeAgentDirectories(pDir));
                }
            }

            return agentDirs;
        }

        static void CassandraLogEventHandler(DSEDiagnosticFileParser.file_cassandra_log4net sender,
                                                DSEDiagnosticFileParser.file_cassandra_log4net.LogProcessingCreationArgs eventArgs)
        {
            var logEvtAnalytics = new DSEDiagnosticAnalytics.CassandraLogEvent(sender);

            sender.OnLogEvent += logEvtAnalytics.LogEventCallBack;
            sender.Tag = logEvtAnalytics;

            Logger.Instance.InfoFormat("Registering CassandraLogEvent Analytics to LogFile {0}", sender.ShortFilePath);
        }

        public static Task<IEnumerable<DSEDiagnosticFileParser.DiagnosticFile>> ProcessDSEDiagnosticFileParser(System.Threading.CancellationTokenSource cancellationSource)
        {
            string defaultCluster = null;

            if (ParserSettings.DiagFolderStruct == ParserSettings.DiagFolderStructOptions.OpsCtrDiagStruct
                    && !DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp.HasValue)
            {
                var captureInfo = DSEDiagnosticFileParser.LibrarySettings.DetermineClusterCaptureInfo(ParserSettings.DiagnosticPath.Name);
                
                if (captureInfo != null)
                {
                    //defaultCluster = clusterName.Trim(); //Turns out OpsCenter translate "-" into "_" for cluster name in the folder name and there is no way of knowing if it has been translated
                    DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp = captureInfo.Item2;
                        
                    Logger.Instance.InfoFormat("Using Diagnostic Tar-Ball \"{0}\" with OpsCenter Capture Date of {1} ({2})",
                                                    captureInfo.Item1,
                                                    DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp.HasValue
                                                        ? DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp.Value.ToString(@"yyyy-MM-dd HH:mm:ss zzz")
                                                        : "<Unkown>",
                                                    captureInfo.Item3);
                }
            }

            if(ParserSettings.DiagFolderStruct == ParserSettings.DiagFolderStructOptions.NodeAgentDiagStruct)
            {
                var nodeDir = ParserSettings.DiagnosticPath.MakeChild("nodes");
                var agentDirs = DetermineNodeAgentDirectories(ParserSettings.DiagnosticPath);

                if (!nodeDir.Exist())
                {                    
                    foreach (var dirDte in agentDirs)
                    {
                        Logger.Instance.InfoFormat("Found Node-Agent Diagnostic Tar-Ball \"{0}\" with Capture Date of {1}",
                                                   dirDte.Item1,
                                                   dirDte.Item2);
                        if (!dirDte.Item1.Move(nodeDir.MakeChild(dirDte.Item1.Name)))
                        {
                            Logger.Instance.ErrorFormat("Node-Agent Diagnostic Tar-Ball \"{0}\" move to \"{1}\" failed!",
                                                           dirDte.Item1,
                                                           nodeDir.MakeChild(dirDte.Item1.Name));
                            throw new System.IO.IOException(string.Format("Node-Agent Diagnostic Tar-Ball \"{0}\" move to \"{1}\" failed!",
                                                                           dirDte.Item1,
                                                                           nodeDir.MakeChild(dirDte.Item1.Name)));
                        }
                    }                    
                }
                if (agentDirs.HasAtLeastOneElement())
                    DSEDiagnosticFileParser.LibrarySettings.CaptureDateTimeRange = new DateTimeOffsetRange(agentDirs.Min(i => i.Item2), agentDirs.Max(i => i.Item2));
            }

            if (ParserSettings.OnlyIncludeXHrsofLogsFromDiagCaptureTime > 0
                && DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp.HasValue)
            {
                ParserSettings.LogRestrictedTimeRange = new DateTimeOffsetRange(DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp.Value - TimeSpan.FromHours(ParserSettings.OnlyIncludeXHrsofLogsFromDiagCaptureTime), DSEDiagnosticLibrary.DSEInfo.NodeToolCaptureTimestamp.Value);
            }

            if (ParserSettings.LogRestrictedTimeRange != null)
            {
                Logger.Instance.WarnFormat("Using UTC Log Range of {0} to {1}",
                                                ParserSettings.LogRestrictedTimeRange.Min == DateTimeOffset.MinValue
                                                            ? "MinValue"
                                                            : ParserSettings.LogRestrictedTimeRange.Min.ToString(@"yyyy-MM-dd HH:mm:ss zzz"),
                                                 ParserSettings.LogRestrictedTimeRange.Max == DateTimeOffset.MaxValue
                                                            ? "MaxValue"
                                                            : ParserSettings.LogRestrictedTimeRange.Max.ToString(@"yyyy-MM-dd HH:mm:ss zzz"));
            }

            DSEDiagnosticFileParser.LibrarySettings.IgnoreWarningsErrosInKeySpaces = ParserSettings.IgnoreKeySpaces;
            DSEDiagnosticFileParser.file_cassandra_log4net.OnLogProcessingCreation += CassandraLogEventHandler;
            
            var diagParserTask = DSEDiagnosticFileParser.DiagnosticFile.ProcessFile(ParserSettings.DiagnosticPath,
                                                                                     clusterName: ParserSettings.ClusterName ?? defaultCluster,
                                                                                     clusterHashCode: ParserSettings.ClusterHashCode,
                                                                                     dseVersion: ParserSettings.DSEVersion,
                                                                                     cancellationSource: cancellationSource,
                                                                                     additionalFilesForClass: ParserSettings.AdditionalFilesForParsingClass,
                                                                                     onlyNodes: ParserSettings.OnlyNodes == null || ParserSettings.OnlyNodes.Count == 0
                                                                                                    ? null
                                                                                                    : ParserSettings.OnlyNodes
                                                                                                        .Select(n => DSEDiagnosticLibrary.NodeIdentifier.Create(n)));

            diagParserTask.Then(task =>
            {
                foreach (var uaNode in DSEDiagnosticLibrary.Cluster.GetUnAssocaitedNodes())
                {
                    if (uaNode.DataCenter == null || uaNode.DataCenter is DSEDiagnosticLibrary.PlaceholderDataCenter)
                    {
                        Logger.Instance.WarnFormat("Node \"{0}\" in DC \"{1}\" was detected as being Orphaned. This may indicated a processing error!", uaNode, uaNode.DataCenter);
                    }
                }
            });
            diagParserTask.ContinueWith(task =>
                {
                    DSEDiagnosticFileParser.file_cassandra_log4net.OnLogProcessingCreation -= CassandraLogEventHandler;
                    DSEDiagnosticAnalytics.CassandraLogEvent.CleanUp();
                },
                                            TaskContinuationOptions.OnlyOnRanToCompletion);
            diagParserTask.Then(ignore => { ConsoleTasksReadFiles.Terminate(); ConsoleLogReadFiles.Terminate(); ConsoleDeCompressFiles.Terminate(); });

            diagParserTask.ContinueWith(task => CanceledFaultProcessing(task.Exception),
                                            TaskContinuationOptions.OnlyOnFaulted);
            diagParserTask.ContinueWith(task => CanceledFaultProcessing(null),
                                            TaskContinuationOptions.OnlyOnCanceled);

            return diagParserTask;
        }
    }
}
