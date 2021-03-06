﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if NET40
using System.CodeDom.Compiler;
using Microsoft.CSharp;
#else
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Runtime.Loader;
#endif
using Common;
using Common.Patterns.Threading;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Net;
using DSEDiagnosticLogger;
using System.IO;

namespace DSEDiagnosticFileParser
{
    public static partial class MiscHelpers
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="extractedFolder"></param>
        /// <param name="forceExtraction">
        /// Only applies when extractToParentFolder is false.       
        /// </param>
        /// <param name="extractToParentFolder">
        /// If false (default), a sub-directory is created based on the compressed file&apos;s name and all uncompressed files will be placed into this directory. Note that if the directory already exists, the compressed file is NOT decompressed. To override this forceExtraction must be true.
        /// If true, all uncompressed files will be placed into this file&apos;s directory.
        /// </param>
        /// <param name="allowRecursiveUnZipping"></param>
        /// <param name="renameOnceFileExtracted">
        /// If true (default), an &quot;extracted&quot; file extension is appended onto the compressed file&apos; name.
        /// </param>
        /// <param name="cancellationToken"></param>
        /// <param name="runFileExtractionInParallel">
        /// If true and allowRecursiveUnZipping is true, extractToParentFolder will be always be false to eliminate a race condition and each unzipped file occurs on separate threads.
        /// If false (default) and allowRecursiveUnZipping is true, extractToParentFolder is always set to true and recursive unzipping is due on the entering thread. 
        /// </param>
        /// <returns></returns>
        public static int UnZipFileToFolder(this IFilePath filePath, out IDirectoryPath extractedFolder,
                                                bool forceExtraction = false,
                                                bool extractToParentFolder = false,
                                                bool allowRecursiveUnZipping = true,
                                                bool renameOnceFileExtracted = true,
                                                CancellationToken? cancellationToken = null,
                                                bool runNestedFileExtractionInParallel = false)
        {
            var extractFileInfo = LibrarySettings.ExtractFilesWithExtensions.FirstOrDefault(i => i.Item1.ToLower() == filePath.FileExtension.ToLower());
            int nbrExtractedFiles = 0;

            extractedFolder = extractToParentFolder
                                ? filePath.ParentDirectoryPath
                                : (IDirectoryPath) filePath.ParentDirectoryPath.MakeChild(extractFileInfo?.Item2 != "gz"
                                                                                            ? filePath.FileNameWithoutExtension
                                                                                            : (filePath.FileName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
                                                                                                ? filePath.FileName.Substring(0, filePath.FileName.Length - 7)
                                                                                                : filePath.FileNameWithoutExtension));

            if (extractFileInfo != null && filePath.Exist())
            {
                var fileAttrs = filePath.GetAttributes();

                if (forceExtraction || extractToParentFolder || !extractedFolder.Exist())
                {
                    var extractType = extractFileInfo.Item2.ToLower();
                    List<IPath> childrenInFolder;

                    cancellationToken?.ThrowIfCancellationRequested();

                    if (extractedFolder.Exist())
                    {
                        childrenInFolder = extractedFolder.Children();
                    }
                    else
                    {
                        extractedFolder.Create();
                        childrenInFolder = new List<IPath>(0);
                    }                   

                   if(!UnZipFileToFolder(extractType,
                                            filePath,
                                            extractedFolder,
                                            cancellationToken))
                    {
                        return 0;
                    }

                    cancellationToken?.ThrowIfCancellationRequested();

                    if (renameOnceFileExtracted)
                    {
                        var newName = filePath.Clone(filePath.FileName, "extracted");

                        if (filePath.Move(newName))
                        {
                            if (Logger.Instance.IsDebugEnabled)
                            {
                                Logger.Instance.DebugFormat("Renamed Extraction file from \"{0}\" to file \"{1}\"",
                                                            filePath.PathResolved,
                                                            newName.PathResolved);
                            }
                        }
                        else
                        {
                            Logger.Instance.ErrorFormat("Renaming Extraction file from \"{0}\" to file \"{1}\" Failed...",
                                                            filePath.PathResolved,
                                                            newName.PathResolved);
                        }
                    }

                    cancellationToken?.ThrowIfCancellationRequested();
                    nbrExtractedFiles = ((IDirectoryPath)extractedFolder).Children().Count - childrenInFolder.Count;

                    Logger.Instance.InfoFormat("Extracted into folder \"{0}\" {1} files...",
                                                    extractedFolder.PathResolved,
                                                    nbrExtractedFiles);

                    if (allowRecursiveUnZipping)
                    {
                        cancellationToken?.ThrowIfCancellationRequested();

                        var additionalExtractionFiles = GetAllChildrenExtractFiles((IDirectoryPath)extractedFolder)
                                                        .Where(f => !childrenInFolder.Contains(f));

                        RunParallelForEach.ForEach(runNestedFileExtractionInParallel, additionalExtractionFiles, compressedFile =>
                        //foreach (var compressedFile in compressedFiles)
                        {
                            cancellationToken?.ThrowIfCancellationRequested();

                            if (compressedFile.Exist())
                            {                                
                                var subNbrFiles = UnZipFileToFolder(compressedFile,
                                                                        out IDirectoryPath tempExtractedFolder,
                                                                        true,
                                                                        !runNestedFileExtractionInParallel,
                                                                        true,
                                                                        renameOnceFileExtracted,
                                                                        cancellationToken,
                                                                        false);

                                if (subNbrFiles == 0)
                                {
                                    Logger.Instance.ErrorFormat("Extraction of Sub-File \"{0}\" to directory \"{1}\" failed...",
                                                                    compressedFile.PathResolved,
                                                                    tempExtractedFolder.PathResolved);
                                }
                                else
                                {
                                    nbrExtractedFiles += subNbrFiles;
                                }
                            }
                        });
                    }
                }                
            }

            return nbrExtractedFiles;
        }

