﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Common;
using DSEDiagnosticLibrary;

namespace DSEDiagnosticFileParser
{
    public sealed class file_nodetool_status : DiagnosticFile
    {
        public file_nodetool_status(CatagoryTypes catagory, IDirectoryPath diagnosticDirectory, IFilePath file, INode node)
            : base(catagory, diagnosticDirectory, file, node)
        {
        }

        public override IEnumerable<T> GetItems<T>()
        {
            throw new NotImplementedException();
        }

        public override uint ProcessFile()
        {
            var fileLines = this.File.ReadAllLines();
            uint nbrItems = 0;

            /*
             Datacenter: datacenter1
            =======================
            Status=Up/Down
            |/ State=Normal/Leaving/Joining/Moving
            --  Address    Load       Tokens  Owns    Host ID                               Rack
            UN  127.0.0.1  47.66 KB   1       33.3%   aaa1b7c1-6049-4a08-ad3e-3697a0e30e10  rack1
            UN  127.0.0.2  47.67 KB   1       33.3%   1848c369-4306-4874-afdf-5c1e95b8732e  rack1
            UN  127.0.0.3  47.67 KB   1       33.3%   49578bf1-728f-438d-b1c1-d8dd644b6f7f  rack1
            1   2           3    4    5       6       7                                     8
            */

            string currentDataCenter = null;
            string line = null;
            Match regExMatch = null;

            foreach (var nodetoolLine in fileLines)
            {
                line = nodetoolLine.Trim();

                if (string.IsNullOrEmpty(line)
                        || line[0] == '='
                        || line[0] == '|'
                        || line[0] == '-'
                        || line[0] == 'S')
                {
                    continue;
                }

                //DataCenter
                regExMatch = this.RegExParser.Match(line, 0);

                if (regExMatch.Success)
                {
                    currentDataCenter = regExMatch.Groups[1].Value;
                    continue;
                }

                //Node
                regExMatch = this.RegExParser.Match(line, 1);

                if(regExMatch.Success)
                {
                    var node = Cluster.TryGetAddNode(regExMatch.Groups[2].Value, currentDataCenter);

                    node.DSE.HostId = new Guid(regExMatch.Groups[7].Value);
                    node.DSE.Rack = regExMatch.Groups[8].Value;
                    node.DSE.StorageUsed = UnitOfMeasure.Create(regExMatch.Groups[3].Value, regExMatch.Groups[4].Value, UnitOfMeasure.Types.Storage);

                    switch(regExMatch.Groups[1].Value[0])
                    {
                        case 'U':
                        case 'u':
                            node.DSE.Statuses = DSEInfo.DSEStatuses.Up;
                            break;
                        case 'D':
                        case 'd':
                            node.DSE.Statuses = DSEInfo.DSEStatuses.Down;
                            break;
                    }

                    if (regExMatch.Groups[6].Value[0] != '?')
                    {
                        node.DSE.StorageUtilization = UnitOfMeasure.Create(regExMatch.Groups[6].Value, UnitOfMeasure.Types.Storage | UnitOfMeasure.Types.Percent | UnitOfMeasure.Types.Utilization);
                    }

                    int nbrVNodes;
                    if(int.TryParse(regExMatch.Groups[5].Value, out nbrVNodes))
                    {
                        if(nbrVNodes == 1)
                        {
                            node.DSE.VNodesEnabled = false;
                        }
                        else
                        {
                            node.DSE.VNodesEnabled = true;
                            node.DSE.NbrVNodes = (uint) nbrVNodes;
                        }
                    }

                    ++nbrItems;
                    Logger.Instance.InfoFormat("Added node \"{0}\" to DataCenter \"{1}\"", node.Id, node.DataCenter.Name);
                    continue;
                }
            }

            return nbrItems;
        }
    }
}