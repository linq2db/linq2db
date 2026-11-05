using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	// Values read through a closure object, a static member of a user type or a collection element are
	// run-time inputs: they stay parameters and never decide the SQL at build time. The cache key is the
	// query tree with those inputs taken out, so a changed input is seen by the same cached query.
	[TestFixture]
	public class CachePrincipleTests : TestBase
	{
		static readonly int[]          _foldedElements = [2];
		static readonly List<int>      _foldedList     = [1];
		static readonly FoldedSettings _foldedSettings = new() { Enabled = true };

		sealed class FoldedSettings
		{
			public bool Enabled;
		}

		sealed class FoldedHolder
		{
			public int Value { get; init; }
		}

		static Expression<Func<Model.Person, bool>> IdIsTwoWhen(Expression condition)
		{
			var p = Expression.Parameter(typeof(Model.Person), "p");

			return Expression.Lambda<Func<Model.Person, bool>>(
				Expression.AndAlso(
					condition,
					Expression.Equal(Expression.Field(p, nameof(Model.Person.ID)), Expression.Constant(2))),
				p);
		}

		static void ShouldHitCacheOnRebuild<TArg>(Func<TArg, IQueryable<int>> build, TArg first, int firstResult, TArg second, int secondResult)
		{
			var query = build(first);

			query
				.ToArray()
				.ShouldBe([firstResult]);

			var missCount = query.GetCacheMissCount();

			build(second)
				.ToArray()
				.ShouldBe([secondResult]);

			query
				.GetCacheMissCount()
				.ShouldBe(missCount);
		}

		[Test, QueryCacheTest]
		public void StaticArrayElementSeesMutation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query()
			{
				return db.Person
					.Where(p => _foldedElements[0] > 0 && p.ID == 2)
					.Select(p => p.ID);
			}

			try
			{
				Query().ToArray().ShouldBe([2]);

				var missCount = Query().GetCacheMissCount();

				_foldedElements[0] = 0;

				Query().ToArray().ShouldBeEmpty();

				// the element is a parameter: the same cached query serves both values
				Query().GetCacheMissCount().ShouldBe(missCount);
			}
			finally
			{
				_foldedElements[0] = 2;
			}
		}

		[Test, QueryCacheTest]
		public void StaticFieldSeesMutation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query()
			{
				return db.Person
					.Where(p => _foldedSettings.Enabled && p.ID == 2)
					.Select(p => p.ID);
			}

			try
			{
				Query().ToArray().ShouldBe([2]);

				var missCount = Query().GetCacheMissCount();

				_foldedSettings.Enabled = false;

				Query().ToArray().ShouldBeEmpty();

				Query().GetCacheMissCount().ShouldBe(missCount);
			}
			finally
			{
				_foldedSettings.Enabled = true;
			}
		}

		[Test, QueryCacheTest]
		public void StaticListCountSeesMutation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query()
			{
				return db.Person
					.Where(p => _foldedList.Count > 0 && p.ID == 2)
					.Select(p => p.ID);
			}

			try
			{
				Query().ToArray().ShouldBe([2]);

				_foldedList.Clear();

				Query().ToArray().ShouldBeEmpty();
			}
			finally
			{
				_foldedList.Clear();
				_foldedList.Add(1);
			}
		}

		[Test, QueryCacheTest]
		public void ConstantMemberSeesNewInstance([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(FoldedHolder holder)
			{
				var condition = Expression.GreaterThan(
					Expression.Property(Expression.Constant(holder), nameof(FoldedHolder.Value)),
					Expression.Constant(0));

				return db.Person
					.Where(IdIsTwoWhen(condition))
					.Select(p => p.ID);
			}

			var query = Query(new FoldedHolder { Value = 1 });

			query.ToArray().ShouldBe([2]);

			var missCount = query.GetCacheMissCount();

			Query(new FoldedHolder { Value = 0 }).ToArray().ShouldBeEmpty();

			query.GetCacheMissCount().ShouldBe(missCount);
		}

		static IQueryable<int> ElementQuery(Model.ITestDataContext db, int[] ids)
		{
			var condition = Expression.GreaterThan(
				Expression.ArrayIndex(Expression.Constant(ids), Expression.Constant(0)),
				Expression.Constant(0));

			return db.Person
				.Where(IdIsTwoWhen(condition))
				.Select(p => p.ID);
		}

		[Test, QueryCacheTest]
		public void ConstantArrayElementSeesMutation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var ids   = new[] { 2 };
			var query = ElementQuery(db, ids);

			query.ToArray().ShouldBe([2]);

			var missCount = query.GetCacheMissCount();

			ids[0] = 0;

			ElementQuery(db, ids).ToArray().ShouldBeEmpty();

			query.GetCacheMissCount().ShouldBe(missCount);
		}

		[Test, QueryCacheTest]
		public void ConstantArrayElementHitsOnEqualArray([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			ShouldHitCacheOnRebuild(ids => ElementQuery(db, ids), new[] { 2 }, 2, new[] { 2 }, 2);
		}

		static IQueryable<int> ElementsQuery(Model.ITestDataContext db, int[] ids)
		{
			var c = Expression.Constant(ids);

			var condition = Expression.AndAlso(
				Expression.GreaterThan(Expression.ArrayIndex(c, Expression.Constant(0)), Expression.Constant(0)),
				Expression.GreaterThan(Expression.ArrayIndex(c, Expression.Constant(1)), Expression.Constant(0)));

			return db.Person
				.Where(IdIsTwoWhen(condition))
				.Select(p => p.ID);
		}

		[Test, QueryCacheTest]
		public void ConstantArrayElementsSeeMutation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var ids   = new[] { 1, 2 };
			var query = ElementsQuery(db, ids);

			query.ToArray().ShouldBe([2]);

			var missCount = query.GetCacheMissCount();

			ids[1] = 0;

			ElementsQuery(db, ids).ToArray().ShouldBeEmpty();

			query.GetCacheMissCount().ShouldBe(missCount);
		}

		[Test, QueryCacheTest]
		public void ConstantArrayElementsHitOnEqualArray([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			ShouldHitCacheOnRebuild(ids => ElementsQuery(db, ids), new[] { 1, 2 }, 2, new[] { 1, 2 }, 2);
		}

		[Test, QueryCacheTest]
		public void ConstantArrayElementBesideEqualArray([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(int[] folded, int[] used)
			{
				var p = Expression.Parameter(typeof(Model.Person), "p");

				var predicate = Expression.Lambda<Func<Model.Person, bool>>(
					Expression.AndAlso(
						Expression.GreaterThan(
							Expression.Call(
								typeof(Sql).GetMethod(nameof(Sql.AsSql))!.MakeGenericMethod(typeof(int)),
								Expression.ArrayIndex(Expression.Constant(folded), Expression.Constant(0))),
							Expression.Constant(0)),
						Expression.Equal(
							Expression.Field(p, nameof(Model.Person.ID)),
							Expression.ArrayIndex(Expression.Constant(used), Expression.Constant(0)))),
					p);

				return db.Person
					.Where(predicate)
					.Select(p => p.ID);
			}

			var folded = new[] { 2 };
			var used   = new[] { 2 };

			Query(folded, used).ToArray().ShouldBe([2]);

			folded[0] = 0;

			Query(folded, used).ToArray().ShouldBeEmpty();
		}

		[Test, QueryCacheTest]
		public void PrunedStaticFieldKeepsCachedQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query()
			{
				return db.Person
					.Select(p => new { p.ID, Flag = _foldedSettings.Enabled })
					.Select(x => x.ID)
					.Where(id => id == 2);
			}

			try
			{
				var query = Query();

				query.ToArray().ShouldBe([2]);

				var missCount = query.GetCacheMissCount();

				_foldedSettings.Enabled = false;

				Query().ToArray().ShouldBe([2]);

				query.GetCacheMissCount().ShouldBe(missCount);
			}
			finally
			{
				_foldedSettings.Enabled = true;
			}
		}

		[Test, QueryCacheTest]
		public void ConstantArrayNullCheckHitsOnEqualArray([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(int[]? ids)
			{
				var condition = Expression.NotEqual(
					Expression.Constant(ids, typeof(int[])),
					Expression.Constant(null, typeof(int[])));

				return db.Person
					.Where(IdIsTwoWhen(condition))
					.Select(p => p.ID);
			}

			ShouldHitCacheOnRebuild(Query, new[] { 1, 2 }, 2, new[] { 1, 2 }, 2);

			Query(null).ToArray().ShouldBeEmpty();
		}

		[Test, QueryCacheTest]
		public void ConstantArrayLength([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(int[] ids)
			{
				var condition = Expression.GreaterThan(
					Expression.ArrayLength(Expression.Constant(ids)),
					Expression.Constant(0));

				return db.Person
					.Where(IdIsTwoWhen(condition))
					.Select(p => p.ID);
			}

			ShouldHitCacheOnRebuild(Query, new[] { 1, 2 }, 2, new[] { 1, 2 }, 2);

			Query([]).ToArray().ShouldBeEmpty();
		}

		// A scalar literal is part of the tree: a different literal is a different query.
		[Test, QueryCacheTest]
		public void ScalarLiteralIsPartOfTheKey([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(int value)
			{
				var condition = Expression.GreaterThan(Expression.Constant(value), Expression.Constant(0));

				return db.Person
					.Where(IdIsTwoWhen(condition))
					.Select(p => p.ID);
			}

			var query = Query(5);

			query.ToArray().ShouldBe([2]);

			var missCount = query.GetCacheMissCount();

			Query(-1).ToArray().ShouldBeEmpty();

			query.GetCacheMissCount().ShouldBe(missCount + 1);
		}

		[Test, QueryCacheTest]
		public void ScalarNullCheckKeepsConstantInKey([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(bool flag)
			{
				var p     = Expression.Parameter(typeof(Model.Person), "p");
				var c     = Expression.Constant(flag, typeof(bool?));
				var isOne = Expression.Equal(Expression.PropertyOrField(p, nameof(Model.Person.ID)), Expression.Constant(1));

				var body = Expression.OrElse(
					Expression.OrElse(
						Expression.Equal(c, Expression.Constant(null, typeof(bool?))),
						Expression.AndAlso(Expression.Property(c, nameof(Nullable<>.Value)), isOne)),
					Expression.AndAlso(Expression.Not(Expression.Property(c, nameof(Nullable<>.Value))), Expression.Not(isOne)));

				return db.Person
					.Where(Expression.Lambda<Func<Model.Person, bool>>(body, p))
					.Select(p => p.ID);
			}

			Query(false).ToArray().ShouldNotContain(1);
			Query(true).ToArray().ShouldBe([1]);
		}

		[Test, QueryCacheTest]
		public void ScalarBesideSurvivingValueKeepsConstantInKey([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(int? value)
			{
				var p  = Expression.Parameter(typeof(Model.Person), "p");
				var c  = Expression.Constant(value, typeof(int?));
				var id = Expression.PropertyOrField(p, nameof(Model.Person.ID));

				var body = Expression.OrElse(
					Expression.Equal(id, Expression.Coalesce(c, Expression.Constant(2))),
					Expression.AndAlso(Expression.Property(c, nameof(Nullable<>.HasValue)), Expression.Equal(id, Expression.Constant(3))));

				return db.Person
					.Where(Expression.Lambda<Func<Model.Person, bool>>(body, p))
					.Select(p => p.ID);
			}

			Query(2).OrderBy(id => id).ToArray().ShouldBe([2, 3]);
			Query(null).OrderBy(id => id).ToArray().ShouldBe([2]);
		}

		sealed class BoolLiteralBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				builder.ResultExpression = new SqlValue(builder.GetValue<bool>(0));
			}
		}

		[Sql.Extension("", BuilderType = typeof(BoolLiteralBuilder), ServerSideOnly = true)]
		static bool BoolLiteral(bool value) => value;

		static readonly MethodInfo _boolLiteral = typeof(CachePrincipleTests).GetMethod(nameof(BoolLiteral), BindingFlags.NonPublic | BindingFlags.Static)!;

		[Test, QueryCacheTest]
		public void ScalarBesideExtensionArgumentKeepsCompare([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(bool flag)
			{
				var p     = Expression.Parameter(typeof(Model.Person), "p");
				var c     = Expression.Constant(flag, typeof(bool?));
				var isOne = Expression.Equal(Expression.PropertyOrField(p, nameof(Model.Person.ID)), Expression.Constant(1));

				var body = Expression.AndAlso(
					Expression.Not(Expression.Call(_boolLiteral, Expression.Equal(c, Expression.Constant(null, typeof(bool?))))),
					Expression.OrElse(
						Expression.AndAlso(Expression.Property(c, nameof(Nullable<>.Value)), isOne),
						Expression.AndAlso(Expression.Not(Expression.Property(c, nameof(Nullable<>.Value))), Expression.Not(isOne))));

				return db.Person
					.Where(Expression.Lambda<Func<Model.Person, bool>>(body, p))
					.Select(p => p.ID);
			}

			Query(false).ToArray().ShouldNotContain(1);
			Query(true).ToArray().ShouldBe([1]);
		}

		// A closure variable in a bool chain: parameter, one cached query for every value.
		[Test, QueryCacheTest]
		public void ClosureFlagIsParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(bool flag)
			{
				return db.Person
					.Where(p => flag && p.ID == 2)
					.Select(p => p.ID);
			}

			ShouldHitCacheOnRebuild(Query, true, 2, true, 2);

			Query(false).ToArray().ShouldBeEmpty();
		}
	}
}
