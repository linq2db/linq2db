using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class ConverterInsertTests : TestBase
	{
		[Table]
		new sealed class Person
		{
			[Column(IsIdentity = false, Configuration = ProviderName.ClickHouse)]
			[Column(IsIdentity = true)]
			           public int                       PersonID;
			[Column]   public Dictionary<string,string> FirstName = null!;
			[Column]   public string                    LastName = null!;
			[Column]   public string?                   MiddleName;
			[Column]   public string                    Gender = null!;
		}

		public enum Gender
		{
			M,
			F
		}

		[Table("Person")]
		sealed class Person2
		{
			[Column(IsIdentity = false, Configuration = ProviderName.ClickHouse)]
			[Column(IsIdentity = true)]
			           public int                       PersonID;
			[Column]   public Dictionary<string,string> FirstName = null!;
			[Column]   public string                    LastName = null!;
			[Column]   public string?                   MiddleName;
			[Column]   public Gender                    Gender;
		}

		[Table("Person")]
		sealed class PurePerson
		{
			[Column(IsIdentity = false, Configuration = ProviderName.ClickHouse)]
			[Column(IsIdentity = true)]
			           public int     PersonID;
			[Column]   public string  FirstName = null!;
			[Column]   public string  LastName  = null!;
			[Column]   public string? MiddleName;
			[Column]   public string  Gender    = null!;
		}

		[Test]
		public void Test([DataSources] string context)
		{
			ResetPersonIdentity(context);

			MappingSchema.Default.SetConverter<Dictionary<string, string>?, string?>(obj => obj == null ? null : obj.Keys.FirstOrDefault());
			MappingSchema.Default.SetConverter<Dictionary<string, string>?, DataParameter?>(obj => obj == null ? null : new DataParameter { Value = obj.Keys.FirstOrDefault(), DataType = DataType.NVarChar });
			MappingSchema.Default.SetConverter<string?, Dictionary<string, string>?>(txt => txt == null ? null : new Dictionary<string, string> { { txt, txt } });

			using var db = GetDataContext(context);
			using var _ = new RestoreBaseTables(db);

			var person = new Person
			{
				FirstName  = new Dictionary<string,string>{ { "123", "123" } },
				LastName   = "456",
				MiddleName = "789",
				Gender     = "M",
			};

			int id;
			if (context.IsAnyOf(TestProvName.AllClickHouse))
			{
				person.PersonID = id = 100;
				db.Insert(person);
			}
			else
				id = Convert.ToInt32(db.InsertWithIdentity(person));

			var p1 = db.GetTable<Person>()    .First(t => t.PersonID == id);
			var p2 = db.GetTable<PurePerson>().First(t => t.PersonID == id);

			Assert.Multiple(() =>
			{
				Assert.That(p1.FirstName.Keys.First(), Is.EqualTo("123"));
				Assert.That(p2.FirstName, Is.EqualTo("123"));
			});
		}

		[Test]
		public void TestEnumDefaultType1([DataSources] string context)
		{
			TestEnumString(context, ms => ms.SetDefaultFromEnumType(typeof(Gender), typeof(string)));
		}

		[Test]
		public void TestEnumDefaultType2([DataSources] string context)
		{
			TestEnumString(context, ms => ms.SetDefaultFromEnumType(typeof(Enum), typeof(string)));
		}

		[Test]
		public void TestEnumConverter([DataSources] string context)
		{
			TestEnumString(context, ms =>
			{
				ms.SetConverter<Gender, string>       (obj => obj.ToString() );
				ms.SetConverter<Gender, DataParameter>(obj => new DataParameter { Value = obj.ToString(), DataType = DataType.NVarChar });
				ms.SetConverter<string, Gender>       (txt => (Gender)Enum.Parse(typeof(Gender), txt));
			});
		}

		private void TestEnumString(string context, Action<MappingSchema> initMappingSchema)
		{
			ResetPersonIdentity(context);

			var ms = new MappingSchema();
			ms.SetConverter<Dictionary<string, string>?, string?>(obj => obj == null ? null : obj.Keys.FirstOrDefault());
			ms.SetConverter<Dictionary<string, string>?, DataParameter?>(obj => obj == null ? null : new DataParameter { Value = obj.Keys.FirstOrDefault(), DataType = DataType.NVarChar });
			ms.SetConverter<string?, Dictionary<string, string>?>(txt => txt == null ? null : new Dictionary<string, string> { { txt, txt } });

			initMappingSchema(ms);

			using var db = GetDataContext(context, ms);
			using var _ = new RestoreBaseTables(db);

			var person = new Person2()
			{
				FirstName  = new Dictionary<string, string> { { "123", "123" } },
				LastName   = "456",
				MiddleName = "789",
				Gender     = Gender.M
			};

			int id;
			if (context.IsAnyOf(TestProvName.AllClickHouse))
			{
				person.PersonID = id = 100;
				db.Insert(person);
			}
			else
				id = Convert.ToInt32(db.InsertWithIdentity(person));

			var p = db.GetTable<PurePerson>().First(t => t.PersonID == id);

			Assert.Multiple(() =>
			{
				Assert.That(p.Gender, Is.EqualTo(Gender.M.ToString()));
				Assert.That(p.FirstName, Is.EqualTo("123"));
			});
		}
	}
}
