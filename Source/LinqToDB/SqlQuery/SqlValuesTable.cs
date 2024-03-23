using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

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
			FieldsLookup  = new();

			SourceID = Interlocked.Increment(ref SelectQuery.SourceIDCounter);
		}

		/// <summary>
		/// Constructor for convert visitor.
		/// </summary>
		internal SqlValuesTable(ISqlExpression? source, List<Func<object, ISqlExpression>>? valueBuilders, IEnumerable<SqlField> fields, List<ISqlExpression[]>? rows)
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
		internal SqlValuesTable(SqlField[] fields, MemberInfo?[]? members, List<ISqlExpression[]> rows)
		{
			Rows         = rows;
			FieldsLookup = new();

			foreach (var field in fields)
			{
				if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");
				_fields.Add(field);
				field.Table = this;
			}

			if (members != null)
			{
				for (var index = 0; index < fields.Length; index++)
				{
					var member = members[index];
					if (member != null)
					{
						var field = fields[index];
						FieldsLookup.Add(member, field);
					}
				}
			}

			SourceID = Interlocked.Increment(ref SelectQuery.SourceIDCounter);
		}

		/// <summary>
		/// Source value expression.
		/// </summary>
		internal ISqlExpression? Source { get; }

		/// <summary>
		/// Used only during build.
		/// </summary>
		internal Dictionary<MemberInfo, SqlField>? FieldsLookup { get; set; }

		private readonly List<SqlField> _fields = new ();

		// Fields from source, used in query. Columns in rows should have same order.
		public List<SqlField> Fields => _fields;

		internal List<Func<object, ISqlExpression>>? ValueBuilders { get; set; }

		internal void Add(SqlField field, MemberInfo? memberInfo, Func<object, ISqlExpression> valueBuilder)
		{
			if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");

			field.Table = this;
			_fields.Add(field);

			if (memberInfo != null)
				FieldsLookup!.Add(memberInfo, field);

			ValueBuilders ??= new List<Func<object, ISqlExpression>>();
			ValueBuilders.Add(valueBuilder);
		}

		internal List<ISqlExpression[]>? Rows { get; private set; }

		internal IReadOnlyList<ISqlExpression[]> BuildRows(EvaluationContext context)
		{
			if (Rows != null)
				return Rows;

			// rows pre-build for remote context

			if (!(Source?.EvaluateExpression(context) is IEnumerable source))
				throw new LinqToDBException($"Source must be enumerable: {Source}");

			var rows = new List<ISqlExpression[]>();

			if (ValueBuilders != null)
			{
				foreach (var record in source)
				{
					if (record == null)
						throw new LinqToDBException("Merge source cannot hold null records");

					var row = new ISqlExpression[ValueBuilders!.Count];
					var idx = 0;
					rows.Add(row);

					foreach (var valueBuilder in ValueBuilders!)
					{
						row[idx] = valueBuilder(record);
						idx++;
					}
				}
			}

			return rows;
		}

		#region ISqlTableSource
		private SqlField? _all;
		SqlField ISqlTableSource.All => _all ??= SqlField.All(this);

		public int SourceID { get; }

		SqlTableType ISqlTableSource.SqlTableType => SqlTableType.Values;

		IList<ISqlExpression> ISqlTableSource.GetKeys(bool allIfEmpty)
		{
			return _fields.ToArray();
		}

		#endregion

		#region ISqlExpression

		public bool CanBeNullable(NullabilityContext nullability) => throw new NotImplementedException();

		int ISqlExpression.Precedence => throw new NotImplementedException();

		Type ISqlExpression.SystemType => typeof(object);

		bool ISqlExpression.Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer) => throw new NotImplementedException();

		#endregion

		#region IQueryElement

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif

		QueryElementType IQueryElement.ElementType => QueryElementType.SqlValuesTable;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
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
		#endregion

		#region IEquatable
		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other) => throw new NotImplementedException();
		#endregion
	}
}
