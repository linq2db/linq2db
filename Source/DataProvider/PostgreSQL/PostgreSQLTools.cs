using System;
using System.Data;
using System.Reflection;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Data;

	public static class PostgreSQLTools
	{
		static readonly PostgreSQLDataProvider _postgreSQLDataProvider = new PostgreSQLDataProvider();

		static PostgreSQLTools()
		{
			DataConnection.AddDataProvider(_postgreSQLDataProvider);
		}

		public static IDataProvider GetDataProvider()
		{
			return _postgreSQLDataProvider;
		}

		public static void ResolvePostgreSQL(string path)
		{
			new AssemblyResolver(path, "Npgsql");
		}

		public static void ResolvePostgreSQL(Assembly assembly)
		{
			new AssemblyResolver(assembly, "Npgsql");
		}

		public static Type GetBitStringType       () { return _postgreSQLDataProvider.BitStringType;        }
		public static Type GetNpgsqlIntervalType  () { return _postgreSQLDataProvider.NpgsqlIntervalType;   }
		public static Type GetNpgsqlInetType      () { return _postgreSQLDataProvider.NpgsqlInetType;       }
		public static Type GetNpgsqlTimeTZType    () { return _postgreSQLDataProvider.NpgsqlTimeTZType;     }
		public static Type GetNpgsqlTimeType      () { return _postgreSQLDataProvider.NpgsqlTimeType;       }
		public static Type GetNpgsqlPointType     () { return _postgreSQLDataProvider.NpgsqlPointType;      }
		public static Type GetNpgsqlLSegType      () { return _postgreSQLDataProvider.NpgsqlLSegType;       }
		public static Type GetNpgsqlBoxType       () { return _postgreSQLDataProvider.NpgsqlBoxType;        }
		public static Type GetNpgsqlPathType      () { return _postgreSQLDataProvider.NpgsqlPathType;       }
		public static Type GetNpgsqlPolygonType   () { return _postgreSQLDataProvider.NpgsqlPolygonType;    }
		public static Type GetNpgsqlCircleType    () { return _postgreSQLDataProvider.NpgsqlCircleType;     }
		public static Type GetNpgsqlMacAddressType() { return _postgreSQLDataProvider.NpgsqlMacAddressType; }

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_postgreSQLDataProvider, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_postgreSQLDataProvider, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_postgreSQLDataProvider, transaction);
		}

		#endregion
	}
}
