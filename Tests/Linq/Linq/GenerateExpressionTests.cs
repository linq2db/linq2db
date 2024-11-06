using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class GenerateExpressionTests : TestBase
	{
		[Test]
		public void Test1([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using(new GenerateExpressionTest(true))
			using (var db = GetDataContext(context))
			{
				var q2 =
					from gc1 in db.GrandChild
						join max in
							from gch in db.GrandChild
							group gch by gch.ChildID into g
							select g.Max(c => c.GrandChildID)
						on gc1.GrandChildID equals max
					select gc1;

				var result =
					from ch in db.Child
						join p   in db.Parent on ch.ParentID equals p.ParentID
						join gc2 in q2        on p.ParentID  equals gc2.ParentID into g
						from gc3 in g.DefaultIfEmpty()
				where gc3 == null || 
						!new[] { 111, 222 }.Contains(gc3.GrandChildID!.Value)
				select new { p.ParentID, gc3 };


				var test = result.GenerateTestString();

				TestContext.Out.WriteLine(test);

				var _ = result.ToList();
			}
		}

		[Test]
		public void Test2([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q2 =
					from gc1 in db.Person
					where gc1.Gender == Model.Gender.Male
					select gc1;


				var test = q2.GenerateTestString();

				TestContext.Out.WriteLine(test);

				var _ = q2.ToList();
			}
		}

		#region issue 4322
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4322")]
		public void Issue4322Test([DataSources] string context)
		{
			using var _ = new GenerateExpressionTest(true);
			using var db = GetDataContext(context);

			var example = new Vector2[] { new Vector2 {X = -10f, Y = 10f} };

			var query = db.GetTable<Entity>().Where(x => PositionFilter(x.Position, example)).Take(3);

			// this works
			TestUtils.DeleteTestCases();
			var testCase = query.GenerateTestString();
			Assert.That(testCase, Does.Not.Contain("Exception"));

			TestUtils.DeleteTestCases();
			Assert.That(() => query.ToArray(), Throws.Exception);

			testCase = TestUtils.GetLastTestCase();
			Assert.That(testCase, Is.Not.Null);
			Assert.That(testCase, Does.Not.Contain("Exception"));
		}

		[Table("entities")]
		sealed class Entity
		{
			[Column("position")]
			[NotNull]
			public Vector3 Position { get; set; } = null!;
		}

		sealed class Vector3
		{
			public float x;
			public float y;
			public float z;
		}

		sealed class Vector2
		{
			public float X;
			public float Y;
		}

		[ExpressionMethod(nameof(PositionFilterImplementation))]
		static bool PositionFilter(Vector3 position, Vector2[] example)
		{
			throw new NotImplementedException();
		}

		static Expression<Func<Vector3, Vector2[], bool>> PositionFilterImplementation()
		{
			return (position, example) => example.Any(t => Sql.Expr<bool>($"{position}.x > {t.X}"));
		}

		#endregion
	}
}
