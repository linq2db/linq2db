using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	using Model;

	[TestFixture]
	public class Issue307Tests : TestBase
	{

		[Table(Name = "Person")]
		public class Entity307
		{
			private Entity307()
			{
			}

			[Column("PersonID"), Identity, PrimaryKey]
			public int ID { get; set; }

			[Column]
			public Gender Gender { get; set; }

			[Column]
			public String FirstName { get; private set; }

			[Column]
			public String MiddleName { get; set; }

			[Column]
			public String LastName { get; set; }

			public void SetFirstName(string firstName)
			{
				FirstName = firstName;
			}

			public static Entity307 Create()
			{
				return new Entity307();
			}
		}

		[Test, DataContextSource]
		public void Issue307Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var obj = Entity307.Create();
					obj.SetFirstName("FirstName307");
					obj.LastName = "LastName307";

					var id1 = Convert.ToInt32(db.InsertWithIdentity(obj));

					var obj2 = db.GetTable<Entity307>().First(_ => _.ID == id1);

					Assert.IsNull(obj2.MiddleName);
					Assert.AreEqual(obj.FirstName, obj2.FirstName);
				}
				finally
				{
					db.Person.Delete(_ => _.ID > MaxPersonID);
				}
			}
		}
	}
}
