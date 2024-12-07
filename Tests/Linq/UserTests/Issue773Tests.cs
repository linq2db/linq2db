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
			sealed class MatchBuilder : Sql.IExtensionCallBuilder
			{
				public void Build(Sql.ISqExtensionBuilder builder)
				{
					var method = (MethodInfo) builder.Member;
					var arg = method.GetGenericArguments().Single();

					builder.AddParameter("table_field", new SqlTable(builder.Mapping.GetEntityDescriptor(arg)));
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
			[Column] public long    Id        { get; set; }
			[Column] public string? FirstName { get; set; }
			[Column] public string? LastName  { get; set; }
			[Column] public string? MidName   { get; set; }
		}

		[Test]
		public void TestAnonymous([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataConnection(context))
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
					_ = query.ToList();
				}
				finally
				{
					// cleanup
					db.Execute("DROP TABLE dataFTS");
				}
			}
		}

		[Test]
		public void TestDirect([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataConnection(context))
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

					Assert.That(list, Is.Empty);

					db.GetTable<DtaFts>().Insert(() => new DtaFts { FirstName = "JohnTheRipper" });
					db.GetTable<DtaFts>().Insert(() => new DtaFts { FirstName = "DoeJohn"       });

					list = data.ToList(); // <=THROWS EXCEPTION

					Assert.That(list, Has.Count.EqualTo(1));
					Assert.That(list[0].FirstName, Is.EqualTo("JohnTheRipper"));
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
