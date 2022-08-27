﻿using System.Data;
using System.Data.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue488Tests : TestBase
	{
		public class LinqDataTypes
		{
			public int ID;
			public decimal MoneyValue;
			[Column(DataType = DataType.Date)]public DateTime DateTimeValue;
			public bool BoolValue;
			public Guid GuidValue;
			public Binary? BinaryValue;
			public short SmallIntValue;
		}

		[Test]
		public void Test1([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var commandInterceptor = new SaveCommandInterceptor();
				db.AddInterceptor(commandInterceptor);

				var date = TestData.Date;
				var q = (from t1 in db.GetTable<LinqDataTypes>()
					join t2 in db.GetTable<LinqDataTypes>() on t1.ID equals t2.ID
					where t2.DateTimeValue == date
					select t2);

				var _ = q.FirstOrDefault();

				var dc = (DataConnection)db;
				Assert.AreEqual(2, commandInterceptor.Parameters.Length);
				Assert.AreEqual(1, commandInterceptor.Parameters.Count(p => p.DbType == DbType.Date));
			}
		}

		[Test]
		public void Test2([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var commandInterceptor = new SaveCommandInterceptor();
				db.AddInterceptor(commandInterceptor);

				var date = TestData.Date;
				var q = (from t1 in db.GetTable<LinqDataTypes>()
					where t1.DateTimeValue == date
					select t1);

				var _ = q.FirstOrDefault();

				var dc = (DataConnection)db;
				Assert.AreEqual(2, commandInterceptor.Parameters.Length);
				Assert.AreEqual(1, commandInterceptor.Parameters.Count(p => p.DbType == DbType.Date));
			}
		}
	}
}
