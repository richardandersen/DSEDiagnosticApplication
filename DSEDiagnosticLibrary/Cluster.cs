﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Common;
using Common.Patterns;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CTS = Common.Patterns.Collections.ThreadSafe;
using IMMLogValue = Common.Patterns.Collections.MemoryMapped.IMMValue<DSEDiagnosticLibrary.ILogEvent>;

namespace DSEDiagnosticLibrary
{
    [JsonObject(MemberSerialization.OptOut)]
	public class Cluster : IEquatable<Cluster>, IEquatable<string>
	{
		public static readonly Cluster MasterCluster = null;
        public static CTS.List<Cluster> Clusters = new CTS.List<Cluster>();
		private static CTS.List<INode> UnAssociatedNodes = new CTS.List<INode>();

        #region constructors

        static Cluster()
		{
            MasterCluster = new Cluster("**LogicalMaster**") { IsMaster = true };
		}

        [JsonObject(MemberSerialization.OptOut)]
        public sealed class OpsCenterInfo
        {
            public Version Version;
            public bool? RepairServiceEnabled;
        }

        public Cluster()
		{
			Clusters.Add(this);
            this.OpsCenter = new OpsCenterInfo();
		}

		public Cluster(string clusterName)
			: this()
		{
			this.Name = StringHelpers.RemoveQuotes(clusterName);
		}

        #endregion

        #region Members

        public string Name { get; }
        public bool IsMaster { get; private set; }

        [JsonProperty(PropertyName="DataCenters")]
        private IEnumerable<IDataCenter> datamemberDataCenters
        {
            get { return this._dataCenters.UnSafe; }
            set { this._dataCenters = new CTS.List<IDataCenter>(value); }
        }
        private CTS.List<IDataCenter> _dataCenters = new CTS.List<IDataCenter>();
        [JsonIgnore]
        public IEnumerable<IDataCenter> DataCenters { get { return this._dataCenters; } }

        [JsonIgnore]
        public IEnumerable<IMMLogValue> LogEvents
        {
            get
            {
                return this.DataCenters.SelectMany(d => d.LogEvents);
            }
        }

        [JsonProperty(PropertyName = "AggregatedStats")]
        private IEnumerable<IAggregatedStats> datamemberAggregatedStats
        {
            get { return this._aggregatedStats.UnSafe; }
            set { this._aggregatedStats = new CTS.List<IAggregatedStats>(value); }
        }

        private CTS.List<IAggregatedStats> _aggregatedStats = new CTS.List<IAggregatedStats>();
        [JsonIgnore]
        public IEnumerable<IAggregatedStats> AggregatedStats { get { return this._aggregatedStats; } }
        public Cluster AssociateItem(IAggregatedStats aggregatedStat)
        {
            this._aggregatedStats.Add(aggregatedStat);
            return this;
        }

        [JsonProperty(PropertyName = "Keyspaces")]
        private IEnumerable<IKeyspace> datamemberKeyspaces
        {
            get { return this._keySpaces.UnSafe; }
            set { this._keySpaces = new CTS.List<IKeyspace>(value); }
        }
        private CTS.List<IKeyspace> _keySpaces = new CTS.List<IKeyspace>();
        [JsonIgnore]
        public IEnumerable<IKeyspace> Keyspaces
        {
            get
            {
                if(this.IsMaster)
                {
                    return this.UnderlyingCluster.SelectMany(c => c.Keyspaces);
                }
                return this._keySpaces;
            }
        }

        [JsonIgnore]
        public IEnumerable<Cluster> UnderlyingCluster
        {
            get
            {
                if(this.IsMaster)
                {
                    return Clusters.Where(c => !c.IsMaster);
                }

                return Enumerable.Empty<Cluster>();
            }
        }

        public IEnumerable<IKeyspace> GetKeyspaces(IDataCenter datacenter)
        {
            this._keySpaces.Lock();
            try
            {
                return this._keySpaces.UnSafe.Where(k => k.DataCenters.Contains(datacenter));
            }
            finally
            {
                this._keySpaces.UnLock();
            }
        }

