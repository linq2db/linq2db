using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class QueryHintsTests : TestBase
	{
		[Test, DataContextSource(ProviderName.Access, ProviderName.MySql, TestProvName.MariaDB)]
		public void Comment(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.QueryHints.Add("---");
				db.NextQueryHints.Add("----");

				var q = db.Parent.Select(p => p);

				var str = q.ToString();

				Console.WriteLine(str);

				Assert.That(str, Contains.Substring("---"));
				Assert.That(str, Contains.Substring("----"));

				var list = q.ToList();

				var ctx = db as DataConnection;

				if (ctx != null)
				{
					Assert.That(ctx.LastQuery, Contains.Substring("---"));
					Assert.That(ctx.LastQuery, Contains.Substring("----"));
				}

				str = q.ToString();

				Console.WriteLine(str);

				Assert.That(str, Contains.Substring("---"));
				Assert.That(str, Is.Not.Contains("----"));

				list = q.ToList();

				if (ctx != null)
				{
					Assert.That(ctx.LastQuery, Contains.Substring("---"));
					Assert.That(ctx.LastQuery, Is.Not.Contains("----"));
				}
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2012)]
		public void Option1(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.QueryHints.Add(SqlServerTools.Sql.OptionRecompile);

				var q = db.Parent.Select(p => p);

				var list = q.ToList();

				var ctx = db as DataConnection;

				if (ctx != null)
				{
					Assert.That(ctx.LastQuery, Contains.Substring("OPTION"));
				}

				list = q.ToList();

				if (ctx != null)
				{
					Assert.That(ctx.LastQuery, Contains.Substring("OPTION"));
				}
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2012)]
		public void Option2(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.NextQueryHints.Add(SqlServerTools.Sql.OptionRecompile);

				var q = db.Parent.Select(p => p);

				var list = q.ToList();

				var ctx = db as DataConnection;

				if (ctx != null)
				{
					Assert.That(ctx.LastQuery, Contains.Substring("OPTION"));
				}

				list = q.ToList();

				if (ctx != null)
				{
					Assert.That(ctx.LastQuery, Is.Not.Contains("OPTION"));
				}
			}
		}
	}
}
