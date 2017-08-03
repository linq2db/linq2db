using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

#if !SL4
using System.Threading.Tasks;
#endif

// ReSharper disable StaticMemberInGenericType

namespace LinqToDB.Linq
{
	using Builder;
	using Data;
	using Common;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;
	using SqlProvider;

	abstract class Query
	{
		#region Init

		public readonly List<QueryInfo> Queries = new List<QueryInfo>(1);

		public abstract void Init(IBuildContext parseContext, List<ParameterAccessor> sqlParameters);

		protected Query(IDataContext dataContext, Expression expression)
		{
			ContextID        = dataContext.ContextID;
			Expression       = expression;
			MappingSchema    = dataContext.MappingSchema;
			ConfigurationID  = dataContext.MappingSchema.ConfigurationID;
			SqlOptimizer     = dataContext.GetSqlOptimizer();
			SqlProviderFlags = dataContext.SqlProviderFlags;
		}

		#endregion

		#region Compare

		public readonly string           ContextID;
		public readonly Expression       Expression;
		public readonly MappingSchema    MappingSchema;
		public readonly string           ConfigurationID;
		public readonly ISqlOptimizer    SqlOptimizer;
		public readonly SqlProviderFlags SqlProviderFlags;

		public bool Compare(string contextID, MappingSchema mappingSchema, Expression expr)
		{
			return
				ContextID.Length       == contextID.Length &&
				ContextID              == contextID        &&
				ConfigurationID.Length == mappingSchema.ConfigurationID.Length &&
				ConfigurationID        == mappingSchema.ConfigurationID &&
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

		#region Helpers

		ConcurrentDictionary<Type,Func<object,object>> _enumConverters;

		public object GetConvertedEnum(Type valueType, object value)
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
	}

	class Query<T> : Query
	{
		#region Init

		public Query(IDataContext dataContext, Expression expression)
			: base(dataContext, expression)
		{
			// IT : # check
			GetIEnumerable = MakeEnumerable;
		}

		public override void Init(IBuildContext parseContext, List<ParameterAccessor> sqlParameters)
		{
			Queries.Add(new QueryInfo
			{
				SelectQuery = parseContext.SelectQuery,
				Parameters  = sqlParameters,
			});
		}

		#endregion

		#region Properties & Fields

		public bool     DoNotChache;
		public Query<T> Next;

		public Func<QueryContext,IDataContextEx,Expression,object[],object>         GetElement;
		public Func<QueryContext,IDataContextEx,Expression,object[],IEnumerable<T>> GetIEnumerable;
#if !SL4
		public Func<QueryContext,IDataContextEx,Expression,object[],CancellationToken,TaskCreationOptions,Task<object>> GetElementAsync;
		public Func<QueryContext,IDataContextEx,Expression,object[],Func<T,bool>,CancellationToken,TaskCreationOptions,Task> GetForEachAsync;
#endif

		IEnumerable<T> MakeEnumerable(QueryContext qc, IDataContextEx dc, Expression expr, object[] ps)
		{
			yield return ConvertTo<T>.From(GetElement(qc, dc, expr, ps));
		}

		#endregion

		#region GetInfo

		static          Query<T> _first;
		static readonly object   _sync = new object();

		const int CacheSize = 100;