        public bool RemoveKeyspace(IKeyspace keySpace)
        {
            return this._keySpaces.Remove(keySpace);
        }

        public IEnumerable<IKeyspace> GetKeyspaces(string keyspaceName)
        {
            keyspaceName = keyspaceName == null ? null : StringHelpers.RemoveQuotes(keyspaceName.Trim());

            this._keySpaces.Lock();
            try
            {
                return string.IsNullOrEmpty(keyspaceName) ? this._keySpaces : this._keySpaces.UnSafe.Where(k => k.Name == keyspaceName);
            }
            finally
            {
                this._keySpaces.UnLock();
            }
        }

        [JsonIgnore]
        public IEnumerable<INode> Nodes
        {
            get
            {
                return this.DataCenters.SelectMany(dc => dc.Nodes);
            }
        }

        public OpsCenterInfo OpsCenter { get; }

		public IEvent AssociateItem(IEvent eventItem)
		{

			return eventItem;
		}

        public IKeyspace AssociateItem(IKeyspace ksItem)
        {
            this._keySpaces.Lock();
            try
            {
                var existingKS = this._keySpaces.UnSafe.FirstOrDefault(ks => ks.Equals(ksItem));

                if (existingKS == null)
                {
                    this._keySpaces.UnSafe.Add(ksItem);
                }
                else
                {
                    if (existingKS.IsPreLoaded && !ksItem.IsPreLoaded)
                    {
                        this.RemoveKeyspace(existingKS);
                        this._keySpaces.UnSafe.Add(ksItem);
                    }
                    else
                    {
                        ksItem = existingKS;
                    }
                }
            }
            finally
            {
                this._keySpaces.UnLock();
            }

            return ksItem;
        }

        public IDataCenter TryGetDataCenter(string dataCenterName)
        {
            this._dataCenters.Lock();
            try
            {
                return string.IsNullOrEmpty(dataCenterName) ? null : this._dataCenters.UnSafe.FirstOrDefault(d => d.Name == dataCenterName);
            }
            finally
            {
                this._dataCenters.UnLock();
            }
        }

        public INode TryGetNode(string nodeId)
        {
            this._dataCenters.Lock();
            try
            {
                return this._dataCenters.UnSafe.SelectMany(dc => dc.Nodes).FirstOrDefault(n => n.Equals(nodeId));
            }
            finally
            {
                this._dataCenters.UnLock();
            }
        }

        public INode TryGetNode(NodeIdentifier nodeId)
        {
            this._dataCenters.Lock();
            try
            {
                return this._dataCenters.UnSafe.SelectMany(dc => dc.Nodes).FirstOrDefault(n => n.Equals(nodeId));
            }
            finally
            {
                this._dataCenters.UnLock();
            }
        }

        public object ToDump()
        {
            return new { Name = this.Name, DataCenters = this.DataCenters, Keyspaces = this.Keyspaces };
        }

        #endregion

        #region Static Methods

        public static Cluster GetCurrentOrMaster()
        {
            if(Clusters.Count == 1)
            {
                return Clusters.First();
            }

            return MasterCluster;
        }

        public static Cluster TryGetAddCluster(string clusterName)
		{
            Clusters.Lock();
            try
            {
                clusterName = StringHelpers.RemoveQuotes(clusterName.Trim());

                var cluster = Clusters.UnSafe.FirstOrDefault(c => c.Name == clusterName);

                if (cluster == null)
                {
                    cluster = new Cluster(clusterName);
                }

                return cluster;
            }
            finally
            {
                Clusters.UnLock();
            }
		}

        public static Cluster TryGetCluster(string clusterName)
        {
            Clusters.Lock();
            try
            {
                clusterName = clusterName == null ? null : StringHelpers.RemoveQuotes(clusterName.Trim());
                return Clusters.UnSafe.FirstOrDefault(c => c.Name == clusterName);
            }
            finally
            {
                Clusters.UnLock();
            }
        }

