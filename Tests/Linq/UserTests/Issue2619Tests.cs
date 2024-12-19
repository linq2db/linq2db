using System;
using System.Linq;
using FluentAssertions;
using LinqToDB;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2619Tests : TestBase
	{
		[Test]
		public void OrderByUnion ([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var persons = db.Person
						.OrderBy (c => c.LastName);

				var union = persons
						.Union (persons);

				var sql = union.ToSqlQuery().Sql;

				sql.Should().NotContain("ORDER");

				FluentActions.Enumerating(() => union).Should().NotThrow();
			}
		}

		[Test]
		public void OrderByUnionModifier([DataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var persons = db.Person
					.OrderBy (c => c.LastName).Take(1);

				var union = persons
					.Union (persons);

				var sql = union.ToSqlQuery().Sql;

				sql.Should().Contain("ORDER", Exactly.Twice());

				FluentActions.Enumerating(() => union).Should().NotThrow();
			}
		}

		[Test]
		public void OrderByConcat([DataSources(ProviderName.SqlCe, TestProvName.AllSqlServer, TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var persons = db.Person
					.OrderBy (c => c.LastName);

				var concat = persons
					.Concat(persons);

				var sql = concat.ToSqlQuery().Sql;

				sql.Should().Contain("ORDER", Exactly.Twice());

				FluentActions.Enumerating(() => concat).Should().NotThrow();
			}
		}

		[Test]
		public void OrderByConcatModifier([DataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var persons = db.Person
					.OrderBy (c => c.LastName).Take(1);

				var concat = persons
					.Concat(persons);

				var sql = concat.ToSqlQuery().Sql;

				sql.Should().Contain("ORDER", Exactly.Twice());

				FluentActions.Enumerating(() => concat).Should().NotThrow();
			}
		}


		[Test]
		public void OrderByExcept([DataSources(TestProvName.AllSybase, TestProvName.AllSqlServer, TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var persons = db.Person
					.OrderBy (c => c.LastName);

				var concat = persons
					.Except(persons);

				var sql = concat.ToSqlQuery().Sql;

				if (!sql.Contains("EXISTS"))
					sql.Should().NotContain("ORDER");

				FluentActions.Enumerating(() => concat).Should().NotThrow();
			}
		}

		[Test]
		public void OrderByExceptModifier([DataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var persons = db.Person
					.OrderBy (c => c.LastName)
					.Take(1);

				var except = persons
					.Except(persons);

				var sql = except.ToSqlQuery().Sql;

				sql.Should().Contain("ORDER", AtLeast.Once());

				FluentActions.Enumerating(() => except).Should().NotThrow();
			}
		}

	}
}