        public static bool UnZipFileToFolder(string extractType,
                                                IFilePath filePath,
                                                IDirectoryPath newExtractedFolder,
                                                CancellationToken? cancellationToken = null)
        {
            bool bResult = true;
            Logger.Instance.InfoFormat("Extracting File \"{0}\" to directory \"{1}\" as {2} type...",
                                                   filePath.PathResolved,
                                                   newExtractedFolder.PathResolved,
                                                   extractType);

            cancellationToken?.ThrowIfCancellationRequested();

            if (extractType == "zip")
            {
#region zip
                try
                {
                    var zip = new ICSharpCode.SharpZipLib.Zip.FastZip()
                    {
                        RestoreDateTimeOnExtract = true
                    };
                    zip.ExtractZip(filePath.PathResolved, newExtractedFolder.PathResolved, ICSharpCode.SharpZipLib.Zip.FastZip.Overwrite.RenameOnlyIfDifferent, null, null, null, true);
                }
                catch(System.Exception ex)
                {
                    Logger.Instance.Error(string.Format("Zip Exception for File \"{0}\".",
                                                                filePath.PathResolved),
                                                ex);
                    bResult = false;
                }
#endregion
            }
            else if (extractType == "gz" || extractType == "tar.gz" || extractType == "tgz")
            {
#region gz
                try
                {
                    using (var stream = filePath.OpenRead())
                    using (var gzipStream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(stream))
                    using (var tarArchive = ICSharpCode.SharpZipLib.Tar.TarArchive.CreateInputTarArchive(gzipStream))
                    {
                        tarArchive.FileOverwriteOption = ICSharpCode.SharpZipLib.Tar.TarArchive.FileOverWriteOptions.RenameOnlyIfDifferent;
                        tarArchive.RestoreDateTimeOnExtract = true;
                        tarArchive.ExtractContents(newExtractedFolder.PathResolved);

                    }
                }
                catch(ICSharpCode.SharpZipLib.Tar.InvalidHeaderException ex)
                {
                    Logger.Instance.Error(string.Format("GZ Invalid Header Exception for File \"{0}\". This indicates that some files were not decompressed or may be incomplete...",
                                                                filePath.PathResolved),
                                                ex);
                }
                catch (System.Exception ex)
                {
                    if ((ex is ICSharpCode.SharpZipLib.Tar.TarException
                                || ex is System.ArgumentOutOfRangeException
                                || ex is ICSharpCode.SharpZipLib.GZip.GZipException)
                            && (ex.Message == "Header checksum is invalid"
                                    || ex.Message == "Header CRC value mismatch"
                                    || ex.Message.StartsWith("Cannot be less than zero")
                                    || ex.Message.StartsWith("ModTime cannot be before Jan 1st 1970")))
                    {
                        Logger.Instance.InfoFormat("Invalid GZ header checksum detected or bad ModTime (\"{1}\") for File \"{0}\". Trying \"msgz\" format...",
                                                        filePath.PathResolved,
                                                        ex.Message);

                        bResult = UnZipFileToFolder("msgz",
                                                        filePath,
                                                        newExtractedFolder,
                                                        cancellationToken);
                    }
                    else
                    {
                        Logger.Instance.Error(string.Format("GZ Exception for File \"{0}\".",
                                                                filePath.PathResolved),
                                                ex);
                        bResult = false;
                    }
                }
#endregion
            }
            else if (extractType == "tar")
            {
#region tar
                try
                {
                    using (var stream = filePath.OpenRead())
                    using (var tarArchive = ICSharpCode.SharpZipLib.Tar.TarArchive.CreateInputTarArchive(stream))
                    {
                        tarArchive.FileOverwriteOption = ICSharpCode.SharpZipLib.Tar.TarArchive.FileOverWriteOptions.RenameOnlyIfDifferent;
                        tarArchive.RestoreDateTimeOnExtract = true;
                        tarArchive.ExtractContents(newExtractedFolder.PathResolved);
                    }
                }
                catch (ICSharpCode.SharpZipLib.Tar.InvalidHeaderException ex)
                {
                    Logger.Instance.Error(string.Format("Tar Invalid Header Exception for File \"{0}\". This indicates that some files were not decompressed or may be incomplete...",
                                                                filePath.PathResolved),
                                                ex);
                }
                catch (System.Exception ex)
                {
                    if ((ex is ICSharpCode.SharpZipLib.Tar.TarException
                                || ex is System.ArgumentOutOfRangeException)
                            && (ex.Message == "Header checksum is invalid"
                                    || ex.Message.StartsWith("Cannot be less than zero")
                                    || ex.Message.StartsWith("ModTime cannot be before Jan 1st 1970")))
                    {
                        Logger.Instance.InfoFormat("Invalid Tar header checksum detected or bad ModTime (\"{1}\") for File \"{0}\". Trying \"tar.gz\" format...",
                                                            filePath.PathResolved,
                                                            ex.Message);
                        bResult = UnZipFileToFolder("tar.gz",
                                                        filePath,
                                                        newExtractedFolder,
                                                        cancellationToken);
                    }
                    else
                    {
                        Logger.Instance.Error(string.Format("Tar Exception for File \"{0}\".",
                                                                filePath.PathResolved),
                                                ex);
                        bResult = false;
                    }
                }
#endregion
            }
            else if(extractType == "bzip2")
            {
#region bzip2
                
                var fileName = filePath.FileNameWithoutExtension;

                if(fileName.EndsWith("tar", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = System.IO.Path.GetFileNameWithoutExtension(fileName);
                }

                if (newExtractedFolder.MakeFile(fileName, out IFilePath newFile))
                {
                    var newFileExists = newFile.Exist();

                    try
                    {
                        ICSharpCode.SharpZipLib.BZip2.BZip2.Decompress(filePath.OpenRead(),
                                                                        newFile.Open(System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write),
                                                                        true);
                    }
                    catch(System.Exception ex)
                    {
                        if (!newFileExists
                                && newFile.Exist())
                        {
                            try
                            {
                                newFile.Delete();
                            }
                            catch { }
                        }

                        Logger.Instance.Error(string.Format("bzip2 Exception for File \"{0}\".",
                                                                filePath.PathResolved),
                                                ex);
                        bResult = false;
                    }

                }
#endregion
            }
            else if (extractType == "msgz")
            {
#region .net framework gz version
                
                var fileName = filePath.FileNameWithoutExtension;

                if (fileName.EndsWith("tar", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = System.IO.Path.GetFileNameWithoutExtension(fileName);
                }

                if (newExtractedFolder.MakeFile(fileName, out IFilePath newFile))
                {
                    var newFileExists = newFile.Exist();

                    try
                    {
                        using (var stream = filePath.OpenRead())
                        using (var decompressedFileStream = newFile.OpenWrite())
                        using (var decompressionStream = new System.IO.Compression.GZipStream(stream,
                                                                                                System.IO.Compression.CompressionMode.Decompress))
                        {
                            decompressionStream.CopyTo(decompressedFileStream);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        if (!newFileExists
                                && newFile.Exist())
                        {
                            try
                            {
                                newFile.Delete();
                            }
                            catch { }
                        }

                        Logger.Instance.Error(string.Format(".Net framework GZ Exception for File \"{0}\".",
                                                                filePath.PathResolved),
                                                ex);
                        bResult = false;
                    }
                }
#endregion
            }
            else
            {
                Logger.Instance.ErrorFormat("Unkown Extraction Type of \"{0}\" for Extracting File \"{1}\" to directory \"{2}\"",
                                                extractType,
                                                filePath.PathResolved,
                                                newExtractedFolder.PathResolved);
                bResult = false;
            }

            return bResult;
        }

        static IEnumerable<IFilePath> GetAllChildrenExtractFiles(IDirectoryPath directoryPath)
        {
            var childrenList = directoryPath.Children();
            var extractedFiles = childrenList.Where(f => f is IFilePath
                                                            && LibrarySettings.ExtractFilesWithExtensions.Any(i => i.Item1.ToLower() == ((IFilePath)f).FileExtension.ToLower()))
                                                .Cast<IFilePath>().ToList();

            foreach (var dirPath in childrenList.Where(p => p.IsDirectoryPath).Cast<IDirectoryPath>())
            {
                extractedFiles.AddRange(GetAllChildrenExtractFiles(dirPath));
            }

            return extractedFiles.DuplicatesRemoved(f => f.PathResolved);
        }

        public static V TryGetValue<K, V>(this Dictionary<K, V> collection, K key)
            where V : class
        {           
            if (collection != null && collection.TryGetValue(key, out V getValue))
            {
                return getValue;
            }

            return default(V);
        }

        public static T TryGetValue<K, T>(this Dictionary<K, Stack<T>> collection, K key)
            where T : class
        {            
            if (collection != null && collection.TryGetValue(key, out Stack<T> getValue))
            {
                return getValue.Count == 0 ? default(T) : getValue.Peek();
            }

            return default(T);
        }

        public static bool TryAddValue<K, V>(this Dictionary<K, V> collection, K key, V value)
        {
            if (collection != null && !collection.ContainsKey(key))
            {
                collection.Add(key, value);
                return true;
            }

            return false;
        }

        public static V SwapValue<K, V>(this Dictionary<K, V> collection, K key, V value, Action<K,V> oldValueAction)
        {           
            if (oldValueAction != null && collection.TryGetValue(key, out V getValue))
            {
                oldValueAction(key, getValue);
            }

            return collection[key] = value;
        }

        public static V SwapValue<K, V>(this Dictionary<K, V> collection, K key, V value)
        {           
            collection.TryGetValue(key, out V getValue);
            collection[key] = value;

            return getValue;
        }

        public static V TryAddUpdate<K, V>(this Dictionary<K, V> collection, K key, Func<K, V> addFunc, Func<K, V, V> updateFunc)
        {           
            if (collection.TryGetValue(key, out V getValue))
            {
                return collection[key] = updateFunc(key, getValue);
            }

            return collection[key] = addFunc(key);
        }

        public static Stack<T> TryAddAppendCollection<K, T>(this Dictionary<K, Stack<T>> collection, K key, T element)
        {
            Stack<T> collectionValue;

            if (collection.ContainsKey(key))
            {
                collectionValue = collection[key];
                collectionValue.Push(element);
                return collectionValue;
            }

            collection[key] = collectionValue = new Stack<T>();
            collectionValue.Push(element);

            return collectionValue;
        }

        public static Stack<T> TryAddOrUpdateCollection<K, T>(this Dictionary<K, Stack<T>> collection, K key, T element)
        {
            Stack<T> collectionValue;

            if (collection.ContainsKey(key))
            {
                collectionValue = collection[key];
                collectionValue.Clear();
                collectionValue.Push(element);
                return collectionValue;
            }

            collection[key] = collectionValue = new Stack<T>();
            collectionValue.Push(element);

            return collectionValue;
        }

        public static Stack<T> TryRemoveCollection<K, T>(this Dictionary<K, Stack<T>> collection, K key)
        {
            Stack<T> collectionValue;

            if (collection.ContainsKey(key))
            {
                collectionValue = collection[key];

                if(collectionValue.Count > 0)
                    collectionValue.Pop();

                return collectionValue;
            }

            return null;
        }

        private static Dictionary<int, Assembly> CompiledSources = new Dictionary<int, Assembly>();

        public static Assembly CompileSource(string name, string sourceCode, IEnumerable<Assembly> addedAssemblies = null)
        {
            sourceCode = sourceCode?.Trim();

            if(string.IsNullOrEmpty(sourceCode))
            {
                throw new ArgumentNullException("souceCode");
            }
           
            if(CompiledSources.TryGetValue(sourceCode.GetHashCode(), out Assembly compiledAssembly))
            {
                return compiledAssembly;
            }

#if NET40
            CodeDomProvider cpd = new CSharpCodeProvider();
            CompilerParameters cp = new CompilerParameters();

            cp.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            cp.ReferencedAssemblies.Add(typeof(DSEDiagnosticFileParser.MiscHelpers).Assembly.Location);
            cp.ReferencedAssemblies.Add(typeof(DSEDiagnosticLibrary.MiscHelpers).Assembly.Location);
            cp.ReferencedAssemblies.Add(typeof(DSEDiagnosticLogger.Logger).Assembly.Location);
            cp.ReferencedAssemblies.Add(typeof(DSEDiagnosticLog4NetParser.LogMessage).Assembly.Location);

            if (LibrarySettings.CodeDomAssemblies != null)
            {
                foreach (var assemblyName in LibrarySettings.CodeDomAssemblies)
                {
                    if (assemblyName.Length > 1 && assemblyName[0] == '-') continue;

                    var file = Common.ConfigHelper.Parse(assemblyName);

                    if (string.IsNullOrEmpty(file))
                    {
                        Logger.Instance.WarnFormat("Dynamic Compiling Assembly Loading Failure for \"{0}\"", assemblyName);
                        System.Diagnostics.Debug.WriteLine(string.Format("Dynamic Compiling Assembly Loading Failure for \"{0}\"", assemblyName), "Warning");
                    }
                    else
                    {
                        cp.ReferencedAssemblies.Add(file);
                    }
                }
            }

            if (addedAssemblies != null)
            {
                foreach (var assembly in addedAssemblies.DuplicatesRemoved(a => a.Location))
                {
                    cp.ReferencedAssemblies.Add(assembly.Location);
                }
            }

            cp.GenerateExecutable = false;
            cp.GenerateInMemory = true;
            cp.CompilerOptions = "/optimize";
            cp.IncludeDebugInformation = false;           

            // Invoke compilation.
            CompilerResults cr = cpd.CompileAssemblyFromSource(cp, sourceCode);

            if(cr.Errors.Count > 0)
            {
                Logger.Instance.ErrorFormat("Dynamic Compiling Error for body \"{0}\".", sourceCode);
                var errorStr = new StringBuilder();

                foreach (var error in cr.Errors)
                {
                    Logger.Instance.Error(error.ToString());
                    errorStr.Append('\t');
                    errorStr.AppendLine(error.ToString());
                }
                throw new ApplicationException(string.Format("Dynamic Compiling Error for {0}{1}{0}with Errors:{0}{2}",
                                                                Environment.NewLine,
                                                                sourceCode,
                                                                errorStr));
            }

            compiledAssembly = cr.CompiledAssembly;            
#else
            var dotnetCoreDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();

            var compilation = CSharpCompilation.Create(name)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location),
                    MetadataReference.CreateFromFile(typeof(DSEDiagnosticFileParser.MiscHelpers).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(DSEDiagnosticLibrary.MiscHelpers).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(DSEDiagnosticLogger.Logger).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(DSEDiagnosticLog4NetParser.LogMessage).Assembly.Location),
                    MetadataReference.CreateFromFile(System.IO.Path.Combine(dotnetCoreDirectory, "mscorlib.dll")),
                    MetadataReference.CreateFromFile(System.IO.Path.Combine(dotnetCoreDirectory, "netstandard.dll")),
                    MetadataReference.CreateFromFile(System.IO.Path.Combine(dotnetCoreDirectory, "System.Runtime.dll")));

            if (LibrarySettings.CodeDomAssemblies != null)
            {
                foreach (var assemblyName in LibrarySettings.CodeDomAssemblies)
                {
                    if (assemblyName.Length > 1 && assemblyName[0] == '-') continue;

                    var file = Common.ConfigHelper.Parse(assemblyName);

                    if (string.IsNullOrEmpty(file))
                    {
                        Logger.Instance.WarnFormat("Dynamic Compiling Assembly Loading Failure for \"{0}\"", assemblyName);
                        System.Diagnostics.Debug.WriteLine(string.Format("Dynamic Compiling Assembly Loading Failure for \"{0}\"", assemblyName), "Warning");
                    }
                    else
                    {
                        compilation.AddReferences(MetadataReference.CreateFromFile(file));
                    }
                }
            }

            if (addedAssemblies != null)
            {
                foreach (var assembly in addedAssemblies.DuplicatesRemoved(a => a.Location))
                {
                    compilation.AddReferences(MetadataReference.CreateFromFile(assembly.Location));
                }
            }

            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(sourceCode));

            var diagMsgs = compilation.GetDiagnostics();

            if (diagMsgs.HasAtLeastOneElement())
            {
                var hasErrors = diagMsgs.Any(m => m.Severity == DiagnosticSeverity.Error);

                if (hasErrors)
                {
                    Logger.Instance.ErrorFormat("Dynamic Compiling Error for body \"{0}\".", sourceCode);
                    var errorStr = new StringBuilder();

                    foreach (var error in diagMsgs)
                    {
                        Logger.Instance.Error(error.ToString());

                        errorStr.Append('\t');
                        errorStr.AppendLine(error.ToString());
                    }

                    throw new ApplicationException(string.Format("Dynamic Compiling Error for {0}{1}{0}with Errors:{0}{2}",
                                                                    Environment.NewLine,
                                                                    sourceCode,
                                                                    errorStr));
                }
#if DEBUG
                Logger.Instance.WarnFormat("Dynamic Compiling Warnings for body \"{0}\".", sourceCode);
                foreach (var error in diagMsgs)
                {
                    Logger.Instance.Warn(error.ToString());                    
                }
#endif
            }

            using (var memoryStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(memoryStream);
                if (emitResult.Success)
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    var context = AssemblyLoadContext.Default;
                    var assembly = context.LoadFromStream(memoryStream);

                    compiledAssembly = assembly;
                }
            }
#endif
            CompiledSources.Add(sourceCode.GetHashCode(), compiledAssembly);
            return compiledAssembly;
        }

        public static MethodInfo CompileMethod(string assemblyName, string methodName, Type returnType, IEnumerable<Tuple<Type, string>> arguments, string body, string defaultStaticClassName = null)
        {
            var className = string.IsNullOrEmpty(defaultStaticClassName) ? methodName + "_class" : defaultStaticClassName;
            methodName = methodName.Trim();

            var methodBody = string.Format(Properties.Settings.Default.CodeDomClassTemplate,
                                            className,
                                            returnType == null ? "void" : Common.ReflectionInfoHelper.MakeNormaizedClassName(returnType.FullName),
                                            methodName,
                                            arguments == null || arguments.Count() == 0
                                                ? string.Empty
                                                : string.Join(",", arguments.Select(a => string.Format("{0} {1}",
                                                                                        Common.ReflectionInfoHelper.MakeNormaizedClassName(a.Item1.FullName),
                                                                                        a.Item2.Trim()))),
                                            body);

            List<Assembly> typeAssemblies = new List<Assembly>();

            if(arguments != null)
            {
                typeAssemblies.AddRange(arguments.Select(a => a.Item1.Assembly));
            }
            if(returnType != null)
            {
                typeAssemblies.Add(returnType.Assembly);
            }

            var ca = CompileSource(assemblyName+className, methodBody, typeAssemblies);
            var t = ca.GetType(className);

            return t.GetMethod(methodName, arguments?.Select(a => a.Item1).ToArray() ?? new Type[0]);
        }

        public static IEnumerable<string> ToExceptionString(this Exception ex, string prefix = null)
        {
            if (ex == null) return Enumerable.Empty<string>();

            IEnumerable<string> innerExceptions = Enumerable.Empty<string>();
            IEnumerable<string> aggregateExceptions = Enumerable.Empty<string>();

            if (ex.InnerException != null)
            {
                innerExceptions = ToExceptionString(ex, ex.GetType().Name +"(inner)");
            }

            if(ex is AggregateException)
            {
                aggregateExceptions = ((AggregateException)ex).InnerExceptions.SelectMany(ae => ToExceptionString(ae, "AggregateException"));
            }

            var stringList = new List<string>();

            stringList.Add((string.IsNullOrEmpty(prefix) ? string.Empty : prefix + ':') + ex.GetType().Name + '{' + ex.Message + ',' + ex.Source + ',' + ex.StackTrace);
            stringList.AddRange(innerExceptions);
            stringList.AddRange(aggregateExceptions);
            return stringList;
        }

        public static bool SetNodeHostName(DSEDiagnosticLibrary.INode node, string hostName, bool tryParseAsIPAdress = false)
        {
            var setHostName = node.DataCenter?.SetNodeHostName(node, hostName, tryParseAsIPAdress);

            if (setHostName == null)
            {
                node.Id.SetIPAddressOrHostName(hostName, tryParseAsIPAdress);               
            }
            else
            {
                if (setHostName.Item1)
                {
                    if (Logger.Instance.IsDebugEnabled)
                    {
                        Logger.Instance.DebugFormat("Added Host Name \"{0}\" to node \"{1}\"", hostName, node);
                    }                    
                }
                else
                {
                    Logger.Instance.WarnFormat("Node \"{0}\" with host name \"{1}\" was found to be duplicated (exists for node \"{2}\"). Host name not set.",
                                                node, hostName, setHostName.Item2);
                }
            }

            return setHostName != null && setHostName.Item1;
        }
        
