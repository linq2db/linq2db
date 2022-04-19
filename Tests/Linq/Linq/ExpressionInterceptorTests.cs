using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using NUnit.Framework;

#pragma warning disable CS8618

namespace Tests.Linq
{
	public class UserSearchResult
	{
		// always populated
		public int    UserId     { get; set; }
		public string FirstName  { get; set; }
		public string LastName   { get; set; }
		public string Supervisor { get; set; }

		// only populated in user maintenance
		public string RoleName { get; set; }

		// only populated in user scheduling
		public int    PTOAccrued   { get; set; }
		public int    ScheduleId   { get; set; }
		public string ScheduleName { get; set; }
	}

	public class User
	{
		public int    UserId     { get; set; }
		public string FirstName  { get; set; }
		public string LastName   { get; set; }
		public string Supervisor { get; set; }
	}

	public static class QueryableExtensions
	{
		public static T EnrichWith<T>(this T obj, T additional) where T : class
		{
			throw new NotImplementedException();
		}
	}

	public static class DtoExtensions
	{
		[ExpressionMethod(nameof(ToUserSearchResultImpl))]
		public static UserSearchResult ToUserSearchResult(this User u)
		{
			throw new NotImplementedException();
		}

		private static Expression<Func<User, UserSearchResult>> ToUserSearchResultImpl()
		{
			return u => new UserSearchResult
			{
				UserId = u.UserId, FirstName = u.FirstName, LastName = u.LastName, Supervisor = u.Supervisor
			};
		}
	}

	public class EnrichInterceptor : IExpressionInterceptor
	{
		public Expression ProcessExpression(Expression expression)
		{
			var transformed = expression.Transform(static e =>
			{
				if (e.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression)e;
					if (mc.Method.DeclaringType == typeof(QueryableExtensions) && mc.Method.IsGenericMethod &&
					    mc.Method.Name          == nameof(QueryableExtensions.EnrichWith))
					{
						var toEnrich   = mc.Arguments[0] as MemberInitExpression;
						var enrichWith = mc.Arguments[1] as MemberInitExpression;

						if (toEnrich == null)
						{
							throw new InvalidOperationException(
								"Enriched expression should be 'SomeClass { Prop = x.Prop }'");
						}

						if (enrichWith == null)
						{
							throw new InvalidOperationException(
								"Expression for extending should be 'SomeClass { AdditionalProp = x.OtherProp }'");
						}

						var newBindings = new List<MemberBinding>(toEnrich.Bindings);

						foreach (var newBinding in enrichWith.Bindings)
						{
							if (newBinding is MemberAssignment ma)
							{
								var found = false;
								for (var i = 0; i < newBindings.Count; i++)
								{
									var oldBinding = newBindings[i];
									if (oldBinding is MemberAssignment maBinding)
									{
										if (maBinding.Member == ma.Member)
										{
											newBindings[i] = maBinding.Update(ma.Expression);
											found          = true;
											break;
										}
									}
								}

								if (!found)
								{
									newBindings.Add(ma);
								}
							}
							else
							{
								// probably we should process other bindings differently
								newBindings.Add(newBinding);
							}
						}

						return toEnrich.Update(toEnrich.NewExpression, newBindings);
					}
				}

				return e;
			});

			return transformed;
		}
	}

	[TestFixture]
	public class ExpressionInterceptorsTests : TestBase
	{
		[Table]
		private class SampleClass
		{
			[Column]
			public int Id { get; set; }

			[Column(Length = 50)]
			public string? Value { get; set; }
		}

		[Test]
		public void EnrichSimple([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var builder = new LinqToDBConnectionOptionsBuilder();
			builder.UseConfigurationString(context)
				.WithInterceptor(new EnrichInterceptor());

			using (var db = new DataConnection(builder.Build()))
			using (var users = db.CreateLocalTable(new[]
			       {
				       new User { UserId = 1, FirstName = "First", LastName = "Last", Supervisor = "Sup" }
			       }))
			using (var table = db.CreateLocalTable(new[] { new SampleClass { Id = 1, Value = "Some" } }))
			{
				var query =
					from x in table
					from u in users
					select u.ToUserSearchResult()
						.EnrichWith(new UserSearchResult { PTOAccrued = 1, LastName = "Enriched" });

				var result = query.First();

				result.PTOAccrued.Should().Be(1);
				result.LastName.Should().Be("Enriched");
			}
		}

		private static IQueryable<UserSearchResult> QueryableResult(IDataContext dc)
		{
			var query =
				from x in dc.GetTable<SampleClass>()
				from u in dc.GetTable<User>()
				select u.ToUserSearchResult()
					.EnrichWith(new UserSearchResult { PTOAccrued = 1, LastName = "Enriched" });

			return query;
		}

		[Test]
		public void EnrichViaQueryableMethod([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var builder = new LinqToDBConnectionOptionsBuilder();
			builder.UseConfigurationString(context)
				.WithInterceptor(new EnrichInterceptor());

			using (var db = new DataConnection(builder.Build()))
			using (var users = db.CreateLocalTable(new[]
			       {
				       new User { UserId = 1, FirstName = "First", LastName = "Last", Supervisor = "Sup" }
			       }))
			using (var table = db.CreateLocalTable(new[] { new SampleClass { Id = 1, Value = "Some" } }))
			{
				var query =
					from x in table
					from u in QueryableResult(db).InnerJoin(u => u.UserId == x.Id)
					select u;

				var result = query.First();

				result.PTOAccrued.Should().Be(1);
				result.LastName.Should().Be("Enriched");
			}
		}
	}
}
