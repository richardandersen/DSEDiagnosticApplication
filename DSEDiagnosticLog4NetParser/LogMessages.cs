﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;
using Common;
using DSEDiagnosticLibrary;
using DSEDiagnosticLogger;
using System.Runtime.CompilerServices;

namespace DSEDiagnosticLog4NetParser
{
    public sealed class LogMessages : ILogMessages
    {
        private readonly static string[] LogLevels = new string[] { "DEBUG ", "INFO ", "WARN ", "ERROR ", "FATAL ", "TRACE." };

        public LogMessages(IFilePath logFile, string log4netConversionPattern, INode node = null)
        {
            if (string.IsNullOrEmpty(log4netConversionPattern)) throw new ArgumentNullException("log4netConversionPattern");
            
            this.LogFile = logFile ?? throw new ArgumentNullException("logFile");
            this.Log4NetConversionPattern = log4netConversionPattern;

            var log4netImporter = Log4NetPatternImporter.GenerateRegularGrammarElement(this.Log4NetConversionPattern);

            this.Log4NetConversionPatternRegExLine = CreateRegEx("LineRegEx", log4netImporter);
            this.DetermineLogDateTime(log4netImporter);
            this.Node = node;
            this._timeZoneInstance = this.Node?.Machine.TimeZone;
            this.IANATimeZoneName = this._timeZoneInstance?.Name;
        }

        public LogMessages(IFilePath logFile, string log4netConversionPattern, string ianaTimeZoneName)
            :this(logFile, log4netConversionPattern)
        {
            this._timeZoneInstance = StringHelpers.FindTimeZone(ianaTimeZoneName);

            if(this._timeZoneInstance == null)
            {
                Logger.Instance.WarnFormat("{0}\t{1}\tCould not fine IANA Timezone \"{2}\". Log4Net LogDateTimewTZOffset property will use a timezone of UTC.",
                                            this.Node,
                                            logFile,
                                            ianaTimeZoneName);
            }
            else
            {
                this.IANATimeZoneName = this._timeZoneInstance.Name;
            }
        }

        public LogMessages(LogMessages logMessages)
        {
            this.LogFile = logMessages.LogFile;
            this.Log4NetConversionPattern = logMessages.Log4NetConversionPattern;
            this.Log4NetConversionPatternRegExLine = logMessages.Log4NetConversionPatternRegExLine;
            this._logTimestampValue = logMessages._logTimestampValue;
            this._datetimeFormat = logMessages._datetimeFormat;
            this.Node = logMessages.Node;
            this._timeZoneInstance = logMessages._timeZoneInstance;
            this.IANATimeZoneName = logMessages.IANATimeZoneName;
        }

        public INode Node { get; }
        public string IANATimeZoneName { get; }
        public string Log4NetConversionPattern { get; }
        public Regex Log4NetConversionPatternRegExLine { get; }

        public IFilePath LogFile { get; }

        private List<LogMessage> _logMessages = new List<LogMessage>();

        public IEnumerable<ILogMessage> Messages { get { return this._logMessages; } }

        public DateTimeOffsetRange LogTimeRange
        {
            get
            {
                return this._startingLogDateTime.IsMinOrMaxValue()
                            ? DateTimeOffsetRange.EmptyMaxMin
                            : new DateTimeOffsetRange(this._startingLogDateTime, this._endingLogDateTime);
            }
        }

        public DateTimeOffsetRange LogFileTimeRange { get; internal set; }

        public LogCompletionStatus CompletionStatus { get; internal set; }

        private List<string> _errors = new List<string>();
        public IEnumerable<string> Errors { get { return this._errors; } }

        public int NbrInvalidLines { get; private set; }
      
