using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class GenerateTests : TestBase
	{
		[Test]
		public void GeneratePredicate()
		{
			Expression<Func<Person,bool>> a = x => x.FirstName == "John";
			Expression<Func<Person,bool>> b = x => x.LastName  == "Pupkin";

			var bBody     = b.GetBody(a.Parameters[0]);
			var predicate = Expression.Lambda<Func<Person,bool>>(Expression.AndAlso(a.Body, bBody), a.Parameters[0]);

			using (var db = new TestDataConnection())
			{
				var q = db.Person.Where(predicate);
				var p = q.First();
			}
		}
	}
}
