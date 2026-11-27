using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public class CalculatedColumnTests : TestBase
	{
		[Table(Name="Person")]
		public class PersonCalculated
		{
			[Column, PrimaryKey,  Identity] public int     PersonID   { get; set; } // INTEGER
			[Column, NotNull              ] public string  FirstName  { get; set; } = null!;
			[Column, NotNull              ] public string  LastName   { get; set; } = null!;
			[Column,    Nullable          ] public string? MiddleName { get; set; } // VARCHAR(50)
			[Column, NotNull              ] public char    Gender     { get; set; } // CHARACTER(1)

			[ExpressionMethod(nameof(GetFullNameExpr), IsColumn = true)]
			public string FullName { get; set; } = null!;

			static Expression<Func<PersonCalculated, string>> GetFullNameExpr()
			{
				return p => p.LastName + ", " + p.FirstName;
			}

			[ExpressionMethod(nameof(GetAsSqlFullNameExpr), IsColumn = true)]
			public string AsSqlFullName { get; set; } = null!;

			static Expression<Func<PersonCalculated, string>> GetAsSqlFullNameExpr()
			{
				return p => Sql.AsSql(p.LastName + ", " + p.FirstName);
			}

			[ExpressionMethod(nameof(GetDoctorCountExpr), IsColumn = true)]
			public int DoctorCount { get; set; }

			static Expression<Func<Model.ITestDataContext,PersonCalculated,int>> GetDoctorCountExpr()
			{
				return (db,p) => db.Doctor.Count(d => d.PersonID == p.PersonID);
			}

			public static IEqualityComparer<PersonCalculated> Comparer = ComparerBuilder.GetEqualityComparer<PersonCalculated>();
		}

		[Table("Doctor")]
		public class DoctorCalculated
		{
			[Column, PrimaryKey, Identity] public int    PersonID { get; set; } // Long
			[Column(Length = 50), NotNull] public string Taxonomy { get; set; } = null!; // text(50)

			// Many association for test
			[Association(ThisKey = "PersonID", OtherKey = "PersonID", CanBeNull = false)]
			public IEnumerable<PersonCalculated> PersonDoctor { get; set; } = null!;
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void CalculatedColumnTest1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q = db.GetTable<PersonCalculated>().Where(i => i.FirstName != "John");
			var l = q.ToList();

			Assert.That(l, Is.Not.Empty);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(l[0].FullName, Is.Not.Null);
				Assert.That(l[0].AsSqlFullName, Is.Not.Null);
				Assert.That(l[0].FullName, Is.EqualTo(l[0].LastName + ", " + l[0].FirstName));
				Assert.That(l[0].AsSqlFullName, Is.EqualTo(l[0].LastName + ", " + l[0].FirstName));
			}
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void CalculatedColumnTest2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var list  = db.GetTable<PersonCalculated>().ToList();
			var query = db.GetTable<PersonCalculated>().Where(i => i.FullName != "Pupkin, John").ToList();

			Assert.That(list, Has.Count.Not.EqualTo(query.Count));

			AreEqual(
				list.Where(i => i.FullName != "Pupkin, John"),
				query,
				PersonCalculated.Comparer);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void CalculatedColumnTest3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q = db.GetTable<PersonCalculated>()
					.Where (i => i.FirstName != "John")
					.Select(t => new
					{
						cnt = db.Doctor.Count(d => d.PersonID == t.PersonID),
						t,
					});
			var l = q.ToList();

			Assert.That(l, Is.Not.Empty);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(l[0].t.FullName, Is.Not.Null);
				Assert.That(l[0].t.AsSqlFullName, Is.Not.Null);
				Assert.That(l[0].t.FullName, Is.EqualTo(l[0].t.LastName + ", " + l[0].t.FirstName));
				Assert.That(l[0].t.AsSqlFullName, Is.EqualTo(l[0].t.LastName + ", " + l[0].t.FirstName));
				Assert.That(l[0].t.DoctorCount, Is.EqualTo(l[0].cnt));
			}
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void CalculatedColumnTest4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q =
					db.GetTable<DoctorCalculated>()
						.SelectMany(d => d.PersonDoctor);
			var l = q.ToList();

			Assert.That(l, Is.Not.Empty);
			Assert.That(l[0].AsSqlFullName, Is.Not.Null);
			Assert.That(l[0].AsSqlFullName, Is.EqualTo(l[0].LastName + ", " + l[0].FirstName));
		}

		[Test]
		public void CalculatedColumnTest5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q =
					db.GetTable<DoctorCalculated>()
						.SelectMany(d => d.PersonDoctor)
						.Select(d => d.FirstName);
			var l = q.ToList();

			l.ShouldNotBeEmpty();
			l[0].ShouldNotBeNull();
		}

		[Table("Person")]
		public class CustomPerson1
		{
			[ExpressionMethod(nameof(Expr), IsColumn = true)]
			public string? MiddleNamePreview { get; set; }

			private static Expression<Func<CustomPerson1, string?>> Expr()
			{
				return e => Sql.TableField<CustomPerson1, string>(e, "MiddleName").Substring(0, 200);
			}
		}

		[Table("Person")]
		public class CustomPerson2
		{
			[ExpressionMethod(nameof(Expr), IsColumn = true)]
			public string? MiddleNamePreview { get; set; }

			private static Expression<Func<CustomPerson2, string?>> Expr()
			{
				return e => Sql.Property<string>(e, "MiddleName").Substring(0, 200);
			}
		}

		[Test]
		public void CalculatedColumnExpression1([IncludeDataSources(true, TestProvName.AllFirebird, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			_ = db.GetTable<CustomPerson1>().ToArray();
		}

		[Test]
		public void CalculatedColumnExpression2([IncludeDataSources(true, TestProvName.AllFirebird, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			_ = db.GetTable<CustomPerson2>().ToArray();
		}

		sealed class InterpolatedStringTable
		{
			public int     Id     { get; set; }
			public string? StrVal { get; set; }
			public int     IntVal { get; set; }

			[ExpressionMethod(nameof(Expr1Impl), IsColumn = true)]
			public string? SimpleExpression { get; set; }

			private static Expression<Func<InterpolatedStringTable, IDataContext, string?>> Expr1Impl() =>
				(e, ctx) => !string.IsNullOrEmpty(e.StrVal) ? e.StrVal : e.IntVal.ToString();

			[ExpressionMethod(nameof(Expr2Impl), IsColumn = true)]
			public string? InterpolatedExpression { get; set; }

			private static Expression<Func<InterpolatedStringTable, IDataContext, string?>> Expr2Impl() =>
				(e, ctx) => $"{(!string.IsNullOrEmpty(e.StrVal) ? e.StrVal : e.IntVal.ToString())}";

			public static readonly InterpolatedStringTable[] Data =
			[
				new () { Id = 1, StrVal = null,     IntVal = 11 },
				new () { Id = 2, StrVal = "",       IntVal = 12 },
				new () { Id = 3, StrVal = "Value3", IntVal = 13 },
			];
		}

		[Test]
		public void TestInterpolatedStringAsExpression([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(InterpolatedStringTable.Data);

			var res = tb.OrderBy(r => r.Id).ToArray();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].SimpleExpression, Is.EqualTo("11"));
				Assert.That(res[1].SimpleExpression, Is.EqualTo("12"));
				Assert.That(res[2].SimpleExpression, Is.EqualTo("Value3"));

				Assert.That(res[0].InterpolatedExpression, Is.EqualTo("11"));
				Assert.That(res[1].InterpolatedExpression, Is.EqualTo("12"));
				Assert.That(res[2].InterpolatedExpression, Is.EqualTo("Value3"));
			}
		}

		[Test]
		public void TestInterpolatedStringAsExpression_ComplexQuery([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(InterpolatedStringTable.Data);

			var res = tb
				.OrderBy(x => x.SimpleExpression)
				.Select(x => new
				{
					x.Id,
					x.SimpleExpression,
					x.InterpolatedExpression
				}).ToArray();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].SimpleExpression, Is.EqualTo("11"));
				Assert.That(res[1].SimpleExpression, Is.EqualTo("12"));
				Assert.That(res[2].SimpleExpression, Is.EqualTo("Value3"));

				Assert.That(res[0].InterpolatedExpression, Is.EqualTo("11"));
				Assert.That(res[1].InterpolatedExpression, Is.EqualTo("12"));
				Assert.That(res[2].InterpolatedExpression, Is.EqualTo("Value3"));
			}

			res = tb
				.OrderBy(x => x.InterpolatedExpression)
				.Select(x => new
				{
					x.Id,
					x.SimpleExpression,
					x.InterpolatedExpression
				}).ToArray();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].SimpleExpression, Is.EqualTo("11"));
				Assert.That(res[1].SimpleExpression, Is.EqualTo("12"));
				Assert.That(res[2].SimpleExpression, Is.EqualTo("Value3"));

				Assert.That(res[0].InterpolatedExpression, Is.EqualTo("11"));
				Assert.That(res[1].InterpolatedExpression, Is.EqualTo("12"));
				Assert.That(res[2].InterpolatedExpression, Is.EqualTo("Value3"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1900")]
		public void ExpressionColumnOverDynamicPropertyFilter([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new []{ new DynamicPropertyExprSource
			{
				Name = "Some",
				Age = 10,
				Options = 4
			}}))
			{
				var result = db.GetTable<DynamicPropertyExprPerson>().Where(p => p.Type == 4 || p.Type == 5).ToList();
				Assert.That(result, Has.Count.EqualTo(1));
			}
		}

		[Table("PersonOptions")]
		public class DynamicPropertyExprPerson
		{
			[Column("AGE")]
			public int Age {get; set;}
			[Column("NAME")]
			public string? Name {get; set;}

			[ExpressionMethod(nameof(GetTypeExpr), IsColumn = true)]
			public int Type {get; set;}

			public static Expression<Func<DynamicPropertyExprPerson, int>> GetTypeExpr()
			{
				return p => Sql.Property<int>(p, "OPTIONS") & 0xF;
			}
		}

		[Table("PersonOptions")]
		public class DynamicPropertyExprSource
		{
			[Column("AGE")]
			public int Age {get; set;}
			[Column("NAME")]
			public string? Name {get; set;}

			[Column("OPTIONS")]
			public int Options { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1969")]
		public void ExpressionColumnSubqueryCountInJoinFilter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new[]
			{
				new CountColumnConfig{Id = 1, Name = "Some1"},
				new CountColumnConfig{Id = 2, Name = "Some2"}
			}))
			using (db.CreateLocalTable(new[]
			{
				new CountColumnKompo{Number = 1, Typ = 1} ,
				new CountColumnKompo{Number = 1, Typ = 2}, 
				new CountColumnKompo{Number = 2, Typ = 3} 
			}))
			{
				var query =
					from k in db.GetTable<CountColumnKompo>()
					from c in db.GetTable<CountColumnConfig>().InnerJoin(c => c.Id == k.Number)
					where c.CountOf > 1
					select c;
				var res = query.ToArray();

				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0].CountOf, Is.EqualTo(2));
					Assert.That(res[1].CountOf, Is.EqualTo(2));
				}
			}
		}

		[Table("CONFIG")]
		public partial class CountColumnConfig
		{
			[Column("ID"),      PrimaryKey,  NotNull] public int     Id   { get; set; } 
			[Column("NAME"),    Nullable            ] public string? Name { get; set; } 
	  
			[ExpressionMethod(nameof(GetFromExpr), IsColumn = true)]
			public int CountOf{ get; set; }

			static Expression<Func<IDataContext, CountColumnConfig, int>> GetFromExpr()
			{
				return (db, p) => db.GetTable<CountColumnKompo>().Where(k => k.Number == p.Id).Count();
			}
		}

		[Table("KOMPO")]
		public partial class CountColumnKompo
		{
			[Column("TYP"),    PrimaryKey, NotNull] public int Typ    { get; set; }
			[Column("NUMBER"),             NotNull] public int Number { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1983")]
		public void ExpressionMethodSubselectColumnInOrFilter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<CardCountPerson>())
			using (db.CreateLocalTable<CardCountCard>())
			{
				var q = from p in db.GetTable<CardCountPerson>()
						select new CardCountPerson()
						{
							Id = p.Id,
							CountOfCards = db.GetTable<CardCountCard>().Count(card => card.PersonId == p.Id && card.CardType == 2)
						};

				// works
				q.Where(cu => cu.CountOfCards == 0 || cu.CountOfCards != 0).ToList();

				// workaround
				db.GetTable<CardCountPerson>().Where(cu => cu.CountOfCards3 == 0 || cu.CountOfCards3 != 0).ToList();

				// fails
				db.GetTable<CardCountPerson>().Where(cu => cu.CountOfCards2 == 0 || cu.CountOfCards2 != 0).ToList();
			}
		}

		public class CardCountPerson
		{
			public int     Id            { get; set; }
			public string? Name          { get; set; }
			public int     CountOfCards  { get; set; }
			[ExpressionMethod(nameof(CountOfCardExpr), IsColumn = true)]
			public int     CountOfCards2 { get; set; }

			public static Expression<Func<CardCountPerson, IDataContext, int>> CountOfCardExpr()
			{
				return (p, db) => db.GetTable<CardCountCard>().Count(card => card.PersonId == p.Id && card.CardType == 2);
			}

			[ExpressionMethod(nameof(CountOfCardExpr3), IsColumn = true)]
			public int    CountOfCards3 { get; set; }

			public static Expression<Func<CardCountPerson, IDataContext, int>> CountOfCardExpr3()
			{
				return (p, db) => p.Cards.Where(card => card.CardType == 2).Count();
			}

			[Association(ThisKey = nameof(Id), OtherKey = nameof(CardCountCard.PersonId))]
			public IEnumerable<CardCountCard> Cards { get; set; } = null!;
		}

		public class CardCountCard
		{
			public int     Id         { get; set; }
			public int     CardType   { get; set; }
			public string? CardNumber { get; set; }
			public int     PersonId   { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3001")]
		public void ExpressionColumnUsingExpressionAssociation([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)]
			string context)
		{
			using var db          = GetDataContext(context);
			using var houseTable  = db.CreateLocalTable(new[] {new CalcColHouse  {Id = 1, Levels   = 2}});
			using var personTable = db.CreateLocalTable(new[] {new CalcColPerson {Id = 1, HouseId  = 1}});
			using var petTable    = db.CreateLocalTable(new[] {new CalcColPet    {Id = 1, PersonId = 1}});

			var data = petTable
				.Select(x => new {x.Id, x.IsHouseMultiLevel})
				.First();

			data.IsHouseMultiLevel.ShouldBeTrue();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3001")]
		public void ExpressionColumnUsingExpressionAssociationWithMissingHouse([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool preferClient)
		{
			using var db          = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var houseTable  = db.CreateLocalTable(new[] {new CalcColHouse  {Id = 1, Levels   = 2}});
			using var personTable = db.CreateLocalTable(new[] {new CalcColPerson {Id = 1, HouseId  = 1}, new CalcColPerson {Id = 2, HouseId  = 99}});
			using var petTable    = db.CreateLocalTable(new[] {new CalcColPet    {Id = 1, PersonId = 1}, new CalcColPet    {Id = 2, PersonId = 2}});

			var data = petTable
				.OrderBy(x => x.Id)
				.Select(x => new {x.Id, x.IsHouseMultiLevel, x.HasNoLevels})
				.ToList();

			data.Select(x => (x.Id, x.IsHouseMultiLevel, x.HasNoLevels)).ShouldBe(new[] {(1, true, false), (2, false, true)});

			// A predicate keeps SQL NULL, so a missing house satisfies neither filter, unlike the projection.
			petTable.Where(x => x.HasNoLevels).Select(x => x.Id).ToList().ShouldBeEmpty();
			petTable.Where(x => x.IsHouseMultiLevel).Select(x => x.Id).ToList().ShouldBe(new[] {1});
		}

		public class CalcColHouse
		{
			[Column] [PrimaryKey] public int Id;

			[Column]
			public int Levels { get; set; }
		}

		public class CalcColPerson
		{
			[Column] public int HouseId;

			[Column] [PrimaryKey] public int Id;

			[Association(ThisKey = nameof(HouseId), OtherKey = "Id", CanBeNull = true)]
			public CalcColHouse House { get; set; } = null!;
		}

		public class CalcColPet
		{
			[Column] [PrimaryKey] public int Id;

			[Column] public int PersonId;

			[Association(ThisKey = nameof(PersonId), OtherKey = "Id", CanBeNull = false)]
			public CalcColPerson Person { get; set; } = null!;

			[ExpressionMethod(nameof(HouseExpression))]
			public CalcColHouse House { get; set; } = null!;

			[ExpressionMethod(nameof(IsHouseMultiLevelExpression), IsColumn = true)]
			public bool IsHouseMultiLevel { get; set; }

			[ExpressionMethod(nameof(HasNoLevelsExpression), IsColumn = true)]
			public bool HasNoLevels { get; set; }

			private static Expression<Func<CalcColPet, CalcColHouse>> HouseExpression()
			{
				return entity => entity.Person.House;
			}

			private static Expression<Func<CalcColPet, bool>> IsHouseMultiLevelExpression()
			{
				// Replacing code below to entity.Parent.House.Levels > 1 will make column work
				return entity => entity.House.Levels > 1;
			}

			private static Expression<Func<CalcColPet, bool>> HasNoLevelsExpression() => entity => entity.House.Levels < 1;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3043")]
		public void CalculatedFieldOverExpressionAssociationChain([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db          = GetDataContext(context);
			using var cityTable   = db.CreateLocalTable(new[] {new AssocChainCity {Id   = 1, Name   = "City", Population = 100}});
			using var streetTable = db.CreateLocalTable(new[] {new AssocChainStreet {Id = 1, CityId = 1}, new AssocChainStreet {Id = 2, CityId = 99}});
			using var houseTable  = db.CreateLocalTable(new[] {new AssocChainHouse {Id  = 1, StreetId = 1}, new AssocChainHouse {Id  = 2, StreetId = 2}});
			using var personTable = db.CreateLocalTable(new[] {new AssocChainPerson {Id = 1, HouseId = 1}, new AssocChainPerson {Id = 2, HouseId = 2}});
			using var petTable    = db.CreateLocalTable(new[] {new AssocChainPet {Id = 1, PersonId = 1}, new AssocChainPet {Id = 2, PersonId = 2}});

			var data = petTable
				.OrderBy(x => x.Id)
				.Select(x => new
				{
					x.Id,
					x.House!.Street!.CityInfo
				})
				.ToList();

			data.Select(x => (x.Id, x.CityInfo)).ShouldBe(new (int, string?)[] {(1, "City 100"), (2, " 0")});
		}

		public class AssocChainCity
		{
			[Column, PrimaryKey]
			public int Id;

			[Column]
			public string Name { get; set; } = null!;

			[Column]
			public int Population { get; set; }
		}

		public class AssocChainStreet
		{
			[Column, PrimaryKey]
			public int Id;

			[Column]
			public int CityId { get; set; }
	
			[Association(ThisKey = nameof(CityId), OtherKey = "Id", CanBeNull = true)]
			public AssocChainCity? City { get; set; }
	
			[ExpressionMethod(nameof(CityInfoExpression))]
			public string? CityInfo { get; set; }

			private static Expression<Func<AssocChainStreet, string>> CityInfoExpression()
			{
				return entity => entity.City!.Name + " " + entity.City.Population;
			}
		}

		public class AssocChainHouse
		{
			[Column, PrimaryKey]
			public int Id;

			[Column]
			public int StreetId { get; set; }
	
			[Association(ThisKey = nameof(StreetId), OtherKey = "Id", CanBeNull = true)]
			public AssocChainStreet? Street { get; set; }
		}

		public class AssocChainPerson
		{
			[Column, PrimaryKey]
			public int Id;

			[Column]
			public int HouseId;

			[Association(ThisKey = nameof(HouseId), OtherKey = "Id", CanBeNull = true)]
			public AssocChainHouse? House { get; set; }
		}

		public class AssocChainPet
		{
			[Column, PrimaryKey]
			public int Id;

			[Column]
			public int PersonId;

			[Association(ThisKey = nameof(PersonId), OtherKey = "Id", CanBeNull = false)]
			public AssocChainPerson Person { get; set; } = null!;

			[ExpressionMethod(nameof(HouseExpression))]
			public AssocChainHouse? House { get; set; }

			private static Expression<Func<AssocChainPet, AssocChainHouse>> HouseExpression()
			{
				return entity => entity.Person.House!;
			}
		}
	}
}
