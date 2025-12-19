using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.DataProvider.SqlServer;
using LinqToDB.SchemaProvider;

using Microsoft.Data.SqlTypes;

using NUnit.Framework;

namespace Tests.SchemaProvider
{
	[TestFixture]
	public class SqlServerTests : TestBase
	{
		[Test]
		public void JsonDataTypeTest([IncludeDataSources(TestProvName.AllSqlServer2025Plus)] string context, [Values] bool preferProviderSpecificTypes)
		{
			using var db = (DataConnection)GetDataContext(context);

			var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions
			{
				GetProcedures = false,
				LoadTable = data => data.Name == "AllTypes",
				PreferProviderSpecificTypes = preferProviderSpecificTypes,
			});

			var table = schema.Tables[0];

			var jsonColumn = table.Columns.First(c => c.ColumnName == "jsonDataType");
			var type       = preferProviderSpecificTypes && db.DataProvider is SqlServerDataProvider { Adapter.SqlJsonType: not null } ? typeof(SqlJson) : typeof(string);

			Assert.That(jsonColumn.Length,     Is.Null);
			Assert.That(jsonColumn.SystemType, Is.EqualTo(type));
		}

		[Test]
		public void VectorDataTypeTest([IncludeDataSources(TestProvName.AllSqlServer2025Plus)] string context, [Values] bool preferProviderSpecificTypes)
		{
			using var db = (DataConnection)GetDataContext(context);

			var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions
			{
				GetProcedures = false,
				LoadTable = data => data.Name == "AllTypes",
				PreferProviderSpecificTypes = preferProviderSpecificTypes,
			});

			var table = schema.Tables[0];

			var jsonColumn = table.Columns.First(c => c.ColumnName == "vectorDataType");
			var type       = preferProviderSpecificTypes && db.DataProvider is SqlServerDataProvider { Adapter.SqlVectorType: not null } ? typeof(SqlVector<float>) : typeof(float[]);

			Assert.That(jsonColumn.Length,     Is.EqualTo(5));
			Assert.That(jsonColumn.SystemType, Is.EqualTo(type));
		}
	}
}
