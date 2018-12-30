namespace LinqToDB.SqlProvider
{
	using System;
	using System.Collections.Generic;
	using SqlQuery;

	public abstract partial class BasicSqlBuilder : ISqlBuilder
	{
		/// <summary>
		/// If <c>false</c>, merge source subquery must inline parameters.
		/// </summary>
		protected virtual bool MergeSupportsParametersInSource => true;

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
		/// If <see cref="SupportsSourceDirectValues"/> set to false and provider doesn't support SELECTs without
		/// FROM clause, this property should contain name of table with single record.
		/// </summary>
		protected virtual string FakeTable => null;

		/// <summary>
		/// If <see cref="SupportsSourceDirectValues"/> set to false and provider doesn't support SELECTs without
		/// FROM clause, this property could contain name of database for table with single record.
		/// </summary>
		protected virtual string FakeTableDatabase => null;

		/// <summary>
		/// If <see cref="SupportsSourceDirectValues"/> set to false and provider doesn't support SELECTs without
		/// FROM clause, this property could contain name of schema for table with single record.
		/// </summary>
		protected virtual string FakeTableSchema => null;

		protected virtual void BuildMergeStatement(SqlMergeStatement mergeStatement)
		{
			BuildMergeInto(mergeStatement);
			BuildMergeSource(mergeStatement.Source);
			BuildMergeOn(mergeStatement);

			foreach (var operation in mergeStatement.Operations)
				BuildMergeOperation(operation);

			BuildMergeTerminator();
		}

		/// <summary>
		/// Allows to add text after generated merge command. E.g. to specify command terminator if provider requires it.
		/// </summary>
		protected virtual void BuildMergeTerminator()
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

			if (operation.Where.Conditions.Count != 0)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where);
			}

			//while (StringBuilder[Command.Length - 1] == ' ')
			//	StringBuilder.Length--;

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

			if (operation.Where.Conditions.Count != 0)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where);
			}

			StringBuilder.AppendLine(" THEN DELETE");
		}

		protected virtual void BuildMergeOperationInsert(SqlMergeOperationClause operation)
		{
			StringBuilder
				.AppendLine()
				.Append("WHEN NOT MATCHED");

			if (operation.Where.Conditions.Count != 0)
			{
				StringBuilder.Append(" AND ");
				BuildSearchCondition(Precedence.Unknown, operation.Where);
			}

			StringBuilder
				.AppendLine(" THEN")
				.Append("INSERT")
				;


			var insertClause = new SqlInsertClause();
			insertClause.Items.AddRange(operation.Items);

			// TODO: refactor BuildInsertClause?
			BuildInsertClause(new SqlInsertOrUpdateStatement(null), insertClause, null, false, false);
			//if (create != null)
			//	BuildCustomInsert(create);
			//else
			//{
			//	StringBuilder.AppendLine();
			//	BuildDefaultInsert();
			//}
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

			//if (Merge.KeyType != null)
			//	BuildPredicateByKeys(Merge.KeyType, Merge.TargetKey, Merge.SourceKey);
			//else
			//BuildPredicateByTargetAndSource(Merge.MatchPredicate ?? MakeDefaultMatchPredicate());

			BuildSearchCondition(Precedence.Unknown, mergeStatement.On);

			//while (Command[Command.Length - 1] == ' ')
			//	Command.Length--;

			StringBuilder.AppendLine(")");
		}

		protected virtual void BuildMergeSourceQuery(SqlMergeSourceTable mergeSource)
		{
			//var inlineParameters = _connection.InlineParameters;
			try
			{
				//_connection.InlineParameters = !MergeSupportsParametersInSource;

				///var ctx = queryableSource.GetMergeContext();

				//ctx.UpdateParameters();

				//var statement = ctx.GetResultStatement();

				//foreach (var columnInfo in ctx.Columns)
				//{
				//	var columnDescriptor = _sourceDescriptor.Columns.Single(_ => _.MemberInfo == columnInfo.Members[0]);
				//	var column = statement.SelectQuery.Select.Columns[columnInfo.Index];

				//	SetColumnAlias(column.Alias, columnDescriptor.ColumnName);
				//}

				//// bind parameters
				//statement.Parameters.Clear();
				//new QueryVisitor().VisitAll(ctx.SelectQuery, expr =>
				//{
				//	switch (expr.ElementType)
				//	{
				//		case QueryElementType.SqlParameter:
				//			{
				//				var p = (SqlParameter)expr;
				//				if (p.IsQueryParameter)
				//					statement.Parameters.Add(p);

				//				break;
				//			}
				//	}
				//});

				//ctx.SetParameters();

				//SaveParameters(statement.Parameters);

				BuildPhysicalTable(mergeSource.Source, null);

				//var cs = new[] { ' ', '\t', '\r', '\n' };

				//while (cs.Contains(Command[Command.Length - 1]))
				//	Command.Length--;
			}
			finally
			{
				//_connection.InlineParameters = inlineParameters;
			}

			BuildMergeAsSourceClause(mergeSource);
		}

		private void BuildMergeAsSourceClause(SqlMergeSourceTable mergeSource)
		{
			//StringBuilder
			//	.AppendLine()
			//	.Append(")");

			StringBuilder.Append(" ");

			ConvertTableName(StringBuilder, null, null, mergeSource.Name);

			if (MergeSupportsColumnAliasesInSource)
			{
				//if (mergeStatement.SourceFields.Count == 0)
				//	throw new LinqToDBException("Merge source doesn't have any columns.");

				StringBuilder.Append(" (");

				var first = true;
				foreach (var field in mergeSource.SourceFields)
				{
					if (!first)
						StringBuilder.AppendLine(", ");
					first = false;
					AppendIndent();
					StringBuilder.Append(Convert(field.PhysicalName, ConvertType.NameToQueryField));
				}

				StringBuilder
					.Append(")");
			}
		}

		private void BuildMergeSourceEnumerable(SqlMergeSourceTable mergeSource)
		{
			if (MergeSupportsSourceDirectValues)
			{
				if (mergeSource.SourceEnumerable.Rows.Count > 0)
				{
					StringBuilder.Append("(");
					BuildValues(mergeSource.SourceFields, mergeSource.SourceEnumerable, true);
					StringBuilder.Append(")");
				}
				else if (MergeEmptySourceSupported)
				{
					BuildMergeEmptySource(mergeSource);
				}
				else
				{
					throw new LinqToDBException($"{this.Name} doesn't support merge with empty source");
				}
				//if (hasData)
				BuildMergeAsSourceClause(mergeSource);
				//else if (EmptySourceSupported)
				//	BuildEmptySource();
				//else
				//	NoopCommand = true;
				return;
			}

			throw new NotImplementedException("BuildMergeSourceEnumerable");
		}

		private void BuildMergeEmptySource(SqlMergeSourceTable mergeSource)
		{
			StringBuilder
				.AppendLine("(")
				.Append("\tSELECT ")
				;

			//var columnTypes = GetSourceColumnTypes();

			for (var i = 0; i < mergeSource.SourceFields.Count; i++)
			{
				var field = mergeSource.SourceFields[i];

				if (i > 0)
					StringBuilder.Append(", ");

				BuildValue(
					new SqlDataType(
						field.DataType,
						field.SystemType,
						field.Length,
						field.Precision,
						field.Scale),
					null);

				//AddSourceValue(
				//	DataContext.MappingSchema.ValueToSqlConverter,
				//	_sourceDescriptor.Columns[i],
				//	columnTypes[i],
				//	null, true, true);

				//StringBuilder
				//	.Append(" ")
				//	.Append(CreateSourceColumnAlias(_sourceDescriptor.Columns[i].ColumnName, true));
			}

			StringBuilder
				.AppendLine()
				.Append("\tFROM ");

			if (!BuildFakeTableName())
				// we don't select anything, so it is ok to use target table
				BuildTableName(mergeSource.Merge.Target, true, false);

			StringBuilder
				.AppendLine("\tWHERE 1 = 0")
				.AppendLine(")");
				//.AppendLine((string)SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias));
		}

		protected virtual bool BuildFakeTableName()
		{
			if (FakeTable != null)
			{
				BuildTableName(StringBuilder, FakeTableDatabase, FakeTableSchema, FakeTable);
				return true;
			}

			return false;
		}

		private void BuildValues(IList<SqlField> fields, SqlValuesTable sourceEnumerable, bool typed)
		{
			//var hasData = false;
			//var columnTypes = GetSourceColumnTypes();
			//var valueConverter = DataContext.MappingSchema.ValueToSqlConverter;

			for (var i = 0; i < sourceEnumerable.Rows.Count; i++)
			{
				var last = sourceEnumerable.Rows.Count == i - 1;

				BuildValuesRow(fields, sourceEnumerable.Rows[i]//, typed && last
					, i == 0);
			}
		}

		private void BuildValuesRow(IList<SqlField> fields, IList<SqlValue> sqlValues, bool first)
		{
			if (!first)
				StringBuilder.AppendLine(",");
			else
				StringBuilder
					.AppendLine("\tVALUES");

			StringBuilder.Append("\t\t(");
			for (var i = 0; i < sqlValues.Count; i++)
			{
				var field = fields[i];
				var value = sqlValues[i];
				if (i > 0)
					StringBuilder.Append(",");

				BuildValue(
					new SqlDataType(
						field.DataType,
						field.SystemType,
						field.Length,
						field.Precision,
						field.Scale),
					value.Value);
				//if (!ValueToSqlConverter.TryConvert(StringBuilder, columnType, sqlValues[i].Value))
				//{
				//	AddSourceValueAsParameter(column.DataType, column.DbType, value);
				//}

				//AddSourceValue(valueConverter, column, columnTypes[i], value, !hasData, lastRecord);
			}

			StringBuilder.Append(")");
		}

		private void BuildMergeSource(SqlMergeSourceTable mergeSource)
		{
			StringBuilder
				.Append("USING ");

			if (mergeSource.SourceQuery != null)
			{
				BuildMergeSourceQuery(mergeSource);
			}
			else
			{
				BuildMergeSourceEnumerable(mergeSource);
			}

			StringBuilder
				.AppendLine();
		}

		protected virtual void BuildMergeInto(SqlMergeStatement merge)
		{
			StringBuilder.Append("MERGE INTO ");
			BuildTableName(merge.Target, true, true);
			StringBuilder.AppendLine();
		}
	}
}
