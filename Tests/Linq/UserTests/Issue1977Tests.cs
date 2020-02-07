using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1977Tests : TestBase
	{
		public class Issue1977Table
		{
			public Guid firstField;
			public Guid secondField;

			public static Issue1977Table[] TestData { get; }
				= new[]
				{
					new Issue1977Table()
					{
						firstField = Guid.NewGuid(),
						secondField = Guid.NewGuid()
					}
				};
		}

		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db    = GetDataContext(context))
			using (var table = db.CreateLocalTable(Issue1977Table.TestData))
			{
				var itemsQuery = table
				.Select
				(
					f => new
					{
						oldStr = nameof(Issue1977Table)
						 + "/" + f.firstField
						 + "/" + f.secondField,
						newStr = Sql.AsSql(Sql.ConcatStrings("/",
							nameof(Issue1977Table),
							Sql.Convert<string, Guid>(f.firstField),
							Sql.Convert<string, Guid>(f.secondField)))
					}
				)
				.Select
				(
					f => new
					{
						equals = Sql.AsSql(f.oldStr == f.newStr)
					}
				);

				Assert.True(itemsQuery.ToArray().All(r => r.equals));
			}
		}
	}
}
