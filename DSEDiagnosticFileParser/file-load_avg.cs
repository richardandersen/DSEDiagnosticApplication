﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Common;
using DSEDiagnosticLibrary;

namespace DSEDiagnosticFileParser
{
    [JsonObject(MemberSerialization.OptOut)]
    public sealed class file_load_avg : DiagnosticFile
    {
        public file_load_avg(CatagoryTypes catagory,
                                IDirectoryPath diagnosticDirectory,
                                IFilePath file,
                                INode node,
                                string defaultClusterName,
                                string defaultDCName,
                                Version targetDSEVersion)
            : base(catagory, diagnosticDirectory, file, node, defaultClusterName, defaultDCName,targetDSEVersion)
        {
        }

        public override IResult GetResult()
        {
            return new EmptyResult(this.File, null, null, this.Node);
        }

        public override uint ProcessFile()
        {
            var fileLine = this.File.ReadAllText();
            
            if(!string.IsNullOrEmpty(fileLine))
            {
                this.Node.Machine.CPULoad.Average = UnitOfMeasure.Create(fileLine, UnitOfMeasure.Types.Utilization | UnitOfMeasure.Types.Percent);
                ++this.NbrItemsParsed;
            }

            this.Processed = true;
            return 0;
        }
    }
}
