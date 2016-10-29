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
#pragma warning disable 0649
			[Identity] public int                       PersonID;
			[Column]   public Dictionary<string,string> FirstName;
			[Column]   public string                    LastName;
			[Column]   public string                    MiddleName;
			[Column]   public string                    Gender;
#pragma warning restore 0649
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
	}
}
