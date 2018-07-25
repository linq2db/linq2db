﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.DataProvider
{
	using Data;
	using Expressions;
	using Extensions;
	using Linq;
	using Linq.Builder;
	using Mapping;
	using SqlProvider;
	using SqlQuery;
	using System.Threading;

	/// <summary>
	/// Basic merge builder's validation options set to validate merge operation on SQL:2008 level without specific
	/// database limitations or extensions.
	/// </summary>
	public class BasicMergeBuilder<TTarget,TSource>
		where TTarget : class
		where TSource : class
	{
		#region .ctor
		protected MergeDefinition<TTarget, TSource> Merge { get; }

		public BasicMergeBuilder(DataConnection dataConnection, IMergeable<TTarget, TSource> merge)
		{
			_connection = dataConnection;
			Merge = (MergeDefinition<TTarget, TSource>)merge;
			ProviderName = dataConnection.DataProvider.Name;
		}
		#endregion

		#region Expression Helpers
		/// <summary>
		/// Replaces references to target or source record with references to a anonymous type properties:
		/// 't' for target record, and 's' for source record.
		/// </summary>
		private class ExpressionParameterRewriter : ExpressionVisitor
		{
			private readonly ParameterExpression _source;

			private readonly ParameterExpression _target;

			private readonly ParameterExpression _tuple;

			/// <param name="tuple">Tuple-typed parameter.</param>
			/// <param name="target">Old target record parameter.</param>
			/// <param name="source">Old source record parameter.</param>
			public ExpressionParameterRewriter(
				ParameterExpression tuple,
				ParameterExpression target,
				ParameterExpression source)
			{
				_tuple  = tuple;
				_target = target;
				_source = source;
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				if (node.Equals(_target))
					return Expression.Property(_tuple, "t");

				if (node.Equals(_source))
					return Expression.Property(_tuple, "s");

				return base.VisitParameter(node);
			}
		}
		#endregion

		#region MERGE : Predicates
		protected void BuildPredicateByKeys(Type keyType, Expression targetKey, Expression sourceKey)
		{
			var target = _connection.GetTable<TTarget>();
			var source = _connection.GetTable<TSource>();

			var join = Expression.Call(
				MemberHelper.MethodOf<IQueryable<int>>(n => n.Join<int, int, int, int>(null, null, null, null))
					.GetGenericMethodDefinition()
					.MakeGenericMethod(typeof(TTarget), typeof(TSource), keyType, typeof(int)),
				Expression.Constant(target),
				Expression.Constant(source),
				targetKey,
				sourceKey,
				Expression.Lambda<Func<TTarget, TSource, int>>(
					Expression.Constant(0),
					Expression.Parameter(typeof(TTarget), _targetAlias),
					Expression.Parameter(typeof(TSource), SourceAlias)));

			var ctx = target.Provider.Execute<ContextParser.Context>(
				Expression.Call(
					null,
					LinqExtensions.SetMethodInfo8.MakeGenericMethod(typeof(int)),
					new[] { join }));

			var statement = ctx.GetResultStatement();

			var selectContext = (SelectContext)ctx.Context;

			var condition = statement.SelectQuery.From.Tables[0].Joins[0].Condition;
			SetSourceColumnAliases(condition, statement.SelectQuery.From.Tables[0].Joins[0].Table.Source);

			ctx.SetParameters();
			SaveParameters(statement.Parameters);

			SqlBuilder.BuildSearchCondition(statement, condition, Command);

			Command.Append(" ");
		}

		protected void BuildPredicateByTargetAndSource(Expression<Func<TTarget, TSource, bool>> predicate)
		{
			var query = _connection.GetTable<TTarget>()
				.SelectMany(_ => _connection.GetTable<TSource>(), (t, s) => new { t, s });

			query = AddConditionOverSourceAndTarget(query, predicate);

			var ctx       = query.GetContext();
			var statement = ctx.GetResultStatement();

			var tables = MoveJoinsToSubqueries(statement, _targetAlias, SourceAlias, QueryElement.Where);
			SetSourceColumnAliases(statement.SelectQuery.Where.SearchCondition, tables.Item2.Source);

			ctx.SetParameters();
			SaveParameters(statement.Parameters);

			SqlBuilder.BuildSearchCondition(statement, statement.SelectQuery.Where.SearchCondition, Command);

			Command.Append(" ");
		}

		protected void BuildSingleTablePredicate<TTable>(
			Expression<Func<TTable, bool>> predicate,
			string tableAlias,
			bool isSource)
			where TTable : class
		{
			var qry       = _connection.GetTable<TTable>().Where(predicate);
			var ctx       = qry.GetContext();
			var statement = ctx.GetResultStatement();

			var tables = MoveJoinsToSubqueries(statement, tableAlias, null, QueryElement.Where);

			if (isSource)
				SetSourceColumnAliases(statement.SelectQuery.Where.SearchCondition, tables.Item1.Source);

			ctx.SetParameters();
			SaveParameters(statement.Parameters);

			SqlBuilder.BuildSearchCondition(statement, statement.SelectQuery.Where.SearchCondition, Command);

			Command.Append(" ");
		}

		private IQueryable<TTuple> AddConditionOverSourceAndTarget<TTuple>(
					IQueryable<TTuple> query,
					Expression<Func<TTarget, TSource, bool>> predicate)
		{
			var p            = Expression.Parameter(typeof(TTuple));
			var rewriter     = new ExpressionParameterRewriter(p, predicate.Parameters[0], predicate.Parameters[1]);
			var newPredicate = Expression.Lambda<Func<TTuple, bool>>(rewriter.Visit(predicate.Body), p);

			return query.Where(newPredicate);
		}

		private void SetSourceColumnAliases(IQueryElement query, ISqlTableSource sourceTable)
		{
			new QueryVisitor().Visit(query, expr =>
			{
				switch (expr.ElementType)
				{
					case QueryElementType.SqlField:
						{
							var field = (SqlField)expr;
							if (field.Table == sourceTable && field.ColumnDescriptor != null)
								field.PhysicalName = GetSourceColumnAlias(field.ColumnDescriptor.ColumnName);
							break;
						}
				}
			});
		}

		#endregion

		#region MERGE : SOURCE
		private readonly IDictionary<string, string> _sourceAliases = new Dictionary<string, string>();

		protected virtual void AddFakeSourceTableName()
		{
			SqlBuilder.BuildTableName(Command, FakeSourceTableDatabase, FakeSourceTableSchema, FakeSourceTable);
		}

		protected virtual void AddSourceValue(
			ValueToSqlConverter valueConverter,
			ColumnDescriptor    column,
			SqlDataType         columnType,
			object              value,
			bool                isFirstRow,
			bool                isLastRow)
		{
			// avoid parameters in source due to low limits for parameters number in providers
			if (!valueConverter.TryConvert(Command, columnType, value))
			{
				AddSourceValueAsParameter(column.DataType, value);
			}
		}

		protected void AddSourceValueAsParameter(DataType dataType, object value)
		{
			var name     = GetNextParameterName();
			var fullName = SqlBuilder.Convert(name, ConvertType.NameToQueryParameter).ToString();

			Command.Append(fullName);

			AddParameter(new DataParameter(name, value, dataType));
		}

		private void BuildAsSourceClause(IEnumerable<string> columnNames)
		{
			Command
				.AppendLine()
				.AppendFormat(") {0}", SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias));

			if (columnNames != null && SupportsColumnAliasesInTableAlias)
			{
				var nameList = columnNames.ToList();

				if (!nameList.Any())
					throw new LinqToDBException("Merge source doesn't have any columns.");

				Command.Append(" (");

				var first = true;
				foreach (var columnName in nameList)
				{
					if (!first)
						Command.Append(", ");
					else
						first = false;

					Command
						.AppendFormat("{0}", CreateSourceColumnAlias(columnName, true));
				}

				Command
					.AppendLine(")");
			}
			else
				Command.AppendLine();
		}

		private void BuildEmptySource()
		{
			Command
				.AppendLine("(")
				.Append("\tSELECT ")
				;

			var columnTypes = GetSourceColumnTypes();

			for (var i = 0; i < _sourceDescriptor.Columns.Count; i++)
			{
				if (i > 0)
					Command.Append(", ");

				AddSourceValue(
					DataContext.MappingSchema.ValueToSqlConverter,
					_sourceDescriptor.Columns[i],
					columnTypes[i],
					null, true, true);

				Command
					.Append(" ")
					.Append(CreateSourceColumnAlias(_sourceDescriptor.Columns[i].ColumnName, true));
			}

			Command
				.AppendLine()
				.Append("\tFROM ");

			if (FakeSourceTable != null)
				AddFakeSourceTableName();
			else // we don't select anything, so it is ok to use target table
				Command.AppendLine(TargetTableName);

			Command
				.AppendLine("\tWHERE 1 = 0")
				.Append(") ")
				.AppendLine((string)SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias));
		}

		private void BuildSource()
		{
			Command.AppendLine("USING");

			if (Merge.QueryableSource != null && SupportsSourceSubQuery)
				BuildSourceSubQuery(Merge.QueryableSource);
			else
			{
				var source = Merge.EnumerableSource ?? Merge.QueryableSource;
				if (SupportsSourceDirectValues)
					BuildSourceDirectValues(source);
				else
					BuildSourceSubQueryValues(source);
			}
		}

		private void SetColumnAlias(string alias, string columnName)
		{
			_sourceAliases.Add(columnName, alias);
		}

		private string CreateSourceColumnAlias(string columnName, bool returnEscaped)
		{
			var alias = "c" + _sourceAliases.Count;
			_sourceAliases.Add(columnName, alias);

			if (returnEscaped)
				alias = (string)SqlBuilder.Convert(alias, ConvertType.NameToQueryFieldAlias);

			return alias;
		}

		private void BuildSourceDirectValues(IEnumerable<TSource> source)
		{
			var hasData        = false;
			var columnTypes    = GetSourceColumnTypes();
			var valueConverter = DataContext.MappingSchema.ValueToSqlConverter;

			TSource next = null;
			foreach (var item in source)
			{
				if (next != null)
					BuildValues(ref hasData, columnTypes, valueConverter, next, false);

				next = item;
			}

			if (next != null)
				BuildValues(ref hasData, columnTypes, valueConverter, next, true);

			if (hasData)
				BuildAsSourceClause(_sourceDescriptor.Columns.Select(_ => _.ColumnName));
			else if (EmptySourceSupported)
				BuildEmptySource();
			else
				NoopCommand = true;
		}

		private void BuildValues(ref bool hasData, SqlDataType[] columnTypes, ValueToSqlConverter valueConverter, TSource item, bool lastRecord)
		{
			if (hasData)
				Command.AppendLine(",");
			else
				Command
					.AppendLine("(")
					.AppendLine("\tVALUES");

			Command.Append("\t(");

			for (var i = 0; i < _sourceDescriptor.Columns.Count; i++)
			{
				if (i > 0)
					Command.Append(",");

				var column = _sourceDescriptor.Columns[i];
				var value = column.GetValue(DataContext.MappingSchema, item);

				AddSourceValue(valueConverter, column, columnTypes[i], value, !hasData, lastRecord);

				if (!SupportsColumnAliasesInTableAlias)
					Command.AppendFormat(" {0}", CreateSourceColumnAlias(column.ColumnName, true));
			}

			Command.Append(")");

			hasData = true;
		}

		private void BuildSourceSubQuery(IQueryable<TSource> queryableSource)
		{
			Command.AppendLine("(");

			var inlineParameters = _connection.InlineParameters;
			try
			{
				_connection.InlineParameters = !SupportsParametersInSource;

				var ctx = queryableSource.GetMergeContext();

				ctx.UpdateParameters();

				var statement = ctx.GetResultStatement();

				foreach (var columnInfo in ctx.Columns)
				{
					var columnDescriptor = _sourceDescriptor.Columns.Single(_ => _.MemberInfo == columnInfo.Members[0]);
					var column           = statement.SelectQuery.Select.Columns[columnInfo.Index];

					SetColumnAlias(column.Alias, columnDescriptor.ColumnName);
				}

				// bind parameters
				statement.Parameters.Clear();
				new QueryVisitor().VisitAll(ctx.SelectQuery, expr =>
				{
					switch (expr.ElementType)
					{
						case QueryElementType.SqlParameter:
							{
								var p = (SqlParameter)expr;
								if (p.IsQueryParameter)
									statement.Parameters.Add(p);

								break;
							}
					}
				});

				ctx.SetParameters();

				SaveParameters(statement.Parameters);

				SqlBuilder.BuildSql(0, statement, Command, startIndent : 1);

				var cs = new [] { ' ', '\t', '\r', '\n' };

				while (cs.Contains(Command[Command.Length - 1]))
					Command.Length--;
			}
			finally
			{
				_connection.InlineParameters = inlineParameters;
			}

			BuildAsSourceClause(null);
		}

		private void BuildSourceSubQueryValues(IEnumerable<TSource> source)
		{
			var hasData        = false;
			var columnTypes    = GetSourceColumnTypes();
			var valueConverter = DataContext.MappingSchema.ValueToSqlConverter;

			TSource next = null;
			foreach (var item in source)
			{
				if (next != null)
					BuildValuesAsSelect(ref hasData, columnTypes, valueConverter, next, false);

				next = item;
			}

			if (next != null)
				BuildValuesAsSelect(ref hasData, columnTypes, valueConverter, next, true);

			if (hasData)
				BuildAsSourceClause(_sourceDescriptor.Columns.Select(_ => _.ColumnName));
			else if (EmptySourceSupported)
				BuildEmptySource();
			else
				NoopCommand = true;
		}

		private void BuildValuesAsSelect(ref bool hasData, SqlDataType[] columnTypes, ValueToSqlConverter valueConverter, TSource item, bool lastItem)
		{
			if (hasData)
				Command
					.AppendLine()
					.AppendLine("\tUNION ALL");
			else
				Command
					.AppendLine("(");

			Command.Append("\tSELECT ");

			for (var i = 0; i < _sourceDescriptor.Columns.Count; i++)
			{
				if (i > 0)
					Command.Append(", ");

				var column = _sourceDescriptor.Columns[i];
				var value = column.GetValue(DataContext.MappingSchema, item);

				AddSourceValue(valueConverter, column, columnTypes[i], value, !hasData, lastItem);

				if (!SupportsColumnAliasesInTableAlias)
					Command
						.Append(" ")
						.Append(hasData ? GetEscapedSourceColumnAlias(column.ColumnName) : CreateSourceColumnAlias(column.ColumnName, true))
						;
			}

			hasData = true;

			if (FakeSourceTable != null)
			{
				Command.Append(" FROM ");
				AddFakeSourceTableName();
			}
		}

		private string GetEscapedSourceColumnAlias(string columnName)
		{
			return (string)SqlBuilder.Convert(GetSourceColumnAlias(columnName), ConvertType.NameToQueryField);
		}

		private string GetSourceColumnAlias(string columnName)
		{
			if (!_sourceAliases.ContainsKey(columnName))
			{
				// this exception thrown when user use projection of mapping class in source query without all
				// required fields
				// Example:
				/*
				 * class Entity
				 * {
				 *     [PrimaryKey]
				 *     public int Id { get; }
				 *
				 *     public int Field1 { get; }
				 *
				 *     public int Field2 { get; }
				 * }
				 *
				 * db.Table
				 *     .Merge()
				 *     .Using(db.Entity.Select(e => new Entity() { Field1 = e.Field2 }))
				 *     // here we expect Id primary key in source, but only Field1 selected
				 *     .OnTargetKey()
				 *     here we expect all fields from source, but only Field1 selected
				 *     .InsertWhenNotMatched()
				 *     .Merge();
				 */
				throw new LinqToDBException($"Column {columnName} doesn't exist in source");
			}

			return _sourceAliases[columnName];
		}

		private SqlDataType[] GetSourceColumnTypes()
		{
			return _sourceDescriptor.Columns
				.Select(c => new SqlDataType(c.DataType, c.MemberType, c.Length, c.Precision, c.Scale))
				.ToArray();
		}
		#endregion

		#region MERGE Generation
		protected virtual void BuildMatch()
		{
			Command.Append("ON (");

			if (Merge.KeyType != null)
				BuildPredicateByKeys(Merge.KeyType, Merge.TargetKey, Merge.SourceKey);
			else
				BuildPredicateByTargetAndSource(Merge.MatchPredicate ?? MakeDefaultMatchPredicate());

			while (Command[Command.Length - 1] == ' ')
				Command.Length--;

			Command.AppendLine(")");
		}

		protected Expression<Func<TTarget, TSource, bool>> MakeDefaultMatchPredicate()
		{
			var pTarget = Expression.Parameter(typeof(TTarget), _targetAlias);
			var pSource = Expression.Parameter(typeof(TSource), SourceAlias);

			Expression ex = null;

			foreach (var column in TargetDescriptor.Columns.Where(c => c.IsPrimaryKey))
			{
				var expr = Expression.Equal(
					Expression.MakeMemberAccess(pTarget, column.MemberInfo),
					Expression.MakeMemberAccess(pSource, column.MemberInfo));
				ex = ex != null ? Expression.AndAlso(ex, expr) : expr;
			}

			if (ex == null)
				throw new LinqToDBException("Method OnTargetKey() needs at least one primary key column");

			var target = _connection.GetTable<TTarget>();
			var source = _connection.GetTable<TSource>();

			return Expression.Lambda<Func<TTarget, TSource, bool>>(ex, pTarget, pSource);
		}

		protected virtual void BuildMergeInto()
		{
			Command
				.Append("MERGE INTO ")
				.Append(TargetTableName)
				.Append(" ")
				.AppendLine((string)SqlBuilder.Convert(_targetAlias, ConvertType.NameToQueryTableAlias));
		}

		protected virtual void BuildOperation(MergeDefinition<TTarget, TSource>.Operation operation)
		{
			switch (operation.Type)
			{
				case MergeOperationType.Update:
					BuildUpdate(operation.MatchedPredicate, operation.UpdateExpression);
					break;
				case MergeOperationType.Delete:
					BuildDelete(operation.MatchedPredicate);
					break;
				case MergeOperationType.Insert:
					BuildInsert(operation.NotMatchedPredicate, operation.CreateExpression);
					break;
				case MergeOperationType.UpdateWithDelete:
					BuildUpdateWithDelete(operation.MatchedPredicate, operation.UpdateExpression, operation.MatchedPredicate2);
					break;
				case MergeOperationType.DeleteBySource:
					BuildDeleteBySource(operation.BySourcePredicate);
					break;
				case MergeOperationType.UpdateBySource:
					BuildUpdateBySource(operation.BySourcePredicate, operation.UpdateBySourceExpression);
					break;
				default:
					throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Allows to add text before generated merge command.
		/// </summary>
		protected virtual void BuildPreambule()
		{
		}

		/// <summary>
		/// Allows to add text after generated merge command. E.g. to specify command terminator if provider requires it.
		/// </summary>
		protected virtual void BuildTerminator()
		{
		}

		protected virtual void BuildUpdateWithDelete(
			Expression<Func<TTarget, TSource, bool>>    updatePredicate,
			Expression<Func<TTarget, TSource, TTarget>> updateExpression,
			Expression<Func<TTarget, TSource, bool>>    deletePredicate)
		{
			// must be implemented by descendant that supports this operation
			throw new NotImplementedException();
		}

		private void BuildCommandText()
		{
			BuildPreambule();

			BuildMergeInto();

			BuildSource();

			if (NoopCommand)
				return;

			BuildMatch();

			foreach (var operation in Merge.Operations)
			{
				BuildOperation(operation);
			}

			BuildTerminator();
		}
		#endregion

		#region Operations: DELETE
		protected virtual void BuildDelete(Expression<Func<TTarget, TSource, bool>> predicate)
		{
			Command
				.AppendLine()
				.Append("WHEN MATCHED ");

			if (predicate != null)
			{
				Command.Append("AND ");
				BuildPredicateByTargetAndSource(predicate);
			}

			Command
				.AppendLine("THEN")
				.AppendLine("DELETE")
				;
		}
		#endregion

		#region Operations: DELETE BY SOURCE
		private void BuildDeleteBySource(Expression<Func<TTarget, bool>> predicate)
		{
			Command
				.AppendLine()
				.Append("WHEN NOT MATCHED By Source ");

			if (predicate != null)
			{
				Command.Append("AND ");
				BuildSingleTablePredicate(predicate, _targetAlias, false);
			}

			Command
				.AppendLine("THEN")
				.AppendLine("DELETE")
				;
		}
		#endregion

		#region Operations: INSERT
		protected void BuildCustomInsert(Expression<Func<TSource, TTarget>> create)
		{
			Expression insertExpression = Expression.Call(
				null,
				LinqExtensions.InsertMethodInfo3.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				new[]
				{
					_connection.GetTable<TSource>().Expression,
					_connection.GetTable<TTarget>().Expression,
					Expression.Quote(create)
				});

			var qry = Query<int>.GetQuery(DataContext, ref insertExpression);
			var statement = qry.Queries[0].Statement;

			// we need InsertOrUpdate for sql builder to generate values clause
			var newInsert = new SqlInsertOrUpdateStatement(statement.SelectQuery) { Insert = statement.GetInsertClause(), Update = statement.GetUpdateClause() };
			newInsert.Parameters.AddRange(statement.Parameters);
			newInsert.Insert.Into.Alias = _targetAlias;

			var tables = MoveJoinsToSubqueries(newInsert, SourceAlias, null, QueryElement.InsertSetter);
			SetSourceColumnAliases(newInsert.Insert, tables.Item1.Source);

			qry.Queries[0].Statement = newInsert;
			QueryRunner.SetParameters(qry, DataContext, insertExpression, null, 0);

			SaveParameters(newInsert.Parameters);

			if (IsIdentityInsertSupported
				&& newInsert.Insert.Items.Any(_ => _.Column is SqlField field && field.IsIdentity))
				OnInsertWithIdentity();

			SqlBuilder.BuildInsertClauseHelper(newInsert, Command);
		}

		protected void BuildDefaultInsert()
		{
			// insert identity field values only if it is supported by database and field is not excluded from
			// implicit insert operation by SkipOnInsert attribute
			// see https://github.com/linq2db/linq2db/issues/914 for more details
			var insertColumns = TargetDescriptor.Columns
				.Where(c => (IsIdentityInsertSupported && c.IsIdentity && !c.SkipOnInsert) || !c.SkipOnInsert)
				.ToList();

			if (IsIdentityInsertSupported && insertColumns.Any(c => c.IsIdentity))
				OnInsertWithIdentity();

			Command.AppendLine("(");

			var first = true;
			foreach (var column in insertColumns)
			{
				if (!first)
					Command
						.Append(",")
						.AppendLine();

				first = false;
				Command.AppendFormat("\t{0}", SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField));
			}

			Command
				.AppendLine()
				.AppendLine(")")
				.AppendLine("VALUES")
				.AppendLine("(");

			var sourceAlias = SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias);

			first = true;

			foreach (var column in insertColumns)
			{
				if (!first)
					Command
						.Append(",")
						.AppendLine();

				first = false;
				Command.AppendFormat(
					"\t{1}.{0}",
					GetEscapedSourceColumnAlias(column.ColumnName),
					sourceAlias);
			}

			Command
				.AppendLine()
				.AppendLine(")");
		}

		protected virtual void BuildInsert(
			Expression<Func<TSource,bool>>    predicate,
			Expression<Func<TSource,TTarget>> create)
		{
			Command
				.AppendLine()
				.Append("WHEN NOT MATCHED ");

			if (predicate != null)
			{
				Command.Append("AND ");
				BuildSingleTablePredicate(predicate, SourceAlias, true);
			}

			Command
				.AppendLine("THEN")
				.Append("INSERT")
				;

			if (create != null)
				BuildCustomInsert(create);
			else
			{
				Command.AppendLine();
				BuildDefaultInsert();
			}
		}

		protected virtual void OnInsertWithIdentity()
		{
		}
		#endregion

		#region Operations: UPDATE
		private enum QueryElement
		{
			Where,
			InsertSetter,
			UpdateSetter
		}

		protected void BuildCustomUpdate(Expression<Func<TTarget, TSource, TTarget>> update)
		{
			// build update query
			var target      = _connection.GetTable<TTarget>();
			var updateQuery = target.SelectMany(_ => _connection.GetTable<TSource>(), (t, s) => new { t, s });
			var predicate   = RewriteUpdatePredicateParameters(updateQuery, update);

			Expression updateExpression = Expression.Call(
				null,
				LinqExtensions.UpdateMethodInfo.MakeGenericMethod(new[] { updateQuery.GetType().GetGenericArgumentsEx()[0], typeof(TTarget) }),
				new[] { updateQuery.Expression, target.Expression, Expression.Quote(predicate) });

			var qry   = Query<int>.GetQuery(DataContext, ref updateExpression);
			var statement = qry.Queries[0].Statement;

			if (ProviderUsesAlternativeUpdate)
				BuildAlternativeUpdateQuery(statement);
			else
			{
				var tables = MoveJoinsToSubqueries(statement, _targetAlias, SourceAlias, QueryElement.UpdateSetter);
				SetSourceColumnAliases(statement.RequireUpdateClause(), tables.Item2.Source);
			}

			QueryRunner.SetParameters(qry, DataContext, updateExpression, null, 0);
			SaveParameters(statement.Parameters);

			SqlBuilder.BuildUpdateSetHelper((SqlUpdateStatement)statement, Command);
		}

		private void BuildAlternativeUpdateQuery(SqlStatement statement)
		{
			var query    = statement.EnsureQuery();
			var subQuery = (SelectQuery)QueryVisitor.Find(query.Where.SearchCondition, e => e.ElementType == QueryElementType.SqlQuery);
			var target   = query.From.Tables[0];
			target.Alias = _targetAlias;

			SqlTableSource source;

			if (subQuery.From.Tables.Count == 2)
			{
				// without associations
				source = subQuery.From.Tables[1];
				query.From.Tables.Add(source);
			}
			else
			{
				// with associations
				source = subQuery.From.Tables[0].Joins[0].Table;

				// collect tables, referenced in FROM clause
				var tableSet = new HashSet<SqlTable>();
				var tables   = new List<SqlTable>();

				new QueryVisitor().Visit(subQuery.From, e =>
				{
					if (e.ElementType == QueryElementType.TableSource)
					{
						var et = (SqlTableSource)e;

						tableSet.Add((SqlTable)et.Source);
						tables.Add((SqlTable)et.Source);
					}
				});

				((ISqlExpressionWalkable)statement.RequireUpdateClause()).Walk(true,
					element => ConvertToSubquery(subQuery, element, tableSet, tables, (SqlTable)target.Source,
						(SqlTable)source.Source));
			}

			source.Alias = SourceAlias;

			SetSourceColumnAliases(statement.RequireUpdateClause(), source.Source);
		}

		protected void BuildDefaultUpdate()
		{
			var updateColumns = TargetDescriptor.Columns
				.Where(c => !c.IsPrimaryKey && !c.IsIdentity && !c.SkipOnUpdate)
				.ToList();

			if (updateColumns.Count > 0)
			{
				Command.AppendLine("SET");

				var sourceAlias = (string)SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias);
				var maxLen      = updateColumns.Max(c => ((string)SqlBuilder.Convert(c.ColumnName, ConvertType.NameToQueryField)).Length);

				var first = true;
				foreach (var column in updateColumns)
				{
					if (!first)
						Command.AppendLine(",");

					first = false;

					var fieldName = (string)SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField);
					Command
						.AppendFormat("\t{0} ", fieldName)
						.Append(' ', maxLen - fieldName.Length)
						.AppendFormat("= {1}.{0}", GetEscapedSourceColumnAlias(column.ColumnName), sourceAlias);
				}

				Command.AppendLine();
			}
			else
				throw new LinqToDBException("Merge.Update call requires updatable columns");
		}

		protected virtual void BuildUpdate(
					Expression<Func<TTarget, TSource, bool>>    predicate,
					Expression<Func<TTarget, TSource, TTarget>> update)
		{
			Command
				.AppendLine()
				.Append("WHEN MATCHED ");

			if (predicate != null)
			{
				Command.Append("AND ");
				BuildPredicateByTargetAndSource(predicate);
			}

			while (Command[Command.Length - 1] ==  ' ')
				Command.Length--;

			Command.AppendLine(" THEN");
			Command.AppendLine("UPDATE");

			if (update != null)
				BuildCustomUpdate(update);
			else
				BuildDefaultUpdate();
		}

		private static ISqlExpression ConvertToSubquery(
							SelectQuery sql,
							ISqlExpression element,
							HashSet<SqlTable> tableSet,
							List<SqlTable> tables,
							SqlTable firstTable,
							SqlTable secondTable)
		{
			// for table field references from association tables we must rewrite them with subquery
			if (element.ElementType == QueryElementType.SqlField)
			{
				var fld = (SqlField)element;
				var tbl = (SqlTable)fld.Table;

				// table is an association table, used in FROM clause - generate subquery
				if (tbl != firstTable && (secondTable == null || tbl != secondTable) && tableSet.Contains(tbl))
				{
					var tempCopy = sql.Clone();
					var tempTables = new List<SqlTableSource>();

					// create copy of tables from main FROM clause for subquery clause
					new QueryVisitor().Visit(tempCopy.From, ee =>
					{
						if (ee.ElementType == QueryElementType.TableSource)
							tempTables.Add((SqlTableSource)ee);
					});

					// main table reference in subquery
					var tt = tempTables[tables.IndexOf(tbl)];

					tempCopy.Select.Columns.Clear();
					tempCopy.Select.Add(((SqlTable)tt.Source).Fields[fld.Name]);

					// create new WHERE for subquery
					tempCopy.Where.SearchCondition.Conditions.Clear();

					var firstTableKeys = tempCopy.From.Tables[0].Source.GetKeys(true);

					foreach (SqlField key in firstTableKeys)
						tempCopy.Where.Field(key).Equal.Field(firstTable.Fields[key.Name]);

					if (secondTable != null)
					{
						var secondTableKeys = tempCopy.From.Tables[0].Joins[0].Table.Source.GetKeys(true);

						foreach (SqlField key in secondTableKeys)
							tempCopy.Where.Field(key).Equal.Field(secondTable.Fields[key.Name]);
					}

					// set main query as parent
					tempCopy.ParentSelect = sql;

					return tempCopy;
				}
			}

			return element;
		}

		private static Tuple<SqlTableSource, SqlTableSource> MoveJoinsToSubqueries(
			SqlStatement statement,
			string       firstTableAlias,
			string       secondTableAlias,
			QueryElement part)
		{
			var baseTablesCount = secondTableAlias == null ? 1 : 2;

			// collect tables, referenced in FROM clause
			var tableSet = new HashSet<SqlTable>();
			var tables   = new List<SqlTable>();

			new QueryVisitor().Visit(statement.SelectQuery.From, e =>
			{
				if (e.ElementType == QueryElementType.TableSource)
				{
					var et = (SqlTableSource)e;

					tableSet.Add((SqlTable)et.Source);
					tables.Add((SqlTable)et.Source);
				}
			});

			if (tables.Count > baseTablesCount)
			{
				var firstTable  = (SqlTable)statement.SelectQuery.From.Tables[0].Source;
				var secondTable = baseTablesCount > 1
					? (SqlTable)statement.SelectQuery.From.Tables[0].Joins[0].Table.Source
					: null;

				ISqlExpressionWalkable queryPart;
				switch (part)
				{
					case QueryElement.Where:
						queryPart = statement.SelectQuery.Where;
						break;
					case QueryElement.InsertSetter:
						queryPart = statement.GetInsertClause();
						break;
					case QueryElement.UpdateSetter:
						queryPart = statement.GetUpdateClause();
						break;
					default:
						throw new InvalidOperationException();
				}

				queryPart.Walk(true, element => ConvertToSubquery(statement.SelectQuery, element, tableSet, tables, firstTable, secondTable));
			}

			var table1   = statement.SelectQuery.From.Tables[0];
			table1.Alias = firstTableAlias;

			SqlTableSource table2 = null;

			if (secondTableAlias != null)
			{
				if (tables.Count > baseTablesCount)
					table2 = statement.SelectQuery.From.Tables[0].Joins[0].Table;
				else
					table2 = statement.SelectQuery.From.Tables[1];

				table2.Alias = secondTableAlias;
			}

			return Tuple.Create(table1, table2);
		}

		Expression<Func<TTuple, TTarget>> RewriteUpdatePredicateParameters<TTuple>(
			IQueryable<TTuple>                        query,
			Expression<Func<TTarget,TSource,TTarget>> predicate)
		{
			var p        = Expression.Parameter(typeof(TTuple));
			var rewriter = new ExpressionParameterRewriter(p, predicate.Parameters[0], predicate.Parameters[1]);

			return Expression.Lambda<Func<TTuple,TTarget>>(rewriter.Visit(predicate.Body), p);
		}
		#endregion

		#region Operations: UPDATE BY SOURCE
		private void BuildUpdateBySource(
							Expression<Func<TTarget, bool>>    predicate,
							Expression<Func<TTarget, TTarget>> update)
		{
			Command
				.AppendLine()
				.Append("WHEN NOT MATCHED By Source ");

			if (predicate != null)
			{
				Command.Append("AND ");
				BuildSingleTablePredicate(predicate, _targetAlias, false);
			}

			Command.AppendLine("THEN UPDATE");

			Expression updateExpression = Expression.Call(
				null,
				LinqExtensions.UpdateMethodInfo2.MakeGenericMethod(typeof(TTarget)),
				new[] { _connection.GetTable<TTarget>().Expression, Expression.Quote(update) });

			var qry = Query<int>.GetQuery(DataContext, ref updateExpression);
			var statement = (SqlUpdateStatement)qry.Queries[0].Statement;

			MoveJoinsToSubqueries(statement, _targetAlias, null, QueryElement.UpdateSetter);

			QueryRunner.SetParameters(qry, DataContext, updateExpression, null, 0);

			SaveParameters(statement.Parameters);

			SqlBuilder.BuildUpdateSetHelper(statement, Command);
		}
		#endregion

		#region Parameters
		private readonly List<DataParameter> _parameters = new List<DataParameter>();

		protected void AddParameter(DataParameter parameter)
		{
			_parameters.Add(parameter);
		}

		private int _parameterCnt;

		/// <summary>
		/// List of generated command parameters.
		/// </summary>
		public DataParameter[] Parameters => _parameters.ToArray();

		/// <summary>
		/// If true, command execution must return 0 without request to database.
		/// </summary>
		public bool NoopCommand { get; private set; }


		protected string GetNextParameterName()
		{
			return string.Format("p{0}", Interlocked.Increment(ref _parameterCnt));
		}

		private void SaveParameters(IEnumerable<SqlParameter> parameters)
		{
			foreach (var param in parameters)
			{
				param.Name = GetNextParameterName();

				AddParameter(new DataParameter(param.Name, param.Value, param.DataType));
			}
		}
		#endregion

		#region Query Generation
		protected readonly string SourceAlias = "Source";

		readonly string           _targetAlias = "Target";
		readonly DataConnection   _connection;
		         EntityDescriptor _sourceDescriptor;

		protected StringBuilder Command { get; } = new StringBuilder();

		protected IDataContext  DataContext => Merge.Target.DataContext;

		/// <summary>
		/// If <see cref="SupportsSourceDirectValues"/> set to false and provider doesn't support SELECTs without
		/// FROM clause, this property should contain name of table with single record.
		/// </summary>
		protected virtual string FakeSourceTable => null;

		/// <summary>
		/// If <see cref="SupportsSourceDirectValues"/> set to false and provider doesn't support SELECTs without
		/// FROM clause, this property could contain name of database for table with single record.
		/// </summary>
		protected virtual string FakeSourceTableDatabase => null;

		/// <summary>
		/// If <see cref="SupportsSourceDirectValues"/> set to false and provider doesn't support SELECTs without
		/// FROM clause, this property could contain name of schema for table with single record.
		/// </summary>
		protected virtual string FakeSourceTableSchema => null;

		/// <summary>
		/// If true, provider allows to set values of identity columns on insert operation.
		/// </summary>
		protected virtual bool IsIdentityInsertSupported => false;

		/// <summary>
		/// If true, builder will generate command for empty enumerable source;
		/// otherwise command generation will be interrupted and 0 result returned without request to database.
		/// </summary>
		protected virtual bool EmptySourceSupported => true;

		protected BasicSqlBuilder SqlBuilder { get; private set;  }

		/// <summary>
		/// If true, provider allows to generate subquery as a source element of merge command.
		/// </summary>
		protected virtual bool SupportsSourceSubQuery => true;

		/// <summary>
		/// If true, provider supports column aliases specification after table alias.
		/// E.g. as table_alias (column_alias1, column_alias2).
		/// </summary>
		protected virtual bool SupportsColumnAliasesInTableAlias => true;

		/// <summary>
		/// If true, provider supports list of VALUES as a source element of merge command.
		/// </summary>
		protected virtual bool SupportsSourceDirectValues => true;

		/// <summary>
		/// If false, parameters in source subquery select list must have type.
		/// </summary>
		protected virtual bool SupportsParametersInSource => true;

		protected EntityDescriptor TargetDescriptor { get; private set; }

		/// <summary>
		/// Target table name, ready for use in SQL. Could include database/schema names or/and escaping.
		/// </summary>
		protected string TargetTableName { get; private set; }

		/// <summary>
		/// Generates SQL and parameters for merge command.
		/// </summary>
		/// <returns>Returns merge command SQL text.</returns>
		public virtual string BuildCommand()
		{
			// prepare required objects
			SqlBuilder = (BasicSqlBuilder)DataContext.CreateSqlProvider();

			_sourceDescriptor = TargetDescriptor = DataContext.MappingSchema.GetEntityDescriptor(typeof(TTarget));
			if (typeof(TTarget) != typeof(TSource))
				_sourceDescriptor = DataContext.MappingSchema.GetEntityDescriptor(typeof(TSource));

			var target = Merge.Target;
			var sb     = new StringBuilder();

			SqlBuilder.ConvertTableName(
				sb,
				target.DatabaseName ?? TargetDescriptor.DatabaseName,
				target.SchemaName   ?? TargetDescriptor.SchemaName,
				target.TableName    ?? TargetDescriptor.TableName);
			TargetTableName = sb.ToString();

			BuildCommandText();

			return Command.ToString();
		}

		protected void BuildColumnType(ColumnDescriptor column, SqlDataType columnType)
		{
			if (column.DbType != null)
				Command.Append(column.DbType);
			else
			{
				if (columnType.DataType == DataType.Undefined)
				{
					columnType = DataContext.MappingSchema.GetDataType(column.StorageType);

					if (columnType.DataType == DataType.Undefined)
					{
						var canBeNull = column.CanBeNull;

						columnType = DataContext.MappingSchema.GetUnderlyingDataType(column.StorageType, ref canBeNull);
					}
				}

				SqlBuilder.BuildTypeName(Command, columnType);
			}
		}
		#endregion

		#region Validation
		static readonly MergeOperationType[] _matchedTypes =
		{
			MergeOperationType.Delete,
			MergeOperationType.Update,
			MergeOperationType.UpdateWithDelete
		};

		static readonly MergeOperationType[] _notMatchedBySourceTypes =
		{
			MergeOperationType.DeleteBySource,
			MergeOperationType.UpdateBySource
		};

		static readonly MergeOperationType[] _notMatchedTypes =
		{
			MergeOperationType.Insert
		};

		/// <summary>
		/// For providers, that use <see cref="BasicSqlOptimizer.GetAlternativeUpdate"/> method to build
		/// UPDATE FROM query, this property should be set to true.
		/// </summary>
		protected virtual bool ProviderUsesAlternativeUpdate => false;

		/// <summary>
		/// If true, merge command could include DeleteBySource and UpdateBySource operations. Those operations
		/// supported only by SQL Server.
		/// </summary>
		protected virtual bool BySourceOperationsSupported => false;

		/// <summary>
		/// If true, merge command could include Delete operation. This operation is a part of SQL 2008 standard.
		/// </summary>
		protected virtual bool DeleteOperationSupported => true;

		/// <summary>
		/// Maximum number of oprations, allowed in single merge command. If value is less than one - there is no limits
		/// on number of commands. This option is used by providers that have limitations on number of operations like
		/// SQL Server.
		/// </summary>
		protected virtual int MaxOperationsCount => 0;

		/// <summary>
		/// If true, merge command operations could have predicates. This is a part of SQL 2008 standard.
		/// </summary>
		protected virtual bool OperationPredicateSupported => true;

		protected string ProviderName { get; }

		/// <summary>
		/// If true, merge command could have multiple operations of the same type with predicates with upt to one
		/// command without predicate. This option is used by providers that doesn't allow multiple operations of the
		/// same type like SQL Server.
		/// </summary>
		protected virtual bool SameTypeOperationsAllowed => true;

		/// <summary>
		/// When this operation enabled, merge command cannot include Delete or Update operations together with
		/// UpdateWithDelete operation in single command. Also use of Delte and Update operations in the same command
		/// not allowed even without UpdateWithDelete operation.
		/// This is Oracle-specific operation.
		/// </summary>
		protected virtual bool UpdateWithDeleteOperationSupported => false;

		/// <summary>
		/// Validates command configuration to not violate common or provider-specific rules.
		/// </summary>
		public virtual void Validate()
		{
			// validate operations limit
			if (MaxOperationsCount > 0 && Merge.Operations.Length > MaxOperationsCount)
				throw new LinqToDBException(string.Format("Merge cannot contain more than {1} operations for {0} provider.", ProviderName, MaxOperationsCount));

			// - validate that specified operations supported by provider
			// - validate that operations don't have conditions if provider doesn't support them
			var hasUpdate           = false;
			var hasDelete           = false;
			var hasUpdateWithDelete = false;

			foreach (var operation in Merge.Operations)
			{
				switch (operation.Type)
				{
					case MergeOperationType.Delete:
						hasDelete = true;
						if (!DeleteOperationSupported)
							throw new LinqToDBException($"Merge Delete operation is not supported by {ProviderName} provider.");
						if (!OperationPredicateSupported && operation.MatchedPredicate != null)
							throw new LinqToDBException($"Merge operation conditions are not supported by {ProviderName} provider.");
						break;
					case MergeOperationType.Insert:
						if (!OperationPredicateSupported && operation.NotMatchedPredicate != null)
							throw new LinqToDBException($"Merge operation conditions are not supported by {ProviderName} provider.");
						break;
					case MergeOperationType.Update:
						hasUpdate = true;
						if (!OperationPredicateSupported && operation.MatchedPredicate != null)
							throw new LinqToDBException($"Merge operation conditions are not supported by {ProviderName} provider.");
						break;
					case MergeOperationType.DeleteBySource:
						if (!BySourceOperationsSupported)
							throw new LinqToDBException($"Merge Delete By Source operation is not supported by {ProviderName} provider.");
						if (!OperationPredicateSupported && operation.BySourcePredicate != null)
							throw new LinqToDBException($"Merge operation conditions are not supported by {ProviderName} provider.");
						break;
					case MergeOperationType.UpdateBySource:
						if (!BySourceOperationsSupported)
							throw new LinqToDBException($"Merge Update By Source operation is not supported by {ProviderName} provider.");
						if (!OperationPredicateSupported && operation.BySourcePredicate != null)
							throw new LinqToDBException($"Merge operation conditions are not supported by {ProviderName} provider.");
						break;
					case MergeOperationType.UpdateWithDelete:
						hasUpdateWithDelete = true;
						if (!UpdateWithDeleteOperationSupported)
							throw new LinqToDBException($"UpdateWithDelete operation not supported by {ProviderName} provider.");
						break;
				}
			}

			// update/delete/updatewithdelete combinations validation
			if (hasUpdateWithDelete && hasUpdate)
				throw new LinqToDBException(
					$"Update operation with UpdateWithDelete operation in the same Merge command not supported by {ProviderName} provider.");
			if (hasUpdateWithDelete && hasDelete)
				throw new LinqToDBException(
					$"Delete operation with UpdateWithDelete operation in the same Merge command not supported by {ProviderName} provider.");
			if (UpdateWithDeleteOperationSupported && hasUpdate && hasDelete)
				throw new LinqToDBException(
					$"Delete and Update operations in the same Merge command not supported by {ProviderName} provider.");

			// - operations without conditions not placed before operations with conditions in each match group
			// - there is no multiple operations without condition in each match group
			ValidateGroupConditions(_matchedTypes);
			ValidateGroupConditions(_notMatchedTypes);
			ValidateGroupConditions(_notMatchedBySourceTypes);

			// validate that there is no duplicate operations (by type) if provider doesn't support them
			if (!SameTypeOperationsAllowed && Merge.Operations.GroupBy(_ => _.Type).Any(_ => _.Count() > 1))
				throw new LinqToDBException($"Multiple operations of the same type are not supported by {ProviderName} provider.");
		}

		private void ValidateGroupConditions(MergeOperationType[] groupTypes)
		{
			var hasUnconditional = false;
			foreach (var operation in Merge.Operations.Where(_ => groupTypes.Contains(_.Type)))
			{
				if (hasUnconditional && operation.HasCondition)
					throw new LinqToDBException("Unconditional Merge operation cannot be followed by operation with condition within the same match group.");

				if (hasUnconditional && !operation.HasCondition)
					throw new LinqToDBException("Multiple unconditional Merge operations not allowed within the same match group.");

				if (!hasUnconditional && !operation.HasCondition)
					hasUnconditional = true;
			}
		}
		#endregion
	}

	internal static class MergeSourceExtensions
	{
		static readonly MethodInfo _methodInfo = MemberHelper.MethodOf(() => GetMergeContext((IQueryable<int>)null)).GetGenericMethodDefinition();

		public static MergeContextParser.Context GetMergeContext<TSource>(this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<MergeContextParser.Context>(
				Expression.Call(
					null,
					_methodInfo.MakeGenericMethod(typeof(TSource)),
					new[] { currentSource.Expression }));
		}
	}

	internal class MergeContextParser : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var call = (MethodCallExpression)buildInfo.Expression;
			return new Context(builder.BuildSequence(new BuildInfo(buildInfo, call.Arguments[0])));
		}

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return buildInfo.Expression is MethodCallExpression call && call.Method.Name == "GetMergeContext";
		}

		public SequenceConvertInfo Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return builder.IsSequence(new BuildInfo(buildInfo, ((MethodCallExpression)buildInfo.Expression).Arguments[0]));
		}

		public class Context : PassThroughContext
		{
			public Action SetParameters;

			public Action UpdateParameters;

			public SqlInfo[] Columns;

			public Context(IBuildContext context) : base(context)
			{
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				query.DoNotCache = true;

				Columns = ConvertToIndex(null, 0, ConvertFlags.All);

				QueryRunner.SetNonQueryQuery(query);

				SetParameters = () => QueryRunner.SetParameters(query, Builder.DataContext, query.Expression, null, 0);

				query.GetElement = (db, expr, ps) => this;

				UpdateParameters = () =>
				{
					query.Queries[0].Parameters.Clear();
					query.Queries[0].Parameters.AddRange(Builder.CurrentSqlParameters);
				};
			}
		}
	}
}