        public static Cluster TryGetCluster(int clusterHashCode)
        {
            Clusters.Lock();
            try
            {
                return Clusters.UnSafe.FirstOrDefault(c => c.GetHashCode() == clusterHashCode);
            }
            finally
            {
                Clusters.UnLock();
            }
        }

        public static IDataCenter TryGetAddDataCenter(string dataCenterName, Cluster cluster = null)
		{
            var currentCuster = cluster == null ? MasterCluster : cluster;

            currentCuster._dataCenters.Lock();
            try
            {
                var currentDC = currentCuster._dataCenters.UnSafe.FirstOrDefault(d => d.Name == dataCenterName);

                if (currentDC == null)
                {
                    currentDC = new DataCenter(currentCuster, dataCenterName);
                    currentCuster._dataCenters.UnSafe.Add(currentDC);
                }

                return currentDC;
            }
            finally
            {
                currentCuster._dataCenters.UnLock();
            }

		}
		public static IDataCenter TryGetAddDataCenter(string dataCenterName, string clusterName = null)
		{
			var currentCuster = string.IsNullOrEmpty(clusterName) ? MasterCluster : TryGetAddCluster(clusterName);

			return TryGetAddDataCenter(dataCenterName, currentCuster);
		}

        public static IDataCenter TryGetDataCenter(string dataCenterName, Cluster cluster)
        {
            if (string.IsNullOrEmpty(dataCenterName)) return null;

            var currentCuster = cluster == null ? MasterCluster : cluster;

            return currentCuster.TryGetDataCenter(dataCenterName);
        }
        public static IDataCenter TryGetDataCenter(string dataCenterName, string clusterName = null)
        {
            if (string.IsNullOrEmpty(dataCenterName)) return null;

            var currentCuster = string.IsNullOrEmpty(clusterName) ? MasterCluster : TryGetCluster(clusterName);

            return TryGetDataCenter(dataCenterName, currentCuster);
        }

        public static IDataCenter TryGetDataCenter(int dataCenterHashCode)
        {
            IDataCenter dataCenter = null;

           foreach (var currentCuster in Clusters)
           {
                currentCuster._dataCenters.Lock();
                try
                {
                    dataCenter = currentCuster._dataCenters.UnSafe.FirstOrDefault(d => d.GetHashCode() == dataCenterHashCode);

                    if (dataCenter != null)
                    {
                        break;
                    }
                }
                finally
                {
                    currentCuster._dataCenters.UnLock();
                }
            }

            return dataCenter;
        }

        public static INode TryGetAddNode(string nodeId, string dataCenterName = null, string clusterName = null)
		{
			if (!NodeIdentifier.ValidNodeIdName(nodeId))
			{ return null; }

			var currentDC = (DataCenter)((string.IsNullOrEmpty(dataCenterName) ? null : TryGetAddDataCenter(dataCenterName, clusterName)));

			if (currentDC == null)
			{
                UnAssociatedNodes.Lock();
                try
                {
                    var node = UnAssociatedNodes.UnSafe.FirstOrDefault(n => n.Equals(nodeId));

                    if (node == null)
                    {
                        var cluster = string.IsNullOrEmpty(clusterName) ? MasterCluster : TryGetAddCluster(clusterName);

                        node = MasterCluster.TryGetNode(nodeId);

                        if (node == null)
                        {
                            node = new Node(cluster, nodeId);
                            UnAssociatedNodes.UnSafe.Add(node);
                        }
                    }

                    return node;
                }
                finally
                {
                    UnAssociatedNodes.UnLock();
                }
			}

			return currentDC.TryGetAddNode(nodeId);
		}

        public static INode TryGetAddNode(string nodeId, IDataCenter dataCenter)
        {
            if (!NodeIdentifier.ValidNodeIdName(nodeId))
            { return null; }

            return ((DataCenter)dataCenter)?.TryGetAddNode(nodeId);
        }

