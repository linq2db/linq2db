using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	sealed class SapHanaOdbcSchemaProvider : SapHanaSchemaProvider
	{
		protected override List<DataTypeInfo> GetDataTypes(DataConnection dataConnection)
		{
			var dts = dataConnection.OpenDbConnection().GetSchema("DataTypes");

			var dt = dts.AsEnumerable()
				.Where(x=> x["ProviderDbType"] != DBNull.Value)
				.Select(t => new DataTypeInfo
				{
					TypeName         = t.Field<string>("TypeName")!,
					DataType         = t.Field<string>("DataType")!,
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
						x.CreateFormat += string.Concat('(',
							string.Join(", ",
								Enumerable.Range(0, x.CreateParameters.Split(',').Length)
									.Select(i => string.Concat('{', i, '}'))),
							')');
					}
				}

				return x;
			}).ToList();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection,
			IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			return dataConnection.Query(rd =>
			{
				// IMPORTANT: reader calls must be ordered to support SequentialAccess
				var schema     = rd.GetString(0);
				var tableName  = rd.GetString(1);
				var indexName  = rd.GetString(2);
				var constraint = rd.IsDBNull(3) ? null : rd.GetString(3);

				if (constraint != "PRIMARY KEY")
					return null;

				var columnName = rd.GetString(4);
				var position   = rd.GetInt32(5);

				return new PrimaryKeyInfo
				{
					TableID        = string.Concat(schema, '.', tableName),
					ColumnName     = columnName,
					Ordinal        = position,
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
				.Where(x => x != null).ToList()!;
		}
	}
}
