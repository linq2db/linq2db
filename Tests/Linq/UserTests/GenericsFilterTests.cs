using System;
using System.Linq.Expressions;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class GenericsFilterTests
	{
		[Test, TestBase.IncludeDataContextSource(ProviderName.SQLite, TestProvName.SQLiteMs)]
		public void WhenPredicateFactoryIsGeneric(string context)
		{
			var predicate = ById<Firm>(0);
			Assert.DoesNotThrow(() => CheckPredicate(context, predicate));
		}

		[Test, TestBase.IncludeDataContextSource(ProviderName.SQLite, TestProvName.SQLiteMs)]
		public void WhenPredicateFactoryIsNotGeneric(string context)
		{
			var predicate = ById(0);
			Assert.DoesNotThrow(() => CheckPredicate(context, predicate));
		}

		Expression<Func<T, bool>> ById<T>(int foobar)
			where T : class, IIdentifiable
		{
			return identifiable => identifiable.Id == foobar;
		}

		Expression<Func<Firm, bool>> ById(int foobar)
		{
			return identifiable => identifiable.Id == foobar;
		}

		void CheckPredicate(string context, Expression<Func<Firm, bool>> predicate)
		{
			using (var db = new DataConnection(context,
#if NETSTANDARD
				"Data Source=:memory:;"
#else
				"Data Source=:memory:;Version=3;New=True;"
#endif
				))
			{
				db.CreateTable<TypeA>();
				db.CreateTable<TypeB>();

				var query = db.GetTable<TypeA>()
					.Select(a => new Firm { Id = a.Id, Value = db.GetTable<TypeB>().Select(b => b.Id).FirstOrDefault() });

				query.Where(predicate).GetEnumerator();
			}
		}

		interface IIdentifiable
		{
			int Id { get; set; }
		}

		class Firm : IIdentifiable
		{
			public int Id { get; set; }
			public int Value { get; set; }
		}

		class TypeA
		{
			public int Id { get; set; }
		}

		class TypeB
		{
			public int Id { get; set; }
		}
	}
}
