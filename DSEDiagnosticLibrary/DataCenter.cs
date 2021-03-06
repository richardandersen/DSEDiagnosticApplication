﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Common;
using Common.Patterns;
using System.Threading.Tasks;
using Common.Patterns.Tasks;
using CTS = Common.Patterns.Collections.ThreadSafe;
using IMMLogValue = Common.Patterns.Collections.MemoryMapped.IMMValue<DSEDiagnosticLibrary.ILogEvent>;

namespace DSEDiagnosticLibrary
{
    public interface IDataCenter : IEquatable<IDataCenter>, IEquatable<string>
    {
        Cluster Cluster { get; }
        string Name { get; }

        IConfigurationLine ConfigurationMatch(string property, string value, ConfigTypes type);

        IEnumerable<INode> Nodes { get; }
        IEnumerable<IKeyspace> Keyspaces { get; }
      
        IKeyspace TryGetKeyspace(string ksName);
        IKeyspace TryGetKeyspace(int keyspaceHashCode);

		INode TryGetNode(string nodeId);
        INode TryGetNode(int nodeHashCode);
        INode TryGetAddNode(INode node);

        /// <summary>
        /// Set the host name to the node only if the name doesn't already exist within the within the DC in a different node.
        /// </summary>
        /// <param name="nodeSettingName"></param>
        /// <param name="hostName"></param>  
        /// <param name="tryParseAsIPAddress"></param>
        /// <returns>
        /// First  -- true to indicate the name was set or false that the name already exists within the DC
        /// Second -- if true the node that was set, if false the node that contained the host name.
        /// </returns>
        Tuple<bool,INode> SetNodeHostName(INode nodeSettingName, string hostName, bool tryParseAsIPAddress = false);

        IEnumerable<IMMLogValue> LogEvents { get; }
		IEnumerable<IConfigurationLine> Configurations { get; }
        IEnumerable<IConfigurationLine> GetConfigurations(INode node);

        IEnumerable<IAggregatedStats> AggregatedStats { get; }
        IDataCenter AssociateItem(IAggregatedStats aggregatedStat);

        /// <summary>
        /// This will return the timezone for this datacenter based on the major of nodes&apos; timezone. If null, the cluster&apos;s default timezone is used.
        /// </summary>
        Common.Patterns.TimeZoneInfo.IZone DefaultMajorityTimeZone { get; }
        /// <summary>
        /// Returns a time zone only if all nodes in the DC have the same time zone. Otherwise null is returned.
        /// </summary>
        Common.Patterns.TimeZoneInfo.IZone TimeZone { get; }

        IEnumerable<IDDL> DDLs { get; }


        TimeSpan? NodeUpTimeAvg { get;  }
        TimeSpan? NodeUpTimeStdRange { get; }
        TimeSpan? LogSystemDurationAvg { get; }
        TimeSpan? LogSystemDurationStdRange { get; }
        TimeSpan? LogSystemGap { get; }
        int LogSystemFiles { get; }
        TimeSpan? LogDebugDurationAvg { get; }
        TimeSpan? LogDebugDurationStdRange { get; }
        TimeSpan? LogDebugGap { get; }
        int LogDebugFiles { get; }

        long ReadCount { get; }
        long WriteCount { get; }
        decimal ReadCountAvg { get; }
        decimal WriteCountAvg { get; }

        decimal DataQualityFactor();
        decimal DataQualityMaxFactor { get; }