        public static INode TryGetAddNode(NodeIdentifier nodeId, string dataCenterName = null, string clusterName = null)
		{
			if (nodeId == null)
			{ return null; }

			var currentDC = (DataCenter)((string.IsNullOrEmpty(dataCenterName) ? null : TryGetAddDataCenter(dataCenterName, clusterName)));

			if (currentDC == null)
			{
                UnAssociatedNodes.Lock();
                try
                {
                    var node = UnAssociatedNodes.UnSafe.FirstOrDefault(n => n.Id == nodeId);

                    if (node == null)
                    {
                        var cluster = string.IsNullOrEmpty(clusterName) ? MasterCluster : TryGetAddCluster(clusterName);

                        node = cluster.TryGetNode(nodeId);

                        if (node == null)
                        {
                            node = new Node(cluster, nodeId);
                            UnAssociatedNodes.UnSafe.Add(node);
                        }
                    }

                    return node;
                }
                finally
                {
                    UnAssociatedNodes.UnLock();
                }
			}

			return currentDC.TryGetAddNode(nodeId);
		}

        public static INode TryGetNode(string nodeId, string dataCenterName = null, string clusterName = null, bool includeUnAssociatedNodes = true)
        {
            var dcNodes = GetNodes(dataCenterName, clusterName, includeUnAssociatedNodes);

            if(dcNodes != null)
            {
                return dcNodes.FirstOrDefault(n => n.Id.Equals(nodeId));
            }

            return null;
        }

        public static INode TryGetNode(int nodeHashCode)
        {
            INode node = null;
            foreach (var currentCluster in Clusters)
            {
                foreach (var currentDC in currentCluster.DataCenters)
                {
                    node = currentDC.TryGetNode(nodeHashCode);

                    if (node != null) return node;
                }
            }

            return null;
        }

        public static IEnumerable<INode> GetNodes(string dataCenterName, string clusterName = null, bool includeUnAssociatedNodes = true)
        {
            if(!string.IsNullOrEmpty(dataCenterName))
            {
                var dcInstance = TryGetDataCenter(dataCenterName, clusterName);
                return dcInstance == null ? Enumerable.Empty<INode>() : dcInstance.Nodes;
            }

            var cluster = TryGetCluster(clusterName);
            var clusterNodes = cluster?.DataCenters?.SelectMany(dc => dc.Nodes);

            if(includeUnAssociatedNodes)
            {
                if(clusterNodes == null)
                {
                    return UnAssociatedNodes.ToList();
                }

                var listNodes = clusterNodes.ToList();

                listNodes.AddRange(UnAssociatedNodes);

                return listNodes;
            }

            return clusterNodes == null ? Enumerable.Empty<INode>() : clusterNodes;
        }

        public static IEnumerable<INode> GetNodes(bool includeUnAssociatedNodes = true)
        {
            var clusterNodes = Cluster.MasterCluster.DataCenters.SelectMany(dc => dc.Nodes);

            if (includeUnAssociatedNodes)
            {
                if (clusterNodes == null)
                {
                    return UnAssociatedNodes.ToList();
                }

                var listNodes = clusterNodes.ToList();

                listNodes.AddRange(UnAssociatedNodes);

                return listNodes;
            }

            return clusterNodes;
        }

        public static INode AssociateDataCenterToNode(string dataCenterName, string nodeId, string clusterName = null)
		{
			if (string.IsNullOrEmpty(nodeId))
			{ throw new ArgumentNullException("nodeId cannot be null"); }
			if(string.IsNullOrEmpty(dataCenterName))
			{ throw new ArgumentNullException("dataCenter cannot be null"); }

			var currentDC = (DataCenter) TryGetAddDataCenter(dataCenterName, clusterName);

            UnAssociatedNodes.Lock();
            try
            {
                var node = (Node)UnAssociatedNodes.UnSafe.FirstOrDefault(n => n.Equals(nodeId));

                if (node == null)
                {
                    return currentDC.TryGetAddNode(nodeId);
                }

                UnAssociatedNodes.UnSafe.Remove(node);

                return currentDC.TryGetAddNode(node);
            }
            finally
            {
                UnAssociatedNodes.UnLock();
            }
		}

