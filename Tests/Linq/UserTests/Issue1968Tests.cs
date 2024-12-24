using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1968Tests : TestBase
	{
		public class University
		{
			public University()
			{
			}

			[Column] public int Id { get; set; }
			[Column] public string? Name { get; set; }
			[Column] public string? Location { get; set; }

			[Association(ThisKey = "Id", OtherKey = nameof(Faculty.UniversityId), CanBeNull = true)]
			public ICollection<Faculty> Faculties { get; set; } = null!;

			public static Expression<Func<University, IDataContext, IQueryable<Subject>>> Expression()
			{
				return (u, db) => db.GetTable<Subject>()
					.Where(x => u.Faculties.Any(m => m.Id == x.FacultyId));
			}

			[Association(QueryExpressionMethod = nameof(Expression))]
			public ICollection<Subject> Subjects { get; set; } = null!;
		}

		public class Faculty
		{
			[Column] public int Id { get; set; }
			[Column] public string? Code { get; set; }
			[Column] public string? FacultyName { get; set; }
			[Column] public string? Direction { get; set; }
			[Column] public int Grant { get; set; }
			[Column] public int Contract { get; set; }
			[Column] public int? UniversityId { get; set; }

			[Association(ThisKey = "UniversityId", OtherKey = "Id", CanBeNull = true)]
			public University? University { get; set; }

			[Association(ThisKey = "Id", OtherKey = "FacultyId", CanBeNull = true)]
			public Subject? Subject { get; set; }
		}

		public class Subject
		{
			[Column] public int SubjectId { get; set; }
			[Column] public string? FirstSubject { get; set; }
			[Column] public string? SecondSubject { get; set; }
			[Column] public string? ThirdSubject { get; set; }
			[Column] public int? FacultyId { get; set; }

			[Association(ThisKey = "FacultyId", OtherKey = "Id", CanBeNull = false)]
			public Faculty Faculty { get; set; } = null!;
		}

		// https://stackoverflow.com/questions/58738542
		[Test]
		public void Issue1968Test([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<University>())
			using (db.CreateLocalTable<Faculty>())
			using (db.CreateLocalTable<Subject>())
			{
				db.Insert(new University()
				{
					Id = 1,
					Location = "location",
					Name = "name"
				});

				db.Insert(new Faculty()
				{
					Id = 1,
					Code = "code",
					Contract = 1,
					Direction = "direction",
					FacultyName = "faculty name",
					Grant = 1,
					UniversityId = 1
				});

				db.Insert(new Subject()
				{
					FacultyId = 1,
					FirstSubject = "first",
					SecondSubject = "second",
					SubjectId = 1,
					ThirdSubject = "third"
				});

				var result = db.GetTable<University>()
					.LoadWith(x => x.Faculties)
					.LoadWith(m => m.Subjects).ToList();

				Assert.That(result, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(result[0].Faculties, Has.Count.EqualTo(1));
					Assert.That(result[0].Subjects, Has.Count.EqualTo(1));
				});
			}
		}
	}
}
