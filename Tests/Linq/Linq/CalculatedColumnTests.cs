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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void CalculatedColumnTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.GetTable<PersonCalculated>().Where(i => i.FirstName != "John");
				var l = q.ToList();

				Assert.That(l,                  Is.Not.Empty);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(l[0].FullName, Is.Not.Null);
					Assert.That(l[0].AsSqlFullName, Is.Not.Null);
					Assert.That(l[0].FullName, Is.EqualTo(l[0].LastName + ", " + l[0].FirstName));
					Assert.That(l[0].AsSqlFullName, Is.EqualTo(l[0].LastName + ", " + l[0].FirstName));
				}
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void CalculatedColumnTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var list  = db.GetTable<PersonCalculated>().ToList();
				var query = db.GetTable<PersonCalculated>().Where(i => i.FullName != "Pupkin, John").ToList();

				Assert.That(list, Has.Count.Not.EqualTo(query.Count));

				AreEqual(
					list.Where(i => i.FullName != "Pupkin, John"),
					query,
					PersonCalculated.Comparer);
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void CalculatedColumnTest3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.GetTable<PersonCalculated>()
					.Where (i => i.FirstName != "John")
					.Select(t => new
					{
						cnt = db.Doctor.Count(d => d.PersonID == t.PersonID),
						t,
					});
				var l = q.ToList();

				Assert.That(l,                    Is.Not.Empty);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(l[0].t.FullName, Is.Not.Null);
					Assert.That(l[0].t.AsSqlFullName, Is.Not.Null);
					Assert.That(l[0].t.FullName, Is.EqualTo(l[0].t.LastName + ", " + l[0].t.FirstName));
					Assert.That(l[0].t.AsSqlFullName, Is.EqualTo(l[0].t.LastName + ", " + l[0].t.FirstName));
					Assert.That(l[0].t.DoctorCount, Is.EqualTo(l[0].cnt));
				}
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void CalculatedColumnTest4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					db.GetTable<DoctorCalculated>()
						.SelectMany(d => d.PersonDoctor);
				var l = q.ToList();

				Assert.That(l,                  Is.Not.Empty);
				Assert.That(l[0].AsSqlFullName, Is.Not.Null);
				Assert.That(l[0].AsSqlFullName, Is.EqualTo(l[0].LastName + ", " + l[0].FirstName));
			}
		}

		[Test]
		public void CalculatedColumnTest5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					db.GetTable<DoctorCalculated>()
						.SelectMany(d => d.PersonDoctor)
						.Select(d => d.FirstName);
				var l = q.ToList();

				l.ShouldNotBeEmpty();
				l[0].ShouldNotBeNull();
			}
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
			using (var db = GetDataContext(context))
			{
				_ = db.GetTable<CustomPerson1>().ToArray();
			}
		}

		[Test]
		public void CalculatedColumnExpression2([IncludeDataSources(true, TestProvName.AllFirebird, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				_ = db.GetTable<CustomPerson2>().ToArray();
			}
		}

		sealed class InterpolatedStringTable
		{
			public int     Id     { get; set; }
			public string? StrVal { get; set; }
			public int     IntVal { get; set; }

			[ExpressionMethod(nameof(Expr1Impl))]
			public string? SimpleExpression { get; set; }

			private static Expression<Func<InterpolatedStringTable, IDataContext, string?>> Expr1Impl() =>
				(e, ctx) => !string.IsNullOrEmpty(e.StrVal) ? e.StrVal : e.IntVal.ToString();

			[ExpressionMethod(nameof(Expr2Impl))]
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
	}
}
