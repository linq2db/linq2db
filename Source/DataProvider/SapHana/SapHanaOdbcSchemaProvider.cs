using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace LinqToDB.DataProvider.SapHana
{
	using Common;
	using Data;
	using SchemaProvider;

	class SapHanaOdbcSchemaProvider:SapHanaSchemaProvider
	{
		private String _dataSourceName;
		private String _databaseName;

		public override DatabaseSchema GetSchema(DataConnection dataConnection, GetSchemaOptions options = null)
		{
			DefaultSchema   = dataConnection.Execute<string>("SELECT CURRENT_SCHEMA FROM DUMMY");
			_databaseName   = ((DbConnection)dataConnection.Connection).Database;
			_dataSourceName = ((DbConnection) dataConnection.Connection).DataSource;            

			if (String.IsNullOrEmpty(_dataSourceName) || String.IsNullOrEmpty(_databaseName))
			{
				using (var reader = dataConnection.ExecuteReader(@"
					SELECT
						HOST,
						KEY,
						VALUE
					FROM M_HOST_INFORMATION
					WHERE KEY = 'sid'"))
				{
					if (reader.Reader.Read())
					{
						_dataSourceName = reader.Reader.GetString(0);
						_databaseName   = reader.Reader.GetString(2);
					}
				}
			}

			return base.GetSchema(dataConnection, options);
		}

		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
#if !NETSTANDARD && !NETSTANDARD2_0
			var dts = ((DbConnection)dataConnection.Connection).GetSchema("DataTypes");

			var dt = dts.AsEnumerable()
				.Where(x=> x["ProviderDbType"] != DBNull.Value)
				.Select(t => new DataTypeInfo
				{
					TypeName         = t.Field<string>("TypeName"),
					DataType         = t.Field<string>("DataType"),
					CreateFormat     = t.Field<string>("CreateFormat"),
					CreateParameters = t.Field<string>("CreateParameters"),
					ProviderDbType   = Converter.ChangeTypeTo<int>(t["ProviderDbType"]),
				}).ToList();

			return dt.GroupBy(x => new {x.TypeName, x.ProviderDbType}).Select(y =>
			{
				var x = y.First();
				if (x.CreateFormat == null)
				{
					x.CreateFormat = x.TypeName;
					if (x.CreateParameters != null)
					{
						x.CreateFormat += String.Concat('(',
							String.Join(", ",
								Enumerable.Range(0, x.CreateParameters.Split(',').Length)
									.Select(i => String.Concat('{', i, '}'))),
							')');
					}
				}
				return x;
			}).ToList();
#else 
			return new List<DataTypeInfo>();
#endif
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			return dataConnection.Query(rd =>
			{
				var constraint = rd.IsDBNull(3) ? null : rd.GetString(3);

				if (constraint != "PRIMARY KEY")
					return null;

				var schema     = rd.GetString(0);
				var tableName  = rd.GetString(1);
				var indexName  = rd.GetString(2);
				var columnName = rd.GetString(4);
				var position   = rd.GetInt32(5);

				return new PrimaryKeyInfo
				{
					TableID = String.Concat(schema, '.', tableName),
					ColumnName = columnName,
					Ordinal = position,
					PrimaryKeyName = indexName
				};
			}, @"
				SELECT
					SCHEMA_NAME,
					TABLE_NAME,
					INDEX_NAME,
					CONSTRAINT,
					COLUMN_NAME,
					POSITION
				FROM INDEX_COLUMNS")
				.Where(x => x != null).ToList();
		}

		protected override DataTable GetProcedureSchema(DataConnection dataConnection, string commandText, CommandType commandType, DataParameter[] parameters)
		{
			if (commandType == CommandType.StoredProcedure)
			{
				commandText = "{ CALL " + commandText + "(" + String.Join(",", parameters.Select(x => "?")) + ")}";    
			}

			//bug SchemaOnly simply doesn't work
			dataConnection.BeginTransaction();

			try
			{
				using (var rd = dataConnection.ExecuteReader(commandText, CommandType.Text, CommandBehavior.Default, parameters))
				{
					return rd.Reader.GetSchemaTable();
				}
			}
			finally
			{
				dataConnection.RollbackTransaction();
			}
		}

		protected override string GetDataSourceName(DbConnection dbConnection)
		{
			return _dataSourceName;
		}

		protected override string GetDatabaseName(DbConnection dbConnection)
		{
			return _databaseName;
		}
	}
}
