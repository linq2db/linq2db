using System;
using System.Collections.Generic;

namespace LinqToDB.SqlProvider
{
	using LinqToDB.Common;
	using SqlQuery;

	public abstract partial class BasicSqlBuilder : ISqlBuilder
	{
		/// <summary>
		/// If true, provider supports column aliases specification after table alias.
		/// E.g. as table_alias (column_alias1, column_alias2).
		/// </summary>
		protected virtual bool MergeSupportsColumnAliasesInSource => true;

		/// <summary>
		/// If true, provider supports list of VALUES as a source element of merge command.
		/// </summary>
		protected virtual bool MergeSupportsSourceDirectValues => true;

		/// <summary>
		/// If true, builder will generate command for empty enumerable source;
		/// Otherwise exception will be generated.
		/// </summary>
		protected virtual bool MergeEmptySourceSupported => true;

		/// <summary>
		/// If <see cref="MergeSupportsSourceDirectValues"/> set to false and provider doesn't support SELECTs without
		/// FROM clause, this property should contain name of table with single record.
		/// </summary>
		protected virtual string? FakeTable => null;

		/// <summary>
		/// If <see cref="MergeSupportsSourceDirectValues"/> set to false and provider doesn't support SELECTs without
		/// FROM clause, this property could contain name of schema for table with single record.
		/// </summary>
		protected virtual string? FakeTableSchema => null;

		protected virtual void BuildMergeStatement(SqlMergeStatement merge)
		{
			BuildTag(merge);

			BuildMergeInto(merge);
			BuildMergeSource(merge);
			BuildMergeOn(merge);

			foreach (var operation in merge.Operations)
				BuildMergeOperation(operation);

			BuildMergeTerminator(merge);
		}

		/// <summary>
		/// Allows to add text after generated merge command. E.g. to specify command terminator if provider requires it.
		/// </summary>
		protected virtual void BuildMergeTerminator(SqlMergeStatement merge)
		{
		}

		private void BuildMergeOperation(SqlMergeOperationClause operation)
		{
			switch (operation.OperationType)
			{
				case MergeOperationType.Update:
					BuildMergeOperationUpdate(operation);
					break;
				case MergeOperationType.Delete:
					BuildMergeOperationDelete(operation);
					break;
				case MergeOperationType.Insert:
					BuildMergeOperationInsert(operation);
					break;
				case MergeOperationType.UpdateWithDelete:
					BuildMergeOperationUpdateWithDelete(operation);
					break;
				case MergeOperationType.DeleteBySource:
					BuildMergeOperationDeleteBySource(operation);
					break;
				case MergeOperationType.UpdateBySource:
					BuildMergeOperationUpdateBySource(operation);
					break;
				default:
					throw new InvalidOperationException($"Unknown merge operation type: {operation.OperationType}");
			}
		}

		protected virtual void BuildMergeOperationUpdate(SqlMergeOperationClause operation)
		{
			StringBuilder
				.AppendLine()
				.Append("WHEN MATCHED");

			if (operation.Where != null)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where, wrapCondition: true);
			}

			StringBuilder.AppendLine(" THEN");
			StringBuilder.AppendLine("UPDATE");

