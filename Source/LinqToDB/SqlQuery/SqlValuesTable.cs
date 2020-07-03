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

		public IList<IList<ISqlExpression>> Rows { get; }

		public void Add(SqlField field)
		{
			if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");

			field.Table = this;

			Fields.Add(field.Name, field);
		}

		#region ISqlTableSource
		private SqlField? _all;
		SqlField ISqlTableSource.All => _all ?? (_all = SqlField.All(this));

		int ISqlTableSource.SourceID => throw new NotImplementedException();

		SqlTableType ISqlTableSource.SqlTableType => SqlTableType.Values;

		IList<ISqlExpression> ISqlTableSource.GetKeys(bool allIfEmpty) => throw new NotImplementedException();
		#endregion

		#region ISqlExpression

		bool ISqlExpression.CanBeNull => throw new NotImplementedException();

		int ISqlExpression.Precedence => throw new NotImplementedException();

		Type ISqlExpression.SystemType => throw new NotImplementedException();

		bool ISqlExpression.Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region ICloneableElement
		ICloneableElement ICloneableElement.Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region IQueryElement
		QueryElementType IQueryElement.ElementType => QueryElementType.SqlValuesTable;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			sb.Append("\n\t");
			for (var i = 0; i < Rows.Count; i++)
			{
				// limit number of printed records
				if (i == 10)
				{
					sb.Append($"-- skipping... total rows: {Rows.Count}");
					break;
				}

				if (i > 0)
					sb.Append(",\n\t)");

				sb.Append("(");
				for (var j = 0; j < Fields.Count; j++)
				{
					if (j > 0)
						sb.Append(", ");

					sb = Rows[i][j].ToString(sb, dic);
				}

				sb.Append(")");
			}

			sb.Append("\n");

			return sb;
		}
		#endregion

		#region IEquatable
		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region ISqlExpressionWalkable
		ISqlExpression ISqlExpressionWalkable.Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
