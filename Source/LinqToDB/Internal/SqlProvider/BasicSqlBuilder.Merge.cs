using System;
using System.Collections.Generic;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlProvider
{
	public abstract partial class BasicSqlBuilder
	{
		/// <summary>
		/// If true, provider supports column aliases specification after table alias.
		/// E.g. as table_alias (column_alias1, column_alias2).
		/// </summary>
		protected virtual bool SupportsColumnAliasesInSource => true;

		/// <summary>
		/// If true, provider require column aliases for each  column.
		/// E.g. as table_alias (column_alias1, column_alias2).
		/// </summary>
		protected virtual bool RequiresConstantColumnAliases => false;

		/// <summary>
		/// If true, provider supports list of VALUES as a source element of merge command.
		/// </summary>
		protected virtual bool IsValuesSyntaxSupported => true;

		/// <summary>
		/// If true, builder will generate command for empty enumerable source;
		/// Otherwise exception will be generated.
		/// </summary>
		protected virtual bool IsEmptyValuesSourceSupported => true;

		/// <summary>
		/// If <see cref="IsValuesSyntaxSupported"/> set to false and provider doesn't support SELECTs without
		/// FROM clause, this property should contain name of table (or equivalent SQL) with single record.
		/// IMPORTANT: as this property could return SQL, we don't escape it, so it should contain only valid SQL/identifiers.
		/// </summary>
		protected virtual string? FakeTable => null;

		/// <summary>
		/// If <see cref="IsValuesSyntaxSupported"/> set to false and provider doesn't support SELECTs without
		/// FROM clause, this property could contain name of schema for table with single record.
		/// Returned name should be already escaped if escaping required.
		/// </summary>
		protected virtual string? FakeTableSchema => null;

		protected virtual void BuildMergeStatement(SqlMergeStatement merge)
		{
			var nullability = new NullabilityContext(merge.SelectQuery);

			BuildTag(merge);

			BuildWithClause(merge.With);
			BuildMergeInto(nullability, merge);
			BuildMergeSource(nullability, merge);
			BuildMergeOn(nullability, merge);

			foreach (var operation in merge.Operations)
				BuildMergeOperation(nullability, operation);

			BuildOutputSubclause(merge.Output);

			BuildSubQueryExtensions(merge);
			BuildQueryExtensions   (merge);
			BuildMergeTerminator   (nullability, merge);
		}

		/// <summary>
		/// Allows to add text after generated merge command. E.g. to specify command terminator if provider requires it.
		/// </summary>
		protected virtual void BuildMergeTerminator(NullabilityContext nullability, SqlMergeStatement merge)
		{
		}

		private void BuildMergeOperation(NullabilityContext nullability, SqlMergeOperationClause operation)
		{
			switch (operation.OperationType)
			{
				case MergeOperationType.Update:
					BuildMergeOperationUpdate(nullability, operation);
					break;
				case MergeOperationType.Delete:
					BuildMergeOperationDelete(nullability, operation);
					break;
				case MergeOperationType.Insert:
					BuildMergeOperationInsert(nullability, operation);
					break;
				case MergeOperationType.UpdateWithDelete:
					BuildMergeOperationUpdateWithDelete(nullability, operation);
					break;
				case MergeOperationType.DeleteBySource:
					BuildMergeOperationDeleteBySource(nullability, operation);
					break;
				case MergeOperationType.UpdateBySource:
					BuildMergeOperationUpdateBySource(nullability, operation);
					break;
				default:
					throw new InvalidOperationException($"Unknown merge operation type: {operation.OperationType}");
			}
		}

		protected virtual void BuildMergeOperationUpdate(NullabilityContext nullability, SqlMergeOperationClause operation)
		{
			StringBuilder
				.AppendLine()
				.Append("WHEN MATCHED");

			if (operation.Where != null)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where, wrapCondition : true);
			}

			StringBuilder.AppendLine(" THEN");
			StringBuilder.AppendLine("UPDATE");

			var update = new SqlUpdateClause();
			update.Items.AddRange(operation.Items);

			var saveStep = BuildStep;
			BuildStep = Step.MergeUpdateClause;

			BuildUpdateSet(null, update);

			BuildStep = saveStep;
		}

		protected virtual void BuildMergeOperationDelete(NullabilityContext nullability, SqlMergeOperationClause operation)
		{
			StringBuilder
				.Append("WHEN MATCHED");

			if (operation.Where != null)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where, wrapCondition : true);
			}

			StringBuilder.AppendLine(" THEN DELETE");
		}

		protected virtual void BuildMergeOperationInsert(NullabilityContext nullability, SqlMergeOperationClause operation)
		{
			StringBuilder
				.AppendLine()
				.Append("WHEN NOT MATCHED");

			if (operation.Where != null)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where, wrapCondition : true);
			}

			StringBuilder
				.AppendLine(" THEN")
				.Append("INSERT");

			var insertClause = new SqlInsertClause();
			insertClause.Items.AddRange(operation.Items);

			var saveStep = BuildStep;

			BuildStep = Step.MergeInsertClause;

			BuildInsertClause(new SqlInsertOrUpdateStatement(null), insertClause, null, false, false);

			BuildStep = saveStep;
		}

		protected virtual void BuildMergeOperationUpdateWithDelete(NullabilityContext nullability, SqlMergeOperationClause operation)
		{
			// Oracle-specific operation
			throw new NotSupportedException($"Merge operation {operation.OperationType} is not supported by {Name}");
		}

		protected virtual void BuildMergeOperationDeleteBySource(NullabilityContext nullability, SqlMergeOperationClause operation)
		{
			// SQL Server-specific operation
			throw new NotSupportedException($"Merge operation {operation.OperationType} is not supported by {Name}");
		}

		protected virtual void BuildMergeOperationUpdateBySource(NullabilityContext nullability, SqlMergeOperationClause operation)
		{
			// SQL Server-specific operation
			throw new NotSupportedException($"Merge operation {operation.OperationType} is not supported by {Name}");
		}

		protected virtual void BuildMergeOn(NullabilityContext nullability, SqlMergeStatement mergeStatement)
		{
			StringBuilder.Append("ON (");

			BuildSearchCondition(Precedence.Unknown, mergeStatement.On, wrapCondition : true);

			StringBuilder.AppendLine(")");
		}

		protected virtual void BuildMergeSourceQuery(NullabilityContext nullability, SqlTableLikeSource mergeSource)
		{
			BuildPhysicalTable(mergeSource.Source, null);

			BuildMergeAsSourceClause(mergeSource, false);
		}

		private void BuildMergeAsSourceClause(SqlTableLikeSource mergeSource, bool forceAlias)
		{
			StringBuilder.Append(' ');

			BuildObjectName(StringBuilder, new (mergeSource.Name), ConvertType.NameToQueryTable, true, TableOptions.NotSet);

			if (SupportsColumnAliasesInSource && (forceAlias || mergeSource.SourceFields.Count > 0))
			{
				StringBuilder.AppendLine();
				StringBuilder.AppendLine(OpenParens);

				++Indent;

				if (mergeSource.SourceFields.Count == 0)
				{
					Convert(StringBuilder, "Unused", ConvertType.NameToQueryField);
				}
				else
				{
					var first = true;
					foreach (var field in mergeSource.SourceFields)
					{
						if (!first)
							StringBuilder.AppendLine(Comma);

						first = false;
						AppendIndent();
						Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
					}
				}

				--Indent;

				StringBuilder.AppendLine();

				StringBuilder.Append(')');
			}
		}

		private void BuildMergeSourceEnumerable(SqlMergeStatement merge)
		{
			var sourceEnumerable = ConvertElement(merge.Source.SourceEnumerable);
			var rows             = sourceEnumerable!.BuildRows(OptimizationContext.EvaluationContext);
			if (rows.Count > 0)
			{
				StringBuilder.Append('(');

				if (IsValuesSyntaxSupported)
					BuildValues(sourceEnumerable, rows);
				else
					BuildValuesAsSelectsUnion(merge.Source.SourceFields, sourceEnumerable, rows);

				StringBuilder.Append(')');
			}
			else if (IsEmptyValuesSourceSupported)
				BuildMergeEmptySource(merge);
			else
				throw new LinqToDBException($"{Name} doesn't support merge with empty source");

			BuildMergeAsSourceClause(merge.Source, true);
		}

		/// <summary>
		/// Checks that value in specific row and column in enumerable source requires type information generation.
		/// </summary>
		/// <param name="source">Merge source table.</param>
		/// <param name="rows">Merge source data.</param>
		/// <param name="row">Index of data row to check. Could contain -1 to indicate that this is a check for empty source NULL value.</param>
		/// <param name="column">Index of data column to check in row.</param>
		/// <returns>Returns <see langword="true"/>, if generated SQL should include type information for value at specified position, otherwise <see langword="false"/> returned.</returns>
		protected virtual bool IsSqlValuesTableValueTypeRequired(SqlValuesTable source, IReadOnlyList<List<ISqlExpression>> rows, int row, int column) => false;

		private void BuildValuesAsSelectsUnion(IList<SqlField> sourceFields, SqlValuesTable source, IReadOnlyList<List<ISqlExpression>> rows)
		{
			var columnTypes = new DbDataType[sourceFields.Count];
			for (var i = 0; i < sourceFields.Count; i++)
				columnTypes[i] = sourceFields[i].Type;

			StringBuilder
				.AppendLine();
			AppendIndent();

			for (var i = 0; i < rows.Count; i++)
			{
				if (i > 0)
				{
					StringBuilder
						.AppendLine();
					AppendIndent();
					StringBuilder.AppendLine("\tUNION ALL");
					AppendIndent();
				}

				// build record select
				StringBuilder.Append("\tSELECT ");

				var row = rows[i];

				if (row.Count == 0)
				{
					StringBuilder.Append('1');
				}
				else
				{
					for (var fieldIndex = 0; fieldIndex < row.Count; fieldIndex++)
					{
						var value = row[fieldIndex];
						if (fieldIndex > 0)
							StringBuilder.Append(InlineComma);

						if (IsSqlValuesTableValueTypeRequired(source, rows, i, fieldIndex))
							BuildTypedExpression(columnTypes[fieldIndex], value);
						else
							BuildExpression(value);

						// add aliases only for first row
						if (RequiresConstantColumnAliases || i == 0)
						{
							StringBuilder.Append(" AS ");
							Convert(StringBuilder, sourceFields[fieldIndex].PhysicalName, ConvertType.NameToQueryField);
						}
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

			if (merge.Source.SourceFields.Count == 0)
			{
				StringBuilder.Append("1 AS ");
				Convert(StringBuilder, "Unused", ConvertType.NameToQueryField);
			}
			else
			{
				for (var i = 0; i < merge.Source.SourceFields.Count; i++)
				{
					var field = merge.Source.SourceFields[i];

					if (i > 0)
						StringBuilder.Append(InlineComma);

					if (IsSqlValuesTableValueTypeRequired(merge.Source.SourceEnumerable!, [], -1, i))
						BuildTypedExpression(field.Type, new SqlValue(field.Type, null));
					else
						BuildExpression(new SqlValue(field.Type, null));

					if (!SupportsColumnAliasesInSource)
					{
						StringBuilder.Append(' ');
						Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
					}
				}
			}

			StringBuilder
				.AppendLine()
				.Append("\tFROM ");

			if (!BuildFakeTableName())
				// we don't select anything, so it is ok to use target table
				BuildTableName(merge.Target, true, false);

			StringBuilder
				.AppendLine(" WHERE 1 = 0")
				.AppendLine(")");
		}

		protected virtual bool BuildFakeTableName()
		{
			if (FakeTable == null)
				return false;

			// NO ESCAPING!
			BuildObjectName(StringBuilder, new (FakeTable, Schema: FakeTableSchema), ConvertType.NameToQueryTable, false, TableOptions.NotSet);
			return true;
		}

		protected void BuildValues(SqlValuesTable source, IReadOnlyList<List<ISqlExpression>> rows)
		{
			if (rows.Count == 0)
				return;

			var columnTypes = new DbDataType[source.Fields.Count];
			for (var i = 0; i < source.Fields.Count; i++)
				columnTypes[i] = source.Fields[i].Type;

			StringBuilder.AppendLine("VALUES");

			++Indent;

			AppendIndent();

			var currentRowLength = 0;

			for (var i = 0; i < rows.Count; i++)
			{
				var currentPos = StringBuilder.Length;
				StringBuilder.Append(OpenParens);
				var row = rows[i];

				if (columnTypes.Length == 0)
				{
					StringBuilder.Append('1');
				}
				else
				{
					for (var j = 0; j < columnTypes.Length; j++)
					{
						var value = row[j];
						if (j > 0)
							StringBuilder.Append(Comma);

						if (IsSqlValuesTableValueTypeRequired(source, rows, i, j))
							BuildTypedExpression(columnTypes[j], value);
						else
							BuildExpression(value);
					}
				}

				StringBuilder.Append(')');

				var rowLength = StringBuilder.Length - currentPos;

				currentRowLength += rowLength;

				if (i < rows.Count - 1)
				{
					if (currentRowLength + rowLength / 2 > 50)
					{
						StringBuilder.Append(Comma).AppendLine();
						currentRowLength = 0;
						AppendIndent();
					}
					else
					{
						StringBuilder.Append(InlineComma);
					}
				}
			}

			--Indent;

			StringBuilder.AppendLine();
			AppendIndent();
		}

		private void BuildMergeSource(NullabilityContext nullability, SqlMergeStatement merge)
		{
			StringBuilder.Append("USING ");

			var buildAsEnumerable = merge.Source.SourceEnumerable != null;
			if (buildAsEnumerable)
			{
				if (merge.Source.SourceQuery != null)
				{
					if (!merge.Source.SourceQuery.IsSimple || merge.Source.SourceQuery.Select.Columns.Count != merge.Source.SourceFields.Count)
						buildAsEnumerable = false;
					else
					{
						for (var i = 0; i < merge.Source.SourceQuery.Select.Columns.Count; i++)
						{
							var columnExp = merge.Source.SourceQuery.Select.Columns[i].Expression;
							if (columnExp is not SqlField)
							{
								buildAsEnumerable = false;
								break;
							}
						}
					}
				}
			}

			if (buildAsEnumerable)
				BuildMergeSourceEnumerable(merge);
			else
				BuildMergeSourceQuery(nullability, merge.Source);

			StringBuilder.AppendLine();
		}

		protected virtual void BuildMergeInto(NullabilityContext nullability, SqlMergeStatement merge)
		{
			StringBuilder.Append("MERGE INTO ");
			BuildTableName(merge.Target, true, true);
			StringBuilder.AppendLine();
		}
	}
}
