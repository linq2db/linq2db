using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using LinqToDB.Expressions;

namespace LinqToDB.SqlQuery
{
	public class SqlValuesTable : ISqlTableSource
	{
		/// <summary>
		/// To create new instance in build context.
		/// </summary>
		/// <param name="source">Expression, that contains enumerable source.</param>
		internal SqlValuesTable(ISqlExpression source)
		{
			Source        = source;
			ValueBuilders = new Dictionary<string, Func<object, IDictionary<Expression, ISqlExpression>, ISqlExpression>>();
			FieldsLookup  = new Dictionary<string, SqlField>();
		}

		/// <summary>
		/// Constructor for convert visitor.
		/// </summary>
		internal SqlValuesTable(ISqlExpression source, Dictionary<string, Func<object, IDictionary<Expression, ISqlExpression>, ISqlExpression>> valueBuilders, IEnumerable<SqlField> fields, IReadOnlyList<ISqlExpression[]>? rows)
		{
			Source        = source;
			ValueBuilders = valueBuilders;

			foreach (var field in fields)
			{
				if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");
				_fields.Add(field);
			}

			if (rows != null)
			{
				IsRowsBuilt = true;
				Rows       = rows;
			}
		}

		/// <summary>
		/// Constructor for remote context.
		/// </summary>
		internal SqlValuesTable(SqlField[] fields, IReadOnlyList<ISqlExpression[]> rows)
		{
			IsRowsBuilt = true;
			Rows       = rows;

			foreach (var field in fields)
			{
				if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");
				_fields.Add(field);
			}
		}

		/// <summary>
		/// Source value expression.
		/// </summary>
		internal ISqlExpression? Source { get; }

		/// <summary>
		/// Used only during build.
		/// </summary>
		internal Dictionary<string, SqlField>? FieldsLookup { get; }

		private readonly List<SqlField> _fields = new ();

		// Fields from source, used in query. Columns in rows should have same order.
		public List<SqlField> Fields => _fields;

		internal Dictionary<string, Func<object, IDictionary<Expression, ISqlExpression>, ISqlExpression>>? ValueBuilders { get; }

		internal void Add(SqlField field, Func<object, IDictionary<Expression, ISqlExpression>, ISqlExpression> valueBuilder)
		{
			if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");

			field.Table = this;
			_fields.Add(field);

			FieldsLookup !.Add(field.Name, field);
			ValueBuilders!.Add(field.Name, valueBuilder);
		}

		internal IReadOnlyList<ISqlExpression[]>? Rows { get; }

		public bool IsRowsBuilt { get; }

		internal SqlValuesTable BuildRows(EvaluationContext context)
		{
			if (IsRowsBuilt || context.ParameterValues == null)
				return this;

			var parameters = new Dictionary<Expression, ISqlExpression>(ExpressionEqualityComparer.Instance);

			// rows pre-build for remote context

			if (!(Source?.EvaluateExpression(context) is IEnumerable source))
				throw new LinqToDBException($"Merge source must be enumerable: {Source}");

			var rows = new List<ISqlExpression[]>();

			foreach (var record in source)
			{
				if (record == null)
					throw new LinqToDBException("Merge source cannot hold null records");

				var row = new ISqlExpression[ValueBuilders!.Count];
				var idx = 0;
				rows.Add(row);

				foreach (var valueBuilder in ValueBuilders!.Values)
				{
					row[idx] = valueBuilder(record, parameters);
					idx++;
				}
			}


			return new SqlValuesTable(_fields.Select(f => new SqlField(f)).ToArray(), rows);
		}

		#region ISqlTableSource
		private SqlField? _all;
		SqlField ISqlTableSource.All => _all ??= SqlField.All(this);

		int ISqlTableSource.SourceID => throw new NotImplementedException();

		SqlTableType ISqlTableSource.SqlTableType => SqlTableType.Values;

		IList<ISqlExpression> ISqlTableSource.GetKeys(bool allIfEmpty) => throw new NotImplementedException();
		#endregion

		#region ISqlExpression

		bool ISqlExpression.CanBeNull => throw new NotImplementedException();

		int ISqlExpression.Precedence => throw new NotImplementedException();

		Type ISqlExpression.SystemType => throw new NotImplementedException();

		bool ISqlExpression.Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer) => throw new NotImplementedException();

		#endregion

		#region IQueryElement
		QueryElementType IQueryElement.ElementType => QueryElementType.SqlValuesTable;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			if (Rows == null)
				return sb;

			sb.Append("\n\t");
			var rows = Rows!;
			for (var i = 0; i < rows.Count; i++)
			{
				// limit number of printed records
				if (i == 10)
				{
					sb.Append($"-- skipping... total rows: {rows.Count}");
					break;
				}

				if (i > 0)
					sb.Append(",\n\t)");

				sb.Append('(');
				for (var j = 0; j < Fields.Count; j++)
				{
					if (j > 0)
						sb.Append(", ");

					sb = rows[i][j].ToString(sb, dic);
				}

				sb.Append(')');
			}

			sb.Append('\n');

			return sb;
		}
		#endregion

		#region IEquatable
		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other) => throw new NotImplementedException();
		#endregion

		#region ISqlExpressionWalkable
		ISqlExpression ISqlExpressionWalkable.Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func) => throw new NotImplementedException();
		#endregion
	}
}
