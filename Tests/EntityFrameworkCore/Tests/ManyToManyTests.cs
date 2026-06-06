#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.EntityFrameworkCore.Tests.Models.ManyToMany;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	[TestFixture]
	public class ManyToManyTests : ContextTestBase<ManyToManyContextBase>
	{
		protected override ManyToManyContextBase CreateProviderContext(string provider, DbContextOptions<ManyToManyContextBase> options)
		{
			return provider switch
			{
				_ when provider.IsAnyOf(TestProvName.AllPostgreSQL) => new PostgreSQL.Models.ManyToMany.ManyToManyContext(options),
				_ when provider.IsAnyOf(TestProvName.AllMySql)       => new Pomelo.Models.ManyToMany.ManyToManyContext(options),
				_ when provider.IsAnyOf(TestProvName.AllSQLite)      => new SQLite.Models.ManyToMany.ManyToManyContext(options),
				_ when provider.IsAnyOf(TestProvName.AllSqlServer)   => new SqlServer.Models.ManyToMany.ManyToManyContext(options),
				_ => throw new InvalidOperationException($"{nameof(CreateProviderContext)} is not implemented for provider {provider}")
			};
		}

		// Compares the EF Core result against the same query routed through our translator via ToLinqToDB().
		private void AssertSame<T>(IQueryable<T> query) => AreEqual(query.ToList(), query.ToLinqToDB().ToList());

		#region 1. Implicit single-key

		[Test]
		public void Implicit_DirectAny([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Students.Where(s => s.Courses.Any(c => c.Title == "Physics")).OrderBy(s => s.Id).Select(s => s.Id));
		}

		[Test]
		public void Implicit_AnyNoPredicate([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Students.Where(s => s.Courses.Any()).OrderBy(s => s.Id).Select(s => s.Id));
		}

		[Test]
		public void Implicit_ReverseDirection([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Courses.Where(c => c.Students.Any(s => s.Name == "Alice")).OrderBy(c => c.Id).Select(c => c.Id));
		}

		[Test]
		public void Implicit_All([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Students.Where(s => s.Courses.All(c => c.Title != "History")).OrderBy(s => s.Id).Select(s => s.Id));
		}

		[Test]
		public void Implicit_Count([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Students.OrderBy(s => s.Id).Select(s => s.Courses.Count()));
		}

		[Test]
		public void Implicit_SelectMany([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Students.SelectMany(s => s.Courses).Select(c => c.Id));
		}

		[Test]
		public void Implicit_Include([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var query = ctx.Students.Include(s => s.Courses).OrderBy(s => s.Id);

			var ef   = query.ToList();
			var l2db = query.ToLinqToDB().ToList();

			AssertLoaded(ef, l2db, s => s.Id, s => s.Courses.Select(c => c.Id));
		}

		#endregion

		#region 2. Explicit join with payload

		[Test]
		public void Explicit_DirectAny([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Orders.Where(o => o.Products.Any(p => p.Name == "Apple")).OrderBy(o => o.Id).Select(o => o.Id));
		}

		[Test]
		public void Explicit_ReverseDirection([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Products.Where(p => p.Orders.Any(o => o.Number == "O-2")).OrderBy(p => p.Id).Select(p => p.Id));
		}

		[Test]
		public void Explicit_SelectMany([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Orders.SelectMany(o => o.Products).Select(p => p.Id));
		}

		[Test]
		public void Explicit_Include([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var query = ctx.Orders.Include(o => o.Products).OrderBy(o => o.Id);

			AssertLoaded(query.ToList(), query.ToLinqToDB().ToList(), o => o.Id, o => o.Products.Select(p => p.Id));
		}

		#endregion

		#region 3. Composite key (explicit join)

		[Test]
		public void Composite_DirectAny([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Projects.Where(p => p.Members.Any(m => m.Name == "Eve")).OrderBy(p => p.Code).Select(p => p.Code));
		}

		[Test]
		public void Composite_ReverseDirection([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Members.Where(m => m.Projects.Any(p => p.Name == "Alpha")).OrderBy(m => m.Id).Select(m => m.Id));
		}

		[Test]
		public void Composite_SelectMany([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Projects.SelectMany(p => p.Members).Select(m => m.Id));
		}

		[Test]
		public void Composite_Include([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var query = ctx.Projects.Include(p => p.Members).OrderBy(p => p.Code);

			AssertLoaded(query.ToList(), query.ToLinqToDB().ToList(), p => p.Code, p => p.Members.Select(m => m.Id));
		}

		#endregion

		#region 4. Self-referencing (explicit join)

		[Test]
		public void SelfRef_FriendsAny([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.People.Where(p => p.Friends.Any(f => f.Name == "Carol")).OrderBy(p => p.Id).Select(p => p.Id));
		}

		[Test]
		public void SelfRef_FriendsOfAny([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.People.Where(p => p.FriendsOf.Any(f => f.Name == "Alice")).OrderBy(p => p.Id).Select(p => p.Id));
		}

		[Test]
		public void SelfRef_SelectMany([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.People.SelectMany(p => p.Friends).Select(f => f.Id));
		}

		[Test]
		public void SelfRef_Include([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var query = ctx.People.Include(p => p.Friends).OrderBy(p => p.Id);

			AssertLoaded(query.ToList(), query.ToLinqToDB().ToList(), p => p.Id, p => p.Friends.Select(f => f.Id));
		}

		#endregion

		#region 5. Multiple distinct relationships between the same pair

		[Test]
		public void MultiplePair_Members([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Users.Where(u => u.Teams.Any(t => t.Name == "Team1")).OrderBy(u => u.Id).Select(u => u.Id));
		}

		[Test]
		public void MultiplePair_Leaders([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);
			AssertSame(ctx.Users.Where(u => u.LedTeams.Any(t => t.Name == "Team1")).OrderBy(u => u.Id).Select(u => u.Id));
		}

		[Test]
		public void MultiplePair_DistinctResults([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var members = ctx.Users.Where(u => u.Teams.Any(t => t.Name == "Team1")).Select(u => u.Id).ToLinqToDB().ToList();
			var leaders = ctx.Users.Where(u => u.LedTeams.Any(t => t.Name == "Team1")).Select(u => u.Id).ToLinqToDB().ToList();

			// The TJoin discriminator must route each navigation to its own join table.
			Assert.That(members, Is.EqualTo(new[] { 1 }));
			Assert.That(leaders, Is.EqualTo(new[] { 2 }));
		}

		#endregion

		#region 6. Multiple implicit relationships between the same pair (unsupported)

		[Test]
		public void MultipleImplicitPair_Throws([EFDataSources] string provider)
		{
			using var ctx = CreateContext(provider);

			var ex = Assert.Catch(() => ctx.Docs.Where(d => d.PrimaryLabels.Any()).ToLinqToDB().ToList());

			Assert.That(ex!.ToString(), Does.Contain("implicit many-to-many"));
		}

		#endregion

		private void AssertLoaded<T>(List<T> ef, List<T> l2db, Func<T, object> key, Func<T, IEnumerable<int>> collection)
		{
			Assert.That(l2db, Has.Count.EqualTo(ef.Count));

			using (Assert.EnterMultipleScope())
			{
				for (var i = 0; i < ef.Count; i++)
				{
					Assert.That(key(l2db[i]), Is.EqualTo(key(ef[i])));
					Assert.That(collection(l2db[i]).OrderBy(x => x), Is.EqualTo(collection(ef[i]).OrderBy(x => x)));
				}
			}
		}
	}
}
#endif
