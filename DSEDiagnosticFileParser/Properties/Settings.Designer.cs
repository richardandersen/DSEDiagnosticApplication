﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DSEDiagnosticFileParser.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[{\"item1\":\".zip\",\"item2\":\"zip\"},{\"item1\":\".tar\",\"item2\":\"tar\"},{\"item1\":\".gz\",\"it" +
            "em2\":\"gz\"}] ")]
        public string ExtractFilesWithExtensions {
            get {
                return ((string)(this["ExtractFilesWithExtensions"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[\r\n{\"Catagory\": \"ZipFile\", \"FilePatterns\": [\".\\\\*.tar\",\".\\\\*.tar.gz\",\".\\\\*.zip\"]," +
            " \"FileParsingClass\": \"DSEDiagnosticFileParser.file_unzip\", \"NodeIdPos\": 0, \"Proc" +
            "essingTaskOption\":\"IgnoreNode\", \"ProcessPriorityLevel\":1000},\r\n{\"Catagory\": \"Com" +
            "mandOutputFile\", \"FilePatterns\": [\".\\\\nodes\\\\*\\\\nodetool\\\\status\"], \"FileParsing" +
            "Class\": \"DSEDiagnosticFileParser.file_nodetool_status\", \"NodeIdPos\": 0, \"Process" +
            "ingTaskOption\":\"IgnoreNode,OnlyOnce\", \"ProcessPriorityLevel\":950},\r\n{\"Catagory\":" +
            " \"CommandOutputFile\", \"FilePatterns\": [\".\\\\nodes\\\\*\\\\dsetool\\\\ring\"], \"FileParsi" +
            "ngClass\": \"DSEDiagnosticFileParser.file_dsetool_ring\", \"NodeIdPos\": 0, \"Processi" +
            "ngTaskOption\":\"IgnoreNode,OnlyOnce\", \"ProcessPriorityLevel\":950},\r\n{\"Catagory\": " +
            "\"CommandOutputFile\", \"FilePatterns\": [\".\\\\nodes\\\\*\\\\nodetool\\\\ring\"], \"FileParsi" +
            "ngClass\": \"DSEDiagnosticFileParser.file_nodetool_ring\", \"NodeIdPos\": 0, \"Process" +
            "ingTaskOption\":\"IgnoreNode,OnlyOnce\", \"ProcessPriorityLevel\":950},\r\n{\"Catagory\":" +
            " \"SystemOutputFile\", \"FilePatterns\": [\".\\\\opscenterd\\\\node_info.json\"], \"FilePar" +
            "singClass\": \"DSEDiagnosticFileParser.json_node_info\", \"NodeIdPos\": -1, \"Processi" +
            "ngTaskOption\":\"AllNodesInDataCenter\",\"ProcessPriorityLevel\":900},\r\n{\"Catagory\": " +
            "\"SystemOutputFile\", \"FilePatterns\": [\".\\\\opscenterd\\\\repair_service.json\"], \"Fil" +
            "eParsingClass\": \"DSEDiagnosticFileParser.json_repair_service\", \"NodeIdPos\": -1, " +
            "\"ProcessingTaskOption\":\"AllNodesInDataCenter\",\"ProcessPriorityLevel\":900},\r\n{\"Ca" +
            "tagory\": \"ConfigurationFile\", \"FilePatterns\": [\".\\\\nodes\\\\*\\\\*.yaml\"], \"FilePars" +
            "ingClass\": \"DSEDiagnosticFileParser.file_yaml\", \"NodeIdPos\": -1, \"ProcessingTask" +
            "Option\":\"ScanForNode,ParallelProcessing\",\"ProcessPriorityLevel\":800},\r\n{\"Catagor" +
            "y\": \"SystemOutputFile\", \"FilePatterns\": [\".\\\\nodes\\\\*\\\\machine-info.json\"], \"Fil" +
            "eParsingClass\": \"DSEDiagnosticFileParser.json_machine_info\", \"NodeIdPos\": -1, \"P" +
            "rocessingTaskOption\":\"ScanForNode,ParallelProcessing\", \"ProcessPriorityLevel\":10" +
            "0},\r\n{\"Catagory\": \"SystemOutputFile\", \"FilePatterns\": [\".\\\\nodes\\\\*\\\\os-info.jso" +
            "n\"], \"FileParsingClass\": \"DSEDiagnosticFileParser.json_os_info\", \"NodeIdPos\": -1" +
            ", \"ProcessingTaskOption\":\"ScanForNode,ParallelProcessing\", \"ProcessPriorityLevel" +
            "\":100},\r\n{\"Catagory\": \"SystemOutputFile\", \"FilePatterns\": [\".\\\\nodes\\\\*\\\\os-metr" +
            "ics\\\\cpu.json\"], \"FileParsingClass\": \"DSEDiagnosticFileParser.jason_cpu\", \"NodeI" +
            "dPos\": -1, \"ProcessingTaskOption\":\"ScanForNode,ParallelProcessing\", \"ProcessPrio" +
            "rityLevel\":100},\r\n{\"Catagory\": \"SystemOutputFile\", \"FilePatterns\": [\".\\\\nodes\\\\*" +
            "\\\\os-metrics\\\\load_avg.json\"], \"FileParsingClass\": \"DSEDiagnosticFileParser.file" +
            "_load_avg\", \"NodeIdPos\": -1, \"ProcessingTaskOption\":\"ScanForNode,ParallelProcess" +
            "ing\", \"ProcessPriorityLevel\":100},\r\n{\"Catagory\": \"CommandOutputFile\", \"FilePatte" +
            "rns\": [\".\\\\nodes\\\\*\\\\agent_version.json\"], \"FileParsingClass\": \"DSEDiagnosticFil" +
            "eParser.file_agent_version\", \"NodeIdPos\": -1, \"ProcessingTaskOption\":\"ScanForNod" +
            "e,ParallelProcessing\", \"ProcessPriorityLevel\":100},\r\n{\"Catagory\": \"SystemOutputF" +
            "ile\", \"FilePatterns\": [\".\\\\nodes\\\\*\\\\java_system_properties.json\"], \"FileParsing" +
            "Class\": \"DSEDiagnosticFileParser.json_java_system_properties\", \"NodeIdPos\": -1, " +
            "\"ProcessingTaskOption\":\"ScanForNode,ParallelProcessing\", \"ProcessPriorityLevel\":" +
            "100},\r\n{\"Catagory\": \"SystemOutputFile\", \"FilePatterns\": [\".\\\\nodes\\\\*\\\\ntp\\\\ntps" +
            "tat\"], \"FileParsingClass\": \"DSEDiagnosticFileParser.file_ntpstat\", \"NodeIdPos\": " +
            "-1, \"ProcessingTaskOption\":\"ScanForNode,ParallelProcessing\", \"ProcessPriorityLev" +
            "el\":100},\r\n{\"Catagory\": \"SystemOutputFile\", \"FilePatterns\": [\".\\\\nodes\\\\*\\\\ntp\\\\" +
            "ntptime\"], \"FileParsingClass\": \"DSEDiagnosticFileParser.file_ntptime\", \"NodeIdPo" +
            "s\": -1, \"ProcessingTaskOption\":\"ScanForNode,ParallelProcessing\", \"ProcessPriorit" +
            "yLevel\":100},\r\n{\"Catagory\": \"SystemOutputFile\", \"FilePatterns\": [\".\\\\nodes\\\\*\\\\j" +
            "ava_heap.json\"], \"FileParsingClass\": \"DSEDiagnosticFileParser.json_java_heap\", \"" +
            "NodeIdPos\": -1, \"ProcessingTaskOption\":\"ScanForNode,ParallelProcessing\", \"Proces" +
            "sPriorityLevel\":100},\r\n{\"Catagory\": \"SystemOutputFile\", \"FilePatterns\": [\".\\\\nod" +
            "es\\\\*\\\\os-metrics\\\\memory.json\"], \"FileParsingClass\": \"DSEDiagnosticFileParser.j" +
            "son_memory\", \"NodeIdPos\": -1, \"ProcessingTaskOption\":\"ScanForNode,ParallelProces" +
            "sing\", \"ProcessPriorityLevel\":100},\r\n{\"Catagory\": \"CommandOutputFile\", \"FilePatt" +
            "erns\": [\".\\\\nodes\\\\*\\\\nodetool\\\\info\"], \"FileParsingClass\": \"DSEDiagnosticFilePa" +
            "rser.file_nodetool_info\", \"NodeIdPos\": -1, \"ProcessingTaskOption\":\"ScanForNode,P" +
            "arallelProcessing\", \"ProcessPriorityLevel\":100},\r\n{\"Catagory\": \"LogFile\", \"FileP" +
            "atterns\": [\".\\\\nodes\\\\*\\\\logs\\\\cassandra\\\\system.log\"], \"FileParsingClass\": \"Use" +
            "rQuery.TestClass\", \"NodeIdPos\": -1}\r\n]")]
        public string ProcessFileMappings {
            get {
                return ((string)(this["ProcessFileMappings"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"{
""file_nodetool_status"":{""RegExStrings"":[""datacenter:\\s+([a-z0-9\\-_$%+=@!?<>^*&]+)\\s*"",""(un|ul|uj|um|dn|dl|dJ|dm)\\s+([a-z0-9.:_\\-+%]+)\\s+([0-9.]+)\\s*([0-9a-z]{0,2})\\s+(\\d+)\\s+([0-9?.% ]+)\\s+([0-9a-f\\-]+)\\s+(.+)""]},
""file_ntpstat"":{""RegExStrings"":[""synchronised to NTP server\\s+\\((.+)\\).+stratum\\s+(\\d+).+time correct.+within\\s+(\\d+\\s+[a-zA-Z]+).+polling.+every\\s+(\\d+\\s+[a-zA-Z]+)""]},
""file_ntptime"":{""RegExStrings"":[""ntp_adjtime.+frequency\\s+([0-9\\-.]+\\s+[a-zA-Z]+)\\,\\s+interval\\s+([0-9\\-.]+\\s+[a-zA-Z]+)\\,\\s+maximum error\\s+([0-9\\-.]+\\s+[a-zA-Z]+)\\,\\s+estimated error\\s+([0-9\\-.]+\\s+[a-zA-Z]+)\\,.+time constant\\s+([0-9\\-.]+)\\,\\s+precision\\s+([0-9\\-.]+\\s+[a-zA-Z]+)\\,\\s+tolerance\\s+([0-9]+\\s+[a-zA-Z]+)"",""ntp_gettime.+maximum error\\s+([0-9\\-.]+\\s+[a-zA-Z]+)\\,\\s+estimated error\\s+([0-9\\-.]+\\s+[a-zA-Z]+)""]},
""file_dsetool_ring"":{""RegExStrings"":[""([a-z0-9.:_\\-+%]+)\\s+([a-z0-9\\-_$%+=@!?<>^*&]+)\\s+([a-z0-9\\-_$%+=@!?<>^*&]+)\\s+([a-z]+)\\s+([a-z]+)\\s+([a-z]+)\\s+([a-z]+)\\s+([0-9.]+\\s*[a-z]{1,2})\\s+(\\w+|\\?)\\s+([0-9\\-]+)""]},
""file_nodetool_ring"":{""RegExStrings"":[""datacenter:\\s+([a-z0-9\\-_$%+=@!?<>^*&]+)\\s*"",""\\s*([0-9\\-]+)"",""([a-z0-9.:_\\-+%]+)\\s+([a-z0-9\\-_$%+=@!?<>^*&]+)\\s+([a-z]+)\\s+([a-z]+)\\s+([0-9.]+\\s*[a-z]{1,2})\\s+(\\w+|\\?)\\s+([0-9\\-]+)""]}
}")]
        public string DiagnosticFileRegExAssocations {
            get {
                return ((string)(this["DiagnosticFileRegExAssocations"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>password</string>\r\n</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection ObscureFiledValues {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["ObscureFiledValues"]));
            }
        }
    }
}