        public static INode AssociateDataCenterToNode(string dataCenterName, INode node)
        {
            if (node == null)
            { throw new ArgumentNullException("node cannot be null"); }
            if (string.IsNullOrEmpty(dataCenterName))
            { throw new ArgumentNullException("dataCenter cannot be null"); }

            var currentDC = (DataCenter)TryGetAddDataCenter(dataCenterName, node.Cluster);

            UnAssociatedNodes.Lock();
            try
            {
                var removeAssocNode = (Node)UnAssociatedNodes.UnSafe.FirstOrDefault(n => n.Equals(node));

                if (currentDC != null && removeAssocNode != null)
                {
                    UnAssociatedNodes.UnSafe.Remove(node);
                }

                return currentDC?.TryGetAddNode(node);
            }
            finally
            {
                UnAssociatedNodes.UnLock();
            }

        }

        /// <summary>
        /// This will either name the associated cluster (if name is null) or create a new cluster and associate this node and it&apos;s associated DC to the new cluster
        /// </summary>
        /// <param name="node"></param>
        /// <param name="clusterName"></param>
        /// <returns></returns>
        public static Cluster NameClusterAssociatewItem(INode node, string clusterName)
        {
            if(node != null && !string.IsNullOrEmpty(clusterName) && node.DataCenter == null)
            {
                var cluster = TryGetAddCluster(clusterName);

                lock (node)
                {
                    if (cluster != null && node.Cluster.Name != clusterName && node.DataCenter == null)
                    {
                        ((Node)node).SetCluster(cluster);
                        return cluster;
                    }
                }
            }

            return NameClusterAssociatewItem(node?.DataCenter, clusterName);
        }

        /// <summary>
        /// This will either name the associated cluster (if name is null) or create a new cluster and associated DC to the new cluster
        /// </summary>
        /// <param name="node"></param>
        /// <param name="clusterName"></param>
        /// <returns></returns>
        public static Cluster NameClusterAssociatewItem(IDataCenter dc, string clusterName)
        {
            if (dc == null || string.IsNullOrEmpty(clusterName)) return null;

            var cluster = TryGetAddCluster(clusterName);

            lock (dc)
            {
                if (cluster != null && dc.Cluster.Name != clusterName)
                {
                    var oldCluster = dc.Cluster;

                    cluster._dataCenters.Lock();
                    oldCluster._dataCenters.Lock();
                    try
                    {
                        if (!cluster._dataCenters.UnSafe.Contains(dc))
                        {
                            cluster._dataCenters.UnSafe.Add(dc);
                            if (!oldCluster.IsMaster)
                            {
                                oldCluster._dataCenters.UnSafe.Remove(dc);
                            }
                        }

                        ((DataCenter)dc).AssociateItem(cluster);
                    }
                    finally
                    {
                        oldCluster._dataCenters.UnLock();
                        cluster._dataCenters.UnLock();
                    }
                }
            }

            return cluster;
        }

        public static IKeyspace TryGetKeySpace(int keyspaceHashCode)
        {
            IKeyspace keyspace = null;

            foreach (var currentCluster in Clusters)
            {
                foreach (var currentDC in currentCluster.DataCenters)
                {
                    keyspace = currentDC.TryGetKeyspace(keyspaceHashCode);

                    if (keyspace != null) return keyspace;
                }
            }

            return null;
        }

        public static IDDLStmt TryGetDDLStmt(int ddlHashCode)
        {
            IDDLStmt ddl = null;

            foreach (var currentCluster in Clusters)
            {
                foreach (var currentDC in currentCluster.DataCenters)
                {
                   foreach (var currentKS in currentDC.Keyspaces)
                   {
                        ddl = currentKS.TryGetDDL(ddlHashCode);
                        if (ddl != null) return ddl;
                   }
                }
            }

            return null;
        }

