using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	using Model;

	[TestFixture]
	public class Issue533Tests : TestBase
	{

		public class MyString
		{
			public string Value;
		}


		[Table(Name = "Person")]
		public class Entity533
		{
			[SequenceName(ProviderName.Firebird, "PersonID")]
			[Column("PersonID"), Identity, PrimaryKey]
			public int      ID         { get; set; }

			[Column]public Gender   Gender     { get; set; }
			[Column]public MyString FirstName  { get; set; }
			[Column]public MyString MiddleName { get; set; }
			[Column]public MyString LastName   { get; set; }
		}

		[Test]
		public void Issue533Test([DataSources] string context)
		{
			var ms = new MappingSchema();
			ms.SetConverter<MyString, string>((obj) =>
			{
				if (obj == null) return null;
				                 return obj.Value;
			});
			ms.SetConverter<MyString, DataParameter>((obj) =>
			{
				if (obj == null) return new DataParameter {                    DataType = DataType.NVarChar };
				                 return new DataParameter { Value = obj.Value, DataType = DataType.NVarChar };
			});
			ms.SetConverter<string, MyString>((txt) =>
			{
				if (string.IsNullOrEmpty(txt)) return null;
				                               return new MyString { Value = txt };
			});

			using (var db = GetDataContext(context, ms))
			using (new DeletePerson(db))
			{
				var obj = new Entity533
				{
					FirstName = new MyString { Value = "FirstName533" },
					LastName  = new MyString { Value = "LastName533" },
				};

				var id1 = Convert.ToInt32(db.InsertWithIdentity(obj));

				var obj2 = db.GetTable<Entity533>().First(_ => _.ID == id1);

				Assert.IsNull  (obj2.MiddleName);
				Assert.AreEqual(obj.FirstName.Value, obj2.FirstName.Value);
			}
		}
	}
}
