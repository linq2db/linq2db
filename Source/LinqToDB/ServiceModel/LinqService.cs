using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Web.Services;

namespace LinqToDB.ServiceModel
{
	using Common;
	using Data;
	using Linq;
	using Extensions;
	using SqlQuery;
	using LinqToDB.Expressions;

	[ServiceBehavior  (InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
	[WebService       (Namespace  = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	public class LinqService : ILinqService
	{
		public bool AllowUpdates { get; set; }

		public static Func<string,Type> TypeResolver = _ => null;

		public virtual DataConnection CreateDataContext(string configuration)
		{
			return new DataConnection(configuration);
		}

		protected virtual void ValidateQuery(LinqServiceQuery query)
		{
			if (AllowUpdates == false && query.Statement.QueryType != QueryType.Select)
				throw new LinqException("Insert/Update/Delete requests are not allowed by the service policy.");
		}

		protected virtual void HandleException(Exception exception)
		{
		}

		#region ILinqService Members

		[WebMethod]
		public virtual LinqServiceInfo GetInfo(string configuration)
		{
			using (var ctx = CreateDataContext(configuration))
			{
				return new LinqServiceInfo
				{
					MappingSchemaType = ctx.DataProvider.MappingSchema.     GetType().AssemblyQualifiedName,
					SqlBuilderType    = ctx.DataProvider.CreateSqlBuilder().GetType().AssemblyQualifiedName,
					SqlOptimizerType  = ctx.DataProvider.GetSqlOptimizer(). GetType().AssemblyQualifiedName,
					SqlProviderFlags  = ctx.DataProvider.SqlProviderFlags,
					ConfigurationList = ctx.MappingSchema.ConfigurationList,
				};
			}
		}

		class QueryContext : IQueryContext
		{
			public SqlStatement   Statement   { get; set; }
			public object         Context     { get; set; }
			public SqlParameter[] Parameters  { get; set; }
			public List<string>   QueryHints  { get; set; }

			public SqlParameter[] GetParameters()
			{
				return Parameters;
			}
		}

		[WebMethod]
		public int ExecuteNonQuery(string configuration, string queryData)
		{
			try
			{
				var query = LinqServiceSerializer.Deserialize(queryData);

				ValidateQuery(query);

				using (var db = CreateDataContext(configuration))
				using (db.DataProvider.ExecuteScope())
				{
					return DataConnection.QueryRunner.ExecuteNonQuery(db, new QueryContext
					{
						Statement  = query.Statement,
						Parameters = query.Parameters,
						QueryHints = query.QueryHints
					});
				}
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		[WebMethod]
		public object ExecuteScalar(string configuration, string queryData)
		{
			try
			{
				var query = LinqServiceSerializer.Deserialize(queryData);

				ValidateQuery(query);

				using (var db = CreateDataContext(configuration))
				using (db.DataProvider.ExecuteScope())
				{
					return DataConnection.QueryRunner.ExecuteScalar(db, new QueryContext
					{
						Statement  = query.Statement,
						Parameters = query.Parameters,
						QueryHints = query.QueryHints
					});
				}
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		[WebMethod]
		public string ExecuteReader(string configuration, string queryData)
		{
			try
			{
				var query = LinqServiceSerializer.Deserialize(queryData);

				ValidateQuery(query);

				using (var db = CreateDataContext(configuration))
				using (db.DataProvider.ExecuteScope())
				{
					using (var rd = DataConnection.QueryRunner.ExecuteReader(db, new QueryContext
					{
						Statement   = query.Statement,
						Parameters  = query.Parameters,
						QueryHints  = query.QueryHints
					}))
					{
						var ret = new LinqServiceResult
						{
							QueryID    = Guid.NewGuid(),
							FieldCount = rd.FieldCount,
							FieldNames = new string[rd.FieldCount],
							FieldTypes = new Type  [rd.FieldCount],
							Data       = new List<string[]>(),
						};

						var names = new HashSet<string>();

						for (var i = 0; i < ret.FieldCount; i++)
						{
							var name = rd.GetName(i);
							var idx  = 0;

							if (names.Contains(name))
							{
								while (names.Contains(name = "c" + ++idx))
								{
								}
							}

							names.Add(name);

							ret.FieldNames[i] = name;
							// ugh...
							ret.FieldTypes[i] = query.Statement.SelectQuery.Select.Columns[i].SystemType;
						}

						var columnTypes = new Type[ret.FieldCount];
						var codes       = new TypeCode[rd.FieldCount];

						for (var i = 0; i < ret.FieldCount; i++)
						{
							columnTypes[i] = ret.FieldTypes[i].ToNullableUnderlying();
							codes[i]       = Type.GetTypeCode(columnTypes[i]);
						}

						var varyingTypes = new List<Type>();

						var columnReaders = new ConvertFromDataReaderExpression.ColumnReader[rd.FieldCount];
						for (var i = 0; i < ret.FieldCount; i++)
							columnReaders[i] = new ConvertFromDataReaderExpression.ColumnReader(db, db.MappingSchema, ret.FieldTypes[i], i);

						while (rd.Read())
						{
							var data  = new string  [rd.FieldCount];

							ret.RowCount++;

							for (var i = 0; i < ret.FieldCount; i++)
							{
								if (!rd.IsDBNull(i))
								{
									var value = columnReaders[i].GetValue(rd);
									var type  = value.GetType();
									var code  = codes[i];

									var idx   = -1;

									if (type != columnTypes[i])
									{
										code = Type.GetTypeCode(type);
										idx  = varyingTypes.IndexOf(type);

										if (idx < 0)
										{
											varyingTypes.Add(type);
											idx = varyingTypes.Count - 1;
										}
									}

									switch (code)
									{
										case TypeCode.Char     : data[i] = ((char)value)               .ToString(CultureInfo.InvariantCulture); break;
										case TypeCode.Byte     : data[i] = ((byte)value)               .ToString(CultureInfo.InvariantCulture); break;
										case TypeCode.SByte    : data[i] = ((sbyte)value)              .ToString(CultureInfo.InvariantCulture); break;
										case TypeCode.Int16    : data[i] = ((short)value)              .ToString(CultureInfo.InvariantCulture); break;
										case TypeCode.UInt16   : data[i] = ((ushort)value)             .ToString(CultureInfo.InvariantCulture); break;
										case TypeCode.Int32    : data[i] = ((int)value)                .ToString(CultureInfo.InvariantCulture); break;
										case TypeCode.UInt32   : data[i] = ((uint)value)               .ToString(CultureInfo.InvariantCulture); break;
										case TypeCode.Int64    : data[i] = ((long)value)               .ToString(CultureInfo.InvariantCulture); break;
										case TypeCode.UInt64   : data[i] = ((ulong)value)              .ToString(CultureInfo.InvariantCulture); break;
										case TypeCode.Decimal  : data[i] = ((decimal)value)            .ToString(CultureInfo.InvariantCulture); break;
										case TypeCode.Double   : data[i] = ((double)value)             .ToString("G17", CultureInfo.InvariantCulture); break;
										case TypeCode.Single   : data[i] = ((float)value)              .ToString("G9" , CultureInfo.InvariantCulture); break;
										case TypeCode.DateTime : data[i] = ((DateTime)value).ToBinary().ToString(CultureInfo.InvariantCulture); break;
										default                :
											{
												if (value is DateTimeOffset dto)
													data[i] = dto.UtcTicks.ToString(CultureInfo.InvariantCulture);
												else if (value is byte[] bytea)
													data[i] = ConvertTo<string>.From(bytea);
												else
													data[i] = (value ?? "").ToString();

												break;
											}
									}

									if (idx >= 0)
										data[i] = "\0" + (char)idx + data[i];
								}
							}

							ret.Data.Add(data);
						}

						ret.VaryingTypes = varyingTypes.ToArray();

						return LinqServiceSerializer.Serialize(ret);
					}
				}
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		[WebMethod]
		public int ExecuteBatch(string configuration, string queryData)
		{
			try
			{
				var data    = LinqServiceSerializer.DeserializeStringArray(queryData);
				var queries = data.Select(LinqServiceSerializer.Deserialize).ToArray();

				foreach (var query in queries)
					ValidateQuery(query);

				using (var db = CreateDataContext(configuration))
				using (db.DataProvider.ExecuteScope())
				{
					db.BeginTransaction();

					foreach (var query in queries)
					{
						DataConnection.QueryRunner.ExecuteNonQuery(db, new QueryContext
						{
							Statement   = query.Statement,
							Parameters  = query.Parameters,
							QueryHints  = query.QueryHints
						});
					}

					db.CommitTransaction();

					return queryData.Length;
				}
			}
			catch (Exception exception)
			{
				HandleException(exception);
				throw;
			}
		}

		#endregion
	}
}
