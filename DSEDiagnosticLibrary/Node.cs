﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net;
using Newtonsoft.Json;
using Common;
using Common.Patterns;
using Common.Patterns.TimeZoneInfo;
using CTS = Common.Patterns.Collections.ThreadSafe;
using CMM = Common.Patterns.Collections.MemoryMapped;
using IMMLogValue = Common.Patterns.Collections.MemoryMapped.IMMValue<DSEDiagnosticLibrary.ILogEvent>;

namespace DSEDiagnosticLibrary
{
    [JsonObject(MemberSerialization.OptOut)]
    public sealed class NodeIdentifier : IEquatable<NodeIdentifier>, IEquatable<IPAddress>, IEquatable<string>, IComparable<NodeIdentifier>
    {
        private NodeIdentifier()
        {
            this._addresses = new System.Collections.Concurrent.ConcurrentBag<IPAddress>();
            this._hostnames = new CTS.List<string>();
        }
        public NodeIdentifier(IPAddress ipAddress)
            : this()
        {
            if (ipAddress != null) this._addresses.Add(ipAddress);
        }
        public NodeIdentifier(string hostName)
            : this()
        {
            this.HostName = hostName?.Trim();
            if (this.HostName == string.Empty) this.HostName = null;
            if (this.HostName != null) this._hostnames.Add(this.HostName);
        }

        #region Methods
        public IPAddress AddIPAddress(IPAddress ipAddress)
        {
            if (ipAddress != null
                && !this._addresses.Contains(ipAddress))
            {
                this._addresses.Add(ipAddress);
            }
            return ipAddress;
        }

        /// <summary>
        /// Adds the IPAdresses from nodeIdentifier into this instance.
        /// </summary>
        /// <param name="nodeIdentifier"></param>
        /// <returns>
        /// Returns the first IPAddress of the addresses in nodeIdentifier that are not within the addresses in this instance.
        /// If null no addresses were found to be different between the instances or nodeIdentifier is null.
        /// </returns>
        public IPAddress AddIPAddress(NodeIdentifier nodeIdentifier)
        {
            if (nodeIdentifier != null)
            {
                var differences = nodeIdentifier._addresses.Complement(this._addresses);

                differences.ForEach(a => this._addresses.Add(a));

                return differences.FirstOrDefault();
            }

            return null;
        }

        public IPAddress AddIPAddress(string strIPAddress)
        {
            strIPAddress = strIPAddress?.Trim();
            if (!string.IsNullOrEmpty(strIPAddress))
            {
                if (IPAddress.TryParse(strIPAddress, out IPAddress ipAddress))
                {
                    return this.AddIPAddress(ipAddress);
                }
            }

            return null;
        }

        public static readonly Regex IPAddressRegEx = new Regex(LibrarySettings.IPAdressRegEx,
                                                                    RegexOptions.Compiled);

        public IPAddress SetIPAddressOrHostName(string strIPAddressOrHostName, bool tryParseAsIPAddress = true)
        {
            IPAddress ipAddress = null;

            strIPAddressOrHostName = strIPAddressOrHostName?.Trim();

            if (ValidNodeIdName(strIPAddressOrHostName))
            {
                if (tryParseAsIPAddress && IPAddress.TryParse(strIPAddressOrHostName, out ipAddress))
                {
                    this.AddIPAddress(ipAddress);
                }
                else
                {
                    bool isLocked = false;
                    this._hostnames.Lock(ref isLocked);
                    try
                    {
                        var hostPos = this._hostnames.UnSafe.FindIndex(n => HostNameEqual(n, strIPAddressOrHostName));

                        if (hostPos >= 0)
                        {
                            var hostParts = this._hostnames.UnSafe[hostPos].Count(c => c == '.');
                            var otherParts = strIPAddressOrHostName.Count(c => c == '.');

                            if (otherParts > hostParts)
                            {
                                if (this.HostName == this._hostnames.UnSafe[hostPos])
                                {
                                    this.HostName = strIPAddressOrHostName;
                                }
                                this._hostnames.UnSafe[hostPos] = strIPAddressOrHostName;
                            }
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(this.HostName))
                            {
                                this.HostName = strIPAddressOrHostName;
                            }
                            else
                            {
                                var hostParts = this.HostName.Count(c => c == '.');
                                var otherParts = strIPAddressOrHostName.Count(c => c == '.');

                                if (otherParts > hostParts)
                                {
                                    this.HostName = strIPAddressOrHostName;
                                }
                            }
                            this._hostnames.UnSafe.Add(strIPAddressOrHostName);
                        }
                    }
                    finally
                    {
                        if (isLocked) this._hostnames.UnLock();
                    }

                }
            }

            return ipAddress;
        }

        /// <summary>
        /// Returns Nodes IP4 address first, Host Name second, and IP6 address only if other two not defined.
        /// </summary>
        /// <returns></returns>
        public string NodeName()
        {
            IPAddress nodeAddress = null;

            if (this._addresses != null && this._addresses.HasAtLeastOneElement())
            {
                if (this._addresses.Count == 1)
                {
                    nodeAddress = this._addresses.First();
                }
                else
                {
                    nodeAddress = this._addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                    ?? this._addresses.FirstOrDefault();
                }
            }

            if (nodeAddress == null)
            {
                return this.HostName;
            }
            else if (nodeAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return nodeAddress.ToString();
            }
            else if (!string.IsNullOrEmpty(this.HostName))
            {
                return this.HostName;
            }

            return nodeAddress.ToString();
        }

        #endregion

        #region Static Methods

        [Flags]
        public enum CreateNodeIdentiferParsingOptions
        {
            IPAddressScan = 0x0001,
            HostNameScan = 0x0002,
            NodeNameEmbedded = 0x0008,
            IPAddressOrHostNameScan = IPAddressScan | HostNameScan,
            IPAddressOrHostNameEmbeddedScan = IPAddressScan | HostNameScan | NodeNameEmbedded
        }

