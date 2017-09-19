﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using DSEDiagnosticLogger;
using Common;

namespace DSEDiagnosticToDataTable
{
    public sealed class NodeDataTable : DataTableLoad
    {
        public NodeDataTable(DSEDiagnosticLibrary.Cluster cluster, CancellationTokenSource cancellationSource = null)
            : base(cluster, cancellationSource)
        {}

        public override DataTable CreateInitializationTable()
        {
            var dtNodeInfo = new DataTable(TableNames.Node, TableNames.Namespace);

            dtNodeInfo.Columns.Add(ColumnNames.NodeIPAddress, typeof(string));
            dtNodeInfo.Columns[ColumnNames.NodeIPAddress].Unique = true; //A
            dtNodeInfo.PrimaryKey = new System.Data.DataColumn[] { dtNodeInfo.Columns[ColumnNames.NodeIPAddress] };

            dtNodeInfo.Columns.Add(ColumnNames.DataCenter, typeof(string));

            dtNodeInfo.Columns.Add("Rack", typeof(string));
            dtNodeInfo.Columns.Add("Status", typeof(string));
            dtNodeInfo.Columns.Add("Instance Type", typeof(string)).AllowDBNull = true;
            dtNodeInfo.Columns.Add("Cluster Name", typeof(string)).AllowDBNull = true;
            dtNodeInfo.Columns.Add("Storage Used (MB)", typeof(decimal)).AllowDBNull = true; //G
            dtNodeInfo.Columns.Add("Storage Utilization", typeof(decimal)).AllowDBNull = true;
            dtNodeInfo.Columns.Add("Health Rating", typeof(string)).AllowDBNull = true;

            dtNodeInfo.Columns.Add("Time Zone Offset", typeof(object)).AllowDBNull = true; //J

            dtNodeInfo.Columns.Add("Start NodeTool Range", typeof(DateTime)).AllowDBNull = true; //k
            dtNodeInfo.Columns.Add("End NodeTool Range", typeof(DateTime)).AllowDBNull = true; //l

            dtNodeInfo.Columns.Add("Uptime (Days)", typeof(TimeSpan)).AllowDBNull = true; //m
            dtNodeInfo.Columns.Add("Uptime", typeof(string)).AllowDBNull = true; //n

            dtNodeInfo.Columns.Add("Log Min Timestamp", typeof(DateTime)).AllowDBNull = true;//o
            dtNodeInfo.Columns.Add("Log Max Timestamp", typeof(DateTime)).AllowDBNull = true;
            dtNodeInfo.Columns.Add("Log Duration", typeof(TimeSpan)).AllowDBNull = true;
            dtNodeInfo.Columns.Add("Log Timespan Difference", typeof(TimeSpan)).AllowDBNull = true;//r
            dtNodeInfo.Columns.Add("Log Nbr Files", typeof(int)).AllowDBNull = true;//s

            dtNodeInfo.Columns.Add("Debug Log Min Timestamp", typeof(DateTime)).AllowDBNull = true;//st
            dtNodeInfo.Columns.Add("Debug Log Max Timestamp", typeof(DateTime)).AllowDBNull = true;
            dtNodeInfo.Columns.Add("Debug Log Duration", typeof(TimeSpan)).AllowDBNull = true;
            dtNodeInfo.Columns.Add("Debug Log Timespan Difference", typeof(TimeSpan)).AllowDBNull = true;//w
            dtNodeInfo.Columns.Add("Debug Log Nbr Files", typeof(int)).AllowDBNull = true;//x

            dtNodeInfo.Columns.Add("Heap Memory (MB)", typeof(string)).AllowDBNull = true; //y
            dtNodeInfo.Columns.Add("Off Heap Memory (MB)", typeof(decimal)).AllowDBNull = true;//z
            dtNodeInfo.Columns.Add("Nbr VNodes", typeof(int)).AllowDBNull = true;//aa
            dtNodeInfo.Columns.Add("Nbr of Exceptions", typeof(int)).AllowDBNull = true;//ab
            dtNodeInfo.Columns.Add("Percent Repaired", typeof(decimal)).AllowDBNull = true;//ac
            dtNodeInfo.Columns.Add("Repair Service Enabled", typeof(bool)).AllowDBNull = true;//ad
            dtNodeInfo.Columns.Add("Seed Node", typeof(bool)).AllowDBNull = true;//ae
            dtNodeInfo.Columns.Add("Gossip Enabled", typeof(bool)).AllowDBNull = true;//af
            dtNodeInfo.Columns.Add("Thrift Enabled", typeof(bool)).AllowDBNull = true;//ag
            dtNodeInfo.Columns.Add("Native Transport Enabled", typeof(bool)).AllowDBNull = true;//ah
            dtNodeInfo.Columns.Add("Key Cache Information", typeof(string)).AllowDBNull = true;//ai
            dtNodeInfo.Columns.Add("Row Cache Information", typeof(string)).AllowDBNull = true;//aj
            dtNodeInfo.Columns.Add("Counter Cache Information", typeof(string)).AllowDBNull = true;//ak

            dtNodeInfo.DefaultView.ApplyDefaultSort = false;
            dtNodeInfo.DefaultView.AllowDelete = false;
            dtNodeInfo.DefaultView.AllowEdit = false;
            dtNodeInfo.DefaultView.AllowNew = false;
            dtNodeInfo.DefaultView.Sort = string.Format("[{0}] ASC, [{1}] ASC", ColumnNames.NodeIPAddress, ColumnNames.DataCenter);

            return dtNodeInfo;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Should re-thrown any exception except for OperationCanceledException</exception>
        public override DataTable LoadTable()
        {
            this.Table.BeginLoadData();
            try
            {
                DataRow dataRow = null;
                int nbrItems = 0;

                foreach (var dataCenter in this.Cluster.DataCenters)
                {
                    nbrItems = 0;
                    this.CancellationToken.ThrowIfCancellationRequested();

                    Logger.Instance.InfoFormat("Loading Node Information for DC \"{0}\"", dataCenter.Name);

                    foreach (var node in dataCenter.Nodes)
                    {
                        this.CancellationToken.ThrowIfCancellationRequested();

                        dataRow = this.Table.NewRow();

                        dataRow.SetField(ColumnNames.DataCenter, dataCenter.Name);
                        dataRow.SetField(ColumnNames.NodeIPAddress, node.Id.NodeName());
                        dataRow.SetField("Rack", node.DSE.Rack);
                        dataRow.SetField("Status", node.DSE.Statuses.ToString());
                        dataRow.SetField("Instance Type", node.DSE.InstanceType.ToString());
                        dataRow.SetField("Cluster Name", node.Cluster.Name);
                        dataRow.SetFieldToDecimal("Storage Used (MB)", node.DSE.StorageUsed, DSEDiagnosticLibrary.UnitOfMeasure.Types.MiB)
                                .SetFieldToDecimal("Storage Utilization", node.DSE.StorageUtilization, DSEDiagnosticLibrary.UnitOfMeasure.Types.Unknown, true);
                        dataRow.SetField("Health Rating", node.DSE.HealthRating);

                        if (node.DSE.NodeToolDateRange != null)
                        {
                            dataRow.SetField("Start NodeTool Range", node.DSE.NodeToolDateRange.Min.DateTime);
                            dataRow.SetField("End NodeTool Range", node.DSE.NodeToolDateRange.Max.DateTime);

                            if(node.DSE.NodeToolDateRange.Min.Offset == node.DSE.NodeToolDateRange.Max.Offset)
                            {
                                dataRow.SetField("Time Zone Offset", node.DSE.NodeToolDateRange.Max.Offset);
                            }
                            else
                            {
                                dataRow.SetField("Time Zone Offset", string.Format(@"{0}{1:hh\:mm}\{2}{3:hh\:mm}",
                                                                                    node.DSE.NodeToolDateRange.Min.Offset < TimeSpan.Zero ? "-" : "+,",
                                                                                    node.DSE.NodeToolDateRange.Min.Offset,
                                                                                    node.DSE.NodeToolDateRange.Max.Offset < TimeSpan.Zero ? "-" : "+,",
                                                                                    node.DSE.NodeToolDateRange.Max.Offset));
                            }
                        }

                        dataRow.SetFieldToTimeSpan("Uptime (Days)", node.DSE.Uptime)
                                .SetFieldToTimeSpan("Uptime", node.DSE.Uptime, @"d\ hh\:mm");

                        {
                            TimeSpan minTzOffset = TimeSpan.Zero;
                            TimeSpan maxTZOffset = TimeSpan.Zero;
                            var systemLogFiles = node.LogFiles
                                                       .Where(l => l.LogFile.Name.IndexOf("debug", StringComparison.OrdinalIgnoreCase) < 0);
                            var debugLogFiles = node.LogFiles
                                                        .Where(l => l.LogFile.Name.IndexOf("debug", StringComparison.OrdinalIgnoreCase) >= 0);

                            var systemLogEntries = systemLogFiles;

                            if (systemLogEntries.HasAtLeastOneElement())
                            {
                                var systemMaxLogTS = systemLogEntries.Max(l => l.LogDateRange.Max);
                                var systemMinLogTS = systemLogEntries.Min(l => l.LogDateRange.Min);
                                var systemDuration = TimeSpan.FromSeconds(systemLogEntries.Sum(l => l.LogDateRange.TimeSpan().TotalSeconds));

                                dataRow.SetField("Log Min Timestamp", systemMinLogTS.DateTime);
                                dataRow.SetField("Log Max Timestamp", systemMaxLogTS.DateTime);
                                dataRow.SetField("Log Duration", systemMaxLogTS - systemMinLogTS);
                                dataRow.SetField("Log Timespan Difference", TimeSpan.FromSeconds(Math.Abs((systemMaxLogTS - systemMinLogTS).TotalSeconds - systemDuration.TotalSeconds)));

                                minTzOffset = systemMinLogTS.Offset;
                                maxTZOffset = systemMaxLogTS.Offset;
                            }
                            dataRow.SetField("Log Nbr Files", systemLogEntries.Count());

                            var debugLogEntries = debugLogFiles;

                            if (debugLogEntries.HasAtLeastOneElement())
                            {
                                var debugMaxLogTS = debugLogEntries.Max(l => l.LogDateRange.Max);
                                var debugMinLogTS = debugLogEntries.Min(l => l.LogDateRange.Min);
                                var debugDuration = TimeSpan.FromSeconds(debugLogEntries.Sum(l => l.LogDateRange.TimeSpan().TotalSeconds));

                                dataRow.SetField("Debug Log Min Timestamp", debugMinLogTS.DateTime);
                                dataRow.SetField("Debug Log Max Timestamp", debugMaxLogTS.DateTime);
                                dataRow.SetField("Debug Log Duration", debugMaxLogTS - debugMinLogTS);
                                dataRow.SetField("Debug Log Timespan Difference", TimeSpan.FromSeconds(Math.Abs((debugMaxLogTS - debugMinLogTS).TotalSeconds - debugDuration.TotalSeconds)));

                                if(debugMaxLogTS.Offset != maxTZOffset)
                                    maxTZOffset = debugMaxLogTS.Offset;
                                if (debugMinLogTS.Offset != minTzOffset)
                                    minTzOffset = debugMinLogTS.Offset;

                            }
                            dataRow.SetField("Debug Log Nbr Files", debugLogEntries.Count());

                            if (minTzOffset == maxTZOffset)
                            {
                                dataRow.SetField("Time Zone Offset", maxTZOffset);
                            }
                            else
                            {
                                dataRow.SetField("Time Zone Offset", string.Format(@"{0}{1:hh\:mm}\{2}{3:hh\:mm}",
                                                                                    minTzOffset < TimeSpan.Zero ? "-" : "+,",
                                                                                    minTzOffset,
                                                                                    maxTZOffset < TimeSpan.Zero ? "-" : "+,",
                                                                                    maxTZOffset));
                            }
                        }

                        if (node.DSE.HeapUsed != null || node.DSE.Heap != null)
                        {
                            var used = node.DSE.HeapUsed == null || node.DSE.HeapUsed.NaN ? "?" : node.DSE.HeapUsed.ConvertTo(DSEDiagnosticLibrary.UnitOfMeasure.Types.MiB).ToString();
                            var size = node.DSE.Heap == null || node.DSE.Heap.NaN ? "?" : node.DSE.Heap.ConvertTo(DSEDiagnosticLibrary.UnitOfMeasure.Types.MiB).ToString();
                            dataRow.SetField("Heap Memory (MB)", used + '/' + size);
                        }
                        dataRow.SetFieldToDecimal("Off Heap Memory (MB)", node.DSE.OffHeap, DSEDiagnosticLibrary.UnitOfMeasure.Types.MiB);
                        if(node.DSE.NbrTokens.HasValue) dataRow.SetField("Nbr VNodes", (int) node.DSE.NbrTokens.Value);
                        if(node.DSE.NbrExceptions.HasValue) dataRow.SetField("Nbr of Exceptions", (int) node.DSE.NbrExceptions.Value);
                        dataRow.SetFieldToDecimal("Percent Repaired", node.DSE.RepairedPercent);
                        if(node.DSE.RepairServiceHasRan.HasValue) dataRow.SetField("Repair Service Enabled", node.DSE.RepairServiceHasRan.HasValue);
                        if(node.DSE.GossipEnabled.HasValue) dataRow.SetField("Gossip Enabled", node.DSE.GossipEnabled.Value);
                        if (node.DSE.ThriftEnabled.HasValue) dataRow.SetField("Thrift Enabled", node.DSE.ThriftEnabled.Value);
                        if (node.DSE.NativeTransportEnabled.HasValue) dataRow.SetField("Native Transport Enabled", node.DSE.NativeTransportEnabled.Value);
                        dataRow.SetField("Key Cache Information", node.DSE.KeyCacheInformation);
                        dataRow.SetField("Row Cache Information", node.DSE.RowCacheInformation);
                        dataRow.SetField("Counter Cache Information", node.DSE.CounterCacheInformation);
                        if (node.DSE.IsSeedNode.HasValue) dataRow.SetField("Seed Node", node.DSE.IsSeedNode.Value);

                        this.Table.Rows.Add(dataRow);
                        ++nbrItems;
                    }

                    Logger.Instance.InfoFormat("Loaded Node Information for DC \"{0}\", Total Nbr Items {1:###,###,##0}", dataCenter.Name, nbrItems);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Instance.Warn("Loading Node Information Canceled");
            }
            finally
            {
                this.Table.AcceptChanges();
                this.Table.EndLoadData();
            }

            return this.Table;
        }
    }
}
