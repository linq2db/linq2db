using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LinqToDB.Common;
using LinqToDB.Mapping;

namespace LinqToDB.SqlQuery
{
	public class SqlCteClause : ISqlTableSource
	{
		public Dictionary<string, SqlField> Fields { get; } = new Dictionary<string, SqlField>();

		public string      Name       { get; set; }
		public SelectQuery Body       { get; set; }
		public Type        ObjectType { get; set; }

		public SqlCteClause(
			[JetBrains.Annotations.NotNull] MappingSchema mappingSchema,
			[JetBrains.Annotations.NotNull] SelectQuery   body,
			[JetBrains.Annotations.NotNull] Type          objectType, 
			string name)
		{
			ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
			Body       = body ?? throw new ArgumentNullException(nameof(body));
			Name       = name;

			var table = new SqlTable(mappingSchema, ObjectType);

			foreach (var field in table.Fields.Values)
			{
				if (table.All == field)
					_all = field;
				else
					Fields.Add(field.Name, field);

				field.Table = this;
			}
		}

		public SqlTableSource TableSource => _tableSource ?? (_tableSource = new SqlTableSource(this, Name));

		public QueryElementType ElementType => QueryElementType.CteClause;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return sb.Append("CTE");
		}

		#region ISqlExpression Members

		public bool CanBeNull => true;
		public int Precedence => SqlQuery.Precedence.Unknown;
		public Type SystemType => ObjectType;


		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return this == other;
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region ISqlExpressionWalkable Members

		public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
		{
			Body = Body?.Walk(skipColumns, func) as SelectQuery;

			return this;
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
		{
			return this == other;
		}

		#endregion

		#region ISqlTableSource Members

		public int           SourceID { get; } = Interlocked.Increment(ref SelectQuery.SourceIDCounter);
		public SqlTableType  SqlTableType => SqlTableType.CTE;

		private SqlField _all;
		private SqlTableSource _tableSource;

		public  SqlField  All
		{
			get => _all ?? (_all = new SqlField { Name = "*", PhysicalName = "*", Table = this });

			internal set
			{
				_all = value;

				if (_all != null)
					_all.Table = this;
			}
		}

		public IList<ISqlExpression> GetKeys(bool allIfEmpty)
		{
			if (allIfEmpty)
			{
				return Fields.Values.Cast<ISqlExpression>().ToArray();
			}
			return Array<ISqlExpression>.Empty;
		}

		#endregion

		#region IQueryElement Members

		public string SqlText =>
			((IQueryElement) this).ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>())
			.ToString();


		#endregion
	}
}
