using System;
using System.Linq;
using System.Linq.Expressions;

using NUnit.Framework;

using LinqToDB.Extensions;

namespace Data.Linq
{
	using Model;

	[TestFixture]
	public class GenerateTest : TestBase
	{
		[Test]
		public void GeneratePredicate()
		{
			Expression<Func<Person,bool>> a = x => x.FirstName == "John";
			Expression<Func<Person,bool>> b = x => x.LastName  == "Pupkin";

			var bBody     = b.Body.Transform(e => e == b.Parameters[0] ? a.Parameters[0] : e);
			var predicate = Expression.Lambda<Func<Person,bool>>(Expression.AndAlso(a.Body, bBody), a.Parameters[0]);

			using (var db = new TestDbManager())
			{
				var q = db.Person.Where(predicate);
				var p = q.First();
			}
		}
	}
}