        public LogMessage AddMessage(string logLine, uint logLinePos, bool ignoreErros = false)
        {
            logLine = logLine?.Trim();

            if (string.IsNullOrEmpty(logLine))
            {
                return null;
            }

            LogMessage logMessage = null;

            try
            {
                var lineMatch = this.Log4NetConversionPatternRegExLine.Match(logLine);

                if (lineMatch.Success)
                {
                    logMessage = new LogMessage(logLinePos, logLine.GetHashCode());
                }
                else if (this._lastMessage != null)
                {
                    if (LogLevels.Any(l => logLine.StartsWith(l, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        if (!ignoreErros)
                        {                            
                            if (logLinePos != 0 || this.NbrInvalidLines == 0)
                            {
                                var error = string.Format("{0}\t{1}\tInvalid line detected in Log4Net Log file at line number {3}. Line ignored. Line is: {2}",
                                                        this.Node,
                                                        this.LogFile.PathResolved,
                                                        logLine,
                                                        logLinePos);
                                Logger.Instance.Warn(error);
                                this._errors.Add(error);                                
                            }
                            else if(logLinePos == 0)
                            {
                                ++this.NbrInvalidLines;
                                throw new System.IO.InvalidDataException(string.Format("An invalid log format was detected which is not a valid Log4Net Log file for node \"{0}\" for file \"{1}\"",
                                                                        this.Node.Id.NodeName(),
                                                                        this.LogFile.Name));
                            }

                            ++this.NbrInvalidLines;
                        }
                        return null;
                    }
                   
                    this._lastMessage.AddExtraMessage(logLine);
                    return this._lastMessage;
                }
                else
                {
                    if (logLine.Length > 4)
                    {
                        var nbrCtrlChars = logLine.Count(c => IsControlChar(c));

                        if (nbrCtrlChars > 0)
                        {
                            throw new System.IO.InvalidDataException(string.Format("A Binary file was detected which is not a valid Log4Net Log file for node \"{0}\" for file \"{1}\"",
                                                                        this.Node.Id.NodeName(),
                                                                        this.LogFile.Name));
                        }                    
                    }

                    if (!ignoreErros)
                    {                        
                        if (logLinePos != 0 || this.NbrInvalidLines == 0)
                        {
                            var error = string.Format("{0}\t{1}\tInvalid line detected in Log4Net Log file at line number {3}. Line ignored. Line is: {2}",
                                                this.Node,
                                                this.LogFile.PathResolved,
                                                logLine,
                                                logLinePos);
                            Logger.Instance.Warn(error);
                            this._errors.Add(error);                           
                        }
                        else if(logLinePos == 0 && this.NbrInvalidLines > 10)
                        {
                            ++this.NbrInvalidLines;
                            throw new System.IO.InvalidDataException(string.Format("An invalid log format was detected which is not a valid Log4Net Log file for node \"{0}\" for file \"{1}\"",
                                                                        this.Node.Id.NodeName(),
                                                                        this.LogFile.Name));
                        }

                        ++this.NbrInvalidLines;                        
                    }
                    return null;
                }
                
                this._lastMessage = logMessage;

                #region Log TimeStamp or Time
                if (this._logTimestampValue)
                {                    
                    if (long.TryParse(lineMatch.Groups["Timestamp"].Value.Trim(), out long timeStamp))
                    {
                        logMessage.LogTimeSpan = TimeSpan.FromMilliseconds(timeStamp);
                    }
                    else
                    {
                        if (!ignoreErros)
                        {
                            var error = string.Format("{0}\t{1}\tInvalid Timestamp detected ({2}) in line \"{3}\" within Log4Net Log file at line number {4}. Line ignored.",
                                                    this.Node,
                                                    this.LogFile.PathResolved,
                                                    lineMatch.Groups["Timestamp"].Value,
                                                    logLine,
                                                    logLinePos);
                            Logger.Instance.Warn(error);
                            this._errors.Add(error);
                        }
                        return null;
                    }
                }
                else
                {                    
                    if (string.IsNullOrEmpty(this._datetimeFormat)
                            ? DateTime.TryParse(lineMatch.Groups["Time"].Value.Trim(), out DateTime timeStamp)
                            : DateTime.TryParseExact(lineMatch.Groups["Time"].Value.Trim(),
                                                        this._datetimeFormat,
                                                        CultureInfo.InvariantCulture,
                                                        DateTimeStyles.None,
                                                        out timeStamp))

                    {
                        logMessage.LogDateTime = timeStamp;

                        if (this._timeZoneInstance == null)
                        {
                            logMessage.LogDateTimewTZOffset = new DateTimeOffset(logMessage.LogDateTime, TimeSpan.Zero);
                        }
                        else
                        {
                            //This is an work-around to fix a timing issue in the TZ lib...
                            try
                            {
                                logMessage.LogDateTimewTZOffset = Common.TimeZones.ConvertToOffset(logMessage.LogDateTime, this._timeZoneInstance);
                            }
                            catch (System.Exception)                            
                            {
                                System.Threading.Thread.Sleep(1);
                                logMessage.LogDateTimewTZOffset = Common.TimeZones.ConvertToOffset(logMessage.LogDateTime, this._timeZoneInstance);
                            }
                        }

                        this._endingLogDateTime = logMessage.LogDateTimewTZOffset;
                        if (this._startingLogDateTime == DateTimeOffset.MinValue)
                        {
                            this._startingLogDateTime = logMessage.LogDateTimewTZOffset;
                        }
                    }
                    else
                    {
                        if (!ignoreErros)
                        {
                            var error = string.Format("{0}\t{1}\tInvalid DateTime detected ({2}) in line \"{3}\" within Log4Net Log file at line number {4}{5}. Line ignored.",
                                                    this.Node,
                                                    this.LogFile.PathResolved,
                                                    lineMatch.Groups["Time"].Value,
                                                    logLine,
                                                    logLinePos,
                                                    string.IsNullOrEmpty(this._datetimeFormat) ? string.Empty : this._datetimeFormat);
                            Logger.Instance.Warn(error);
                            this._errors.Add(error);
                        }
                        return null;
                    }
                }
                #endregion
                #region Log Level
                {
                    var levelValue = lineMatch.Groups["Level"].Value.Trim();

                    if(string.IsNullOrEmpty(levelValue))
                    {
                        if (!ignoreErros)
                        {
                            var error = string.Format("{0}\t{1}\tInvalid Log Level detected ({2}) in line \"{3}\" within Log4Net Log file at line number {4}. Line ignored.",
                                                    this.Node,
                                                    this.LogFile.PathResolved,
                                                    levelValue,
                                                    logLine,
                                                    logLinePos);
                            Logger.Instance.Warn(error);
                            this._errors.Add(error);
                        }
                        return null;
                    }
                    else if(levelValue.StartsWith("TRACE", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var splitValue = levelValue.Split('.');
                        var levels = splitValue.Skip(1);

                        logMessage.TraceEnabled = true;
                        levelValue = string.Join(".", levels);                        
                    }

                    if (Enum.TryParse(levelValue, true, out LogLevels logLevel))
                    {
                        logMessage.Level = logLevel;
                    }
                    else
                    {
                        if (!ignoreErros)
                        {
                            var error = string.Format("{0}\t{1}\tInvalid Log Level detected ({2}) in line \"{3}\" within Log4Net Log file at line number {4}. Line ignored.",
                                                    this.Node,
                                                    this.LogFile.PathResolved,
                                                    levelValue,
                                                    logLine,
                                                    logLinePos);
                            Logger.Instance.Warn(error);
                            this._errors.Add(error);
                        }
                        return null;
                    }
                }
                #endregion
                #region Log File Line
                {                    
                    if (int.TryParse(lineMatch.Groups["Line"].Value.Trim(), out int fileLine))
                    {
                        logMessage.FileLine = fileLine;
                    }
                    else
                    {
                        if (!ignoreErros)
                        {
                            var error = string.Format("{0}\t{1}\tInvalid Log File Line detected ({2}) in line \"{3}\" within Log4Net Log file at line number {4}. Line ignored.",
                                                    this.Node,
                                                    this.LogFile.PathResolved,
                                                    lineMatch.Groups["Line"].Value,
                                                    logLine,
                                                    logLinePos);
                            Logger.Instance.Warn(error);
                            this._errors.Add(error);
                        }
                        return null;
                    }
                }
                #endregion
                #region Remaining Log string fields
                logMessage.FileName = lineMatch.Groups["File"].Value.Trim();
                logMessage.ThreadId = lineMatch.Groups["Thread"].Value.Trim();
                logMessage.Message = lineMatch.Groups["Message"].Value.Trim();
                #endregion

                this._logMessages.Add(logMessage);
            }
            catch(System.Exception ex)
            {
                Logger.Instance.Error(string.Format("Exception occurred while parsing line \"{0}\" in file \"{1}\" at line pos {2}",
                                                        logLine,
                                                        this.LogFile.PathResolved,
                                                        logLinePos),
                                        ex);
                throw;
            }
            
            return logMessage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetEndingTimeRange(DateTimeOffset possibleEndDateTime)
        {
            if (this._endingLogDateTime < possibleEndDateTime)
            {
                this._endingLogDateTime = possibleEndDateTime;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetStartingTimeRange(DateTimeOffset possibleStartDateTime)
        {
            if (this._startingLogDateTime == DateTimeOffset.MinValue)
            {
                this._startingLogDateTime = possibleStartDateTime;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetStartingTime(DateTimeOffset startDateTime)
        {
            this._startingLogDateTime = startDateTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetEndingTime(DateTimeOffset endDateTime)
        {
            this._endingLogDateTime = endDateTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool RemoveMessage(LogMessage removeMsg = null)
        {
            bool bResult = false;

            if(this._logMessages.Count > 0)
            {
                if(removeMsg == null)
                {
                    this._logMessages.RemoveAt(this._logMessages.Count - 1);
                    bResult = true;
                }
                else
                {
                    this._logMessages.Remove(removeMsg);
                    bResult = true;
                }
            }

            return bResult;
        }

        #region Dispose Methods

        public bool Disposed { get; private set; }

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
        	Dispose(true);
        	// This object will be cleaned up by the Dispose method.
        	// Therefore, you should call GC.SupressFinalize to
        	// take this object off the finalization queue
        	// and prevent finalization code for this object
        	// from executing a second time.
        	GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
        	// Check to see if Dispose has already been called.
        	if(!this.Disposed)
        	{

        		if(disposing)
        		{
                    // Dispose all managed resources.
                    if (this._logMessages != null) this._logMessages.Clear();
                    if (this._errors != null) this._errors.Clear();

                    this._logMessages = null;
                    this._lastMessage = null;
                    this._errors = null;
                }

        		//Dispose of all unmanaged resources

        		// Note disposing has been done.
        		this.Disposed = true;

        	}
        }
        #endregion //end of Dispose Methods

        #region private members

        LogMessage _lastMessage;
        string _datetimeFormat;
        bool _logTimestampValue = false;
        DateTimeOffset _startingLogDateTime;
        DateTimeOffset _endingLogDateTime;
        Common.Patterns.TimeZoneInfo.IZone _timeZoneInstance = null;       

        static Regex CreateRegEx(string field, IDictionary<string, Log4NetPatternImporter.OutputField> outputFields)
        {
            if (outputFields.TryGetValue(field, out Log4NetPatternImporter.OutputField outputField))
            {
                return new Regex(outputField.Code.ToString(),
                                    RegexOptions.Compiled
                                        | RegexOptions.IgnorePatternWhitespace
                                        | RegexOptions.IgnoreCase);
            }

            return null;
        }

        void DetermineLogDateTime(IDictionary<string, Log4NetPatternImporter.OutputField> outputFields)
        {
            if (outputFields.TryGetValue("Time", out Log4NetPatternImporter.OutputField outputField))
            {
                this._logTimestampValue = false;
                this._datetimeFormat = outputField.Code?.ToString();
            }
            else if (outputFields.TryGetValue("Timestamp", out outputField))
            {
                this._logTimestampValue = true;
                this._datetimeFormat = outputField.Code?.ToString();
            }
            else
            {
                throw new ArgumentOutOfRangeException(string.Format("Could not find the \"Time\" field in the Log4Net Conversion Pattern string ({0}) for file \"{1}\".",
                                                                        this.Log4NetConversionPattern,
                                                                        this.LogFile.PathResolved));
            }
        }

        private static bool IsControlChar(char ch)
        {
            return (ch > ControlChars.NUL && ch < ControlChars.BS)
                || (ch > ControlChars.CR && ch < ControlChars.SUB);
        }

        static class ControlChars
        {
            public static char NUL = (char)0; // Null char
            public static char BS = (char)8; // Back Space
            public static char CR = (char)13; // Carriage Return
            public static char SUB = (char)26; // Substitute
        }
        #endregion
    }
}
