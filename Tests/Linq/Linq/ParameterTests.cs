using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

#if !NOFSHARP
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
			using (var  db = GetDataContext(context))
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

		[Test, DataContextSource(ProviderName.SqlCe, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.PostgreSQL, ProviderName.Informix, ProviderName.DB2)]
		public void CharAsSqlParameter1(string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "0 \x0 ' 0";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test, DataContextSource(ProviderName.SqlCe, ProviderName.SQLite, TestProvName.SQLiteMs, ProviderName.PostgreSQL, ProviderName.Informix, ProviderName.Informix, ProviderName.DB2)]
		public void CharAsSqlParameter2(string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "\x0 \x0 ' \x0";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test, DataContextSource(ProviderName.SqlCe, ProviderName.PostgreSQL, ProviderName.Informix, ProviderName.Informix, ProviderName.DB2, TestProvName.SQLiteMs)]
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
		public void ExposeSqlStringParameter(string context)
		{
			using (var db = new DataConnection(context))
			{
				var p   = "abc";
				var sql = db.GetTable<Person>().Where(t => t.FirstName == p).ToString();

				Console.WriteLine(sql);

				Assert.That(sql, Contains.Substring("(3)"));
			}
		}

		class AllTypes
		{
			public decimal DecimalDataType;
			public byte[]  BinaryDataType;
		}

		[Test, DataContextSource(false)]
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

		[Test, DataContextSource(false)]
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
	}
}
