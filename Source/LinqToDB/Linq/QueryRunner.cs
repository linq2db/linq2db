using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
	using System.Diagnostics.CodeAnalysis;
	using Async;
	using Builder;
	using Common;
	using Common.Internal.Cache;
	using Common.Logging;
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

			class ReaderMapperInfo
			{
				public Expression<Func<IQueryRunner, IDataReader, T>> MapperExpression = null!;
				public Func<IQueryRunner, IDataReader, T>             Mapper = null!;
				public bool                                           IsFaulted;
			}

			public T Map(IDataContext context, IQueryRunner queryRunner, IDataReader dataReader)
			{
				var dataReaderType = dataReader.GetType();

				if (!_mappers.TryGetValue(dataReaderType, out var mapperInfo))
				{
					var mapperExpression = TransformMapperExpression(context, dataReader, dataReaderType, false);

					queryRunner.MapperExpression = mapperExpression;

					var mapper = mapperExpression.Compile();
					mapperInfo = new ReaderMapperInfo() { MapperExpression = mapperExpression, Mapper = mapper };
					_mappers.TryAdd(dataReaderType, mapperInfo);
				}

				try
				{
					return mapperInfo.Mapper(queryRunner, dataReader);
				}
				// SqlNullValueException: MySqlData
				// OracleNullValueException: managed and native oracle providers
				catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is LinqToDBConvertException || ex.GetType().Name.Contains("NullValueException"))
				{
					// TODO: debug cases when our tests go into slow-mode (e.g. sqlite.ms)
					if (mapperInfo.IsFaulted)
						throw;

					if (context.GetTraceSwitch().TraceInfo)
						context.WriteTraceLine(
							$"Mapper has switched to slow mode. Mapping exception: {ex.Message}",
							context.GetTraceSwitch().DisplayName,
							TraceLevel.Error);

					queryRunner.MapperExpression = mapperInfo.MapperExpression;

					var expression = TransformMapperExpression(context, dataReader, dataReaderType, true);

					// create new instance to avoid race conditions without locks
					var expr   = mapperInfo.MapperExpression;
					mapperInfo = new ReaderMapperInfo()
					{
						MapperExpression = expr,
						Mapper           = expression.Compile(),
						IsFaulted        = true
					};

					_mappers[dataReaderType] = mapperInfo;

					return mapperInfo.Mapper(queryRunner, dataReader);
				}
			}

			// transform extracted to separate method to avoid closures allocation on mapper cache hit
			private Expression<Func<IQueryRunner, IDataReader, T>> TransformMapperExpression(
				IDataContext context,
				IDataReader  dataReader,
				Type         dataReaderType,
				bool         slowMode)
			{
				var converterExpr = context.MappingSchema.GetConvertExpression(dataReaderType, typeof(IDataReader), false, false);
				var variableType  = converterExpr != null ? context.DataReaderType : dataReaderType;

				ParameterExpression? oldVariable = null;
				ParameterExpression? newVariable = null;

				if (slowMode)
				{
					return (Expression<Func<IQueryRunner, IDataReader, T>>)_expression.Transform(e =>
					{
						if (e is ConvertFromDataReaderExpression ex)
							return new ConvertFromDataReaderExpression(ex.Type, ex.Index, ex.Converter, newVariable!, context);

						return replaceVariable(e);
					});
				}
				else
				{
					return (Expression<Func<IQueryRunner, IDataReader, T>>)_expression.Transform(
						e =>
						{
							if (e is ConvertFromDataReaderExpression ex)
								return ex.Reduce(context, dataReader, newVariable!).Transform(replaceVariable);

							return replaceVariable(e);
						});
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
				sql.Statement = query.SqlOptimizer.Finalize(sql.Statement, query.InlineParameters);

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
					runtime.Select(p => new ParameterAccessor(Expression.Constant(p.Value), (e, pc, o) => p.Value,
						(e, pc, o) => p.Type.DataType != DataType.Undefined || p.Value == null
							? p.Type
							: p.Type.WithDataType(query.MappingSchema.GetDataType(p.Value.GetType()).Type.DataType),
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
			var result = ConvertVisitor.Convert(expression, (visitor, e) =>
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
						var newExpression = new SqlExpression(expr.SystemType, newExpr, expr.Precedence, expr.IsAggregate, expr.IsPure, newExpressions.ToArray());
						// force re-entrance
						visitor.VisitedElements[expr] = null;
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
			statement = ConvertVisitor.Convert(statement, (visitor, e) =>
			{
				if (e.ElementType == QueryElementType.SqlParameter)
				{
					var parameter = (SqlParameter)e;
					if (parameter.IsQueryParameter)
					{
						var parentElement = visitor.ParentElement;
						if (parentElement is SqlColumn)
							columnExpressions.Add(parameter);
						else if (parentElement!.ElementType == QueryElementType.SetExpression)
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
						visitor.VisitedElements.Add(parameter, null);
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
								accessor.DbDataTypeAccessor,
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
					sqlParameter.Expression = null!;
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
			Query query, Expression expression, IDataContext? parametersContext, object?[]? parameters, int queryNumber)
		{
			var queryContext = query.Queries[queryNumber];

			foreach (var p in queryContext.Parameters)
			{
				var value = p.Accessor(expression, parametersContext, parameters);

				if (value is IEnumerable vs)
				{
					var type = vs.GetType();
					var etype = type.GetItemType();

					if (etype == null || etype == typeof(object) || etype.IsEnum ||
						type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
						etype.GetGenericArguments()[0].IsEnum)
					{
						var values = new List<object?>();

						foreach (var v in vs)
						{
							value = v;

							if (v != null)
							{
								var valueType = v.GetType();

								if (valueType.ToNullableUnderlying().IsEnum)
									value = query.GetConvertedEnum(valueType, v);
							}

							values.Add(value);
						}

						value = values;
					}
				}

				p.SqlParameter.Value = value;

				var dbDataType = p.DbDataTypeAccessor(expression, parametersContext, parameters);

				p.SqlParameter.Type = p.SqlParameter.Type.WithSetValues(dbDataType);
			}
		}

		internal static ParameterAccessor GetParameter(Type type, IDataContext dataContext, SqlField field)
		{
			var exprParam = Expression.Parameter(typeof(Expression), "expr");

			Expression getter = Expression.Convert(
				Expression.Property(
					Expression.Convert(exprParam, typeof(ConstantExpression)),
					ReflectionHelper.Constant.Value),
				type);

			var descriptor    = field.ColumnDescriptor;
			var dbValueLambda = descriptor.GetDbParamLambda();

			Expression? valueGetter;
			Expression? dbDataTypeExpression;

			valueGetter = InternalExtensions.ApplyLambdaToExpression(dbValueLambda, getter);

			if (typeof(DataParameter).IsSameOrParentOf(valueGetter.Type))
			{
				dbDataTypeExpression = Expression.PropertyOrField(valueGetter, nameof(DataParameter.DbDataType));
				valueGetter          = Expression.PropertyOrField(valueGetter, nameof(DataParameter.Value));
			}
			else
			{
				var dbDataType = field.ColumnDescriptor.GetDbDataType();
				dbDataType = dbDataType.WithSystemType(valueGetter.Type);

				dbDataTypeExpression = Expression.Constant(dbDataType);
			}

			var param = ExpressionBuilder.CreateParameterAccessor(
				dataContext, valueGetter, dbDataTypeExpression, valueGetter, exprParam, Expression.Parameter(typeof(object[]), "ps"), Expression.Parameter(typeof(IDataContext), "ctx"), field.Name.Replace('.', '_'));

			return param;
		}

		private static Type GetType<T>([DisallowNull] T obj, IDataContext db)
			//=> typeof(T);
			//=> obj.GetType();
			=> db.MappingSchema.GetEntityDescriptor(typeof(T)).InheritanceMapping?.Count > 0 ? obj!.GetType() : typeof(T);

		#endregion

		#region SetRunQuery

		static IEnumerable<TSource> SkipLazy<TSource>(
			IEnumerable<TSource> source,
			ISqlExpression count)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (count  == null) throw new ArgumentNullException(nameof(count));

			using (var enumerator = source.GetEnumerator())
			{
				int cnt = -1;
				while (enumerator.MoveNext())
				{
					if (cnt < 0)
						cnt = (int)count.EvaluateExpression()!;
					if (cnt > 0)
					{
						--cnt;
						continue;
					}					
					yield return enumerator.Current;
				}
			}
		}

		static Tuple<
			Func<Query,IDataContext,Mapper<T>,Expression,object?[]?,object?[]?,int,IEnumerable<T>>,
			Func<Expression,IDataContext?,object?[]?,int>?,
			Func<Expression,IDataContext?,object?[]?,int>?>
			GetExecuteQuery<T>(
				Query query,
				Func<Query,IDataContext,Mapper<T>,Expression,object?[]?,object?[]?,int,IEnumerable<T>> queryFunc)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			Func<Expression,IDataContext?,object?[]?,int>? skip = null, take = null;

			var selectQuery = query.Queries[0].Statement.SelectQuery!;
			var select      = selectQuery.Select;

			if (select.SkipValue != null && !query.SqlProviderFlags.GetIsSkipSupportedFlag(selectQuery))
			{
				var q = queryFunc;

				if (select.SkipValue is SqlValue value)
				{
					var n = (int)value.Value!;

					if (n > 0)
					{
						queryFunc = (qq, db, mapper, expr, ps, preambles, qn) => q(qq, db, mapper, expr, ps, preambles, qn).Skip(n);
						skip      = (expr, pc, ps) => n;
					}
				}
				else if (select.SkipValue is SqlParameter skipParam && skipParam.IsQueryParameter)
				{
					var i     = GetParameterIndex(query, select.SkipValue);
					queryFunc = (qq, db, mapper, expr, ps, preambles, qn) => q(qq, db, mapper, expr, ps, preambles, qn).Skip((int)query.Queries[0].Parameters[i].Accessor(expr, db, ps)!);
					skip      = (expr, pc, ps) => (int)query.Queries[0].Parameters[i].Accessor(expr, pc, ps)!;
				}
				else
				{
					queryFunc = (qq, db, mapper, expr, ps, preambles, qn) => SkipLazy(q(qq, db, mapper, expr, ps, preambles, qn), select.SkipValue);
					skip      = (expr, pc, ps) => (int)select.SkipValue.EvaluateExpression()!;
				}
			}

			if (select.TakeValue != null && !query.SqlProviderFlags.IsTakeSupported)
			{
				var q = queryFunc;

				if (select.TakeValue is SqlValue value)
				{
					var n = (int)value.Value!;

					if (n > 0)
					{
						queryFunc = (qq, db, mapper, expr, ps, preambles, qn) => q(qq, db, mapper, expr, ps, preambles, qn).Take(n);
						take      = (expr, pc, ps) => n;
					}
				}
				else if (select.TakeValue is SqlParameter takeParam && takeParam.IsQueryParameter)
				{
					var i = GetParameterIndex(query, select.TakeValue);
					queryFunc = (qq, db, mapper, expr, ps, preambles, qn) => q(qq, db, mapper, expr, ps, preambles, qn).Take((int)query.Queries[0].Parameters[i].Accessor(expr, db, ps)!);
					take  = (expr, pc, ps) => (int)query.Queries[0].Parameters[i].Accessor(expr, pc, ps)!;
				}
				else
				{
					queryFunc = (qq, db, mapper, expr, ps, preambles, qn) => q(qq, db, mapper, expr, ps, preambles, qn).Take((int)select.TakeValue.EvaluateExpression()!);
					take      = (expr, pc, ps) => (int)select.TakeValue.EvaluateExpression()!;
				}
			}

			return Tuple.Create(queryFunc, skip, take);
		}

		static IEnumerable<T> ExecuteQuery<T>(
			Query        query,
			IDataContext dataContext,
			Mapper<T>    mapper,
			Expression   expression,
			object?[]?   ps,
			object?[]?   preambles,
			int          queryNumber)
		{
			using (var runner = dataContext.GetQueryRunner(query, queryNumber, expression, ps, preambles))
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

		static async Task ExecuteQueryAsync<T>(
			Query                         query,
			IDataContext                  dataContext,
			Mapper<T>                     mapper,
			Expression                    expression,
			object?[]?                    ps,
			object?[]?                    preambles,
			int                           queryNumber,
			Func<T,bool>                  func,
			Func<Expression,IDataContext?,object?[]?,int>? skipAction,
			Func<Expression,IDataContext?,object?[]?,int>? takeAction,
			CancellationToken             cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, queryNumber, expression, ps, preambles))
			{
				var dr = await runner.ExecuteReaderAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
#if !NET45 && !NET46
				await using (dr.ConfigureAwait(Configuration.ContinueOnCapturedContext))
#else
				using (dr)
#endif
				{
					var skip = skipAction?.Invoke(expression, dataContext, ps) ?? 0;

					while (skip-- > 0 && await dr.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
					{}

					var take = takeAction?.Invoke(expression, dataContext, ps) ?? int.MaxValue;

					while (take-- > 0 && await dr.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
					{
						runner.RowsCount++;
						if (!func(mapper.Map(dataContext, runner, dr.DataReader)))
							break;
					}
				}
			}
		}

		class AsyncEnumeratorImpl<T> : IAsyncEnumerator<T>
		{
			readonly Query                         _query;
			readonly IDataContext                  _dataContext;
			readonly Mapper<T>                     _mapper;
			readonly Expression                    _expression;
			readonly object?[]?                    _ps;
			readonly object?[]?                    _preambles;
			readonly int                           _queryNumber;
			readonly Func<Expression,IDataContext?,object?[]?,int>? _skipAction;
			readonly Func<Expression,IDataContext?,object?[]?,int>? _takeAction;
			readonly CancellationToken             _cancellationToken;

			IQueryRunner?     _queryRunner;
			IDataReaderAsync? _dataReader;
			int              _take;

			public AsyncEnumeratorImpl(
				Query                            query,
				IDataContext                     dataContext,
				Mapper<T>                        mapper,
				Expression                       expression,
				object?[]?                       ps,
				object?[]?                       preambles,
				int                              queryNumber,
				Func<Expression,IDataContext?,object?[]?,int>? skipAction,
				Func<Expression,IDataContext?,object?[]?,int>? takeAction,
				CancellationToken                cancellationToken)
			{
				_query             = query;
				_dataContext       = dataContext;
				_mapper            = mapper;
				_expression        = expression;
				_ps                = ps;
				_preambles         = preambles;
				_queryNumber       = queryNumber;
				_skipAction        = skipAction;
				_takeAction        = takeAction;
				_cancellationToken = cancellationToken;
			}

			public T Current { get; set; } = default!;

#if NET45 || NET46
			public async Task<bool> MoveNextAsync()
#else
			public async ValueTask<bool> MoveNextAsync()
#endif
			{
				if (_queryRunner == null)
				{
					_queryRunner = _dataContext.GetQueryRunner(_query, _queryNumber, _expression, _ps, _preambles);

					_dataReader = await _queryRunner.ExecuteReaderAsync(_cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

					var skip = _skipAction?.Invoke(_expression, _dataContext, _ps) ?? 0;

					while (skip-- > 0)
					{
						if (!await _dataReader.ReadAsync(_cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
							return false;
					}

					_take = _takeAction?.Invoke(_expression, _dataContext, _ps) ?? int.MaxValue;
				}

				if (_take-- > 0 && await _dataReader!.ReadAsync(_cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
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

				_queryRunner = null;
			}

#if NET45 || NET46
			public Task DisposeAsync()
			{
				Dispose();
				return TaskEx.CompletedTask;
			}
#else
			public async ValueTask DisposeAsync()
			{
				_queryRunner?.Dispose();
				if (_dataReader != null) 
					await _dataReader.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				
				_queryRunner = null;
			}
#endif
		}

		class AsyncEnumerableImpl<T> : IAsyncEnumerable<T>
		{
			readonly Query                            _query;
			readonly IDataContext                     _dataContext;
			readonly Mapper<T>                        _mapper;
			readonly Expression                       _expression;
			readonly object?[]?                       _ps;
			readonly object?[]?                       _preambles;
			readonly int                              _queryNumber;
			readonly Func<Expression,IDataContext?,object?[]?,int>? _skipAction;
			readonly Func<Expression,IDataContext?,object?[]?,int>? _takeAction;

			public AsyncEnumerableImpl(
				Query                            query,
				IDataContext                     dataContext,
				Mapper<T>                        mapper,
				Expression                       expression,
				object?[]?                       ps,
				object?[]?                       preambles,
				int                              queryNumber,
				Func<Expression,IDataContext?,object?[]?,int>? skipAction,
				Func<Expression,IDataContext?,object?[]?,int>? takeAction)
			{
				_query       = query;
				_dataContext = dataContext;
				_mapper      = mapper;
				_expression  = expression;
				_ps          = ps;
				_preambles   = preambles;
				_queryNumber = queryNumber;
				_skipAction  = skipAction;
				_takeAction  = takeAction;
			}

			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
			{
				return new AsyncEnumeratorImpl<T>(
					_query, _dataContext, _mapper, _expression, _ps, _preambles, _queryNumber, _skipAction, _takeAction, cancellationToken);
			}
		}

		static IAsyncEnumerable<T> ExecuteQueryAsync<T>(
			Query                            query,
			IDataContext                     dataContext,
			Mapper<T>                        mapper,
			Expression                       expression,
			object?[]?                       ps,
			object?[]?                       preambles,
			int                              queryNumber,
			Func<Expression,IDataContext?,object?[]?,int>? skipAction,
			Func<Expression,IDataContext?,object?[]?,int>? takeAction)
		{
			return new AsyncEnumerableImpl<T>(
				query, dataContext, mapper, expression, ps, preambles, queryNumber, skipAction, takeAction);
		}

		static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataReader,T>> expression)
		{
			var executeQuery = GetExecuteQuery<T>(query, ExecuteQuery);

			ClearParameters(query);

			var mapper   = new Mapper<T>(expression);
			var runQuery = executeQuery.Item1;

			query.GetIEnumerable = (db, expr, ps, preambles) => runQuery(query, db, mapper, expr, ps, preambles, 0);

			var skipAction = executeQuery.Item2;
			var takeAction = executeQuery.Item3;

			query.GetForEachAsync = (db, expr, ps, preambles, action, token) =>
				ExecuteQueryAsync(query, db, mapper, expr, ps, preambles, 0, action, skipAction, takeAction, token);

			query.GetIAsyncEnumerable = (db, expr, ps, preambles) =>
				ExecuteQueryAsync(query, db, mapper, expr, ps, preambles, 0, skipAction, takeAction);
		}

		static readonly PropertyInfo _dataContextInfo = MemberHelper.PropertyOf<IQueryRunner>( p => p.DataContext);
		static readonly PropertyInfo _expressionInfo  = MemberHelper.PropertyOf<IQueryRunner>( p => p.Expression);
		static readonly PropertyInfo _parametersInfo  = MemberHelper.PropertyOf<IQueryRunner>( p => p.Parameters);
		static readonly PropertyInfo _preamblesInfo   = MemberHelper.PropertyOf<IQueryRunner>( p => p.Preambles);
		static readonly PropertyInfo _rowsCountnfo    = MemberHelper.PropertyOf<IQueryRunner>( p => p.RowsCount);

		static Expression<Func<IQueryRunner,IDataReader,T>> WrapMapper<T>(
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object?[]?,object?[]?,T>> expression)
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
							Expression.Property(queryRunnerParam, _parametersInfo),
							Expression.Property(queryRunnerParam, _preamblesInfo),
						}),
					queryRunnerParam,
					dataReaderParam);
		}

		#endregion

		#region SetRunQuery / Cast, Concat, Union, OfType, ScalarSelect, Select, SequenceContext, Table

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object?[]?,object?[]?,T>> expression)
		{
			var l = WrapMapper(expression);

			SetRunQuery(query, l);
		}

		#endregion

		#region SetRunQuery / Select 2

		public static void SetRunQuery<T>(
			Query<T> query,
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object?[]?,object?[]?,int,T>> expression)
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
							Expression.Property(queryRunnerParam, _parametersInfo),
							Expression.Property(queryRunnerParam, _preamblesInfo),
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
			Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object?[]?,object?[]?,object>> expression)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters(query);

			var l      = WrapMapper(expression);
			var mapper = new Mapper<object>(l);

			query.GetElement      = (db, expr, ps, preambles) => ExecuteElement(query, db, mapper, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => ExecuteElementAsync<object?>(query, db, mapper, expr, ps, preambles, token);
		}

		static T ExecuteElement<T>(
			Query          query,
			IDataContext   dataContext,
			Mapper<T>      mapper,
			Expression     expression,
			object?[]?     ps,
			object?[]?     preambles)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps, preambles))
			{
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
		}

		static async Task<T> ExecuteElementAsync<T>(
			Query             query,
			IDataContext      dataContext,
			Mapper<object>    mapper,
			Expression        expression,
			object?[]?        ps,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps, preambles))
			{
				var dr = await runner.ExecuteReaderAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
#if !NET45 && !NET46
				await using (dr.ConfigureAwait(Configuration.ContinueOnCapturedContext))
#else
				using (dr)
#endif
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
		}

		#endregion

		#region ScalarQuery

		public static void SetScalarQuery(Query query)
		{
			FinalizeQuery(query);

			if (query.Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters(query);

			query.GetElement      = (db, expr, ps, preambles) => ScalarQuery(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => ScalarQueryAsync(query, db, expr, ps, preambles, token);
		}

		static object? ScalarQuery(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
				return runner.ExecuteScalar();
		}

		static async Task<object?> ScalarQueryAsync(
			Query             query,
			IDataContext      dataContext,
			Expression        expression,
			object?[]?        ps,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps, preambles))
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

			query.GetElement      = (db, expr, ps, preambles) => NonQueryQuery(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => NonQueryQueryAsync(query, db, expr, ps, preambles, token);
		}

		static int NonQueryQuery(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
				return runner.ExecuteNonQuery();
		}

		static async Task<object?> NonQueryQueryAsync(
			Query             query,
			IDataContext      dataContext,
			Expression        expression,
			object?[]?        ps,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expression, ps, preambles))
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

			query.GetElement      = (db, expr, ps, preambles)        => NonQueryQuery2(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => NonQueryQuery2Async(query, db, expr, ps, preambles, token);
		}

		static int NonQueryQuery2(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
			{
				var n = runner.ExecuteNonQuery();

				if (n != 0)
					return n;

				runner.QueryNumber = 1;

				return runner.ExecuteNonQuery();
			}
		}

		static async Task<object?> NonQueryQuery2Async(
			Query             query,
			IDataContext      dataContext,
			Expression        expr,
			object?[]?        parameters,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
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

			query.GetElement      = (db, expr, ps, preambles)        => QueryQuery2(query, db, expr, ps, preambles);
			query.GetElementAsync = (db, expr, ps, preambles, token) => QueryQuery2Async(query, db, expr, ps, preambles, token);
		}

		static int QueryQuery2(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
			{
				var n = runner.ExecuteScalar();

				if (n != null)
					return 0;

				runner.QueryNumber = 1;

				return runner.ExecuteNonQuery();
			}
		}

		static async Task<object?> QueryQuery2Async(
			Query             query,
			IDataContext      dataContext,
			Expression        expr,
			object?[]?        parameters,
			object?[]?        preambles,
			CancellationToken cancellationToken)
		{
			using (var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles))
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

		public static string GetSqlText(Query query, IDataContext dataContext, Expression expr, object?[]? parameters, object?[]? preambles)
		{
			var runner = dataContext.GetQueryRunner(query, 0, expr, parameters, preambles);
			return runner.GetSqlText();
		}

		#endregion
	}
}
