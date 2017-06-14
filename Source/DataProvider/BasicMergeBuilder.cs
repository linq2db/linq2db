using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Linq.Builder;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.DataProvider
{
	/// <summary>
	/// Basic merge builder's validation options set to validate merge operation on SQL:2008 level without specific
	/// database limitations or extensions.
	/// </summary>
	public class BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		#region .ctor
		protected MergeDefinition<TTarget, TSource> Merge { get; private set; }

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
				_tuple = tuple;
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
					LinqExtensions._setMethodInfo8.MakeGenericMethod(typeof(int)),
					new[] { join }));

			var sql = ctx.SelectQuery;

			var selectContext = (SelectContext)ctx.Context;

			var condition = sql.From.Tables[0].Joins[0].Condition;
			SetSourceColumnAliases(condition, sql.From.Tables[0].Joins[0].Table.Source);
			
			ctx.SetParameters();
			SaveParameters(sql.Parameters);

			SqlBuilder.BuildSearchCondition(sql, condition, Command);

			Command.Append(" ");
		}

		protected void BuildPredicateByTargetAndSource(Expression<Func<TTarget, TSource, bool>> predicate)
		{
			var query = _connection.GetTable<TTarget>()
				.SelectMany(_ => _connection.GetTable<TSource>(), (t, s) => new { t, s });

			query = AddConditionOverSourceAndTarget(query, predicate);

			var ctx = query.GetContext();
			var sql = ctx.SelectQuery;

			var selectContext = (SelectContext)ctx.Context;

			var tables = MoveJoinsToSubqueries(sql, _targetAlias, SourceAlias, QueryElement.Where);
			SetSourceColumnAliases(sql.Where.SearchCondition, tables.Item2.Source);

			ctx.SetParameters();
			SaveParameters(sql.Parameters);

			SqlBuilder.BuildSearchCondition(sql, sql.Where.SearchCondition, Command);

			Command.Append(" ");
		}

		protected void BuildSingleTablePredicate<TTable>(
			Expression<Func<TTable, bool>> predicate,
			string tableAlias,
			bool isSource)
			where TTable : class
		{
			var qry = _connection.GetTable<TTable>().Where(predicate);
			var ctx = qry.GetContext();
			var sql = ctx.SelectQuery;

			var tables = MoveJoinsToSubqueries(sql, tableAlias, null, QueryElement.Where);

			if (isSource)
				SetSourceColumnAliases(sql.Where.SearchCondition, tables.Item1.Source);

			ctx.SetParameters();
			SaveParameters(sql.Parameters);

			SqlBuilder.BuildSearchCondition(sql, sql.Where.SearchCondition, Command);

			Command.Append(" ");
		}

		private IQueryable<TTuple> AddConditionOverSourceAndTarget<TTuple>(
					IQueryable<TTuple> query,
					Expression<Func<TTarget, TSource, bool>> predicate)
		{
			var p = Expression.Parameter(typeof(TTuple));

			var rewriter = new ExpressionParameterRewriter(p, predicate.Parameters[0], predicate.Parameters[1]);

			var newPredicate = Expression.Lambda<Func<TTuple, bool>>(rewriter.Visit(predicate.Body), p);

			return query.Where(newPredicate);
		}

		private void BuildDefaultMatchPredicate()
		{
			var first = true;
			var targetAlias = (string)SqlBuilder.Convert(_targetAlias, ConvertType.NameToQueryTableAlias);
			var sourceAlias = (string)SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias);
			foreach (var column in TargetDescriptor.Columns.Where(c => c.IsPrimaryKey))
			{
				if (!first)
					Command.AppendLine(" AND");

				first = false;
				Command
					.AppendFormat(
						"\t{0}.{1} = {2}.{3}",
						targetAlias, SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField),
						sourceAlias, GetEscapedSourceColumnAlias(column.ColumnName));
			}
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

		private class QueryContext : IQueryContext
		{
			public SqlParameter[] SqlParameters;

			public object Context { get; set; }

			public List<string> QueryHints { get; set; }

			public SelectQuery SelectQuery { get; set; }

			public SqlParameter[] GetParameters()
			{
				return SqlParameters;
			}
		}
		#endregion

		#region MERGE : SOURCE
		private readonly IDictionary<string, string> _sourceAliases = new Dictionary<string, string>();

		protected virtual void AddFakeSourceTableName()
		{
			SqlBuilder.BuildTableName(Command, FakeSourceTableDatabase, FakeSourceTableOwner, FakeSourceTable);
		}

		protected virtual void AddSourceValue(
			ValueToSqlConverter valueConverter,
			ColumnDescriptor column,
			SqlDataType columnType,
			object value)
		{
			// avoid parameters in source due to low limits for parameters number in providers
			if (!valueConverter.TryConvert(Command, columnType, value))
			{
				var name = GetNextParameterName();

				var fullName = SqlBuilder.Convert(name, ConvertType.NameToQueryParameter).ToString();

				Command.Append(fullName);

				_parameters.Add(new DataParameter(name, value, column.DataType));
			}
		}

		private void BuildAsSourceClause(IEnumerable<string> columnNames)
		{
			Command
				.AppendLine()
				.AppendFormat("\t) {0}", SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias));

			if (columnNames != null && SupportsColumnAliasesInTableAlias)
			{
				if (!columnNames.Any())
					throw new LinqToDBException("Merge source doesn't have any columns.");

				Command.Append(" (");

				var first = true;
				foreach (var columnName in columnNames)
				{
					if (!first)
						Command.Append(", ");

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
			Command.Append("(SELECT ");

			var columnTypes = GetSourceColumnTypes();

			for (var i = 0; i < TargetDescriptor.Columns.Count; i++)
			{
				if (i > 0)
					Command.Append(", ");

				AddSourceValue(
					ContextInfo.DataContext.MappingSchema.ValueToSqlConverter,
					_sourceDescriptor.Columns[i],
					columnTypes[i],
					null);

				Command
					.Append(" ")
					.Append(CreateSourceColumnAlias(TargetDescriptor.Columns[i].ColumnName, true));
			}

			Command
				.Append(" FROM ")
				.Append(TargetTableName)
				.Append(" WHERE 1 = 0) ")
				.AppendLine((string)SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias));
		}

		private void BuildSource()
		{
			Command.Append("USING ");

			if (Merge.QueryableSource != null && SuportsSourceSubQuery)
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

		private string CreateSourceColumnAlias(string columnName, bool returnEscaped)
		{
			var alias = "src" + _sourceAliases.Count;
			_sourceAliases.Add(columnName, alias);

			if (returnEscaped)
				alias = (string)SqlBuilder.Convert(alias, ConvertType.NameToQueryFieldAlias);

			return alias;
		}

		private void BuildSourceDirectValues(IEnumerable<TSource> source)
		{
			var hasData = false;

			var columnTypes = GetSourceColumnTypes();

			var valueConverter = ContextInfo.DataContext.MappingSchema.ValueToSqlConverter;

			foreach (var item in source)
			{
				if (hasData)
					Command.AppendLine(",");
				else
					Command
						.AppendLine("(")
						.AppendLine("\tVALUES");

				hasData = true;

				Command.Append("\t(");

				for (var i = 0; i < _sourceDescriptor.Columns.Count; i++)
				{
					if (i > 0)
						Command.Append(",");

					var column = _sourceDescriptor.Columns[i];
					var value = column.GetValue(ContextInfo.DataContext.MappingSchema, item);

					AddSourceValue(valueConverter, column, columnTypes[i], value);

					if (!SupportsColumnAliasesInTableAlias)
						Command.AppendFormat(" {0}", CreateSourceColumnAlias(column.ColumnName, true));
				}

				Command.Append(")");
			}

			if (hasData)
				BuildAsSourceClause(_sourceDescriptor.Columns.Select(_ => _.ColumnName));
			else if (EmptySourceSupported)
				BuildEmptySource();
			else
				NoopCommand = true;
		}

		private void BuildSourceSubQuery(IQueryable<TSource> queryableSource)
		{
			Command.Append("(");

			var inlineParameters = _connection.InlineParameters;
			try
			{
				_connection.InlineParameters = !SupportsParametersInSource;

				var ctx = queryableSource.GetMergeContext();
				var query = ctx.SelectQuery;

				// update list of selected fields
				var info = ctx.FixSelectList();

				query.Select.Columns.Clear();
				foreach (var column in info)
				{
					var columnDescriptor = _sourceDescriptor.Columns.Where(_ => _.MemberInfo == column.Members[0]).Single();

					var alias = CreateSourceColumnAlias(columnDescriptor.ColumnName, false);
					query.Select.Columns.Add(new SelectQuery.Column(query, column.Sql, alias));
				}

				// bind parameters
				query.Parameters.Clear();
				new QueryVisitor().VisitAll(query, expr =>
				{
					switch (expr.ElementType)
					{
						case QueryElementType.SqlParameter:
							{
								var p = (SqlParameter)expr;
								if (p.IsQueryParameter)
									query.Parameters.Add(p);

								break;
							}
					}
				});

				ctx.SetParameters();

				SaveParameters(query.Parameters);

				var queryContext = new QueryContext()
				{
					SelectQuery = query,
					SqlParameters = query.Parameters.ToArray()
				};

				var preparedQuery = (DataConnection.PreparedQuery)ContextInfo.DataContext.SetQuery(queryContext);

				Command.Append(preparedQuery.Commands[0]);
			}
			finally
			{
				_connection.InlineParameters = inlineParameters;
			}

			BuildAsSourceClause(null);
		}

		private void BuildSourceSubQueryValues(IEnumerable<TSource> source)
		{
			var hasData = false;

			var columnTypes = GetSourceColumnTypes();

			var valueConverter = ContextInfo.DataContext.MappingSchema.ValueToSqlConverter;

			foreach (var item in source)
			{
				if (hasData)
					Command
						.AppendLine()
						.AppendLine("\t\tUNION ALL");
				else
					Command
						.AppendLine("(");

				Command.Append("\tSELECT ");

				for (var i = 0; i < _sourceDescriptor.Columns.Count; i++)
				{
					if (i > 0)
						Command.Append(",");

					var column = _sourceDescriptor.Columns[i];
					var value = column.GetValue(ContextInfo.DataContext.MappingSchema, item);

					AddSourceValue(valueConverter, column, columnTypes[i], value);

					if (!SupportsColumnAliasesInTableAlias)
						Command.AppendFormat(" {0}", hasData ? GetEscapedSourceColumnAlias(column.ColumnName) : CreateSourceColumnAlias(column.ColumnName, true));
				}

				hasData = true;

				if (FakeSourceTable != null)
				{
					Command.Append(" FROM ");
					AddFakeSourceTableName();
				}
			}

			if (hasData)
				BuildAsSourceClause(_sourceDescriptor.Columns.Select(_ => _.ColumnName));
			else if (EmptySourceSupported)
				BuildEmptySource();
			else
				NoopCommand = true;
		}

		private string GetEscapedSourceColumnAlias(string columnName)
		{
			return (string)SqlBuilder.Convert(GetSourceColumnAlias(columnName), ConvertType.NameToQueryField);
		}

		private string GetSourceColumnAlias(string columnName)
		{
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

			if (Merge.MatchPredicate != null)
				BuildPredicateByTargetAndSource(Merge.MatchPredicate);
			else if (Merge.KeyType != null)
				BuildPredicateByKeys(Merge.KeyType, Merge.TargetKey, Merge.SourceKey);
			else
				BuildDefaultMatchPredicate();

			Command.AppendLine(")");
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
			Expression<Func<TTarget, TSource, bool>> updatePredicate,
			Expression<Func<TTarget, TSource, TTarget>> updateExpression,
			Expression<Func<TTarget, TSource, bool>> deletePredicate)
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
				.AppendLine("THEN DELETE");
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
				.AppendLine("THEN DELETE");
		}
		#endregion

		#region Operations: INSERT
		protected void BuildCustomInsert(Expression<Func<TSource, TTarget>> create)
		{
			var insertExpression = Expression.Call(
				null,
				LinqExtensions._insertMethodInfo3.MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
				new[]
				{
					_connection.GetTable<TSource>().Expression,
					_connection.GetTable<TTarget>().Expression,
					Expression.Quote(create)
				});

			var qry = Query<int>.GetQuery(ContextInfo.DataContext, insertExpression);
			var query = qry.Queries[0].SelectQuery;

			query.Insert.Into.Alias = _targetAlias;

			// we need Insert type for proper query cloning (maybe this is a bug in clone function?)
			query.QueryType = QueryType.Insert;

			var tables = MoveJoinsToSubqueries(query, SourceAlias, null, QueryElement.InsertSetter);
			SetSourceColumnAliases(query.Insert, tables.Item1.Source);

			// we need InsertOrUpdate for sql builder to generate values clause
			query.QueryType = QueryType.InsertOrUpdate;

			qry.SetParameters(insertExpression, null, 0);

			SaveParameters(query.Parameters);

			if (IsIdentityInsertSupported
				&& query.Insert.Items.Any(_ => _.Column is SqlField && ((SqlField)_.Column).IsIdentity))
				OnInsertWithIdentity();

			SqlBuilder.BuildInsertClauseHelper(query, Command);
		}

		protected void BuildDefaultInsert()
		{
			var insertColumns = TargetDescriptor.Columns
				.Where(c => IsIdentityInsertSupported && c.IsIdentity || !c.SkipOnInsert)
				.ToList();

			if (IsIdentityInsertSupported && TargetDescriptor.Columns.Any(c => c.IsIdentity))
				OnInsertWithIdentity();

			Command.AppendLine("\t(");

			var first = true;
			foreach (var column in insertColumns)
			{
				if (!first)
					Command
						.Append(",")
						.AppendLine();

				first = false;
				Command.AppendFormat("\t\t{0}", SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField));
			}

			Command
				.AppendLine()
				.AppendLine("\t)")
				.AppendLine("\tVALUES")
				.AppendLine("\t(");

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
					"\t\t{1}.{0}",
					GetEscapedSourceColumnAlias(column.ColumnName),
					sourceAlias);
			}

			Command
				.AppendLine()
				.AppendLine("\t)");
		}

		protected virtual void BuildInsert(
					Expression<Func<TSource, bool>> predicate,
					Expression<Func<TSource, TTarget>> create)
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
				.AppendLine("THEN INSERT");

			if (create != null)
				BuildCustomInsert(create);
			else
				BuildDefaultInsert();
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
			var target = _connection.GetTable<TTarget>();
			var updateQuery = target.SelectMany(_ => _connection.GetTable<TSource>(), (t, s) => new { t, s });
			var predicate = RewriteUpdatePredicateParameters(updateQuery, update);

			var updateExpression = Expression.Call(
				null,
				LinqExtensions._updateMethodInfo.MakeGenericMethod(new[] { updateQuery.GetType().GetGenericArgumentsEx()[0], typeof(TTarget) }),
				new[] { updateQuery.Expression, target.Expression, Expression.Quote(predicate) });

			var qry = Query<int>.GetQuery(ContextInfo.DataContext, updateExpression);
			var query = qry.Queries[0].SelectQuery;

			if (ProviderUsesAlternativeUpdate)
				BuildAlternativeUpdateQuery(query);
			else
			{
				var tables = MoveJoinsToSubqueries(query, _targetAlias, SourceAlias, QueryElement.UpdateSetter);
				SetSourceColumnAliases(query.Update, tables.Item2.Source);
			}

			qry.SetParameters(updateExpression, null, 0);
			SaveParameters(query.Parameters);

			SqlBuilder.BuildUpdateSetHelper(query, Command);
		}

		private void BuildAlternativeUpdateQuery(SelectQuery query)
		{
			var subQuery = (SelectQuery)QueryVisitor.Find(query.Where.SearchCondition, e => e.ElementType == QueryElementType.SqlQuery);

			var target = query.From.Tables[0];
			target.Alias = _targetAlias;

			SelectQuery.TableSource source = null;

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
				var tables = new List<SqlTable>();
				new QueryVisitor().Visit(subQuery.From, e =>
				{
					if (e.ElementType == QueryElementType.TableSource)
					{
						var et = (SelectQuery.TableSource)e;

						tableSet.Add((SqlTable)et.Source);
						tables.Add((SqlTable)et.Source);
					}
				});

				((ISqlExpressionWalkable)query.Update).Walk(true, element => ConvertToSubquery(subQuery, element, tableSet, tables, (SqlTable)target.Source, (SqlTable)source.Source));
			}

			source.Alias = SourceAlias;

			SetSourceColumnAliases(query.Update, source.Source);
		}

		protected void BuildDefaultUpdate()
		{
			var updateColumns = TargetDescriptor.Columns
				.Where(c => !c.IsPrimaryKey && !c.IsIdentity && !c.SkipOnUpdate)
				.ToList();

			if (updateColumns.Count > 0)
			{
				Command.AppendLine("\tSET");

				var sourceAlias = (string)SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias);
				var maxLen = updateColumns.Max(c => ((string)SqlBuilder.Convert(c.ColumnName, ConvertType.NameToQueryField)).Length);

				var first = true;
				foreach (var column in updateColumns)
				{
					if (!first)
						Command.AppendLine(",");

					first = false;

					var fieldName = (string)SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField);
					Command
						.AppendFormat("\t\t{0} ", fieldName)
						.Append(' ', maxLen - fieldName.Length)
						.AppendFormat("= {1}.{0}", GetEscapedSourceColumnAlias(column.ColumnName), sourceAlias);
				}
			}
			else
				throw new LinqToDBException("Merge.Update call requires updatable columns");
		}

		protected virtual void BuildUpdate(
					Expression<Func<TTarget, TSource, bool>> predicate,
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

			Command.AppendLine(" THEN UPDATE");

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
					tempCopy.QueryType = QueryType.Select;
					var tempTables = new List<SelectQuery.TableSource>();

					// create copy of tables from main FROM clause for subquery clause
					new QueryVisitor().Visit(tempCopy.From, ee =>
					{
						if (ee.ElementType == QueryElementType.TableSource)
							tempTables.Add((SelectQuery.TableSource)ee);
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

		private static Tuple<SelectQuery.TableSource, SelectQuery.TableSource> MoveJoinsToSubqueries(
			SelectQuery sql,
			string firstTableAlias,
			string secondTableAlias,
			QueryElement part)
		{
			var baseTablesCount = secondTableAlias == null ? 1 : 2;

			// collect tables, referenced in FROM clause
			var tableSet = new HashSet<SqlTable>();
			var tables = new List<SqlTable>();
			new QueryVisitor().Visit(sql.From, e =>
			{
				if (e.ElementType == QueryElementType.TableSource)
				{
					var et = (SelectQuery.TableSource)e;

					tableSet.Add((SqlTable)et.Source);
					tables.Add((SqlTable)et.Source);
				}
			});

			if (tables.Count > baseTablesCount)
			{
				var firstTable = (SqlTable)sql.From.Tables[0].Source;
				var secondTable = baseTablesCount > 1
					? (SqlTable)sql.From.Tables[0].Joins[0].Table.Source
					: null;

				ISqlExpressionWalkable queryPart;
				switch (part)
				{
					case QueryElement.Where:
						queryPart = sql.Where;
						break;
					case QueryElement.InsertSetter:
						queryPart = sql.Insert;
						break;
					case QueryElement.UpdateSetter:
						queryPart = sql.Update;
						break;
					default:
						throw new InvalidOperationException();
				}

				queryPart.Walk(true, element => ConvertToSubquery(sql, element, tableSet, tables, firstTable, secondTable));
			}

			var table1 = sql.From.Tables[0];
			table1.Alias = firstTableAlias;

			SelectQuery.TableSource table2 = null;

			if (secondTableAlias != null)
			{
				if (tables.Count > baseTablesCount)
					table2 = sql.From.Tables[0].Joins[0].Table;
				else
					table2 = sql.From.Tables[1];

				table2.Alias = secondTableAlias;
			}

			return Tuple.Create(table1, table2);
		}

		private Expression<Func<TTuple, TTarget>> RewriteUpdatePredicateParameters<TTuple>(
			IQueryable<TTuple> query,
			Expression<Func<TTarget, TSource, TTarget>> predicate)
		{
			var p = Expression.Parameter(typeof(TTuple));

			var rewriter = new ExpressionParameterRewriter(p, predicate.Parameters[0], predicate.Parameters[1]);

			return Expression.Lambda<Func<TTuple, TTarget>>(rewriter.Visit(predicate.Body), p);
		}
		#endregion

		#region Operations: UPDATE BY SOURCE
		private void BuildUpdateBySource(
							Expression<Func<TTarget, bool>> predicate,
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

			var updateExpression = Expression.Call(
				null,
				LinqExtensions._updateMethodInfo2.MakeGenericMethod(new[] { typeof(TTarget) }),
				new[] { _connection.GetTable<TTarget>().Expression, Expression.Quote(update) });

			var qry = Query<int>.GetQuery(ContextInfo.DataContext, updateExpression);
			var query = qry.Queries[0].SelectQuery;

			MoveJoinsToSubqueries(query, _targetAlias, null, QueryElement.UpdateSetter);

			qry.SetParameters(updateExpression, null, 0);
			SaveParameters(query.Parameters);

			SqlBuilder.BuildUpdateSetHelper(query, Command);
		}
		#endregion

		#region Parameters
		private readonly List<DataParameter> _parameters = new List<DataParameter>();

		private int _parameterCnt;

		/// <summary>
		/// List of generated command parameters.
		/// </summary>
		public DataParameter[] Parameters
		{
			get
			{
				return _parameters.ToArray();
			}
		}

		/// <summary>
		/// If true, command execution must return 0 without request to database.
		/// </summary>
		public bool NoopCommand { get; private set; }


		private string GetNextParameterName()
		{
			return string.Format("p{0}", _parameterCnt++);
		}

		private void SaveParameters(IEnumerable<SqlParameter> parameters)
		{
			foreach (var param in parameters)
			{
				param.Name = GetNextParameterName();

				_parameters.Add(new DataParameter(param.Name, param.Value, param.DataType));
			}
		}
		#endregion

		#region Query Generation
		protected readonly string SourceAlias = "Source";

		private readonly string _targetAlias = "Target";

		private StringBuilder _command = new StringBuilder();

		private DataConnection _connection;

		private EntityDescriptor _sourceDescriptor;

		protected StringBuilder Command
		{
			get
			{
				return _command;
			}
		}

		protected IDataContextInfo ContextInfo
		{
			get
			{
				return Merge.Target.DataContextInfo;
			}
		}

		protected int EnumerableSourceSize { get; private set; }

		/// <summary>
		/// If <see cref="SupportsSourceDirectValues"/> set to false and provider doesn't support SELECTs without
		/// FROM clause, this property should contain name of table with single record.
		/// </summary>
		protected virtual string FakeSourceTable
		{
			get
			{
				return null;
			}
		}

		/// <summary>
		/// If <see cref="SupportsSourceDirectValues"/> set to false and provider doesn't support SELECTs without
		/// FROM clause, this property could contain name of database for table with single record.
		/// </summary>
		protected virtual string FakeSourceTableDatabase
		{
			get
			{
				return null;
			}
		}

		/// <summary>
		/// If <see cref="SupportsSourceDirectValues"/> set to false and provider doesn't support SELECTs without
		/// FROM clause, this property could contain name of schema for table with single record.
		/// </summary>
		protected virtual string FakeSourceTableOwner
		{
			get
			{
				return null;
			}
		}

		/// <summary>
		/// If true, provider allows to set values of identity columns on insert operation.
		/// </summary>
		protected virtual bool IsIdentityInsertSupported
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// If true, builder will generate command for empty enumerable source;
		/// otherwise command generation will be interrupted and 0 result returned without request to database.
		/// </summary>
		protected virtual bool EmptySourceSupported
		{
			get
			{
				return true;
			}
		}

		protected BasicSqlBuilder SqlBuilder { get; private set; }

		/// <summary>
		/// If true, provider allows to generate subquery as a source element of merge command.
		/// </summary>
		protected virtual bool SuportsSourceSubQuery
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// If true, provider supports column aliases specification after table alias.
		/// E.g. as table_alias (column_alias1, column_alias2).
		/// </summary>
		protected virtual bool SupportsColumnAliasesInTableAlias
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// If true, provider supports list of VALUES as a source element of merge command.
		/// </summary>
		protected virtual bool SupportsSourceDirectValues
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// If false, parameters in source subquery select list must have type.
		/// </summary>
		protected virtual bool SupportsParametersInSource
		{
			get
			{
				return true;
			}
		}

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
			SqlBuilder = (BasicSqlBuilder)ContextInfo.DataContext.CreateSqlProvider();

			_sourceDescriptor = TargetDescriptor = ContextInfo.DataContext.MappingSchema.GetEntityDescriptor(typeof(TTarget));
			if (typeof(TTarget) != typeof(TSource))
				_sourceDescriptor = ContextInfo.DataContext.MappingSchema.GetEntityDescriptor(typeof(TSource));

			var target = (Table<TTarget>)Merge.Target;
			var sb = new StringBuilder();
			SqlBuilder.ConvertTableName(
				sb,
				target.DatabaseName ?? TargetDescriptor.DatabaseName,
				target.SchemaName ?? TargetDescriptor.SchemaName,
				target.TableName ?? TargetDescriptor.TableName);
			TargetTableName = sb.ToString();

			BuildCommandText();

			return Command.ToString();
		}
		#endregion

		#region Validation
		private static MergeOperationType[] _matchedTypes = new[]
						{
			MergeOperationType.Delete,
			MergeOperationType.Update,
			MergeOperationType.UpdateWithDelete
		};

		private static MergeOperationType[] _notMatchedBySourceTypes = new[]
		{
			MergeOperationType.DeleteBySource,
			MergeOperationType.UpdateBySource
		};

		private static MergeOperationType[] _notMatchedTypes = new[]
		{
			MergeOperationType.Insert
		};

		/// <summary>
		/// For providers, that use <see cref="BasicSqlOptimizer.GetAlternativeUpdate"/> method to build
		/// UPDATE FROM query, this property should be set to true.
		/// </summary>
		protected virtual bool ProviderUsesAlternativeUpdate
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// If true, merge command could include DeleteBySource and UpdateBySource operations. Those operations
		/// supported only by SQL Server.
		/// </summary>
		protected virtual bool BySourceOperationsSupported
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// If true, merge command could include Delete operation. This operation is a part of SQL 2008 standard.
		/// </summary>
		protected virtual bool DeleteOperationSupported
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Maximum number of oprations, allowed in single merge command. If value is less than one - there is no limits
		/// on number of commands. This option is used by providers that have limitations on number of operations like
		/// SQL Server.
		/// </summary>
		protected virtual int MaxOperationsCount
		{
			get
			{
				return 0;
			}
		}

		/// <summary>
		/// If true, merge command operations could have predicates. This is a part of SQL 2008 standard.
		/// </summary>
		protected virtual bool OperationPredicateSupported
		{
			get
			{
				return true;
			}
		}

		protected string ProviderName { get; private set; }

		/// <summary>
		/// If true, merge command could have multiple operations of the same type with predicates with upt to one
		/// command without predicate. This option is used by providers that doesn't allow multiple operations of the
		/// same type like SQL Server.
		/// </summary>
		protected virtual bool SameTypeOperationsAllowed
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// When this operation enabled, merge command cannot include Delete or Update operations together with
		/// UpdateWithDelete operation in single command. Also use of Delte and Update operations in the same command
		/// not allowed even without UpdateWithDelete operation.
		/// This is Oracle-specific operation.
		/// </summary>
		protected virtual bool UpdateWithDeleteOperationSupported
		{
			get
			{
				return false;
			}
		}

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
			var hasUpdate = false;
			var hasDelete = false;
			var hasUpdateWithDelete = false;
			foreach (var operation in Merge.Operations)
			{
				switch (operation.Type)
				{
					case MergeOperationType.Delete:
						hasDelete = true;
						if (!DeleteOperationSupported)
							throw new LinqToDBException(string.Format("Merge Delete operation is not supported by {0} provider.", ProviderName));
						if (!OperationPredicateSupported && operation.MatchedPredicate != null)
							throw new LinqToDBException(string.Format("Merge operation conditions are not supported by {0} provider.", ProviderName));
						break;
					case MergeOperationType.Insert:
						if (!OperationPredicateSupported && operation.NotMatchedPredicate != null)
							throw new LinqToDBException(string.Format("Merge operation conditions are not supported by {0} provider.", ProviderName));
						break;
					case MergeOperationType.Update:
						hasUpdate = true;
						if (!OperationPredicateSupported && operation.MatchedPredicate != null)
							throw new LinqToDBException(string.Format("Merge operation conditions are not supported by {0} provider.", ProviderName));
						break;
					case MergeOperationType.DeleteBySource:
						if (!BySourceOperationsSupported)
							throw new LinqToDBException(string.Format("Merge Delete By Source operation is not supported by {0} provider.", ProviderName));
						if (!OperationPredicateSupported && operation.BySourcePredicate != null)
							throw new LinqToDBException(string.Format("Merge operation conditions are not supported by {0} provider.", ProviderName));
						break;
					case MergeOperationType.UpdateBySource:
						if (!BySourceOperationsSupported)
							throw new LinqToDBException(string.Format("Merge Update By Source operation is not supported by {0} provider.", ProviderName));
						if (!OperationPredicateSupported && operation.BySourcePredicate != null)
							throw new LinqToDBException(string.Format("Merge operation conditions are not supported by {0} provider.", ProviderName));
						break;
					case MergeOperationType.UpdateWithDelete:
						hasUpdateWithDelete = true;
						if (!UpdateWithDeleteOperationSupported)
							throw new LinqToDBException(string.Format("UpdateWithDelete operation not supported by {0} provider.", ProviderName));
						break;
				}
			}

			// update/delete/updatewithdelete combinations validation
			if (hasUpdateWithDelete && hasUpdate)
				throw new LinqToDBException(string.Format("Update operation with UpdateWithDelete operation in the same Merge command not supported by {0} provider.", ProviderName));
			if (hasUpdateWithDelete && hasDelete)
				throw new LinqToDBException(string.Format("Delete operation with UpdateWithDelete operation in the same Merge command not supported by {0} provider.", ProviderName));
			if (UpdateWithDeleteOperationSupported && hasUpdate && hasDelete)
				throw new LinqToDBException(string.Format("Delete and Update operations in the same Merge command not supported by {0} provider.", ProviderName));

			// - operations without conditions not placed before operations with conditions in each match group
			// - there is no multiple operations without condition in each match group
			ValidateGroupConditions(_matchedTypes);
			ValidateGroupConditions(_notMatchedTypes);
			ValidateGroupConditions(_notMatchedBySourceTypes);

			// validate that there is no duplicate operations (by type) if provider doesn't support them
			if (!SameTypeOperationsAllowed && Merge.Operations.GroupBy(_ => _.Type).Any(_ => _.Count() > 1))
				throw new LinqToDBException(string.Format("Multiple operations of the same type are not supported by {0} provider.", ProviderName));
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
			if (source == null) throw new ArgumentNullException("source");

			return source.Provider.Execute<MergeContextParser.Context>(
				Expression.Call(
					null,
					_methodInfo.MakeGenericMethod(typeof(TSource)),
					new[] { source.Expression }));
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
			var call = buildInfo.Expression as MethodCallExpression;
			return call != null && call.Method.Name == "GetMergeContext";
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

			private Action UpdateParameters;

			public Context(IBuildContext context) : base(context)
			{
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				query.DoNotChache = true;
				query.SetNonQueryQuery();

				SetParameters = () => query.SetParameters(query.Expression, null, 0);

				query.GetElement = (ctx, db, expr, ps) => this;

				UpdateParameters = () =>
				{
					query.Queries[0].Parameters.Clear();
					query.Queries[0].Parameters.AddRange(Builder.CurrentSqlParameters);
				};
			}

			public SqlInfo[] FixSelectList()
			{
				var columns = base.ConvertToIndex(null, 1, ConvertFlags.All);

				UpdateParameters();

				return columns;
			}
		}
	}
}