using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.NitrosBase
{
	using LinqToDB.Data;
	using SchemaProvider;

	class NitrosBaseSchemaProvider : SchemaProviderBase
	{
		// TODO: implement schema load functionality using queries to schema tables or GetSchema API calls

		protected override List<ColumnInfo> GetColumns(DataConnection dataConnection, GetSchemaOptions options)
		{
			// TODO: implement
			throw new NotImplementedException();
		}

		protected override DataType GetDataType(string? dataType, string? columnType, long? length, int? prec, int? scale)
		{
			// TODO: implement
			throw new NotImplementedException();
		}

		protected override IReadOnlyCollection<ForeignKeyInfo> GetForeignKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// TODO: implement
			throw new NotImplementedException();
		}

		protected override IReadOnlyCollection<PrimaryKeyInfo> GetPrimaryKeys(DataConnection dataConnection, IEnumerable<TableSchema> tables, GetSchemaOptions options)
		{
			// TODO: implement
			throw new NotImplementedException();
		}

		protected override string? GetProviderSpecificTypeNamespace()
		{
			// TODO: implement
			throw new NotImplementedException();
		}

		protected override List<TableInfo> GetTables(DataConnection dataConnection, GetSchemaOptions options)
		{
			// TODO: implement
			throw new NotImplementedException();
		}
	}
}
