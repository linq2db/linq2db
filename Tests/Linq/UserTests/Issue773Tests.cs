using System;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Data;
	using LinqToDB.Mapping;
	using LinqToDB.SqlQuery;

	[TestFixture]
	public class Issue773Tests : TestBase
	{
		public static class SqlLite
		{
			class MatchBuilder : Sql.IExtensionCallBuilder
			{
				public void Build(Sql.ISqExtensionBuilder builder)
				{
					var method = (MethodInfo) builder.Member;
					var arg = method.GetGenericArguments().Single();

					builder.AddParameter("table_field", new SqlTable(builder.Mapping, arg));
				}
			}

			// ReSharper disable once UnusedTypeParameter
			[Sql.Extension("{table_field} match {match}", BuilderType = typeof(MatchBuilder), IsPredicate = true)]
			public static bool MatchFts<TEntity>([ExprParameter]string match)
			{
				throw new InvalidOperationException();
			}
		}

		[Table("dataFTS")]
		public partial class DtaFts
		{
			[Column] public long   Id        { get; set; }
			[Column] public string FirstName { get; set; }
			[Column] public string LastName  { get; set; }
			[Column] public string MidName   { get; set; }
		}

		[Test]
		public void TestAnonymous([IncludeDataSources(false, ProviderName.SQLite)] string context)
		{
			using (var db = new DataConnection(context))
			{
				db.Execute(@"
DROP TABLE IF EXISTS dataFTS;
CREATE VIRTUAL TABLE dataFTS USING fts4(`ID` INTEGER, `FirstName` TEXT, `LastName` TEXT, `MidName` TEXT )");

				try
				{
					var data = db.GetTable<DtaFts>()
						.Select(result =>
						new
						{
							result.FirstName,
							result.MidName,
							result.LastName,
						});

					var query = data.Where(arg => SqlLite.MatchFts<DtaFts>("John*"));
					Console.WriteLine(query.ToString());
					var _ = query.ToList();
				}
				finally
				{
					// cleanup
					db.Execute("DROP TABLE dataFTS");
				}
			}
		}

		[Test]
		public void TestDirect([IncludeDataSources(false, ProviderName.SQLite)] string context)
		{
			using (var db = new DataConnection(context))
			{
				db.Execute(@"
DROP TABLE IF EXISTS dataFTS;
CREATE VIRTUAL TABLE dataFTS USING fts4(`ID` INTEGER, `FirstName` TEXT, `LastName` TEXT, `MidName` TEXT )");

				try
				{
					var data = db.GetTable<DtaFts>()
						.Where(arg => SqlLite.MatchFts<DtaFts>("John*"))
						.Select(result =>
						new
						{
							result.FirstName,
							result.MidName,
							result.LastName,
						});

					var list = data.ToList(); // <=THROWS EXCEPTION

					Assert.AreEqual(0, list.Count);

					db.GetTable<DtaFts>().Insert(() => new DtaFts { FirstName = "JohnTheRipper" });
					db.GetTable<DtaFts>().Insert(() => new DtaFts { FirstName = "DoeJohn"       });

					list = data.ToList(); // <=THROWS EXCEPTION

					Assert.AreEqual(1, list.Count);
					Assert.AreEqual("JohnTheRipper", list[0].FirstName);
				}
				finally
				{
					// cleanup
					db.Execute("DROP TABLE dataFTS");
				}
			}
		}
	}
}
