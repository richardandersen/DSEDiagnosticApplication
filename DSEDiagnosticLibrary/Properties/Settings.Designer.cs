﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DSEDiagnosticLibrary.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.9.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>+</string>\r\n  <string>@</string>\r\n</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection HostNamePathNameCharSeparators {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["HostNamePathNameCharSeparators"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MiB")]
        public string DefaultStorageSizeUnit {
            get {
                return ((string)(this["DefaultStorageSizeUnit"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("SEC")]
        public string DefaultTimeUnit {
            get {
                return ((string)(this["DefaultTimeUnit"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"(?:(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|((([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))|(([0-9A-Fa-f]{1,4}:){6}(:[0-9A-Fa-f]{1,4}|((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){5}(((:[0-9A-Fa-f]{1,4}){1,2})|:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){4}(((:[0-9A-Fa-f]{1,4}){1,3})|((:[0-9A-Fa-f]{1,4})?:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){3}(((:[0-9A-Fa-f]{1,4}){1,4})|((:[0-9A-Fa-f]{1,4}){0,2}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){2}(((:[0-9A-Fa-f]{1,4}){1,5})|((:[0-9A-Fa-f]{1,4}){0,3}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){1}(((:[0-9A-Fa-f]{1,4}){1,6})|((:[0-9A-Fa-f]{1,4}){0,4}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(:(((:[0-9A-Fa-f]{1,4}){1,7})|((:[0-9A-Fa-f]{1,4}){0,5}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:)))(%[0-9A-Za-z]+)?(/[1-9]{1,3})?)")]
        public string IPAdressRegEx {
            get {
                return ((string)(this["IPAdressRegEx"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"[
{""ConfigType"":""DSE"",""ContainsString"":""dse.yaml"",""MatchAction"":""FileNamewExtension,Equals""},
{""ConfigType"":""DSE"",""ContainsString"":""dse-env.sh"",""MatchAction"":""FileNamewExtension,Equals""},
{""ConfigType"":""DSE"",""ContainsString"":""dse"",""MatchAction"":""FileNamewExtension,Equals""},
{""ConfigType"":""Solr"",""ContainsString"":""solr"",""MatchAction"":""FileNameOnly,StartsWith""},
{""ConfigType"":""Spark"",""ContainsString"":""spark"",""MatchAction"":""FileNameOnly,StartsWith""},
{""ConfigType"":""Spark"",""ContainsString"":""spark"",""MatchAction"":""FileNameOnly,EndsWith""},
{""ConfigType"":""Spark"",""ContainsString"":""dse-spark"",""MatchAction"":""FileNameOnly,StartsWith""},
{""ConfigType"":""Hadoop"",""ContainsString"":""hadoop"",""MatchAction"":""FileNameOnly,Contains""},
{""ConfigType"":""Snitch"",""ContainsString"":""cassandra-topology.properties"",""MatchAction"":""FileNamewExtension,Equals""},
{""ConfigType"":""Snitch"",""ContainsString"":""cassandra-rackdc.properties"",""MatchAction"":""FileNamewExtension,Equals""},
{""ConfigType"":""Snitch"",""ContainsString"":""cassandra-topology.yaml"",""MatchAction"":""FileNamewExtension,Equals""},
{""ConfigType"":""Cassandra"",""ContainsString"":""cassandra.yaml"",""MatchAction"":""FileNamewExtension,Equals""},
{""ConfigType"":""Cassandra"",""ContainsString"":""cassandra-env.sh"",""MatchAction"":""FileNamewExtension,Equals""},
{""ConfigType"":""OpsCenter"",""ContainsString"":""address.yaml"",""MatchAction"":""FileNamewExtension,Equals""}
]")]
        public string ConfigTypeMappers {
            get {
                return ((string)(this["ConfigTypeMappers"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MiB")]
        public string DefaultMemorySizeUnit {
            get {
                return ((string)(this["DefaultMemorySizeUnit"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MiB, SEC")]
        public string DefaultStorageRate {
            get {
                return ((string)(this["DefaultStorageRate"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MiB, SEC")]
        public string DefaultMemoryRate {
            get {
                return ((string)(this["DefaultMemoryRate"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("9")]
        public int UnitOfMeasureRoundDecimals {
            get {
                return ((int)(this["UnitOfMeasureRoundDecimals"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>map</string>\r\n  <string>set</string>\r\n  <string>list</string>\r\n</ArrayOfSt" +
            "ring>")]
        public global::System.Collections.Specialized.StringCollection CQLCollectionTypes {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["CQLCollectionTypes"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("(?:\\s|^)tuple\\s*\\<")]
        public string TupleRegEx {
            get {
                return ((string)(this["TupleRegEx"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("(?:\\s|^)frozen\\s*\\<")]
        public string FrozenRegEx {
            get {
                return ((string)(this["FrozenRegEx"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("\\sstatic(?:\\s|$)")]
        public string StaticRegEx {
            get {
                return ((string)(this["StaticRegEx"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("\\sprimary\\s+key(?:\\s|$)")]
        public string PrimaryKeyRegEx {
            get {
                return ((string)(this["PrimaryKeyRegEx"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("(?:\\s|^)blob(?:\\s|$)")]
        public string BlobRegEx {
            get {
                return ((string)(this["BlobRegEx"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("(?:\\s|^)counter(?:\\s|$)")]
        public string CounterRegEx {
            get {
                return ((string)(this["CounterRegEx"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>system_auth</string>
  <string>system_distributed</string>
  <string>system_schema</string>
  <string>system</string>
  <string>system_traces</string>
</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection SystemKeyspaces {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["SystemKeyspaces"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>dse_system</string>
  <string>dse_security</string>
  <string>solr_admin</string>
  <string>dse_auth</string>
  <string>dse_leases</string>
  <string>dse_perf</string>
  <string>OpsCenter</string>
  <string>HiveMetaStore</string>
  <string>cfs_archive</string>
  <string>cfs</string>
  <string>dsefs</string>
  <string>dse_system_local</string>
  <string>dse_analytics</string>
  <string>dse_insights_local</string>
  <string>dse_insights</string>
</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection DSEKeyspaces {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["DSEKeyspaces"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>system_traces</string>\r\n  <string>dse_perf</string>\r\n</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection PerformanceKeyspaces {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["PerformanceKeyspaces"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>system.paxos</string>
  <string>system.batches</string>
  <string>system.batchlog</string>
  <string>system.prepared_statements</string>
  <string>system_traces</string>
  <string>dse_perf</string>
  <string>system_auth</string>
  <string>dse_security</string>
  <string>system.hints</string>
  <string>OpsCenter</string>
</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection TablesUsageFlag {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["TablesUsageFlag"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>SASIIndex</string>\r\n</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection IsSasIIIndexClasses {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["IsSasIIIndexClasses"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>Cql3SolrSecondaryIndex</string>
  <string>ThriftSolrSecondaryIndex</string>
</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection IsSolrIndexClass {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["IsSolrIndexClass"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("^\\s*\\(?(\\-?\\d+)\\s*\\,\\s*(\\-?\\d+)\\s*\\]?\\s*$")]
        public string TokenRangeRegEx {
            get {
                return ((string)(this["TokenRangeRegEx"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>tmp</string>
  <string>ka</string>
  <string>jb</string>
  <string>ja</string>
  <string>ic</string>
  <string>bti</string>
  <string>mc</string>
  <string>aa</string>
  <string>md</string>
  <string>ac</string>
  <string>mb</string>
  <string>lb</string>
  <string>la</string>
  <string>k</string>
  <string>j</string>
</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection SSTableVersionMarkers {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["SSTableVersionMarkers"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("^(?:([a-z0-9\\-_$%+=@!?<>^*&]+)\\-([0-9a-f]{32})$|([a-z0-9\\-_$%+=@!?<>^*&]+)$)")]
        public string SSTableColumnFamilyRegEx {
            get {
                return ((string)(this["SSTableColumnFamilyRegEx"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("100")]
        public int LogMessageToStringMaxLength {
            get {
                return ((int)(this["LogMessageToStringMaxLength"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("UTC")]
        public string DefaultClusterTZ {
            get {
                return ((string)(this["DefaultClusterTZ"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool LogEventsAreMemoryMapped {
            get {
                return ((bool)(this["LogEventsAreMemoryMapped"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("debug")]
        public string DebugLogFileName {
            get {
                return ((string)(this["DebugLogFileName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("^f[ef]\\d+::\\d+$")]
        public string IgnoreIP6AddressRegEx {
            get {
                return ((string)(this["IgnoreIP6AddressRegEx"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("snapshots")]
        public string SSTablePathSnapShotDirName {
            get {
                return ((string)(this["SSTablePathSnapShotDirName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3")]
        public int SSTablePathSnapShotDirPos {
            get {
                return ((int)(this["SSTablePathSnapShotDirPos"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("^(?:([0-9a-f]{32})$|([0-9a-f\\-]{36})$)")]
        public string SSTablePathSnapShotDirRegEx {
            get {
                return ((string)(this["SSTablePathSnapShotDirRegEx"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public int SSTablePathSnapShotGuidDirPos {
            get {
                return ((int)(this["SSTablePathSnapShotGuidDirPos"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("164")]
        public ulong SolrAttrChar {
            get {
                return ((ulong)(this["SolrAttrChar"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8593")]
        public ulong HighAttrChar {
            get {
                return ((ulong)(this["HighAttrChar"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8595")]
        public ulong LowAttrChar {
            get {
                return ((ulong)(this["LowAttrChar"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8801")]
        public ulong MidAttrChar {
            get {
                return ((ulong)(this["MidAttrChar"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("9675")]
        public ulong PHAttrChar {
            get {
                return ((ulong)(this["PHAttrChar"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8800")]
        public ulong UpTimeLogMismatchAttrChar {
            get {
                return ((ulong)(this["UpTimeLogMismatchAttrChar"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("04:00:00")]
        public global::System.TimeSpan ThresholdAvgHrs {
            get {
                return ((global::System.TimeSpan)(this["ThresholdAvgHrs"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("16:00:00")]
        public global::System.TimeSpan ThresholdUpTimeLogHrs {
            get {
                return ((global::System.TimeSpan)(this["ThresholdUpTimeLogHrs"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool EnableAttrSymbols {
            get {
                return ((bool)(this["EnableAttrSymbols"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8366")]
        public ulong TriggerAttrChar {
            get {
                return ((ulong)(this["TriggerAttrChar"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8376")]
        public ulong IndexAttrChar {
            get {
                return ((ulong)(this["IndexAttrChar"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8594")]
        public ulong MVTblAttrChar {
            get {
                return ((ulong)(this["MVTblAttrChar"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8592")]
        public ulong MVAttrChar {
            get {
                return ((ulong)(this["MVAttrChar"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("41990")]
        public ulong GraphAttrChar {
            get {
                return ((ulong)(this["GraphAttrChar"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("42222")]
        public ulong AnalyticsAttrChar {
            get {
                return ((ulong)(this["AnalyticsAttrChar"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("04:00:00")]
        public global::System.TimeSpan NodeDetectedLongPuaseThreshold {
            get {
                return ((global::System.TimeSpan)(this["NodeDetectedLongPuaseThreshold"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[\r\n{\"Name\":\"solr_query\",\"ColType\":\"text\"}\r\n]")]
        public string CQLSpecialColumns {
            get {
                return ((string)(this["CQLSpecialColumns"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool TreatLogDirNamesAsHostName {
            get {
                return ((bool)(this["TreatLogDirNamesAsHostName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("-2")]
        public decimal DCDQNegativeNumeratorFactor {
            get {
                return ((decimal)(this["DCDQNegativeNumeratorFactor"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("-1")]
        public decimal DCDQNodeSysLogAvgStateFactor {
            get {
                return ((decimal)(this["DCDQNodeSysLogAvgStateFactor"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("-1.5")]
        public decimal DCDQNoCountsFactor {
            get {
                return ((decimal)(this["DCDQNoCountsFactor"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("-0.5")]
        public decimal DCDQSysDebugLogAvgStateFactor {
            get {
                return ((decimal)(this["DCDQSysDebugLogAvgStateFactor"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.15")]
        public decimal DCNodeReadThresholdPct {
            get {
                return ((decimal)(this["DCNodeReadThresholdPct"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.15")]
        public decimal DCNodeWriteThresholdPct {
            get {
                return ((decimal)(this["DCNodeWriteThresholdPct"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8801")]
        public ulong UpTimeLogSimilarAttrChar {
            get {
                return ((ulong)(this["UpTimeLogSimilarAttrChar"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5")]
        public int DebugLogFileRequiredDSEVersion {
            get {
                return ((int)(this["DebugLogFileRequiredDSEVersion"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3")]
        public int DebugLogFileRequiredCVersion {
            get {
                return ((int)(this["DebugLogFileRequiredCVersion"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("-2")]
        public decimal NodeDQNegativeMissingSystemLogs {
            get {
                return ((decimal)(this["NodeDQNegativeMissingSystemLogs"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("-1")]
        public decimal NodeDQNegativeMissingDebugLogs {
            get {
                return ((decimal)(this["NodeDQNegativeMissingDebugLogs"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("-0.1")]
        public decimal NodeDQNegativeStarts {
            get {
                return ((decimal)(this["NodeDQNegativeStarts"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("-0.25")]
        public decimal NodeDQNegativeTokenChanges {
            get {
                return ((decimal)(this["NodeDQNegativeTokenChanges"]));
            }
        }
    }
}
