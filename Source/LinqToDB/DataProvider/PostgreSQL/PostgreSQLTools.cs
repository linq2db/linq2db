using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Configuration;
	using Data;

	[PublicAPI]
	public static class PostgreSQLTools
	{
		static readonly PostgreSQLDataProvider _postgreSQLDataProvider   = new PostgreSQLDataProvider();
		static readonly PostgreSQLDataProvider _postgreSQLDataProvider92 = new PostgreSQLDataProvider(ProviderName.PostgreSQL92, PostgreSQLVersion.v92);
		static readonly PostgreSQLDataProvider _postgreSQLDataProvider93 = new PostgreSQLDataProvider(ProviderName.PostgreSQL93, PostgreSQLVersion.v93);
		static readonly PostgreSQLDataProvider _postgreSQLDataProvider95 = new PostgreSQLDataProvider(ProviderName.PostgreSQL95, PostgreSQLVersion.v95);

		public static bool AutoDetectProvider { get; set; }

		static PostgreSQLTools()
		{
			AutoDetectProvider = true;

			DataConnection.AddDataProvider(_postgreSQLDataProvider);
			DataConnection.AddDataProvider(_postgreSQLDataProvider92);
			DataConnection.AddDataProvider(_postgreSQLDataProvider93);
			DataConnection.AddDataProvider(_postgreSQLDataProvider95);

			DataConnection.AddProviderDetector(ProviderDetector);
		}

		static IDataProvider ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			//if (css.IsGlobal /* DataConnection.IsMachineConfig(css)*/)
			//	return null;

			switch (css.ProviderName)
			{
				case ""               :
				case null             :

					if (css.Name == "PostgreSQL")
						goto case "PostgreSQL";
					break;

				case "PostgreSQL92"   :
				case "PostgreSQL.92"  :
				case "PostgreSQL.9.2" :
					return _postgreSQLDataProvider;

				case "PostgreSQL93"   : case "PostgreSQL.93"  : case "PostgreSQL.9.3" :
				case "PostgreSQL94"   : case "PostgreSQL.94"  : case "PostgreSQL.9.4" :
					return _postgreSQLDataProvider93;

				case "PostgreSQL95"   : case "PostgreSQL.95"  : case "PostgreSQL.9.5" :
				case "PostgreSQL96"   : case "PostgreSQL.96"  : case "PostgreSQL.9.6" :
					return _postgreSQLDataProvider95;

				case "PostgreSQL"     :
				case "Npgsql"         :

					if (css.Name.Contains("92") || css.Name.Contains("9.2"))
						return _postgreSQLDataProvider;

					if (css.Name.Contains("93") || css.Name.Contains("9.3") ||
						css.Name.Contains("94") || css.Name.Contains("9.4"))
						return _postgreSQLDataProvider93;

					if (css.Name.Contains("95") || css.Name.Contains("9.5") ||
						css.Name.Contains("96") || css.Name.Contains("9.6"))
						return _postgreSQLDataProvider95;

					if (AutoDetectProvider)
					{
						try
						{
							var connectionType    = Type.GetType("Npgsql.NpgsqlConnection, Npgsql", true);
							var connectionCreator = DynamicDataProviderBase.CreateConnectionExpression(connectionType).Compile();
							var cs                = string.IsNullOrWhiteSpace(connectionString) ? css.ConnectionString : connectionString;

							using (var conn = connectionCreator(cs))
							{
								conn.Open();

								var postgreSqlVersion = ((dynamic)conn).PostgreSqlVersion;

								if (postgreSqlVersion.Major > 9 || postgreSqlVersion.Major == 9 && postgreSqlVersion.Minor > 4)
								{
									return _postgreSQLDataProvider95;
								}

								if (postgreSqlVersion.Major == 9 && postgreSqlVersion.Minor > 2)
								{
									return _postgreSQLDataProvider93;
								}

								return _postgreSQLDataProvider;
							}
						}
						catch (Exception)
						{
							return _postgreSQLDataProvider;
						}
					}

					break;
			}

			return null;
		}

		public static IDataProvider GetDataProvider(PostgreSQLVersion version = PostgreSQLVersion.v92)
		{
			switch (version)
			{
				case PostgreSQLVersion.v95:
					return _postgreSQLDataProvider95;
				case PostgreSQLVersion.v93:
					return _postgreSQLDataProvider93;
				case PostgreSQLVersion.v92:
					return _postgreSQLDataProvider92;
				default:
					return _postgreSQLDataProvider;
			}
		}

		public static void ResolvePostgreSQL(string path)
		{
			new AssemblyResolver(path, "Npgsql");
		}

		public static void ResolvePostgreSQL(Assembly assembly)
		{
			new AssemblyResolver(assembly, "Npgsql");
		}

		public static Type GetBitStringType       () { return _postgreSQLDataProvider92.BitStringType;        }
		public static Type GetNpgsqlIntervalType  () { return _postgreSQLDataProvider92.NpgsqlIntervalType;   }
		public static Type GetNpgsqlInetType      () { return _postgreSQLDataProvider92.NpgsqlInetType;       }
		public static Type GetNpgsqlTimeTZType    () { return _postgreSQLDataProvider92.NpgsqlTimeTZType;     }
		public static Type GetNpgsqlTimeType      () { return _postgreSQLDataProvider92.NpgsqlTimeType;       }
		public static Type GetNpgsqlPointType     () { return _postgreSQLDataProvider92.NpgsqlPointType;      }
		public static Type GetNpgsqlLSegType      () { return _postgreSQLDataProvider92.NpgsqlLSegType;       }
		public static Type GetNpgsqlBoxType       () { return _postgreSQLDataProvider92.NpgsqlBoxType;        }
		public static Type GetNpgsqlPathType      () { return _postgreSQLDataProvider92.NpgsqlPathType;       }
		public static Type GetNpgsqlPolygonType   () { return _postgreSQLDataProvider92.NpgsqlPolygonType;    }
		public static Type GetNpgsqlCircleType    () { return _postgreSQLDataProvider92.NpgsqlCircleType;     }
		public static Type GetNpgsqlMacAddressType() { return _postgreSQLDataProvider92.NpgsqlMacAddressType; }

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, PostgreSQLVersion version = PostgreSQLVersion.v92)
		{
			return new DataConnection(GetDataProvider(version), connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection, PostgreSQLVersion version = PostgreSQLVersion.v92)
		{
			return new DataConnection(GetDataProvider(version), connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction, PostgreSQLVersion version = PostgreSQLVersion.v92)
		{
			return new DataConnection(GetDataProvider(version), transaction);
		}

		#endregion

		#region BulkCopy

		private static BulkCopyType _defaultBulkCopyType = BulkCopyType.MultipleRows;
		public  static BulkCopyType  DefaultBulkCopyType
		{
			get { return _defaultBulkCopyType;  }
			set { _defaultBulkCopyType = value; }
		}

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection             dataConnection,
			IEnumerable<T>             source,
			int                        maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied> rowsCopiedCallback = null)
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.MultipleRows,
					MaxBatchSize       = maxBatchSize,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		#endregion
	}
}
