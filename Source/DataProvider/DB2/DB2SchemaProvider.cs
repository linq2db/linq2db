using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace LinqToDB.DataProvider.DB2
{
	using Common;

	using Data;
	using SchemaProvider;

	class DB2SchemaProvider : SchemaProviderBase
	{
		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var dts = ((DbConnection)dataConnection.Connection).GetSchema("DataTypes");

			return dts.AsEnumerable()
				.Select(t => new DataTypeInfo
				{
					TypeName         = t.Field<string>("SQL_TYPE_NAME"),
					DataType         = t.Field<string>("FRAMEWORK_TYPE"),
					//CreateFormat     = t.Field<string>("CreateFormat"),
					CreateParameters = t.Field<string>("CREATE_PARAMS"),
					//ProviderDbType   = t.Field<int>   ("ProviderDbType"),
				})
				.ToList();
		}

		protected override DataType GetDataType(string dataType, string columnType)
		{
			throw new NotImplementedException();
		}

		static readonly string[] _systemSchemas = new[]
		{
			"SYSPUBLIC", "SYSIBM", "SYSCAT", "SYSIBMADM", "SYSSTAT", "SYSTOOLS"
		};

		protected override List<TableInfo> GetTables(DataConnection dataConnection)
		{
			var tables = ((DbConnection)dataConnection.Connection).GetSchema("Tables");

			return
			(
				from t in tables.AsEnumerable()
				where
					new[] {"TABLE", "VIEW"}.Contains(t.Field<string>("TABLE_TYPE"))
				let catalog = t.Field<string>("TABLE_CATALOG")
				let schema  = t.Field<string>("TABLE_SCHEMA")
				let name    = t.Field<string>("TABLE_NAME")
				where
					ExcludedSchemas.Length != 0 || IncludedSchemas.Length != 0 ||
					ExcludedSchemas.Length == 0 && IncludedSchemas.Length == 0 && !_systemSchemas.Contains(schema)
				select new TableInfo
				{
					TableID         = catalog + '.' + schema + '.' + name,
					CatalogName     = catalog,
					SchemaName      = schema,
					TableName       = name,
					IsDefaultSchema = schema.IsNullOrEmpty(),
					IsView          = t.Field<string>("TABLE_TYPE") == "VIEW",
					Description     = t.Field<string>("REMARKS"),
				}
			).ToList();
		}

		protected override List<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection)
		{
			return
			(
				from pk in dataConnection.Query(
					rd => new
					{
						id   = dataConnection.Connection.Database + "." + rd[0] + "." + rd[1],
						name = rd.GetString(2),
						cols = rd.GetString(3).Split('+').Skip(1).ToArray(),
					},@"
					SELECT
						TABSCHEMA,
						TABNAME,
						INDNAME,
						COLNAMES
					FROM
						SYSCAT.INDEXES
					WHERE
						UNIQUERULE = 'P'")
				from col in pk.cols.Select((c,i) => new { c, i })
				select new PrimaryKeyInfo
				{
					TableID        = pk.id,
					PrimaryKeyName = pk.name,
					ColumnName     = col.c,
					Ordinal        = col.i
				}
			).ToList();
		}

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection)
		{
			var cols = ((DbConnection)dataConnection.Connection).GetSchema("Columns");

			return new List<ColumnInfo>();
		}

		protected override List<ForeingKeyInfo> GetForeignKeys(DataConnection dataConnection)
		{
			var fks = ((DbConnection)dataConnection.Connection).GetSchema("ForeignKeys");

			return new List<ForeingKeyInfo>();
		}
	}
}
