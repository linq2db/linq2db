using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class ParameterTests
	{
		sealed class SideEffectCounter
		{
			public int Value;
			public int Next() => Value++;
		}

		[Test]
		public void OptimizeDuplicateParameters_ReusesSameLinqParameter([DataSources(TestProvName.AllAccess, TestProvName.AllSapHana, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(true));

			var value = 1;

			var query = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == value || t.Int2 == value);

			query.ToSqlQuery().Parameters.Count.ShouldBe(1);
		}

		[Test]
		public void OptimizeDuplicateParameters_DefaultKeepsCurrentBehavior([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(false));

			var value = "str";

			var query = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String1 == value || t.String2 == value);

			query.ToSqlQuery().Parameters.Count.ShouldBe(2);
		}

		[Test]
		public void OptimizeDuplicateParameters_DoesNotDeduplicate_NonPure_Guid([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(true));

			var query = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == Guid.NewGuid().GetHashCode() || t.Int2 == Guid.NewGuid().GetHashCode());

			query.ToSqlQuery().Parameters.Count.ShouldBe(2);
		}

		[Test]
		public void OptimizeDuplicateParameters_DoesNotDeduplicate_NonPure_Random([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(true));

			var rnd = new Random();
			var query = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == rnd.Next() || t.Int2 == rnd.Next());

			query.ToSqlQuery().Parameters.Count.ShouldBe(2);
		}

		[Test]
		public void OptimizeDuplicateParameters_DoesNotDeduplicate_NonPure_PostIncrement([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(true));

			var c = new SideEffectCounter();
			var query = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == c.Next() || t.Int2 == c.Next());

			query.ToSqlQuery().Parameters.Count.ShouldBe(2);
		}

		[Test]
		public void OptimizeDuplicateParameters_ReusesSameLinqParameter_IntAndNullableIntColumns([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(true));

			int? value = 1;

			var query = db.GetTable<ParameterDeduplication>()
				.Where(t => t.Int1 == value || t.IntN1 == value);

			query.ToSqlQuery().Parameters.Count.ShouldBe(1);
		}

		[Test]
		public void OptimizeDuplicateParameters_DoesNotReuseDifferentValues_String([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(true));

			var value1 = "str";
			var value2 = "str1";

			var query = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == value1 || t.String2 == value2);

			query.ToSqlQuery().Parameters.Count.ShouldBe(2);
		}

		[Test]
		public void OptimizeDuplicateParameters_ReusesSameLinqParameter_NullableInt([DataSources(TestProvName.AllAccess, TestProvName.AllSapHana, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(true));

			int? value = 1;

			var query = db.GetTable<ParameterDeduplication>()
				.Where(t => t.IntN1 == value || t.IntN2 == value);

			query.ToSqlQuery().Parameters.Count.ShouldBe(1);
		}

		[Test]
		public void OptimizeDuplicateParameters_CompareSqlWithAndWithoutOption([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			Issue404? usage = Issue404.Value1;
			var allUsages = !usage.HasValue;

			QuerySql sqlWithoutOptimization;
			using (var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(false)))
			{
				var query = db.GetTable<Table404Two>()
					.Where(v => allUsages || v.Usage == usage.GetValueOrDefault());

				sqlWithoutOptimization = query.ToSqlQuery();
			}

			QuerySql sqlWithOptimization;
			using (var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(true)))
			{
				var query = db.GetTable<Table404Two>()
					.Where(v => allUsages || v.Usage == usage.GetValueOrDefault());

				sqlWithOptimization = query.ToSqlQuery();
			}

			// This test validates parameter name consistency between optimization modes.
			// It is not about duplicate count differences for this query shape.
			//
			sqlWithoutOptimization.Parameters.Count.ShouldBe(1);
			sqlWithOptimization.   Parameters.Count.ShouldBe(1);
			sqlWithoutOptimization.Sql.ShouldContain("@usage");
			sqlWithOptimization.   Sql.ShouldContain("@usage");
			sqlWithOptimization.   Sql.ShouldNotContain("@p");
		}

		[Test]
		public void OptimizeDuplicateParameters_ReusesSameLinqParameter_String([DataSources(TestProvName.AllAccess, TestProvName.AllSapHana, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(true));

			var value = "str";

			var query = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == value || t.String3 == value);

			query.ToSqlQuery().Parameters.Count.ShouldBe(1);
		}

		[Test]
		public void OptimizeDuplicateParameters_ReusesSameLinqParameter_String_Access([IncludeDataSources(TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(true));

			var value = "str";

			var query = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == value || t.String3 == value);

			query.ToSqlQuery().Parameters.Count.ShouldBe(2);
		}

		[Test]
		public void OptimizeDuplicateParameters_UsesMultipleParameters_FromArrayElements([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(true));

			var values = new[] { "str", "str1" };

			var query = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String2 == values[0] || t.String3 == values[1]);

			query.ToSqlQuery().Parameters.Count.ShouldBe(2);
		}

		[Test]
		public void OptimizeDuplicateParameters_DoesNotReuseDifferentColumnDataTypes([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(true));

			var value = "str";

			var query = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String1 == value || t.String2 == value);

			query.ToSqlQuery().Parameters.Count.ShouldBe(2);
		}

		[Test]
		public void OptimizeDuplicateParameters_DoesNotReuseDifferentLinqParameters([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context, o => o.UseOptimizeDuplicateParameters(true));

			var value1 = "str";
			var value2 = "str";

			var query = db.GetTable<ParameterDeduplication>()
				.Where(t => t.String1 == value1 || t.String2 == value2);

			query.ToSqlQuery().Parameters.Count.ShouldBe(2);
		}
	}
}

