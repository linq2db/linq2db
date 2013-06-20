﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Data
{
	using Common;
	using Expressions;
	using Extensions;

	public static class DataConnectionExtensions
	{
		#region Query with object reader

		public static IEnumerable<T> Query<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql)
		{
			connection.SetCommand(sql);

			using (var rd = connection.Command.ExecuteReader())
				while (rd.Read())
					yield return objectReader(rd);
		}

		public static IEnumerable<T> QueryProc<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, params DataParameter[] parameters)
		{
			connection.SetCommand(sql);
			connection.Command.CommandType = CommandType.StoredProcedure;

			SetParameters(connection, parameters);

			using (var rd = connection.Command.ExecuteReader())
				while (rd.Read())
					yield return objectReader(rd);
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, params DataParameter[] parameters)
		{
			connection.SetCommand(sql);

			SetParameters(connection, parameters);

			using (var rd = connection.Command.ExecuteReader())
				while (rd.Read())
					yield return objectReader(rd);
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql, object parameters)
		{
			connection.SetCommand(sql);

			var dps = GetDataParameters(connection, parameters);

			SetParameters(connection, dps);

			using (var rd = connection.Command.ExecuteReader())
				while (rd.Read())
					yield return objectReader(rd);
		}

		#endregion

		#region Query

		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql)
		{
			connection.SetCommand(sql);
			return ExecuteQuery<T>(connection);
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			connection.SetCommand(sql);

			SetParameters(connection, parameters);

			return ExecuteQuery<T>(connection);
		}

		public static IEnumerable<T> QueryProc<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			connection.SetCommand(sql);
			connection.Command.CommandType = CommandType.StoredProcedure;

			SetParameters(connection, parameters);

			return ExecuteQuery<T>(connection);
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql, DataParameter parameter)
		{
			return Query<T>(connection, sql, new[] { parameter });
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql, object parameters)
		{
			connection.SetCommand(sql);

			var dps = GetDataParameters(connection, parameters);

			SetParameters(connection, dps);

			return ExecuteQuery<T>(connection);
		}

		static IEnumerable<T> ExecuteQuery<T>(DataConnection connection)
		{
			using (var rd = connection.Command.ExecuteReader())
			{
				if (rd.Read())
				{
					var objectReader = GetObjectReader<T>(connection, rd, connection.Command.CommandText);
					var isFaulted    = false;

					do
					{
						T result;

						try
						{
							result = objectReader(rd);
						}
						catch (InvalidCastException)
						{
							if (isFaulted)
								throw;

							isFaulted    = true;
							objectReader = GetObjectReader2<T>(connection, rd, connection.Command.CommandText);
							result       = objectReader(rd);
						}

						yield return result;

					} while (rd.Read());
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
			connection.SetCommand(sql);
			return connection.Command.ExecuteNonQuery();
		}

		public static int Execute(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			connection.SetCommand(sql);

			SetParameters(connection, parameters);

			return connection.Command.ExecuteNonQuery();
		}

		public static int ExecuteProc(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			connection.SetCommand(sql);
			connection.Command.CommandType = CommandType.StoredProcedure;

			SetParameters(connection, parameters);

			return connection.Command.ExecuteNonQuery();
		}

		public static int Execute(this DataConnection connection, string sql, object parameters)
		{
			connection.SetCommand(sql);

			var dps = GetDataParameters(connection, parameters);

			SetParameters(connection, dps);

			return connection.Command.ExecuteNonQuery();
		}

		#endregion

		#region Execute scalar

		public static T Execute<T>(this DataConnection connection, string sql)
		{
			connection.SetCommand(sql);
			return ExecuteScalar<T>(connection, sql);
		}

		public static T Execute<T>(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			connection.SetCommand(sql);

			SetParameters(connection, parameters);

			return ExecuteScalar<T>(connection, sql);
		}

		public static T Execute<T>(this DataConnection connection, string sql, DataParameter parameter)
		{
			connection.SetCommand(sql);

			SetParameters(connection, new[] { parameter });

			return ExecuteScalar<T>(connection, sql);
		}

		public static T Execute<T>(this DataConnection connection, string sql, object parameters)
		{
			connection.SetCommand(sql);

			var dps = GetDataParameters(connection, parameters);

			SetParameters(connection, dps);

			return ExecuteScalar<T>(connection, sql);
		}

		static T ExecuteScalar<T>(DataConnection connection, string sql)
		{
			using (var rd = connection.Command.ExecuteReader())
			{
				if (rd.Read())
				{
					var objectReader = GetObjectReader<T>(connection, rd, sql);

#if DEBUG
					//var value = rd.GetValue(0);
#endif

					try
					{
						return objectReader(rd);
					}
					catch (InvalidCastException)
					{
						return GetObjectReader2<T>(connection, rd, sql)(rd);
					}
				}
			}

			return default(T);
		}

		#endregion

		#region ExecuteReader

		public static DataReader ExecuteReader(this DataConnection connection, string sql)
		{
			connection.SetCommand(sql);
			return new DataReader { Connection = connection, Reader = connection.Command.ExecuteReader() };
		}

		public static DataReader ExecuteReader(this DataConnection connection, string sql, params DataParameter[] parameters)
		{
			connection.SetCommand(sql);

			SetParameters(connection, parameters);

			return new DataReader { Connection = connection, Reader = connection.Command.ExecuteReader() };
		}

		public static DataReader ExecuteReader(this DataConnection connection, string sql, DataParameter parameter)
		{
			return ExecuteReader(connection, sql, new[] { parameter });
		}

		public static DataReader ExecuteReader(this DataConnection connection, string sql, object parameters)
		{
			connection.SetCommand(sql);

			var dps = GetDataParameters(connection, parameters);

			SetParameters(connection, dps);

			return new DataReader { Connection = connection, Reader = connection.Command.ExecuteReader() };
		}

		public static DataReader ExecuteReader(
			this DataConnection    connection,
			string                 sql,
			CommandType            commandType,
			CommandBehavior        commandBehavior,
			params DataParameter[] parameters)
		{
			connection.SetCommand(sql);

			connection.Command.CommandType = commandType;

			SetParameters(connection, parameters);

			return new DataReader { Connection = connection, Reader = connection.Command.ExecuteReader(commandBehavior) };
		}

		static IEnumerable<T> ExecuteQuery<T>(DataConnection connection, IDataReader rd, string sql)
		{
			if (rd.Read())
			{
				var objectReader = GetObjectReader<T>(connection, rd, sql);
				var isFaulted    = false;

				do
				{
					T result;

					try
					{
						result = objectReader(rd);
					}
					catch (InvalidCastException)
					{
						if (isFaulted)
							throw;

						isFaulted    = true;
						objectReader = GetObjectReader2<T>(connection, rd, sql);
						result       = objectReader(rd);
					}

					yield return result;

				} while (rd.Read());
			}
		}

		#region Query with object reader

		public static IEnumerable<T> Query<T>(this DataReader reader, Func<IDataReader,T> objectReader)
		{
			while (reader.Reader.Read())
				yield return objectReader(reader.Reader);
		}

		#endregion

		#region Query

		public static IEnumerable<T> Query<T>(this DataReader reader)
		{
			if (reader.ReadNumber != 0)
				if (!reader.Reader.NextResult())
					return Enumerable.Empty<T>();

			reader.ReadNumber++;

			return ExecuteQuery<T>(reader.Connection, reader.Reader, reader.Connection.Command.CommandText + "$$$" + reader.ReadNumber);
		}

		#endregion

		#region Query with template

		public static IEnumerable<T> Query<T>(this DataReader reader, T template)
		{
			return Query<T>(reader);
		}

		#endregion

		#region Execute scalar

		public static T Execute<T>(this DataReader reader)
		{
			if (reader.ReadNumber != 0)
				if (!reader.Reader.NextResult())
					return default(T);

			reader.ReadNumber++;

			var sql = reader.Connection.Command.CommandText + "$$$" + reader.ReadNumber;

			if (reader.Reader.Read())
			{
				var objectReader = GetObjectReader<T>(reader.Connection, reader.Reader, sql);

				try
				{
					return objectReader(reader.Reader);
				}
				catch (InvalidCastException)
				{
					return GetObjectReader2<T>(reader.Connection, reader.Reader, sql)(reader.Reader);
				}
			}

			return default(T);
		}

		#endregion

		#endregion

		#region GetObjectReader

		public struct QueryKey : IEquatable<QueryKey>
		{
			public QueryKey(Type type, int configID, string sql)
			{
				_type     = type;
				_configID = configID;
				_sql      = sql;

				unchecked
				{
					_hashCode = -1521134295 * (-1521134295 * (-1521134295 * 639348056 + _type.GetHashCode()) + _configID.GetHashCode()) + _sql.GetHashCode();
				}
			}

			public override bool Equals(object obj)
			{
				return Equals((QueryKey)obj);
			}

			readonly int    _hashCode;
			readonly Type   _type;
			readonly int    _configID;
			readonly string _sql;

			public override int GetHashCode()
			{
				return _hashCode;
			}

			public bool Equals(QueryKey other)
			{
				return
					_type     == other._type &&
					_sql      == other._sql  &&
					_configID == other._configID
					;
			}
		}

		static readonly ConcurrentDictionary<QueryKey,Delegate> _objectReaders = new ConcurrentDictionary<QueryKey,Delegate>();

		static Func<IDataReader,T> GetObjectReader<T>(DataConnection dataConnection, IDataReader dataReader, string sql)
		{
			var key = new QueryKey(typeof(T), dataConnection.ID, sql);

			Delegate func;

			if (!_objectReaders.TryGetValue(key, out func))
			{
				//return GetObjectReader2<T>(dataConnection, dataReader);
				_objectReaders[key] = func = CreateObjectReader<T>(dataConnection, dataReader, (type,idx,dataReaderExpr) =>
					new ConvertFromDataReaderExpression(type, idx, dataReaderExpr, dataConnection).Reduce(dataReader));
			}

			return (Func<IDataReader,T>)func;
		}

		static Func<IDataReader,T> GetObjectReader2<T>(DataConnection dataConnection, IDataReader dataReader, string sql)
		{
			var key = new QueryKey(typeof(T), dataConnection.ID, sql);

			var func = CreateObjectReader<T>(dataConnection, dataReader, (type,idx,dataReaderExpr) =>
				new ConvertFromDataReaderExpression(type, idx, dataReaderExpr, dataConnection).Reduce());

			_objectReaders[key] = func;

			return func;
		}

		static Func<IDataReader,T> CreateObjectReader<T>(
			DataConnection dataConnection,
			IDataReader    dataReader,
			Func<Type,int,Expression,Expression> getMemberExpression)
		{
			var parameter      = Expression.Parameter(typeof(IDataReader));
			var dataReaderExpr = Expression.Convert(parameter, dataReader.GetType());

			Expression expr;

			if (dataConnection.MappingSchema.IsScalarType(typeof(T)))
			{
				expr = getMemberExpression(typeof(T), 0, dataReaderExpr);
			}
			else
			{
				var td    = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));
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
								getMemberExpression(p.ParameterType, names.IndexOf(p.Name), dataReaderExpr) :
								Expression.Constant(dataConnection.MappingSchema.GetDefaultValue(p.ParameterType), p.ParameterType)));
					}
				}

				if (expr == null)
				{
					var members =
					(
						from n in names.Select((name,idx) => new { name, idx })
						let   member = td.Columns.FirstOrDefault(m => 
							string.Compare(m.ColumnName, n.name, dataConnection.MappingSchema.ColumnComparisonOption) == 0)
						where member != null
						select new
						{
							Member = member,
							Expr   = getMemberExpression(member.MemberType, n.idx, dataReaderExpr),
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

			return lex.Compile();
		}

		#endregion

		#region SetParameters

		static void SetParameters(DataConnection dataConnection, DataParameter[] parameters)
		{
			if (parameters == null)
				return;

			foreach (var parameter in parameters)
			{
				var p        = dataConnection.Command.CreateParameter();
				var dataType = parameter.DataType;
				var value    = parameter.Value;

				if (dataType == DataType.Undefined && value != null)
					dataType = dataConnection.MappingSchema.GetDataType(value.GetType());

				if (parameter.Direction != null) p.Direction = parameter.Direction.Value;
				if (parameter.Size      != null) p.Size      = parameter.Size.     Value;

				dataConnection.DataProvider.SetParameter(p, parameter.Name, dataType, value);
				dataConnection.Command.Parameters.Add(p);
			}
		}

		public struct ParamKey : IEquatable<ParamKey>
		{
			public ParamKey(Type type, int configID)
			{
				_type     = type;
				_configID = configID;

				unchecked
				{
					_hashCode = -1521134295 * (-1521134295 * 639348056 + _type.GetHashCode()) + _configID.GetHashCode();
				}
			}

			public override bool Equals(object obj)
			{
				return Equals((ParamKey)obj);
			}

			readonly int    _hashCode;
			readonly Type   _type;
			readonly int    _configID;

			public override int GetHashCode()
			{
				return _hashCode;
			}

			public bool Equals(ParamKey other)
			{
				return
					_type     == other._type &&
					_configID == other._configID
					;
			}
		}

		static readonly ConcurrentDictionary<ParamKey,Func<object,DataParameter[]>> _parameterReaders =
			new ConcurrentDictionary<ParamKey,Func<object,DataParameter[]>>();

		static readonly PropertyInfo _dataParameterName     = MemberHelper.PropertyOf<DataParameter>(p => p.Name);
		static readonly PropertyInfo _dataParameterDataType = MemberHelper.PropertyOf<DataParameter>(p => p.DataType);
		static readonly PropertyInfo _dataParameterValue    = MemberHelper.PropertyOf<DataParameter>(p => p.Value);

		static DataParameter[] GetDataParameters(DataConnection dataConnection, object parameters)
		{
			if (parameters == null)
				return null;

			if (parameters is DataParameter[])
				return (DataParameter[])parameters;

			if (parameters is DataParameter)
				return new[] { (DataParameter)parameters };

			Func<object,DataParameter[]> func;
			var type = parameters.GetType();
			var key  = new ParamKey(type, dataConnection.ID);

			if (!_parameterReaders.TryGetValue(key, out func))
			{
				var td  = dataConnection.MappingSchema.GetEntityDescriptor(type);
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
								td.Columns.Select(m =>
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

									var memberType  = m.MemberType.ToNullableUnderlying();
									var valueGetter = Expression.PropertyOrField(obj, m.MemberName) as Expression;
									var mapper      = dataConnection.MappingSchema.GetConvertExpression(memberType, typeof(DataParameter), createDefault : false);

									if (mapper != null)
									{
										return Expression.Call(
											MemberHelper.MethodOf(() => PrepareDataParameter(null, null)),
											mapper.GetBody(valueGetter),
											Expression.Constant(m.ColumnName));
									}

									if (memberType.IsEnum)
									{
										var mapType  = ConvertBuilder.GetDefaultMappingFromEnumType(dataConnection.MappingSchema, memberType);
										var convExpr = dataConnection.MappingSchema.GetConvertExpression(m.MemberType, mapType);

										memberType  = mapType;
										valueGetter = convExpr.GetBody(valueGetter);
									}

									return (Expression)Expression.MemberInit(
										Expression.New(typeof(DataParameter)),
										Expression.Bind(
											_dataParameterName,
											Expression.Constant(m.ColumnName)),
										Expression.Bind(
											_dataParameterDataType,
											Expression.Constant(dataConnection.MappingSchema.GetDataType(memberType))),
										Expression.Bind(
											_dataParameterValue,
											Expression.Convert(valueGetter, typeof(object))));
								}))
						}
					),
					p);

				_parameterReaders[key] = func = expr.Compile();
			}

			return func(parameters);
		}

		static DataParameter PrepareDataParameter(DataParameter dataParameter, string name)
		{
			if (dataParameter == null)
				return new DataParameter { Name = name };

			dataParameter.Name = name;

			return dataParameter;
		}

		#endregion

		#region BulkCopy

		public static int BulkCopy<T>(this DataConnection dataConnection, int maxBatchSize, IEnumerable<T> source)
		{
			return dataConnection.DataProvider.BulkCopy(dataConnection, maxBatchSize, source);
		}

		public static int BulkCopy<T>(this DataConnection dataConnection, IEnumerable<T> source)
		{
			return BulkCopy(dataConnection, 0, source);
		}

		public static int BulkCopy<T>(this DataConnection dataConnection, int maxBatchSize, params T[] source)
		{
			return BulkCopy(dataConnection, maxBatchSize, (IEnumerable<T>)source);
		}

		public static int BulkCopy<T>(this DataConnection dataConnection, params T[] source)
		{
			return BulkCopy(dataConnection, 0, (IEnumerable<T>)source);
		}

		#endregion
	}
}
