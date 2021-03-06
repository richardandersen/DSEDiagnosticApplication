﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Common;

namespace DSEDiagnosticLibrary
{
    public interface ICQLMaterializedView : ICQLTable, IEquatable<ICQLMaterializedView>, IEquatable<ICQLTable>
    {
        ICQLTable Table { get; }
        string WhereClause { get; }
    }

    [JsonObject(MemberSerialization.OptOut)]
    public sealed class CQLMaterializedView : CQLTable, ICQLMaterializedView
    {
        public CQLMaterializedView(IFilePath cqlFile,
                                    uint lineNbr,
                                    IKeyspace keyspace,
                                    string name,
                                    ICQLTable baseTable,
                                    IEnumerable<ICQLColumn> columns,
                                    IEnumerable<ICQLColumn> primaryKeys,
                                    IEnumerable<ICQLColumn> clusteringKeys,
                                    IEnumerable<CQLOrderByColumn> orderByCols,
                                    IDictionary<string, object> properties,
                                    string whereClause,
                                    string ddl,
                                    INode defindingNode = null,
                                    bool associateViewToColumn = true,
                                    bool associateViewToKeyspace = true,
                                    bool associateViewToTable = true)
            : base(cqlFile,
                    lineNbr,
                    keyspace,
                    name,
                    columns,
                    primaryKeys,
                    clusteringKeys,
                    orderByCols,
                    properties,
                    ddl,
                    defindingNode,
                    associateViewToColumn,
                    false)
        {
            if (baseTable == null) throw new NullReferenceException(string.Format("CQLMaterializedView must be associated to a CQL Table. It cannot be null for CQL \"{0}\"", ddl));

            this.Table = baseTable;
            this.WhereClause = whereClause;

            if(associateViewToTable)
            {
                baseTable.AssociateItem(this);
            }
            if (associateViewToKeyspace)
            {
                this.Keyspace.AssociateItem(this);
            }
        }

        #region ICQLMaterializedView

        public ICQLTable Table { get;  }
        public string WhereClause { get;  }

        public new object ToDump()
        {
            return new { MaterializedView = this.FullName, Cluster = this.Cluster.Name, DataCenter = this.DataCenter.Name, Me = this };
        }

        #endregion

        #region IEquatable<ICQLMaterializedView>
        public bool Equals(ICQLMaterializedView other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            return this.DDL == other.DDL;
        }

        #endregion

        #region overrides
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is ICQLMaterializedView) return this.Equals((ICQLMaterializedView)obj);
            if (obj is ICQLTable) return this.Equals((ICQLTable)obj);

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("CQLMaterializedView{{{0}}}", this.DDL);
        }

        #endregion

        public new static ICQLMaterializedView TryGet(IKeyspace ksInstance, string name)
        {
            return ksInstance.TryGetView(name);
        }

    }

}