        /// <summary>
        /// Tries to find a Table, View, or Index based on either a SSTable file path or a keyspace.&lt;item name&gt; where item is a table, view, or index name.
        /// </summary>
        /// <param name="SSTableOrKSItemName">
        /// a SSTable file path or a keyspace.&lt;item name&gt; where item is a table, view, or index name
        /// </param>
        /// <param name="cluster"></param>
        /// <param name="dc"></param>
        /// <param name="defaultKSName">
        /// only used if the keyspace name is not found. Ignored for SSTable file
        /// </param>
        /// <returns>
        /// Returns null if not found, multiple names found, SSTableOrKSItemName and/or cluster is null. Otherwise a Table, view, index instance is returned.
        /// </returns>
        public static IDDLStmt TryGetTableIndexViewbyString(string SSTableOrKSItemName, Cluster cluster, IDataCenter dc = null, string defaultKSName = null)
        {
            if(string.IsNullOrEmpty(SSTableOrKSItemName) || cluster == null)
            {
                return null;
            }

            var tableviewInstances = TryGetTableIndexViewsbyString(SSTableOrKSItemName, cluster, dc, defaultKSName);

            if(tableviewInstances.Count() <= 1)
            {
                return tableviewInstances.FirstOrDefault();
            }

            return null;
        }

        public static IDDLStmt TryGetTableIndexViewbyString(string SSTableOrKSItemName, string clusterName, string dcName = null, string defaultKSName = null)
        {
            var cluster = Cluster.TryGetCluster(clusterName);
            return TryGetTableIndexViewbyString(SSTableOrKSItemName, cluster, cluster?.TryGetDataCenter(dcName), defaultKSName);
        }

        public static IEnumerable<IDDLStmt> TryGetTableIndexViewsbyString(string SSTableOrKSItemName, Cluster cluster, IDataCenter dc = null, string defaultKSName = null)
        {
            if (string.IsNullOrEmpty(SSTableOrKSItemName) || cluster == null)
            {
                return Enumerable.Empty<IDDLStmt>();
            }

            IEnumerable<IKeyspace> ksInstances;

            if (SSTableOrKSItemName[0] == '/')
            {
                var kstblTuple = StringHelpers.ParseSSTableFileIntoKSTableNames(SSTableOrKSItemName);

                if (kstblTuple != null)
                {
                    defaultKSName = kstblTuple.Item1 ?? defaultKSName;
                    ksInstances = dc == null
                                    ? cluster.GetKeyspaces(defaultKSName)
                                    : (defaultKSName == null
                                            ? cluster.GetKeyspaces(dc)
                                            : cluster.GetKeyspaces(dc).Where(ks => ks.Equals(defaultKSName)));

                    var tableviewInstances = ksInstances.Select(k => (IDDLStmt)k.TryGetTable(kstblTuple.Item2)
                                                                ?? (IDDLStmt)k.TryGetView(kstblTuple.Item2)
                                                                ?? (IDDLStmt)k.TryGetIndex(kstblTuple.Item2))
                                                .Where(d => d != null);

                    if (tableviewInstances.Count() <= 1)
                    {
                        return tableviewInstances;
                    }

                    var tableviewInstance = kstblTuple.Item3 == null
                                            ? null
                                            : tableviewInstances.FirstOrDefault(t => t is ICQLTable && ((ICQLTable)t).Id.ToString("N") == kstblTuple.Item3);

                    if (tableviewInstance == null)
                    {
                        return tableviewInstances;
                    }

                    return new IDDLStmt[] { tableviewInstance };
                }
            }

            var parsedName = StringHelpers.SplitTableName(SSTableOrKSItemName);
            defaultKSName = parsedName.Item1 ?? defaultKSName;
            ksInstances = dc == null
                            ? cluster.GetKeyspaces(defaultKSName)
                            : (defaultKSName == null
                                    ? cluster.GetKeyspaces(dc)
                                    : cluster.GetKeyspaces(dc).Where(ks => ks.Equals(defaultKSName)));

            return ksInstances.Select(k => ((IDDLStmt)k.TryGetTable(parsedName.Item2)
                                                ?? (IDDLStmt)k.TryGetView(parsedName.Item2))
                                                ?? (IDDLStmt)k.TryGetIndex(parsedName.Item2))
                        .Where(d => d != null);
        }

