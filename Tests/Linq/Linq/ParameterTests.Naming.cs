using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class ParameterTests
	{
		// A parameter carries a value, so it reads better named after where that value comes from than
		// after the column it is compared against. When the expression is not itself a member access, the
		// walk follows the value's own spine - unary operators, the array or container of an element read,
		// the target of a parameterless GetValueOrDefault - and takes the first member access it reaches.
		// A method call that computes a new value is named exactly as it was before, which may be the
		// column name or the generic fallback depending on what the call site carries.
		//
		// Assertions go against DataParameter.Name rather than the SQL text: the name carries no provider
		// prefix there, and a substring check over the SQL would match "@p" inside "@price".

		[Test]
		public void ParameterName_FromArrayBeingIndexed([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var values = new[] { "str", "str1" };

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == values[0] || t.String3 == values[1])
				.ToSqlQuery();

			sql.Parameters.Select(p => p.Name).ShouldBe(["values", "values_1"]);
		}

		[Test]
		public void ParameterName_FromNullableTarget([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			int? value = 1;

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == value.GetValueOrDefault())
				.ToSqlQuery();

			sql.Parameters.Select(p => p.Name).ShouldBe(["value"]);
		}

		[Test]
		public void ParameterName_NullableTargetWithExplicitDefault([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			int? value = null;

			// This overload can return the argument rather than the target's value - naming the parameter
			// after `value` would describe something it does not hold, so the target's name is not used.
			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == value.GetValueOrDefault(7))
				.ToSqlQuery();

			// Checked as a prefix rather than an exact sequence: the target's name must not appear at all,
			// including as "value_1" after uniquification or alongside a parameter added later. The
			// non-empty check keeps ShouldAllBe from passing vacuously if the value stops being a parameter.
			sql.Parameters.ShouldNotBeEmpty();
			sql.Parameters.Select(p => p.Name).ShouldAllBe(n => !n!.StartsWith("value", StringComparison.Ordinal));
		}

		[Test]
		public void ParameterName_FromIndexedCollection([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var names = new List<string> { "str" };

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == names[0])
				.ToSqlQuery();

			sql.Parameters.Select(p => p.Name).ShouldBe(["names"]);
		}

		[Test]
		public void ParameterName_ComputingCallIsNotNamedAfterTarget([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var text = "st";

			// Trim returns a new value, so naming the parameter after `text` would describe the wrong thing.
			// Such calls are left exactly as they were before source-based naming existed - here that means
			// the generic fallback, since this call site carries no column descriptor to name after either.
			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == text.Trim())
				.ToSqlQuery();

			sql.Parameters.Select(p => p.Name).ShouldBe(["p"]);
		}

		[Test]
		public void ParameterName_FromRenamedIndexerTarget([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var text = "sx";

			// string declares its indexer as [IndexerName("Chars")], so text[0] is a get_Chars call and not
			// get_Item - a real indexer read that a test matching on the accessor's name would reject.
			var sql = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == text[0])
				.ToSqlQuery();

			sql.Parameters.Select(p => p.Name).ShouldBe(["text"]);
		}

		[Test]
		public void ParameterName_FromHandBuiltIndexExpression([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var values = new[] { "str", "str1" };

			// The C# compiler never emits IndexExpression, only a hand-built tree carries one. It is rewritten into
			// the compiler's values[0] shape before naming, so it must be named after the array exactly as that is.
			Expression<Func<string[]>> captured = () => values;

			var entity    = Expression.Parameter(typeof(ParameterDeduplication), "t");
			var predicate = Expression.Lambda<Func<ParameterDeduplication, bool>>(
				Expression.Equal(
					Expression.Property(entity, nameof(ParameterDeduplication.String2)),
					Expression.ArrayAccess(captured.Body, Expression.Constant(0))),
				entity);

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(predicate)
				.ToSqlQuery();

			sql.Parameters.Select(p => p.Name).ShouldBe(["values"]);
		}

		// Naming happens before any provider code; the providers match the fixture's other naming tests.
		[Test]
		public void ParameterName_FromExplicitInterfaceIndexer([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var segment = new ArraySegment<int>([1, 2]);

			// ArraySegment<T> implements IList<T>'s indexer explicitly, so the getter a hand-built index over it reads
			// through is named "System.Collections.Generic.IList<T>.get_Item" rather than "get_Item".
			var indexer = typeof(ArraySegment<int>)
				.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
				.Single(p => p.Name.StartsWith("System.Collections.Generic.IList<", StringComparison.Ordinal));

			Expression<Func<ArraySegment<int>>> captured = () => segment;

			var entity    = Expression.Parameter(typeof(ParameterDeduplication), "t");
			var predicate = Expression.Lambda<Func<ParameterDeduplication, bool>>(
				Expression.Equal(
					Expression.Property(entity, nameof(ParameterDeduplication.Int1)),
					Expression.MakeIndex(captured.Body, indexer, [Expression.Constant(0)])),
				entity);

			var sql = db.GetTable<ParameterDeduplication>()
				.Where(predicate)
				.ToSqlQuery();

			sql.Parameters
				.Select(p => p.Name)
				.ShouldBe(["segment"]);
		}

		// verifies that the proper SQL parameter name is used when constructing the query
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4254")]
		public void VerifyParameterNameIsCorrect([IncludeDataSources(TestProvName.AllPostgreSQL95Plus, TestProvName.AllSqlServer2008Plus)] string context)
		{
			// prep tables
			{
				using var db = GetDataContext(context);
				// drop tables if they already exist
				db.GetTable<ParamNameItemCategory>().Drop(false);
				db.GetTable<ParamNameItemShare>().Drop(false);
				db.GetTable<ParamNameItem>().Drop(false);
				// create tables
				db.CreateTable<ParamNameItem>();
				db.CreateTable<ParamNameItemShare>();
				db.CreateTable<ParamNameItemCategory>();
			}
			// run tests
			try
			{
				// test 1
				{
					using var db = GetDataContext(context);

					var now = TestData.DateTime;
					var userId = TestData.Guid1;

					var result = GetQuery(db, userId, now)
						.Select(MediaItemSearchSharedResultProjection(now))
						.ToList();
				}

				// test 2 (same query; should also work properly)
				{
					using var db = GetDataContext(context);

					var now = TestData.DateTime3;
					var userId = TestData.Guid2;

					var result = GetQuery(db, userId, now)
						.Select(MediaItemSearchSharedResultProjection(now))
						.ToList();
				}
			}
			finally
			{
				// drop tables
				using var db = GetDataContext(context);
				db.GetTable<ParamNameItemCategory>().Drop(false);
				db.GetTable<ParamNameItemShare>().Drop(false);
				db.GetTable<ParamNameItem>().Drop(false);
			}

			static Expression<Func<ParamNameItem, ParamNameSearchResult>> MediaItemSearchSharedResultProjection(DateTime now)
			{
				return x => new ParamNameSearchResult
				{
					CategoryIds = x.Categories.Select(y => y.CategoryId).ToList(),
					IsUnvisited = x.UserShare.Any(y => y.ExpiresAt > now)
				};
			}

			static IQueryable<ParamNameItem> GetQuery(IDataContext context, Guid userId, DateTime now)
			{
				return context.GetTable<ParamNameItem>()
					.Where(x =>
						x.UserShare.Any(y => y.UserId == userId && y.ExpiresAt > now) ||
						x.UserShare.Any(y => y.CreatedById == userId && y.ExpiresAt > now));
			}
		}

		public sealed class ParamNameSearchResult
		{
			public IList<Guid> CategoryIds { get; set; } = new List<Guid>();

			public bool IsUnvisited { get; set; }
		}

		[Table("param_name_item_categories")]
		public sealed class ParamNameItemCategory
		{
			[Column("id")]
			[PrimaryKey]
			public Guid Id { get; set; }

			[Column("category_id")]
			public Guid CategoryId { get; set; }

			[Column("media_item_id")]
			public Guid MediaItemId { get; set; }
		}

		[Table("param_name_item_shares")]
		public sealed class ParamNameItemShare
		{
			[Column("id")]
			[PrimaryKey]
			public Guid Id { get; set; }

			[Column("media_item_id")]
			public Guid MediaItemId { get; set; }

			[Column("created_by_id")]
			public Guid CreatedById { get; set; }

			[Column("user_id")]
			public Guid UserId { get; set; }

			[Column("expires_at")]
			public DateTime ExpiresAt { get; set; }
		}

		[Table("param_name_items")]
		public sealed class ParamNameItem
		{
			[Column("id")]
			[PrimaryKey]
			public Guid Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ParamNameItemCategory.MediaItemId))]
			public IList<ParamNameItemCategory> Categories { get; set; } = new List<ParamNameItemCategory>();

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ParamNameItemShare.MediaItemId))]
			public IList<ParamNameItemShare> UserShare { get; set; } = new List<ParamNameItemShare>();
		}

		[Table]
		sealed class NamedParameterItem
		{
			[Column("id"), PrimaryKey] public int    Id    { get; set; }
			[Column("value")]          public string Value { get; set; } = null!;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5371")]
		public void DataParameterNamePreserved([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var testData = new[]
			{
				new NamedParameterItem { Id = 1, Value = "ONE" },
				new NamedParameterItem { Id = 2, Value = "two" },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result = table
				.Where(x => Sql.Expr<bool>("LOWER(\"value\") = LOWER({0})", new DataParameter("@p1", "ONE", DataType.NVarChar)))
				.ToArray();

			result.Length.ShouldBe(1);
			result[0].Id.ShouldBe(1);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5371")]
		public void DataParameterNamePreservedMultiple([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var testData = new[]
			{
				new NamedParameterItem { Id = 1, Value = "ONE" },
				new NamedParameterItem { Id = 2, Value = "two" },
				new NamedParameterItem { Id = 3, Value = "three" },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result = table
				.Where(x => Sql.Expr<bool>($"LOWER({Sql.FieldExpr(x.Value)}) = LOWER({new DataParameter("@p1", "ONE", DataType.NVarChar)})") ||
				             Sql.Expr<bool>($"LOWER({Sql.FieldExpr(x.Value)}) = LOWER({new DataParameter("@p2", "two", DataType.NVarChar)})"))
				.OrderBy(x => x.Id)
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Id.ShouldBe(1);
			result[1].Id.ShouldBe(2);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5371")]
		public void DataParameterNameInSqlStatement([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var testData = new[]
			{
				new NamedParameterItem { Id = 1, Value = "ONE" },
				new NamedParameterItem { Id = 2, Value = "two" },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var query = table
				.Where(x => Sql.Expr<bool>($"UPPER({Sql.FieldExpr(x.Value)}) = UPPER({new DataParameter("@p1", "ONE", DataType.NVarChar)})"));

			var parameters = query.GetStatement().CollectParameters();

			parameters.Length.ShouldBe(1);
			parameters[0].Name.ShouldBe("@p1");
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5371")]
		public void DataParameterNullName([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var testData = new[]
			{
				new NamedParameterItem { Id = 1, Value = "ONE" },
				new NamedParameterItem { Id = 2, Value = "two" },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result = table
				.Where(x => Sql.Expr<bool>($"LOWER({Sql.FieldExpr(x.Value)}) = LOWER({new DataParameter(null, "ONE", DataType.NVarChar)})"))
				.ToArray();

			result.Length.ShouldBe(1);
			result[0].Id.ShouldBe(1);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5371")]
		public void DataParameterEmptyName([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var testData = new[]
			{
				new NamedParameterItem { Id = 1, Value = "ONE" },
				new NamedParameterItem { Id = 2, Value = "two" },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result = table
				.Where(x => Sql.Expr<bool>($"LOWER({Sql.FieldExpr(x.Value)}) = LOWER({new DataParameter("", "ONE", DataType.NVarChar)})"))
				.ToArray();

			result.Length.ShouldBe(1);
			result[0].Id.ShouldBe(1);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5371")]
		public void DataParameterNullNameAndExplicitNameMixed([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var testData = new[]
			{
				new NamedParameterItem { Id = 1, Value = "ONE" },
				new NamedParameterItem { Id = 2, Value = "two" },
				new NamedParameterItem { Id = 3, Value = "three" },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result = table
				.Where(x => Sql.Expr<bool>($"LOWER({Sql.FieldExpr(x.Value)}) = LOWER({new DataParameter("@p1", "ONE", DataType.NVarChar)})") ||
				             Sql.Expr<bool>($"LOWER({Sql.FieldExpr(x.Value)}) = LOWER({new DataParameter(null, "two", DataType.NVarChar)})"))
				.OrderBy(x => x.Id)
				.ToArray();

			result.Length.ShouldBe(2);
			result[0].Id.ShouldBe(1);
			result[1].Id.ShouldBe(2);
		}
	}
}
