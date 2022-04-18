﻿using System;
using System.Linq;
using FluentAssertions;
using LinqToDB;
using NUnit.Framework;
using Tests.Model;

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

				var sql = union.ToString();

				sql.Should().NotContain("ORDER");

				Assert.DoesNotThrow (() =>
				{
					union.ToList();
				});
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

				var sql = union.ToString();

				sql.Should().Contain("ORDER", Exactly.Twice());

				Assert.DoesNotThrow (() =>
				{
					union.ToList();
				});
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

				var sql = concat.ToString();

				sql.Should().Contain("ORDER", Exactly.Twice());

				Assert.DoesNotThrow (() =>
				{
					concat.ToList();
				});
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

				var sql = concat.ToString();

				sql.Should().Contain("ORDER", Exactly.Twice());

				Assert.DoesNotThrow (() =>
				{
					concat.ToList();
				});
			}
		}


		[Test]
		public void OrderByExcept([DataSources(TestProvName.AllSybase, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var persons = db.Person
					.OrderBy (c => c.LastName);

				var concat = persons
					.Except(persons);

				var sql = concat.ToString();

				sql.Should().Contain("ORDER", AtLeast.Once());

				Assert.DoesNotThrow (() =>
				{
					concat.ToList();
				});
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

				var sql = except.ToString();

				sql.Should().Contain("ORDER", AtLeast.Once());

				Assert.DoesNotThrow (() =>
				{
					except.ToList();
				});
			}
		}

	}
}
