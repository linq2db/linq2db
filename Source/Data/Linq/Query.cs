using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Sql;
using LinqToDB.SqlProvider;

namespace LinqToDB.Data.Linq
{
	using Common;
	using LinqToDB.Extensions;
	using Mapping;
	using Builder;

	public abstract class Query
	{
		#region Init

		public abstract void Init(IBuildContext parseContext, List<ParameterAccessor> sqlParameters);

		#endregion

		#region Compare

		public string        ContextID;
		public Expression    Expression;
		public MappingSchema MappingSchema;

		public bool Compare(string contextID, MappingSchema mappingSchema, Expression expr)
		{
			return
				ContextID.Length == contextID.Length &&
				ContextID        == contextID        &&
				MappingSchema    == mappingSchema    &&
				Expression.EqualsTo(expr, _queryableAccessorDic);
		}

		readonly Dictionary<Expression,Func<Expression,IQueryable>> _queryableAccessorDic  = new Dictionary<Expression,Func<Expression,IQueryable>>();
		readonly List<Func<Expression,IQueryable>>                  _queryableAccessorList = new List<Func<Expression,IQueryable>>();

		public int AddQueryableAccessors(Expression expr, Expression<Func<Expression,IQueryable>> qe)
		{
			Func<Expression,IQueryable> e;

			if (_queryableAccessorDic.TryGetValue(expr, out e))
				return _queryableAccessorList.IndexOf(e);

			e = qe.Compile();

			_queryableAccessorDic. Add(expr, e);
			_queryableAccessorList.Add(e);

			return _queryableAccessorList.Count - 1;
		}

		public Expression GetIQueryable(int n, Expression expr)
		{
			return _queryableAccessorList[n](expr).Expression;
		}

		#endregion
	}

	public class Query<T> : Query
	{
		public Query()
		{
			GetIEnumerable = MakeEnumerable;
		}

		public override void Init(IBuildContext parseContext, List<ParameterAccessor> sqlParameters)
		{
			Queries.Add(new QueryInfo
			{
				SqlQuery   = parseContext.SqlQuery,
				Parameters = sqlParameters,
			});

			ContextID         = parseContext.Builder.DataContextInfo.ContextID;
			MappingSchema     = parseContext.Builder.MappingSchema;
			CreateSqlProvider = parseContext.Builder.DataContextInfo.CreateSqlProvider;
			Expression        = parseContext.Builder.OriginalExpression;
			//Parameters        = parameters;
		}

		#region Properties & Fields

		public Query<T>              Next;
		public ParameterExpression[] CompiledParameters;
		public List<QueryInfo>       Queries = new List<QueryInfo>(1);
		public Func<ISqlProvider>    CreateSqlProvider;

		private ISqlProvider _sqlProvider; 
		public  ISqlProvider  SqlProvider
		{
			get { return _sqlProvider ?? (_sqlProvider = CreateSqlProvider()); }
		}

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
						query = new ExpressionBuilder(new Query<T>(), dataContextInfo, expr, null).Build<T>();

						query.Next = _first;
						_first = query;
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
				sql.SqlQuery   = SqlProvider.Finalize(sql.SqlQuery);
				sql.Parameters = sql.Parameters
					.Select (p => new { p, idx = sql.SqlQuery.Parameters.IndexOf(p.SqlParameter) })
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

			SqlProvider.SqlQuery = Queries[0].SqlQuery;

			GetElement = (ctx,db,expr,ps) => NonQueryQuery(db, expr, ps);
		}

