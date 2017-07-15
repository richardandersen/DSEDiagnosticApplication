﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSEDiagnosticLibrary;
using DSEDiagnosticLogger;
using Common;
using Common.Path;
using Common.Patterns.Threading;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace DSEDiagnosticFileParser
{
    public abstract class DiagnosticFile
    {
		public enum CatagoryTypes
		{
			Unknown = 0,
			ConfigurationFile,
			LogFile,
            AchievedLogFile,
            CommandOutputFile,
			SystemOutputFile,
            CQLFile,
            ZipFile,
            TransformFile
		}

        #region Events

        /// <summary>
        ///     Called when an exception is caught and processed.
        /// </summary>
        public static event ExceptionEventArgs.EventHandler OnException;

        public static bool InvokeExceptionEvent(DiagnosticFile sender,
                                                    System.Exception exception,
                                                    System.Threading.CancellationTokenSource cancellationTokenSource,
                                                    object[] associatedObjects)
        {
            return ExceptionEventArgs.InvokeEvent(sender, exception, cancellationTokenSource, associatedObjects, OnException);
        }

        public static bool InvokeExceptionEvent(string sender,
                                                    System.Exception exception,
                                                    System.Threading.CancellationTokenSource cancellationTokenSource,
                                                    object[] associatedObjects)
        {
            return ExceptionEventArgs.InvokeEvent(sender, exception, cancellationTokenSource, associatedObjects, OnException);
        }

        public static bool InvokeProgressionEvent(DiagnosticFile sender,
                                                    ProgressionEventArgs.Categories category,
                                                    string stepName,
                                                    int? threadId = null,
                                                    uint? step = null,
                                                    uint? nbrSteps = null,
                                                    DateTime? timeStamp = null,
                                                    string messageString = null,
                                                    params object[] messageArgs)
        {
            return ProgressionEventArgs.InvokeEvent(sender, category, stepName, threadId, step, nbrSteps, timeStamp, messageString, messageArgs);
        }

        public static bool InvokeProgressionEvent(IEnumerable<DiagnosticFile> sender,
                                                    ProgressionEventArgs.Categories category,
                                                    string stepName,
                                                    int? threadId = null,
                                                    uint? step = null,
                                                    uint? nbrSteps = null,
                                                    DateTime? timeStamp = null,
                                                    string messageString = null,
                                                    params object[] messageArgs)
        {
            return ProgressionEventArgs.InvokeEvent(sender, category, stepName, threadId, step, nbrSteps, timeStamp, messageString, messageArgs);
        }

        public static bool InvokeProgressionEvent(FileMapper sender,
                                                    ProgressionEventArgs.Categories category,
                                                    string stepName,
                                                    int? threadId = null,
                                                    uint? step = null,
                                                    uint? nbrSteps = null,
                                                    DateTime? timeStamp = null,
                                                    string messageString = null,
                                                    params object[] messageArgs)
        {
            return ProgressionEventArgs.InvokeEvent(sender, category, stepName, threadId, step, nbrSteps, timeStamp, messageString, messageArgs);
        }

        public static bool InvokeProgressionEvent(IEnumerable<FileMapper> sender,
                                                    ProgressionEventArgs.Categories category,
                                                    string stepName,
                                                    int? threadId = null,
                                                    uint? step = null,
                                                    uint? nbrSteps = null,
                                                    DateTime? timeStamp = null,
                                                    string messageString = null,
                                                    params object[] messageArgs)
        {
            return ProgressionEventArgs.InvokeEvent(sender, category, stepName, threadId, step, nbrSteps, timeStamp, messageString, messageArgs);
        }

        public static bool InvokeProgressionEvent(IFilePath sender,
                                                    ProgressionEventArgs.Categories category,
                                                    string stepName,
                                                    int? threadId = null,
                                                    uint? step = null,
                                                    uint? nbrSteps = null,
                                                    DateTime? timeStamp = null,
                                                    string messageString = null,
                                                    params object[] messageArgs)
        {
            return ProgressionEventArgs.InvokeEvent(sender, category, stepName, threadId, step, nbrSteps, timeStamp, messageString, messageArgs);
        }

        public static event ProgressionEventArgs.EventHandler OnProgression
        {
            // Explicit event definition with accessor methods
            add
            {
                ProgressionEventArgs.OnProgression += value;
            }
            remove
            {
                ProgressionEventArgs.OnProgression -= value;
            }
        }

        #endregion //end of Events

        public static Dictionary<string, RegExParseString> RegExAssocations = LibrarySettings.DiagnosticFileRegExAssocations;
        private static bool _DisableParallelProcessing = false;
        public static bool DisableParallelProcessing
        {
            get { return _DisableParallelProcessing; }
            set
            {
                _DisableParallelProcessing = value;
                Logger.Instance.WarnFormat("DisableParallelProcessing: {0}", DisableParallelProcessing);
            }
        }

        protected DiagnosticFile(CatagoryTypes catagory,
									IDirectoryPath diagnosticDirectory,
									IFilePath file,
									INode node,
                                    string defaultClusterName,
                                    string defaultDCName,
                                    Version targetDSEVersion)
		{
			this.Catagory = catagory;
			this.DiagnosticDirectory = diagnosticDirectory;
			this.File = file;
			this.Node = node;
			this.ParsedTimeRange = new DateTimeRange();
			this.ParsedTimeRange.SetMinimal(DateTime.Now);
            this.DefaultClusterName = defaultClusterName;
            this.DefaultDataCenterName = defaultDCName;
            this.TargetDSEVersion = targetDSEVersion;

            this.RegExParser = RegExAssocations.TryGetValue(this.GetType().Name);

            Logger.Instance.DebugFormat("Loaded class \"{0}\"{{ Catagory{{{7}}}, File{{{1}}}, Node{{{2}}}, DefaultCluster{{{3}}}, DefaultDC{{{4}}}, TargetVersion{{{5}}}, RegExParser{{{6}}} }}",
                                            this.GetType().Name,
                                            this.File,
                                            this.Node,
                                            this.DefaultClusterName,
                                            this.DefaultDataCenterName,
                                            this.TargetDSEVersion,
                                            this.RegExParser,
                                            this.Catagory);
        }

        #region Public Members
        public CatagoryTypes Catagory { get; private set; }
		public IDirectoryPath DiagnosticDirectory { get; private set; }
		public IFilePath File { get; private set; }
		public INode Node { get; private set; }
        public string DefaultClusterName { get; private set; }
        public string DefaultDataCenterName { get; private set; }
        public DateTimeRange ParseOnlyInTimeRange { get; protected set; }
        public Version TargetDSEVersion { get; protected set; }
        public int NbrItemsParsed { get; protected set; }
		public DateTimeRange ParsedTimeRange { get; protected set; }
        /// <summary>
        /// True to indicate the process was sucessfully completed;
        /// </summary>
        public bool Processed { get; protected set; }
        public System.Exception Exception { get; protected set; }
        private int _nbrItemGenerated = 0;
        public int NbrItemGenerated { get { return this._nbrItemGenerated; } }
        public RegExParseString RegExParser { get; protected set; }
        public CancellationToken CancellationToken { get; set; }
        public bool Canceled { get; protected set; }
        public Task<DiagnosticFile> Task { get; protected set; }

        public IResult Result { get { return this.GetResult(); } }

        #endregion

		#region static

		/// <summary>
		///
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="nodeIdPos">
		/// The directory level of the node&apos;s IP Address or host name as the directory name. The directory above the file is position 1. It can be embedded and can be either an IPAddress or host name (seperated by a space or plus sign).
		/// If 0, the file name is searched where the node&apos;s Ip Address or host name. This can be embeded within the file name. If a host name is used it must be at the beginning of the file name and seperated by a space or plus sign.
		/// If -1, the file name is first reviewed, than from the file each directory level is searched up to root. During the directory search only IPAddresses are being looked for. They can be embedded.
		/// </param>
		/// <returns></returns>
		protected static NodeIdentifier DetermineNodeIdentifier(IFilePath filePath, int nodeIdPos)
		{
			NodeIdentifier nodeId = null;

			if(nodeIdPos <= 0)
			{
                nodeId = NodeIdentifier.CreateNodeIdentifer(filePath.FileNameWithoutExtension);
            }

            if (nodeId == null && nodeIdPos != 0)
			{
				bool scanning = nodeIdPos < 0;
				var directories = filePath.ParentDirectoryPath.PathResolved.Split(Path.DirectorySeparatorChar);

				if (scanning)
				{
					for (int dirLevel = directories.Length - 1; dirLevel >= 0; --dirLevel)
					{
						nodeId = NodeIdentifier.CreateNodeIdentifer(directories[dirLevel]);

						if(nodeId != null)
						{
							return nodeId;
						}
					}
				}
				else
				{
					nodeId = NodeIdentifier.CreateNodeIdentifer(directories[directories.Length - nodeIdPos]);
				}
			}

			return nodeId;
		}

        public static async Task<IEnumerable<DiagnosticFile>> ProcessFileWaitable(IDirectoryPath diagnosticDirectory,
                                                                                    string dataCenterName = null,
                                                                                    string clusterName = null,
                                                                                    Version dseVersion = null,
                                                                                    CancellationTokenSource cancellationSource = null)
        {
            cancellationSource?.Token.ThrowIfCancellationRequested();

            return await System.Threading.Tasks.Task.Factory.StartNew(() => DSEDiagnosticFileParser.DiagnosticFile.ProcessFile(diagnosticDirectory,
                                                                                                                                dataCenterName,
                                                                                                                                clusterName,
                                                                                                                                dseVersion,
                                                                                                                                cancellationSource)).Unwrap();
        }

        public static IEnumerable<DiagnosticFile> ProcessFile(IDirectoryPath diagnosticDirectory,
                                                                string dataCenterName,
                                                                string clusterName,
                                                                Version dseVersion,
                                                                short processPriorityLevel,
                                                                FileMapper.ProcessingTaskOptions parallelProcessingWithinPriorityLevel,
                                                                FileMapper fileMapper,
                                                                Common.Patterns.Collections.ThreadSafe.List<DiagnosticFile> diagFilesList,
                                                                ParallelLoopState loopState)
        {
            var localDiagFiles = new List<DiagnosticFile>();

            try
            {
                InvokeProgressionEvent(fileMapper,
                                        ProgressionEventArgs.Categories.Start | ProgressionEventArgs.Categories.Process | ProgressionEventArgs.Categories.FileMapper,
                                        "File Map",
                                        null,
                                        1,
                                        2,
                                        null,
                                        diagnosticDirectory.PathResolved);

                if(fileMapper.CancellationSource?.IsCancellationRequested ?? false)
                {
                    loopState?.Stop();
                    fileMapper.CancellationSource.Token.ThrowIfCancellationRequested();
                }

                if (loopState != null && loopState.IsStopped)
                {
                    if (fileMapper.CancellationSource == null) return localDiagFiles;
                    else fileMapper.CancellationSource.Cancel(true);
                }

                var diagnosticFiles = ProcessFile(diagnosticDirectory, fileMapper, dataCenterName, clusterName, dseVersion);

                diagFilesList.AddRange(diagnosticFiles);
                localDiagFiles.AddRange(diagnosticFiles);

                InvokeProgressionEvent(diagnosticFiles,
                                        ProgressionEventArgs.Categories.End | ProgressionEventArgs.Categories.Process | ProgressionEventArgs.Categories.DiagnosticFile,
                                        "File Map",
                                        null,
                                        2,
                                        2,
                                        null,
                                        diagnosticDirectory.PathResolved);

                fileMapper.CancellationSource?.Token.ThrowIfCancellationRequested();
            }
            catch (TaskCanceledException)
            {
                Logger.Instance.WarnFormat("{0}\t{1}\tProcessing was canceled for Mapping Level.",
                                            processPriorityLevel, parallelProcessingWithinPriorityLevel);

                InvokeProgressionEvent(fileMapper,
                                        ProgressionEventArgs.Categories.Cancel | ProgressionEventArgs.Categories.Process | ProgressionEventArgs.Categories.FileMapper,
                                        "File Map",
                                        null,
                                        2,
                                        2,
                                        null,
                                        diagnosticDirectory.PathResolved);
            }
            catch (AggregateException ae)
            {
                bool canceled = false;

                foreach (Exception e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                    {
                        if (!canceled)
                        {
                            canceled = true;
                            Logger.Instance.WarnFormat("{0}\t{1}\tProcessing was canceled for Mapping Level.",
                                                        processPriorityLevel, parallelProcessingWithinPriorityLevel);
                            InvokeProgressionEvent(fileMapper,
                                        ProgressionEventArgs.Categories.Cancel | ProgressionEventArgs.Categories.Process | ProgressionEventArgs.Categories.FileMapper,
                                        "File Map",
                                        null,
                                        2,
                                        2,
                                        null,
                                        diagnosticDirectory.PathResolved);
                        }
                        continue;
                    }
                    Logger.Instance.Error("Exception during FileMapping Processing", e);
                }

                if (!canceled)
                {
                    ExceptionEventArgs.InvokeEvent(fileMapper,
                                                    ae,
                                                    fileMapper.CancellationSource,
                                                    new object[] {  diagnosticDirectory,
                                                                        dataCenterName,
                                                                        clusterName,
                                                                        dseVersion },
                                                    OnException);
                }
            }
            catch (System.Exception ex)
            {
                Logger.Instance.Error("Exception during FileMapping Processing", ex);

                ExceptionEventArgs.InvokeEvent(fileMapper,
                                                   ex,
                                                   fileMapper.CancellationSource,
                                                   new object[] {  diagnosticDirectory,
                                                                        dataCenterName,
                                                                        clusterName,
                                                                        dseVersion },
                                                   OnException);
            }

            return localDiagFiles;
        }

        public static Task<IEnumerable<DiagnosticFile>> ProcessFile(IDirectoryPath diagnosticDirectory,
                                                                        string dataCenterName = null,
                                                                        string clusterName = null,
                                                                        Version dseVersion = null,
                                                                        CancellationTokenSource cancellationSource = null)
        {
            var diagFilesList = new Common.Patterns.Collections.ThreadSafe.List<DiagnosticFile>();
            var mappers = DSEDiagnosticFileParser.LibrarySettings.ProcessFileMappings
                            .Where(f => f.Enabled)
                            .OrderByDescending(o => o.ProcessPriorityLevel)
                            .ThenBy(o => DSEDiagnosticFileParser.FileMapper.DetermineParallelOptions(o.ProcessingTaskOption))
                            .GroupBy(k => new { ProcessPriorityLevel = k.ProcessPriorityLevel, ParallelProcessingWithinPriorityLevel = DSEDiagnosticFileParser.FileMapper.DetermineParallelOptions(k.ProcessingTaskOption) });
            int mapperId = 0;
            var parallelOptions = new ParallelOptions();

            //if(cancellationSource != null) parallelOptions.CancellationToken = cancellationSource.Token;
            parallelOptions.MaxDegreeOfParallelism = System.Environment.ProcessorCount;

            foreach (var mapperGroup in mappers)
            {
                var fileMappings = mapperGroup.Where(m => m.MatchVersion == null).ToList();

                if (dseVersion != null)
                {
                    var versionedMapper = mapperGroup.Where(m => m.MatchVersion != null && dseVersion >= m.MatchVersion).OrderByDescending(m => m.MatchVersion).FirstOrDefault();
                    fileMappings.Add(versionedMapper);
                }

                Logger.Instance.InfoFormat("FileMapper<{0}, {1}>",
                                                mapperId = fileMappings.GetHashCode(),
                                                JsonConvert.SerializeObject(fileMappings));

                InvokeProgressionEvent(fileMappings,
                                        ProgressionEventArgs.Categories.Start | ProgressionEventArgs.Categories.FileMapper | ProgressionEventArgs.Categories.Process | ProgressionEventArgs.Categories.Collection,
                                        "File Mapping Collection",
                                        null,
                                        1,
                                        2,
                                        null,
                                        diagnosticDirectory.PathResolved);

                if (DisableParallelProcessing || mapperGroup.Key.ParallelProcessingWithinPriorityLevel == FileMapper.ProcessingTaskOptions.None)
                {
                    Logger.Instance.InfoFormat("FileMapper<{0}, SyncMode>",
                                                mapperId);

                    foreach (var fileMapper in fileMappings)
                    {
                        if (fileMapper.CancellationSource == null && cancellationSource != null) fileMapper.CancellationSource = cancellationSource;

                        var diagnosticFiles = ProcessFile(diagnosticDirectory,
                                                            dataCenterName,
                                                            clusterName,
                                                            dseVersion,                                                            
                                                            mapperGroup.Key.ProcessPriorityLevel,
                                                            mapperGroup.Key.ParallelProcessingWithinPriorityLevel,
                                                            fileMapper,
                                                            diagFilesList,
                                                            null);

                        System.Threading.Tasks.Task.WaitAll(diagnosticFiles.Select(d => d.Task).ToArray());
                    }

                    Logger.Instance.InfoFormat("FileMapper<{0}, SyncMode, Completed>",
                                                mapperId);
                }
                else if (mapperGroup.Key.ParallelProcessingWithinPriorityLevel.HasFlag(FileMapper.ProcessingTaskOptions.ParallelProcessing))
                {
                    Logger.Instance.InfoFormat("FileMapper<{0}, ParallelProcessing>",
                                                mapperId);

                    Parallel.ForEach(fileMappings, parallelOptions, (fileMapper, loopState) =>
                    {
                       if (fileMapper.CancellationSource == null && cancellationSource != null) fileMapper.CancellationSource = cancellationSource;

                        ProcessFile(diagnosticDirectory,
                                    dataCenterName,
                                    clusterName,
                                    dseVersion,                                    
                                    mapperGroup.Key.ProcessPriorityLevel,
                                    mapperGroup.Key.ParallelProcessingWithinPriorityLevel,
                                    fileMapper,
                                    diagFilesList,
                                    loopState);
                    });

                    Logger.Instance.InfoFormat("FileMapper<{0}, ParallelProcessing, Completed>",
                                                mapperId);
                }
                else if (mapperGroup.Key.ParallelProcessingWithinPriorityLevel.HasFlag(FileMapper.ProcessingTaskOptions.ParallelProcessingWithinPriorityLevel))
                {
                    Logger.Instance.InfoFormat("FileMapper<{0}, ParallelProcessingWithinPriorityLevel>",
                                                mapperId);

                    var localDiagFilesList = new List<DiagnosticFile>();
                    Parallel.ForEach(fileMappings, parallelOptions, (fileMapper, loopState) =>
                    {
                        if (fileMapper.CancellationSource == null && cancellationSource != null) fileMapper.CancellationSource = cancellationSource;

                        localDiagFilesList.AddRange(ProcessFile(diagnosticDirectory,
                                                                    dataCenterName,
                                                                    clusterName,
                                                                    dseVersion,                                                                    
                                                                    mapperGroup.Key.ProcessPriorityLevel,
                                                                    mapperGroup.Key.ParallelProcessingWithinPriorityLevel,
                                                                    fileMapper,
                                                                    diagFilesList,
                                                                    loopState));
                    });

                    Logger.Instance.InfoFormat("FileMapper<{0}, ParallelProcessingWithinPriorityLevel, Waiting>",
                                                mapperId);

                    System.Threading.Tasks.Task.WaitAll(localDiagFilesList.Select(d => d.Task).ToArray());

                    Logger.Instance.InfoFormat("FileMapper<{0}, ParallelProcessingWithinPriorityLevel, Completed>",
                                                mapperId);
                }
                else
                {
                    Logger.Instance.ErrorFormat("{0}\t{1}\tError with Mapping's Parallel Processing Type. Unknown Value of \"{1}\".",
                                                    mapperGroup.Key.ProcessPriorityLevel, mapperGroup.Key.ParallelProcessingWithinPriorityLevel);
                }

                InvokeProgressionEvent(fileMappings,
                                        ProgressionEventArgs.Categories.End | ProgressionEventArgs.Categories.Process | ProgressionEventArgs.Categories.FileMapper | ProgressionEventArgs.Categories.Collection,
                                        "File Mapping Collection",
                                        null,
                                        2,
                                        2,
                                        null,
                                        diagnosticDirectory.PathResolved);
            }

            return diagFilesList.Count == 0
                        ? Common.Patterns.Tasks.CompletionExtensions.CompletedTask<IEnumerable<DiagnosticFile>>(Enumerable.Empty<DiagnosticFile>())
                        : Task<IEnumerable<DiagnosticFile>>.Factory.ContinueWhenAll(diagFilesList.Select(f => f.Task).ToArray(), ignoreItem => diagFilesList);
        }

        public static IEnumerable<DiagnosticFile> ProcessFile(IDirectoryPath diagnosticDirectory,
                                                                            FileMapper fileMappings,
                                                                            string dataCenterName = null,
                                                                            string clusterName = null,
                                                                            Version targetDSEVersion = null)
		{
            CancellationToken? cancellationToken = fileMappings.CancellationSource?.Token;

            if (cancellationToken.HasValue) cancellationToken.Value.ThrowIfCancellationRequested();

            InvokeProgressionEvent(fileMappings,
                                        ProgressionEventArgs.Categories.Start | ProgressionEventArgs.Categories.Process | ProgressionEventArgs.Categories.FileMapper | ProgressionEventArgs.Categories.Collection,
                                        "Determining Files from Mapper",
                                        null,
                                        1,
                                        2,
                                        null,
                                        diagnosticDirectory.PathResolved);

            var ignoreFilesRegEx = string.IsNullOrEmpty(fileMappings?.IgnoreFilesMatchingRegEx)
                                            ? null
                                            : new Regex(fileMappings.IgnoreFilesMatchingRegEx,
                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);
			var mergesFiles = fileMappings?.FilePathMerge(diagnosticDirectory);
            var resultingFiles = mergesFiles == null
                                ? Enumerable.Empty<IPath>()
                                : mergesFiles.SelectMany(f =>
                                    {
                                        try
                                        {
                                            if (cancellationToken.HasValue) cancellationToken.Value.ThrowIfCancellationRequested();
                                            return f.HasWildCardPattern()
                                                    ? f.GetWildCardMatches()
                                                    : new List<IPath>() { f };
                                        }
                                        catch (System.IO.DirectoryNotFoundException) { }
                                        catch (System.IO.FileNotFoundException) { }
                                        return Enumerable.Empty<IPath>();
                                    });
            var targetFiles = resultingFiles.Where(f => f is IFilePath
                                                            && f.Exist())
                                            .Cast<IFilePath>()
                                            .OrderByDescending(f => f.GetLastWriteTimeUtc())
                                            .DuplicatesRemoved(f => f) //If there are duplicates, keep the most current...
                                            .Where(f => ignoreFilesRegEx == null ? true : !ignoreFilesRegEx.IsMatch(f.PathResolved));
			var diagnosticInstances = new List<DiagnosticFile>();
			var instanceType = fileMappings.GetFileParsingType();

            InvokeProgressionEvent(fileMappings,
                                        ProgressionEventArgs.Categories.End | ProgressionEventArgs.Categories.Process | ProgressionEventArgs.Categories.FileMapper,
                                        "Determining Files from Mapper",
                                        null,
                                        2,
                                        2,
                                        null,
                                        "{0} collected {1} files",
                                        diagnosticDirectory.PathResolved,
                                        targetFiles.Count());

            if (instanceType == null)
            {
                Logger.Instance.ErrorFormat("<NoNodeId>\t<NoFile>\tFile Mapper File Parsing Class \"{0}\" was not found for Category {1}",
                                            fileMappings.FileParsingClass,
                                            fileMappings.Catagory);
                return Enumerable.Empty<DiagnosticFile>();
            }

            if (cancellationToken.HasValue) cancellationToken.Value.ThrowIfCancellationRequested();

            resultingFiles.Complement(targetFiles)
                            .ForEach(i => Logger.Instance.WarnFormat("<NoNodeId>\t{0}\tFile was skipped for processing because it either doesn't exist or is a file folder.",
                                                                        i.PathResolved));

            if(!string.IsNullOrEmpty(fileMappings.DefaultCluster))
            {
                clusterName = fileMappings.DefaultCluster;
            }
            if(!string.IsNullOrEmpty(fileMappings.DefaultDataCenter))
            {
                dataCenterName = fileMappings.DefaultDataCenter;
            }

            if (cancellationToken.HasValue) cancellationToken.Value.ThrowIfCancellationRequested();

            INode[] processTheseNodes = null;

            if(fileMappings.ProcessingTaskOption.HasFlag(FileMapper.ProcessingTaskOptions.AllNodesInCluster))
            {
                processTheseNodes = string.IsNullOrEmpty(clusterName) && Cluster.Clusters.Count() == 1
                                        ? Cluster.Clusters.First().Nodes.ToArray()                                            
                                        : Cluster.GetNodes(null, clusterName).ToArray();
            }
            else if (fileMappings.ProcessingTaskOption.HasFlag(FileMapper.ProcessingTaskOptions.AllNodesInDataCenter))
            {
                processTheseNodes = Cluster.GetNodes(dataCenterName, clusterName).ToArray();
            }

            if (processTheseNodes != null && processTheseNodes.Length == 0)
            {
                Logger.Instance.ErrorFormat("<NoNodeId>\t<NoFile>\tNode Processing Option {0} failed to retreive any node for Cluster \"{1}\", Data Center \"{2}\". File Mapper File Parsing Class \"{3}\" for Category {4} will NOT be processed!",
                                                fileMappings.ProcessingTaskOption,
                                                clusterName,
                                                dataCenterName,
                                                fileMappings.FileParsingClass,
                                                fileMappings.Catagory);
                return Enumerable.Empty<DiagnosticFile>();
            }

            DiagnosticFile resultInstance = null;
            List<DiagnosticFile> localDFList = new List<DiagnosticFile>();
            bool onlyOnceProcessing = fileMappings.ProcessingTaskOption.HasFlag(FileMapper.ProcessingTaskOptions.OnlyOnce);

            //targetFiles
            foreach (var targetFile in targetFiles)
			{
                localDFList.Clear();

                if (cancellationToken.HasValue) cancellationToken.Value.ThrowIfCancellationRequested();

                if (onlyOnceProcessing && resultInstance != null && resultInstance.Processed)
                {
                    break;
                }

                InvokeProgressionEvent(fileMappings,
                                        ProgressionEventArgs.Categories.Start | ProgressionEventArgs.Categories.Process | ProgressionEventArgs.Categories.FileMapper,
                                        "Processing File",
                                        null,
                                        1,
                                        2,
                                        null,
                                       targetFile.PathResolved);

                resultInstance = null;

                if (processTheseNodes == null)
                {
                    if (fileMappings.ProcessingTaskOption.HasFlag(FileMapper.ProcessingTaskOptions.ScanForNode))
                    {
                        var nodeId = DetermineNodeIdentifier(targetFile, fileMappings.NodeIdPos);

                        if (nodeId == null)
                        {
                            Logger.Instance.ErrorFormat("<NoNodeId>\t{0}\tCouldn't detect node identity (IPAdress or host name) for this file path. This file will be skipped.", targetFile.PathResolved);
                        }
                        else
                        {
                            resultInstance = ProcessFile(diagnosticDirectory,
                                                            targetFile,
                                                            instanceType,
                                                            fileMappings.Catagory,
                                                            nodeId,
                                                            null,
                                                            dataCenterName,
                                                            clusterName,
                                                            cancellationToken,
                                                            targetDSEVersion,
                                                            !DisableParallelProcessing && fileMappings.ProcessingTaskOption.HasFlag(FileMapper.ProcessingTaskOptions.ParallelProcessFiles));

                            if (resultInstance != null)
                            {
                                diagnosticInstances.Add(resultInstance);
                                localDFList.Add(resultInstance);
                            }
                        }
                    }
                    else
                    {
                        resultInstance = ProcessFile(diagnosticDirectory,
                                                        targetFile,
                                                        instanceType,
                                                        fileMappings.Catagory,
                                                        null,
                                                        null,
                                                        dataCenterName,
                                                        clusterName,
                                                        cancellationToken,
                                                        targetDSEVersion,
                                                        !DisableParallelProcessing && fileMappings.ProcessingTaskOption.HasFlag(FileMapper.ProcessingTaskOptions.ParallelProcessFiles));

                        if (resultInstance != null)
                        {
                            diagnosticInstances.Add(resultInstance);
                            localDFList.Add(resultInstance);
                        }
                    }
                }
                else
                {
                    foreach (var node in processTheseNodes)
                    {
                        resultInstance = ProcessFile(diagnosticDirectory,
                                                        targetFile,
                                                        instanceType,
                                                        fileMappings.Catagory,
                                                        null,
                                                        node,
                                                        dataCenterName,
                                                        clusterName,
                                                        cancellationToken,
                                                        targetDSEVersion,
                                                        !DisableParallelProcessing && fileMappings.ProcessingTaskOption.HasFlag(FileMapper.ProcessingTaskOptions.ParallelProcessFiles));

                        if (resultInstance != null)
                        {
                            diagnosticInstances.Add(resultInstance);
                            localDFList.Add(resultInstance);
                            if (onlyOnceProcessing && resultInstance.Processed)
                            {
                                InvokeProgressionEvent(localDFList,
                                                       ProgressionEventArgs.Categories.End | ProgressionEventArgs.Categories.Process | ProgressionEventArgs.Categories.DiagnosticFile | ProgressionEventArgs.Categories.Collection,
                                                       "Processing File",
                                                       null,
                                                       2,
                                                       2,
                                                       null,
                                                      targetFile.PathResolved);
                                break;
                            }
                        }

                        if (cancellationToken.HasValue) cancellationToken.Value.ThrowIfCancellationRequested();
                    }
                }

                InvokeProgressionEvent(localDFList,
                                        ProgressionEventArgs.Categories.End | ProgressionEventArgs.Categories.Process | ProgressionEventArgs.Categories.DiagnosticFile | ProgressionEventArgs.Categories.Collection,
                                        "Processing File",
                                        null,
                                        2,
                                        2,
                                        null,
                                       targetFile.PathResolved);
            }

            InvokeProgressionEvent(diagnosticInstances,
                                        ProgressionEventArgs.Categories.End | ProgressionEventArgs.Categories.Process | ProgressionEventArgs.Categories.Collection | ProgressionEventArgs.Categories.DiagnosticFile,
                                        "Determining Files from Mapper",
                                        null,
                                        2,
                                        2,
                                        null,
                                        diagnosticDirectory.PathResolved);

            return diagnosticInstances;
		}

		public static DiagnosticFile ProcessFile(IDirectoryPath diagnosticDirectory,
													IFilePath processFile,
													System.Type instanceType,
													CatagoryTypes catagory,
													NodeIdentifier nodeId,
                                                    INode useNode = null,
													string dataCenterName = null,
													string clusterName = null,
                                                    CancellationToken? cancellationToken = null,
                                                    Version targetDSEVersion = null,
                                                    bool runAsTask = false)
		{
			var node = useNode == null ? Cluster.TryGetAddNode(nodeId, dataCenterName, clusterName) : useNode;
			var processingFileInstance = (DiagnosticFile) Activator.CreateInstance(instanceType, catagory, diagnosticDirectory, processFile, node, clusterName, dataCenterName, targetDSEVersion);

            if(cancellationToken.HasValue)
            {
                processingFileInstance.CancellationToken = cancellationToken.Value;
            }

            var action = (Action)(() =>
            {
                try
                {
                    Logger.Instance.InfoFormat("{0}\t{1}\tBegin{2}Processing of File", node, processFile.PathResolved, runAsTask ? " (Async) " : " ");

                    processingFileInstance.CancellationToken.ThrowIfCancellationRequested();

                    InvokeProgressionEvent(processingFileInstance,
                                                ProgressionEventArgs.Categories.Start | ProgressionEventArgs.Categories.DiagnosticFile,
                                                "Process File",
                                                null,
                                                1,
                                                2,
                                                null,
                                                "Catagory<{0}>, InstanceType<{1}>, Node<{2}>",
                                                catagory, instanceType.Name, node);

                    var nbrItems = processingFileInstance.ProcessFile();

                    processingFileInstance.ParsedTimeRange.SetMaximum(DateTime.Now);
                    processingFileInstance._nbrItemGenerated = unchecked((int)nbrItems);

                    if (!processingFileInstance.Processed && nbrItems > 0) processingFileInstance.Processed = true;

                    if (nbrItems > 0)
                    {
                        Logger.Instance.InfoFormat("{0}\t{1}\tEnd of Processing of File that resulted in {2:###,##0} objects", node, processFile.PathResolved, nbrItems);
                    }
                    else
                    {
                        Logger.Instance.WarnFormat("{0}\t{1}\tEnd of Processing of File that resulted in {2:###,##0} objects", node, processFile.PathResolved, nbrItems);
                    }

                    InvokeProgressionEvent(processingFileInstance,
                                                ProgressionEventArgs.Categories.End | ProgressionEventArgs.Categories.DiagnosticFile,
                                                "Process File",
                                                null,
                                                2,
                                                2,
                                                null,
                                                "Catagory<{0}>, InstanceType<{1}>, Node<{2}>, Result<{3}>",
                                                catagory, instanceType.Name, node, nbrItems);
                }
                catch(TaskCanceledException)
                {
                    Logger.Instance.WarnFormat("{0}\t{1}\tProcessing was canceled.",
                                                node, processFile.PathResolved);
                    if(processingFileInstance != null)
                    {
                        processingFileInstance.Canceled = true;
                        processingFileInstance.ParsedTimeRange.SetMaximum(DateTime.Now);
                        InvokeProgressionEvent(processingFileInstance,
                                                ProgressionEventArgs.Categories.Cancel | ProgressionEventArgs.Categories.DiagnosticFile,
                                                "Process File",
                                                null,
                                                2,
                                                2,
                                                null,
                                                "Catagory<{0}>, InstanceType<{1}>, Node<{2}>, Result<{3}>",
                                                catagory, instanceType.Name, node, processingFileInstance.NbrItemsParsed);
                    }
                }
                catch (AggregateException ae)
                {
                    bool otherExceptions = false;
                    bool canceled = false;

                    foreach (Exception e in ae.InnerExceptions)
                    {
                        if (e is TaskCanceledException)
                        {
                            if (!canceled)
                            {
                                Logger.Instance.WarnFormat("{0}\t{1}\tProcessing was canceled.",
                                                            node, processFile.PathResolved);
                                if (processingFileInstance != null)
                                {
                                    processingFileInstance.ParsedTimeRange.SetMaximum(DateTime.Now);
                                    processingFileInstance.Canceled = true;
                                    InvokeProgressionEvent(processingFileInstance,
                                                            ProgressionEventArgs.Categories.Cancel | ProgressionEventArgs.Categories.DiagnosticFile,
                                                            "Process File",
                                                            null,
                                                            2,
                                                            2,
                                                            null,
                                                            "Catagory<{0}>, InstanceType<{1}>, Node<{2}>, Result<{3}>",
                                                            catagory, instanceType.Name, node, processingFileInstance.NbrItemsParsed);
                                }
                                canceled = true;
                            }
                            continue;
                        }
                        otherExceptions = true;
                    }

                    if(!canceled)
                    {
                        ExceptionEventArgs.InvokeEvent(processingFileInstance,
                                                           ae,
                                                           null,
                                                           new object[] { diagnosticDirectory,
                                                                                processFile,
                                                                                instanceType,
                                                                                catagory,
                                                                                nodeId,
                                                                                useNode,
                                                                                dataCenterName,
                                                                                clusterName,
                                                                                cancellationToken,
                                                                                targetDSEVersion,
                                                                                runAsTask },
                                                           OnException);
                    }

                    if(otherExceptions)
                    {
                        if (processingFileInstance == null)
                        {
                            throw;
                        }

                        processingFileInstance.Exception = ae;
                        processingFileInstance.ParsedTimeRange.SetMaximum(DateTime.Now);

                        Logger.Instance.Error(string.Format("{0}\t{1}\tProcessing failed with exception.",
                                                            node, processFile?.PathResolved), ae);
                    }
                }
                catch(System.Exception ex)
                {
                    ExceptionEventArgs.InvokeEvent(processingFileInstance,
                                                       ex,
                                                       null,
                                                       new object[] { diagnosticDirectory,
                                                                            processFile,
                                                                            instanceType,
                                                                            catagory,
                                                                            nodeId,
                                                                            useNode,
                                                                            dataCenterName,
                                                                            clusterName,
                                                                            cancellationToken,
                                                                            targetDSEVersion,
                                                                            runAsTask },
                                                       OnException);

                    if (processingFileInstance == null)
                    {
                        throw;
                    }

                    processingFileInstance.ParsedTimeRange.SetMaximum(DateTime.Now);
                    processingFileInstance.Exception = ex;
                    Logger.Instance.Error(string.Format("{0}\t{1}\tProcessing failed with exception.",
                                                            node, processFile?.PathResolved), ex);
                }
            });

            if (runAsTask)
            {
                processingFileInstance.Task = Task<DiagnosticFile>.Factory.StartNew(() => { action(); return processingFileInstance; },
                                                                                        processingFileInstance.CancellationToken);
                processingFileInstance.Task.ContinueWith(r =>
                                                            {
                                                                if(!r.Result.Canceled)
                                                                {
                                                                    r.Result.Canceled = r.IsCanceled;
                                                                    InvokeProgressionEvent(r.Result,
                                                                                            ProgressionEventArgs.Categories.Cancel | ProgressionEventArgs.Categories.DiagnosticFile,
                                                                                            "Process File",
                                                                                            null,
                                                                                            2,
                                                                                            2,
                                                                                            null,
                                                                                            "Catagory<{0}>, InstanceType<{1}>, Node<{2}>, Result<{3}>",
                                                                                            r.Result.Catagory, r.Result.GetType().Name, r.Result.Node, r.Result.NbrItemsParsed);
                                                                }
                                                            },
                                                            TaskContinuationOptions.AttachedToParent
                                                            | TaskContinuationOptions.ExecuteSynchronously
                                                            | TaskContinuationOptions.OnlyOnCanceled);
            }
            else
            {
                action();
                processingFileInstance.Task = Common.Patterns.Tasks.CompletionExtensions.CompletedTask(processingFileInstance);
            }

            return processingFileInstance;
		}

		#endregion

		#region abstracts

		public abstract IResult GetResult();

        /// <summary>
        ///
        ///
        /// Processed
        /// </summary>
        /// <returns>
        /// Returns the number of items created/generated. This number sets the NbrItemGenerated property and if greater than zero the Processed property is set to true.
        /// </returns>
        /// <remarks>
        /// The implication of this method should also set the NbrItemsParsed property to the items read/parsed/reviewed.
        /// </remarks>
		public abstract uint ProcessFile();

		#endregion
	}
}
