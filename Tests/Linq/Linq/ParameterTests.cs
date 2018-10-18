using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

#if !NETSTANDARD1_6 && !NETSTANDARD2_0 && !TRAVIS
using Tests.FSharp.Models;
#else
using Tests.Model;
#endif

namespace Tests.Linq
{
	[TestFixture]
	public class ParameterTests : TestBase
	{
		[Test, DataContextSource]
		public void InlineParameter(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.InlineParameters = true;

				var id = 1;

				var parent1 = db.Parent.FirstOrDefault(p => p.ParentID == id);
				id++;
				var parent2 = db.Parent.FirstOrDefault(p => p.ParentID == id);

				Assert.That(parent1.ParentID, Is.Not.EqualTo(parent2.ParentID));
			}
		}

		[Test, DataContextSource]
		public void TestQueryCacheWithNullParameters(string context)
		{
			using (var db = GetDataContext(context))
			{
				int? id = null;
				Assert.AreEqual(0, db.Person.Where(_ => _.ID == id).Count());

				id = 1;
				Assert.AreEqual(1, db.Person.Where(_ => _.ID == id).Count());
			}
		}

		[Test, DataContextSource(ProviderName.SqlCe, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.PostgreSQL, ProviderName.Informix, ProviderName.DB2, ProviderName.SapHana)]
		public void CharAsSqlParameter1(string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "0 \x0 ' 0";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test, DataContextSource(ProviderName.SqlCe, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.PostgreSQL, ProviderName.Informix, ProviderName.Informix, ProviderName.DB2, ProviderName.SapHana)]
		public void CharAsSqlParameter2(string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "\x0 \x0 ' \x0";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test, DataContextSource(ProviderName.SqlCe, ProviderName.PostgreSQL, ProviderName.Informix, ProviderName.Informix, ProviderName.DB2, ProviderName.SQLiteMS, ProviderName.SapHana)]
		public void CharAsSqlParameter3(string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "\x0";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test, DataContextSource]
		public void CharAsSqlParameter4(string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "\x1-\x2-\x3";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test, DataContextSource(ProviderName.PostgreSQL, ProviderName.Informix, ProviderName.Informix, ProviderName.DB2)]
		public void CharAsSqlParameter5(string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = '\x0';
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test, DataContextSource(false)]
		public void SqlStringParameter(string context)
		{
			using (var db = new DataConnection(context))
			{
				var p = "John";
				var person1 = db.GetTable<Person>().Where(t => t.FirstName == p).Single();

				p = "Tester";
				var person2 = db.GetTable<Person>().Where(t => t.FirstName == p).Single();

				Assert.That(person1.FirstName, Is.EqualTo("John"));
				Assert.That(person2.FirstName, Is.EqualTo("Tester"));
			}
		}

		// Excluded providers inline such parameter
		[Test, DataContextSource(false, ProviderName.DB2, ProviderName.DB2LUW, ProviderName.DB2zOS, ProviderName.Informix)]
		public void ExposeSqlStringParameter(string context)
		{
			using (var db = new DataConnection(context))
			{
				var p   = "abc";
				var sql = db.GetTable<Person>().Where(t => t.FirstName == p).ToString();

				Console.WriteLine(sql);

				Assert.That(sql, Contains.Substring("(3)").Or.Contains("(4000)"));
			}
		}

		class AllTypes
		{
			public decimal DecimalDataType;
			public byte[]  BinaryDataType;
		}

		// Excluded providers inline such parameter
		[Test, DataContextSource(false, ProviderName.DB2, ProviderName.DB2LUW, ProviderName.DB2zOS, ProviderName.Informix)]
		public void ExposeSqlDecimalParameter(string context)
		{
			using (var db = new DataConnection(context))
			{
				var p   = 123.456m;
				var sql = db.GetTable<AllTypes>().Where(t => t.DecimalDataType == p).ToString();

				Console.WriteLine(sql);

				Assert.That(sql, Contains.Substring("(6,3)"));
			}
		}

		// DB2: see DB2SqlOptimizer.SetQueryParameter - binary parameters inlined for DB2
		[Test, DataContextSource(false, ProviderName.DB2)]
		public void ExposeSqlBinaryParameter(string context)
		{
			using (var db = new DataConnection(context))
			{
				var p   = new byte[] { 0, 1, 2 };
				var sql = db.GetTable<AllTypes>().Where(t => t.BinaryDataType == p).ToString();

				Console.WriteLine(sql);

				Assert.That(sql, Contains.Substring("(3)").Or.Contains("Blob"));
			}
		}

		[Test, DataContextSource]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var dt = DateTime.Now;

				if (context.Contains("Informix"))
					dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);

				var list = db.Types.Where(t => t.DateTimeValue == Sql.ToSql(dt)).ToList();
			}
		}

		[Test, DataContextSource]
		public void Test2(string context)
		{
			using (var db = GetDataContext(context))
			{
				int id1 = 1, id2 = 10000;

				var parent1 = db.Parent.FirstOrDefault(p => p.ParentID == id1 || p.ParentID >= id1 || p.ParentID >= id2);
				id1++;
				var parent2 = db.Parent.FirstOrDefault(p => p.ParentID == id1 || p.ParentID >= id1 || p.ParentID >= id2);

				Assert.That(parent1.ParentID, Is.Not.EqualTo(parent2.ParentID));
			}
		}
	}
}
