﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSEDiagnosticLibrary;
using Common;
using Common.Path;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;

namespace DSEDiagnosticFileParser
{
	public abstract class ProcessJsonFile : DiagnosticFile
	{
		public ProcessJsonFile(CatagoryTypes catagory,
									IDirectoryPath diagnosticDirectory,
									IFilePath file,
									INode node,
                                    string defaultClusterName,
                                    string defaultDCName)
			: base(catagory, diagnosticDirectory, file, node, defaultClusterName, defaultDCName)
		{
		}

        public override uint ProcessFile()
        {
            JObject jsonObject = null;

            using (StreamReader fileStream = this.File.OpenText())
            using (JsonTextReader reader = new JsonTextReader(fileStream))
            {
                jsonObject = (JObject)JToken.ReadFrom(reader);
            }

            return jsonObject == null ? 0 : this.ProcessJSON(jsonObject);
        }

        public abstract uint ProcessJSON(JObject jObject);

        public abstract override IResult GetResult();
    }
}