        object ToDump();
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class PlaceholderDataCenter : IDataCenter
    {
        private readonly DataCenter _assocDC;

        protected PlaceholderDataCenter()
        { }

        internal PlaceholderDataCenter(DataCenter assocatedDC, string name, string className)
        {
            this._name = name;
            this._className = className;
            this._assocDC = assocatedDC;
        }
        
        internal INode AddNode(INode node)
        {
            if (node == null)
            { return null; }

            if (!this._nodes.Contains(node))
            {
                this._nodes.Add(node);
            }

            return node;
        }

        virtual public IConfigurationLine ConfigurationMatch(string property,
                                                                string value,
                                                                ConfigTypes type)
        {
            return this._assocDC.ConfigurationMatch(property, value, type);
        }

        public object ToDump()
        {
            return new { Name = this.Name,
                            Cluster = this.Cluster,
                            Nodes = this.Nodes,
                            Keyspaces = this.Keyspaces,
                            Configurations = this.Configurations,
                            LogEvents = this.LogEvents,
                            PlaceHolder=true};
        }

        #region IDataCenter
        virtual public Cluster Cluster
        {
            get { return this._assocDC.Cluster; }
            protected set { throw new NotImplementedException(); }
        }

        virtual public IEnumerable<IKeyspace> Keyspaces
        {
            get
            {
                return Enumerable.Empty<IKeyspace>();
            }
        }

        [JsonProperty(PropertyName="Name")]
        private string _name = null;
        [JsonProperty(PropertyName="ClassName")]
        private string _className = null;
        [JsonIgnore]
        virtual public string Name
        {
            get { return string.Format("{0}<{1}>", this._name, this._nodes.Count()); }
            protected set { throw new NotImplementedException(); }
        }

        [JsonProperty(PropertyName="Nodes")]
        IEnumerable<INode> datamemberNodes
        {
            get { return this._nodes.UnSafe; }
            set { this._nodes = new CTS.List<INode>(value); }
        }
        protected CTS.List<INode> _nodes = new CTS.List<INode>();
        [JsonIgnore]
        public IEnumerable<INode> Nodes
        {
            get { return this._nodes; }
        }

        virtual public IKeyspace TryGetKeyspace(string ksName)
        {
            throw new NotImplementedException();
        }

        virtual public IKeyspace TryGetKeyspace(int keyspaceHashCode)
        {
            throw new NotImplementedException();
        }

        public INode TryGetNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            { return null; }

            return this._nodes.FirstOrDefault(n => n.Equals(nodeId));
        }
        public INode TryGetNode(int nodeHashCode)
        {
            return this._nodes.FirstOrDefault(n => n.GetHashCode() == nodeHashCode);
        }
        virtual public INode TryGetAddNode(INode node)
        {
            throw new NotImplementedException();
        }

        virtual public IEnumerable<IConfigurationLine> GetConfigurations(INode node)
        {
            return this._assocDC.GetConfigurations(node);
        }

        public Tuple<bool,INode> SetNodeHostName(INode nodeSettingName, string hostName, bool tryParseAsIPAddress = false)
        {
            var existNode = this._nodes.Where(n => !n.Equals(nodeSettingName)).FirstOrDefault(n => n.Id.HostNameExists(hostName));

            if (existNode == null)
            {
                nodeSettingName.Id.SetIPAddressOrHostName(hostName, tryParseAsIPAddress);
                return new Tuple<bool,INode>(true, nodeSettingName);
            }

            return new Tuple<bool, INode>(false, existNode);
        }

        [JsonIgnore]
        virtual public IEnumerable<IMMLogValue> LogEvents { get { throw new NotImplementedException(); } }
        [JsonIgnore]
        virtual public IEnumerable<IConfigurationLine> Configurations { get { return this._assocDC.Configurations; } }
        [JsonIgnore]
        virtual public IEnumerable<IDDL> DDLs { get { throw new NotImplementedException(); } }

        [JsonIgnore]
        virtual public IEnumerable<IAggregatedStats> AggregatedStats { get { throw new NotImplementedException(); } }
        virtual public IDataCenter AssociateItem(IAggregatedStats aggregatedStat) { throw new NotImplementedException(); }

        /// <summary>
        /// Returns a time zone only if all nodes in the DC have the same time zone. Otherwise null is returned.
        /// </summary>
        [JsonIgnore]
        public Common.Patterns.TimeZoneInfo.IZone TimeZone
        {
            get
            {
                var tz = this._nodes.FirstOrDefault()?.Machine.ExplictTimeZone;
                return this._nodes.All(n => n.Machine.ExplictTimeZone == tz) ? tz : null;
            }
        }

