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
		new class Person
		{
			[Identity] public int                       PersonID;
			[Column]   public Dictionary<string,string> FirstName;
			[Column]   public string                    LastName;
			[Column]   public string                    MiddleName;
			[Column]   public string                    Gender;
		}

		public enum Gender
		{
			M,
			F
		}

		[Table("Person")]
		class Person2
		{
			[Identity] public int                       PersonID;
			[Column]   public Dictionary<string,string> FirstName;
			[Column]   public string                    LastName;
			[Column]   public string                    MiddleName;
			[Column]   public Gender                    Gender;
		}

		[Table("Person")]
		class PurePerson
		{
			[Identity] public int                       PersonID;
			[Column]   public string                    FirstName;
			[Column]   public string                    LastName;
			[Column]   public string                    MiddleName;
			[Column]   public string                    Gender;
		}

		[Test]
		public void Test([DataSources] string context)
		{
			MappingSchema.Default.SetConverter<Dictionary<string,string>, string>       (obj => obj == null ? null : obj.Keys.FirstOrDefault());
			MappingSchema.Default.SetConverter<Dictionary<string,string>, DataParameter>(obj => obj == null ? null : new DataParameter { Value = obj.Keys.FirstOrDefault(), DataType = DataType.NVarChar});
			MappingSchema.Default.SetConverter<string, Dictionary<string,string>>       (txt => txt == null ? null : new Dictionary<string,string> { { txt, txt } });

			using (var db = GetDataContext(context))
			{

				var id = Convert.ToInt32(db.InsertWithIdentity(new Person
				{
					FirstName  = new Dictionary<string,string>{ { "123", "123" } },
					LastName   = "456",
					MiddleName = "789",
					Gender     = "M",
				}));

				var p1 = db.GetTable<Person>()    .First(t => t.PersonID == id);
				var p2 = db.GetTable<PurePerson>().First(t => t.PersonID == id);

				Assert.That(p1.FirstName.Keys.First(), Is.EqualTo("123"));
				Assert.That(p2.FirstName,              Is.EqualTo("123"));

				db.Delete(p1);
			}
		}

		[ActiveIssue("What is this test???")]
		[Test]
		public void TestFail([IncludeDataSources(true, ProviderName.SQLiteClassic, ProviderName.SQLiteMS)]
			string context)
		{
			try
			{
				TestEnumString(context, ms => { });
				Assert.Fail("Value constraint expected");
			}
			catch (Exception)
			{
				//
			}
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
				ms.SetConverter<Gender, DataParameter>(obj => new DataParameter { Value = obj.ToString() });
				ms.SetConverter<string, Gender>       (txt => (Gender)Enum.Parse(typeof(Gender), txt));
			});
		}

		public void TestEnumString(string context, Action<MappingSchema> initMappingSchema)
		{
			var ms = new MappingSchema();
			ms.SetConverter<Dictionary<string, string>, string>       (obj => obj == null ? null : obj.Keys.FirstOrDefault());
			ms.SetConverter<Dictionary<string, string>, DataParameter>(obj => obj == null ? null : new DataParameter { Value = obj.Keys.FirstOrDefault(), DataType = DataType.NVarChar });
			ms.SetConverter<string, Dictionary<string, string>>       (txt => txt == null ? null : new Dictionary<string, string> { { txt, txt } });

			initMappingSchema(ms);

			using (var db = GetDataContext(context, ms))
			{
				var id = Convert.ToInt32(db.InsertWithIdentity(new Person2
				{
					FirstName  = new Dictionary<string, string> { { "123", "123" } },
					LastName   = "456",
					MiddleName = "789",
					Gender     = Gender.M
				}));

				var p = db.GetTable<PurePerson>().First(t => t.PersonID == id);

				Assert.AreEqual(Gender.M.ToString(), p.Gender);
				Assert.AreEqual("123",               p.FirstName);

				db.Delete(p);
			}
		}
	}
}
