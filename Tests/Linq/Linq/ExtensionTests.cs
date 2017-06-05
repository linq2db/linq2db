﻿using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.SqlQuery;
	using Model;

	[TestFixture]
	public class ExtensionTests : TestBase
	{
		public class ParenTable
		{
			public int  ParentID;
			public int? Value1;
		}

		[Sql.Function("DB_NAME", ServerSideOnly = true)]
		static string DbName()
		{
			throw new InvalidOperationException();
		}

		private static string GetDatabaseName(ITestDataContext db)
		{
			return db.Types.Select(_ => DbName()).First();
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void TableName(string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<ParenTable>().TableName("Parent").ToList();
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void DatabaseName(string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<Parent>().DatabaseName(GetDatabaseName(db)).ToList();
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void OwnerName(string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<Parent>().SchemaName("dbo").ToList();
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void AllNames(string context)
		{
			using (var db = GetDataContext(context))
				db.GetTable<ParenTable>()
					.DatabaseName(GetDatabaseName(db))
					.SchemaName("dbo")
					.TableName("Parent")
					.ToList();
		}
	}
}
