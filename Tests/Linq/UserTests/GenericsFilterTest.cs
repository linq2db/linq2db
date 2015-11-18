using System;
using System.Linq.Expressions;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	internal class GenericsFilterTest
	{
		[Test]
		public void WhenPredicateFactoryIsGeneric()
		{
			var predicate = ById<Firm>(0);
			Assert.DoesNotThrow(() => CheckPredicate(predicate));
		}

		[Test]
		public void WhenPredicateFactoryIsNotGeneric()
		{
			var predicate = ById(0);
			Assert.DoesNotThrow(() => CheckPredicate(predicate));
		}

		private Expression<Func<T, bool>> ById<T>(int foobar)
			where T : class, IIdentifiable
		{
			return identifiable => identifiable.Id == foobar;
		}

		private Expression<Func<Firm, bool>> ById(int foobar)
		{
			return identifiable => identifiable.Id == foobar;
		}

		private void CheckPredicate(Expression<Func<Firm, bool>> predicate)
		{
			using (var db = new DataConnection(ProviderName.SQLite, "Data Source=:memory:;Version=3;New=True;"))
			{
				db.CreateTable<TypeA>();
				db.CreateTable<TypeB>();

				var query = db.GetTable<TypeA>()
					.Select(a => new Firm {Id = a.Id, Value = db.GetTable<TypeB>().Select(b => b.Id).FirstOrDefault()});

				query.Where(predicate).GetEnumerator();
			}
		}

		private interface IIdentifiable
		{
			int Id { get; set; }
		}

		private class Firm : IIdentifiable
		{
			public int Id { get; set; }
			public int Value { get; set; }
		}

		private class TypeA
		{
			public int Id { get; set; }
		}

		private class TypeB
		{
			public int Id { get; set; }
		}
	}
}
