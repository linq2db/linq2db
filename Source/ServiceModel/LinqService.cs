using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Web.Services;

namespace LinqToDB.ServiceModel
{
	using Data;
	using Data.Linq;
	using Data.Sql;

	[ServiceBehavior  (InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
	[WebService       (Namespace  = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	public class LinqService : ILinqService
	{
		public LinqService()
		{
		}

		public LinqService(string configuration)
		{
			Configuration = configuration;
		}

		public string Configuration { get; set; }
		public bool   AllowUpdates  { get; set; }

		public static Func<string,Type> TypeResolver = _ => null;

		public virtual IDataContext CreateDataContext()
		{
			return Settings.CreateDefaultDataContext(Configuration);
		}

		protected virtual void ValidateQuery(LinqServiceQuery query)
		{
			if (AllowUpdates == false && !query.Query.IsSelect)
				throw new LinqException("Insert/Update/Delete requests are not allowed by the service policy.");
		}

		#region ILinqService Members

		public Type SqlProviderType { get; set; }

		[WebMethod]
		public virtual string GetSqlProviderType()
		{
			if (SqlProviderType == null)
				using (var ctx = CreateDataContext())
					SqlProviderType = ctx.CreateSqlProvider().GetType();

			return SqlProviderType.FullName;
		}

		class QueryContext : IQueryContext
		{
			public SqlQuery       SqlQuery   { get; set; }
			public object         Context    { get; set; }
			public SqlParameter[] Parameters { get; set; }

			public SqlParameter[] GetParameters()
			{
				return Parameters;
			}
		}

		[WebMethod]
		public int ExecuteNonQuery(string queryData)
		{
			var query = LinqServiceSerializer.Deserialize(queryData);

			ValidateQuery(query);

			using (var db = CreateDataContext())
			{
				var obj = db.SetQuery(new QueryContext { SqlQuery = query.Query, Parameters = query.Parameters });
				return db.ExecuteNonQuery(obj);
			}
		}

		[WebMethod]
		public object ExecuteScalar(string queryData)
		{
			var query = LinqServiceSerializer.Deserialize(queryData);

			ValidateQuery(query);

			using (var db = CreateDataContext())
			{
				var obj = db.SetQuery(new QueryContext { SqlQuery = query.Query, Parameters = query.Parameters });
				return db.ExecuteScalar(obj);
			}
		}

		[WebMethod]
		public string ExecuteReader(string queryData)
		{
			var query = LinqServiceSerializer.Deserialize(queryData);

			ValidateQuery(query);

			using (var db = CreateDataContext())
			{
				var obj = db.SetQuery(new QueryContext { SqlQuery = query.Query, Parameters = query.Parameters });

				using (var rd = db.ExecuteReader(obj))
				{
					var ret = new LinqServiceResult
					{
						QueryID    = Guid.NewGuid(),
						FieldCount = rd.FieldCount,
						FieldNames = new string[rd.FieldCount],
						FieldTypes = new Type  [rd.FieldCount],
						Data       = new List<string[]>(),
					};

					for (var i = 0; i < ret.FieldCount; i++)
					{
						ret.FieldNames[i] = rd.GetName(i);
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
									case TypeCode.DateTime : data[i] = rd.GetDateTime(i).ToString(CultureInfo.InvariantCulture); break;
									default                :
										{
											if (ret.FieldTypes[i] == typeof(byte[]))
												data[i] = Convert.ToBase64String((byte[])rd.GetValue(i));
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

		[WebMethod]
		public int ExecuteBatch(string queryData)
		{
			var data    = LinqServiceSerializer.DeserializeStringArray(queryData);
			var queries = data.Select<string,LinqServiceQuery>(LinqServiceSerializer.Deserialize).ToArray();

			foreach (var query in queries)
				ValidateQuery(query);

			using (var db = CreateDataContext())
			{
				if (db is DbManager) ((DbManager)db).BeginTransaction();

				foreach (var query in queries)
				{
					var obj = db.SetQuery(new QueryContext { SqlQuery = query.Query, Parameters = query.Parameters });
					db.ExecuteNonQuery(obj);
				}

				if (db is DbManager) ((DbManager)db).CommitTransaction();

				return queryData.Length;
			}
		}

		#endregion
	}
}
