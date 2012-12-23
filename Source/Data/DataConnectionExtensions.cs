using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.Data
{
	public static class DataConnectionExtensions
	{
		#region Query with object reader

		public static IEnumerable<T> Query<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql)
		{
			connection.Command.CommandText = sql;

			using (var rd = connection.Command.ExecuteReader())
				while (rd.Read())
					yield return objectReader(rd);
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, params DataParameter[] parameters)
		{
			connection.Command.CommandText = sql;

			SetParameters(connection, parameters);

			using (var rd = connection.Command.ExecuteReader())
				while (rd.Read())
					yield return objectReader(rd);
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, object parameters)
		{
			connection.Command.CommandText = sql;

			var dps = GetDataParameters(connection.MappingSchema, parameters);

			SetParameters(connection, dps);

			using (var rd = connection.Command.ExecuteReader())
				while (rd.Read())
					yield return objectReader(rd);
		}

		#endregion

		#region Query

		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql)
		{
			connection.Command.CommandText = sql;

			using (var rd = connection.Command.ExecuteReader())
			{
				if (rd.Read())
				{
					var objectReader = GetObjectReader<T>(connection, rd);
					do
						yield return objectReader(rd);
					while (rd.Read());
				}
			}
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			connection.Command.CommandText = sql;

			SetParameters(connection, parameters);

			using (var rd = connection.Command.ExecuteReader())
			{
				if (rd.Read())
				{
					var objectReader = GetObjectReader<T>(connection, rd);
					do
						yield return objectReader(rd);
					while (rd.Read());
				}
			}
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql, DataParameter parameter)
		{
			return Query<T>(connection, sql, new[] { parameter });
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql, object parameters)
		{
			connection.Command.CommandText = sql;

			var dps = GetDataParameters(connection.MappingSchema, parameters);

			SetParameters(connection, dps);

			using (var rd = connection.Command.ExecuteReader())
			{
				if (rd.Read())
				{
					var objectReader = GetObjectReader<T>(connection, rd);
					do
						yield return objectReader(rd);
					while (rd.Read());
				}
			}
		}

		#endregion

		#region Query with template

		public static IEnumerable<T> Query<T>(this DataConnection connection, T template, string sql, params DataParameter[] parameters)
		{
			return Query<T>(connection, sql, parameters);
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, T template, string sql, object parameters)
		{
			return Query<T>(connection, sql, parameters);
		}

		#endregion

		#region Execute

		public static int Execute(this DataConnection connection, string sql)
		{
			connection.Command.CommandText = sql;
			return connection.Command.ExecuteNonQuery();
		}

		public static int Execute(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			connection.Command.CommandText = sql;

			SetParameters(connection, parameters);

			return connection.Command.ExecuteNonQuery();
		}

		public static int Execute(this DataConnection connection, string sql, object parameters)
		{
			connection.Command.CommandText = sql;

			var dps = GetDataParameters(connection.MappingSchema, parameters);

			SetParameters(connection, dps);

			return connection.Command.ExecuteNonQuery();
		}

		#endregion

		#region Execute scalar

		public static T Execute<T>(this DataConnection connection, string sql)
		{
			connection.Command.CommandText = sql;

			using (var rd = connection.Command.ExecuteReader())
			{
				if (rd.Read())
				{
					var objectReader = GetObjectReader<T>(connection, rd);
					return objectReader(rd);
				}
			}

			return default(T);
		}

		public static T Execute<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			connection.Command.CommandText = sql;

			SetParameters(connection, parameters);

			using (var rd = connection.Command.ExecuteReader())
			{
				if (rd.Read())
				{
					var objectReader = GetObjectReader<T>(connection, rd);
					return objectReader(rd);
				}
			}

			return default(T);
		}

		public static T Execute<T>(this DataConnection connection, string sql, DataParameter parameter)
		{
			return Execute<T>(connection, sql, new[] { parameter });
		}

		public static T Execute<T>(this DataConnection connection, string sql, object parameters)
		{
			connection.Command.CommandText = sql;

			var dps = GetDataParameters(connection.MappingSchema, parameters);

			SetParameters(connection, dps);

			using (var rd = connection.Command.ExecuteReader())
			{
				if (rd.Read())
				{
					var objectReader = GetObjectReader<T>(connection, rd);
					return objectReader(rd);
				}
			}

			return default(T);
		}

		#endregion

		#region GetObjectReader

		static readonly ConcurrentDictionary<object, Delegate> _objectReaders = new ConcurrentDictionary<object, Delegate>();

		static Func<IDataReader,T> GetObjectReader<T>(DataConnection dataConnection, IDataReader dataReader)
		{
			var key = new
			{
				Type   = typeof(T),
				Config = dataConnection.ConfigurationString ?? dataConnection.ConnectionString ?? dataConnection.Connection.ConnectionString,
				Sql    = dataConnection.Command.CommandText,
			};

			Delegate func;

			if (!_objectReaders.TryGetValue(key, out func))
			{
				var dataProvider   = dataConnection.DataProvider;
				var parameter      = Expression.Parameter(typeof(IDataReader));
				var dataReaderExpr = dataProvider.ConvertDataReader(parameter);

				Expression expr;

				Func<Type,int,Expression> getMemberExpression = (type,idx) =>
				{
					var ex = dataProvider.GetReaderExpression(dataConnection.MappingSchema, dataReader, idx, dataReaderExpr, type);

					if (ex.NodeType == ExpressionType.Lambda)
					{
						var l = (LambdaExpression)ex;
						ex = l.Body.Transform(e => e == l.Parameters[0] ? dataReaderExpr : e);
					}

					return ex;
				};

				if (dataConnection.MappingSchema.IsScalarType(typeof(T)))
				{
					expr = getMemberExpression(typeof(T), 0);
				}
				else
				{
					var td    = new TypeDescriptor(dataConnection.MappingSchema, typeof(T));
					var names = new List<string>(dataReader.FieldCount);

					for (var i = 0; i < dataReader.FieldCount; i++)
						names.Add(dataReader.GetName(i));

					expr = null;

					var ctors = typeof(T).GetConstructors().Select(c => new { c, ps = c.GetParameters() }).ToList();

					if (ctors.Count > 0 && ctors.All(c => c.ps.Length > 0))
					{
						var q =
							from c in ctors
							let count = c.ps.Count(p => names.Contains(p.Name))
							orderby count descending
							select c;

						var ctor = q.FirstOrDefault();

						if (ctor != null)
						{
							expr = Expression.New(
								ctor.c,
								ctor.ps.Select(p => names.Contains(p.Name) ?
									getMemberExpression(p.ParameterType, names.IndexOf(p.Name)) :
									Expression.Constant(dataConnection.MappingSchema.GetDefaultValue(p.ParameterType), p.ParameterType)));
						}
					}

					if (expr == null)
					{
						var members =
						(
							from n in names.Select((name,idx) => new { name, idx })
							let   member = td.Members.FirstOrDefault(m => m.ColumnName == n.name)
							where member != null
							select new
							{
								Member = member,
								Expr   = getMemberExpression(member.MemberType, n.idx),
							}
						).ToList();

						expr = Expression.MemberInit(
							Expression.New(typeof(T)),
							members.Select(m => Expression.Bind(m.Member.MemberInfo, m.Expr)));
					}
				}

				if (expr.GetCount(e => e == dataReaderExpr) > 1)
				{
					var dataReaderVar = Expression.Variable(dataReaderExpr.Type, "dr");
					var assignment    = Expression.Assign(dataReaderVar, dataReaderExpr);

					expr = expr.Transform(e => e == dataReaderExpr ? dataReaderVar : e);
					expr = Expression.Block(new[] { dataReaderVar }, new[] { assignment, expr });
				}

				var lex = Expression.Lambda<Func<IDataReader,T>>(expr, parameter);

				func = lex.Compile();
			}

			return (Func<IDataReader,T>)func;
		}

		#endregion

		#region SetParameters

		static void SetParameters(DataConnection dataConnection, DataParameter[] parameters)
		{
			if (parameters == null || parameters.Length == 0)
				return;

			dataConnection.Command.Parameters.Clear();

			foreach (var parameter in parameters)
			{
				var p = dataConnection.DataProvider.GetParameter(
					dataConnection.Command,
					parameter.Name,
					parameter.DataType,
					parameter.Value);

				dataConnection.Command.Parameters.Add(p);
			}
		}

		static readonly ConcurrentDictionary<Type, Func<object,DataParameter[]>> _parameterReaders =
			new ConcurrentDictionary<Type,Func<object,DataParameter[]>>();

		static readonly PropertyInfo _dataParameterName     = MemberHelper.PropertyOf<DataParameter>(p => p.Name);
		static readonly PropertyInfo _dataParameterDataType = MemberHelper.PropertyOf<DataParameter>(p => p.DataType);
		static readonly PropertyInfo _dataParameterValue    = MemberHelper.PropertyOf<DataParameter>(p => p.Value);

		static DataParameter[] GetDataParameters(MappingSchema mappingSchema, object parameters)
		{
			if (parameters == null)
				return null;

			if (parameters is DataParameter[])
				return (DataParameter[])parameters;

			if (parameters is DataParameter)
				return new[] { (DataParameter)parameters };

			Func<object,DataParameter[]> func;
			var type = parameters.GetType();

			if (!_parameterReaders.TryGetValue(type, out func))
			{
				var td  = new TypeDescriptor(mappingSchema, type);
				var p   = Expression.Parameter(typeof(object), "p");
				var obj = Expression.Parameter(parameters.GetType(), "obj");

				var expr = Expression.Lambda<Func<object,DataParameter[]>>(
					Expression.Block(
						new[] { obj },
						new Expression[]
						{
							Expression.Assign(obj, Expression.Convert(p, type)),
							Expression.NewArrayInit(
								typeof(DataParameter),
								td.Members.Select(m =>
								{
									if (m.MemberType == typeof(DataParameter))
									{
										var pobj = Expression.Parameter(typeof(DataParameter));

										return Expression.Block(
											new[] { pobj },
											new Expression[]
											{
												Expression.Assign(pobj, Expression.PropertyOrField(obj, m.MemberName)),
												Expression.MemberInit(
													Expression.New(typeof(DataParameter)),
													Expression.Bind(
														_dataParameterName,
														Expression.Coalesce(
															Expression.MakeMemberAccess(pobj, _dataParameterName),
															Expression.Constant(m.ColumnName))),
													Expression.Bind(
														_dataParameterDataType,
														Expression.MakeMemberAccess(pobj, _dataParameterDataType)),
													Expression.Bind(
														_dataParameterValue,
														Expression.Convert(
															Expression.MakeMemberAccess(pobj, _dataParameterValue),
															typeof(object))))
											});
									}

									return (Expression)Expression.MemberInit(
										Expression.New(typeof(DataParameter)),
										Expression.Bind(
											_dataParameterName,
											Expression.Constant(m.ColumnName)),
										Expression.Bind(
											_dataParameterValue,
											Expression.Convert(
												Expression.PropertyOrField(obj, m.MemberName),
												typeof(object))));
								}))
						}
					),
					p);

				_parameterReaders[parameters.GetType()] = func = expr.Compile();
			}

			return func(parameters);
		}

		#endregion
	}
}
