﻿using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Access;

using NUnit.Framework;

namespace Tests.UserTests
{
	// https://github.com/linq2db/linq2db.LINQPad/issues/10
	[TestFixture]
	[SkipCI]
	public class Issue0010Tests : TestBase
	{
		[Test, SkipCategory("Access.12")]
		public void TestOleDb([IncludeDataSources(ProviderName.Access)] string context)
		{
			using (var db = new DataConnection(new AccessOleDbDataProvider(), "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=Database\\issue_10_linqpad.accdb;"))
			{
				var schemaProvider = db.DataProvider.GetSchemaProvider();

				// call twice to ensure connection is still in good shape after call
				var schema = schemaProvider.GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));
				schema     = schemaProvider.GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));

				// and query known table to be completely sure connection is not broken
				db.Execute("SELECT * FROM CLONECODE");

				// all returned primary keys are defined on system/access tables
				Assert.True(schema.Tables.Any(t => t.Columns.Any(c => c.IsPrimaryKey)));
			}
		}

		[Test, SkipCategory("Access.12")]
		public void TestOdbc([IncludeDataSources(ProviderName.AccessOdbc)] string context)
		{
			using (var db = new DataConnection(new AccessODBCDataProvider(), "Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=Database\\issue_10_linqpad.accdb;"))
			{
				var schemaProvider = db.DataProvider.GetSchemaProvider();

				// call twice to ensure connection is still in good shape after call
				var schema = schemaProvider.GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));
				schema = schemaProvider.GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));

				// and query known table to be completely sure connection is not broken
				db.Execute("SELECT * FROM CLONECODE");

				// PKs not available from ODBC
				// all returned primary keys are defined on system/access tables
				// Assert.True(schema.Tables.Any(t => t.Columns.Any(c => c.IsPrimaryKey)));
			}
		}
	}
}