#region JSON

        public static Dictionary<string, JObject> TryGetValues(this JObject jsonObj)
        {
            return jsonObj?.ToObject<Dictionary<string, JObject>>();
        }

        public static IEnumerable<T> TryGetValues<T>(this JToken jtoken, string key)
        {
            return jtoken?.SelectToken(key).ToObject<IEnumerable<T>>();
        }

        public static JToken TryGetValue(this JObject jsonObj, string key)
        {
            return jsonObj?.GetValue(key);
        }

        public static T TryGetValue<T>(this JObject jsonObj, string key)
        {
            if (jsonObj != null)
            {
                return jsonObj.Value<T>(key);
            }

            return default(T);
        }

        public static T TryGetValue<T>(this JToken jtoken, string key)
        {
            if (jtoken != null)
            {
                return jtoken.Value<T>(key);
            }

            return default(T);
        }

        public static T TryGetValue<T>(this JToken jtoken, string key, ref T updateField)
        {
            if (jtoken != null)
            {
                return updateField = jtoken.Value<T>(key);
            }

            return default(T);
        }

        public static T TryGetValue<T>(this JToken jtoken)
        {
            if (jtoken != null)
            {
                return jtoken.ToObject<T>();
            }

            return default(T);
        }

        public static JToken TryGetValue(this JToken jtoken, string key)
        {
            if (jtoken != null)
            {
                return jtoken.SelectToken(key);
            }

            return null;
        }

        public static T TryGetValue<T>(this JToken jtoken, int index)
        {
            if (jtoken != null)
            {
                return jtoken.Value<T>(index);
            }

            return default(T);
        }

        /// <summary>
        /// sets outExpr if jtoken value is not null
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="jtoken"></param>
        /// <param name="input"></param>
        /// <param name="outObj"></param>
        /// <param name="outExpr"></param>
        /// <returns>True for successful</returns>
        /// <example>
        /// <code>
        /// jsonObj.TryGetValue("10.14.150.121").TryGetValue("devices").TryGetValue("other").NullSafeSet(myInstance, i => i.field);
        /// </code>
        /// </example>
        public static bool NullSafeSet<TClass, TProperty>(this JToken jtoken, TClass outObj, Expression<Func<TClass, TProperty>> outExpr)
        {
            if (jtoken != null)
            {
                var expr = (MemberExpression)outExpr.Body;
                var prop = (PropertyInfo)expr.Member;

                var jsonValue = jtoken.ToObject(prop.PropertyType);

                if (jsonValue != null)
                {
                    prop.SetValue(outObj, jsonValue, null);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// sets outExpr if jtoken value is not null
        /// </summary>
        /// <typeparam name="TClass"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="jtoken"></param>
        /// <param name="index"></param>
        /// <param name="outObj"></param>
        /// <param name="outExpr"></param>
        /// <returns></returns>
        public static bool NullSafeSet<TClass, TProperty>(this JToken jtoken, int index, TClass outObj, Expression<Func<TClass, TProperty>> outExpr)
        {
            if (jtoken != null)
            {
                var expr = (MemberExpression)outExpr.Body;
                var prop = (PropertyInfo)expr.Member;

                var jsonValue = jtoken.ElementAtOrDefault(index)?.ToObject(prop.PropertyType);

                if (jsonValue != null)
                {
                    prop.SetValue(outObj, jsonValue, null);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// calls setOutput if jtoken value is not null
        /// </summary>
        /// <typeparam name="JValue"></typeparam>
        /// <param name="jtoken"></param>
        /// <param name="index"></param>
        /// <param name="setOutput"></param>
        /// <returns></returns>
        public static bool NullSafeSet<JValue>(this JToken jtoken, int index, Action<JValue> setOutput)
        {
            if (jtoken != null)
            {
                var jsonValue = jtoken.ElementAtOrDefault(index)?.ToObject(typeof(JValue));

                if (jsonValue != null)
                {
                    setOutput((JValue)jsonValue);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// calls setOutput if jtoken value is not null or calls onNullValue if jtoken value is null.
        /// </summary>
        /// <typeparam name="JValue"></typeparam>
        /// <param name="jtoken"></param>
        /// <param name="index"></param>
        /// <param name="setOutput"></param>
        /// <param name="onNullValue"></param>
        /// <returns></returns>
        public static bool NullSafeSet<JValue>(this JToken jtoken, int index, Action<JValue> setOutput, Action onNullValue)
            where JValue : struct
        {
            if (jtoken != null)
            {
                var jsonValue = jtoken.ElementAtOrDefault(index)?.ToObject(typeof(Nullable<JValue>));

                if (jsonValue != null && ((JValue?)jsonValue).HasValue)
                {
                    setOutput((JValue)jsonValue);
                    return true;
                }
                else
                {
                    onNullValue?.Invoke();
                }
            }

            return false;
        }

        /// <summary>
        /// calls setOutput if jtoken value is not null
        /// </summary>
        /// <typeparam name="JValue"></typeparam>
        /// <param name="jtoken"></param>
        /// <param name="setOutput"></param>
        /// <returns></returns>
        public static bool NullSafeSet<JValue>(this JToken jtoken, Action<JValue> setOutput)            
        {
            if (jtoken != null)
            {
                var jsonValue = jtoken.ToObject(typeof(JValue));

                if (jsonValue != null)
                {
                    setOutput((JValue)jsonValue);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// calls setOutput if jtoken value is not null or call onNullValue id jtoken value is null
        /// </summary>
        /// <typeparam name="JValue"></typeparam>
        /// <param name="jtoken"></param>
        /// <param name="setOutput"></param>
        /// <param name="onNullValue"></param>
        /// <returns></returns>
        public static bool NullSafeSet<JValue>(this JToken jtoken, Action<JValue> setOutput, Action onNullValue)
            where JValue : struct
        {
            if (jtoken != null)
            {
                var jsonValue = jtoken.ToObject(typeof(Nullable<JValue>));

                if (jsonValue != null && ((JValue?)jsonValue).HasValue)
                {
                    setOutput((JValue)jsonValue);
                    return true;
                }
                else
                {
                    onNullValue?.Invoke();
                }
            }

            return false;
        }

        /// <summary>
        /// Only calls setOutput if inputValue is null and jToken value is not null.
        /// </summary>
        /// <typeparam name="JValue"></typeparam>
        /// <param name="jtoken"></param>
        /// <param name="nIndex"></param>
        /// <param name="inputValue"></param>
        /// <param name="setOutput"></param>
        /// <returns></returns>
        public static bool EmptySafeSet<JValue>(this JToken jtoken, int nIndex, JValue inputValue, Action<JValue> setOutput)
            where JValue : class
        {
            if (jtoken != null)
            {
                if (inputValue == null)
                {
                    return jtoken.NullSafeSet<JValue>(nIndex, setOutput);
                }
            }

            return false;
        }

        /// <summary>
        /// Only calls setOutput if inputValue is null or string.empty and jToken value is not null.
        /// </summary>
        /// <param name="jtoken"></param>
        /// <param name="nIndex"></param>
        /// <param name="inputValue"></param>
        /// <param name="setOutput"></param>
        /// <returns></returns>
        public static bool EmptySafeSet(this JToken jtoken, int nIndex, string inputValue, Action<string> setOutput)
        {
            if (jtoken != null)
            {
                if (string.IsNullOrEmpty(inputValue))
                {
                    return jtoken.NullSafeSet<string>(nIndex, setOutput);
                }
            }

            return false;
        }

        /// <summary>
        /// Only calls setOutput if inputValue does not have a value and jToken value is not null.
        /// </summary>
        /// <typeparam name="JValue"></typeparam>
        /// <param name="jtoken"></param>
        /// <param name="nIndex"></param>
        /// <param name="inputValue"></param>
        /// <param name="setOutput"></param>
        /// <returns></returns>
        public static bool EmptySafeSet<JValue>(this JToken jtoken, int nIndex, Nullable<JValue> inputValue, Action<JValue> setOutput)
            where JValue : struct
        {
            if (jtoken != null)
            {
                if (!inputValue.HasValue)
                {
                    return jtoken.NullSafeSet<JValue>(nIndex, setOutput, null);
                }
            }

            return false;
        }


        /// <summary>
        /// Only calls setOutput if inputValue is null and jToken value is not null.
        /// </summary>
        /// <typeparam name="JValue"></typeparam>
        /// <param name="jtoken"></param>
        /// <param name="inputValue"></param>
        /// <param name="setOutput"></param>
        /// <returns></returns>
        public static bool EmptySafeSet<JValue>(this JToken jtoken, JValue inputValue, Action<JValue> setOutput)
            where JValue : class
        {
            if (jtoken != null)
            {
                if(inputValue == null)
                {
                    return jtoken.NullSafeSet<JValue>(setOutput);
                }
            }

            return false;
        }

        /// <summary>
        /// Only calls setOutput if inputValue is null or string.empty and jToken value is not null.
        /// </summary>
        /// <param name="jtoken"></param>
        /// <param name="inputValue"></param>
        /// <param name="setOutput"></param>
        /// <returns></returns>
        public static bool EmptySafeSet(this JToken jtoken, string inputValue, Action<string> setOutput)
        {
            if (jtoken != null)
            {
                if (string.IsNullOrEmpty(inputValue))
                {
                    return jtoken.NullSafeSet<string>(setOutput);
                }
            }

            return false;
        }

        /// <summary>
        /// Only calls setOutput if inputValue does not have a value and jToken value is not null.
        /// </summary>
        /// <typeparam name="JValue"></typeparam>
        /// <param name="jtoken"></param>
        /// <param name="inputValue"></param>
        /// <param name="setOutput"></param>
        /// <returns></returns>
        public static bool EmptySafeSet<JValue>(this JToken jtoken, Nullable<JValue> inputValue, Action<JValue> setOutput)
            where JValue : struct
        {
            if (jtoken != null)
            {
                if(!inputValue.HasValue)
                {
                    return jtoken.NullSafeSet<JValue>(setOutput, null);
                }
            }

            return false;
        }
#endregion

    }
}
