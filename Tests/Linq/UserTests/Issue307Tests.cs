using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue307Tests : TestBase
	{

		[Table(Name = "Person")]
		public class Entity307
		{
			private Entity307()
			{
			}

			[Column("PersonID", Configuration = ProviderName.ClickHouse)]
			[Column("PersonID", IsIdentity = true)]
			[PrimaryKey]
			public int ID { get; set; }

			[Column]
			public Gender Gender { get; set; }

			[Column]
			public string FirstName { get; private set; } = null!;

			[Column]
			public string MiddleName { get; set; } = null!;

			[Column]
			public string? LastName { get; set; }

			public void SetFirstName(string firstName)
			{
				FirstName = firstName;
			}

			public static Entity307 Create()
			{
				return new Entity307();
			}
		}

		[Test]
		public void Issue307Test([DataSources] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var obj = Entity307.Create();
				obj.SetFirstName("FirstName307");
				obj.LastName = "LastName307";

				int id;
				if (context.IsAnyOf(TestProvName.AllClickHouse))
				{
					obj.ID = id = 100;
					db.Insert(obj);
				}
				else
					id = db.InsertWithInt32Identity(obj);

				var obj2 = db.GetTable<Entity307>().First(_ => _.ID == id);

				Assert.Multiple(() =>
				{
					Assert.That(obj2.MiddleName, Is.Null);
					Assert.That(obj2.FirstName, Is.EqualTo(obj.FirstName));
				});
			}
		}
	}
}
