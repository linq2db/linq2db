namespace LinqToDB.SqlProvider
{
	using System;
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

		protected virtual void BuildMergeStatement(SqlMergeStatement mergeStatement)
		{
			BuildMergeInto(mergeStatement);
			BuildMergeSource(mergeStatement);
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
			throw new NotImplementedException("BuildMergeOperationUpdate");
		}

		protected virtual void BuildMergeOperationDelete(SqlMergeOperationClause operation)
		{
			throw new NotImplementedException("BuildMergeOperationDelete");
		}

		protected virtual void BuildMergeOperationInsert(SqlMergeOperationClause operation)
		{
			throw new NotImplementedException("BuildMergeOperationInsert");
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

		private void BuildMergeSourceQuery(SqlMergeStatement mergeStatement)
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

				BuildPhysicalTable(mergeStatement.SourceQuery, null);

				//var cs = new[] { ' ', '\t', '\r', '\n' };

				//while (cs.Contains(Command[Command.Length - 1]))
				//	Command.Length--;
			}
			finally
			{
				//_connection.InlineParameters = inlineParameters;
			}

			BuildMergeAsSourceClause(mergeStatement);
		}

		private void BuildMergeAsSourceClause(SqlMergeStatement mergeStatement)
		{
			//StringBuilder
			//	.AppendLine()
			//	.Append(")");

			StringBuilder.Append(" ");

			ConvertTableName(StringBuilder, null, null, mergeStatement.SourceName);

			if (MergeSupportsColumnAliasesInSource)
			{
				//if (mergeStatement.SourceFields.Count == 0)
				//	throw new LinqToDBException("Merge source doesn't have any columns.");

				StringBuilder.Append(" (");

				var first = true;
				foreach (var field in mergeStatement.SourceFields.Values)
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

		private void BuildMergeSourceEnumerable(SqlMergeStatement mergeStatement)
		{
			throw new NotImplementedException("BuildMergeSourceEnumerable");
		}

		private void BuildMergeSource(SqlMergeStatement mergeStatement)
		{
			StringBuilder
				.Append("USING ");

			if (mergeStatement.SourceQuery != null)
			{
				BuildMergeSourceQuery(mergeStatement);
			}
			else
			{
				BuildMergeSourceEnumerable(mergeStatement);
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
