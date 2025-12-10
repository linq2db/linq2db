using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1554Tests : TestBase
	{
		[Table("Issue1554Table")]
		public class PersonCache
		{
			[PrimaryKey]
			public int       Id              { get; set; }
			[Column]
			public KeyTypes  ClaimedKeyType  { get; set; }
			[Column]
			public KeyTypes? ClaimedKeyTypeN { get; set; }

			public static PersonCache TestInstance = new PersonCache()
			{
				Id              = 0,
				ClaimedKeyType  = KeyTypes.RSA,
				ClaimedKeyTypeN = KeyTypes.RSA
			};
		}

		public class PersonCacheFluent
		{
			public int       Id              { get; set; }
			public KeyTypes  ClaimedKeyType  { get; set; }
			public KeyTypes? ClaimedKeyTypeN { get; set; }

			public static PersonCacheFluent TestInstance = new PersonCacheFluent()
			{
				Id              = 0,
				ClaimedKeyType  = KeyTypes.RSA,
				ClaimedKeyTypeN = KeyTypes.RSA
			};
		}

		public enum KeyTypes : byte
		{
			[MapValue("RSA")]
			RSA = 1,
			[MapValue("EC")]
			EC  = 2
		}

		public enum KeyTypes2
		{
			[MapValue("NOT_RSA")]
			RSA = 1,
			[MapValue("NOT_EC")]
			EC  = 2
		}

		[Test]
		public void TestUpdate1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<PersonCache>();
			db.Insert(PersonCache.TestInstance);

			var claimedKeyType = KeyTypes.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestUpdate2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<PersonCache>();
			db.Insert(PersonCache.TestInstance);

			object claimedKeyType = KeyTypes.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestUpdate3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<PersonCache>();
			db.Insert(PersonCache.TestInstance);

			var claimedKeyType = (KeyTypes)KeyTypes2.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestUpdate4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<PersonCache>();
			db.Insert(PersonCache.TestInstance);

			object claimedKeyType = (KeyTypes)KeyTypes2.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestUpdate5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<PersonCache>();
			db.Insert(PersonCache.TestInstance);

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, (KeyTypes)KeyTypes2.EC)
				.Set(p => p.ClaimedKeyTypeN, (KeyTypes)KeyTypes2.EC)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestInsert1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<PersonCache>();
			var claimedKeyType = KeyTypes.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestInsert2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<PersonCache>();
			object claimedKeyType = KeyTypes.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestInsert3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<PersonCache>();
			var claimedKeyType = (KeyTypes)KeyTypes2.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestInsert4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<PersonCache>();
			object claimedKeyType = (KeyTypes)KeyTypes2.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestInsert5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<PersonCache>();
			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, (KeyTypes)KeyTypes2.EC)
				.Value(p => p.ClaimedKeyTypeN, (KeyTypes)KeyTypes2.EC)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestFluentUpdate1([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentMapping());
			using var table = db.CreateLocalTable<PersonCacheFluent>();
			db.Insert(PersonCacheFluent.TestInstance);

			var claimedKeyType = KeyTypes.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestFluentUpdate2([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentMapping());
			using var table = db.CreateLocalTable<PersonCacheFluent>();
			db.Insert(PersonCacheFluent.TestInstance);

			object claimedKeyType = KeyTypes.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestFluentUpdate3([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentMapping());
			using var table = db.CreateLocalTable<PersonCacheFluent>();
			db.Insert(PersonCacheFluent.TestInstance);

			var claimedKeyType = (KeyTypes)KeyTypes2.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestFluentUpdate4([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentMapping());
			using var table = db.CreateLocalTable<PersonCacheFluent>();
			db.Insert(PersonCacheFluent.TestInstance);

			object claimedKeyType = (KeyTypes)KeyTypes2.EC;

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, claimedKeyType)
				.Set(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestFluentUpdate5([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentMapping());
			using var table = db.CreateLocalTable<PersonCacheFluent>();
			db.Insert(PersonCacheFluent.TestInstance);

			table.Where(p => p.Id == 0).AsUpdatable()
				.Set(p => p.ClaimedKeyType, (KeyTypes)KeyTypes2.EC)
				.Set(p => p.ClaimedKeyTypeN, (KeyTypes)KeyTypes2.EC)
				.Update();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestFluentInsert1([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentMapping());
			using var table = db.CreateLocalTable<PersonCacheFluent>();
			var claimedKeyType = KeyTypes.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestFluentInsert2([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentMapping());
			using var table = db.CreateLocalTable<PersonCacheFluent>();
			object  claimedKeyType     = KeyTypes.EC;
			object? claimedKeyTypeNull = null;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyTypeNull)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.Null);
			}
		}

		[Test]
		public void TestFluentInsert3([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentMapping());
			using var table = db.CreateLocalTable<PersonCacheFluent>();
			var claimedKeyType = (KeyTypes)KeyTypes2.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestFluentInsert4([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentMapping());
			using var table = db.CreateLocalTable<PersonCacheFluent>();
			object claimedKeyType = (KeyTypes)KeyTypes2.EC;

			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, claimedKeyType)
				.Value(p => p.ClaimedKeyTypeN, claimedKeyType)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		[Test]
		public void TestFluentInsert6([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureFluentMapping());
			using var table = db.CreateLocalTable<PersonCacheFluent>();
			table.Value(p => p.Id, 0)
				.Value(p => p.ClaimedKeyType, (KeyTypes)KeyTypes2.EC)
				.Value(p => p.ClaimedKeyTypeN, (KeyTypes)KeyTypes2.EC)
				.Insert();

			var record = table.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(record.ClaimedKeyType, Is.EqualTo(KeyTypes.EC));
				Assert.That(record.ClaimedKeyTypeN, Is.EqualTo(KeyTypes.EC));
			}
		}

		private static MappingSchema ConfigureFluentMapping()
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<PersonCacheFluent>()
					.HasTableName("Issue1554FluentTable")
					.Property(p => p.Id)
						.IsPrimaryKey()
					.Property(p => p.ClaimedKeyType)
					.Property(p => p.ClaimedKeyTypeN)
				.Build();

			return ms;
		}
	}
}