		int NonQueryQuery(IDataContextInfo dataContextInfo, Expression expr, object[] parameters)
		{
			var dataContext = dataContextInfo.DataContext;

			object query = null;

			try
			{
				query = SetCommand(dataContext, expr, parameters, 0);
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

		public void SetNonQueryQuery2()
		{
			FinalizeQuery();

			if (Queries.Count != 2)
				throw new InvalidOperationException();

			SqlProvider.SqlQuery = Queries[0].SqlQuery;

			GetElement = (ctx,db,expr,ps) => NonQueryQuery2(db, expr, ps);
		}

		int NonQueryQuery2(IDataContextInfo dataContextInfo, Expression expr, object[] parameters)
		{
			var dataContext = dataContextInfo.DataContext;

			object query = null;

			try
			{
				query = SetCommand(dataContext, expr, parameters, 0);

				var n = dataContext.ExecuteNonQuery(query);

				if (n != 0)
					return n;

				query = SetCommand(dataContext, expr, parameters, 1);
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

			SqlProvider.SqlQuery = Queries[0].SqlQuery;

			GetElement = (ctx,db,expr,ps) => ScalarQuery<TS>(db, expr, ps);
		}

		TS ScalarQuery<TS>(IDataContextInfo dataContextInfo, Expression expr, object[] parameters)
		{
			var dataContext = dataContextInfo.DataContext;

			object query = null;

			try
			{
				query = SetCommand(dataContext, expr, parameters, 0);
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

		#region Query

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
				query = SetCommand(dataContext, expr, parameters, queryNumber);

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

		object SetCommand(IDataContext dataContext, Expression expr, object[] parameters, int idx)
		{
			lock (this)
			{
				SetParameters(expr, parameters, idx);
				return dataContext.SetQuery(Queries[idx]);
			}
		}

		void SetParameters(Expression expr, object[] parameters, int idx)
		{
			foreach (var p in Queries[idx].Parameters)
				p.SqlParameter.Value = p.Accessor(expr, parameters);
		}

		#endregion

		#region GetSqlText

		public string GetSqlText(IDataContext dataContext, Expression expr, object[] parameters, int idx)
		{
			var query = SetCommand(dataContext, expr, parameters, 0);
			return dataContext.GetSqlText(query);
		}

		#endregion

		#region Inner Types

		internal delegate TElement Mapper<TElement>(
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
				SqlQuery = new SqlQuery();
			}

			public SqlQuery SqlQuery { get; set; }
			public object   Context  { get; set; }

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

		static object ConvertNullable<TT>(TT value, TT defaultValue)
			where TT : struct
		{
			return value.Equals(defaultValue) ? null : (object)value;
		}

		static ParameterAccessor GetParameter<TR>(IDataContext dataContext, SqlField field)
		{
			var exprParam = Expression.Parameter(typeof(Expression), "expr");

			Expression getter = Expression.Convert(
				Expression.Property(
					Expression.Convert(exprParam, typeof(ConstantExpression)),
					ReflectionHelper.Constant.Value),
				typeof(T));

			var mm       = field.MemberMapper;
			var members  = mm.MemberName.Split('.');
			var defValue = Expression.Constant(mm.MapMemberInfo.Type.GetDefaultValue(), mm.MapMemberInfo.Type);

			for (var i = 0; i < members.Length; i++)
			{
				var member = members[i];
				var pof    = Expression.PropertyOrField(getter, member) as Expression;

				getter = i == 0 ? pof : Expression.Condition(Expression.Equal(getter, Expression.Constant(null)), defValue, pof);
			}

			if (!mm.Type.IsClass && mm.MapMemberInfo.Nullable && !mm.Type.IsNullable())
			{
				var method = ReflectionHelper.Expressor<int>.MethodExpressor(_ => ConvertNullable(0, 0))
					.GetGenericMethodDefinition()
					.MakeGenericMethod(mm.Type);

				getter = Expression.Call(null, method, getter, Expression.Constant(mm.MapMemberInfo.NullValue));
			}
			else
				getter = Expression.Convert(getter, typeof(object));

			var mapper    = Expression.Lambda<Func<Expression,object[],object>>(
				getter,
				new [] { exprParam, Expression.Parameter(typeof(object[]), "ps") });

			var param = new ParameterAccessor
			{
				Expression   = null,
				Accessor     = mapper.Compile(),
				SqlParameter = new SqlParameter(field.SystemType, field.Name.Replace('.', '_'), null, dataContext.MappingSchema)
			};

			if (field.SystemType.IsEnum)
				param.SqlParameter.SetEnumConverter(field.SystemType, dataContext.MappingSchema);

			return param;
		}

		#region Insert

		public static int Insert(IDataContextInfo dataContextInfo, T obj)
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
						var sqlQuery = new SqlQuery { QueryType = QueryType.Insert };

						sqlQuery.Insert.Into = sqlTable;

						ei = new Query<int>
						{
							MappingSchema     = dataContextInfo.MappingSchema,
							ContextID         = dataContextInfo.ContextID,
							CreateSqlProvider = dataContextInfo.CreateSqlProvider,
							Queries           = { new Query<int>.QueryInfo { SqlQuery = sqlQuery, } }
						};

						foreach (var field in sqlTable.Fields)
						{
							if (field.Value.IsInsertable)
							{
								var param = GetParameter<int>(dataContextInfo.DataContext, field.Value);

								ei.Queries[0].Parameters.Add(param);

								sqlQuery.Insert.Items.Add(new SqlQuery.SetExpression(field.Value, param.SqlParameter));
							}
							else if (field.Value.IsIdentity)
							{
								var expr = ei.SqlProvider.GetIdentityExpression(sqlTable, field.Value, false);

								if (expr != null)
									sqlQuery.Insert.Items.Add(new SqlQuery.SetExpression(field.Value, expr));
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
						var sqlQuery = new SqlQuery { QueryType = QueryType.Insert };

						sqlQuery.Insert.Into         = sqlTable;
						sqlQuery.Insert.WithIdentity = true;

						ei = new Query<object>
						{
							MappingSchema     = dataContextInfo.MappingSchema,
							ContextID         = dataContextInfo.ContextID,
							CreateSqlProvider = dataContextInfo.CreateSqlProvider,
							Queries           = { new Query<object>.QueryInfo { SqlQuery = sqlQuery, } }
						};

						foreach (var field in sqlTable.Fields)
						{
							if (field.Value.IsInsertable)
							{
								var param = GetParameter<object>(dataContextInfo.DataContext, field.Value);

								ei.Queries[0].Parameters.Add(param);

								sqlQuery.Insert.Items.Add(new SqlQuery.SetExpression(field.Value, param.SqlParameter));
							}
							else if (field.Value.IsIdentity)
							{
								var expr = ei.SqlProvider.GetIdentityExpression(sqlTable, field.Value, true);

								if (expr != null)
									sqlQuery.Insert.Items.Add(new SqlQuery.SetExpression(field.Value, expr));
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
						var sqlQuery = new SqlQuery { QueryType = QueryType.InsertOrUpdate };

						ParameterAccessor param;

						sqlQuery.Insert.Into  = sqlTable;
						sqlQuery.Update.Table = sqlTable;

						sqlQuery.From.Table(sqlTable);

						ei = new Query<int>
						{
							MappingSchema     = dataContextInfo.MappingSchema,
							ContextID         = dataContextInfo.ContextID,
							CreateSqlProvider = dataContextInfo.CreateSqlProvider,
							Queries           = { new Query<int>.QueryInfo { SqlQuery = sqlQuery, } }
						};

						var supported = ei.SqlProvider.IsInsertOrUpdateSupported && ei.SqlProvider.CanCombineParameters;

						// Insert.
						//
						foreach (var field in sqlTable.Fields.Select(f => f.Value))
						{
							if (field.IsInsertable)
							{
								if (!supported || !fieldDic.TryGetValue(field, out param))
								{
									param = GetParameter<int>(dataContextInfo.DataContext, field);
									ei.Queries[0].Parameters.Add(param);

									if (supported)
										fieldDic.Add(field, param);
								}

								sqlQuery.Insert.Items.Add(new SqlQuery.SetExpression(field, param.SqlParameter));
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
							throw new LinqException(
								string.Format("There are no fields to update in the type '{0}'.", sqlTable.Name));

						foreach (var field in fields)
						{
							if (!supported || !fieldDic.TryGetValue(field, out param))
							{
								param = GetParameter<int>(dataContextInfo.DataContext, field);
								ei.Queries[0].Parameters.Add(param);

								if (supported)
									fieldDic.Add(field, param = GetParameter<int>(dataContextInfo.DataContext, field));
							}

							sqlQuery.Update.Items.Add(new SqlQuery.SetExpression(field, param.SqlParameter));
						}

						sqlQuery.Update.Keys.AddRange(q.Select(i => i.i));

						// Set the query.
						//
						if (ei.SqlProvider.IsInsertOrUpdateSupported)
							ei.SetNonQueryQuery();
						else
							ei.MakeAlternativeInsertOrUpdate(sqlQuery);

						ObjectOperation<T>.InsertOrUpdate.Add(key, ei);
					}
				}
			}

			return (int)ei.GetElement(null, dataContextInfo, Expression.Constant(obj), null);
		}

		internal void MakeAlternativeInsertOrUpdate(SqlQuery sqlQuery)
		{
			var dic = new Dictionary<ICloneableElement,ICloneableElement>();

			var insertQuery = (SqlQuery)sqlQuery.Clone(dic, _ => true);

			insertQuery.QueryType = QueryType.Insert;
			insertQuery.ClearUpdate();
			insertQuery.From.Tables.Clear();

			Queries.Add(new QueryInfo
			{
				SqlQuery   = insertQuery,
				Parameters = Queries[0].Parameters
					.Select(p => new ParameterAccessor
						{
							Expression   = p.Expression,
							Accessor     = p.Accessor,
							SqlParameter = dic.ContainsKey(p.SqlParameter) ? (SqlParameter)dic[p.SqlParameter] : null
						})
					.Where(p => p.SqlParameter != null)
					.ToList(),
			});

			var keys = sqlQuery.Update.Keys;

			foreach (var key in keys)
				sqlQuery.Where.Expr(key.Column).Equal.Expr(key.Expression);

			sqlQuery.QueryType = QueryType.Update;
			sqlQuery.ClearInsert();

			SetNonQueryQuery2();

			Queries.Add(new QueryInfo
			{
				SqlQuery   = insertQuery,
				Parameters = Queries[0].Parameters.ToList(),
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
						var sqlQuery = new SqlQuery { QueryType = QueryType.Update };

						sqlQuery.From.Table(sqlTable);

						ei = new Query<int>
						{
							MappingSchema     = dataContextInfo.MappingSchema,
							ContextID         = dataContextInfo.ContextID,
							CreateSqlProvider = dataContextInfo.CreateSqlProvider,
							Queries           = { new Query<int>.QueryInfo { SqlQuery = sqlQuery, } }
						};

						var keys   = sqlTable.GetKeys(true).Cast<SqlField>();
						var fields = sqlTable.Fields.Values.Where(f => f.IsUpdatable).Except(keys).ToList();

						if (fields.Count == 0)
						{
							if (Configuration.Linq.IgnoreEmptyUpdate)
								return 0;

							throw new LinqException(
								string.Format("There are no fields to update in the type '{0}'.", sqlTable.Name));
						}

						foreach (var field in fields)
						{
							var param = GetParameter<int>(dataContextInfo.DataContext, field);

							ei.Queries[0].Parameters.Add(param);

							sqlQuery.Update.Items.Add(new SqlQuery.SetExpression(field, param.SqlParameter));
						}

						foreach (var field in keys)
						{
							var param = GetParameter<int>(dataContextInfo.DataContext, field);

							ei.Queries[0].Parameters.Add(param);

							sqlQuery.Where.Field(field).Equal.Expr(param.SqlParameter);
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
						var sqlQuery = new SqlQuery { QueryType = QueryType.Delete };

						sqlQuery.From.Table(sqlTable);

						ei = new Query<int>
						{
							MappingSchema     = dataContextInfo.MappingSchema,
							ContextID         = dataContextInfo.ContextID,
							CreateSqlProvider = dataContextInfo.CreateSqlProvider,
							Queries           = { new Query<int>.QueryInfo { SqlQuery = sqlQuery, } }
						};

						var keys = sqlTable.GetKeys(true).Cast<SqlField>().ToList();

						if (keys.Count == 0)
							throw new LinqException(
								string.Format("Table '{0}' does not have primary key.", sqlTable.Name));

						foreach (var field in keys)
						{
							var param = GetParameter<int>(dataContextInfo.DataContext, field);

							ei.Queries[0].Parameters.Add(param);

							sqlQuery.Where.Field(field).Equal.Expr(param.SqlParameter);
						}

						ei.SetNonQueryQuery();

						ObjectOperation<T>.Delete.Add(key, ei);
					}

			return (int)ei.GetElement(null, dataContextInfo, Expression.Constant(obj), null);
		}

		#endregion

		#endregion

		#region New Builder Support

		public void SetElementQuery(Func<QueryContext,IDataContext,IDataReader,Expression,object[],object> mapper)
		{
			FinalizeQuery();

			if (Queries.Count != 1)
				throw new InvalidOperationException();

			SqlProvider.SqlQuery = Queries[0].SqlQuery;

			GetElement = (ctx,db,expr,ps) => RunQuery(ctx, db,expr, ps, mapper);
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
				query = SetCommand(dataContext, expr, parameters, 0);

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

		internal void SetQuery(Func<QueryContext,IDataContext,IDataReader,Expression,object[],T> mapper)
		{
			FinalizeQuery();

			if (Queries.Count != 1)
				throw new InvalidOperationException();

			Func<IDataContextInfo,Expression,object[],int,IEnumerable<IDataReader>> query = RunQuery;

			SqlProvider.SqlQuery = Queries[0].SqlQuery;

			var select = Queries[0].SqlQuery.Select;

			if (select.SkipValue != null && !SqlProvider.IsSkipSupported)
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

			if (select.TakeValue != null && !SqlProvider.IsTakeSupported)
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

			GetIEnumerable = (ctx,db,expr,ps) => Map(query(db, expr, ps, 0), ctx, db, expr, ps, mapper);
		}

		static IEnumerable<T> Map(
			IEnumerable<IDataReader> data,
			QueryContext             queryContext,
			IDataContextInfo         dataContextInfo,
			Expression               expr,
			object[]                 ps,
			Func<QueryContext,IDataContext,IDataReader,Expression,object[],T> mapper)
		{
			if (queryContext == null)
				queryContext = new QueryContext(dataContextInfo, expr, ps);

			foreach (var dr in data)
				yield return mapper(queryContext, dataContextInfo.DataContext, dr, expr, ps);
		}

		#endregion
	}

	public class ParameterAccessor
	{
		public Expression                       Expression;
		public Func<Expression,object[],object> Accessor;
		public SqlParameter                     SqlParameter;
	}
}
