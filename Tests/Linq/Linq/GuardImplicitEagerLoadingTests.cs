using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class GuardImplicitEagerLoadingTests : TestBase
	{
		[Test]
		public void Explicit_LoadWith_Allowed([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { GuardImplicitEagerLoading = true });

			var result = db.Parent
				.LoadWith(p => p.Children)
				.OrderBy(p => p.ParentID)
				.ToList();

			result.ShouldNotBeEmpty();
			result.ForEach(p => p.Children.ShouldNotBeNull());
		}

		[Test]
		public void Implicit_Select_Collection_Throws([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { GuardImplicitEagerLoading = true });

			var query = from p in db.Parent
						select new { p.ParentID, p.Children };

			var ex = Assert.Throws<LinqToDBException>(() => query.ToList());
			ex.Message.ShouldContain("LoadWith");
		}

		[Test]
		public void Implicit_OptionOff_NoThrow([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);

			var query = from p in db.Parent
						select new { p.ParentID, p.Children };

			Assert.DoesNotThrow(() => query.ToList());
		}

		[Test]
		public void Explicit_ThenLoad_Allowed([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { GuardImplicitEagerLoading = true });

			var result = db.Parent
				.LoadWith(p => p.Children)
				.ThenLoad(c => c.GrandChildren)
				.OrderBy(p => p.ParentID)
				.ToList();

			result.ShouldNotBeEmpty();
			foreach (var p in result)
			{
				p.Children.ShouldNotBeNull();
				p.Children.ForEach(c => c.GrandChildren.ShouldNotBeNull());
			}
		}

		[Test]
		public void Explicit_WithLoadStrategyMarkers_Allowed([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { GuardImplicitEagerLoading = true });

			// Exactly the projection Implicit_Select_Collection_Throws rejects — adding a per-query
			// With*LoadStrategy marker opts the whole query into eager loading, so the guard must be skipped.
			var queryUnion = (from p in db.Parent
							  orderby p.ParentID
							  select new { p.ParentID, p.Children })
				.WithUnionLoadStrategy();

			Assert.DoesNotThrow(() => queryUnion.ToList());

			var queryKeyed = (from p in db.Parent
							  orderby p.ParentID
							  select new { p.ParentID, p.Children })
				.WithKeyedLoadStrategy();

			Assert.DoesNotThrow(() => queryKeyed.ToList());

			var querySeparate = (from p in db.Parent
								 orderby p.ParentID
								 select new { p.ParentID, p.Children })
				.WithSeparateLoadStrategy();

			Assert.DoesNotThrow(() => querySeparate.ToList());
		}

		[Test]
		public void Explicit_LoadWith_ComplexSelect_NestedCollection_Allowed([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with { GuardImplicitEagerLoading = true });

			// The load-function projects Children into new Child instances that include their GrandChildren
			// association — a nested collection inside the load-function's Select.  The guard must not throw
			// because the entire sub-tree is inside an ExplicitEagerLoad marker placed by LoadWith.
			var result = db.Parent
				.LoadWith(p => p.Children, ch => ch.Select(c => new Child
				{
					ParentID     = c.ParentID,
					ChildID      = c.ChildID,
					GrandChildren = c.GrandChildren,
				}))
				.OrderBy(p => p.ParentID)
				.ToList();

			result.ShouldNotBeEmpty();
			foreach (var p in result)
			{
				p.Children.ShouldNotBeNull();
				p.Children.ForEach(c => c.GrandChildren.ShouldNotBeNull());
			}
		}

		[Test]
		public void GlobalDefaultStrategy_Plus_Option_StillThrows([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db   = GetDataContext(context);
			using var _opt = db.UseLinqOptions(o => o with
			{
				GuardImplicitEagerLoading      = true,
				DefaultEagerLoadingStrategy    = EagerLoadingStrategy.CteUnion,
			});

			// GlobalDefaultStrategy alone does not satisfy GuardImplicitEagerLoading — an explicit
			// per-query marker (With*LoadStrategy) or LoadWith is still required.
			var query = from p in db.Parent
						select new { p.ParentID, p.Children };

			var ex = Assert.Throws<LinqToDBException>(() => query.ToList());
			ex.Message.ShouldContain("LoadWith");
		}
	}
}
