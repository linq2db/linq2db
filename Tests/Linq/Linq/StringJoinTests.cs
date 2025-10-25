extern alias MySqlConnector;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using MySqlConnector::MySqlConnector;

using NUnit.Framework;

namespace Tests.Linq
{
	public class StringJoinTests : TestBase
	{
		const string SupportedProviders = TestProvName.AllPostgreSQL + "," + TestProvName.AllSqlServer2017Plus + "," + TestProvName.AllSQLite + "," + TestProvName.AllMySql + "," + TestProvName.AllClickHouse;

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
		public void JoinWithGrouping([IncludeDataSources(true, SupportedProviders)] string context)
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

		[ActiveIssue(Configuration = TestProvName.AllSqlServer2016Plus, Details = "SQL Server limitation for single select")]
		[Test]
		public void JoinWithGroupingVarious([IncludeDataSources(true, SupportedProviders)] string context)
		{
			var       data  = SampleClass.GenerateDataUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query = from t in table
				group t by t.Id
				into g
				select new
				{
					Id                                     = g.Key,

					NullableDistinct                       = string.Join(", ", g.Select(x => x.NullableValue).Distinct()),
					NullableDistinctNotNullDistinct        = string.Join(", ", g.Select(x => x.NullableValue).Where(x => x != null).Distinct()),
					NullableDistinctNotNullDistinctOrdered = string.Join(", ", g.Select(x => x.NullableValue).Where(x => x != null).Distinct().OrderByDescending(x => x)),

					NotNullableDistinct                    = string.Join(", ", g.Select(x => x.NotNullableValue).Distinct()),
					NotNullableDistinctOrdered             = string.Join(", ", g.Select(x => x.NotNullableValue).Distinct().OrderByDescending(x => x)),
				}
				into s
				orderby s.Id
				select s;

			AssertQuery(query);
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllMariaDB, TestProvName.AllMySql57], ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[RequiresCorrelatedSubquery]
		[Test]
		public void JoinWithGroupingAndUnsupportedMethod([IncludeDataSources(true, SupportedProviders)] string context)
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

		[ActiveIssue(Configuration = TestProvName.AllSqlServer2016Plus, Details = "SQL Server limitation for single select")]
		[RequiresCorrelatedSubquery]
		[Test]
		public void JoinWithGroupingOrdered([IncludeDataSources(true, SupportedProviders)] string context)
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

					NotNullableeOrderedCustom = string.Join(", ", g
						.OrderBy(x => x.NullableValue == null ? 0 : 1)
						.ThenByDescending(x => x.NotNullableValue)
							.ThenBy(x => x.NullableValue)
						.Select(x => x.NotNullableValue)),
				}
				into s
				orderby s.Id
				select s;

			AssertQuery(query);
		}

		[ActiveIssue(Configuration = TestProvName.AllSqlServer2016Plus, Details = "SQL Server limitation for single select")]
		[Test]
		public void JoinWithGroupingOrderSimple([IncludeDataSources(true, SupportedProviders)] string context)
		{
			var       data  = SampleClass.GenerateDataNotUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query = from t in table
				group t by t.Id
				into g
				select new
				{
					Id = g.Key,

					NotNullableeOrderedNoNulls = string.Join(", ", g
						.Where(x => x.NullableValue != null)
						.OrderBy(x => x.NullableValue)
						.ThenBy(x => x.Id)
						.Select(x => x.NullableValue)),

					NotNullableeOrderedNulls = string.Join(", ", g
						.OrderBy(x => x.NullableValue)
						.Select(x => x.NullableValue)),
				}
				into s
				orderby s.Id
				select s;

			AssertQuery(query);
		}

		[ActiveIssue(Configuration = TestProvName.AllSqlServer2016Plus, Details = "SQL Server limitation for single select")]
		[Test]
		public void JoinWithGroupingDistinctSimple([IncludeDataSources(true, SupportedProviders)] string context)
		{
			var       data  = SampleClass.GenerateDataNotUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query = from t in table
				group t by t.Id
				into g
				select new
				{
					Id = g.Key,

					NotNullableOrderedNoNulls = string.Join(", ", g
						.Where(x => x.NullableValue != null)
						.OrderBy(x => x.NullableValue)
						.ThenBy(x => x.Id)
						.Select(x => x.NullableValue)
						.Distinct()),

					NotNullableOrderedNulls = string.Join(", ", g
						.Select(x => x.NullableValue ?? "")
						.OrderBy(x => x)
						.Distinct()),
				}
				into s
				orderby s.Id
				select s;

			AssertQuery(query);
		}

		[Test]
		public void JoinAggregateExecuteNullable([IncludeDataSources(true, SupportedProviders)] string context)
		{
			var       data  = SampleClass.GenerateDataUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var allAggregated = table.AggregateExecute(e => string.Join(", ", e.OrderBy(x => x.NotNullableValue).Select(x => x.NullableValue)));
			var expected      = string.Join(", ", data.OrderBy(x => x.NotNullableValue).Select(x => x.NullableValue));

			Assert.That(allAggregated, Is.EqualTo(expected));
		}

		[Test]
		public void JoinAggregateExecuteNullableOnlyNotNull([IncludeDataSources(true, SupportedProviders)] string context)
		{
			var       data  = SampleClass.GenerateDataUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var allAggregated = table.AggregateExecute(e => string.Join(", ", e.OrderBy(x => x.NotNullableValue).Where(e => e.NullableValue != null).Select(x => x.NullableValue)));
			var expected      = string.Join(", ", data.OrderBy(x => x.NotNullableValue).Where(e => e.NullableValue != null).Select(x => x.NullableValue));

			Assert.That(allAggregated, Is.EqualTo(expected));
		}

		[ActiveIssue(Configuration = TestProvName.AllSqlServer2016Plus, Details = "SQL Server limitation for single select")]
		[ThrowsForProvider(typeof(MySqlException), providers: [TestProvName.AllMariaDB, TestProvName.AllMySql57], ErrorMessage = "Unknown table 't'")]
		[RequiresCorrelatedSubquery]
		[Test]
		public void JoinAggregateArray([IncludeDataSources(true, SupportedProviders)] string context)
		{
			var       data  = SampleClass.GenerateDataUniquerId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query = 
				from t in table
				select new
				{
					Aggregated = Sql.AsSql(string.Join(", ", new [] {t.NullableValue, t.NotNullableValue, t.VarcharValue, t.NVarcharValue})),

					AggregatedNotNullFilteredDistinct = Sql.AsSql(string.Join(", ", new [] {t.NotNullableValue, t.NotNullableValue, t.NotNullableValue, t.NVarcharValue}
						.Where(x=> x != null)
						.Distinct()
						.OrderBy(x => x))),
						
					AggregatedFilteredDistinct = Sql.AsSql(string.Join(", ", new [] {t.NotNullableValue, t.NotNullableValue, t.NotNullableValue, t.NVarcharValue}
						.Where(x=> x != "A")
						.Distinct()
						.OrderBy(x => x == null ? 0 : 1)
						.ThenBy(x => x))),
				};

			AssertQuery(query);
		}

		[ThrowsForProvider(typeof(MySqlException), providers: [TestProvName.AllMariaDB, TestProvName.AllMySql57], ErrorMessage = "Unknown table 't'")]
		[RequiresCorrelatedSubquery]
		[Test]
		public void JoinAggregateArrayNotNull([IncludeDataSources(true, SupportedProviders)] string context)
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
		public void JoinAggregateArrayNotNullAndFilter([IncludeDataSources(true, SupportedProviders)] string context)
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
