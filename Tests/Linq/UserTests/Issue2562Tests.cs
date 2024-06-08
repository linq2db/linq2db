using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Linq;
using NUnit.Framework;

namespace Tests.UserTests
{
	public static class FBExtensions
	{
		[Sql.Extension("LIST({expr}, {splitter})", TokenName = "function", PreferServerSide = true)]
		public static string FbList<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr, [ExprParameter] string splitter)
		{
			throw new LinqException($"'{nameof(FbList)}' is server-side method.");
		}

		[Sql.Extension("LIST({expr})", TokenName = "function",  PreferServerSide = true)]
		public static string FbList<T>(this Sql.ISqlExtension? ext, [ExprParameter] T expr)
		{
			throw new LinqException($"'{nameof(FbList)}' is server-side method.");
		}
	}
	
	[TestFixture]
	public class Issue2562Tests : TestBase
	{
		public class Person2562
		{
			public int CardTypeId { get; set; }
			public string? CardNumber { get; set; }

			[ExpressionMethod(nameof(Person_LicPlates), IsColumn = true)]
			public string? Lics { get; set; }

			public static Expression<Func<Person2562, IDataContext, string?>> Person_LicPlates()
			{
				return (person, db) => db.GetTable<ExternalId2562>()
					.Where(x => x.CardTypeId == person.CardTypeId && x.CardNumber == person.CardNumber && x.TypeId == 2)
					.Select(x => Sql.Ext.FbList(x.Id)).FirstOrDefault();
			}
		}

		public class ExternalId2562
		{
			public int CardTypeId { get; set; }
			public string CardNumber { get; set; } = null!;
			public int TypeId { get; set; }
			public string Id { get; set; } = null!;
		}


		[Test]
		public void DynamicColumn([IncludeDataSources(TestProvName.AllFirebird)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<Person2562>())
			using (db.CreateLocalTable<ExternalId2562>())
			{
				var actual = table.ToArray();

				var expected = db.GetTable<Person2562>().Select(person => new Person2562()
				{
					CardNumber = person.CardNumber,
					CardTypeId = person.CardTypeId,
					Lics = db.GetTable<ExternalId2562>()
						.Where(x => x.CardTypeId == person.CardTypeId && x.CardNumber == person.CardNumber &&
						            x.TypeId == 2)
						.Select(x => Sql.Ext.FbList(x.Id)).FirstOrDefault()
				}).ToArray();
			}
		}
	}
}
