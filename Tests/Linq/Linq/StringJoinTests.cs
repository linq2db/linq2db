extern alias MySqlConnector;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;
using LinqToDB.Schema;

using NUnit.Framework;

#pragma warning disable CA1820

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

			public static SampleClass[] GenerateDataUniqueId()
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

			public static SampleClass[] GenerateDataNotUniqueId()
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

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Id), CanBeNull = true)]
			public List<SampleClass> Children { get; set; } = null!;
		}

		[Test]
		public void JoinWithGrouping([DataSources] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
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
		public void JoinWithGroupingParameter([DataSources] string context, [Values(", ", ": ")]string separator)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query = from t in table
				group t by t.Id
				into g
				select new
				{
					Id          = g.Key,
					Nullable    = string.Join(separator, g.Select(x => x.NullableValue)),
					NotNullable = string.Join(separator, g.Select(x => x.NotNullableValue)),
				}
				into s
				orderby s.Id
				select s;

			AssertQuery(query);
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllDB2], ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test]
		public void JoinWithGroupingVarious([DataSources(TestProvName.AllSqlServer2016Plus, TestProvName.AllOracle)] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
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

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllMariaDB, TestProvName.AllMySql57, TestProvName.AllDB2], ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void JoinWithGroupingAndUnsupportedMethod([DataSources(TestProvName.AllOracle)] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
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

		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void JoinWithGroupingOrdered([DataSources(TestProvName.AllSqlServer2016Plus, TestProvName.AllOracle)] string context)
		{
			var       data  = SampleClass.GenerateDataNotUniqueId();
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

		[Test]
		public void JoinWithGroupingOrderSimple([DataSources(TestProvName.AllSqlServer2016Plus, TestProvName.AllOracle, TestProvName.AllDB2)] string context)
		{
			var       data  = SampleClass.GenerateDataNotUniqueId();
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

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllDB2], ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test]
		public void JoinWithGroupingDistinctSimple([DataSources(TestProvName.AllSqlServer2016Plus, TestProvName.AllSapHana, TestProvName.AllOracle, TestProvName.AllSybase)] string context)
		{
			var       data  = SampleClass.GenerateDataNotUniqueId();
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

		[ThrowsCannotBeConverted(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)]
		[Test]
		public void JoinAggregateExecuteNullable([DataSources(TestProvName.AllOracle)] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var allAggregated = table.AggregateExecute(e => string.Join(", ", e.OrderBy(x => x.NotNullableValue).Select(x => x.NullableValue)));
			var expected      = string.Join(", ", data.OrderBy(x => x.NotNullableValue).Select(x => x.NullableValue));

			Assert.That(allAggregated, Is.EqualTo(expected));
		}

		[Test]
		public void JoinAggregateExecuteNullableButFiltered([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var allAggregated = table.AggregateExecute(e => string.Join(", ", e.OrderBy(x => x.NotNullableValue).Select(x => x.NullableValue).Where(x => x != null)));
			var expected      = string.Join(", ", data.OrderBy(x => x.NotNullableValue).Select(x => x.NullableValue).Where(x => x != null));

			Assert.That(allAggregated, Is.EqualTo(expected));
		}

		[ThrowsCannotBeConverted(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)]
		[Test]
		public void JoinAggregateExecuteFiltered([DataSources(true)] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var allAggregated = table.AggregateExecute(e => string.Join(", ", e.OrderBy(x => x.NotNullableValue).Select(x => x.NullableValue).Where(x => x != null && x.In("A", "B"))));
			var expected      = string.Join(", ", data.OrderBy(x => x.NotNullableValue).Select(x => x.NullableValue).Where(x => x != null && x.In("A", "B")));

			Assert.That(allAggregated, Is.EqualTo(expected));
		}

		[ThrowsCannotBeConverted(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)]
		[Test]
		public async Task JoinAggregateExecuteNullableButFilteredAsync([DataSources] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var allAggregated = await table.AggregateExecuteAsync(e => string.Join(", ", e.OrderBy(x => x.NotNullableValue).Select(x => x.NullableValue).Where(x => x != null)));
			var expected      = string.Join(", ", data.OrderBy(x => x.NotNullableValue).Select(x => x.NullableValue).Where(x => x != null));

			Assert.That(allAggregated, Is.EqualTo(expected));
		}

		[ThrowsCannotBeConverted(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)]
		[Test]
		public void JoinAggregateExecuteNullableOnlyNotNull([DataSources] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var allAggregated = table.AggregateExecute(e => string.Join(", ", e.OrderBy(x => x.NotNullableValue).Where(e => e.NullableValue != null).Select(x => x.NullableValue)));
			var expected      = string.Join(", ", data.OrderBy(x => x.NotNullableValue).Where(e => e.NullableValue != null).Select(x => x.NullableValue));

			Assert.That(allAggregated, Is.EqualTo(expected));
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllDB2], ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[ThrowsCannotBeConverted(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)]
		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void JoinAggregateArray([DataSources(TestProvName.AllSqlServer2016Plus, TestProvName.AllOracle, TestProvName.AllMariaDB, TestProvName.AllMySql57)] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
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

		[ThrowsCannotBeConverted(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)]
		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllOracle11, TestProvName.AllOracle11, TestProvName.AllDB2], ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public void JoinAggregateArrayNotNull([DataSources(TestProvName.AllMariaDB, TestProvName.AllMySql57)] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
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

		[ActiveIssue(Configurations = [TestProvName.AllOracle], Details = "Null and '' comparison")]
		[Test]
		public void JoinAggregateArrayNotNullAndFilter([DataSources(true, TestProvName.AllOracle11, TestProvName.AllSybase)] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				select Sql.AsSql(string.Join(", ", new[] { t.NullableValue, t.NotNullableValue, t.VarcharValue, t.NVarcharValue }.Where(x => x != null).Where(x => x!.Contains("A"))));

			query = query.Where(x => !string.IsNullOrEmpty(x));

			AssertQuery(query);
		}

		[Test]
		public void JoinOnClient([DataSources(true)] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				select new
				{
					Result = string.Join(", ", data.Select(x => x.NullableValue).Where(x => x != null))
				};

			Assert.DoesNotThrow(() => _ = query.ToList());
		}

		[ThrowsCannotBeConverted(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)]
		[Test]
		public void StringJoinAssociationSubqueryUpdate1([DataSources(TestProvName.AllClickHouse, TestProvName.AllMySql57)] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				select new
				{
					t,
					Result = t.Children.Select(ag => ag.VarcharValue).StringAggregate(" | ").ToValue(),
				};

			query.Update(
				t => t.t,
				t => new SampleClass
				{
					VarcharValue  = t.Result,
					NVarcharValue = t.Result
				});
		}

		[ThrowsCannotBeConverted(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)]
		[Test]
		public void StringJoinAssociationSubqueryUpdate2([DataSources(TestProvName.AllClickHouse, TestProvName.AllMySql57)] string context)
		{
			var       data  = SampleClass.GenerateDataUniqueId();
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				select new
				{
					t,
					Result = string.Join(", ", t.Children!.Select(x => x.VarcharValue))
				};

			query.Update(
				t => t.t,
				t => new SampleClass
				{
					VarcharValue  = t.Result,
					NVarcharValue = t.Result
				});
		}
	}
}
