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
	using SqlQuery;

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
			public SqlStatement    Statement { get; set; }
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
				{
					return DataConnection.QueryRunner.ExecuteScalar(db, new QueryContext
					{
						Statement = query.Statement,
						Parameters  = query.Parameters,
						QueryHints  = query.QueryHints
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
				{
					using (var rd = DataConnection.QueryRunner.ExecuteReader(db, new QueryContext
					{
						Statement = query.Statement,
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
							ret.FieldTypes[i] = rd.GetFieldType(i);
						}

						var varyingTypes = new List<Type>();

						while (rd.Read())
						{
							var data  = new string  [rd.FieldCount];
							var codes = new TypeCode[rd.FieldCount];

							for (var i = 0; i < ret.FieldCount; i++)
								codes[i] = Type.GetTypeCode(ret.FieldTypes[i]);

							ret.RowCount++;

							for (var i = 0; i < ret.FieldCount; i++)
							{
								if (!rd.IsDBNull(i))
								{
									var code = codes[i];
									var type = rd.GetFieldType(i);
									var idx = -1;

									if (type != ret.FieldTypes[i])
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
										case TypeCode.Decimal  : data[i] = rd.GetDecimal (i).ToString(CultureInfo.InvariantCulture); break;
										case TypeCode.Double   : data[i] = rd.GetDouble  (i).ToString(CultureInfo.InvariantCulture); break;
										case TypeCode.Single   : data[i] = rd.GetFloat   (i).ToString(CultureInfo.InvariantCulture); break;
										case TypeCode.DateTime : data[i] = rd.GetDateTime(i).ToString("o");                          break;
										default                :
											{
												if (type == typeof(DateTimeOffset))
												{
													var dt = rd.GetValue(i);

													if (dt is DateTime)
														data[i] = ((DateTime)dt).ToString("o");
													else if (dt is DateTimeOffset)
														data[i] = ((DateTimeOffset)dt).ToString("o");
													else
														data[i] = rd.GetValue(i).ToString();
												}
												else if (type == typeof(byte[]))
													data[i] = ConvertTo<string>.From((byte[])rd.GetValue(i));
												else
													data[i] = (rd.GetValue(i) ?? "").ToString();

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
