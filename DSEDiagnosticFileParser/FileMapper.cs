﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Path;
using System.Threading;

namespace DSEDiagnosticFileParser
{
	public sealed class FileMapper
	{
        [Flags]
        public enum ProcessingTaskOptions
        {
            /// <summary>
            /// Determine Node by scanning the File Path
            /// </summary>
            ScanForNode = 0x0001,
            /// <summary>
            /// Process this file for using all nodes defined within a data center
            /// </summary>
            AllNodesInDataCenter = 0x0002,
            /// <summary>
            /// Process this file for using all nodes defined within a cluster
            /// </summary>
            AllNodesInCluster = 0x0004,
            /// <summary>
            /// If defined, the mapping class will be required to determine the proper node
            /// </summary>
            IgnoreNode = 0x0008,
            /// <summary>
            /// If defined, this file processing task will be ran in parallel with other files processing tasks within this ProcessPriority level.
            /// If this is not defined, all non-parallel processing tasks will be executed sequentially within this level and than all parallel defined processing tasks will be executed in parallel.
            /// </summary>
            /// <remarks>
            /// Lower process priorities will be block until all file processing within this level completes unless AllowLowerPriorityProcessingToStart is defined.
            /// </remarks>
            ParallelProcessingWithinPriorityLevel = 0x0010,
            /// <summary>
            /// If defined and this task is still executing and no other tasks at this level are running without this defined, lower tasks will be able to start.
            /// If not defined, lower tasks will be blocked until this task completes.
            /// </summary>
            AllowLowerPriorityProcessingToStart = 0x0020,
            /// <summary>
            /// If defined all files found that match FilePatterns are processed in parallel.
            /// Note this option is ignored if OnlyOnce is defined.
            /// </summary>
            ParallelProcessFiles = 0x0040,
            /// <summary>
            /// As soon as the first file is successfully processed, all reminding file to be process are canceled.  
            /// Warning: ParallelProcessFiles is ignored (all files are processed synchronous. 
            /// </summary>
            OnlyOnce = 0x0080,

            Default = ScanForNode | ParallelProcessingWithinPriorityLevel | ParallelProcessFiles
        }

        public FileMapper()
        {
            this.NodeIdPos = -1;
            this.ProcessingTaskOption = ProcessingTaskOptions.Default;
            this.MatchVersion = null;
        }

        public FileMapper(DiagnosticFile.CatagoryTypes catagory,
                            string fileParsingClass,
                            int? nodeIdPos,
                            ProcessingTaskOptions? processingOptions,
                            params string [] filePatterns)
        {
            this.NodeIdPos = nodeIdPos.HasValue ? nodeIdPos.Value : -1;
            this.Catagory = catagory;
            this.FileParsingClass = fileParsingClass;
            this.ProcessingTaskOption = processingOptions.HasValue ? processingOptions.Value : ProcessingTaskOptions.Default;
            this.FilePatterns = filePatterns;
        }

        public FileMapper(DiagnosticFile.CatagoryTypes catagory,
                            string fileParsingClass,
                            int? nodeIdPos,
                            ProcessingTaskOptions? processingOptions,
                            string version,
                            params string[] filePatterns)
            : this(catagory, fileParsingClass, nodeIdPos, processingOptions, filePatterns)
        {
           this.MatchVersion = new Version(version);
        }

        public DiagnosticFile.CatagoryTypes Catagory { get; set; }

		public string[] FilePatterns { get; set; }
		public string FileParsingClass { get; set; }
		/// <summary>
		/// The directory level of the node&apos;s IP Address or host name as the directory name. The directory above the file is position 1. It can be embedded and can be either an IPAddress or host name (seperated by a space or plus sign).
		/// If 0, the file name is searched where the node&apos;s Ip Address or host name. This can be embeded within the file name. If a host name is used it must be at the beginning of the file name and seperated by a space or plus sign.
		/// If -1, the file name is first reviewed, than from the file each directory level is searched up to root. During the directory search only IPAddresses are being looked for. They can be embedded.
		/// </summary>
		public int NodeIdPos { get; set; }
        public ProcessingTaskOptions ProcessingTaskOption { get; set; }

        /// <summary>
        /// The processing task level. Higher numbers (levels) means this task will be processed sooner than lower numbers (levels).
        /// </summary>
        public short ProcessPriorityLevel { get; set; }

        public string DefaultDataCenter { get; set; }
        public string DefaultCluster { get; set;}

        /// <summary>
        /// If null any DSE version will match this mappings.
        /// If defined, any DSE version that is equal to or greater than this version will match this mapping.
        /// </summary>
        public Version MatchVersion { get; set; }
        public IFilePath[] FilePathMerge(IDirectoryPath diagnosticDirectory)
		{
			if(this.FilePatterns == null)
			{
				return null;
			}

			return FilePatterns.Select(p => PathUtils.Parse(p, diagnosticDirectory.Path)).Cast<IFilePath>().ToArray();
		}

		public Type GetFileParsingType()
		{
			return string.IsNullOrEmpty(this.FileParsingClass) ? null : TypeHelpers.GetDataType(this.FileParsingClass);
		}

        public CancellationTokenSource CancellationSource { get; set; }
        
    }
}