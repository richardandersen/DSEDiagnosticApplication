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
    public sealed class json_machine_info : ProcessJsonFile
    {
        public json_machine_info(CatagoryTypes catagory,
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

            jObject.TryGetValue("arch").NullSafeSet<string>(c => this.Node.Machine.CPU.Architecture = c);
            jObject.TryGetValue("memory").NullSafeSet<long>(c => this.Node.Machine.Memory.PhysicalMemory = UnitOfMeasure.Create(c, UnitOfMeasure.Types.Memory | UnitOfMeasure.Types.MiB));

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
