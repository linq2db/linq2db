using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue5049Tests : TestBase
	{
		[Table(Name = "PersonWithAssociation")]
		public sealed class PersonWithAssociation
		{
			[Column, PrimaryKey, Identity]
			public int Id { get; set; }

			[Column]
			public string? Name { get; set; }

			[Association(QueryExpressionMethod = nameof(GetGradeStats))]
			public GradeStatsCls? GradeStats { get; set; }

			public class GradeStatsCls
			{
				public int    PersonId     { get; set; }
				public double AverageGrade { get; set; }
			}

			public static Expression<Func<PersonWithAssociation, IDataContext, IQueryable<GradeStatsCls?>>> GetGradeStats()
			{
				return (person, db) => db.GetTable<PersonGrade>()
					.Where(x => x.PersonId == person.Id)
					.GroupBy(x => x.PersonId)
					.Select(g => new GradeStatsCls { PersonId = g.Key, AverageGrade = g.Average(x => (double)x.Grade), })
					.DefaultIfEmpty();
			}
		}

		[Table(Name = "PersonGrades")]
		public sealed class PersonGrade
		{
			[Column, PrimaryKey]
			public int Id { get; set; }

			[Column]
			public int PersonId { get; set; }

			[Column]
			public string? Subject { get; set; }

			[Column]
			public int Grade { get; set; }
		}

		[Test]
		public void Issue5049Test([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var personsData = new[] { new PersonWithAssociation { Id = 1, Name = "Bob" } };
			var personGrateData = new[]
			{
				new PersonGrade { Id = 1, PersonId = 1, Subject = "English", Grade = 8 },
				new PersonGrade { Id = 2, PersonId = 1, Subject = "Math", Grade = 5 },
				new PersonGrade { Id = 3, PersonId = 1, Subject = "Geography", Grade = 9 }
			};

			var persons     = db.CreateLocalTable(personsData);
			var personGrate = db.CreateLocalTable(personGrateData);

			var query = db.GetTable<PersonWithAssociation>().LoadWith(p => p.GradeStats)
				.Where(p => p.GradeStats!.AverageGrade > 5)
				.ToArray();

		}
	}
}
