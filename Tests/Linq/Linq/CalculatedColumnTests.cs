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

		[Test, DataContextSource]
		public void CalculatedColumnTest1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.GetTable<PersonCalculated>().Where(i => i.FirstName != "John");
				var l = q.ToList();

				Assert.That(l, Is.Not.Empty);
				Assert.That(l[0].FullName,      Is.Not.Null);
				Assert.That(l[0].AsSqlFullName, Is.Not.Null);
				Assert.That(l[0].FullName,      Is.EqualTo(l[0].LastName + ", " + l[0].FirstName));
				Assert.That(l[0].AsSqlFullName, Is.EqualTo(l[0].LastName + ", " + l[0].FirstName));
			}
		}

		[Test, DataContextSource]
		public void CalculatedColumnTest2(string context)
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

		[Test, DataContextSource]
		public void CalculatedColumnTest3(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.GetTable<PersonCalculated>().Where(i => i.FirstName != "John").Select(t => new { t });
				var l = q.ToList();

				Assert.That(l, Is.Not.Empty);
				Assert.That(l[0].t.FullName,      Is.Not.Null);
				Assert.That(l[0].t.AsSqlFullName, Is.Not.Null);
				Assert.That(l[0].t.FullName,      Is.EqualTo(l[0].t.LastName + ", " + l[0].t.FirstName));
				Assert.That(l[0].t.AsSqlFullName, Is.EqualTo(l[0].t.LastName + ", " + l[0].t.FirstName));
			}
		}
	}
}