        private static readonly Regex IgnoreIP6Addresses = new Regex(Properties.Settings.Default.IgnoreIP6AddressRegEx, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool ValidNodeIdName(string possibleNodeName)
        {
            possibleNodeName = possibleNodeName?.Trim();

            if (string.IsNullOrEmpty(possibleNodeName)
                     || possibleNodeName == "127.0.0.1"
                     || possibleNodeName == "0.0.0.0"
                     || possibleNodeName == "::1"
                     || possibleNodeName == "0:0:0:0:0:0:0:1"
                     || IgnoreIP6Addresses.IsMatch(possibleNodeName)
                     || possibleNodeName.ToLower().StartsWith("localhost")
                     || possibleNodeName.ToLower().EndsWith("localhost")
                     || possibleNodeName.ToLower().StartsWith("loopback")
                     || possibleNodeName.ToLower().EndsWith("loopback"))
            {
                return false;
            }

            return true;
        }

        public static bool GlobalIP6Address(string possibleNodeName)
        {
            possibleNodeName = possibleNodeName?.Trim();

            if (string.IsNullOrEmpty(possibleNodeName))
            {
                return false;
            }

            return IgnoreIP6Addresses.IsMatch(possibleNodeName);
        }

        public static bool DetermineIfNameHasIPAddressOrHostName(string possibleNodeName, bool checkForHostName = true)
        {
            var ipMatch = IPAddressRegEx.Match(possibleNodeName);

            if (!ipMatch.Success)
            {
                if (checkForHostName)
                {
                    var nameParts = possibleNodeName.Split(LibrarySettings.HostNamePathNameCharSeparators);

                    return nameParts.Length > 1;
                }

                return false;
            }

            return true;
        }

        /// <summary>
		/// Evaluates possibleNodeName to determine a Node&apos;s IPAdress (V4 or V6) or HostName.
		/// If a nodes&apos;s name is an IPAdress it can be embeded anywhere in the name.
		/// If it is a host name it must be at the beginning of the name separated by either a &quot;@&quot; (e.g., &quot;hostname@filename.ext&quot;) or plus sign (e.g., hostname+filename.ext)
		/// If it is not an IPAdress or embedded host name, it is assumed possibleNodeName is a host name.
		/// </summary>
		/// <param name="possibleNodeName">string that is evaluted to determine if it is either an IPAdress or host name.</param>
		/// <param name="options">
		/// </param>
		/// <returns>
		/// Returns null if possibleNodeName is null, empty, or not valid (e.g., 127.0.0.1, hostname, etc.).
		/// Returns a NodeIdentifier instance with either an IPAdress or host name.
		/// </returns>
		public static NodeIdentifier CreateNodeIdentifer(string possibleNodeName, CreateNodeIdentiferParsingOptions options = CreateNodeIdentiferParsingOptions.IPAddressOrHostNameEmbeddedScan)
        {
            IPAddress ipAddress = null;

            possibleNodeName = possibleNodeName?.Trim();

            if (ValidNodeIdName(possibleNodeName))
            {
                if (options.HasFlag(CreateNodeIdentiferParsingOptions.NodeNameEmbedded))
                {
                    if (options.HasFlag(CreateNodeIdentiferParsingOptions.IPAddressScan))
                    {
                        var ipMatch = IPAddressRegEx.Match(possibleNodeName);

                        if (ipMatch.Success)
                        {
                            if (IPAddress.TryParse(ipMatch.Value, out ipAddress))
                            {
                                return new NodeIdentifier(ipAddress);
                            }
                        }
                    }

                    if (options.HasFlag(CreateNodeIdentiferParsingOptions.HostNameScan))
                    {
                        var nameParts = possibleNodeName.Split(LibrarySettings.HostNamePathNameCharSeparators);

                        if (nameParts.Length > 1)
                        {
                            return new NodeIdentifier(nameParts[0]);
                        }
                    }

                    return null;
                }

                if (options.HasFlag(CreateNodeIdentiferParsingOptions.IPAddressScan))
                {
                    if (IPAddress.TryParse(possibleNodeName, out ipAddress))
                    { return new NodeIdentifier(ipAddress); }
                }

                if (options.HasFlag(CreateNodeIdentiferParsingOptions.HostNameScan))
                {
                    return new NodeIdentifier(possibleNodeName);
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new NodeIdentifier based on strIPAddressOrHostName
        /// </summary>
        /// <param name="strIPAddressOrHostName">
        /// This methods does not perform any type of scans, it just takes the argument and determines if it is an IP Address. If not it becomes the host name.
        /// </param>
        /// <param name="tryParseAsIPAddress"></param>
        /// <returns>
        /// Returns a NodeIdentifier instance or thrown an ArgumentException id the Id name is not valid.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown is the Id string is not valid
        /// </exception>
        public static NodeIdentifier Create(string strIPAddressOrHostName, bool tryParseAsIPAddress = true)
        {
            if (NodeIdentifier.ValidNodeIdName(strIPAddressOrHostName))
            {
                var instance = new NodeIdentifier();

                instance.SetIPAddressOrHostName(strIPAddressOrHostName, tryParseAsIPAddress);

                return instance;
            }

            throw new ArgumentException(string.Format("NodeIdentifier Name \"{0}\" is not valid", strIPAddressOrHostName, "strIPAddressOrHostName"));
        }

        public static bool operator ==(NodeIdentifier a, NodeIdentifier b)
        {
            return ReferenceEquals(a, b) || (!ReferenceEquals(a, null) && a.Equals(b));
        }

        public static bool operator !=(NodeIdentifier a, NodeIdentifier b)
        {
            if (ReferenceEquals(a, null) && !ReferenceEquals(b, null)) return true;
            if (ReferenceEquals(b, null) && !ReferenceEquals(a, null)) return true;
            if (ReferenceEquals(a, b)) return false;

            return !a.Equals(b);
        }

        #endregion

        [JsonProperty(PropertyName = "HostNames")]
        private IEnumerable<string> datamemberHostNames
        {
            get { return this._hostnames; }
            set
            {
                this._hostnames = value == null
                     ? null
                     : new Common.Patterns.Collections.ThreadSafe.List<string>(value);
            }
        }
        private Common.Patterns.Collections.ThreadSafe.List<string> _hostnames { get; set; }

        public string HostName { get; private set; }
        [JsonIgnore]
        public IEnumerable<string> HostNames { get { return this._hostnames; } }

        [JsonProperty(PropertyName = "Addresses")]
        private IEnumerable<string> datamemberAddresses
        {
            get { return this._addresses?.Select(a => a.ToString()); }
            set
            {
                this._addresses = value == null
                     ? null
                     : new System.Collections.Concurrent.ConcurrentBag<IPAddress>(value.Select(a => IPAddress.Parse(a)));
            }
        }
        private System.Collections.Concurrent.ConcurrentBag<IPAddress> _addresses { get; set; }
        [JsonIgnore]
        public IEnumerable<IPAddress> Addresses { get { return this._addresses; } }

        public bool HostNameExists(string checkHostName)
        {
            if (this._hostnames.Any(n => HostNameEqual(n, checkHostName))) return true;

            return false;
        }

        #region IEquatable
        public bool Equals(NodeIdentifier other)
        {
            if (ReferenceEquals(other, null)) return false;

            if (ReferenceEquals(this, other)) return true;

            //Prefer IP Address
            if (this._addresses.HasAtLeastOneElement() && other._addresses.HasAtLeastOneElement())
            {
                if (this._addresses.Contains(other._addresses)) return true;
            }

            if (this._hostnames.Any(other._hostnames, (x, y) => HostNameEqual(x, y))) return true;

            return false;
        }

        public bool Equals(IPAddress other)
        {
            if (this._addresses != null
                    && other != null)
            {
                return this._addresses.Contains(other);
            }

            return false;
        }

        public bool Equals(string other)
        {
            other = other?.Trim();

            if (string.IsNullOrEmpty(other)) return false;

            if (other[0] == '/')
            {
                other = other.Substring(1);
            }

            if (this._hostnames.Any(n => HostNameEqual(n, other))) return true;

            if (this._addresses.HasAtLeastOneElement())
            {
                if (IPAddress.TryParse(other, out IPAddress address))
                {
                    return this.Equals(address);
                }
            }

            return false;
        }

        /// <summary>
        /// Returns false if either or all argument(s) is/are null or string.empty
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool HostNameEqual(string a, string b)
        {
            a = a?.Trim();
            b = b?.Trim();

            if (string.IsNullOrEmpty(a)) return false;
            if (string.IsNullOrEmpty(b)) return false;
            if (a == b) return true;

            var thisHostParts = a.Split('.');
            var otherHostParts = b.Split('.');

            //Make sure it is not something like
            // host.a.b.c and host.d.e.f
            // so we should only check something like host.a.b.com == host
            if (thisHostParts.Length == 1 || otherHostParts.Length == 1)
            {
                //Just check for only host name (first value in array)
                // host.a.b.com == host
                if (thisHostParts[0] == otherHostParts[0])
                {
                    return true;
                }
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is NodeIdentifier)
            { return this.Equals((NodeIdentifier)obj); }

            if (obj is IPAddress)
            { return this.Equals((IPAddress)obj); }

            if (obj is string)
            { return this.Equals((string)obj); }

            return false;
        }

        #endregion

        public NodeIdentifier Clone(bool onlyIPAddress = false, bool onlyFirstItem = false)
        {
            var newId = new NodeIdentifier();

            if (onlyIPAddress && this._addresses.HasAtLeastOneElement())
            {
                if (onlyFirstItem)
                {
                    newId._addresses.Add(this._addresses.First());
                }
                else
                {
                    this._addresses.ForEach(a => newId._addresses.Add(a));
                }
            }
            else
            {
                if (onlyFirstItem)
                {
                    if (this._hostnames.HasAtLeastOneElement())
                    {
                        newId._hostnames.Add(this._hostnames.First());
                        newId.HostName = newId._hostnames.First();
                    }
                    if (this._addresses.HasAtLeastOneElement())
                        newId._addresses.Add(this._addresses.First());
                }
                else
                {
                    newId.HostName = this.HostName;
                    this._hostnames.ForEach(n => newId._hostnames.Add(n));
                    this._addresses.ForEach(a => newId._addresses.Add(a));
                    newId._hashCode = this._hashCode;
                }
            }

            return newId;
        }

        public override string ToString()
        {
            return this.ToString("NodeIdentifier");
        }

        public string ToString(string className)
        {
            string nodeAddress = null;

            if (this._addresses != null)
            {
                if (this._addresses.Count == 1)
                {
                    nodeAddress = this._addresses.First().ToString();
                }
                else
                {
                    nodeAddress = string.Join(", ", this._addresses);
                }
            }

            if (string.IsNullOrEmpty(this.HostName))
            {
                if (!string.IsNullOrEmpty(nodeAddress))
                {
                    return string.Format("{0}{{{1}}}", className, nodeAddress);
                }
            }
            else
            {
                return string.IsNullOrEmpty(nodeAddress)
                        ? string.Format("{0}{{{1}}}", className, this.HostName)
                        : string.Format("{0}{{{1}, {2}}}", className, this.HostName, nodeAddress);
            }

            return base.ToString();
        }

        [JsonProperty(PropertyName = "HashCode")]
        private int _hashCode = 0;
        public override int GetHashCode()
        {
            if (this._hashCode != 0) return this._hashCode;

            if (this._addresses != null && this._addresses.HasAtLeastOneElement())
                return this._hashCode = this._addresses.First().GetHashCode();

            return string.IsNullOrEmpty(this.HostName) ? 0 : this.HostName.GetHashCode();
        }

        public object ToDump()
        {
            return new { Host = this.HostName, IPAddresses = string.Join(",", this.Addresses.Select(i => i.ToString())) };
        }

        public static readonly Comparer ComparerInstance = new Comparer();

        public sealed class Comparer : IEqualityComparer<NodeIdentifier>
        {
            public bool Equals(NodeIdentifier x, NodeIdentifier y)
            {
                if (x == null && y == null) return true;
                if (x == null) return false;
                if (y == null) return false;

                return x.Equals(y);
            }

            public int GetHashCode(NodeIdentifier obj)
            {
                return obj == null ? 0 : obj.GetHashCode();
            }
        }

        public int CompareTo(NodeIdentifier other)
        {
            if (other == null) return 1;

            if (this.Equals(other)) return 0;

            return this.NodeName().CompareTo(other.NodeName());
        }
    }

    [JsonObject(MemberSerialization.OptOut)]
    public sealed class NodeStateChange : IComparable<NodeStateChange>, IEquatable<NodeStateChange>
    {

        public NodeStateChange(DetectedStates state,
                                DateTimeOffset eventTime,
                                DateTime eventTimeLocal,
                                INode detectedByNode = null,
                                TimeSpan? duration = null)
        {
            this.State = state;
            this.EventTime = eventTime;
            this.EventTimeLocal = eventTimeLocal;
            this.DetectedByNode = detectedByNode;

            if (duration.HasValue)
            {
                if (duration.Value.TotalMilliseconds >= 0d)
                {
                    this.Duration = duration;
                    if (this.Duration.Value > LibrarySettings.NodeDetectedLongPuaseThreshold) this.State |= DetectedStates.LongPause;
                }
                else
                    this.Duration = null;
            }
        }

        [Flags]
        public enum DetectedStates
        {
            None = 0x0000,
            Dead = 0x0001,
            NotResponding = 0x0002,
            Down = 0x0004,
            Up = 0x0008,
            Shutdown = 0x0010 | Down,
            Started = 0x0020 | Up,
            Restarted = 0x0100 | Started,
            GCPause = 0x0040 | NotResponding,
            LongPause = 0x0080,
            Added = 0x0200,
            Removed = 0x0400,
            TokenOwnershipChanged = 0x0800,
            UnableToStart = 0x1000,
            NetworkEvent = 0x2000,
            OtherStates = UnableToStart | TokenOwnershipChanged | Added | Removed
        }

        public Int16 SortOrder()
        {
            switch (this.State)
            {
                case DetectedStates.None:
                    break;
                case DetectedStates.Dead:
                    return 3;
                case DetectedStates.NotResponding:
                    return 2;
                case DetectedStates.Down:
                    return 1;
                case DetectedStates.Up:
                    return 7;
                case DetectedStates.Shutdown:
                    return 0;
                case DetectedStates.Started:
                    return 6;
                case DetectedStates.Restarted:
                    return 5;
                case DetectedStates.GCPause:
                    return 4;
                case DetectedStates.LongPause:
                    break;
                case DetectedStates.Added:
                    return 7;
                case DetectedStates.Removed:
                    return 7;
                case DetectedStates.TokenOwnershipChanged:
                    return 8;
                case DetectedStates.UnableToStart:
                    return 8;
                default:
                    break;
            }

            return 0;
        }

        public int CompareTo(NodeStateChange other)
        {
            if (other == null) return 1;

            if (this.State == other.State
                && this.EventTime == other.EventTime)
            {
                var sortOrder = this.SortOrder();
                var otherSortOrder = other.SortOrder();

                if (sortOrder == otherSortOrder)
                {
                    if (this.DetectedByNode == other.DetectedByNode)
                        return 0;

                    if (this.DetectedByNode == null)
                        return -1;
                    if (other.DetectedByNode == null)
                        return 1;

                    return this.DetectedByNode.Id.NodeName().CompareTo(other.DetectedByNode.Id.NodeName());
                }

                return sortOrder < otherSortOrder ? -1 : 1;
            }

            return this.EventTime.CompareTo(other.EventTime);
        }

        public bool Equals(NodeStateChange other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            if (this.State == other.State
                    && this.EventTime == other.EventTime)
            {
                var sortOrder = this.SortOrder();
                var otherSortOrder = other.SortOrder();

                if (sortOrder == otherSortOrder)
                {
                    if (this.DetectedByNode == other.DetectedByNode)
                        return true;

                    if (this.DetectedByNode == null || other.DetectedByNode == null)
                        return false;

                    return this.DetectedByNode.Id.NodeName() == other.DetectedByNode.Id.NodeName();
                }

                return false;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is NodeStateChange n)
            {
                return this.Equals(n);
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return this.EventTime.GetHashCode() * 31
                            + this.State.GetHashCode() * 62
                            + this.DetectedByNode?.GetHashCode() ?? 0;
            }
        }

        public DetectedStates State { get; }
        public DateTimeOffset EventTime { get; }
        public DateTime EventTimeLocal { get; }

        public TimeSpan? Duration { get; }

        /// <summary>
        /// If null, this event occurred on this node
        /// </summary>
        public INode DetectedByNode { get; }
    }

    [JsonObject(MemberSerialization.OptOut)]
    public sealed class MachineInfo
    {
        [JsonObject(MemberSerialization.OptOut)]
        public sealed class CPUInfo
        {
            public string Architecture;
            public uint? Cores;
        }

        [JsonObject(MemberSerialization.OptOut)]
        public sealed class CPULoadInfo
        {
            public UnitOfMeasure Average;
            public UnitOfMeasure Idle;
            public UnitOfMeasure System;
            public UnitOfMeasure User;
            public UnitOfMeasure IOWait;
            public UnitOfMeasure StealTime;
        }

        [JsonObject(MemberSerialization.OptOut)]
        public sealed class MemoryInfo
        {
            public UnitOfMeasure PhysicalMemory;
            public UnitOfMeasure Available;
            public UnitOfMeasure Cache;
            public UnitOfMeasure Buffers;
            public UnitOfMeasure Shared;
            public UnitOfMeasure Free;
            public UnitOfMeasure Used;
        }

        [JsonObject(MemberSerialization.OptOut)]
        public sealed class JavaInfo
        {
            [JsonObject(MemberSerialization.OptOut)]
            public struct MemoryInfo
            {
                public UnitOfMeasure Committed;
                public UnitOfMeasure Initial;
                public UnitOfMeasure Maximum;
                public UnitOfMeasure Used;
            }

            public string Vendor;
            public int? Model;
            public string RuntimeName;
            public string Version;
            public string GCType;
            public MemoryInfo NonHeapMemory;
            public MemoryInfo HeapMemory;
        }

        [JsonObject(MemberSerialization.OptOut)]
        public sealed class NTPInfo
        {
            [JsonConverter(typeof(IPAddressJsonConverter))]
            public IPAddress NTPServer;
            public int? Stratum;
            public UnitOfMeasure Correction;
            public UnitOfMeasure Polling;
            public UnitOfMeasure MaximumError;
            public UnitOfMeasure EstimatedError;
            public int? TimeConstant;
            public UnitOfMeasure Precision;
            public UnitOfMeasure Frequency;
            public UnitOfMeasure Tolerance;
        }

        [JsonObject(MemberSerialization.OptOut)]
        public sealed class DeviceInfo
        {
            public IEnumerable<KeyValuePair<string, UnitOfMeasure>> Free;
            public IEnumerable<KeyValuePair<string, UnitOfMeasure>> Used;
            public IEnumerable<KeyValuePair<string, decimal>> PercentUtilized;
        }

        public MachineInfo(INode assocatedNode)
        {
            this._assocatedNode = assocatedNode;
            this.CPU = new CPUInfo();
            this.CPULoad = new CPULoadInfo();
            this.Java = new JavaInfo();
            this.Memory = new MemoryInfo();
            this.NTP = new NTPInfo();
            this.Devices = new DeviceInfo();
        }
        private readonly INode _assocatedNode;

        public CPUInfo CPU;
        public string OS;
        public string OSVersion;
        public string Kernel;
        public string CloudVMType;
        public string Placement;

        /// <summary>
        /// Returns a time zone instance depending on if ExplictTimeZone is set. If not set the default time zone is used.
        /// The default time zone can be defined at the node, DC, or cluster levels.
        /// </summary>
        /// <seealso cref="ExplictTimeZone"/>
        /// <seealso cref="TimeZoneName"/>
        /// <seealso cref="DefaultAssocItemToTimeZone"/>
        /// <seealso cref="DataCenter.DefaultTimeZones"/>
        /// <seealso cref="Cluster.DefaultTimeZone"/>
        [JsonIgnore]
        public IZone TimeZone
        {
            get
            {
                if (this._explictTimeZone == null)
                {
                    if (string.IsNullOrEmpty(this._timezoneName))
                    {
                        if (Node.DefaultTimeZones != null && Node.DefaultTimeZones.Length > 0)
                        {
                            this.ExplictTimeZone = Node.DefaultTimeZones.FindDefaultTimeZone(this._assocatedNode.Id.NodeName());
                        }

                        if (this._explictTimeZone == null)
                        {
                            this.UsesDefaultTZ = true;
                            return this._assocatedNode.DataCenter?.DefaultMajorityTimeZone;
                        }
                    }
                    else
                    {
                        this.UsesDefaultTZ = false;
                        this._explictTimeZone = StringHelpers.FindTimeZone(this._timezoneName);
                    }
                }
                return this._explictTimeZone;
            }
        }

        /// <summary>
        /// If true the default time zone from the DC is used.
        /// </summary>
        [JsonIgnore]
        public bool UsesDefaultTZ { get; private set; } = true;

        private Common.Patterns.TimeZoneInfo.IZone _explictTimeZone = null;
        /// <summary>
        /// This is the time zone that has been explicitly defined. This will also set TimeZoneName.
        /// </summary>
        /// <seealso cref="TimeZoneName"/>
        public Common.Patterns.TimeZoneInfo.IZone ExplictTimeZone
        {
            get { return this._explictTimeZone; }
            set
            {
                if (value == null)
                    this.UsesDefaultTZ = true;
                else
                    this.UsesDefaultTZ = false;

                this._explictTimeZone = value;
                this._timezoneName = this._explictTimeZone?.Name;
            }
        }

        private string _timezoneName;
        /// <summary>
        /// Sets the time zone. This will also set ExplictTimeZone property if the name is a valid IANA name.
        /// </summary>
        /// <seealso cref="ExplictTimeZone"/>
        public string TimeZoneName
        {
            get { return this._timezoneName; }
            set
            {
                if (value != this._timezoneName)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        this._timezoneName = null;
                        this._explictTimeZone = null;
                        this.UsesDefaultTZ = true;
                    }
                    else
                    {
                        this._timezoneName = value;
                        this._explictTimeZone = StringHelpers.FindTimeZone(this._timezoneName);
                        this.UsesDefaultTZ = false;
                    }
                }
            }
        }
        public CPULoadInfo CPULoad;
        public MemoryInfo Memory;
        public JavaInfo Java;
        public DeviceInfo Devices;
        public NTPInfo NTP;
    }

    [JsonObject(MemberSerialization.OptOut)]
    public sealed class DSEInfo
    {
        public static DateTimeOffset? NodeToolCaptureTimestamp = null;

        [Flags]
        public enum InstanceTypes
        {

            Unkown = 0,
            Cassandra = 0x0001,
            Search = 0x0002,
            Analytics = 0x0004,
            TT = 0x0008,
            JT = 0x0010,
            Graph = 0x0020,
            AdvancedReplication = 0x0040,
            Hadoop = 0x0080,
            CFS = 0x0100,
            SW = 0x0200,
            SM = 0x0400,
            MultiInstance = 0x0800,
            Analytics_TT = Analytics | TT,
            Analytics_JT = Analytics | JT,
            Analytics_SM = Analytics | SM,
            Analytics_SW = Analytics | SW,
            Cassandra_JT = Cassandra | JT,
            SearchAnalytics = Search | Analytics,
            SearchAnalytics_TT = Search | Analytics | TT,
            SearchAnalytics_JT = Search | Analytics | JT,
            SearchAnalytics_SW = Search | Analytics | SW,
            SearchAnalytics_SM = Search | Analytics | SM
        }

        public enum DSEStatuses
        {
            Unknown = 0,
            Up,
            Down
        }

        [JsonObject(MemberSerialization.OptOut)]
        public sealed class VersionInfo
        {
            public Version DSE;
            public Version Cassandra;
            public Version Search;
            public Version Analytics;
            public Version OpsCenterAgent;
            public Guid? Schema;

            public static Version Parse(string version)
            {
                version = version?.Trim();
                if (string.IsNullOrEmpty(version)) return null;

                var parts = version.Split('.');

                if (parts.Length > 4)
                {
                    parts = new string[] { parts[0], parts[1], parts[2], parts[3] };
                }
                return new Version(string.Join(".", parts.Select(i => i.Trim())));
            }

            public static Guid? ParseSchema(string version)
            {
                version = version?.Trim();
                if (string.IsNullOrEmpty(version)) return null;
                Guid schemaVersion;

                try
                {
                    schemaVersion = new Guid(version);
                }
                catch
                {
                    return null;
                }

                return schemaVersion;
            }
        }

        [JsonObject(MemberSerialization.OptOut)]
        public sealed class DirectoryLocations
        {
            public string CassandraYamlFile;
            public string DSEYamlFile;
            public string HintsDir; //hints_directory
            public IEnumerable<string> DataDirs; //data_file_directories
            public string CommitLogDir; //commitlog_directory
            public string SavedCacheDir; //saved_caches_directory
        }

        [JsonObject(MemberSerialization.OptOut)]
        public sealed class DeviceLocations
        {
            public IEnumerable<string> Others;
            public IEnumerable<string> Data;
            public string CommitLog;
            public string SavedCache;
        }

        public DSEInfo()
        {
            this.Versions = new VersionInfo();
            this.Locations = new DirectoryLocations();
            this.Devices = new DeviceLocations();
        }

        public InstanceTypes InstanceType;
        public VersionInfo Versions;
        public Guid HostId;
        public Guid InsightClientId;

        /// <summary>
        /// Can be set in the DSE.yaml file (server_id) for multi-instance nodes to allow a group of DSE nodes on one physical server to be identified on that one physical server.
        /// </summary>
        public string PhysicalServerId;
        public string Rack;
        public string DataCenterSuffix;
        public DSEStatuses Statuses;
        public UnitOfMeasure StorageUsed;
        public UnitOfMeasure StorageUtilization;
        public string HealthRating;
        [JsonProperty(PropertyName = "CaptureTimestamp")]
        private DateTimeOffset? _captureTimestamp = null;
        [JsonIgnore]
        public DateTimeOffset? CaptureTimestamp
        {
            get { return this._captureTimestamp.HasValue ? this._captureTimestamp.Value : NodeToolCaptureTimestamp; }
            set
            {
                this._captureTimestamp = value;
            }
        }
        public DateTimeOffsetRange NodeToolDateRange;
        public UnitOfMeasure Uptime;
        public UnitOfMeasure Heap;
        public UnitOfMeasure HeapUsed;
        public UnitOfMeasure OffHeap;
        public bool? IsSeedNode; //seed_provider.parameters.seeds: "10.14.148.34,10.14.148.51"
        public bool? VNodesEnabled;
        public uint? NbrTokens;
        public uint? NbrExceptions;
        public bool? GossipEnabled;
        public bool? ThriftEnabled;
        public bool? NativeTransportEnabled;
        public bool? RepairServiceHasRan;
        public DateTimeRange RepairServiceRanRange;
        public UnitOfMeasure RepairedPercent;
        public DirectoryLocations Locations;
        public DeviceLocations Devices;
        public string EndpointSnitch;
        public string Partitioner;
        public string KeyCacheInformation;
        public string RowCacheInformation;
        public string CounterCacheInformation;
        public string ChunkCacheInformation;

        public DateTimeOffsetRange LogSystemDateRange;
        public TimeSpan? LogSystemDuration;
        public TimeSpan? LogSystemGap;
        public int LogSystemFiles;
        public DateTimeOffsetRange LogDebugDateRange;
        public TimeSpan? LogDebugDuration;
        public TimeSpan? LogDebugGap;
        public int LogDebugFiles;

        public long ReadCount;
        public long WriteCount;
        public long KeyCount;
        public long SSTableCount;

        [JsonProperty(PropertyName = "TokenRanges")]
        private readonly List<TokenRangeInfo> _tokenRanges = new List<TokenRangeInfo>();
        [JsonIgnore]
        public IEnumerable<TokenRangeInfo> TokenRanges { get { return this._tokenRanges; } }

        public TokenRangeInfo AddTokenPair(string start, string end, string tokenLoad)
        {
            var range = TokenRangeInfo.CreateTokenRange(start, end, tokenLoad);

            if (this._tokenRanges.Any(r => r.IsWithinRange(range)))
            {
                throw new ArgumentException(string.Format("Range {0} is within Current Token Ranges for this node.", range), "start,end,load");
            }

            this._tokenRanges.Add(range);

            return range;
        }
    }

    [JsonObject(MemberSerialization.OptOut)]
    public sealed class AnalyticsInfo
    {
        public AnalyticsInfo()
        {
            this.RunningTotalProperties = new Dictionary<string, object>();
        }

        public IDictionary<string, object> RunningTotalProperties
        {
            get;
        }

    }
    
    [JsonObject(MemberSerialization.OptOut)]
    public sealed class LogFileInfo
    {
        public static bool IsDebugLogFile(IFilePath logFile)
        {
            return logFile.Name.IndexOf(Properties.Settings.Default.DebugLogFileName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool DebugLogFileRequired(Version dseVersion, Version cassandraVersion)
        {
            return dseVersion == null
                    ? (cassandraVersion == null ? false : cassandraVersion.Major >= Properties.Settings.Default.DebugLogFileRequiredCVersion)
                    : dseVersion.Major >= Properties.Settings.Default.DebugLogFileRequiredDSEVersion; 
        }

        public LogFileInfo(IFilePath logFile,
                            DateTimeOffsetRange logfileDateRange,
                            int logItems,
                            IEnumerable<ILogEvent> orphanedEvents = null,
                            DateTimeOffsetRange logDateRange = null,
                            IEnumerable<DateTimeOffsetRange> nodeRestarts = null,
                            bool? isDebugLog = null)
        {
            this.LogFile = logFile;
            this.LogFileDateRange = logfileDateRange;
            this.LogItems = logItems;
            this.LogDateRange = logDateRange ?? logfileDateRange;
            this.LogFileSize = new UnitOfMeasure(logFile.FileInfo().Length, UnitOfMeasure.Types.Byte | UnitOfMeasure.Types.Storage);
            this.IsDebugLog = isDebugLog.HasValue ? isDebugLog.Value : IsDebugLogFile(this.LogFile);

            if (orphanedEvents != null)
            {
                this._orphanedEvents.AddRange(orphanedEvents);
            }

            if(nodeRestarts != null)
            {
                this._restarts.AddRange(nodeRestarts);
            }
        }

        public IFilePath LogFile { get; }
        /// <summary>
        /// The date range of the actual file
        /// </summary>
        public DateTimeOffsetRange LogFileDateRange { get; }
        /// <summary>
        /// The date range of the log entries
        /// </summary>
        public DateTimeOffsetRange LogDateRange { get; }
        public int LogItems { get; }
        public UnitOfMeasure LogFileSize { get; }

        [JsonProperty(PropertyName = "OrphanedEvents")]
        private IEnumerable<ILogEvent> datamemberOrphanedEvents
        {
            get { return this._orphanedEvents.UnSafe; }
            set { this._orphanedEvents = new CTS.List<ILogEvent>(value); }
        }

        private CTS.List<ILogEvent> _orphanedEvents = new CTS.List<ILogEvent>();
        [JsonIgnore]
        public IList<ILogEvent> OrphanedEvents { get { return this._orphanedEvents; } }

        private readonly CTS.List<DateTimeOffsetRange> _restarts = new CTS.List<DateTimeOffsetRange>();
        [JsonIgnore]
        public IList<DateTimeOffsetRange> Restarts { get { return this._restarts; } }

        public bool IsDebugLog { get; }

        public override string ToString()
        {
            return string.Format("LogFileInfo{{{0}, LogFileRange={1}, LogRange={2}, Items={3}, Orphans={4}}}",
                                    this.LogFile.Name,
                                    this.LogFileDateRange,
                                    this.LogDateRange,
                                    this.LogItems,
                                    this.OrphanedEvents.Count);
        }
    }
	public interface INode : IEquatable<INode>, IEquatable<NodeIdentifier>, IEquatable<IPAddress>, IEquatable<string>
	{
		Cluster Cluster { get; }
		IDataCenter DataCenter { get; }
		NodeIdentifier Id { get; }
		MachineInfo Machine { get; }
		DSEInfo DSE { get; }

        AnalyticsInfo Analytics { get; }

        /// <summary>
        /// If true event memory is mapped to virtual memory (false indicates event memory is in physical memory)
        /// </summary>
        bool IsMemoryMapped { get; }

        
        IEnumerable<IMMLogValue> LogEvents { get; }
        IEnumerable<IMMLogValue> LogEventsCache(LogCassandraEvent.ElementCreationTypes creationType, bool presistCache = false, bool safeRead = true);
        IEnumerable<IMMLogValue> LogEventsRead(LogCassandraEvent.ElementCreationTypes creationType, bool safeRead = true);

        void BeginBatchUpdate();
        bool EndBatchUpdate();

        bool WaitBatchUpdateCompletion(System.Threading.CancellationToken? cancelToken = null);

        INode AssociateItem(IMMLogValue eventItems);
        IMMLogValue AssociateItem(ILogEvent eventItems);
        IEnumerable<IAggregatedStats> AggregatedStats { get; }
        INode AssociateItem(IAggregatedStats aggregatedStat);

        IEnumerable<LogFileInfo> LogFiles { get; }
        INode AssociateItem(LogFileInfo logFileInfo);
        INode AssociateItem(NodeStateChange stateChange);

        IEnumerable<IConfigurationLine> Configurations { get; }

        IEnumerable<NodeStateChange> StateChanges { get; }

        /// <summary>
        /// Will return the node&apos;s name and optionally include certain attributes 
        /// </summary>
        /// <param name="forceAttrs"></param>
        /// <returns></returns>
        string NodeName(bool forceAttrs = false);

        string DCName();

        object ToDump();

        bool UpdateDSENodeToolDateRange();

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// Returns 1 to indicate higher then DC Average
        /// Returns 0 to indicate Within DC Average Threshold
        /// Returns -1 to indicate lower then DC Average
        /// False to indicate node&apos;s value is NaN
        /// </returns>
        int? NodeUpTimeDCAvgState();
        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// Returns 1 to indicate higher then DC Average
        /// Returns 0 to indicate Within DC Average Threshold
        /// Returns -1 to indicate lower then DC Average
        /// False to indicate node&apos;s value is NaN
        /// </returns>
        int? NodeSystemLogDCAvgState();
        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// Returns 1 to indicate higher then DC Average
        /// Returns 0 to indicate Within DC Average Threshold
        /// Returns -1 to indicate lower then DC Average
        /// False to indicate node&apos;s value is NaN
        /// </returns>
        int? NodeDebugLogDCAvgState();
        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// Returns 1 to indicate higher then debug duration than system
        /// Returns 0 to indicate Within DC Average Threshold
        /// Returns -1 to indicate lower then debug duration than system
        /// False to indicate node&apos;s value is NaN
        /// </returns>
        int? NodeSystemDebugLogState();
        bool NodeDCAvgStateWithinThreshold();
        int? NodeDCReadCntWithinThreshold();
        int? NodeDCWriteCntWithinThreshold();
        decimal DataQualityFactor();
    }

    [JsonObject(MemberSerialization.OptOut)]
    public sealed class Node : INode
	{
        /// <summary>
        /// An array of nodes that are associated to a IANA time zone name.
        /// </summary>
        public static DefaultAssocItemToTimeZone[] DefaultTimeZones = null;
        
        static Node()
		{
		}

		private Node()
		{
            this.DSE = new DSEInfo();
            this.Machine = new MachineInfo(this);
            this.Analytics = new AnalyticsInfo();

            if (LibrarySettings.LogEventsAreMemoryMapped)
            {
                this._eventsCMM = new CMM.List<LogCassandraEvent, ILogEvent>();
            }
            else
            {
                this._eventsCTS = new CTS.List<IMMLogValue>();
            }        
        }

		public Node(IDataCenter dataCenter, string nodeId)
            : this()
		{
			this.DataCenter = dataCenter;
			this.Id = NodeIdentifier.Create(nodeId);            
        }

		public Node(IDataCenter dataCenter, NodeIdentifier nodeId)
            : this()
		{
			this.DataCenter = dataCenter;
			this.Id = nodeId;
		}

        internal Node(Cluster cluster, NodeIdentifier nodeId)
            : this()
        {
            this._defaultCluster = cluster;
            this.Id = nodeId;
        }

        internal Node(Cluster cluster, string nodeId)
            : this()
        {
            this._defaultCluster = cluster;
            this.Id = NodeIdentifier.Create(nodeId);
        }

        public IDataCenter SetNodeToDataCenter(IDataCenter dataCenter)
		{
			if (dataCenter == null)
			{
				throw new ArgumentNullException("dataCenter cannot be null");
			}

			if(this.DataCenter == null)
			{				
                lock(this)
                {
                    if(this.DataCenter == null)
                    {
                        this.DataCenter = dataCenter;                        
                        this._hashcode = 0;
                    }
                }
			}

			if(ReferenceEquals(this.DataCenter, dataCenter))
			{
                Cluster.RemoveNodeFromUnAssociatedList(this.Id);
                this._defaultCluster = null;
                return this.DataCenter;
			}

			throw new ArgumentOutOfRangeException("dataCenter",
													string.Format("Node \"{0}\" has an existing DataCenter of \"{1}\". Trying to set it to DataCenter \"{2}\". This is not allowed once a datacenter is set.",
																	this.Id,
																	this.DataCenter.Name,
																	dataCenter.Name));
		}

        internal Cluster SetCluster(Cluster associateCluster)
        {
            if (associateCluster != null && this._defaultCluster != null)
            {
                lock (this)
                {
                    if (this._defaultCluster != null && !ReferenceEquals(this._defaultCluster, associateCluster))
                    {
                        this._defaultCluster = associateCluster;
                        this._hashcode = 0;
                    }
                }
            }

            return this._defaultCluster;
        }

        #region INode
        [JsonProperty(PropertyName="DefaultCluster")]
        private Cluster _defaultCluster = null;
        [JsonIgnore]
        public Cluster Cluster
		{
			get { return this.DataCenter?.Cluster ?? this._defaultCluster; }
		}

        [JsonProperty(PropertyName="DefaultDC")]
        private IDataCenter _defaultDC = null;
        [JsonIgnore]
        public IDataCenter DataCenter
		{
			get
            {
                return this._defaultDC;
            }
			private set
            {
                if (!ReferenceEquals(this._defaultDC, value))
                {
                    lock (this)
                    {
                        this._defaultDC = value;
                        if (this._defaultDC != null)
                        {
                            this._defaultCluster = null;
                        }
                    }
                }
            }
		}

        public DSEInfo DSE
		{
			get;
		}

        public AnalyticsInfo Analytics
        {
            get;
        }

        public NodeIdentifier Id
		{
			get;
			private set;
		}

        public MachineInfo Machine
		{
			get;
		}

        /*[JsonProperty(PropertyName="LogEvents")]
        private IEnumerable<IMMLogValue> datamemberEvents
        {
            get { return this._events.UnSafe; }
            set { this._events = new CTS.List<LogCassandraEvent>(value); }
        }*/

        private readonly CMM.List<LogCassandraEvent,ILogEvent> _eventsCMM;
        private readonly  CTS.List<IMMLogValue> _eventsCTS;

        /// <summary>
        /// If true event memory is mapped to virtual memory (false indicates event memory is in physical memory)
        /// </summary>
        [JsonIgnore]
        public bool IsMemoryMapped
        {   
            get { return this._eventsCMM != null; }
        }

        [JsonIgnore]
        public IEnumerable<IMMLogValue> LogEvents { get { return this._eventsCMM?.ToEnumerable() ?? (IEnumerable<IMMLogValue>) this._eventsCTS; } }

        public IEnumerable<IMMLogValue> LogEventsCache(LogCassandraEvent.ElementCreationTypes creationType, bool presistCache = false, bool safeRead = true)
        {
            if (this._eventsCMM == null)
            {
                if (safeRead)
                {
                    return (IEnumerable<IMMLogValue>)this._eventsCTS;
                }
                else
                {
                    this.WaitBatchUpdateCompletion();
                    return (IEnumerable<IMMLogValue>)this._eventsCTS.UnSafe;
                }
            }

            return this._eventsCMM.ToEnumerableCached((Common.Patterns.Collections.MemoryMapperElementCreationTypes)creationType, presistCache);
        }

        public IEnumerable<IMMLogValue> LogEventsRead(LogCassandraEvent.ElementCreationTypes creationType, bool safeRead = true)
        {
            if (this._eventsCMM == null)
            {
                if (safeRead)
                    return (IEnumerable<IMMLogValue>)this._eventsCTS;
                else
                {
                    this.WaitBatchUpdateCompletion();
                    return (IEnumerable<IMMLogValue>)this._eventsCTS.UnSafe;
                }
            }

            return this._eventsCMM.ToEnumerable((Common.Patterns.Collections.MemoryMapperElementCreationTypes)creationType);
        }

        private System.Threading.ManualResetEvent batchUpdateSignalEventRead { get; } = new System.Threading.ManualResetEvent(true);
        private Int32 batchEntryCnt = 0;

        /// <summary>
        ///
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public void BeginBatchUpdate()
        {            
            lock(this.batchUpdateSignalEventRead)
            {
                var refCnt = System.Threading.Interlocked.Increment(ref this.batchEntryCnt);

                if (refCnt == 1)
                {
                    this.batchUpdateSignalEventRead.Reset();                                        
                }
            }           
        }
        public bool EndBatchUpdate()
        {            
            lock (this.batchUpdateSignalEventRead)
            {
                var refCnt = System.Threading.Interlocked.Decrement(ref this.batchEntryCnt);

                if (refCnt <= 0)
                {
                    this.batchEntryCnt = 0;
                    this.batchUpdateSignalEventRead.Set();
                    return true;
                }                
            }

            return false;
        }
        public bool WaitBatchUpdateCompletion(System.Threading.CancellationToken? cancelToken = null)
        {
            if(cancelToken.HasValue)
            {
                int handlePos = System.Threading.WaitHandle.WaitAny(new System.Threading.WaitHandle[] { this.batchUpdateSignalEventRead, cancelToken.Value.WaitHandle });
                cancelToken.Value.ThrowIfCancellationRequested();
                return true;
            }

            return this.batchUpdateSignalEventRead.WaitOne();
        }

        public INode AssociateItem(IMMLogValue eventItems)
        {
            if (this._eventsCMM == null)
                this._eventsCTS.Add(eventItems);
            else
                this._eventsCMM.Add(eventItems);

            return this;
        }

        public IMMLogValue AssociateItem(ILogEvent eventItems)
        {
            if (eventItems.NodeTransitionState.HasValue)
            {
                TimeSpan? duration = ((eventItems.Type & EventTypes.SingleInstance) == EventTypes.SingleInstance
                                                && (eventItems.Type & EventTypes.SessionBegin) != EventTypes.SessionBegin)
                                            || (eventItems.Type & EventTypes.AggregateData) == EventTypes.AggregateData
                                            || (eventItems.Type & EventTypes.ExceptionElement) == EventTypes.ExceptionElement
                                            || (eventItems.Type & EventTypes.SessionEnd) == EventTypes.SessionEnd
                                            || (eventItems.Class & EventClasses.Orphaned) == EventClasses.Orphaned
                                        ? eventItems.Duration
                                        : (TimeSpan?)null;
                if (eventItems.AssociatedNodes != null && eventItems.AssociatedNodes.HasAtLeastOneElement())
                {
                    foreach (var targetNode in eventItems.AssociatedNodes)
                    {
                        var nodeState = new DSEDiagnosticLibrary.NodeStateChange(eventItems.NodeTransitionState.Value,
                                                                                    eventItems.EventTime,
                                                                                    eventItems.EventTimeLocal,
                                                                                    eventItems.Node,
                                                                                    duration);
                        targetNode.AssociateItem(nodeState);
                    }
                }
                else
                {
                    var nodeState = new DSEDiagnosticLibrary.NodeStateChange(eventItems.NodeTransitionState.Value,
                                                                                eventItems.EventTime,
                                                                                eventItems.EventTimeLocal,
                                                                                null,
                                                                                duration);
                    this.AssociateItem(nodeState);
                }
            }

            if (this._eventsCMM == null)
            {
                var mmValue = new CMM.MMValue<ILogEvent>(eventItems);
                this._eventsCTS.Add(mmValue);
                return mmValue;
            }

            return this._eventsCMM.AddElement((LogCassandraEvent) eventItems);
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
        public IEnumerable<IAggregatedStats> AggregatedStatsUnSafe { get { return this._aggregatedStats.UnSafe; } }

        public INode AssociateItem(IAggregatedStats aggregatedStat)
        {
            this._aggregatedStats.Add(aggregatedStat);
            return this;
        }

        [JsonProperty(PropertyName = "LogFiles")]
        private IEnumerable<LogFileInfo> datamemberLogFiles
        {
            get { return this._logFiles.UnSafe; }
            set { this._logFiles = new CTS.List<LogFileInfo>(value); }
        }

        private CTS.List<LogFileInfo> _logFiles = new CTS.List<LogFileInfo>();
        [JsonIgnore]
        public IEnumerable<LogFileInfo> LogFiles { get { return this._logFiles; } }
        public INode AssociateItem(LogFileInfo logFileInfo)
        {
            var fndItem = this._logFiles.FirstOrDefault(i => i.LogFile == logFileInfo.LogFile);

            if(fndItem != null)
            {
                if (fndItem.LogItems == 0 && (logFileInfo.LogItems != 0 || logFileInfo.LogDateRange != null))
                {
                    this._logFiles.Remove(fndItem);
                }
                else
                {
                    return this;
                }
            }

            this._logFiles.Add(logFileInfo);
            return this;
        }

        [JsonIgnore]
		public IEnumerable<IConfigurationLine> Configurations
        {
            get
            {
                return this.DataCenter.GetConfigurations(this);
            }
        }

        [JsonProperty(PropertyName = "StateChanges")]
        private CTS.List<NodeStateChange> _stateChanges = new CTS.List<NodeStateChange>();

        [JsonIgnore]
        public IEnumerable<NodeStateChange> StateChanges
        {
                get {
                        this.WaitBatchUpdateCompletion();
      
                        return this._stateChanges.UnSafe;
                    }
        }

        public INode AssociateItem(NodeStateChange stateChange)
        {
            bool isLocked = false;
            try
            {
                System.Threading.Monitor.Enter(this._stateChanges.UnSafe, ref isLocked);

                if (!this._stateChanges.UnSafe.Exists(n => n.Equals(stateChange)))
                    this._stateChanges.UnSafe.Add(stateChange);
            }
            finally
            {
                if (isLocked) System.Threading.Monitor.Exit(this._stateChanges.UnSafe);
            }

            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// Returns 1 to indicate higher then DC Average
        /// Returns 0 to indicate Within DC Average Threshold
        /// Returns -1 to indicate lower then DC Average
        /// False to indicate node&apos;s value is NaN
        /// </returns>
        public int? NodeUpTimeDCAvgState()
        {
            if (this.DSE.Uptime.NaN)
            {
                return null;
            }

            if (this.DataCenter.NodeUpTimeAvg.HasValue
                    && Math.Abs((this.DSE.Uptime.ConvertToTimeSpan() - this.DataCenter.NodeUpTimeAvg.Value).TotalHours) > Properties.Settings.Default.ThresholdAvgHrs.TotalHours)
            {
                if (this.DSE.Uptime.ConvertToTimeSpan() < this.DataCenter.NodeUpTimeAvg - this.DataCenter.NodeUpTimeStdRange)
                {
                    return -1;
                }
                else if (this.DSE.Uptime.ConvertToTimeSpan() > this.DataCenter.NodeUpTimeAvg + this.DataCenter.NodeUpTimeStdRange)
                {
                    return 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// Returns 1 to indicate higher then DC Average
        /// Returns 0 to indicate Within DC Average Threshold
        /// Returns -1 to indicate lower then DC Average
        /// False to indicate node&apos;s value is NaN
        /// </returns>
        public int? NodeSystemLogDCAvgState()
        {
            if (!this.DSE.LogSystemDuration.HasValue)
            {
                return null;
            }

            if (Math.Abs((this.DSE.LogSystemDuration.Value - this.DataCenter.LogSystemDurationAvg.Value).TotalHours) > Properties.Settings.Default.ThresholdAvgHrs.TotalHours)
            {
                if (this.DSE.LogSystemDuration.Value < this.DataCenter.LogSystemDurationAvg - this.DataCenter.LogSystemDurationStdRange)
                {
                    return -1;
                }
                else if (this.DSE.LogSystemDuration.Value > this.DataCenter.LogSystemDurationAvg + this.DataCenter.LogSystemDurationStdRange)
                {
                    return 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// Returns 1 to indicate higher then DC Average
        /// Returns 0 to indicate Within DC Average Threshold
        /// Returns -1 to indicate lower then DC Average
        /// False to indicate node&apos;s value is NaN
        /// </returns>
        public int? NodeDebugLogDCAvgState()
        {
            if (!this.DSE.LogDebugDuration.HasValue)
            {
                return null;
            }

            if (Math.Abs((this.DSE.LogDebugDuration.Value - this.DataCenter.LogDebugDurationAvg.Value).TotalHours) > Properties.Settings.Default.ThresholdAvgHrs.TotalHours)
            {
                if (this.DSE.LogDebugDuration.Value < this.DataCenter.LogDebugDurationAvg - this.DataCenter.LogDebugDurationStdRange)
                {
                    return -1;
                }
                else if (this.DSE.LogDebugDuration.Value > this.DataCenter.LogDebugDurationAvg + this.DataCenter.LogDebugDurationStdRange)
                {
                    return 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// Returns 1 to indicate higher then debug duration than system
        /// Returns 0 to indicate Within DC Average Threshold
        /// Returns -1 to indicate lower then debug duration than system
        /// False to indicate node&apos;s value is NaN
        /// </returns>
        public int? NodeSystemDebugLogState()
        {
            if (!this.DSE.LogSystemDuration.HasValue || !this.DSE.LogDebugDuration.HasValue)
            {              
                return null;
            }
            
            if (Math.Abs((this.DSE.LogDebugDuration.Value - this.DSE.LogSystemDuration.Value).TotalHours) > Properties.Settings.Default.ThresholdAvgHrs.TotalHours
                    || this.DSE.LogDebugDuration.Value < Properties.Settings.Default.ThresholdAvgHrs
                    || this.DSE.LogSystemDuration < Properties.Settings.Default.ThresholdAvgHrs)
            {
                if (this.DSE.LogDebugDuration.Value < this.DSE.LogSystemDuration.Value)
                {
                    return -1;
                }
                else if (this.DSE.LogDebugDuration.Value > this.DSE.LogSystemDuration.Value)
                {
                    return 1;
                }
            }

            return 0;
        }


        public bool NodeDCAvgStateWithinThreshold()
        {
            if(!this.DSE.Uptime.NaN
                && this.DSE.LogSystemDuration.HasValue
                && Math.Abs((this.DSE.LogSystemDuration.Value - this.DSE.Uptime.ConvertToTimeSpan()).TotalHours) <= Properties.Settings.Default.ThresholdUpTimeLogHrs.TotalHours)
            {
                return true;
            }
            
            return false;
        }

        public decimal NodeDCAvgStateFactor()
        {
            if (!this.DSE.Uptime.NaN
                && this.DSE.LogSystemDuration.HasValue)
            {
                var diff = (decimal) (this.DSE.LogSystemDuration.Value - this.DSE.Uptime.ConvertToTimeSpan()).TotalHours;

                return diff < 0m ? Math.Abs(diff) / (decimal)Properties.Settings.Default.ThresholdUpTimeLogHrs.TotalHours : 1m;
            }

            return 1m;
        }

        public int? NodeDCReadCntWithinThreshold()
        {
            if (this.DSE.NodeToolDateRange == null && this.DSE.WriteCount == 0 && this.DSE.ReadCount == 0)
                return null;

            var dcAvg = this.DataCenter.ReadCountAvg;
            var dcThreshold = dcAvg * LibrarySettings.DCNodeReadThresholdPct;

            if (this.DSE.ReadCount < dcAvg - dcThreshold)
                return -1;

            if (this.DSE.ReadCount > dcAvg + dcThreshold)
                return 1;

            return 0;
        }
        public int? NodeDCWriteCntWithinThreshold()
        {
            if (this.DSE.NodeToolDateRange == null && this.DSE.WriteCount == 0 && this.DSE.ReadCount == 0)
                return null;

            var dcAvg = this.DataCenter.WriteCountAvg;
            var dcThreshold = dcAvg * LibrarySettings.DCNodeWriteThresholdPct;

            if (this.DSE.WriteCount < dcAvg - dcThreshold)
                return -1;

            if (this.DSE.WriteCount > dcAvg + dcThreshold)
                return 1;

            return 0;
        }

        public decimal DataQualityFactor()
        {
            decimal negFactor = Properties.Settings.Default.DCDQNegativeNumeratorFactor / (decimal)this.DataCenter.Nodes.Count();
            var debugRequired = LogFileInfo.DebugLogFileRequired(this.DSE.Versions?.DSE, this.DSE.Versions?.Cassandra);
            var nbrStarts = this.StateChanges.Count(i => i.DetectedByNode == null
                                                            && (i.State == NodeStateChange.DetectedStates.Added
                                                                || i.State.HasFlag(NodeStateChange.DetectedStates.Started)));
            var nbrTokenOwership = this.StateChanges.Count(i => i.State.HasFlag(NodeStateChange.DetectedStates.TokenOwnershipChanged));

            var dqFactor = (this.NodeUpTimeDCAvgState().HasValue ? (this.NodeUpTimeDCAvgState().Value == 0 ? 1m : negFactor) : 0m)
                                            + (this.NodeSystemLogDCAvgState().HasValue
                                                    ? (this.NodeSystemLogDCAvgState().Value == 0 ? 1m : negFactor)
                                                    : Properties.Settings.Default.NodeDQNegativeMissingSystemLogs + negFactor)
                                            + (debugRequired
                                                    ? (this.NodeDebugLogDCAvgState().HasValue
                                                        ? (this.NodeDebugLogDCAvgState().Value == 0 ? 1m : negFactor)
                                                        : Properties.Settings.Default.NodeDQNegativeMissingDebugLogs + negFactor)
                                                    : 1m)
                                            + (debugRequired
                                                    ? (this.NodeSystemDebugLogState().HasValue
                                                            ? (this.NodeSystemDebugLogState().Value == 0 ? 1m : Properties.Settings.Default.DCDQSysDebugLogAvgStateFactor + negFactor)
                                                            : 0m)
                                                    : 1m)
                                            + (this.NodeDCAvgStateWithinThreshold()
                                                ? 1m
                                                : (Properties.Settings.Default.DCDQNodeSysLogAvgStateFactor + negFactor) * this.NodeDCAvgStateFactor())                                                    
                                            + (this.Configurations.IsEmpty() ? 0m : 1m)
                                            + (this.DSE.Versions?.DSE == null && this.DSE.Versions?.Cassandra == null ? 0m : 1m)
                                            + (this.DSE.Devices?.CommitLog == null && (this.DSE.Devices.Data?.IsEmpty() ?? true) ? 0m : 1m)
                                            + (this.Machine?.Java?.HeapMemory.Committed.NaN ?? true ? 0m : 1m)
                                            + (this.Machine?.Memory?.Available.NaN ?? true ? 0m : 1m)
                                            + (this.DSE.WriteCount > 0 || this.DSE.ReadCount > 0 || this.DSE.SSTableCount > 0 ? 1m : Properties.Settings.Default.DCDQNoCountsFactor)
                                            + (this.NodeDCReadCntWithinThreshold().HasValue && this.NodeDCReadCntWithinThreshold() == 0 ? .5m : 0.25m)
                                            + (this.NodeDCWriteCntWithinThreshold().HasValue && this.NodeDCWriteCntWithinThreshold() == 0 ? .5m : 0.25m)
                                            + (nbrStarts * Properties.Settings.Default.NodeDQNegativeStarts)
                                            + (nbrTokenOwership * Properties.Settings.Default.NodeDQNegativeTokenChanges);

            return dqFactor < 0 ? 0 : dqFactor;
        }

        public const decimal DataQualityMaxFactor = 12;

        public string NodeName(bool forceAttrs = false)
        {
            var nodeName = this.Id.NodeName();

            if(LibrarySettings.EnableAttrSymbols || forceAttrs)
            {                
                string strAttr = string.Empty;
                var upTimeState = this.NodeUpTimeDCAvgState();
                var systemState = this.NodeSystemLogDCAvgState();
                var debugState = this.NodeDebugLogDCAvgState();
                var readState = this.NodeDCReadCntWithinThreshold();
                var writeState = this.NodeDCWriteCntWithinThreshold();

                if (upTimeState.HasValue)
                {
                    if (upTimeState.Value == -1)
                    {
                        strAttr += (char)Properties.Settings.Default.LowAttrChar;                        
                    }
                    else if(upTimeState.Value == 1)
                    {
                        strAttr += (char)Properties.Settings.Default.HighAttrChar;
                    }
                    else
                    {
                        strAttr += (char)Properties.Settings.Default.MidAttrChar;
                    }
                }
                else
                {
                    strAttr += (char)Properties.Settings.Default.PHAttrChar;
                }

                if (systemState.HasValue)
                {
                    if (systemState.Value == -1)
                    {
                        strAttr += (char)Properties.Settings.Default.LowAttrChar;
                    }
                    else if (systemState.Value == 1)
                    {
                        strAttr += (char)Properties.Settings.Default.HighAttrChar;
                    }
                    else
                    {
                        strAttr += (char)Properties.Settings.Default.MidAttrChar;
                    }
                }
                else
                {
                    strAttr += (char)Properties.Settings.Default.PHAttrChar;
                }

                if (debugState.HasValue)
                {
                    if (debugState.Value == -1)
                    {
                        strAttr += (char)Properties.Settings.Default.LowAttrChar;
                    }
                    else if (debugState.Value == 1)
                    {
                        strAttr += (char)Properties.Settings.Default.HighAttrChar;
                    }
                    else
                    {
                        strAttr += (char)Properties.Settings.Default.MidAttrChar;
                    }
                }
                else
                {
                    strAttr += (char)Properties.Settings.Default.PHAttrChar;
                }

                //if (!hasAttr)
                //    strAttr = strAttr.TrimEnd((char)Properties.Settings.Default.PHAttrChar, (char)Properties.Settings.Default.MidAttrChar);

                if (this.NodeDCAvgStateWithinThreshold())
                {
                    strAttr += (char)Properties.Settings.Default.UpTimeLogSimilarAttrChar;
                }
                else
                {
                    strAttr += (char)Properties.Settings.Default.UpTimeLogMismatchAttrChar;
                }

                if (readState.HasValue)
                {
                    if (readState.Value == -1)
                    {
                        strAttr += (char)Properties.Settings.Default.LowAttrChar;
                    }
                    else if (readState.Value == 1)
                    {
                        strAttr += (char)Properties.Settings.Default.HighAttrChar;
                    }
                    else
                    {
                        strAttr += (char)Properties.Settings.Default.MidAttrChar;
                    }
                }
                else
                {
                    strAttr += (char)Properties.Settings.Default.PHAttrChar;
                }
                if (writeState.HasValue)
                {
                    if (writeState.Value == -1)
                    {
                        strAttr += (char)Properties.Settings.Default.LowAttrChar;
                    }
                    else if (writeState.Value == 1)
                    {
                        strAttr += (char)Properties.Settings.Default.HighAttrChar;
                    }
                    else
                    {
                        strAttr += (char)Properties.Settings.Default.MidAttrChar;
                    }
                }
                else
                {
                    strAttr += (char)Properties.Settings.Default.PHAttrChar;
                }

                if ((this.DSE.InstanceType & DSEInfo.InstanceTypes.Search) == DSEInfo.InstanceTypes.Search)
                {
                    strAttr += (char)Properties.Settings.Default.SolrAttrChar;
                }
                if ((this.DSE.InstanceType & DSEInfo.InstanceTypes.Analytics) == DSEInfo.InstanceTypes.Analytics)
                {
                    strAttr += (char)Properties.Settings.Default.AnalyticsAttrChar;
                }
                if ((this.DSE.InstanceType & DSEInfo.InstanceTypes.Graph) == DSEInfo.InstanceTypes.Graph)
                {
                    strAttr += (char)Properties.Settings.Default.GraphAttrChar;
                }

                if (strAttr != string.Empty)
                {
                    nodeName += " " + strAttr;
                }                
            }

            return nodeName;
        }

        public string DCName()
        {
            return this.DataCenter?.Name ?? "<UnKnown>";
        }

        public bool UpdateDSENodeToolDateRange()
        {
            bool bResult = false;
            var machineTZ = this.Machine.TimeZone;

            if(!this.DSE.Uptime.NaN
                && this.DSE.CaptureTimestamp.HasValue)
            {
                DateTimeOffset refDTO;

                if (machineTZ == null)
                {
                    refDTO = this.DSE.CaptureTimestamp.Value;
                }
                else
                {
                    try
                    {
                        refDTO = Common.TimeZones.Convert(this.DSE.CaptureTimestamp.Value, machineTZ);
                    }
                    catch(System.Exception)
                    {
                        System.Threading.Thread.Sleep(1);
                        refDTO = Common.TimeZones.Convert(this.DSE.CaptureTimestamp.Value, machineTZ);
                    }
                }

                if (this.DSE.NodeToolDateRange == null || this.DSE.NodeToolDateRange.Max.Offset != refDTO.Offset)
                {
                    this.DSE.NodeToolDateRange = new DateTimeOffsetRange(refDTO - (TimeSpan)this.DSE.Uptime, refDTO);
                    bResult = true;
                }
            }

            return bResult;
        }

        public object ToDump()
        {
            return new { this.Id,
                            this.DataCenter,
                            DSEInfo = this.DSE,
                            MachineInfo = this.Machine,
                            this.LogEvents,
                            this.Configurations };
        }
		#endregion

		#region IEquatable
		public bool Equals(IPAddress other)
		{
			return this.Id.Equals(other);
		}

		public bool Equals(string other)
		{
			return this.Id.Equals(other);
		}
		public bool Equals(INode other)
		{
			return other == null ? false : this.Id.Equals(other.Id);
		}

		public bool Equals(NodeIdentifier other)
		{
			return this.Id.Equals(other);
		}
		#endregion

		#region Overrides

		public override bool Equals(object obj)
		{
			if(ReferenceEquals(this, obj))
			{ return true; }
			if(obj is INode)
			{ return this.Equals((INode)obj); }
			if(obj is string)
			{ return this.Equals((string)obj); }
			if(obj is NodeIdentifier)
			{ return this.Equals((NodeIdentifier)obj); }
			if (obj is IPAddress)
			{ return this.Equals((IPAddress)obj); }

			return false;
		}

        [JsonProperty(PropertyName="HashCode")]
        private int _hashcode = 0;
		public override int GetHashCode()
		{
            unchecked
            {
                if (this._hashcode != 0) return this._hashcode;
                if (this.Id == null) return 0;
                if (this.Cluster.IsMaster || this.DataCenter == null) return this.Id.GetHashCode();

                return this._hashcode = this.DataCenter.GetHashCode() * 31 + this.Id.GetHashCode();
            }
		}

		public override string ToString()
		{
			return this.Id == null ? base.ToString() : this.Id.ToString("Node");
		}

        public static bool operator ==(Node a, Node b)
        {
            return ReferenceEquals(a, b) || (!ReferenceEquals(a, null) && a.Equals(b));
        }

        public static bool operator !=(Node a, Node b)
        {
            if (ReferenceEquals(a, null) && !ReferenceEquals(b, null)) return true;
            if (ReferenceEquals(b, null) && !ReferenceEquals(a, null)) return true;
            if (ReferenceEquals(a, b)) return false;

            return !a.Equals(b);
        }

        #endregion

	}
}