			var update = new SqlUpdateClause();
			update.Items.AddRange(operation.Items);
			BuildUpdateSet(null, update);
		}

		protected virtual void BuildMergeOperationDelete(SqlMergeOperationClause operation)
		{
			StringBuilder
				.Append("WHEN MATCHED");

			if (operation.Where != null)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where, wrapCondition: true);
			}

			StringBuilder.AppendLine(" THEN DELETE");
		}

		protected virtual void BuildMergeOperationInsert(SqlMergeOperationClause operation)
		{
			StringBuilder
				.AppendLine()
				.Append("WHEN NOT MATCHED");

			if (operation.Where != null)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where, wrapCondition: true);
			}

			StringBuilder
				.AppendLine(" THEN")
				.Append("INSERT");


			var insertClause = new SqlInsertClause();
			insertClause.Items.AddRange(operation.Items);

			BuildInsertClause(new SqlInsertOrUpdateStatement(null), insertClause, null, false, false);
		}

		protected virtual void BuildMergeOperationUpdateWithDelete(SqlMergeOperationClause operation)
		{
			// Oracle-specific operation
			throw new NotSupportedException($"Merge operation {operation.OperationType} is not supported by {Name}");
		}

		protected virtual void BuildMergeOperationDeleteBySource(SqlMergeOperationClause operation)
		{
			// SQL Server-specific operation
			throw new NotSupportedException($"Merge operation {operation.OperationType} is not supported by {Name}");
		}

		protected virtual void BuildMergeOperationUpdateBySource(SqlMergeOperationClause operation)
		{
			// SQL Server-specific operation
			throw new NotSupportedException($"Merge operation {operation.OperationType} is not supported by {Name}");
		}

		protected virtual void BuildMergeOn(SqlMergeStatement mergeStatement)
		{
			StringBuilder.Append("ON (");

			BuildSearchCondition(Precedence.Unknown, mergeStatement.On, wrapCondition: true);

			StringBuilder.AppendLine(")");
		}

		protected virtual void BuildMergeSourceQuery(SqlTableLikeSource mergeSource)
		{
			mergeSource = ConvertElement(mergeSource);
			
			BuildPhysicalTable(mergeSource.Source, null);

			BuildMergeAsSourceClause(mergeSource);
		}

		private void BuildMergeAsSourceClause(SqlTableLikeSource mergeSource)
		{
			mergeSource = ConvertElement(mergeSource);
			StringBuilder.Append(' ');

			ConvertTableName(StringBuilder, null, null, null, mergeSource.Name, TableOptions.NotSet);

			if (MergeSupportsColumnAliasesInSource)
			{
				StringBuilder.AppendLine();
				StringBuilder.AppendLine(OpenParens);

				++Indent;

				var first = true;
				foreach (var field in mergeSource.SourceFields)
				{
					if (!first)
						StringBuilder.AppendLine(Comma);

					first = false;
					AppendIndent();
					Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
				}

				--Indent;

				StringBuilder.AppendLine();

				StringBuilder.Append(')');
			}
		}

		private void BuildMergeSourceEnumerable(SqlMergeStatement merge)
		{
			merge = ConvertElement(merge);
			var rows = merge.Source.SourceEnumerable!.Rows!;
			if (rows.Count > 0)
			{
				StringBuilder.Append('(');

				if (MergeSupportsSourceDirectValues)
					BuildValues(merge.Source.SourceEnumerable, rows);
				else
					BuildValuesAsSelectsUnion(merge.Source.SourceFields, merge.Source.SourceEnumerable, rows);

				StringBuilder.Append(')');
			}
			else if (MergeEmptySourceSupported)
				BuildMergeEmptySource(merge);
			else
				throw new LinqToDBException($"{Name} doesn't support merge with empty source");

			BuildMergeAsSourceClause(merge.Source);
		}

		/// <summary>
		/// Checks that value in specific row and column in enumerable source requires type information generation.
		/// </summary>
		/// <param name="source">Merge source table.</param>
		/// <param name="rows">Merge source data.</param>
		/// <param name="row">Index of data row to check. Could contain -1 to indicate that this is a check for empty source NULL value.</param>
		/// <param name="column">Index of data column to check in row.</param>
		/// <returns>Returns <c>true</c>, if generated SQL should include type information for value at specified position, otherwise <c>false</c> returned.</returns>
		protected virtual bool MergeSourceValueTypeRequired(SqlValuesTable source, IReadOnlyList<ISqlExpression[]> rows, int row, int column) => false;

		private void BuildValuesAsSelectsUnion(IList<SqlField> sourceFields, SqlValuesTable source, IReadOnlyList<ISqlExpression[]> rows)
		{
			var columnTypes = new SqlDataType[sourceFields.Count];
			for (var i = 0; i < sourceFields.Count; i++)
				columnTypes[i] = new SqlDataType(sourceFields[i]);

			for (var i = 0; i < rows.Count; i++)
			{
				if (i > 0)
					StringBuilder
						.AppendLine()
						.AppendLine("\tUNION ALL");

				// build record select
				StringBuilder.Append("\tSELECT ");

				var row = rows[i];
				for (var j = 0; j < row.Length; j++)
				{
					var value = row[j];
					if (j > 0)
						StringBuilder.Append(InlineComma);

					if (MergeSourceValueTypeRequired(source, rows, i, j))
						BuildTypedExpression(columnTypes[j], value);
					else
						BuildExpression(value);

					// add aliases only for first row
					if (!MergeSupportsColumnAliasesInSource && i == 0)
					{
						StringBuilder.Append(' ');
						Convert(StringBuilder, sourceFields[j].PhysicalName, ConvertType.NameToQueryField);
					}
				}

				if (FakeTable != null)
				{
					StringBuilder.Append(" FROM ");
					BuildFakeTableName();
				}
			}
		}

		private void BuildMergeEmptySource(SqlMergeStatement merge)
		{
			StringBuilder
				.AppendLine(OpenParens)
				.Append("\tSELECT ")
				;

			for (var i = 0; i < merge.Source.SourceFields.Count; i++)
			{
				var field = merge.Source.SourceFields[i];

				if (i > 0)
					StringBuilder.Append(InlineComma);

				if (MergeSourceValueTypeRequired(merge.Source.SourceEnumerable!, Array<ISqlExpression[]>.Empty, -1, i))
					BuildTypedExpression(new SqlDataType(field), new SqlValue(field.Type!.Value, null));
				else
					BuildExpression(new SqlValue(field.Type!.Value, null));

				if (!MergeSupportsColumnAliasesInSource)
				{
					StringBuilder.Append(' ');
					Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
				}
			}

			StringBuilder
				.AppendLine()
				.Append("\tFROM ");

			if (!BuildFakeTableName())
				// we don't select anything, so it is ok to use target table
				BuildTableName(merge.Target, true, false);

			StringBuilder
				.AppendLine("\tWHERE 1 = 0")
				.AppendLine(")");
		}

		protected virtual bool BuildFakeTableName()
		{
			if (FakeTable == null)
				return false;

			BuildTableName(StringBuilder, null, null, FakeTableSchema, FakeTable, TableOptions.NotSet);
			return true;
		}

		private void BuildValues(SqlValuesTable source, IReadOnlyList<ISqlExpression[]> rows)
		{
			var columnTypes = new SqlDataType[source.Fields.Count];
			for (var i = 0; i < source.Fields.Count; i++)
				columnTypes[i] = new SqlDataType(source.Fields[i]);

			for (var i = 0; i < rows.Count; i++)
			{
				var row = rows[i];

				if (i != 0)
					StringBuilder.AppendLine(Comma);
				else
					StringBuilder.AppendLine("\tVALUES");

				StringBuilder.Append("\t\t(");
				for (var j = 0; j < row.Length; j++)
				{
					var value = row[j];
					if (j > 0)
						StringBuilder.Append(InlineComma);

					if (MergeSourceValueTypeRequired(source, rows, i, j))
						BuildTypedExpression(columnTypes[j], value);
					else
						BuildExpression(value);
				}

				StringBuilder.Append(')');
			}
		}

		private void BuildMergeSource(SqlMergeStatement merge)
		{
			StringBuilder.Append("USING ");

			if (merge.Source.SourceQuery != null)
				BuildMergeSourceQuery(merge.Source);
			else
				BuildMergeSourceEnumerable(merge);

			StringBuilder.AppendLine();
		}

		protected virtual void BuildMergeInto(SqlMergeStatement merge)
		{
			StringBuilder.Append("MERGE INTO ");
			BuildTableName(merge.Target, true, true);
			StringBuilder.AppendLine();
		}
	}
}
