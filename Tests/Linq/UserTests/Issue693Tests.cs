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
	public class Issue693Tests : TestBase
	{

		[Table(Name = "Person")]
		public class Entity533
		{
			[SequenceName(ProviderName.Firebird, "PersonID")]
			[Column("PersonID"), Identity, PrimaryKey]
			        public int     ID         { get; set; }

			[Column]public Gender  Gender     { get; set; }
			[Column]public string  FirstName  { get; set; }
			[DataType(DataType.NVarChar, Configuration = ProviderName.Sybase)]
			[Column]public Test?   MiddleName { get; set; }
			[Column]public string  LastName   { get; set; }
		}

		public enum Test
		{
			A
		}

		[Test, DataContextSource]
		public void Issue693Test(string context)
		{
			var ms = new MappingSchema();

			ms.SetConverter<Test?, string>((obj) =>
			{
				if (obj != null)
					return obj.ToString();
				return null;
			});

			ms.SetConverter<Test?,DataParameter>((obj) =>
			{
				if (obj != null)
					return new DataParameter { Value = obj.ToString() };
				return new DataParameter { Value = DBNull.Value };
			});

			ms.SetConverter<string, Test?>((txt) =>
			{
				if (string.IsNullOrEmpty(txt))
					return null;
				return (Test?)Enum.Parse(typeof(Test), txt, true);
			});


			using (var db = GetDataContext(context, ms))
			using (new DeletePerson(db))
			{
				var obj = new Entity533
				{
					FirstName  = "a",
					MiddleName = Test.A,
					LastName   = "b",
					Gender     = Gender.Male
				};

				var id1 = Convert.ToInt32(db.InsertWithIdentity(obj));

				var obj2 = new Entity533
				{
					FirstName  = "c",
					MiddleName = null,
					LastName   = "d",
					Gender     = Gender.Male
				};

				var id2 = Convert.ToInt32(db.InsertWithIdentity(obj2));

				var obj3 = db.GetTable<Entity533>().First(_ => _.ID == id1);
				var obj4 = db.GetTable<Entity533>().First(_ => _.ID == id2);

				Assert.IsNull (obj4.MiddleName);
				Assert.NotNull(obj3.MiddleName);
			}
		}
	}
}
