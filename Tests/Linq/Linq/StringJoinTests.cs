using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	public class StringJoinTests : TestBase
	{
		[Table]
		sealed class SampleClass
		{
			[Column]                                                              public int     Id               { get; set; }
			[Column(Length = 50, CanBeNull = true)]                               public string? NullableValue    { get; set; }
			[Column(Length = 50, CanBeNull = false)]                              public string  NotNullableValue { get; set; } = string.Empty;
			[Column(Length = 50, CanBeNull = true, DataType = DataType.VarChar)]  public string? VarcharValue     { get; set; }
			[Column(Length = 50, CanBeNull = true, DataType = DataType.NVarChar)] public string? NVarcharValue    { get; set; }

			public static SampleClass[] GenerateDataUniquerId()
			{
				var data = new[]
				{
					new SampleClass { Id = 1, NullableValue = "A", NotNullableValue = "B", VarcharValue = "C", NVarcharValue = "D" },
					new SampleClass { Id = 2, NullableValue = "E", NotNullableValue = "F", VarcharValue = "G", NVarcharValue = "H" },
					new SampleClass { Id = 3, NullableValue = "I", NotNullableValue = "J", VarcharValue = "K", NVarcharValue = "L" },
					new SampleClass { Id = 4, NullableValue = null, NotNullableValue = "M", VarcharValue = null, NVarcharValue = null },
				};
				return data;
			}

			public static SampleClass[] GenerateDataNotUniquerId()
			{
				var data = new[]
				{
					new SampleClass { Id = 1, NullableValue = "A", NotNullableValue  = "B", VarcharValue = "C", NVarcharValue  = "D" },
					new SampleClass { Id = 1, NullableValue = "E", NotNullableValue  = "F", VarcharValue = "G", NVarcharValue  = "H" },
					new SampleClass { Id = 2, NullableValue = "I", NotNullableValue  = "J", VarcharValue = "K", NVarcharValue  = "L" },
					new SampleClass { Id = 2, NullableValue = null, NotNullableValue = "M", VarcharValue = null, NVarcharValue = null },
				};
				return data;
			}
		}

		[Test]
		public void JoinWithGrouping([IncludeDataSources(true, TestProvName.PostgreSQL16)] string context)
		{
			var       data  = SampleClass.GenerateDataUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query = from t in table
				group t by t.Id
				into g
				select new
				{
					Id = g.Key, 
					Nullable = string.Join(", ", g.Select(x => x.NullableValue)),
					NotNullable = string.Join(", ", g.Select(x => x.NotNullableValue)),
				}
				into s
				orderby s.Id
				select s;

			AssertQuery(query);
		}

		[Test]
		public void JoinWithGroupingAndUnsupportedMethod([IncludeDataSources(true, TestProvName.PostgreSQL16)] string context)
		{
			var       data  = SampleClass.GenerateDataUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query = from t in table
				group t by t.Id
				into g
				select new
				{
					Id          = g.Key, 
					Nullable    = string.Join(", ", g.OrderBy(x => x.NotNullableValue).Select(x => x.NullableValue).Take(2)),
					NotNullable = string.Join(", ", g.OrderBy(x => x.NotNullableValue).Select(x => x.NotNullableValue).Take(2)),
				}
				into s
				orderby s.Id
				select s;

			AssertQuery(query);
		}

		[Test]
		public void JoinWithGroupingOrdered([IncludeDataSources(true, TestProvName.PostgreSQL16)] string context)
		{
			var       data  = SampleClass.GenerateDataNotUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			string?[] values = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", null, null, null, null, "", "" };

			var query = from t in table
				group t by t.Id
				into g
				select new
				{
					Id          = g.Key,
					
					Nullable    = string.Join(", ", g
						.OrderBy(x => x.NotNullableValue)
						.ThenByDescending(x => x.NullableValue)
						.Select(x => x.NullableValue)
					),

					NullableFiltered = string.Join(", ", g
						.OrderBy(x => x.NotNullableValue)
						.ThenByDescending(x => x.NullableValue)
						.Select(x => x.NullableValue)
						.Where(x => x != null && x != "")
					),

                    NotNullable = string.Join(", ", g
						.OrderByDescending(x => x.NotNullableValue)
						.ThenByDescending(x => x.NullableValue)
						.Select(x => x.NotNullableValue)),
						

					NullableDoubleOrder    = string.Join(", ", g
						.OrderBy(x => x.NotNullableValue)
						.ThenByDescending(x => x.NullableValue)
						.OrderByDescending(x => x.NotNullableValue)
						.Select(x => x.NullableValue)
					),
					NotNullableDoubleOrder = string.Join(", ", g
						.OrderByDescending(x => x.NullableValue)
						.ThenByDescending(x => x.NotNullableValue)
						.OrderByDescending(x => x.NotNullableValue)
						.Select(x => x.NotNullableValue)),
				}
				into s
				orderby s.Id
				select s;

			AssertQuery(query);
		}

		[Test]
		public void JoinAggregateExecuteNullable([IncludeDataSources(true, TestProvName.PostgreSQL16)] string context)
		{
			var       data  = SampleClass.GenerateDataUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var allAggregated = table.AggregateExecute(e => string.Join(", ", e.OrderBy(x => x.NotNullableValue).Select(x => x.NullableValue)));
			var expected      = string.Join(", ", data.OrderBy(x => x.NotNullableValue).Select(x => x.NullableValue));

			Assert.That(allAggregated, Is.EqualTo(expected));
		}

		[Test]
		public void JoinAggregateExecuteNullableOnlyNotNull([IncludeDataSources(true, TestProvName.PostgreSQL16)] string context)
		{
			var       data  = SampleClass.GenerateDataUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var allAggregated = table.AggregateExecute(e => string.Join(", ", e.OrderBy(x => x.NotNullableValue).Where(e => e.NullableValue != null).Select(x => x.NullableValue)));
			var expected      = string.Join(", ", data.OrderBy(x => x.NotNullableValue).Where(e => e.NullableValue != null).Select(x => x.NullableValue));

			Assert.That(allAggregated, Is.EqualTo(expected));
		}

		[Test]
		public void JoinAggregateArray([IncludeDataSources(true, TestProvName.PostgreSQL16)] string context)
		{
			var       data  = SampleClass.GenerateDataUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query = 
				from t in table
				select new
				{
					//Aggregated = Sql.AsSql(string.Join(", ", new [] {t.NullableValue, t.NotNullableValue, t.VarcharValue, t.NVarcharValue})),
					AggregatedDistinct = Sql.AsSql(string.Join(", ", new [] {t.NotNullableValue, t.NotNullableValue, t.NotNullableValue, t.NVarcharValue}
						.Where(x=> x != null)
						.Distinct()
						.OrderBy(x => x))),
				};

			AssertQuery(query);
		}

		[Test]
		public void JoinAggregateArrayNotNull([IncludeDataSources(true, TestProvName.PostgreSQL16)] string context)
		{
			var       data  = SampleClass.GenerateDataUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				select new
				{
					NotNullValue = Sql.AsSql(string.Join(", ", new[] { t.NullableValue, t.NotNullableValue, t.VarcharValue, t.NVarcharValue }.Where(x => x != null))),
					NotNullDistinctValue = Sql.AsSql(string.Join(", ", new[] { t.NullableValue, t.NotNullableValue, t.VarcharValue, t.NVarcharValue }.Where(x => x != null).Distinct().OrderBy(x => x)))
				};

			AssertQuery(query);
		}

		[Test]
		public void JoinAggregateArrayNotNullAndFilter([IncludeDataSources(true, TestProvName.PostgreSQL16)] string context)
		{
			var       data  = SampleClass.GenerateDataUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				select Sql.AsSql(string.Join(", ", new[] { t.NullableValue, t.NotNullableValue, t.VarcharValue, t.NVarcharValue }.Where(x => x != null).Where(x => x!.Contains("A"))));

			AssertQuery(query);
		}
	}
}
