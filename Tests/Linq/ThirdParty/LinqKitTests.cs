using System.Linq;

using LinqKit;

using NUnit.Framework;

using Tests.Model;

namespace Tests.ThirdParty
{
	[TestFixture]
	public class LinqKitTests : TestBase
	{
		[Test]
		public void Expandable([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var predicate = PredicateBuilder.New<Person>();

				var names = new[] { "John", "Doe" };

				foreach (var name in names)
					predicate = predicate.Or(i => LinqToDB.Sql.Like(i.FirstName, name));

				var items = db.Person.AsExpandable().Where(predicate);

				var query = db.Person.AsQueryable();
				query = query.Where(i => items.Any(o => o.ID == i.ID));

				var result = query.ToList();
			}
		}
	}
}