        /// <summary>
        /// This will return the timezone for this datacenter based on the DC associated timezones, majority of nodes&apos; timezone, or the cluster&apos;s default timezone.
        /// This can return null.
        /// </summary>
        /// <seealso cref="DefaultAssocItemToTimeZone"/>
        /// <seealso cref="Cluster.DefaultTimeZone"/>
        [JsonIgnore]        
        public Common.Patterns.TimeZoneInfo.IZone DefaultMajorityTimeZone
        {
            get
            {
                Common.Patterns.TimeZoneInfo.IZone tz = null;

                if(DataCenter.DefaultTimeZones != null && DataCenter.DefaultTimeZones.Length > 0)
                {
                    tz = DataCenter.DefaultTimeZones.FindDefaultTimeZone(this.Name);
                }

                if (tz == null)
                {
                    tz = this.TimeZone;
                }

                if(tz == null)
                {
                    tz = StringHelpers.FindTimeZone(this._nodes.GroupBy(x => x.Machine.TimeZoneName)
                                                               .OrderByDescending(g => g.Count())
                                                               .First()
                                                               .Key);
                }

                if(tz == null)
                {
                    tz = this.Cluster?.DefaultTimeZone;
                }

                return tz;
            }
        }


        public TimeSpan? NodeUpTimeAvg { get; set; }
        public TimeSpan? NodeUpTimeStdRange { get; set; }
        public TimeSpan? LogSystemDurationAvg { get; set; }
        public TimeSpan? LogSystemDurationStdRange { get; set; }
        public TimeSpan? LogSystemGap { get; set; }
        public int LogSystemFiles { get; set; }
        public TimeSpan? LogDebugDurationAvg { get; set; }
        public TimeSpan? LogDebugDurationStdRange { get; set; }
        public TimeSpan? LogDebugGap { get; set; }
        public int LogDebugFiles { get; set; }

        [JsonIgnore]
        public long ReadCount
        {
            get { return this._nodes.Scalar(n => n.DSE.ReadCount).DefaultIfEmpty().Sum(); }
        }
        [JsonIgnore]
        public long WriteCount
        {
            get { return this._nodes.Scalar(n => n.DSE.WriteCount).DefaultIfEmpty().Sum(); }
        }
        [JsonIgnore]
        public decimal ReadCountAvg
        {
            get { return (decimal) this._nodes.Scalar(n => n.DSE.ReadCount).DefaultIfEmpty().Average(); }
        }
        [JsonIgnore]
        public decimal WriteCountAvg
        {
            get { return (decimal) this._nodes.Scalar(n => n.DSE.WriteCount).DefaultIfEmpty().Average(); }
        }

        public decimal DataQualityFactor()
        {
            decimal negFactor = Properties.Settings.Default.DCDQNegativeNumeratorFactor / (decimal) this._nodes.UnSafe.Count;            
            decimal factor = this._nodes.UnSafe
                            .Select(n => n.DataQualityFactor())
                            .DefaultIfEmpty()
                            .Sum();
            return factor;
        }
        
        public decimal DataQualityMaxFactor { get { return this._nodes.Count * Node.DataQualityMaxFactor; } }


        #endregion

        #region IEquatable
        virtual public bool Equals(string other)
        {
            return this._name == other;
        }

        virtual public bool Equals(IDataCenter other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(other,null)) return false;
            if (other is DataCenter && this._assocDC != null) return this._assocDC.Name == ((DataCenter)other).Name;

            if (this.Cluster.IsMaster || other.Cluster.IsMaster) return this._name == ((PlaceholderDataCenter)other)._name;

            return this.Cluster.Equals(other.Cluster) && this._name == ((PlaceholderDataCenter)other)._name;
        }
        #endregion

        #region overrides

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            { return true; }
            if (obj is IDataCenter)
            { return this.Equals((IDataCenter)obj); }
            if (obj is string)
            { return this.Equals((string)obj); }

