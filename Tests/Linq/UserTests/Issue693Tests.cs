using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue693Tests : TestBase
	{

		[Table(Name = "Person")]
		public class Entity533
		{
			[SequenceName(ProviderName.Firebird, "PersonID")]
			[Column("PersonID", Configuration = ProviderName.ClickHouse)]
			[Column("PersonID", IsIdentity = true)]
			[PrimaryKey]
			        public int     ID         { get; set; }

			[Column]public Gender  Gender     { get; set; }
			[Column]public string  FirstName  { get; set; } = null!;
			[DataType(DataType.NVarChar, Configuration = ProviderName.Sybase)]
			[Column]public Test?   MiddleName { get; set; }
			[Column]public string  LastName   { get; set; } = null!;
		}

		public enum Test
		{
			A
		}

		[Test]
		public void Issue693Test([DataSources] string context)
		{
			ResetPersonIdentity(context);

			var ms = new MappingSchema();

			ms.SetConverter<Test?, string?>((obj) =>
			{
				if (obj != null)
					return obj.ToString();
				return null;
			});

			ms.SetConverter<Test?,DataParameter>((obj) =>
			{
				if (obj != null)
					return new DataParameter { Value = obj.ToString(), DataType = DataType.NVarChar };
				return new DataParameter { Value = DBNull.Value };
			});

			ms.SetConverter<string, Test?>((txt) =>
			{
				if (string.IsNullOrEmpty(txt))
					return null;
				return (Test?)Enum.Parse(typeof(Test), txt, true);
			});

			using (var db = GetDataContext(context, ms))
			using (new RestoreBaseTables(db))
			{
				var obj = new Entity533
				{
					FirstName  = "a",
					MiddleName = Test.A,
					LastName   = "b",
					Gender     = Gender.Male
				};

				int id1;
				if (context.IsAnyOf(TestProvName.AllClickHouse))
				{
					obj.ID = id1 = 100;
					db.Insert(obj);
				}
				else
					id1 = db.InsertWithInt32Identity(obj);

				var obj2 = new Entity533
				{
					FirstName  = "c",
					MiddleName = null,
					LastName   = "d",
					Gender     = Gender.Male
				};

				int id2;
				if (context.IsAnyOf(TestProvName.AllClickHouse))
				{
					obj2.ID = id2 = 101;
					db.Insert(obj2);
				}
				else
					id2 = db.InsertWithInt32Identity(obj2);

				var obj3 = db.GetTable<Entity533>().First(_ => _.ID == id1);
				var obj4 = db.GetTable<Entity533>().First(_ => _.ID == id2);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(obj4.MiddleName, Is.Null);
					Assert.That(obj3.MiddleName, Is.Not.Null);
				}
			}
		}
	}
}
