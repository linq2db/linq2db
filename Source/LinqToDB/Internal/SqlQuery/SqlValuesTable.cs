using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlValuesTable : SqlExpressionBase, ISqlTableSource
	{
		/// <summary>
		/// To create new instance in build context.
		/// </summary>
		internal SqlValuesTable()
		{
			Source       = null;

			SourceID = Interlocked.Increment(ref SelectQuery.SourceIDCounter);
		}

		/// <summary>
		/// To create new instance in build context.
		/// </summary>
		/// <param name="source">Expression, that contains enumerable source.</param>
		internal SqlValuesTable(ISqlExpression source)
		{
			Source        = source;

			SourceID = Interlocked.Increment(ref SelectQuery.SourceIDCounter);
		}

		/// <summary>
		/// Constructor for convert visitor.
		/// </summary>
		internal SqlValuesTable(ISqlExpression? source, List<Func<object, ISqlExpression>>? valueBuilders, IEnumerable<SqlField> fields, List<List<ISqlExpression>>? rows)
		{
			Source        = source;
			ValueBuilders = valueBuilders;
			Rows          = rows;

			foreach (var field in fields)
			{
				if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");
				_fields.Add(field);
				field.Table = this;
			}

			SourceID = Interlocked.Increment(ref SelectQuery.SourceIDCounter);
		}

		/// <summary>
		/// Constructor for remote context.
		/// </summary>
		internal SqlValuesTable(SqlField[] fields, List<List<ISqlExpression>> rows)
		{
			Rows = rows;

			foreach (var field in fields)
			{
				if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");
				_fields.Add(field);
				field.Table = this;
			}

			SourceID = Interlocked.Increment(ref SelectQuery.SourceIDCounter);
		}

		/// <summary>
		/// Source value expression.
		/// </summary>
		internal ISqlExpression? Source { get; private set; }

		private readonly List<SqlField> _fields = new ();

		// Fields from source, used in query. Columns in rows should have same order.
		public List<SqlField> Fields => _fields;

		internal List<Func<object, ISqlExpression>>? ValueBuilders { get; set; }

		internal void AddFieldWithValueBuilder(SqlField field, Func<object, ISqlExpression> valueBuilder)
		{
			if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");

			field.Table = this;
			_fields.Add(field);

			ValueBuilders ??= new List<Func<object, ISqlExpression>>();
			ValueBuilders.Add(valueBuilder);
		}

		internal List<List<ISqlExpression>>? Rows { get; set; }

		/// <summary>
		/// Resolved temp-table configuration — per-call AsQueryable chain merged with the
		/// <see cref="LinqToDB.DataOptions"/>'s <c>UseTempTablesForLocalCollections</c> default
		/// at AST-build time. When non-null with a Threshold, the SQL builder routes this VALUES
		/// table through a temporary table whenever the materialized row count exceeds
		/// <c>Spec.Threshold</c>. The AST itself stays immutable through SQL building — the
		/// per-emission name lives in <see cref="TempTableName"/>; the per-execute decision lives
		/// in the <c>QueryExecutionContext</c>.
		/// </summary>
		internal TempTableSpec? TempTableSpec { get; set; }

		/// <summary>
		/// Element type of the configured AsQueryable source — what <c>CreateTable&lt;T&gt;</c> /
		/// <c>BulkCopy</c> needs at execute time. Stashed by <c>EnumerableBuilder</c> alongside
		/// <see cref="TempTableSpec"/> so the temp-table run-step doesn't need to recover the
		/// type from <see cref="Source"/>'s SystemType (which is the parameter's value type, not
		/// the element type for non-generic <c>IEnumerable</c> sources or arrays).
		/// </summary>
		internal Type? TempTableElementType { get; set; }

		/// <summary>
		/// Temp-table name assigned by <c>EnumerableBuilder</c> at AST construction time when a
		/// <see cref="TempTableSpec"/> is stamped. Stable across the lifetime of the cached
		/// <c>Query&lt;T&gt;</c> and propagated through visitor transformations. The SQL builder
		/// reads it — it never writes it (no AST mutation during SQL building). Self-join siblings
		/// over the same source share one name via <c>ExpressionBuilder</c>'s per-source map.
		/// </summary>
		internal string? TempTableName { get; set; }

		internal IReadOnlyList<List<ISqlExpression>> BuildRows(EvaluationContext context)
		{
			if (Rows != null)
				return Rows;

			// rows pre-build for remote context

			if (Source?.EvaluateExpression(context) is not IEnumerable source)
				throw new LinqToDBException($"Source must be enumerable: {Source}");

			return BuildRowsFromSource(ValueBuilders, source);
		}

		/// <summary>
		/// Applies the given <paramref name="valueBuilders"/> to the records in
		/// <paramref name="source"/> and returns the resulting SQL row representations. Shared
		/// between <see cref="BuildRows"/> (the standard SQL-emit path) and the
		/// <c>UseInlineValues</c> branch in <c>BasicSqlBuilder.BuildSqlValuesTable</c> when the
		/// init-query Setup decided to fall back to inline VALUES — the materialized source items
		/// flow in as <paramref name="source"/> directly from the captured
		/// <c>QueryExecutionContext</c> decision so we don't iterate the user's IEnumerable twice.
		/// </summary>
		internal static List<List<ISqlExpression>> BuildRowsFromSource(List<Func<object, ISqlExpression>>? valueBuilders, IEnumerable source)
		{
			var rows = source is ICollection coll
				? new List<List<ISqlExpression>>(coll.Count)
				: new List<List<ISqlExpression>>();

			if (valueBuilders != null)
			{
				foreach (var record in source)
				{
					if (record == null)
						throw new LinqToDBException("Merge source cannot hold null records");

					var row = new List<ISqlExpression>(valueBuilders.Count);
					rows.Add(row);

					foreach (var valueBuilder in valueBuilders)
						row.Add(valueBuilder(record));
				}
			}
			else
			{
				// columns are not used, but cardinality must be preserved
				foreach (var record in source)
				{
					if (record == null)
						throw new LinqToDBException("Merge source cannot hold null records");

					rows.Add([]);
				}
			}

			return rows;
		}

		#region ISqlTableSource
		SqlField ISqlTableSource.All => field ??= SqlField.All(this);

		public int SourceID { get; }

		SqlTableType ISqlTableSource.SqlTableType => SqlTableType.Values;

		IList<ISqlExpression> ISqlTableSource.GetKeys(bool allIfEmpty)
		{
			return _fields.ToArray();
		}

		#endregion

		#region ISqlExpression

		public override bool CanBeNullable(NullabilityContext nullability) => throw new NotSupportedException();

		public override int Precedence => throw new NotSupportedException();

		public override Type SystemType => typeof(object);

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer) => throw new NotSupportedException();

		#endregion

		#region IQueryElement

		public override QueryElementType ElementType => QueryElementType.SqlValuesTable;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			var rows = Rows;
			if (rows?.Count > 0)
			{
				writer.Append("VALUES");
				writer.AppendLine();

				using (writer.IndentScope())
					for (var i = 0; i < rows.Count; i++)
					{
						// limit number of printed records
						if (i == 10)
						{
							writer
								.Append("-- skipping... total rows: ")
								.Append(rows.Count);
							break;
						}

						if (i > 0)
							writer.AppendLine(',');

						writer.Append('(');

						for (var j = 0; j < Fields.Count; j++)
						{
							if (j > 0)
								writer.Append(", ");

							writer.AppendElement(rows[i][j]);
						}

						writer.Append(')');
					}

				writer.AppendLine();
			}
			else
			{
				writer.Append("VALUES (...)");
			}

			writer.Append('[');

			for (var i = 0; i < Fields.Count; i++)
			{
				if (i > 0)
					writer.Append(", ");
				writer.Append(Fields[i].PhysicalName);
			}

			writer.Append(']');

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(SourceID);
			hash.Add(Source?.GetElementHashCode() ?? 0);
			
			foreach (var field in Fields)
				hash.Add(field.GetElementHashCode());

			if (Rows != null)
			{
				foreach (var row in Rows)
				{
					foreach (var expr in row)
						hash.Add(expr.GetElementHashCode());
				}
			}

			return hash.ToHashCode();
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlValuesTable(this);

		#endregion

		#region IEquatable
		public override bool Equals([NotNullWhen(true)] ISqlExpression? other) => throw new NotSupportedException();
		#endregion

		public void Modify(ISqlExpression? source)
		{
			Source = source;
		}

		public void AddField(SqlField field)
		{
			Fields.Add(field);
			field.Table = this;
		}

		public void RemoveField(int fieldIndex)
		{
			if (fieldIndex < 0 || fieldIndex >= Fields.Count)
				throw new ArgumentOutOfRangeException(nameof(fieldIndex));

			Fields.RemoveAt(fieldIndex);
			ValueBuilders?.RemoveAt(fieldIndex);

			if (Rows != null)
			{
				foreach (var row in Rows)
				{
					row.RemoveAt(fieldIndex);
				}
			}
		}
	}
}