		public static Query<T> GetQuery(IDataContext dataContext, Expression expr)
		{
			var query = FindQuery(dataContext, expr);

			if (query == null)
			{
				lock (_sync)
				{
					query = FindQuery(dataContext, expr);

					if (query == null)
					{
						if (Configuration.Linq.GenerateExpressionTest)
						{
							var testFile = new ExpressionTestGenerator().GenerateSource(expr);
#if !SILVERLIGHT && !NETFX_CORE
							if (DataConnection.TraceSwitch.TraceInfo)
								DataConnection.WriteTraceLine(
									"Expression test code generated: '" + testFile + "'.", 
									DataConnection.TraceSwitch.DisplayName);
#endif
						}

						query = new Query<T>(dataContext, expr);

						try
						{
							query = new ExpressionBuilder(query, dataContext, expr, null).Build<T>();
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

		static Query<T> FindQuery(IDataContext dataContext, Expression expr)
		{
			Query<T> prev = null;
			var      n    = 0;

			for (var query = _first; query != null; query = query.Next)
			{
				if (query.Compare(dataContext.ContextID, dataContext.MappingSchema, expr))
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

		#region Object Operations

		static class ObjectOperation<T1>
		{
			public static readonly object Sync = new object();

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
			IDataContext dataContext, T obj,
			string tableName = null, string databaseName = null, string schemaName = null)
		{
			if (Equals(default(T), obj))
				return 0;

			Query<int> ei;

			var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID };

			if (!ObjectOperation<T>.Insert.TryGetValue(key, out ei))
				lock (ObjectOperation<T>.Sync)
					if (!ObjectOperation<T>.Insert.TryGetValue(key, out ei))
					{
						var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
						var sqlQuery = new SelectQuery { QueryType = QueryType.Insert };

						if (tableName    != null) sqlTable.PhysicalName = tableName;
						if (databaseName != null) sqlTable.Database     = databaseName;
						if (schemaName   != null) sqlTable.Owner        = schemaName;

						sqlQuery.Insert.Into = sqlTable;

						ei = new Query<int>(dataContext, null)
						{
							Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
						};

						foreach (var field in sqlTable.Fields)
						{
							if (field.Value.IsInsertable)
							{
								var param = GetParameter(dataContext, field.Value);

								ei.Queries[0].Parameters.Add(param);

								sqlQuery.Insert.Items.Add(new SelectQuery.SetExpression(field.Value, param.SqlParameter));
							}
							else if (field.Value.IsIdentity)
							{
								var sqlb = dataContext.CreateSqlProvider();
								var expr = sqlb.GetIdentityExpression(sqlTable);

								if (expr != null)
									sqlQuery.Insert.Items.Add(new SelectQuery.SetExpression(field.Value, expr));
							}
						}

						QueryRunner.SetNonQueryQuery(ei);

						ObjectOperation<T>.Insert.Add(key, ei);
					}

			return (int)ei.GetElement(null, (IDataContextEx)dataContext, Expression.Constant(obj), null);
		}

		#endregion

		#region InsertWithIdentity

		public static object InsertWithIdentity(IDataContext dataContext, T obj)
		{
			if (Equals(default(T), obj))
				return 0;

			Query<object> ei;

			var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID };

			if (!ObjectOperation<T>.InsertWithIdentity.TryGetValue(key, out ei))
				lock (ObjectOperation<T>.Sync)
					if (!ObjectOperation<T>.InsertWithIdentity.TryGetValue(key, out ei))
					{
						var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
						var sqlQuery = new SelectQuery { QueryType = QueryType.Insert };

						sqlQuery.Insert.Into         = sqlTable;
						sqlQuery.Insert.WithIdentity = true;

						ei = new Query<object>(dataContext, null)
						{
							Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
						};

						foreach (var field in sqlTable.Fields)
						{
							if (field.Value.IsInsertable)
							{
								var param = GetParameter(dataContext, field.Value);

								ei.Queries[0].Parameters.Add(param);

								sqlQuery.Insert.Items.Add(new SelectQuery.SetExpression(field.Value, param.SqlParameter));
							}
							else if (field.Value.IsIdentity)
							{
								var sqlb = dataContext.CreateSqlProvider();
								var expr = sqlb.GetIdentityExpression(sqlTable);

								if (expr != null)
									sqlQuery.Insert.Items.Add(new SelectQuery.SetExpression(field.Value, expr));
							}
						}

						QueryRunner.SetScalarQuery(ei);

						ObjectOperation<T>.InsertWithIdentity.Add(key, ei);
					}

			return ei.GetElement(null, (IDataContextEx)dataContext, Expression.Constant(obj), null);
		}

		#endregion

		#region InsertOrReplace

		public static int InsertOrReplace(IDataContext dataContext, T obj)
		{
			if (Equals(default(T), obj))
				return 0;

			Query<int> ei;

			var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID };

			if (!ObjectOperation<T>.InsertOrUpdate.TryGetValue(key, out ei))
				lock (ObjectOperation<T>.Sync)
					if (!ObjectOperation<T>.InsertOrUpdate.TryGetValue(key, out ei))
					{
						var fieldDic = new Dictionary<SqlField, ParameterAccessor>();
						var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
						var sqlQuery = new SelectQuery { QueryType = QueryType.InsertOrUpdate };

						ParameterAccessor param;

						sqlQuery.Insert.Into  = sqlTable;
						sqlQuery.Update.Table = sqlTable;

						sqlQuery.From.Table(sqlTable);

						ei = new Query<int>(dataContext, null)
						{
							Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
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
									param = GetParameter(dataContext, field);
									ei.Queries[0].Parameters.Add(param);

									if (supported)
										fieldDic.Add(field, param);
								}

								sqlQuery.Insert.Items.Add(new SelectQuery.SetExpression(field, param.SqlParameter));
							}
							else if (field.IsIdentity)
							{
								throw new LinqException("InsertOrReplace method does not support identity field '{0}.{1}'.", sqlTable.Name, field.Name);
							}
						}

						// Update.
						//
						var keys   = sqlTable.GetKeys(true).Cast<SqlField>().ToList();
						var fields = sqlTable.Fields.Values.Where(f => f.IsUpdatable).Except(keys).ToList();

						if (keys.Count == 0)
							throw new LinqException("InsertOrReplace method requires the '{0}' table to have a primary key.", sqlTable.Name);

						var q =
						(
							from k in keys
							join i in sqlQuery.Insert.Items on k equals i.Column
							select new { k, i }
						).ToList();

						var missedKey = keys.Except(q.Select(i => i.k)).FirstOrDefault();

						if (missedKey != null)
							throw new LinqException("InsertOrReplace method requires the '{0}.{1}' field to be included in the insert setter.",
								sqlTable.Name,
								missedKey.Name);

						if (fields.Count == 0)
							throw new LinqException("There are no fields to update in the type '{0}'.", sqlTable.Name);

						foreach (var field in fields)
						{
							if (!supported || !fieldDic.TryGetValue(field, out param))
							{
								param = GetParameter(dataContext, field);
								ei.Queries[0].Parameters.Add(param);

								if (supported)
									fieldDic.Add(field, param = GetParameter(dataContext, field));
							}

							sqlQuery.Update.Items.Add(new SelectQuery.SetExpression(field, param.SqlParameter));
						}

						sqlQuery.Update.Keys.AddRange(q.Select(i => i.i));

						// Set the query.
						//
						if (ei.SqlProviderFlags.IsInsertOrUpdateSupported)
							QueryRunner.SetNonQueryQuery(ei);
						else
							ei.MakeAlternativeInsertOrUpdate(sqlQuery);

						ObjectOperation<T>.InsertOrUpdate.Add(key, ei);
					}

			return (int)ei.GetElement(null, (IDataContextEx)dataContext, Expression.Constant(obj), null);
		}

		internal void MakeAlternativeInsertOrUpdate(SelectQuery selectQuery)
		{
			var dic = new Dictionary<ICloneableElement, ICloneableElement>();

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

			selectQuery.ClearInsert();

			if (selectQuery.Update.Items.Count > 0)
			{
				selectQuery.QueryType = QueryType.Update;
				QueryRunner.SetNonQueryQuery2(this);
			}
			else
			{
				selectQuery.QueryType = QueryType.Select;
				selectQuery.Select.Columns.Clear();
				selectQuery.Select.Columns.Add(new SelectQuery.Column(selectQuery, new SqlExpression("1")));
				QueryRunner.SetQueryQuery2(this);
			}

			Queries.Add(new QueryInfo
			{
				SelectQuery = insertQuery,
				Parameters  = Queries[0].Parameters.ToList(),
			});
		}

		#endregion

		#region Update

		public static int Update(IDataContext dataContext, T obj)
		{
			if (Equals(default(T), obj))
				return 0;

			Query<int> ei;

			var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID };

			if (!ObjectOperation<T>.Update.TryGetValue(key, out ei))
				lock (ObjectOperation<T>.Sync)
					if (!ObjectOperation<T>.Update.TryGetValue(key, out ei))
					{
						var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
						var sqlQuery = new SelectQuery { QueryType = QueryType.Update };

						sqlQuery.From.Table(sqlTable);

						ei = new Query<int>(dataContext, null)
						{
							Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
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
							var param = GetParameter(dataContext, field);

							ei.Queries[0].Parameters.Add(param);

							sqlQuery.Update.Items.Add(new SelectQuery.SetExpression(field, param.SqlParameter));
						}

						foreach (var field in keys)
						{
							var param = GetParameter(dataContext, field);

							ei.Queries[0].Parameters.Add(param);

							sqlQuery.Where.Field(field).Equal.Expr(param.SqlParameter);

							if (field.CanBeNull)
								sqlQuery.IsParameterDependent = true;
						}

						QueryRunner.SetNonQueryQuery(ei);

						ObjectOperation<T>.Update.Add(key, ei);
					}

			return (int)ei.GetElement(null, (IDataContextEx)dataContext, Expression.Constant(obj), null);
		}

		#endregion

		#region Delete

		public static int Delete(IDataContext dataContext, T obj)
		{
			if (Equals(default(T), obj))
				return 0;

			Query<int> ei;

			var key = new { dataContext.MappingSchema.ConfigurationID, dataContext.ContextID };

			if (!ObjectOperation<T>.Delete.TryGetValue(key, out ei))
				lock (_sync)
					if (!ObjectOperation<T>.Delete.TryGetValue(key, out ei))
					{
						var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
						var sqlQuery = new SelectQuery { QueryType = QueryType.Delete };

						sqlQuery.From.Table(sqlTable);

						ei = new Query<int>(dataContext, null)
						{
							Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
						};

						var keys = sqlTable.GetKeys(true).Cast<SqlField>().ToList();

						if (keys.Count == 0)
							throw new LinqException("Table '{0}' does not have primary key.".Args(sqlTable.Name));

						foreach (var field in keys)
						{
							var param = GetParameter(dataContext, field);

							ei.Queries[0].Parameters.Add(param);

							sqlQuery.Where.Field(field).Equal.Expr(param.SqlParameter);

							if (field.CanBeNull)
								sqlQuery.IsParameterDependent = true;
						}

						QueryRunner.SetNonQueryQuery(ei);

						ObjectOperation<T>.Delete.Add(key, ei);
					}

			return (int)ei.GetElement(null, (IDataContextEx)dataContext, Expression.Constant(obj), null);
		}

		#endregion

		#endregion

		#region DDL Operations

		public static ITable<T> CreateTable(IDataContext dataContext,
			string         tableName       = null,
			string         databaseName    = null,
			string         schemaName      = null,
			string         statementHeader = null,
			string         statementFooter = null,
			DefaulNullable defaulNullable  = DefaulNullable.None)
		{
			var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
			var sqlQuery = new SelectQuery { QueryType = QueryType.CreateTable };

			if (tableName    != null) sqlTable.PhysicalName = tableName;
			if (databaseName != null) sqlTable.Database     = databaseName;
			if (schemaName   != null) sqlTable.Owner        = schemaName;

			sqlQuery.CreateTable.Table           = sqlTable;
			sqlQuery.CreateTable.StatementHeader = statementHeader;
			sqlQuery.CreateTable.StatementFooter = statementFooter;
			sqlQuery.CreateTable.DefaulNullable  = defaulNullable;

			var query = new Query<int>(dataContext, null)
			{
				Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
			};

			QueryRunner.SetNonQueryQuery(query);

			query.GetElement(null, (IDataContextEx)dataContext, Expression.Constant(null), null);

			ITable<T> table = new Table<T>(dataContext);

			if (tableName    != null) table = table.TableName   (tableName);
			if (databaseName != null) table = table.DatabaseName(databaseName);
			if (schemaName   != null) table = table.SchemaName  (schemaName);

			return table;
		}

		public static void DropTable(IDataContext dataContext,
			string tableName    = null,
			string databaseName = null,
			string ownerName    = null)
		{
			var sqlTable = new SqlTable<T>(dataContext.MappingSchema);
			var sqlQuery = new SelectQuery { QueryType = QueryType.CreateTable };

			if (tableName    != null) sqlTable.PhysicalName = tableName;
			if (databaseName != null) sqlTable.Database     = databaseName;
			if (ownerName    != null) sqlTable.Owner        = ownerName;

			sqlQuery.CreateTable.Table  = sqlTable;
			sqlQuery.CreateTable.IsDrop = true;

			var query = new Query<int>(dataContext, null)
			{
				Queries = { new QueryInfo { SelectQuery = sqlQuery, } }
			};

			QueryRunner.SetNonQueryQuery(query);

			query.GetElement(null, (IDataContextEx)dataContext, Expression.Constant(null), null);
		}

		#endregion
	}

	class QueryInfo : IQueryContext
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

	class ParameterAccessor
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