        public static IEnumerable<IDDLStmt> TryGetTableIndexViewsbyString(string SSTableOrKSItemName, string clusterName, string dcName = null, string defaultKSName = null)
        {
            var cluster = Cluster.TryGetCluster(clusterName);
            return TryGetTableIndexViewsbyString(SSTableOrKSItemName, cluster, cluster?.TryGetDataCenter(dcName), defaultKSName);
        }

        /// <summary>
        /// This will return an IDDLSmt based on a Solr Log name like &quot;coafstatim_application_appownrtxt_index&quot;.
        /// </summary>
        /// <param name="solrIndexName"></param>
        /// <param name="cluster"></param>
        /// <param name="dc"></param>
        /// <param name="defaultKSName"></param>
        /// <returns></returns>
        public static IDDLStmt TryGetSolrIndexbyString(string solrIndexName, Cluster cluster, IDataCenter dc = null, string defaultKSName = null)
        {
            if (string.IsNullOrEmpty(solrIndexName) || cluster == null)
            {
                return null;
            }

            var ksInstances = dc == null
                               ? cluster.GetKeyspaces(defaultKSName)
                               : (defaultKSName == null
                                       ? cluster.GetKeyspaces(dc)
                                       : cluster.GetKeyspaces(dc).Where(ks => ks.Equals(defaultKSName)));

            var possibleKSInstance = ksInstances.Where(k => solrIndexName.StartsWith(k.Name))
                                        .OrderByDescending(k => k.Name.Length)
                                        .FirstOrDefault();

            if(possibleKSInstance == null)
            {
                //We have to do a Scan
                var possibleIndex = ksInstances.Select(ks => ks.TryGetIndex(solrIndexName)).Where(i => i != null && i.IsSolr).FirstOrDefault();

                return possibleIndex;
            }

            return possibleKSInstance.TryGetIndex(solrIndexName);
        }

        public static void Clear()
        {
            Clusters.Clear();
            UnAssociatedNodes.Clear();
            MasterCluster._keySpaces.Clear();
            MasterCluster._dataCenters.Clear();
        }

        internal static INode FindNodeInUnAssocaitedNodes(string nodeId)
        {
            return UnAssociatedNodes.FirstOrDefault(n => n.Id.Equals(nodeId));
        }
        internal static INode FindNodeInUnAssocaitedNodes(NodeIdentifier nodeId)
        {
            return UnAssociatedNodes.FirstOrDefault(n => n.Id.Equals(nodeId));
        }

        /// <summary>
        /// Nodes that are not associated to a data center.
        ///
        /// Note that this is will changed based on processing since a node can become associated anytime during the process.
        /// </summary>
        /// <returns></returns>
        static public IEnumerable<INode> GetUnAssocaitedNodes()
        {
            return UnAssociatedNodes;
        }

        #endregion

        #region IEquatable
        public bool Equals(Cluster other)
		{
			if (ReferenceEquals(this, other))
			{ return true; }

			return this.Name == other.Name;
		}

		public bool Equals(string other)
		{
			return this.Name == other;
		}
		#endregion

		#region Object Overrides
		public override string ToString()
		{
			return string.Format("Cluster{{Name=\"{0}\", DataCenters={{{1}}}}}. Keyspaces={{{2}}}",
									this.Name,
									this.DataCenters == null
										? string.Empty
										: string.Join(",", this.DataCenters.Select(i => i.Name)),
                                    string.Join(",", this.Keyspaces.Select(i => i.Name)));
		}

		public override bool Equals(object obj)
		{
			if(obj is Cluster)
			{
				return this.Equals((Cluster)obj);
			}
			if(obj is string)
			{
				return this.Equals((string)obj);
			}
			return false;
		}

        [JsonProperty(PropertyName="HashCode")]
        private int _hashcode = 0;
		public override int GetHashCode()
		{
            if (this._hashcode != 0) return this._hashcode;

			return string.IsNullOrEmpty(this.Name) ? 0 : this._hashcode = this.Name.GetHashCode();
		}

		#endregion
	}
}
