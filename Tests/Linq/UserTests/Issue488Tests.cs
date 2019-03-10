﻿using System;
using System.Data;
using System.Data.Linq;
using System.Linq;

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
			public Binary BinaryValue;
			public short SmallIntValue;
		}

		[Test]
		public void Test1([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var date = DateTime.Today;
				var q = (from t1 in db.GetTable<LinqDataTypes>()
					join t2 in db.GetTable<LinqDataTypes>() on t1.ID equals t2.ID
					where t2.DateTimeValue == date
					select t2);

				var _ = q.FirstOrDefault();

				Assert.AreEqual(1, ((DataConnection)db).Command.Parameters.Count);
				Assert.AreEqual(DbType.Date, ((IDbDataParameter) ((DataConnection)db).Command.Parameters[0]).DbType);
			}
		}

		[Test]
		public void Test2([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var date = DateTime.Today;
				var q = (from t1 in db.GetTable<LinqDataTypes>()
					where t1.DateTimeValue == date
					select t1);

				var _ = q.FirstOrDefault();

				Assert.AreEqual(1, ((DataConnection)db).Command.Parameters.Count);
				Assert.AreEqual(DbType.Date, ((IDbDataParameter) ((DataConnection)db).Command.Parameters[0]).DbType);
			}
		}
	}
}