            return false;
        }

        [JsonProperty(PropertyName="HashCode")]
        private int _hashcode = 0;
        public override int GetHashCode()
        {
            unchecked
            {
                if (this.Cluster.IsMaster) return this._name.GetHashCode();

                return this._hashcode = (589 + this.Cluster.GetHashCode()) * 31 + this.Name.GetHashCode() ;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}{{{1}}}", this._className, this.Name);
        }

        #endregion

    }

    [JsonObject(MemberSerialization.OptOut)]
    public sealed class DataCenter : PlaceholderDataCenter
	{
        /// <summary>
        /// An array of data centers that are associated to a IANA time zone name.
        /// </summary>
        public static DefaultAssocItemToTimeZone[] DefaultTimeZones = null;

        public DataCenter()
		{ }

		public DataCenter(Cluster cluster, string name)
		{
			this.Cluster = cluster;
			this.Name = name;
		}

        public override IKeyspace TryGetKeyspace(string ksName)
        {
            ksName = StringHelpers.RemoveQuotes(ksName?.Trim());

            return this.Keyspaces.FirstOrDefault(k => k.Name == ksName);
        }

        public override IKeyspace TryGetKeyspace(int keyspaceHashCode)
        {
            return this.Keyspaces.FirstOrDefault(k => k.GetHashCode() == keyspaceHashCode);
        }

        public INode TryGetAddNode(string nodeId)
		{
			if(!NodeIdentifier.ValidNodeIdName(nodeId))
			{ return null; }

            this._nodes.Lock();
            try
            {
                var node = this._nodes.UnSafe.FirstOrDefault(n => n.Equals(nodeId));

                if (node == null)
                {
                    node = Cluster.MasterCluster.TryGetNode(nodeId);

                    if (node == null)
                    {
                        node = Cluster.FindNodeInUnAssocaitedNodes(nodeId);

                        if (node == null)
                        {
                            node = new Node(this, nodeId);
                        }
                        else
                        {
                            ((Node)node).SetNodeToDataCenter(this);
                        }
                    }
                    else
                    {
                        ((Node)node).SetNodeToDataCenter(this);
                    }
                    this._nodes.UnSafe.Add(node);
                }

                return node;
            }
            finally
            {
                this._nodes.UnLock();
            }

		}

		public INode TryGetAddNode(NodeIdentifier nodeId)
		{
			if (nodeId == null)
			{ return null; }

            this._nodes.Lock();
            try
            {
                var node = this._nodes.UnSafe.FirstOrDefault(n => n.Equals(nodeId));
                
                if (node == null)
                {
                    node = Cluster.MasterCluster.TryGetNode(nodeId);

                    if (node == null)
                    {
                        node = Cluster.FindNodeInUnAssocaitedNodes(nodeId);

                        if (node == null)
                        {
                            node = new Node(this, nodeId);
                        }
                        else
                        {
                            ((Node)node).SetNodeToDataCenter(this);
                        }
                    }
                    else
                    {
                        ((Node)node).SetNodeToDataCenter(this);
                    }
                    this._nodes.UnSafe.Add(node);
                }

                return node;
            }
            finally
            {
                this._nodes.UnLock();
            }
		}

		override public INode TryGetAddNode(INode node)
		{
			if (node == null)
			{ return null; }

            this._nodes.Lock();
            try
            {
                if (!this._nodes.UnSafe.Contains(node))
                {
                    this._nodes.UnSafe.Add(node);
                }

                ((Node)node).SetNodeToDataCenter(this);

                return node;
            }
            finally
            {
                this._nodes.UnLock();
            }
		}
		public IDataCenter AssociateItem(IEvent eventItem)
		{
			if (eventItem != null)
			{
			}

			return this;
		}
		public IDataCenter AssociateItem(IConfigurationLine configItem)
		{
            lock(this._configurations)
            {
                this._configurations.Add(configItem);
            }

			return this;
		}
        
        override public IEnumerable<IConfigurationLine> GetConfigurations(INode node)
        {
            lock (this._configurations) { return this._configurations.Where(c => c.DataCenter is DataCenter
                                                                                    ? ((YamlConfigurationLine)c).Node.Equals(node)
                                                                                    : c.DataCenter.Nodes.Contains(node)); }
        }

        override public IConfigurationLine ConfigurationMatch(string property,
                                                                string value,
                                                                ConfigTypes type)
        {
            lock (this._configurations)
            {
                return this._configurations.FirstOrDefault(c => c.Match(this, property, value, type));
            }
        }

        /// <summary>
        /// This will override an existing cluster association without warning!
        /// </summary>
        /// <param name="newCluster"></param>
        /// <returns></returns>
        internal IDataCenter AssociateItem(Cluster newCluster)
        {
            if (newCluster == null) throw new NullReferenceException("newCluster cannot be null");

            if (!ReferenceEquals(this.Cluster, newCluster))
            {
                lock (this)
                {
                    if (!ReferenceEquals(this.Cluster, newCluster))
                    {
                        this.Cluster = newCluster;
                        this._nodes.ForEach(n => ((Node)n).SetCluster(newCluster));
                    }
                }
            }

            return this;
        }

        public new object ToDump()
        {
            return new { Name = this.Name,
                            Cluster = this.Cluster,
                            Nodes = this.Nodes,
                            Keyspaces = this.Keyspaces,
                            DDL = this.DDLs,
                            Configurations = this.Configurations,
                            LogEvents = this.LogEvents };
        }

        #region IDataCenter

        override public Cluster Cluster
        {
            get;
            protected set;
        }

        [JsonIgnore]
        override public IEnumerable<IKeyspace> Keyspaces
		{
			get
			{
                return this.Cluster.GetKeyspaces(this);
			}
		}

        override public string Name
		{
			get;
			protected set;
		}

        [JsonIgnore]
        override public IEnumerable<IMMLogValue> LogEvents { get { return this.Nodes.SelectMany(n => n.LogEvents); } }

        [JsonProperty(PropertyName="Configurations")]
        private List<IConfigurationLine> _configurations = new List<IConfigurationLine>();
        [JsonIgnore]
        override public IEnumerable<IConfigurationLine> Configurations { get { lock (this._configurations) { return this._configurations.ToArray(); } } }

        [JsonProperty(PropertyName = "AggregatedStats")]
        private IEnumerable<IAggregatedStats> datamemberAggregatedStats
        {
            get { return this._aggregatedStats.UnSafe; }
            set { this._aggregatedStats = new CTS.List<IAggregatedStats>(value); }
        }

        private CTS.List<IAggregatedStats> _aggregatedStats = new CTS.List<IAggregatedStats>();
        [JsonIgnore]
        public override IEnumerable<IAggregatedStats> AggregatedStats { get { return this._aggregatedStats; } }
        public override IDataCenter AssociateItem(IAggregatedStats aggregatedStat)
        {
            this._aggregatedStats.Add(aggregatedStat);
            return this;
        }

        [JsonIgnore]
		override public IEnumerable<IDDL> DDLs
        {
            get
            {
                return this.Keyspaces.Concat(this.Keyspaces.SelectMany(k => k.DDLs));
            }
        }

        #endregion

        #region IEquatable
        override public bool Equals(string other)
		{
            return this.Name == other;
        }

		override public bool Equals(IDataCenter other)
		{
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(other, null)) return false;
            if (other.GetType() == typeof(PlaceholderDataCenter)) return ((PlaceholderDataCenter)other).Equals(this);

            if (this.Cluster.IsMaster || other.Cluster.IsMaster) return this.Name == other.Name;

            return this.Cluster.Equals(other.Cluster) && this.Name == other.Name;
        }
		#endregion

		#region overrides

		public override bool Equals(object obj)
		{
            return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("DataCenter{{{0}}}", string.IsNullOrEmpty(this.Name) ? "<Unknown>" : this.Name);
		}

        public static bool operator ==(DataCenter a, DataCenter b)
        {
            return ReferenceEquals(a, b) || (!ReferenceEquals(a, null) && a.Equals(b));
        }

        public static bool operator !=(DataCenter a, DataCenter b)
        {
            if (ReferenceEquals(a, null) && !ReferenceEquals(b, null)) return true;
            if (ReferenceEquals(b, null) && !ReferenceEquals(a, null)) return true;
            if (ReferenceEquals(a, b)) return false;

            return !a.Equals(b);
        }
        #endregion

	}
}
