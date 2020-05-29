﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

namespace LinqToDB.SqlQuery
{
	public class SqlCteTable : SqlTable, ICloneableElement
	{
		public          CteClause? Cte  { get; private set; }

		public override string?    Name
		{
			get => Cte?.Name ?? base.Name;
			set => base.Name = value;
		}

		public override string?    PhysicalName
		{
			get => Cte?.Name ?? base.PhysicalName;
			set => base.PhysicalName = value;
		}

		public SqlCteTable(
			MappingSchema mappingSchema,
			CteClause     cte)
			: base(mappingSchema, cte.ObjectType, cte.Name)
		{
			Cte = cte ?? throw new ArgumentNullException(nameof(cte));

			// CTE has it's own names even there is mapping
			foreach (var field in Fields.Values)
				field.PhysicalName = field.Name;
		}

		internal SqlCteTable(int id, string alias, SqlField[] fields, CteClause cte)
			: base(id, cte.Name, alias, string.Empty, string.Empty, string.Empty, cte.Name, cte.ObjectType, null, fields, SqlTableType.Cte, null)
		{
			Cte = cte ?? throw new ArgumentNullException(nameof(cte));
		}

		internal SqlCteTable(int id, string alias, SqlField[] fields)
			: base(id, null, alias, string.Empty, string.Empty, string.Empty, null, null, null, fields, SqlTableType.Cte, null)
		{
		}

		internal void SetDelayedCteObject(CteClause cte)
		{
			Cte          = cte ?? throw new ArgumentNullException(nameof(cte));
			Name         = cte.Name;
			PhysicalName = cte.Name;
			ObjectType   = cte.ObjectType;
		}

		public SqlCteTable(SqlCteTable table, IEnumerable<SqlField> fields, CteClause cte)
		{
			Alias              = table.Alias;
			Server             = table.Server;
			Database           = table.Database;
			Schema             = table.Schema;

			PhysicalName       = table.PhysicalName;
			ObjectType         = table.ObjectType;
			SequenceAttributes = table.SequenceAttributes;

			Cte                = cte;

			AddRange(fields);
		}

		public override QueryElementType ElementType  => QueryElementType.SqlCteTable;
		public override SqlTableType     SqlTableType => SqlTableType.Cte;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return sb.Append(Name);
		}

		#region IQueryElement Members

		public string SqlText =>
			((IQueryElement) this).ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>())
			.ToString();


		#endregion

		ICloneableElement ICloneableElement.Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			if (!objectTree.TryGetValue(this, out var clone))
			{
				var table = new SqlCteTable(this, Array<SqlField>.Empty, Cte == null ? throw new ArgumentException() : (CteClause)Cte.Clone(objectTree, doClone))
				{
					Name               = base.Name,
					Alias              = Alias,
					Server             = Server,
					Database           = Database,
					Schema             = Schema,
					PhysicalName       = base.PhysicalName,
					ObjectType         = ObjectType,
					SqlTableType       = SqlTableType,
				};

				table.Fields.Clear();

				foreach (var field in Fields)
				{
					var fc = new SqlField(field.Value);

					objectTree.Add(field.Value, fc);
					table.     Add(fc);
				}

				objectTree.Add(this, table);
				objectTree.Add(All,  table.All);

				clone = table;
			}

			return clone;
		}
	}
}
