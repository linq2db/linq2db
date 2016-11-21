using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
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
		new class Person2
		{
			[Identity] public int                       PersonID;
			[Column]   public Dictionary<string,string> FirstName;
			[Column]   public string                    LastName;
			[Column]   public string                    MiddleName;
			[Column]   public Gender                    Gender;
		}


		[Test, DataContextSource]
		public void Test(string context)
		{
			MappingSchema.Default.SetConverter<Dictionary<string,string>,string>(obj => obj == null ? null : obj.Keys.FirstOrDefault());
			MappingSchema.Default.SetConverter<Dictionary<string,string>,DataParameter>(obj => obj == null ? null : new DataParameter { Value = obj.Keys.FirstOrDefault(), DataType = DataType.NVarChar});
			MappingSchema.Default.SetConverter<string,Dictionary<string,string>>(txt => txt == null ? null : new Dictionary<string,string> { { txt, txt } });

			using (var db = GetDataContext(context))
			{
				var tbl = db.GetTable<Person>();

				tbl.Where(t => t.LastName == "456").Delete();

				db.Insert(new Person
				{
					FirstName  = new Dictionary<string,string>{ { "123", "123" } },
					LastName   = "456",
					MiddleName = "789",
					Gender     = "M",
				});

				var p = tbl.First(t => t.LastName == "456");

				Assert.That(p.FirstName.Keys.First(), Is.EqualTo("123"));

				tbl.Where(t => t.LastName == "456").Delete();
			}
		}

		private void TraceHelperGenderWasString(string s1, string s2, ref bool DataConnection)
		{
			
		}

		[Test, DataContextSource]
		public void TestEnumNoString(string context)
		{
			MappingSchema.Default.SetConverter<Dictionary<string, string>, string>(obj => obj == null ? null : obj.Keys.FirstOrDefault());
			MappingSchema.Default.SetConverter<Dictionary<string, string>, DataParameter>(obj => obj == null ? null : new DataParameter { Value = obj.Keys.FirstOrDefault(), DataType = DataType.NVarChar });
			MappingSchema.Default.SetConverter<string, Dictionary<string, string>>(txt => txt == null ? null : new Dictionary<string, string> { { txt, txt } });

			using (var db = GetDataContext(context))
			{
				var tbl = db.GetTable<Person2>();

				tbl.Where(t => t.LastName == "456").Delete();

				bool typeWasInteger = false;
				try
				{
					var handler = new Action<string, string>((s1, s2) => typeWasInteger = s1.Contains("Gender = 0") ? true : typeWasInteger);
					DataConnection.WriteTraceLine += handler;
					db.Insert(new Person2
					{
						FirstName = new Dictionary<string, string> {{"123", "123"}},
						LastName = "456",
						MiddleName = "789",
						Gender = Gender.M
					});
					DataConnection.WriteTraceLine -= handler;
				}
				catch(Exception) //Inser may fail cause of constraint
				{ }

				Assert.That(typeWasInteger, Is.True);

				tbl.Where(t => t.LastName == "456").Delete();
			}
		}

		[Test, DataContextSource]
		public void TestEnumString(string context)
		{
			MappingSchema.Default.SetConverter<Dictionary<string, string>, string>(obj => obj == null ? null : obj.Keys.FirstOrDefault());
			MappingSchema.Default.SetConverter<Dictionary<string, string>, DataParameter>(obj => obj == null ? null : new DataParameter { Value = obj.Keys.FirstOrDefault(), DataType = DataType.NVarChar });
			MappingSchema.Default.SetConverter<string, Dictionary<string, string>>(txt => txt == null ? null : new Dictionary<string, string> { { txt, txt } });

			MappingSchema.Default.SetConverter<Gender, string>((obj) =>	{ return obj.ToString(); });
			MappingSchema.Default.SetConverter<Gender, DataParameter>((obj) => { return new DataParameter { Value = obj.ToString() }; });
			MappingSchema.Default.SetConverter<string, Gender>((txt) => { return (Gender)Enum.Parse(typeof(Gender), txt); });


			using (var db = GetDataContext(context))
			{
				var tbl = db.GetTable<Person2>();

				tbl.Where(t => t.LastName == "456").Delete();

				bool typeWasInteger = false;
				var handler = new Action<string, string>((s1, s2) => typeWasInteger = s1.Contains("Gender = 0") ? true : typeWasInteger);
				DataConnection.WriteTraceLine += handler;
				db.Insert(new Person2
				{
					FirstName = new Dictionary<string, string> { { "123", "123" } },
					LastName = "456",
					MiddleName = "789",
					Gender = Gender.M
				});
				DataConnection.WriteTraceLine -= handler;

				var p = tbl.First(t => t.LastName == "456");

				Assert.That(typeWasInteger, Is.False);

				tbl.Where(t => t.LastName == "456").Delete();
			}
		}
	}
}
