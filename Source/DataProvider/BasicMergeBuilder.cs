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

		public BasicMergeBuilder(IMerge<TTarget, TSource> merge, string providerName)
		{
			Merge = (MergeDefinition<TTarget, TSource>)merge;
			ProviderName = providerName;
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
		private IQueryable<TTuple> AddConditionOverSourceAndTarget<TTuple>(
																	IQueryable<TTuple> query,
																	Expression<Func<TTarget, TSource, bool>> predicate)
		{
			var p = Expression.Parameter(typeof(TTuple));

			var rewriter = new ExpressionParameterRewriter(p, predicate.Parameters[0], predicate.Parameters[1]);

			var newPredicate = Expression.Lambda<Func<TTuple, bool>>(rewriter.Visit(predicate.Body), p);

			return query.Where(newPredicate);
		}

		private void GenerateDefaultMatchPredicate()
		{
			var first = true;
			var targetAlias = (string)SqlBuilder.Convert(_targetAlias, ConvertType.NameToQueryTableAlias);
			var sourceAlias = (string)SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias);
			foreach (var column in _targetColumns.Where(c => c.Column.IsPrimaryKey))
			{
				if (!first)
					Command.AppendLine(" AND");

				first = false;
				Command
					.AppendFormat("\t{1}.{0} = {2}.{0}", column.Name, targetAlias, sourceAlias);
			}
		}

		protected void GeneratePredicateByTargetAndSource(Expression<Func<TTarget, TSource, bool>> predicate)
		{
			var query = _connection.GetTable<TTarget>()
				.SelectMany(_ => _connection.GetTable<TSource>(), (t, s) => new { t, s });

			query = AddConditionOverSourceAndTarget(query, predicate);

			var ctx = query.GetContext();
			var sql = ctx.SelectQuery;

			var selectContext = (SelectContext)ctx.Context;

			MoveJoinsToSubqueries(sql, _targetAlias, SourceAlias, QueryElement.Where);

			ctx.SetParameters();
			SaveParameters(sql.Parameters);

			SqlBuilder.BuildWhereSearchCondition(sql, Command);

			Command.Append(" ");
		}

		protected void GenerateSingleTablePredicate<TTable>(Expression<Func<TTable, bool>> predicate, string tableAlias)
			where TTable : class
		{
			var qry = _connection.GetTable<TTable>().Where(predicate);
			var ctx = qry.GetContext();
			var sql = ctx.SelectQuery;

			MoveJoinsToSubqueries(sql, tableAlias, null, QueryElement.Where);

			ctx.SetParameters();
			SaveParameters(sql.Parameters);

			SqlBuilder.BuildWhereSearchCondition(sql, Command);

			Command.Append(" ");
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
		protected virtual void GenerateSource()
		{
			Command.Append("USING ");

			if (Merge.QueryableSource != null && SuportsSourceSubQuery)
				GenerateSourceSubquery(Merge.QueryableSource);
			else
			{
				var source = Merge.EnumerableSource ?? Merge.QueryableSource;
				if (SupportsSourceDirectValues)
					GenerateSourceDirectValues(source);
				else
					GenerateSourceSubQueryValues(source);
			}
		}

		private void GenerateAsSource(IEnumerable<string> columnNames)
		{
			Command.AppendFormat(") {0}", SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias));

			if (columnNames != null && SupportsColumnAliasesInTableAlias)
			{
				if (!columnNames.Any())
					throw new LinqToDBException("Merge source doesn't have any columns.");

				Command.AppendLine("(");

				var first = true;
				foreach (var columnName in columnNames)
				{
					if (!first)
						Command
							.AppendLine(",");

					first = false;

					Command
						.AppendFormat("\t{0}", SqlBuilder.Convert(columnName, ConvertType.NameToQueryFieldAlias));
				}

				Command
					.AppendLine()
					.AppendLine(")");
			}
			else
				Command.AppendLine();
		}

		private void GenerateEmptySource()
		{
			Command.Append("(SELECT ");

			var columnTypes = GetSourceColumnTypes();

			// TODO: source columns
			for (var i = 0; i < _targetColumns.Length; i++)
			{
				if (i > 0)
					Command.Append(", ");

				AddSourceValue(
					ContextInfo.MappingSchema.ValueToSqlConverter,
					_sourceDescriptor.Columns[i],
					columnTypes[i],
					null);

				Command
					.Append(" ")
					.Append(_targetColumns[i].Name);
			}

			Command
				.Append(" FROM ")
				.Append(TargetTableName)
				.Append(" WHERE 1 = 0) ")
				.AppendLine((string)SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias));
		}

		private void GenerateSourceDirectValues(IEnumerable<TSource> source)
		{
			var hasData = false;

			var columnTypes = GetSourceColumnTypes();

			var valueConverter = ContextInfo.MappingSchema.ValueToSqlConverter;

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
					var value = column.GetValue(ContextInfo.MappingSchema, item);

					AddSourceValue(valueConverter, column, columnTypes[i], value);

					if (!SupportsColumnAliasesInTableAlias)
						Command.AppendFormat(" {0}", SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryFieldAlias));
				}

				Command.Append(")");
			}

			if (hasData)
				GenerateAsSource(_sourceDescriptor.Columns.Select(_ => _.ColumnName));
			else
				GenerateEmptySource();
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

		private void GenerateSourceSubquery(IQueryable<TSource> queryableSource)
		{
			Command.Append("(");

			var ctx = queryableSource.GetMergeContext();
			var query = ctx.SelectQuery;

			// update list of selected fields
			var info = ctx.FixSelectList();

			query.Select.Columns.Clear();
			foreach (var column in info)
			{
				var columnDescriptor = _sourceDescriptor.Columns.Where(_ => _.MemberInfo == column.Members[0]).Single();

				var alias = (string)SqlBuilder.Convert(columnDescriptor.ColumnName, ConvertType.NameToQueryField);
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

			GenerateAsSource(null);
		}

		private void GenerateSourceSubQueryValues(IEnumerable<TSource> source)
		{
			var hasData = false;

			var columnTypes = GetSourceColumnTypes();

			var valueConverter = ContextInfo.MappingSchema.ValueToSqlConverter;

			foreach (var item in source)
			{
				if (hasData)
					Command
						.AppendLine()
						.AppendLine("\t\tUNION ALL");
				else
					Command
						.AppendLine("(");

				hasData = true;

				Command.Append("\tSELECT ");

				for (var i = 0; i < _sourceDescriptor.Columns.Count; i++)
				{
					if (i > 0)
						Command.Append(",");

					var column = _sourceDescriptor.Columns[i];
					var value = column.GetValue(ContextInfo.MappingSchema, item);

					AddSourceValue(valueConverter, column, columnTypes[i], value);

					if (!SupportsColumnAliasesInTableAlias)
						Command.AppendFormat(" {0}", SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryFieldAlias));
				}

				if (FakeSourceTable != null)
				{
					Command.Append(" FROM ");
					AddFakeSourceTableName();
				}
			}

			if (hasData)
				GenerateAsSource(_sourceDescriptor.Columns.Select(_ => _.ColumnName));
			else
				GenerateEmptySource();
		}

		protected virtual void AddFakeSourceTableName()
		{
			SqlBuilder.BuildTableName(Command, FakeSourceTableDatabase, FakeSourceTableOwner, FakeSourceTable);
		}

		private SqlDataType[] GetSourceColumnTypes()
		{
			return _sourceDescriptor.Columns
				.Select(c => new SqlDataType(c.DataType, c.MemberType, c.Length, c.Precision, c.Scale))
				.ToArray();
		}
		#endregion

		#region MERGE Generation
		protected virtual void GenerateMatch()
		{
			Command.Append("ON (");

			if (Merge.MatchPredicate == null)
				GenerateDefaultMatchPredicate();
			else
				GeneratePredicateByTargetAndSource(Merge.MatchPredicate);

			Command.AppendLine(")");
		}

		protected virtual void GenerateMergeInto()
		{
			Command
				.Append("MERGE INTO ")
				.Append(TargetTableName)
				.Append(" ")
				.AppendLine((string)SqlBuilder.Convert(_targetAlias, ConvertType.NameToQueryTableAlias));
		}

		protected virtual void GenerateOperation(MergeDefinition<TTarget, TSource>.Operation operation)
		{
			switch (operation.Type)
			{
				case MergeOperationType.Update:
					GenerateUpdate(operation.MatchedPredicate, operation.UpdateExpression);
					break;
				case MergeOperationType.Delete:
					GenerateDelete(operation.MatchedPredicate);
					break;
				case MergeOperationType.Insert:
					GenerateInsert(operation.NotMatchedPredicate, operation.CreateExpression);
					break;
				case MergeOperationType.UpdateWithDelete:
					GenerateUpdateWithDelete(operation.MatchedPredicate, operation.UpdateExpression, operation.MatchedPredicate2);
					break;
				case MergeOperationType.DeleteBySource:
					GenerateDeleteBySource(operation.BySourcePredicate);
					break;
				case MergeOperationType.UpdateBySource:
					GenerateUpdateBySource(operation.BySourcePredicate, operation.UpdateBySourceExpression);
					break;
				default:
					throw new InvalidOperationException();
			}
		}

		protected virtual void GenerateUpdateWithDelete(
			Expression<Func<TTarget, TSource, bool>> updatePredicate,
			Expression<Func<TTarget, TSource, TTarget>> updateExpression,
			Expression<Func<TTarget, TSource, bool>> deletePredicate)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Allows to add text before generated merge command.
		/// </summary>
		protected virtual void GeneratePreambule()
		{
		}

		/// <summary>
		/// Allows to add text after generated merge command. E.g. to specify command terminator if provider requires it.
		/// </summary>
		protected virtual void GenerateTerminator()
		{
		}

		private void GenerateCommand()
		{
			GeneratePreambule();

			GenerateMergeInto();

			GenerateSource();

			GenerateMatch();

			foreach (var operation in Merge.Operations)
			{
				GenerateOperation(operation);
			}

			GenerateTerminator();
		}
		#endregion

		#region Operations: DELETE
		protected virtual void GenerateDelete(Expression<Func<TTarget, TSource, bool>> predicate)
		{
			Command
				.AppendLine()
				.Append("WHEN MATCHED ");

			if (predicate != null)
			{
				Command.Append("AND ");
				GeneratePredicateByTargetAndSource(predicate);
			}

			Command
				.AppendLine("THEN DELETE");
		}
		#endregion

		#region Operations: DELETE BY SOURCE
		private void GenerateDeleteBySource(Expression<Func<TTarget, bool>> predicate)
		{
			Command
				.AppendLine()
				.Append("WHEN NOT MATCHED By Source ");

			if (predicate != null)
			{
				Command.Append("AND ");
				GenerateSingleTablePredicate(predicate, _targetAlias);
			}

			Command
				.AppendLine("THEN DELETE");
		}
		#endregion

		#region Operations: INSERT
		protected virtual void GenerateInsert(
													Expression<Func<TSource, bool>> predicate,
													Expression<Func<TSource, TTarget>> create)
		{
			Command
				.AppendLine()
				.Append("WHEN NOT MATCHED ");

			if (predicate != null)
			{
				Command.Append("AND ");
				GenerateSingleTablePredicate(predicate, SourceAlias);
			}

			Command
				.AppendLine("THEN INSERT");

			if (create != null)
				GenerateCustomInsert(create);
			else
				GenerateDefaultInsert();
		}

		protected virtual void OnInsertWithIdentity()
		{
		}

		protected void GenerateCustomInsert(Expression<Func<TSource, TTarget>> create)
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

			var qry = Query<int>.GetQuery(ContextInfo, insertExpression);
			var query = qry.Queries[0].SelectQuery;

			query.Insert.Into.Alias = _targetAlias;

			// we need Insert type for proper query cloning (maybe this is a bug in clone function?)
			query.QueryType = QueryType.Insert;

			MoveJoinsToSubqueries(query, SourceAlias, null, QueryElement.InsertSetter);

			// we need InsertOrUpdate for sql builder to generate values clause
			query.QueryType = QueryType.InsertOrUpdate;

			qry.SetParameters(insertExpression, null, 0);

			SaveParameters(query.Parameters);

			if (IsIdentityInsertSupported
				&& query.Insert.Items.Any(_ => _.Column is SqlField && ((SqlField)_.Column).IsIdentity))
				OnInsertWithIdentity();

			SqlBuilder.BuildInsertClauseHelper(query, Command);
		}

		protected void GenerateDefaultInsert()
		{
			var insertColumns = _targetColumns
				.Where(c => IsIdentityInsertSupported && c.Column.IsIdentity || !c.Column.SkipOnInsert)
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
				Command.AppendFormat("\t\t{0}", column.Name);
			}

			Command
				.AppendLine()
				.AppendLine("\t)")
				.AppendLine("\tVALUES")
				.AppendLine("\t(");

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
					column.Name,
					SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias));
			}

			Command
				.AppendLine()
				.AppendLine("\t)");
		}
		#endregion

		#region Operations: UPDATE
		private enum QueryElement
		{
			Where,
			InsertSetter,
			UpdateSetter
		}

		private static IQueryElement ConvertToSubquery(
							SelectQuery sql,
							IQueryElement element,
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

		private static void MoveJoinsToSubqueries(
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

				switch (part)
				{
					case QueryElement.Where:
						// replace references to fields from associated tables to subqueries in where clause
						var whereClause = new QueryVisitor()
							.Convert(sql.Where, element => ConvertToSubquery(sql, element, tableSet, tables, firstTable, secondTable))
							.SearchCondition.Conditions.ToList();

						// replace WHERE condition with new one
						sql.Where.SearchCondition.Conditions.Clear();
						sql.Where.SearchCondition.Conditions.AddRange(whereClause);
						break;
					case QueryElement.InsertSetter:
						for (var i = 0; i < sql.Insert.Items.Count; i++)
						{
							sql.Insert.Items[i] = new QueryVisitor()
								.Convert(sql.Insert.Items[i], element => ConvertToSubquery(sql, element, tableSet, tables, firstTable, secondTable));
						}
						break;
					case QueryElement.UpdateSetter:
						for (var i = 0; i < sql.Update.Items.Count; i++)
						{
							sql.Update.Items[i] = new QueryVisitor()
								.Convert(sql.Update.Items[i], element => ConvertToSubquery(sql, element, tableSet, tables, firstTable, secondTable));
						}
						break;
					default:
						throw new InvalidOperationException();
				}
			}

			sql.From.Tables[0].Alias = firstTableAlias;

			if (secondTableAlias != null)
			{
				if (tables.Count > baseTablesCount)
					sql.From.Tables[0].Joins[0].Table.Alias = secondTableAlias;
				else
					sql.From.Tables[1].Alias = secondTableAlias;
			}
		}

		protected void GenerateCustomUpdate(Expression<Func<TTarget, TSource, TTarget>> update)
		{
			// build update query
			var target = _connection.GetTable<TTarget>();
			var updateQuery = target.SelectMany(_ => _connection.GetTable<TSource>(), (t, s) => new { t, s });
			var predicate = RewriteUpdatePredicateParameters(updateQuery, update);

			var updateExpression = Expression.Call(
				null,
				LinqExtensions._updateMethodInfo.MakeGenericMethod(new[] { updateQuery.GetType().GetGenericArgumentsEx()[0], typeof(TTarget) }),
				new[] { updateQuery.Expression, target.Expression, Expression.Quote(predicate) });

			var qry = Query<int>.GetQuery(ContextInfo, updateExpression);
			var query = qry.Queries[0].SelectQuery;
			query.Update.Table.Alias = _targetAlias;

			MoveJoinsToSubqueries(query, _targetAlias, SourceAlias, QueryElement.UpdateSetter);

			qry.SetParameters(updateExpression, null, 0);
			SaveParameters(query.Parameters);

			SqlBuilder.BuildUpdateSetHelper(query, Command);
		}

		protected void GenerateDefaultUpdate()
		{
			var updateColumns = _targetColumns
				.Where(c => !c.Column.IsPrimaryKey && !c.Column.IsIdentity && !c.Column.SkipOnUpdate)
				.ToList();

			if (updateColumns.Count > 0)
			{
				Command.AppendLine("\tSET");

				var maxLen = updateColumns.Max(c => c.Name.Length);

				var first = true;
				foreach (var column in updateColumns)
				{
					if (!first)
						Command.AppendLine(",");

					first = false;

					Command
						.AppendFormat("\t\t{0} ", column.Name)
						.Append(' ', maxLen - column.Name.Length)
						.AppendFormat("= {1}.{0}", column.Name, SqlBuilder.Convert(SourceAlias, ConvertType.NameToQueryTableAlias));
				}
			}
			else
				throw new LinqToDBException("Merge.Update call requires updatable columns");
		}

		protected virtual void GenerateUpdate(
					Expression<Func<TTarget, TSource, bool>> predicate,
					Expression<Func<TTarget, TSource, TTarget>> update)
		{
			Command
				.AppendLine()
				.Append("WHEN MATCHED ");

			if (predicate != null)
			{
				Command.Append("AND ");
				GeneratePredicateByTargetAndSource(predicate);
			}

			Command.AppendLine(" THEN UPDATE");

			if (update != null)
				GenerateCustomUpdate(update);
			else
				GenerateDefaultUpdate();
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
		private void GenerateUpdateBySource(
													Expression<Func<TTarget, bool>> predicate,
													Expression<Func<TTarget, TTarget>> update)
		{
			Command
				.AppendLine()
				.Append("WHEN NOT MATCHED By Source ");

			if (predicate != null)
			{
				Command.Append("AND ");
				GenerateSingleTablePredicate(predicate, _targetAlias);
			}

			Command.AppendLine("THEN UPDATE");

			var updateExpression = Expression.Call(
				null,
				LinqExtensions._updateMethodInfo2.MakeGenericMethod(new[] { typeof(TTarget) }),
				new[] { _connection.GetTable<TTarget>().Expression, Expression.Quote(update) });

			var qry = Query<int>.GetQuery(ContextInfo, updateExpression);
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

		protected BasicSqlBuilder SqlBuilder { get; private set; }

		private ColumnInfo[] _targetColumns;

		protected StringBuilder Command
		{
			get
			{
				return _command;
			}
		}

		protected int EnumerableSourceSize { get; private set; }

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

		protected EntityDescriptor TargetDescriptor { get; private set; }

		/// <summary>
		/// Target table name, ready for use in SQL. Could include database/schema names or/and escaping.
		/// </summary>
		protected string TargetTableName { get; private set; }

		protected IDataContextInfo ContextInfo
		{
			get
			{
				return Merge.Target.DataContextInfo;
			}
		}

		/// <summary>
		/// Generates SQL and parameters for merge command.
		/// </summary>
		/// <returns>Returns merge command SQL text.</returns>
		public virtual string BuildCommand()
		{
			// prepare required objects
			SqlBuilder = (BasicSqlBuilder)ContextInfo.CreateSqlBuilder();

			_sourceDescriptor = TargetDescriptor = ContextInfo.MappingSchema.GetEntityDescriptor(typeof(TTarget));
			if (typeof(TTarget) != typeof(TSource))
				_sourceDescriptor = ContextInfo.MappingSchema.GetEntityDescriptor(typeof(TSource));

			_connection = ContextInfo.DataContext as DataConnection;

			_targetColumns = TargetDescriptor.Columns
				.Select(c => new ColumnInfo()
				{
					Column = c,
					Name = (string)SqlBuilder.Convert(c.ColumnName, ConvertType.NameToQueryField)
				})
				.ToArray();

			var target = (Table<TTarget>)Merge.Target;
			var sb = new StringBuilder();
			SqlBuilder.ConvertTableName(
				sb,
				target.DatabaseName ?? TargetDescriptor.DatabaseName,
				target.SchemaName ?? TargetDescriptor.SchemaName,
				target.TableName ?? TargetDescriptor.TableName);
			TargetTableName = sb.ToString();

			Merge = AddExtraCommands(Merge);

			GenerateCommand();

			return Command.ToString();
		}

		protected virtual MergeDefinition<TTarget, TSource> AddExtraCommands(MergeDefinition<TTarget, TSource> merge)
		{
			return merge;
		}

		private class ColumnInfo
		{
			public ColumnDescriptor Column;

			public string Name;
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

		protected string ProviderName { get; private set; }

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
