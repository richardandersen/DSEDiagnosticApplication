﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using DSEDiagnosticLibrary;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace DSEDiagnosticFileParser
{
    [JsonObject(MemberSerialization.OptOut)]
    public class json_location : ProcessJsonFile
    {
        public json_location(CatagoryTypes catagory,
                                    IDirectoryPath diagnosticDirectory,
                                    IFilePath file,
                                    INode node,
                                    string defaultClusterName,
                                    string defaultDCName,
                                    Version targetDSEVersion)
			: base(catagory, diagnosticDirectory, file, node, defaultClusterName, defaultDCName, targetDSEVersion)
		{
        }

        public override uint ProcessJSON(JObject jObject)
        {

            jObject.TryGetValue("dse").NullSafeSet<string>(c => { if (c != "package") this.Node.DSE.Locations.DSEYamlFile = c; });
            jObject.TryGetValue("cassandra").NullSafeSet<string>(c => { if (c != "package") this.Node.DSE.Locations.CassandraYamlFile = c; });

            this.NbrItemsParsed = 2;
            this.Processed = true;
            return 0;
        }

        public override IResult GetResult()
        {
            return new EmptyResult(this.File, null, null, this.Node);
        }
    }
}
