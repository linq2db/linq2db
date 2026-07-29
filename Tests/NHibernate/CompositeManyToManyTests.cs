using System.Linq;

using LinqToDB;
using LinqToDB.NHibernate.Tests.Models.CompositeManyToMany;

using NHibernate;
using NHibernate.Linq;

using NUnit.Framework;

using Shouldly;

using Tests;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Verifies many-to-many association support when BOTH linked entities have composite (multi-column)
	/// primary keys and the junction carries all four key columns. The synthesized association must AND
	/// every key/element column pair AND pair them in the correct order — the seed data uses distinct
	/// values across every key column, so any mis-paired join returns the wrong rows.
	/// </summary>
	[TestFixture]
	public class CompositeManyToManyTests : NHTestBase
	{
		static void SeedGraph(ISessionFactory sf)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			// linq2db commands run inside the NHibernate transaction (the attached connection shares it via UseTransaction).
			session.GetTable<CourseStudent>().Delete();
			session.GetTable<Student>().Delete();
			session.GetTable<Course>().Delete();

			session.Save(new Course  { DeptId = 10, CourseNo = 101, Title = "Algorithms" });
			session.Save(new Course  { DeptId = 20, CourseNo = 201, Title = "Databases"  });
			session.Save(new Student { CampusId = 1, Roll = 5, Name = "Ann" });
			session.Save(new Student { CampusId = 2, Roll = 6, Name = "Bob" });
			session.Save(new Student { CampusId = 3, Roll = 7, Name = "Cy"  });

			// Course (10,101) -> students (1,5) and (2,6); course (20,201) -> student (3,7).
			session.Save(new CourseStudent { DeptId = 10, CourseNo = 101, CampusId = 1, Roll = 5 });
			session.Save(new CourseStudent { DeptId = 10, CourseNo = 101, CampusId = 2, Roll = 6 });
			session.Save(new CourseStudent { DeptId = 20, CourseNo = 201, CampusId = 3, Roll = 7 });

			tx.Commit();
		}

		[Test]
		public void CompositeKey_NavigatesThroughJunction(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();

			var names = session.GetTable<Course>()
				.Where(c => c.DeptId == 10 && c.CourseNo == 101)
				.SelectMany(c => c.Students)
				.Select(s => s.Name)
				.OrderBy(n => n)
				.ToList();

			names.ShouldBe(new[] { "Ann", "Bob" });

			// The same composite-key junction navigation through NHibernate's own LINQ provider must agree.
			var nhNames = session.Query<Course>()
				.Where(c => c.DeptId == 10 && c.CourseNo == 101)
				.SelectMany(c => c.Students)
				.Select(s => s.Name)
				.OrderBy(n => n)
				.ToList();

			nhNames.ShouldBe(names);
		}

		[Test]
		public void CompositeKey_OtherCourseGetsOnlyItsOwnStudents(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();

			var names = session.GetTable<Course>()
				.Where(c => c.DeptId == 20 && c.CourseNo == 201)
				.SelectMany(c => c.Students)
				.Select(s => s.Name)
				.OrderBy(n => n)
				.ToList();

			names.ShouldBe(new[] { "Cy" });

			// NHibernate's own LINQ provider must return only this course's own student.
			var nhNames = session.Query<Course>()
				.Where(c => c.DeptId == 20 && c.CourseNo == 201)
				.SelectMany(c => c.Students)
				.Select(s => s.Name)
				.OrderBy(n => n)
				.ToList();

			nhNames.ShouldBe(names);
		}
	}
}
