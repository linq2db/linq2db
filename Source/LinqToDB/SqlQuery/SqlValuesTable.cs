using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlValuesTable : ISqlTableSource
	{
		public SqlValuesTable()
		{
			Rows = new List<IList<ISqlExpression>>();
		}

		internal SqlValuesTable(SqlField[] fields, IList<IList<ISqlExpression>> rows)
		{
			if (fields != null)
				foreach (var field in fields)
					Add(field);

			Rows = rows;
		}

		public Dictionary<string, SqlField> Fields { get; } = new Dictionary<string, SqlField>();

		private SqlField _all;
		SqlField ISqlTableSource.All => _all ?? (_all = new SqlField { Name = "*", PhysicalName = "*", Table = this });

		int ISqlTableSource.SourceID => throw new NotImplementedException();

		SqlTableType ISqlTableSource.SqlTableType => SqlTableType.Values;

		bool ISqlExpression.CanBeNull => throw new NotImplementedException();

		int ISqlExpression.Precedence => throw new NotImplementedException();

		Type ISqlExpression.SystemType => throw new NotImplementedException();

		QueryElementType IQueryElement.ElementType => QueryElementType.SqlValuesTable;

		public IList<IList<ISqlExpression>> Rows { get; }

		public void Add(SqlField field)
		{
			if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");

			field.Table = this;

			Fields.Add(field.Name, field);
		}

		ICloneableElement ICloneableElement.Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			throw new NotImplementedException();
		}

		bool ISqlExpression.Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			throw new NotImplementedException();
		}

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
		{
			throw new NotImplementedException();
		}

		IList<ISqlExpression> ISqlTableSource.GetKeys(bool allIfEmpty)
		{
			throw new NotImplementedException();
		}

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			throw new NotImplementedException();
		}

		ISqlExpression ISqlExpressionWalkable.Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			throw new NotImplementedException();
		}
	}
}
