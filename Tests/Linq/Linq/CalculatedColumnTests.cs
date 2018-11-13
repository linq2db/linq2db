using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	public class CalculatedColumnTests : TestBase
	{
		[Table(Name="Person")]
		public class PersonCalculated
		{
			[Column, PrimaryKey,  Identity] public int    PersonID   { get; set; } // INTEGER
			[Column, NotNull              ] public string FirstName  { get; set; }
			[Column, NotNull              ] public string LastName   { get; set; }
			[Column,    Nullable          ] public string MiddleName { get; set; } // VARCHAR(50)
			[Column, NotNull              ] public char   Gender     { get; set; } // CHARACTER(1)

			[ExpressionMethod(nameof(GetFullNameExpr), IsColumn = true)]
			public string FullName { get; set; }

			static Expression<Func<PersonCalculated, string>> GetFullNameExpr()
			{
				return p => p.LastName + ", " + p.FirstName;
			}

			[ExpressionMethod(nameof(GetAsSqlFullNameExpr), IsColumn = true)]
			public string AsSqlFullName { get; set; }

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

			public static IEqualityComparer<PersonCalculated> Comparer = Tools.ComparerBuilder<PersonCalculated>.GetEqualityComparer();
		}

		[Table("Doctor")]
		public class DoctorCalculated
		{
			[Column, PrimaryKey, Identity] public int    PersonID { get; set; } // Long
			[Column(Length = 50), NotNull] public string Taxonomy { get; set; } // text(50)

			// Many association for test
			[Association(ThisKey="PersonID", OtherKey="PersonID", CanBeNull = false, KeyName="PersonDoctor", BackReferenceName="PersonDoctor")]
			public IEnumerable<PersonCalculated> PersonDoctor { get; set; }
		}

		[Test]
		public void CalculatedColumnTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.GetTable<PersonCalculated>().Where(i => i.FirstName != "John");
				var l = q.ToList();

				Assert.That(l,                  Is.Not.Empty);
				Assert.That(l[0].FullName,      Is.Not.Null);
				Assert.That(l[0].AsSqlFullName, Is.Not.Null);
				Assert.That(l[0].FullName,      Is.EqualTo(l[0].LastName + ", " + l[0].FirstName));
				Assert.That(l[0].AsSqlFullName, Is.EqualTo(l[0].LastName + ", " + l[0].FirstName));
			}
		}

		[Test]
		public void CalculatedColumnTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var list  = db.GetTable<PersonCalculated>().ToList();
				var query = db.GetTable<PersonCalculated>().Where(i => i.FullName != "Pupkin, John").ToList();

				Assert.That(list.Count, Is.Not.EqualTo(query.Count));

				AreEqual(
					list.Where(i => i.FullName != "Pupkin, John"),
					query,
					PersonCalculated.Comparer);
			}
		}

		[Test]
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
				Assert.That(l[0].t.FullName,      Is.Not.Null);
				Assert.That(l[0].t.AsSqlFullName, Is.Not.Null);
				Assert.That(l[0].t.FullName,      Is.EqualTo(l[0].t.LastName + ", " + l[0].t.FirstName));
				Assert.That(l[0].t.AsSqlFullName, Is.EqualTo(l[0].t.LastName + ", " + l[0].t.FirstName));
				Assert.That(l[0].t.DoctorCount,   Is.EqualTo(l[0].cnt));
			}
		}

		[ActiveIssue(Configuration = ProviderName.SapHana)]
		[Test]
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
	}
}
