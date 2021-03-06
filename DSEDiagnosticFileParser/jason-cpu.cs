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
    public sealed class jason_cpu : ProcessJsonFile
    {
        public jason_cpu(CatagoryTypes catagory,
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

            jObject.TryGetValue("%idle").NullSafeSet<decimal>(c => this.Node.Machine.CPULoad.Idle = UnitOfMeasure.Create(c, UnitOfMeasure.Types.Percent | UnitOfMeasure.Types.Utilization));
            jObject.TryGetValue("%system").NullSafeSet<decimal>(c => this.Node.Machine.CPULoad.System = UnitOfMeasure.Create(c, UnitOfMeasure.Types.Percent | UnitOfMeasure.Types.Utilization));
            jObject.TryGetValue("%user").NullSafeSet<decimal>(c => this.Node.Machine.CPULoad.User = UnitOfMeasure.Create(c, UnitOfMeasure.Types.Percent | UnitOfMeasure.Types.Utilization));
            jObject.TryGetValue("%iowait").NullSafeSet<decimal>(c => this.Node.Machine.CPULoad.IOWait = UnitOfMeasure.Create(c, UnitOfMeasure.Types.Percent | UnitOfMeasure.Types.Utilization));
            jObject.TryGetValue("%steal").NullSafeSet<decimal>(c => this.Node.Machine.CPULoad.StealTime = UnitOfMeasure.Create(c, UnitOfMeasure.Types.Percent | UnitOfMeasure.Types.Utilization));

            this.NbrItemsParsed = 5;
            this.Processed = true;
            return 0;
        }

        public override IResult GetResult()
        {
            return new EmptyResult(this.File, null, null, this.Node);
        }
    }
}
