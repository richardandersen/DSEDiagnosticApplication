﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace DSEDiagnosticLibrary
{
    public interface ICQLTrigger : IDDLStmt, IEquatable<string>, IEquatable<ICQLTrigger>
    {
        ICQLTable Table { get; }
        string JavaClass { get; }
    }

    public sealed class CQLTrigger : ICQLTrigger, IEquatable<ICQLTable>
    {
        public CQLTrigger(IFilePath cqlFile,
                            uint lineNbr,
                            string name,
                            ICQLTable table,
                            string javaClass,
                            string ddl,
                            INode defindingNode = null,
                            bool associateTriggerToKeyspace = true,
                            bool associateTriggerToTable = true)
        {
            if (string.IsNullOrEmpty(name)) throw new NullReferenceException("CQLTrigger name cannot be null");
            if (table == null) throw new NullReferenceException("CQLTrigger must be associated to a CQL Table. It cannot be null");
            if (string.IsNullOrEmpty(javaClass)) throw new NullReferenceException("CQLTrigger must have a Java Class");
            if (string.IsNullOrEmpty(ddl)) throw new NullReferenceException("CQLTrigger must have a DDL string");

            this.Path = cqlFile;
            this.Table = table;
            this.Node = defindingNode ?? table.Node;
            this.LineNbr = lineNbr;
            this.Name = StringHelpers.RemoveQuotes(name.Trim());
            this.DDL = ddl;
            this.JavaClass = StringHelpers.RemoveQuotes(javaClass.Trim());
            this.Items = 1;

            if (associateTriggerToKeyspace)
            {
                this.Keyspace.AssociateItem(this);
            }
            if (associateTriggerToTable)
            {
                this.Table.AssociateItem(this);
            }
        }

        #region IParsed
        public SourceTypes Source { get { return SourceTypes.CQL; } }
        public IPath Path { get; private set; }
        public Cluster Cluster { get { return this.Table.Cluster; } }
        public IDataCenter DataCenter { get { return this.Node?.DataCenter ?? this.Table.DataCenter; } }
        public INode Node { get; private set; }
        public int Items { get; private set; }
        public uint LineNbr { get; private set; }

        #endregion

        #region IDDLStmt
        public IKeyspace Keyspace { get { return this.Table.Keyspace; } }
        public string Name { get; private set; }
        public string DDL { get; private set; }
        public object ToDump()
        {
            return this;
        }
        public bool Equals(string other)
        {
            return this.Name == other;
        }

        #endregion

        #region ICQLTrigger
        public ICQLTable Table { get; private set; }
        public string JavaClass { get; private set; }
        #endregion

        #region IEquatable

        public bool Equals(ICQLTrigger other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            return this.DDL == other.DDL;
        }
        public bool Equals(ICQLTable other)
        {
            return this.Table.Equals(other);
        }
        #endregion

        #region overrides
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is string) return this.Equals((string)obj);
            if (obj is IKeyspace) return this.Keyspace.Equals((IKeyspace)obj);
            if (obj is IDataCenter) return this.DataCenter.Equals((IDataCenter)obj);
            if (obj is ICQLTable) return this.Equals((ICQLTable)obj);
            if (obj is ICQLTrigger) return this.Equals((ICQLTrigger)obj);

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return (this.Keyspace.Name + "." + this.Name).GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("CQLTrigger{{{0}}}", this.DDL);
        }
        #endregion
    }
}
