#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using System.Collections.Concurrent;
	using System.Diagnostics;
	using Async;
	using Builder;
	using Common;
	using Common.Internal.Cache;
	using Data;
	using Extensions;
	using LinqToDB.Expressions;
	using SqlQuery;

	static partial class QueryRunner
	{
		public static class Cache<T>
		{
			static Cache()
			{
				Query.CacheCleaners.Enqueue(ClearCache);
			}

			public static void ClearCache()
			{
				QueryCache.Compact(1);
			}

			internal static MemoryCache QueryCache { get; } = new MemoryCache(new MemoryCacheOptions());
		}

		#region Mapper

		class Mapper<T>
		{
			public Mapper(Expression<Func<IQueryRunner,IDataReader,T>> mapperExpression)
			{
				_expression = mapperExpression;
			}

			readonly Expression<Func<IQueryRunner,IDataReader,T>> _expression;
			readonly ConcurrentDictionary<Type, ReaderMapperInfo> _mappers = new ConcurrentDictionary<Type, ReaderMapperInfo>();

			public IQueryRunner QueryRunner;

			class ReaderMapperInfo
			{
				public Expression<Func<IQueryRunner, IDataReader, T>> MapperExpression;
				public Func<IQueryRunner, IDataReader, T>             Mapper;
				public bool                                           IsFaulted;
			}

			public T Map(IDataContext context, IQueryRunner queryRunner, IDataReader dataReader)
			{
				var dataReaderType = dataReader.GetType();

				ParameterExpression oldVariable;
				ParameterExpression newVariable;
				LambdaExpression    converterExpr;
				Type                variableType;
				ReaderMapperInfo    mapperInfo;

				if (!_mappers.TryGetValue(dataReaderType, out mapperInfo))
				{
					converterExpr = context.MappingSchema.GetConvertExpression(dataReaderType, typeof(IDataReader), false, false);
					variableType  = converterExpr != null ? context.DataReaderType : dataReaderType;

					oldVariable = null;
					newVariable = null;
					var mapperExpression = (Expression<Func<IQueryRunner,IDataReader,T>>)_expression.Transform(
						e => {
							if (e is ConvertFromDataReaderExpression ex)
								return ex.Reduce(context, dataReader, newVariable).Transform(replaceVariable);

							return replaceVariable(e);
						});

					var qr = QueryRunner;
					if (qr != null)
						qr.MapperExpression = mapperExpression;

					var mapper = mapperExpression.Compile();
					mapperInfo = new ReaderMapperInfo() { MapperExpression = mapperExpression, Mapper = mapper };
					_mappers.TryAdd(dataReaderType, mapperInfo);
				}

				try
				{
					return mapperInfo.Mapper(queryRunner, dataReader);
				}
				catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is LinqToDBConvertException)
				{
					// TODO: debug cases when our tests go into slow-mode (e.g. sqlite.ms)
					if (mapperInfo.IsFaulted)
						throw;

					if (DataConnection.TraceSwitch.TraceInfo)
						DataConnection.WriteTraceLine(
							$"Mapper has switched to slow mode. Mapping exception: {ex.Message}",
							DataConnection.TraceSwitch.DisplayName,
							TraceLevel.Error);

					var qr = QueryRunner;
					if (qr != null)
						qr.MapperExpression = mapperInfo.MapperExpression;

					converterExpr = context.MappingSchema.GetConvertExpression(dataReaderType, typeof(IDataReader), false, false);
					variableType  = converterExpr != null ? context.DataReaderType : dataReaderType;

					oldVariable = null;
					newVariable = null;
					var expression = (Expression<Func<IQueryRunner, IDataReader, T>>)_expression.Transform(e => {
						if (e is ConvertFromDataReaderExpression ex)
							return new ConvertFromDataReaderExpression(ex.Type, ex.Index, newVariable, context);

						return replaceVariable(e);
					});

					mapperInfo.Mapper = expression.Compile();

					mapperInfo.IsFaulted = true;

					return mapperInfo.Mapper(queryRunner, dataReader);
				}

				Expression replaceVariable(Expression e)
				{
					if (e is ParameterExpression vex && vex.Name == "ldr")
					{
						oldVariable = vex;
						return newVariable ?? (newVariable = Expression.Variable(variableType, "ldr"));
					}

					if (e is BinaryExpression bex
						&& bex.NodeType == ExpressionType.Assign
						&& bex.Left == oldVariable)
					{
						Expression dataReaderExpression = Expression.Convert(_expression.Parameters[1], dataReaderType);

						if (converterExpr != null)
						{
							dataReaderExpression = Expression.Convert(converterExpr.GetBody(dataReaderExpression), variableType);
						}

						return Expression.Assign(newVariable, dataReaderExpression);
					}

					return e;
				}
			}
		}

		#endregion

		#region Helpers

		static void FinalizeQuery(Query query)
		{
			foreach (var sql in query.Queries)
			{
				sql.Statement = query.SqlOptimizer.Finalize(sql.Statement);

				sql.Statement.UpdateIsParameterDepended();
				sql.Statement.SetAliases();

				// normalize parameters
				if (query.SqlProviderFlags.IsParameterOrderDependent)
					sql.Statement = NormalizeParameters(sql.Statement, sql.Parameters);
				else
					sql.Statement.CollectParameters();

				var parameters =
					sql.Parameters
						.Select(p => new {p, idx = sql.Statement.Parameters.IndexOf(p.SqlParameter)})
						.OrderBy(p => p.idx)
						.Select(p => p.p);

				var alreadyAdded = new HashSet<SqlParameter>(sql.Parameters.Select(pp => pp.SqlParameter));

				var runtime = sql.Statement.Parameters.Where(p => !alreadyAdded.Contains(p));

				// combining with dynamically created parameters

				parameters = parameters.Concat(
					runtime.Select(p => new ParameterAccessor(Expression.Constant(p.Value), (e, o) => p.Value,
						(e, o) => p.DataType != DataType.Undefined || p.Value == null
							? p.DataType
							: query.MappingSchema.GetDataType(p.Value.GetType()).DataType,
						(e, o) => p.DbType,
						(e, o) => p.DbSize,
						p))
				);

				sql.Parameters = parameters.ToList();
			}
		}

		private static bool HasQueryParameters(ISqlExpression expr)
		{
			var hasParameters  = null != new QueryVisitor().Find(expr,
				el => el.ElementType == QueryElementType.SqlParameter &&
				      ((SqlParameter)el).IsQueryParameter);

			return hasParameters;
		}

		private static T NormalizeExpressions<T>(T expression) 
			where T : class, IQueryElement
		{
			var queryVisitor = new QueryVisitor();
			var result = queryVisitor.ConvertImmutable(expression, e =>
			{
				if (e.ElementType == QueryElementType.SqlExpression)
				{
					var expr = (SqlExpression)e;

					// we interested in modifying only expressions which have parameters
					if (HasQueryParameters(expr))
					{
						if (expr.Expr.IsNullOrEmpty() || expr.Parameters.Length == 0)
							return expr;

						var newExpressions = new List<ISqlExpression>();

						var newExpr = QueryHelper.TransformExpressionIndexes(expr.Expr,
							idx =>
							{
								if (idx >= 0 && idx < expr.Parameters.Length)
								{
									var paramExpr  = expr.Parameters[idx];
									var normalized = paramExpr;
									var newIndex   = newExpressions.Count;

									if (newExpressions.Contains(normalized) && HasQueryParameters(normalized))
									{
										normalized = (ISqlExpression)normalized.Clone(
											new Dictionary<ICloneableElement, ICloneableElement>(),
											c => true);
									}

									newExpressions.Add(normalized);
									return newIndex;
								}
								return idx;
							});

						// always create copy
						var newExpression = new SqlExpression(expr.SystemType, newExpr, expr.Precedence, expr.IsAggregate, newExpressions.ToArray());
						// force re-entrance
						queryVisitor.VisitedElements.Remove(expr);
						queryVisitor.VisitedElements.Add(expr, null);
						return newExpression;
					}
				}
				return e;
			});

			return result;
		}

		private static SqlStatement NormalizeParameters(SqlStatement statement, List<ParameterAccessor> accessors)
		{
			// remember accessor indexes
			new QueryVisitor().VisitAll(statement, e =>
			{
				if (e.ElementType == QueryElementType.SqlParameter)
				{
					var parameter = (SqlParameter)e;
					if (parameter.IsQueryParameter)
					{
						var idx = accessors.FindIndex(a => object.ReferenceEquals(a.SqlParameter, parameter));
						parameter.AccessorId = idx >= 0 ? (int?)idx : null;
					}
				}
			});
			
			// correct expressions, we have to put expressions in correct order and duplicate them if they are reused 
			statement = NormalizeExpressions(statement);

			var found                     = new HashSet<ISqlExpression>();
			var columnExpressions         = new HashSet<ISqlExpression>();
			var parameterDuplicateVisitor = new QueryVisitor();
			statement = parameterDuplicateVisitor.ConvertImmutable(statement, e =>
			{
				if (e.ElementType == QueryElementType.SqlParameter)
				{
					var parameter = (SqlParameter)e;
					if (parameter.IsQueryParameter)
					{
						var parentElement = parameterDuplicateVisitor.ParentElement;
						if (parentElement is SqlColumn)
							columnExpressions.Add(parameter);
						else if (parentElement.ElementType == QueryElementType.SetExpression)
						{
							// consider that expression is already processed by SelectQuery and we do not need duplication.
							// It is specific how InsertStatement is built
							if (columnExpressions.Contains(parameter))
								return parameter;
						}

						if (!found.Add(parameter))
						{
							var newParameter =
								(SqlParameter)parameter.Clone(new Dictionary<ICloneableElement, ICloneableElement>(),
									c => true);
							return newParameter;
						}

						// notify visitor to process this parameter always
						parameterDuplicateVisitor.VisitedElements.Add(parameter, null);
					}
				}

				return e;
			});

			// clone accessors for new parameters
			new QueryVisitor().Visit(statement, e =>
			{
				if (e.ElementType == QueryElementType.SqlParameter)
				{
					var parameter = (SqlParameter)e;
					if (parameter.IsQueryParameter && parameter.AccessorId != null)
					{
						var accessor = accessors[parameter.AccessorId.Value];
						if (!ReferenceEquals(accessor.SqlParameter, parameter))
						{
							var newAccessor = new ParameterAccessor
							(
								accessor.Expression,
								accessor.Accessor,
								accessor.DataTypeAccessor,
								accessor.DbTypeAccessor,
								accessor.SizeAccessor,
								parameter
							);

							parameter.AccessorId = accessors.Count;
							accessors.Add(newAccessor);
						}
					}
				}
			});

			statement.CollectParameters();

			return statement;
		}

		static void ClearParameters(Query query)
		{
#if !DEBUG
			foreach (var q in query.Queries)
				foreach (var sqlParameter in q.Parameters)
					sqlParameter.Expression = null;
#endif
		}

		static int GetParameterIndex(Query query, ISqlExpression parameter)
		{
			var parameters = query.Queries[0].Parameters;

			for (var i = 0; i < parameters.Count; i++)
			{
				var p = parameters[i].SqlParameter;

				if (p == parameter)
					return i;
			}

			throw new InvalidOperationException();
		}

		internal static void SetParameters(
			Query query, IDataContext dataContext, Expression expression, object[] parameters, int queryNumber)
		{
			var queryContext = query.Queries[queryNumber];

			foreach (var p in queryContext.Parameters)
			{
				var value = p.Accessor(expression, parameters);

				if (value is IEnumerable vs)
				{
					var type = vs.GetType();
					var etype = type.GetItemType();

					if (etype == null || etype == typeof(object) || etype.IsEnum ||
						type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
						etype.GetGenericArguments()[0].IsEnum)
					{
						var values = new List<object>();

						foreach (var v in vs)
						{
							value = v;

							if (v != null)
							{
								var valueType = v.GetType();

								if (valueType.ToNullableUnderlying().IsEnum)
									value = query.GetConvertedEnum(valueType, value);
							}

							values.Add(value);
						}

						value = values;
					}
				}

				p.SqlParameter.Value = value;

//				if (value != null && dataContext.InlineParameters && p.SqlParameter.IsQueryParameter)
//				{
//					var type = value.GetType();
//
//					if (type != typeof(byte[]) && dataContext.MappingSchema.IsScalarType(type))
//						p.SqlParameter.IsQueryParameter = false;
//				}

				var dataType = p.DataTypeAccessor(expression, parameters);

				if (dataType != DataType.Undefined)
					p.SqlParameter.DataType = dataType;

				var dbType = p.DbTypeAccessor(expression, parameters);

				if (!string.IsNullOrEmpty(dbType))
					p.SqlParameter.DbType = dbType;

				var size = p.SizeAccessor(expression, parameters);

				if (size != null)
					p.SqlParameter.DbSize = size;

			}
		}

		static ParameterAccessor GetParameter(Type type, IDataContext dataContext, SqlField field)
		{
			var exprParam = Expression.Parameter(typeof(Expression), "expr");

			Expression getter = Expression.Convert(
				Expression.Property(
					Expression.Convert(exprParam, typeof(ConstantExpression)),
					ReflectionHelper.Constant.Value),
				type);

			getter = field.ColumnDescriptor.MemberAccessor.GetterExpression.GetBody(getter);

			Expression dataTypeExpression = Expression.Constant(DataType.Undefined);
			Expression dbTypeExpression   = Expression.Constant(null, typeof(string));
			Expression dbSizeExpression   = Expression.Constant(field.Length, typeof(int?));

			var convertExpression = dataContext.MappingSchema.GetConvertExpression(new DbDataType(field.SystemType, field.DataType, field.DbType, field.Length),
				new DbDataType(typeof(DataParameter), field.DataType, field.DbType, field.Length), createDefault: false);

			if (convertExpression != null)
			{
				var body           = convertExpression.GetBody(getter);
				getter             = Expression.PropertyOrField(body, "Value");
				dataTypeExpression = Expression.PropertyOrField(body, "DataType");
				dbTypeExpression   = Expression.PropertyOrField(body, "DbType");
				dbSizeExpression   = Expression.PropertyOrField(body, "Size");
			}

			var param = ExpressionBuilder.CreateParameterAccessor(
				dataContext, getter, dataTypeExpression, dbTypeExpression, dbSizeExpression, getter, exprParam, Expression.Parameter(typeof(object[]), "ps"), field.Name.Replace('.', '_'), expr: convertExpression);

			return param;
		}

		private static Type GetType<T>(T obj, IDataContext db)
			//=> typeof(T);
			//=> obj.GetType();
			=> db.MappingSchema.GetEntityDescriptor(typeof(T)).InheritanceMapping?.Count > 0 ? obj.GetType() : typeof(T);

		#endregion

		#region SetRunQuery

		static Tuple<
			Func<Query,IDataContext,Mapper<T>,Expression,object[],int,IEnumerable<T>>,
			Func<Expression,object[],int>,
			Func<Expression,object[],int>>
			GetExecuteQuery<T>(
				Query query,
				Func<Query,IDataContext,Mapper<T>,Expression,object[],int,IEnumerable<T>> queryFunc)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			Func<Expression,object[],int> skip = null, take = null;

			var selectQuery = query.Queries[0].Statement.SelectQuery;
			var select      = selectQuery.Select;

			if (select.SkipValue != null && !query.SqlProviderFlags.GetIsSkipSupportedFlag(selectQuery))
			{
				var q = queryFunc;

				if (select.SkipValue is SqlValue value)
				{
					var n = (int)((IValueContainer)value).Value;

					if (n > 0)
					{
						queryFunc = (qq, db, mapper, expr, ps, qn) => q(qq, db, mapper, expr, ps, qn).Skip(n);
						skip  = (expr, ps) => n;
					}
				}
				else if (select.SkipValue is SqlParameter)
				{
					var i = GetParameterIndex(query, select.SkipValue);
					queryFunc = (qq, db, mapper, expr, ps, qn) => q(qq, db, mapper, expr, ps, qn).Skip((int)query.Queries[0].Parameters[i].Accessor(expr, ps));
					skip  = (expr,ps) => (int)query.Queries[0].Parameters[i].Accessor(expr, ps);
				}
				else
				{
					queryFunc = (qq, db, mapper, expr, ps, qn) => q(qq, db, mapper, expr, ps, qn).Skip((int)select.SkipValue.EvaluateExpression());
					skip  = (expr,ps) => (int)select.SkipValue.EvaluateExpression();
				}
			}

			if (select.TakeValue != null && !query.SqlProviderFlags.IsTakeSupported)
			{
				var q = queryFunc;

				if (select.TakeValue is SqlValue value)
				{
					var n = (int)((IValueContainer)value).Value;

					if (n > 0)
					{
						queryFunc = (qq, db, mapper, expr, ps, qn) => q(qq, db, mapper, expr, ps, qn).Take(n);
						take      = (expr, ps) => n;
					}
				}
				else if (select.TakeValue is SqlParameter)
				{
					var i = GetParameterIndex(query, select.TakeValue);
					queryFunc = (qq, db, mapper, expr, ps, qn) => q(qq, db, mapper, expr, ps, qn).Take((int)query.Queries[0].Parameters[i].Accessor(expr, ps));
					take      = (expr,ps) => (int)query.Queries[0].Parameters[i].Accessor(expr, ps);
				}
				else
				{
					queryFunc = (qq, db, mapper, expr, ps, qn) => q(qq, db, mapper, expr, ps, qn).Take((int)select.TakeValue.EvaluateExpression());
					take      = (expr,ps) => (int)select.TakeValue.EvaluateExpression();
				}
			}

			return Tuple.Create(queryFunc, skip, take);
		}

		static IEnumerable<T> ExecuteQuery<T>(
			Query        query,
			IDataContext dataContext,
			Mapper<T>    mapper,
			Expression   expression,
			object[]     ps,
			int          queryNumber)
		{
			using (var runner = dataContext.GetQueryRunner(query, queryNumber, expression, ps))
			try
			{
				mapper.QueryRunner = runner;

				using (var dr = runner.ExecuteReader())
				{
					while (dr.Read())
					{
						var value = mapper.Map(dataContext, runner, dr);
						runner.RowsCount++;
						yield return value;
					}
				}
			}
			finally
			{
				mapper.QueryRunner = null;
			}
		}

		static async Task ExecuteQueryAsync<T>(
			Query                         query,
			IDataContext                  dataContext,
			Mapper<T>                     mapper,
			Expression                    expression,
			object[]                      ps,
			int                           queryNumber,
			Func<T,bool>                  func,
			Func<Expression,object[],int> skipAction,
			Func<Expression,object[],int> takeAction,
			CancellationToken             cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, queryNumber, expression, ps))
			{
				try
				{
					mapper.QueryRunner = runner;

					using (var dr = await runner.ExecuteReaderAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
					{
						var skip = skipAction?.Invoke(expression, ps) ?? 0;

						while (skip-- > 0 && await dr.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
							{}

						var take = takeAction?.Invoke(expression, ps) ?? int.MaxValue;

						while (take-- > 0 && await dr.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
						{
							runner.RowsCount++;
							if (!func(mapper.Map(dataContext, runner, dr.DataReader)))
								break;
						}
					}
				}
				finally
				{
					mapper.QueryRunner = null;
				}
			}
		}

		class AsyncEnumeratorImpl<T> : IAsyncEnumerator<T>
		{
			readonly Query                         _query;
			readonly IDataContext                  _dataContext;
			readonly Mapper<T>                     _mapper;
			readonly Expression                    _expression;
			readonly object[]                      _ps;
			readonly int                           _queryNumber;
			readonly Func<Expression,object[],int> _skipAction;
			readonly Func<Expression,object[],int> _takeAction;

			IQueryRunner     _queryRunner;
			IDataReaderAsync _dataReader;
			int              _take;

			public AsyncEnumeratorImpl(
				Query                         query,
				IDataContext                  dataContext,
				Mapper<T>                     mapper,
				Expression                    expression,
				object[]                      ps,
				int                           queryNumber,
				Func<Expression,object[],int> skipAction,
				Func<Expression,object[],int> takeAction)
			{
				_query       = query;
				_dataContext = dataContext;
				_mapper      = mapper;
				_expression  = expression;
				_ps          = ps;
				_queryNumber = queryNumber;
				_skipAction  = skipAction;
				_takeAction  = takeAction;
			}

			public T Current { get; set; }

			public async Task<bool> MoveNext(CancellationToken cancellationToken)
			{
				if (_queryRunner == null)
				{
					_queryRunner = _dataContext.GetQueryRunner(_query, _queryNumber, _expression, _ps);

					_mapper.QueryRunner = _queryRunner;

					_dataReader = await _queryRunner.ExecuteReaderAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

					var skip = _skipAction?.Invoke(_expression, _ps) ?? 0;

					while (skip-- > 0)
					{
						if (!await _dataReader.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
							return false;
					}

					_take = _takeAction?.Invoke(_expression, _ps) ?? int.MaxValue;
				}

				if (_take-- > 0 && await _dataReader.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
				{
					_queryRunner.RowsCount++;

					Current = _mapper.Map(_dataContext, _queryRunner, _dataReader.DataReader);

					return true;
				}

				return false;
			}

			public void Dispose()
			{
				_queryRunner?.Dispose();
				_dataReader ?.Dispose();

				_mapper.QueryRunner = _queryRunner = null;
			}
		}

		class AsyncEnumerableImpl<T> : IAsyncEnumerable<T>
		{
			readonly Query                         _query;
			readonly IDataContext                  _dataContext;
			readonly Mapper<T>                     _mapper;
			readonly Expression                    _expression;
			readonly object[]                      _ps;
			readonly int                           _queryNumber;
			readonly Func<Expression,object[],int> _skipAction;
			readonly Func<Expression,object[],int> _takeAction;

			public AsyncEnumerableImpl(
				Query                         query,
				IDataContext                  dataContext,
				Mapper<T>                     mapper,
				Expression                    expression,
				object[]                      ps,
				int                           queryNumber,
				Func<Expression,object[],int> skipAction,
				Func<Expression,object[],int> takeAction)
			{
				_query       = query;
				_dataContext = dataContext;
				_mapper      = mapper;
				_expression  = expression;
				_ps          = ps;
				_queryNumber = queryNumber;
				_skipAction  = skipAction;
				_takeAction  = takeAction;
			}

			public IAsyncEnumerator<T> GetEnumerator()
			{
				return new AsyncEnumeratorImpl<T>(
					_query, _dataContext, _mapper, _expression, _ps, _queryNumber, _skipAction, _takeAction);
			}
		}

		static IAsyncEnumerable<T> ExecuteQueryAsync<T>(
			Query                         query,
			IDataContext                  dataContext,
			Mapper<T>                     mapper,
			Expression                    expression,
			object[]                      ps,
			int                           queryNumber,
			Func<Expression,object[],int> skipAction,
			Func<Expression,object[],int> takeAction)
		{
			return new AsyncEnumerableImpl<T>(
				query, dataContext, mapper, expression, ps, queryNumber, skipAction, takeAction);
		}

		static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataReader,T>> expression)
		{
			var executeQuery = GetExecuteQuery<T>(query, ExecuteQuery);

			ClearParameters(query);

			var mapper   = new Mapper<T>(expression);
			var runQuery = executeQuery.Item1;

			query.GetIEnumerable = (db, expr, ps) => runQuery(query, db, mapper, expr, ps, 0);

			var skipAction = executeQuery.Item2;
			var takeAction = executeQuery.Item3;

			query.GetForEachAsync = (db, expr, ps, action, token) =>
				ExecuteQueryAsync(query, db, mapper, expr, ps, 0, action, skipAction, takeAction, token);

			query.GetIAsyncEnumerable = (db, expr, ps) =>
				ExecuteQueryAsync(query, db, mapper, expr, ps, 0, skipAction, takeAction);
		}

		static readonly PropertyInfo _dataContextInfo = MemberHelper.PropertyOf<IQueryRunner>( p => p.DataContext);
		static readonly PropertyInfo _expressionInfo  = MemberHelper.PropertyOf<IQueryRunner>( p => p.Expression);
		static readonly PropertyInfo _parametersnfo   = MemberHelper.PropertyOf<IQueryRunner>( p => p.Parameters);
		static readonly PropertyInfo _rowsCountnfo    = MemberHelper.PropertyOf<IQueryRunner>( p => p.RowsCount);

		static Expression<Func<IQueryRunner,IDataReader,T>> WrapMapper<T>(
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],T>> expression)
		{
			var queryRunnerParam = Expression.Parameter(typeof(IQueryRunner), "qr");
			var dataReaderParam = Expression.Parameter(typeof(IDataReader), "dr");

			return
				Expression.Lambda<Func<IQueryRunner,IDataReader,T>>(
					Expression.Invoke(
						expression, new Expression[]
						{
							queryRunnerParam,
							Expression.Property(queryRunnerParam, _dataContextInfo),
							dataReaderParam,
							Expression.Property(queryRunnerParam, _expressionInfo),
							Expression.Property(queryRunnerParam, _parametersnfo),
						}),
					queryRunnerParam,
					dataReaderParam);
		}

		#endregion

		#region SetRunQuery / Cast, Concat, Union, OfType, ScalarSelect, Select, SequenceContext, Table

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],T>> expression)
		{
			var l = WrapMapper(expression);

			SetRunQuery(query, l);
		}

		#endregion

		#region SetRunQuery / Select 2

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],int,T>> expression)
		{
			var queryRunnerParam = Expression.Parameter(typeof(IQueryRunner), "qr");
			var dataReaderParam  = Expression.Parameter(typeof(IDataReader),  "dr");

			var l =
				Expression.Lambda<Func<IQueryRunner,IDataReader,T>>(
					Expression.Invoke(
						expression, new Expression[]
						{
							queryRunnerParam,
							Expression.Property(queryRunnerParam, _dataContextInfo),
							dataReaderParam,
							Expression.Property(queryRunnerParam, _expressionInfo),
							Expression.Property(queryRunnerParam, _parametersnfo),
							Expression.Property(queryRunnerParam, _rowsCountnfo),
						}),
					queryRunnerParam,
					dataReaderParam);

			SetRunQuery(query, l);
		}

		#endregion

		#region SetRunQuery / Aggregation, All, Any, Contains, Count

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],object>> expression)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters(query);

			var l      = WrapMapper(expression);
			var mapper = new Mapper<object>(l);

			query.GetElement      = (db, expr, ps) => ExecuteElement(query, db, mapper, expr, ps);
			query.GetElementAsync = (db, expr, ps, token) => ExecuteElementAsync<object>(query, db, mapper, expr, ps, token);
		}

		static T ExecuteElement<T>(
			Query          query,
			IDataContext   dataContext,
			Mapper<T>      mapper,
			Expression     expression,
			object[]       ps)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps))
			try
			{
				mapper.QueryRunner = runner;

				using (var dr = runner.ExecuteReader())
				{
					while (dr.Read())
					{
						var value = mapper.Map(dataContext, runner, dr);
						runner.RowsCount++;
						return value;
					}
				}

				return Array<T>.Empty.First();
			}
			finally
			{
				mapper.QueryRunner = null;
			}
		}

		static async Task<T> ExecuteElementAsync<T>(
			Query             query,
			IDataContext      dataContext,
			Mapper<object>    mapper,
			Expression        expression,
			object[]          ps,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps))
			{
				try
				{
					mapper.QueryRunner = runner;

					using (var dr = await runner.ExecuteReaderAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
					{
						if (await dr.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
						{
							runner.RowsCount++;

							var item = mapper.Map(dataContext, runner, dr.DataReader);

							return dataContext.MappingSchema.ChangeTypeTo<T>(item);
						}

						return Array<T>.Empty.First();
					}
				}
				finally
				{
					mapper.QueryRunner = null;
				}
			}
		}

		#endregion

		#region ScalarQuery

		public static void SetScalarQuery(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters(query);

			query.GetElement      = (db, expr, ps) => ScalarQuery(query, db, expr, ps);
			query.GetElementAsync = (db, expr, ps, token) => ScalarQueryAsync(query, db, expr, ps, token);
		}

		static object ScalarQuery(Query query, IDataContext dataContext, Expression expr, object[] parameters)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters))
				return runner.ExecuteScalar();
		}

		static async Task<object> ScalarQueryAsync(
			Query             query,
			IDataContext      dataContext,
			Expression        expression,
			object[]          ps,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps))
				return await runner.ExecuteScalarAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
		}

		#endregion

		#region NonQueryQuery

		public static void SetNonQueryQuery(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters(query);

			query.GetElement      = (db, expr, ps) => NonQueryQuery(query, db, expr, ps);
			query.GetElementAsync = (db, expr, ps, token) => NonQueryQueryAsync(query, db, expr, ps, token);
		}

		static int NonQueryQuery(Query query, IDataContext dataContext, Expression expr, object[] parameters)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters))
				return runner.ExecuteNonQuery();
		}

		static async Task<object> NonQueryQueryAsync(
			Query             query,
			IDataContext      dataContext,
			Expression        expression,
			object[]          ps,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps))
				return await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
		}

		#endregion

		#region NonQueryQuery2

		public static void SetNonQueryQuery2(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 2)
				throw new InvalidOperationException();

			ClearParameters(query);

			query.GetElement      = (db, expr, ps)        => NonQueryQuery2(query, db, expr, ps);
			query.GetElementAsync = (db, expr, ps, token) => NonQueryQuery2Async(query, db, expr, ps, token);
		}

		static int NonQueryQuery2(Query query, IDataContext dataContext, Expression expr, object[] parameters)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters))
			{
				var n = runner.ExecuteNonQuery();

				if (n != 0)
					return n;

				runner.QueryNumber = 1;

				return runner.ExecuteNonQuery();
			}
		}

		static async Task<object> NonQueryQuery2Async(
			Query             query,
			IDataContext      dataContext,
			Expression        expr,
			object[]          parameters,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters))
			{
				var n = await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				if (n != 0)
					return n;

				runner.QueryNumber = 1;

				return await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}
		}

		#endregion

		#region QueryQuery2

		public static void SetQueryQuery2(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 2)
				throw new InvalidOperationException();

			ClearParameters(query);

			query.GetElement      = (db, expr, ps) => QueryQuery2(query, db, expr, ps);
			query.GetElementAsync = (db, expr, ps, token) => QueryQuery2Async(query, db, expr, ps, token);
		}

		static int QueryQuery2(Query query, IDataContext dataContext, Expression expr, object[] parameters)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters))
			{
				var n = runner.ExecuteScalar();

				if (n != null)
					return 0;

				runner.QueryNumber = 1;

				return runner.ExecuteNonQuery();
			}
		}

		static async Task<object> QueryQuery2Async(
			Query             query,
			IDataContext      dataContext,
			Expression        expr,
			object[]          parameters,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters))
			{
				var n = await runner.ExecuteScalarAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

				if (n != null)
					return 0;

				runner.QueryNumber = 1;

				return await runner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}
		}

		#endregion

		#region GetSqlText

		public static string GetSqlText(Query query, IDataContext dataContext, Expression expr, object[] parameters, int idx)
		{
			var runner = dataContext.GetQueryRunner(query, 0, expr, parameters);
			return runner.GetSqlText();
		}

		#endregion
	}
}
