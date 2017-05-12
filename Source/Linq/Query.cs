using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	using Builder;
	using Data;
	using Common;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	abstract class Query
	{
		#region Init

		public abstract void Init(IBuildContext parseContext, List<ParameterAccessor> sqlParameters);

		#endregion

		#region Compare

		public string           ContextID;
		public Expression       Expression;
		public MappingSchema    MappingSchema;
		public SqlProviderFlags SqlProviderFlags;

		public bool Compare(string contextID, MappingSchema mappingSchema, Expression expr)
		{
			return
				ContextID.Length == contextID.Length &&
				ContextID        == contextID        &&
				MappingSchema    == mappingSchema    &&
				Expression.EqualsTo(expr, _queryableAccessorDic);
		}

		readonly Dictionary<Expression,QueryableAccessor> _queryableAccessorDic  = new Dictionary<Expression,QueryableAccessor>();
		readonly List<QueryableAccessor>                  _queryableAccessorList = new List<QueryableAccessor>();

		internal int AddQueryableAccessors(Expression expr, Expression<Func<Expression,IQueryable>> qe)
		{
			QueryableAccessor e;

			if (_queryableAccessorDic.TryGetValue(expr, out e))
				return _queryableAccessorList.IndexOf(e);

			e = new QueryableAccessor { Accessor = qe.Compile() };
			e.Queryable = e.Accessor(expr);

			_queryableAccessorDic. Add(expr, e);
			_queryableAccessorList.Add(e);

			return _queryableAccessorList.Count - 1;
		}

		public Expression GetIQueryable(int n, Expression expr)
		{
			return _queryableAccessorList[n].Accessor(expr).Expression;
		}

		#endregion
	}

	class Query<T> : Query
	{
		#region Init

		public Query()
		{
			GetIEnumerable = MakeEnumerable;
		}

		public override void Init(IBuildContext parseContext, List<ParameterAccessor> sqlParameters)
		{
			Queries.Add(new QueryInfo
			{
				SelectQuery = parseContext.SelectQuery,
				Parameters  = sqlParameters,
			});

			ContextID        = parseContext.Builder.DataContextInfo.ContextID;
			MappingSchema    = parseContext.Builder.MappingSchema;
			SqlProviderFlags = parseContext.Builder.DataContextInfo.SqlProviderFlags;
			SqlOptimizer     = parseContext.Builder.DataContextInfo.GetSqlOptimizer();
			Expression       = parseContext.Builder.OriginalExpression;
		}

		void ClearParameters()
		{
			foreach (var query in Queries)
				foreach (var sqlParameter in query.Parameters)
					sqlParameter.Expression = null;
		}

		#endregion

		#region Properties & Fields

		public          bool              DoNotChache;
		public          Query<T>          Next;
		public readonly List<QueryInfo>   Queries = new List<QueryInfo>(1);
		public          ISqlOptimizer     SqlOptimizer;

		public Func<QueryContext,IDataContextInfo,Expression,object[],object>         GetElement;
		public Func<QueryContext,IDataContextInfo,Expression,object[],IEnumerable<T>> GetIEnumerable;

		IEnumerable<T> MakeEnumerable(QueryContext qc, IDataContextInfo dci, Expression expr, object[] ps)
		{
			yield return ConvertTo<T>.From(GetElement(qc, dci, expr, ps));
		}

		#endregion

		#region GetInfo

		static          Query<T> _first;
		static readonly object   _sync = new object();

		const int CacheSize = 100;

		public static Query<T> GetQuery(IDataContextInfo dataContextInfo, Expression expr)
		{
			var query = FindQuery(dataContextInfo, expr);

			if (query == null)
			{
				lock (_sync)
				{
					query = FindQuery(dataContextInfo, expr);

					if (query == null)
					{
						if (Configuration.Linq.GenerateExpressionTest)
						{
							var testFile = new ExpressionTestGenerator().GenerateSource(expr);
#if !SILVERLIGHT && !NETFX_CORE
							DataConnection.WriteTraceLine(
								"Expression test code generated: '" + testFile + "'.", 
								DataConnection.TraceSwitch.DisplayName);
#endif
						}

						try
						{
							query = new ExpressionBuilder(new Query<T>(), dataContextInfo, expr, null).Build<T>();
						}
						catch (Exception)
						{
							if (!Configuration.Linq.GenerateExpressionTest)
							{
#if !SILVERLIGHT && !NETFX_CORE
								DataConnection.WriteTraceLine(
									"To generate test code to diagnose the problem set 'LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true'.",
									DataConnection.TraceSwitch.DisplayName);
#endif
							}

							throw;
						}

						if (!query.DoNotChache)
						{
							query.Next = _first;
							_first = query;
						}
					}
				}
			}

			return query;
		}

		static Query<T> FindQuery(IDataContextInfo dataContextInfo, Expression expr)
		{
			Query<T> prev = null;
			var      n    = 0;

			for (var query = _first; query != null; query = query.Next)
			{
				if (query.Compare(dataContextInfo.ContextID, dataContextInfo.MappingSchema, expr))
				{
					if (prev != null)
					{
						lock (_sync)
						{
							prev.Next  = query.Next;
							query.Next = _first;
							_first     = query;
						}
					}

					return query;
				}

				if (n++ >= CacheSize)
				{
					query.Next = null;
					return null;
				}

				prev = query;
			}

			return null;
		}

		#endregion

		#region NonQueryQuery

		void FinalizeQuery()
		{
			foreach (var sql in Queries)
			{
				sql.SelectQuery = SqlOptimizer.Finalize(sql.SelectQuery);
				sql.Parameters  = sql.Parameters
					.Select (p => new { p, idx = sql.SelectQuery.Parameters.IndexOf(p.SqlParameter) })
					.OrderBy(p => p.idx)
					.Select (p => p.p)
					.ToList();
			}
		}

		public void SetNonQueryQuery()
		{
			FinalizeQuery();

			if (Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters();

			GetElement = (ctx,db,expr,ps) => NonQueryQuery(db, expr, ps);
		}

		int NonQueryQuery(IDataContextInfo dataContextInfo, Expression expr, object[] parameters)
		{
			var dataContext = dataContextInfo.DataContext;

			object query = null;

			try
			{
				query = SetCommand(dataContext, expr, parameters, 0, true);

				var res = dataContext.ExecuteNonQuery(query);

				return res;
			}
			finally
			{
				if (query != null)
					dataContext.ReleaseQuery(query);

				if (dataContextInfo.DisposeContext)
					dataContext.Dispose();
			}
		}

		public void SetNonQueryQuery2()
		{
			FinalizeQuery();

			if (Queries.Count != 2)
				throw new InvalidOperationException();

			ClearParameters();

			GetElement = (ctx,db,expr,ps) => NonQueryQuery2(db, expr, ps);
		}

		int NonQueryQuery2(IDataContextInfo dataContextInfo, Expression expr, object[] parameters)
		{
			var dataContext = dataContextInfo.DataContext;

			object query = null;

			try
			{
				query = SetCommand(dataContext, expr, parameters, 0, true);

				var n = dataContext.ExecuteNonQuery(query);

				if (n != 0)
					return n;

				query = SetCommand(dataContext, expr, parameters, 1, true);
				return dataContext.ExecuteNonQuery(query);
			}
			finally
			{
				if (query != null)
					dataContext.ReleaseQuery(query);

				if (dataContextInfo.DisposeContext)
					dataContext.Dispose();
			}
		}

		#endregion

		#region ScalarQuery

		public void SetScalarQuery<TS>()
		{
			FinalizeQuery();

			if (Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters();

			GetElement = (ctx,db,expr,ps) => ScalarQuery<TS>(db, expr, ps);
		}

		TS ScalarQuery<TS>(IDataContextInfo dataContextInfo, Expression expr, object[] parameters)
		{
			var dataContext = dataContextInfo.DataContext;

			object query = null;

			try
			{
				query = SetCommand(dataContext, expr, parameters, 0, true);
				return (TS)dataContext.ExecuteScalar(query);
			}
			finally
			{
				if (query != null)
					dataContext.ReleaseQuery(query);

				if (dataContextInfo.DisposeContext)
					dataContext.Dispose();
			}
		}

		#endregion

		#region RunQuery

		int GetParameterIndex(ISqlExpression parameter)
		{
			for (var i = 0; i < Queries[0].Parameters.Count; i++)
			{
				var p = Queries[0].Parameters[i].SqlParameter;

				if (p == parameter)
					return i;
			}

			throw new InvalidOperationException();
		}

		IEnumerable<IDataReader> RunQuery(IDataContextInfo dataContextInfo, Expression expr, object[] parameters, int queryNumber)
		{
			var dataContext = dataContextInfo.DataContext;

			object query = null;

			try
			{
				query = SetCommand(dataContext, expr, parameters, queryNumber, true);

				using (var dr = dataContext.ExecuteReader(query))
					while (dr.Read())
						yield return dr;
			}
			finally
			{
				if (query != null)
					dataContext.ReleaseQuery(query);

				if (dataContextInfo.DisposeContext)
					dataContext.Dispose();
			}
		}

		object SetCommand(IDataContext dataContext, Expression expr, object[] parameters, int idx, bool clearQueryHints)
		{
			lock (this)
			{
				SetParameters(expr, parameters, idx);

				var query = Queries[idx];

				if (idx == 0 && (dataContext.QueryHints.Count > 0 || dataContext.NextQueryHints.Count > 0))
				{
					query.QueryHints = new List<string>(dataContext.QueryHints);
					query.QueryHints.AddRange(dataContext.NextQueryHints);

					if (clearQueryHints)
						dataContext.NextQueryHints.Clear();
				}

				return dataContext.SetQuery(query);
			}
		}

		ConcurrentDictionary<Type,Func<object,object>> _enumConverters;

		internal void SetParameters(Expression expr, object[] parameters, int idx)
		{
			foreach (var p in Queries[idx].Parameters)
			{
				var value = p.Accessor(expr, parameters);

				if (value is IEnumerable)
				{
					var type  = value.GetType();
					var etype = type.GetItemType();

					if (etype == null || etype == typeof(object) || etype.IsEnumEx() ||
						(type.IsGenericTypeEx() && type.GetGenericTypeDefinition() == typeof(Nullable<>) && etype.GetGenericArgumentsEx()[0].IsEnumEx()))
					{
						var values = new List<object>();

						foreach (var v in (IEnumerable)value)
						{
							value = v;

							if (v != null)
							{
								var valueType = v.GetType();

								if (valueType.ToNullableUnderlying().IsEnumEx())
									value = GetConvertedEnum(valueType, value);
							}

							values.Add(value);
						}

						value = values;
					}
				}

				p.SqlParameter.Value = value;

				var dataType = p.DataTypeAccessor(expr, parameters);
				if (dataType != DataType.Undefined)
					p.SqlParameter.DataType = dataType;
			}
		}

		private object GetConvertedEnum(Type valueType, object value)
		{
			if (_enumConverters == null)
				_enumConverters = new ConcurrentDictionary<Type, Func<object, object>>();

			Func<object, object> converter;

			if (!_enumConverters.TryGetValue(valueType, out converter))
			{
				var toType    = Converter.GetDefaultMappingFromEnumType(MappingSchema, valueType);
				var convExpr  = MappingSchema.GetConvertExpression(valueType, toType);
				var convParam = Expression.Parameter(typeof(object));

				var lex = Expression.Lambda<Func<object, object>>(
					Expression.Convert(convExpr.GetBody(Expression.Convert(convParam, valueType)), typeof(object)),
					convParam);

				converter = lex.Compile();

				_enumConverters.GetOrAdd(valueType, converter);
			}

			return converter(value);
		}

		#endregion

		#region GetSqlText

		public string GetSqlText(IDataContext dataContext, Expression expr, object[] parameters, int idx)
		{
			var query = SetCommand(dataContext, expr, parameters, 0, false);
			return dataContext.GetSqlText(query);
		}

		#endregion

		#region Inner Types

		internal delegate TElement Mapper<out TElement>(
			Query<T>      query,
			QueryContext  qc,
			IDataContext  dc,
			IDataReader   rd,
			MappingSchema ms,
			Expression    expr,
			object[]      ps);

		public class QueryInfo : IQueryContext
		{
			public QueryInfo()
			{
				SelectQuery = new SelectQuery();
			}

			public SelectQuery  SelectQuery { get; set; }
			public object       Context     { get; set; }
			public List<string> QueryHints  { get; set; }

			public SqlParameter[] GetParameters()
			{
				var ps = new SqlParameter[Parameters.Count];

				for (var i = 0; i < ps.Length; i++)
					ps[i] = Parameters[i].SqlParameter;

				return ps;
			}

			public List<ParameterAccessor> Parameters = new List<ParameterAccessor>();
		}

		#endregion

		#region Object Operations

		static class ObjectOperation<T1>
		{
			public static readonly Dictionary<object,Query<int>>    Insert             = new Dictionary<object,Query<int>>();
			public static readonly Dictionary<object,Query<object>> InsertWithIdentity = new Dictionary<object,Query<object>>();
			public static readonly Dictionary<object,Query<int>>    InsertOrUpdate     = new Dictionary<object,Query<int>>();
			public static readonly Dictionary<object,Query<int>>    Update             = new Dictionary<object,Query<int>>();
			public static readonly Dictionary<object,Query<int>>    Delete             = new Dictionary<object,Query<int>>();
		}

		static ParameterAccessor GetParameter(IDataContext dataContext, SqlField field)
		{
			var exprParam = Expression.Parameter(typeof(Expression), "expr");

			Expression getter = Expression.Convert(
				Expression.Property(
					Expression.Convert(exprParam, typeof(ConstantExpression)),
					ReflectionHelper.Constant.Value),
				typeof(T));

			var members  = field.Name.Split('.');
			var defValue = Expression.Constant(dataContext.MappingSchema.GetDefaultValue(field.SystemType), field.SystemType);

			for (var i = 0; i < members.Length; i++)
			{
				var        member = members[i];
				Expression pof    = Expression.PropertyOrField(getter, member);

				getter = i == 0 ? pof : Expression.Condition(Expression.Equal(getter, Expression.Constant(null)), defValue, pof);
			}

			Expression dataTypeExpression = Expression.Constant(DataType.Undefined);

			var expr = dataContext.MappingSchema.GetConvertExpression(field.SystemType, typeof(DataParameter), createDefault: false);

			if (expr != null)
			{
				var body = expr.GetBody(getter);

				getter             = Expression.PropertyOrField(body, "Value");
				dataTypeExpression = Expression.PropertyOrField(body, "DataType");
			}

			var param = ExpressionBuilder.CreateParameterAccessor(
				dataContext, getter, dataTypeExpression, getter, exprParam, Expression.Parameter(typeof(object[]), "ps"), field.Name.Replace('.', '_'));

			return param;
		}

		#region Insert

		public static int Insert(
			IDataContextInfo dataContextInfo, T obj,
			string tableName = null, string databaseName = null, string schemaName = null)
		{
			if (Equals(default(T), obj))
				return 0;

			Query<int> ei;

			var key = new { dataContextInfo.MappingSchema, dataContextInfo.ContextID };

			if (!ObjectOperation<T>.Insert.TryGetValue(key, out ei))
				lock (_sync)
					if (!ObjectOperation<T>.Insert.TryGetValue(key, out ei))
					{
						var sqlTable = new SqlTable<T>(dataContextInfo.MappingSchema);
						var sqlQuery = new SelectQuery { QueryType = QueryType.Insert };

						if (tableName    != null) sqlTable.PhysicalName = tableName;
						if (databaseName != null) sqlTable.Database     = databaseName;
						if (schemaName   != null) sqlTable.Owner        = schemaName;

						sqlQuery.Insert.Into = sqlTable;

						ei = new Query<int>
						{
							MappingSchema = dataContextInfo.MappingSchema,
							ContextID     = dataContextInfo.ContextID,
							SqlOptimizer  = dataContextInfo.GetSqlOptimizer(),
							Queries       = { new Query<int>.QueryInfo { SelectQuery = sqlQuery, } }
						};

						foreach (var field in sqlTable.Fields)
						{
							if (field.Value.IsInsertable)
							{
								var param = GetParameter(dataContextInfo.DataContext, field.Value);

								ei.Queries[0].Parameters.Add(param);

								sqlQuery.Insert.Items.Add(new SelectQuery.SetExpression(field.Value, param.SqlParameter));
							}
							else if (field.Value.IsIdentity)
							{
								var sqlb = dataContextInfo.CreateSqlBuilder();
								var expr = sqlb.GetIdentityExpression(sqlTable);

								if (expr != null)
									sqlQuery.Insert.Items.Add(new SelectQuery.SetExpression(field.Value, expr));
							}
						}

						ei.SetNonQueryQuery();

						ObjectOperation<T>.Insert.Add(key, ei);
					}

			return (int)ei.GetElement(null, dataContextInfo, Expression.Constant(obj), null);
		}

		#endregion

		#region InsertWithIdentity

		public static object InsertWithIdentity(IDataContextInfo dataContextInfo, T obj)
		{
			if (Equals(default(T), obj))
				return 0;

			Query<object> ei;

			var key = new { dataContextInfo.MappingSchema, dataContextInfo.ContextID };

			if (!ObjectOperation<T>.InsertWithIdentity.TryGetValue(key, out ei))
				lock (_sync)
					if (!ObjectOperation<T>.InsertWithIdentity.TryGetValue(key, out ei))
					{
						var sqlTable = new SqlTable<T>(dataContextInfo.MappingSchema);
						var sqlQuery = new SelectQuery { QueryType = QueryType.Insert };

						sqlQuery.Insert.Into         = sqlTable;
						sqlQuery.Insert.WithIdentity = true;

						ei = new Query<object>
						{
							MappingSchema = dataContextInfo.MappingSchema,
							ContextID     = dataContextInfo.ContextID,
							SqlOptimizer  = dataContextInfo.GetSqlOptimizer(),
							Queries       = { new Query<object>.QueryInfo { SelectQuery = sqlQuery, } }
						};

						foreach (var field in sqlTable.Fields)
						{
							if (field.Value.IsInsertable)
							{
								var param = GetParameter(dataContextInfo.DataContext, field.Value);

								ei.Queries[0].Parameters.Add(param);

								sqlQuery.Insert.Items.Add(new SelectQuery.SetExpression(field.Value, param.SqlParameter));
							}
							else if (field.Value.IsIdentity)
							{
								var sqlb = dataContextInfo.CreateSqlBuilder();
								var expr = sqlb.GetIdentityExpression(sqlTable);

								if (expr != null)
									sqlQuery.Insert.Items.Add(new SelectQuery.SetExpression(field.Value, expr));
							}
						}

						ei.SetScalarQuery<object>();

						ObjectOperation<T>.InsertWithIdentity.Add(key, ei);
					}

			return ei.GetElement(null, dataContextInfo, Expression.Constant(obj), null);
		}

		#endregion

		#region InsertOrReplace

		public static int InsertOrReplace(IDataContextInfo dataContextInfo, T obj)
		{
			if (Equals(default(T), obj))
				return 0;

			Query<int> ei;

			var key = new { dataContextInfo.MappingSchema, dataContextInfo.ContextID };

			if (!ObjectOperation<T>.InsertOrUpdate.TryGetValue(key, out ei))
			{
				lock (_sync)
				{
					if (!ObjectOperation<T>.InsertOrUpdate.TryGetValue(key, out ei))
					{
						var fieldDic = new Dictionary<SqlField, ParameterAccessor>();
						var sqlTable = new SqlTable<T>(dataContextInfo.MappingSchema);
						var sqlQuery = new SelectQuery { QueryType = QueryType.InsertOrUpdate };

						ParameterAccessor param;

						sqlQuery.Insert.Into  = sqlTable;
						sqlQuery.Update.Table = sqlTable;

						sqlQuery.From.Table(sqlTable);

						ei = new Query<int>
						{
							MappingSchema    = dataContextInfo.MappingSchema,
							ContextID        = dataContextInfo.ContextID,
							SqlOptimizer     = dataContextInfo.GetSqlOptimizer(),
							Queries          = { new Query<int>.QueryInfo { SelectQuery = sqlQuery, } },
							SqlProviderFlags = dataContextInfo.SqlProviderFlags,
						};

						var supported = ei.SqlProviderFlags.IsInsertOrUpdateSupported && ei.SqlProviderFlags.CanCombineParameters;

						// Insert.
						//
						foreach (var field in sqlTable.Fields.Select(f => f.Value))
						{
							if (field.IsInsertable)
							{
								if (!supported || !fieldDic.TryGetValue(field, out param))
								{
									param = GetParameter(dataContextInfo.DataContext, field);
									ei.Queries[0].Parameters.Add(param);

									if (supported)
										fieldDic.Add(field, param);
								}

								sqlQuery.Insert.Items.Add(new SelectQuery.SetExpression(field, param.SqlParameter));
							}
							else if (field.IsIdentity)
							{
								throw new LinqException("InsertOrUpdate method does not support identity field '{0}.{1}'.", sqlTable.Name, field.Name);
							}
						}

						// Update.
						//
						var keys   = sqlTable.GetKeys(true).Cast<SqlField>().ToList();
						var fields = sqlTable.Fields.Values.Where(f => f.IsUpdatable).Except(keys).ToList();

						if (keys.Count == 0)
							throw new LinqException("InsertOrUpdate method requires the '{0}' table to have a primary key.", sqlTable.Name);

						var q =
						(
							from k in keys
							join i in sqlQuery.Insert.Items on k equals i.Column
							select new { k, i }
						).ToList();

						var missedKey = keys.Except(q.Select(i => i.k)).FirstOrDefault();

						if (missedKey != null)
							throw new LinqException("InsertOrUpdate method requires the '{0}.{1}' field to be included in the insert setter.",
								sqlTable.Name,
								missedKey.Name);

						if (fields.Count == 0)
							throw new LinqException("There are no fields to update in the type '{0}'.", sqlTable.Name);

						foreach (var field in fields)
						{
							if (!supported || !fieldDic.TryGetValue(field, out param))
							{
								param = GetParameter(dataContextInfo.DataContext, field);
								ei.Queries[0].Parameters.Add(param);

								if (supported)
									fieldDic.Add(field, param = GetParameter(dataContextInfo.DataContext, field));
							}

							sqlQuery.Update.Items.Add(new SelectQuery.SetExpression(field, param.SqlParameter));
						}

						sqlQuery.Update.Keys.AddRange(q.Select(i => i.i));

						// Set the query.
						//
						if (ei.SqlProviderFlags.IsInsertOrUpdateSupported)
							ei.SetNonQueryQuery();
						else
							ei.MakeAlternativeInsertOrUpdate(sqlQuery);

						ObjectOperation<T>.InsertOrUpdate.Add(key, ei);
					}
				}
			}

			return (int)ei.GetElement(null, dataContextInfo, Expression.Constant(obj), null);
		}

		internal void MakeAlternativeInsertOrUpdate(SelectQuery selectQuery)
		{
			var dic = new Dictionary<ICloneableElement,ICloneableElement>();

			var insertQuery = (SelectQuery)selectQuery.Clone(dic, _ => true);

			insertQuery.QueryType = QueryType.Insert;
			insertQuery.ClearUpdate();
			insertQuery.From.Tables.Clear();

			Queries.Add(new QueryInfo
			{
				SelectQuery = insertQuery,
				Parameters  = Queries[0].Parameters
					.Select(p => new ParameterAccessor
						(
							p.Expression,
							p.Accessor,
							p.DataTypeAccessor,
							dic.ContainsKey(p.SqlParameter) ? (SqlParameter)dic[p.SqlParameter] : null
						))
					.Where(p => p.SqlParameter != null)
					.ToList(),
			});

			var keys = selectQuery.Update.Keys;

			foreach (var key in keys)
				selectQuery.Where.Expr(key.Column).Equal.Expr(key.Expression);

			selectQuery.QueryType = QueryType.Update;
			selectQuery.ClearInsert();

			SetNonQueryQuery2();

			Queries.Add(new QueryInfo
			{
				SelectQuery = insertQuery,
				Parameters  = Queries[0].Parameters.ToList(),
			});
		}

		#endregion

		#region Update

		public static int Update(IDataContextInfo dataContextInfo, T obj)
		{
			if (Equals(default(T), obj))
				return 0;

			Query<int> ei;

			var key = new { dataContextInfo.MappingSchema, dataContextInfo.ContextID };

			if (!ObjectOperation<T>.Update.TryGetValue(key, out ei))
				lock (_sync)
					if (!ObjectOperation<T>.Update.TryGetValue(key, out ei))
					{
						var sqlTable = new SqlTable<T>(dataContextInfo.MappingSchema);
						var sqlQuery = new SelectQuery { QueryType = QueryType.Update };

						sqlQuery.From.Table(sqlTable);

						ei = new Query<int>
						{
							MappingSchema = dataContextInfo.MappingSchema,
							ContextID     = dataContextInfo.ContextID,
							SqlOptimizer  = dataContextInfo.GetSqlOptimizer(),
							Queries       = { new Query<int>.QueryInfo { SelectQuery = sqlQuery, } }
						};

						var keys   = sqlTable.GetKeys(true).Cast<SqlField>().ToList();
						var fields = sqlTable.Fields.Values.Where(f => f.IsUpdatable).Except(keys).ToList();

						if (fields.Count == 0)
						{
							if (Configuration.Linq.IgnoreEmptyUpdate)
								return 0;

							throw new LinqException(
								(keys.Count == sqlTable.Fields.Count ?
									"There are no fields to update in the type '{0}'. No PK is defined or all fields are keys." :
									"There are no fields to update in the type '{0}'.")
								.Args(sqlTable.Name));
						}

						foreach (var field in fields)
						{
							var param = GetParameter(dataContextInfo.DataContext, field);

							ei.Queries[0].Parameters.Add(param);

							sqlQuery.Update.Items.Add(new SelectQuery.SetExpression(field, param.SqlParameter));
						}

						foreach (var field in keys)
						{
							var param = GetParameter(dataContextInfo.DataContext, field);

							ei.Queries[0].Parameters.Add(param);

							sqlQuery.Where.Field(field).Equal.Expr(param.SqlParameter);

							if (field.CanBeNull)
								sqlQuery.IsParameterDependent = true;
						}

						ei.SetNonQueryQuery();

						ObjectOperation<T>.Update.Add(key, ei);
					}

			return (int)ei.GetElement(null, dataContextInfo, Expression.Constant(obj), null);
		}

		#endregion

		#region Delete

		public static int Delete(IDataContextInfo dataContextInfo, T obj)
		{
			if (Equals(default(T), obj))
				return 0;

			Query<int> ei;

			var key = new { dataContextInfo.MappingSchema, dataContextInfo.ContextID };

			if (!ObjectOperation<T>.Delete.TryGetValue(key, out ei))
				lock (_sync)
					if (!ObjectOperation<T>.Delete.TryGetValue(key, out ei))
					{
						var sqlTable = new SqlTable<T>(dataContextInfo.MappingSchema);
						var sqlQuery = new SelectQuery { QueryType = QueryType.Delete };

						sqlQuery.From.Table(sqlTable);

						ei = new Query<int>
						{
							MappingSchema = dataContextInfo.MappingSchema,
							ContextID     = dataContextInfo.ContextID,
							SqlOptimizer  = dataContextInfo.GetSqlOptimizer(),
							Queries       = { new Query<int>.QueryInfo { SelectQuery = sqlQuery, } }
						};

						var keys = sqlTable.GetKeys(true).Cast<SqlField>().ToList();

						if (keys.Count == 0)
							throw new LinqException("Table '{0}' does not have primary key.".Args(sqlTable.Name));

						foreach (var field in keys)
						{
							var param = GetParameter(dataContextInfo.DataContext, field);

							ei.Queries[0].Parameters.Add(param);

							sqlQuery.Where.Field(field).Equal.Expr(param.SqlParameter);

							if (field.CanBeNull)
								sqlQuery.IsParameterDependent = true;
						}

						ei.SetNonQueryQuery();

						ObjectOperation<T>.Delete.Add(key, ei);
					}

			return (int)ei.GetElement(null, dataContextInfo, Expression.Constant(obj), null);
		}

		#endregion

		#endregion

		#region DDL Operations

		public static ITable<T> CreateTable(IDataContextInfo dataContextInfo,
			string         tableName       = null,
			string         databaseName    = null,
			string         schemaName      = null,
			string         statementHeader = null,
			string         statementFooter = null,
			DefaulNullable defaulNullable  = DefaulNullable.None)
		{
			var sqlTable = new SqlTable<T>(dataContextInfo.MappingSchema);
			var sqlQuery = new SelectQuery { QueryType = QueryType.CreateTable };

			if (tableName    != null) sqlTable.PhysicalName = tableName;
			if (databaseName != null) sqlTable.Database     = databaseName;
			if (schemaName   != null) sqlTable.Owner        = schemaName;

			sqlQuery.CreateTable.Table           = sqlTable;
			sqlQuery.CreateTable.StatementHeader = statementHeader;
			sqlQuery.CreateTable.StatementFooter = statementFooter;
			sqlQuery.CreateTable.DefaulNullable  = defaulNullable;

			var query = new Query<int>
			{
				MappingSchema = dataContextInfo.MappingSchema,
				ContextID     = dataContextInfo.ContextID,
				SqlOptimizer  = dataContextInfo.GetSqlOptimizer(),
				Queries       = { new Query<int>.QueryInfo { SelectQuery = sqlQuery, } }
			};

			query.SetNonQueryQuery();

			query.GetElement(null, dataContextInfo, Expression.Constant(null), null);

			ITable<T> table = new Table<T>(dataContextInfo);

			if (tableName    != null) table = table.TableName   (tableName);
			if (databaseName != null) table = table.DatabaseName(databaseName);
			if (schemaName    != null) table = table.SchemaName  (schemaName);

			return table;
		}

		public static void DropTable(IDataContextInfo dataContextInfo,
			string tableName    = null,
			string databaseName = null,
			string ownerName    = null)
		{
			var sqlTable = new SqlTable<T>(dataContextInfo.MappingSchema);
			var sqlQuery = new SelectQuery { QueryType = QueryType.CreateTable };

			if (tableName    != null) sqlTable.PhysicalName = tableName;
			if (databaseName != null) sqlTable.Database     = databaseName;
			if (ownerName    != null) sqlTable.Owner        = ownerName;

			sqlQuery.CreateTable.Table  = sqlTable;
			sqlQuery.CreateTable.IsDrop = true;

			var query = new Query<int>
			{
				MappingSchema = dataContextInfo.MappingSchema,
				ContextID     = dataContextInfo.ContextID,
				SqlOptimizer  = dataContextInfo.GetSqlOptimizer(),
				Queries       = { new Query<int>.QueryInfo { SelectQuery = sqlQuery, } }
			};

			query.SetNonQueryQuery();

			query.GetElement(null, dataContextInfo, Expression.Constant(null), null);
		}

		#endregion

		#region New Builder Support

		public void SetElementQuery(Func<QueryContext,IDataContext,IDataReader,Expression,object[],object> mapper)
		{
			FinalizeQuery();

			if (Queries.Count != 1)
				throw new InvalidOperationException();

			ClearParameters();

			GetElement = (ctx,db,expr,ps) => RunQuery(ctx, db, expr, ps, mapper);
		}

		TE RunQuery<TE>(
			QueryContext     ctx,
			IDataContextInfo dataContextInfo,
			Expression       expr,
			object[]         parameters,
			Func<QueryContext,IDataContext,IDataReader,Expression,object[],TE> mapper)
		{
			var dataContext = dataContextInfo.DataContext;

			object query = null;

			try
			{
				query = SetCommand(dataContext, expr, parameters, 0, true);

				using (var dr = dataContext.ExecuteReader(query))
					while (dr.Read())
						return mapper(ctx, dataContext, dr, expr, parameters);

				return Array<TE>.Empty.First();
			}
			finally
			{
				if (query != null)
					dataContext.ReleaseQuery(query);

				if (dataContextInfo.DisposeContext)
					dataContext.Dispose();
			}
		}

		Func<IDataContextInfo,Expression,object[],int,IEnumerable<IDataReader>> GetQuery()
		{
			FinalizeQuery();

			if (Queries.Count != 1)
				throw new InvalidOperationException();

			Func<IDataContextInfo,Expression,object[],int,IEnumerable<IDataReader>> query = RunQuery;

			var select = Queries[0].SelectQuery.Select;

			if (select.SkipValue != null && !SqlProviderFlags.GetIsSkipSupportedFlag(Queries[0].SelectQuery))
			{
				var q = query;

				if (select.SkipValue is SqlValue)
				{
					var n = (int)((IValueContainer)select.SkipValue).Value;

					if (n > 0)
						query = (db, expr, ps, qn) => q(db, expr, ps, qn).Skip(n);
				}
				else if (select.SkipValue is SqlParameter)
				{
					var i = GetParameterIndex(select.SkipValue);
					query = (db, expr, ps, qn) => q(db, expr, ps, qn).Skip((int)Queries[0].Parameters[i].Accessor(expr, ps));
				}
			}

			if (select.TakeValue != null && !SqlProviderFlags.IsTakeSupported)
			{
				var q = query;

				if (select.TakeValue is SqlValue)
				{
					var n = (int)((IValueContainer)select.TakeValue).Value;

					if (n > 0)
						query = (db, expr, ps, qn) => q(db, expr, ps, qn).Take(n);
				}
				else if (select.TakeValue is SqlParameter)
				{
					var i = GetParameterIndex(select.TakeValue);
					query = (db, expr, ps, qn) => q(db, expr, ps, qn).Take((int)Queries[0].Parameters[i].Accessor(expr, ps));
				}
			}

			return query;
		}

		internal void SetQuery(Expression<Func<QueryContext,IDataContext,IDataReader,Expression,object[],T>> expression)
		{
			var query   = GetQuery();
			var mapInfo = new MapInfo { Expression = expression };

			ClearParameters();

			GetIEnumerable = (ctx,db,expr,ps) => Map(query(db, expr, ps, 0), ctx, db, expr, ps, mapInfo);
		}

		class MapInfo
		{
			public Expression<Func<QueryContext,IDataContext,IDataReader,Expression,object[],T>> Expression;
			public            Func<QueryContext,IDataContext,IDataReader,Expression,object[],T>  Mapper;
			public Expression<Func<QueryContext,IDataContext,IDataReader,Expression,object[],T>> MapperExpression;
		}

		static IEnumerable<T> Map(
			IEnumerable<IDataReader> data,
			QueryContext             queryContext,
			IDataContextInfo         dataContextInfo,
			Expression               expr,
			object[]                 ps,
			MapInfo                  mapInfo)
		{
			var closeQueryContext = false;

			if (queryContext == null)
			{
				closeQueryContext = true;
				queryContext = new QueryContext(dataContextInfo, expr, ps);
			}

			var isFaulted = false;

			try
			{
				foreach (var dr in data)
				{
					var mapper = mapInfo.Mapper;

					if (mapper == null)
					{
						mapInfo.MapperExpression = mapInfo.Expression.Transform(e =>
						{
							var ex = e as ConvertFromDataReaderExpression;
							return ex != null ? ex.Reduce(dr) : e;
						}) as Expression<Func<QueryContext, IDataContext, IDataReader, Expression, object[], T>>;

						// IT : # MapperExpression.Compile()
						//
						mapInfo.Mapper = mapper = mapInfo.MapperExpression.Compile();
					}

					T result;

					try
					{
						result = mapper(queryContext, dataContextInfo.DataContext, dr, expr, ps);
					}
					catch (FormatException)
					{
						if (isFaulted)
							throw;

						isFaulted = true;

						mapInfo.Mapper = mapInfo.Expression.Compile();
						result = mapInfo.Mapper(queryContext, dataContextInfo.DataContext, dr, expr, ps);
					}
					catch (InvalidCastException)
					{
						if (isFaulted)
							throw;

						isFaulted = true;

						mapInfo.Mapper = mapInfo.Expression.Compile();
						result = mapInfo.Mapper(queryContext, dataContextInfo.DataContext, dr, expr, ps);
					}

					yield return result;
				}
			}
			finally
			{
				if (closeQueryContext)
					queryContext.Close();
			}
		}

		internal void SetQuery(Expression<Func<QueryContext,IDataContext,IDataReader,Expression,object[],int,T>> expression)
		{
			var query   = GetQuery();
			var mapInfo = new MapInfo2 { Expression = expression };

			ClearParameters();

			GetIEnumerable = (ctx,db,expr,ps) => Map(query(db, expr, ps, 0), ctx, db, expr, ps, mapInfo);
		}

		class MapInfo2
		{
			public Expression<Func<QueryContext,IDataContext,IDataReader,Expression,object[],int,T>> Expression;
			public            Func<QueryContext,IDataContext,IDataReader,Expression,object[],int,T>  Mapper;
		}

		static IEnumerable<T> Map(
			IEnumerable<IDataReader> data,
			QueryContext             queryContext,
			IDataContextInfo         dataContextInfo,
			Expression               expr,
			object[]                 ps,
			MapInfo2                 mapInfo)
		{
			if (queryContext == null)
				queryContext = new QueryContext(dataContextInfo, expr, ps);

			var counter   = 0;
			var isFaulted = false;

			foreach (var dr in data)
			{
				var mapper = mapInfo.Mapper;

				if (mapper == null)
				{
					var mapperExpression = mapInfo.Expression.Transform(e =>
					{
						var ex = e as ConvertFromDataReaderExpression;
						return ex != null ? ex.Reduce(dr) : e;
					}) as Expression<Func<QueryContext,IDataContext,IDataReader,Expression,object[],int,T>>;

					mapInfo.Mapper = mapper = mapperExpression.Compile();
				}

				T result;
				
				try
				{
					result = mapper(queryContext, dataContextInfo.DataContext, dr, expr, ps, counter);
				}
				catch (FormatException)
				{
					if (isFaulted)
						throw;

					isFaulted = true;

					mapInfo.Mapper = mapInfo.Expression.Compile();
					result         = mapInfo.Mapper(queryContext, dataContextInfo.DataContext, dr, expr, ps, counter);
				}
				catch (InvalidCastException)
				{
					if (isFaulted)
						throw;

					isFaulted = true;

					mapInfo.Mapper = mapInfo.Expression.Compile();
					result         = mapInfo.Mapper(queryContext, dataContextInfo.DataContext, dr, expr, ps, counter);
				}

				counter++;

				yield return result;
			}
		}

		#endregion
	}

	public class ParameterAccessor
	{
		public ParameterAccessor(
			Expression                           expression,
			Func<Expression, object[], object>   accessor,
			Func<Expression, object[], DataType> dataTypeAccessor,
			SqlParameter                         sqlParameter
			)
		{
			Expression       = expression;
			Accessor         = accessor;
			DataTypeAccessor = dataTypeAccessor;
			SqlParameter     = sqlParameter;
		}

		public          Expression                         Expression;
		public readonly Func<Expression,object[],object>   Accessor;
		public readonly Func<Expression,object[],DataType> DataTypeAccessor;
		public readonly SqlParameter                       SqlParameter;
	}
}
