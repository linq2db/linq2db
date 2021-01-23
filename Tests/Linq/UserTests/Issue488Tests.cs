using System;
using System.Data;
using System.Data.Common;
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
			public Binary? BinaryValue;
			public short SmallIntValue;
		}

		[Test]
		public void Test1([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				DbParameter[]? parameters = null!;
				((DataConnection)db).OnCommandInitialized += args =>
				{
					parameters = args.Command.Parameters.Cast<DbParameter>().ToArray();
				};

				var date = TestData.Date;
				var q = (from t1 in db.GetTable<LinqDataTypes>()
					join t2 in db.GetTable<LinqDataTypes>() on t1.ID equals t2.ID
					where t2.DateTimeValue == date
					select t2);

				var _ = q.FirstOrDefault();

				var dc = (DataConnection)db;
				Assert.AreEqual(2, parameters.Length);
				Assert.AreEqual(1, parameters.Count(p => p.DbType == DbType.Date));
			}
		}

		[Test]
		public void Test2([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				DbParameter[]? parameters = null!;
				((DataConnection)db).OnCommandInitialized += args =>
				{
					parameters = args.Command.Parameters.Cast<DbParameter>().ToArray();
				};

				var date = TestData.Date;
				var q = (from t1 in db.GetTable<LinqDataTypes>()
					where t1.DateTimeValue == date
					select t1);

				var _ = q.FirstOrDefault();

				var dc = (DataConnection)db;
				Assert.AreEqual(2, parameters.Length);
				Assert.AreEqual(1, parameters.Count(p => p.DbType == DbType.Date));
			}
		}
	}
}
