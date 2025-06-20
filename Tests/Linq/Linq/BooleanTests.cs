using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class BooleanTests : TestBase
	{
		sealed class BooleanTable
		{
			static BooleanTable()
			{
				var id = 1;

				Data = (from boolean in new[] { true, false }
					from booleanN in new bool?[] { true, false, null }
					from int32 in new[] { -1, 0, 1 }
					from int32N in new int?[] { -1, 0, 1, null }
					from dec in new[] { -0.1m, 0m, 0.1m }
					from decN in new decimal?[] { -0.1m, 0m, 0.1m, null }
					from dbl in new[] { -0.1, 0.0, 0.1 }
					from dblТ in new double?[] { -0.1, 0.0, 0.1, null }
					select new BooleanTable()
					{
						Id       = id++,
						Boolean  = boolean,
						BooleanN = booleanN,
						Int32    = int32,
						Int32N   = int32N,
						Decimal  = dec,
						DecimalN = decN,
						Double   = dbl,
						DoubleN  = dblТ,
					}).ToArray();
			}

			[PrimaryKey] public int Id { get; set; }
			[Column] public bool Boolean { get; set; }
			[Column] public bool? BooleanN { get; set; }
			[Column] public int Int32 { get; set; }
			[Column] public int? Int32N { get; set; }
			[Column] public decimal Decimal { get; set; }
			[Column] public decimal? DecimalN { get; set; }
			[Column] public double Double { get; set; }
			[Column] public double? DoubleN { get; set; }

			public static readonly BooleanTable[] Data;
		}

		sealed class SybaseBooleanTable
		{
			static SybaseBooleanTable()
			{
				var id = 1;

				Data = (from boolean in new[] { true, false }
						from int32 in new[] { -1, 0, 1 }
						from int32N in new int?[] { -1, 0, 1, null }
						from dec in new[] { -0.1m, 0m, 0.1m }
						from decN in new decimal?[] { -0.1m, 0m, 0.1m, null }
						from dbl in new[] { -0.1, 0.0, 0.1 }
						from dblТ in new double?[] { -0.1, 0.0, 0.1, null }
						select new SybaseBooleanTable()
						{
							Id = id++,
							Boolean = boolean,
							Int32 = int32,
							Int32N = int32N,
							Decimal = dec,
							DecimalN = decN,
							Double = dbl,
							DoubleN = dblТ,
						}).ToArray();
			}

			[PrimaryKey] public int Id { get; set; }
			[Column] public bool Boolean { get; set; }
			[Column] public int Int32 { get; set; }
			[Column] public int? Int32N { get; set; }
			[Column(Scale = 2)] public decimal Decimal { get; set; }
			[Column(Scale = 2)] public decimal? DecimalN { get; set; }
			[Column] public double Double { get; set; }
			[Column] public double? DoubleN { get; set; }

			public static readonly SybaseBooleanTable[] Data;
		}

		[Test]
		public void Test([DataSources(false, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(BooleanTable.Data);

			Test();
			db.InlineParameters = true;
			Test();

			void Test()
			{
				var True = true;
				var False = false;
				bool? TrueN = true;
				bool? FalseN = false;
				bool? Null = null;

				AssertQuery(tb.Where(r => r.Boolean == True));
				AssertQuery(tb.Where(r => r.Boolean == False));
				AssertQuery(tb.Where(r => r.Boolean == TrueN));
				AssertQuery(tb.Where(r => r.Boolean == FalseN));
				AssertQuery(tb.Where(r => r.Boolean == Null));

				AssertQuery(tb.Where(r => r.BooleanN == True));
				AssertQuery(tb.Where(r => r.BooleanN == False));
				AssertQuery(tb.Where(r => r.BooleanN == TrueN));
				AssertQuery(tb.Where(r => r.BooleanN == FalseN));
				AssertQuery(tb.Where(r => r.BooleanN == Null));

				AssertQuery(tb.Where(r => r.Boolean != True));
				AssertQuery(tb.Where(r => r.Boolean != False));
				AssertQuery(tb.Where(r => r.Boolean != TrueN));
				AssertQuery(tb.Where(r => r.Boolean != FalseN));
				AssertQuery(tb.Where(r => r.Boolean != Null));

				AssertQuery(tb.Where(r => r.BooleanN != True));
				AssertQuery(tb.Where(r => r.BooleanN != False));
				AssertQuery(tb.Where(r => r.BooleanN != TrueN));
				AssertQuery(tb.Where(r => r.BooleanN != FalseN));
				AssertQuery(tb.Where(r => r.BooleanN != Null));

				AssertQuery(tb.GroupBy(r => r.Id)
					.Select(g => new
					{
						g.Key,

						Count = g.Count(r => r.Boolean),
						Count_Explicit = g.Count(r => r.Boolean == true),
						CountN = g.Count(r => r.BooleanN == true),

						Count_False = g.Count(r => r.Boolean == false),
						CountN_False = g.Count(r => r.BooleanN == false),

						Count_NotTrue = g.Count(r => r.Boolean != true),
						CountN_NotTrue = g.Count(r => r.BooleanN != true),

						Count_NotFalse = g.Count(r => r.Boolean != false),
						CountN_NotFalse = g.Count(r => r.BooleanN != false),

						CountInt32 = g.Count(r => r.Int32 == 0),
						Count32N = g.Count(r => r.Int32N == 0),
						CountDecimal = g.Count(r => r.Decimal == 0),
						CountDecimalN = g.Count(r => r.DecimalN == 0),
						CountDouble = g.Count(r => r.Double == 0),
						CountDoubleN = g.Count(r => r.DoubleN == 0),

						CountInt32_NotEqual = g.Count(r => r.Int32 != 0),
						Count32N_NotEqual = g.Count(r => r.Int32N != 0),
						CountDecimal_NotEqual = g.Count(r => r.Decimal != 0),
						CountDecimalN_NotEqual = g.Count(r => r.DecimalN != 0),
						CountDouble_NotEqual = g.Count(r => r.Double != 0),
						CountDoubleN_NotEqual = g.Count(r => r.DoubleN != 0),

						CountInt32_Greater = g.Count(r => r.Int32 > 0),
						Count32N_Greater = g.Count(r => r.Int32N > 0),
						CountDecimal_Greater = g.Count(r => r.Decimal > 0),
						CountDecimalN_Greater = g.Count(r => r.DecimalN > 0),
						CountDouble_Greater = g.Count(r => r.Double > 0),
						CountDoubleN_Greater = g.Count(r => r.DoubleN > 0),

						CountInt32_Less = g.Count(r => r.Int32 < 0),
						Count32N_Less = g.Count(r => r.Int32N < 0),
						CountDecimal_Less = g.Count(r => r.Decimal < 0),
						CountDecimalN_Less = g.Count(r => r.DecimalN < 0),
						CountDouble_Less = g.Count(r => r.Double < 0),
						CountDoubleN_Less = g.Count(r => r.DoubleN < 0),

						CountInt32_GreaterEqual = g.Count(r => r.Int32 >= 0),
						Count32N_GreaterEqual = g.Count(r => r.Int32N >= 0),
						CountDecimal_GreaterEqual = g.Count(r => r.Decimal >= 0),
						CountDecimalN_GreaterEqual = g.Count(r => r.DecimalN >= 0),
						CountDouble_GreaterEqual = g.Count(r => r.Double >= 0),
						CountDoubleN_GreaterEqual = g.Count(r => r.DoubleN >= 0),

						CountInt32_LessEqual = g.Count(r => r.Int32 <= 0),
						Count32N_LessEqual = g.Count(r => r.Int32N <= 0),
						CountDecimal_LessEqual = g.Count(r => r.Decimal <= 0),
						CountDecimalN_LessEqual = g.Count(r => r.DecimalN <= 0),
						CountDouble_LessEqual = g.Count(r => r.Double <= 0),
						CountDoubleN_LessEqual = g.Count(r => r.DoubleN <= 0),
					}));

				var query = tb.Select(r => new
				{
					r.Id,

					Condition1 = r.Int32 == 0,
					Condition2 = r.Int32N == 0,
					Condition3 = r.Decimal == 0,
					Condition4 = r.DecimalN == 0,
					Condition5 = r.Double == 0,
					Condition6 = r.DoubleN == 0,
					Condition11 = (r.Int32N ?? r.Int32) == 0,
					Condition12 = (r.DecimalN ?? r.Decimal) == 0,
					Condition13 = (r.DoubleN ?? r.Double) == 0,
					Condition21 = (r.Boolean ? r.Int32N : r.Int32) == 0,
					Condition22 = (r.BooleanN == false ? r.Int32N : r.Int32) == 0,
					Condition23 = (r.Boolean ? r.DecimalN : r.Decimal) == 0,
					Condition24 = (r.BooleanN == false ? r.DecimalN : r.Decimal) == 0,
					Condition25 = (r.Boolean ? r.DoubleN : r.Double) == 0,
					Condition26 = (r.BooleanN == false ? r.DoubleN : r.Double) == 0,

					Condition101 = r.Int32 > 0,
					Condition102 = r.Int32N > 0,
					Condition103 = r.Decimal > 0,
					Condition104 = r.DecimalN > 0,
					Condition105 = r.Double > 0,
					Condition106 = r.DoubleN > 0,
					Condition111 = (r.Int32N ?? r.Int32) > 0,
					Condition112 = (r.DecimalN ?? r.Decimal) > 0,
					Condition113 = (r.DoubleN ?? r.Double) > 0,
					Condition121 = (r.Boolean ? r.Int32N : r.Int32) > 0,
					Condition122 = (r.BooleanN == false ? r.Int32N : r.Int32) > 0,
					Condition123 = (r.Boolean ? r.DecimalN : r.Decimal) > 0,
					Condition124 = (r.BooleanN == false ? r.DecimalN : r.Decimal) > 0,
					Condition125 = (r.Boolean ? r.DoubleN : r.Double) > 0,
					Condition126 = (r.BooleanN == false ? r.DoubleN : r.Double) > 0,

					Condition201 = r.Int32 >= 0,
					Condition202 = r.Int32N >= 0,
					Condition203 = r.Decimal >= 0,
					Condition204 = r.DecimalN >= 0,
					Condition205 = r.Double >= 0,
					Condition206 = r.DoubleN >= 0,
					Condition211 = (r.Int32N ?? r.Int32) >= 0,
					Condition212 = (r.DecimalN ?? r.Decimal) >= 0,
					Condition213 = (r.DoubleN ?? r.Double) >= 0,
					Condition221 = (r.Boolean ? r.Int32N : r.Int32) >= 0,
					Condition222 = (r.BooleanN == false ? r.Int32N : r.Int32) >= 0,
					Condition223 = (r.Boolean ? r.DecimalN : r.Decimal) >= 0,
					Condition224 = (r.BooleanN == false ? r.DecimalN : r.Decimal) >= 0,
					Condition225 = (r.Boolean ? r.DoubleN : r.Double) >= 0,
					Condition226 = (r.BooleanN == false ? r.DoubleN : r.Double) >= 0,

					Condition301 = r.Int32 < 0,
					Condition302 = r.Int32N < 0,
					Condition303 = r.Decimal < 0,
					Condition304 = r.DecimalN < 0,
					Condition305 = r.Double < 0,
					Condition306 = r.DoubleN < 0,
					Condition311 = (r.Int32N ?? r.Int32) < 0,
					Condition312 = (r.DecimalN ?? r.Decimal) < 0,
					Condition313 = (r.DoubleN ?? r.Double) < 0,
					Condition321 = (r.Boolean ? r.Int32N : r.Int32) < 0,
					Condition322 = (r.BooleanN == false ? r.Int32N : r.Int32) < 0,
					Condition323 = (r.Boolean ? r.DecimalN : r.Decimal) < 0,
					Condition324 = (r.BooleanN == false ? r.DecimalN : r.Decimal) < 0,
					Condition325 = (r.Boolean ? r.DoubleN : r.Double) < 0,
					Condition326 = (r.BooleanN == false ? r.DoubleN : r.Double) < 0,

					Condition401 = r.Int32 <= 0,
					Condition402 = r.Int32N <= 0,
					Condition403 = r.Decimal <= 0,
					Condition404 = r.DecimalN <= 0,
					Condition405 = r.Double <= 0,
					Condition406 = r.DoubleN <= 0,
					Condition411 = (r.Int32N ?? r.Int32) <= 0,
					Condition412 = (r.DecimalN ?? r.Decimal) <= 0,
					Condition413 = (r.DoubleN ?? r.Double) <= 0,
					Condition421 = (r.Boolean ? r.Int32N : r.Int32) <= 0,
					Condition422 = (r.BooleanN == false ? r.Int32N : r.Int32) <= 0,
					Condition423 = (r.Boolean ? r.DecimalN : r.Decimal) <= 0,
					Condition424 = (r.BooleanN == false ? r.DecimalN : r.Decimal) <= 0,
					Condition425 = (r.Boolean ? r.DoubleN : r.Double) <= 0,
					Condition426 = (r.BooleanN == false ? r.DoubleN : r.Double) <= 0,
				});

				// IFX returns incorrect values for one field in 3 records for unknown reason on provider level
				// same query in DBeaver works properly
				// SET1, Id = 5390, Condition23 False (expected True)
				// SET2, Id = 3531, Condition125 False (expected True)
				// SET2, Id = 6084, Condition22 False (expected True)
				// could be DB2 provider issue
				if (context.IsAnyOf(TestProvName.AllInformix))
					query.Concat(query).ToArray();
				else
					AssertQuery(query.Concat(query));

				var serverQuery = tb.Select(g => new
				{
					Count = Sql.Ext.Count(g.Boolean).ToValue(),
					Count_Explicit = Sql.Ext.Count(g.Boolean == true).ToValue(),
					CountN_Nullable = Sql.Ext.Count(g.BooleanN).ToValue(),
					CountN = Sql.Ext.Count(g.BooleanN == true).ToValue(),

					Count_False = Sql.Ext.Count(g.Boolean == false).ToValue(),
					CountN_False = Sql.Ext.Count(g.BooleanN == false).ToValue(),

					Count_NotTrue = Sql.Ext.Count(g.Boolean != true).ToValue(),
					CountN_NotTrue = Sql.Ext.Count(g.BooleanN != true).ToValue(),

					Count_NotFalse = Sql.Ext.Count(g.Boolean != false).ToValue(),
					CountN_NotFalse = Sql.Ext.Count(g.BooleanN != false).ToValue(),

					CountInt32 = Sql.Ext.Count(g.Int32 == 0).ToValue(),
					Count32N = Sql.Ext.Count(g.Int32N == 0).ToValue(),
					CountDecimal = Sql.Ext.Count(g.Decimal == 0).ToValue(),
					CountDecimalN = Sql.Ext.Count(g.DecimalN == 0).ToValue(),
					CountDouble = Sql.Ext.Count(g.Double == 0).ToValue(),
					CountDoubleN = Sql.Ext.Count(g.DoubleN == 0).ToValue(),

					CountInt32_NotEqual = Sql.Ext.Count(g.Int32 != 0).ToValue(),
					Count32N_NotEqual = Sql.Ext.Count(g.Int32N != 0).ToValue(),
					CountDecimal_NotEqual = Sql.Ext.Count(g.Decimal != 0).ToValue(),
					CountDecimalN_NotEqual = Sql.Ext.Count(g.DecimalN != 0).ToValue(),
					CountDouble_NotEqual = Sql.Ext.Count(g.Double != 0).ToValue(),
					CountDoubleN_NotEqual = Sql.Ext.Count(g.DoubleN != 0).ToValue(),

					CountInt32_Greater = Sql.Ext.Count(g.Int32 > 0).ToValue(),
					Count32N_Greater = Sql.Ext.Count(g.Int32N > 0).ToValue(),
					CountDecimal_Greater = Sql.Ext.Count(g.Decimal > 0).ToValue(),
					CountDecimalN_Greater = Sql.Ext.Count(g.DecimalN > 0).ToValue(),
					CountDouble_Greater = Sql.Ext.Count(g.Double > 0).ToValue(),
					CountDoubleN_Greater = Sql.Ext.Count(g.DoubleN > 0).ToValue(),

					CountInt32_Less = Sql.Ext.Count(g.Int32 < 0).ToValue(),
					Count32N_Less = Sql.Ext.Count(g.Int32N < 0).ToValue(),
					CountDecimal_Less = Sql.Ext.Count(g.Decimal < 0).ToValue(),
					CountDecimalN_Less = Sql.Ext.Count(g.DecimalN < 0).ToValue(),
					CountDouble_Less = Sql.Ext.Count(g.Double < 0).ToValue(),
					CountDoubleN_Less = Sql.Ext.Count(g.DoubleN < 0).ToValue(),

					CountInt32_GreaterEqual = Sql.Ext.Count(g.Int32 >= 0).ToValue(),
					Count32N_GreaterEqual = Sql.Ext.Count(g.Int32N >= 0).ToValue(),
					CountDecimal_GreaterEqual = Sql.Ext.Count(g.Decimal >= 0).ToValue(),
					CountDecimalN_GreaterEqual = Sql.Ext.Count(g.DecimalN >= 0).ToValue(),
					CountDouble_GreaterEqual = Sql.Ext.Count(g.Double >= 0).ToValue(),
					CountDoubleN_GreaterEqual = Sql.Ext.Count(g.DoubleN >= 0).ToValue(),

					CountInt32_LessEqual = Sql.Ext.Count(g.Int32 <= 0).ToValue(),
					Count32N_LessEqual = Sql.Ext.Count(g.Int32N <= 0).ToValue(),
					CountDecimal_LessEqual = Sql.Ext.Count(g.Decimal <= 0).ToValue(),
					CountDecimalN_LessEqual = Sql.Ext.Count(g.DecimalN <= 0).ToValue(),
					CountDouble_LessEqual = Sql.Ext.Count(g.Double <= 0).ToValue(),
					CountDoubleN_LessEqual = Sql.Ext.Count(g.DoubleN <= 0).ToValue(),
				});

				var clientQuery = tb.AsEnumerable().GroupBy(r => 1).Select(g => new
				{
					Count = g.Count(r => r.Boolean),
					Count_Explicit = g.Count(r => r.Boolean == true),
					CountN_Nullable = g.Count(r => r.BooleanN == true),
					CountN = g.Count(r => r.BooleanN == true),

					Count_False = g.Count(r => r.Boolean == false),
					CountN_False = g.Count(r => r.BooleanN == false),

					Count_NotTrue = g.Count(r => r.Boolean != true),
					CountN_NotTrue = g.Count(r => r.BooleanN != true),

					Count_NotFalse = g.Count(r => r.Boolean != false),
					CountN_NotFalse = g.Count(r => r.BooleanN != false),

					CountInt32 = g.Count(r => r.Int32 == 0),
					Count32N = g.Count(r => r.Int32N == 0),
					CountDecimal = g.Count(r => r.Decimal == 0),
					CountDecimalN = g.Count(r => r.DecimalN == 0),
					CountDouble = g.Count(r => r.Double == 0),
					CountDoubleN = g.Count(r => r.DoubleN == 0),

					CountInt32_NotEqual = g.Count(r => r.Int32 != 0),
					Count32N_NotEqual = g.Count(r => r.Int32N != 0),
					CountDecimal_NotEqual = g.Count(r => r.Decimal != 0),
					CountDecimalN_NotEqual = g.Count(r => r.DecimalN != 0),
					CountDouble_NotEqual = g.Count(r => r.Double != 0),
					CountDoubleN_NotEqual = g.Count(r => r.DoubleN != 0),

					CountInt32_Greater = g.Count(r => r.Int32 > 0),
					Count32N_Greater = g.Count(r => r.Int32N > 0),
					CountDecimal_Greater = g.Count(r => r.Decimal > 0),
					CountDecimalN_Greater = g.Count(r => r.DecimalN > 0),
					CountDouble_Greater = g.Count(r => r.Double > 0),
					CountDoubleN_Greater = g.Count(r => r.DoubleN > 0),

					CountInt32_Less = g.Count(r => r.Int32 < 0),
					Count32N_Less = g.Count(r => r.Int32N < 0),
					CountDecimal_Less = g.Count(r => r.Decimal < 0),
					CountDecimalN_Less = g.Count(r => r.DecimalN < 0),
					CountDouble_Less = g.Count(r => r.Double < 0),
					CountDoubleN_Less = g.Count(r => r.DoubleN < 0),

					CountInt32_GreaterEqual = g.Count(r => r.Int32 >= 0),
					Count32N_GreaterEqual = g.Count(r => r.Int32N >= 0),
					CountDecimal_GreaterEqual = g.Count(r => r.Decimal >= 0),
					CountDecimalN_GreaterEqual = g.Count(r => r.DecimalN >= 0),
					CountDouble_GreaterEqual = g.Count(r => r.Double >= 0),
					CountDoubleN_GreaterEqual = g.Count(r => r.DoubleN >= 0),

					CountInt32_LessEqual = g.Count(r => r.Int32 <= 0),
					Count32N_LessEqual = g.Count(r => r.Int32N <= 0),
					CountDecimal_LessEqual = g.Count(r => r.Decimal <= 0),
					CountDecimalN_LessEqual = g.Count(r => r.DecimalN <= 0),
					CountDouble_LessEqual = g.Count(r => r.Double <= 0),
					CountDoubleN_LessEqual = g.Count(r => r.DoubleN <= 0),
				});

				// TODO: https://github.com/linq2db/linq2db/issues/2842
				//AreEqual(clientQuery, serverQuery);
			}
		}

		[Test]
		public void TestSybase([IncludeDataSources(false, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<SybaseBooleanTable>();

			using (var _ = new DisableBaseline("Test setup"))
			{
				tb.BulkCopy(new BulkCopyOptions() { MaxBatchSize = 500 }, SybaseBooleanTable.Data);
			}

			Test();
			db.InlineParameters = true;
			Test();

			void Test()
			{
				var True = true;
				var False = false;
				bool? TrueN = true;
				bool? FalseN = false;
				bool? Null = null;

				AssertQuery(tb.Where(r => r.Boolean == True));
				AssertQuery(tb.Where(r => r.Boolean == False));
				AssertQuery(tb.Where(r => r.Boolean == TrueN));
				AssertQuery(tb.Where(r => r.Boolean == FalseN));
				AssertQuery(tb.Where(r => r.Boolean == Null));

				AssertQuery(tb.Where(r => r.Boolean != True));
				AssertQuery(tb.Where(r => r.Boolean != False));
				AssertQuery(tb.Where(r => r.Boolean != TrueN));
				AssertQuery(tb.Where(r => r.Boolean != FalseN));
				AssertQuery(tb.Where(r => r.Boolean != Null));

				AssertQuery(tb.GroupBy(r => r.Id)
					.Select(g => new
					{
						g.Key,

						Count = g.Count(r => r.Boolean),
						Count_Explicit = g.Count(r => r.Boolean == true),

						Count_False = g.Count(r => r.Boolean == false),

						Count_NotTrue = g.Count(r => r.Boolean != true),

						Count_NotFalse = g.Count(r => r.Boolean != false),

						CountInt32 = g.Count(r => r.Int32 == 0),
						Count32N = g.Count(r => r.Int32N == 0),
						CountDecimal = g.Count(r => r.Decimal == 0),
						CountDecimalN = g.Count(r => r.DecimalN == 0),
						CountDouble = g.Count(r => r.Double == 0),
						CountDoubleN = g.Count(r => r.DoubleN == 0),

						CountInt32_NotEqual = g.Count(r => r.Int32 != 0),
						Count32N_NotEqual = g.Count(r => r.Int32N != 0),
						CountDecimal_NotEqual = g.Count(r => r.Decimal != 0),
						CountDecimalN_NotEqual = g.Count(r => r.DecimalN != 0),
						CountDouble_NotEqual = g.Count(r => r.Double != 0),
						CountDoubleN_NotEqual = g.Count(r => r.DoubleN != 0),

						CountInt32_Greater = g.Count(r => r.Int32 > 0),
						Count32N_Greater = g.Count(r => r.Int32N > 0),
						CountDecimal_Greater = g.Count(r => r.Decimal > 0),
						CountDecimalN_Greater = g.Count(r => r.DecimalN > 0),
						CountDouble_Greater = g.Count(r => r.Double > 0),
						CountDoubleN_Greater = g.Count(r => r.DoubleN > 0),

						CountInt32_Less = g.Count(r => r.Int32 < 0),
						Count32N_Less = g.Count(r => r.Int32N < 0),
						CountDecimal_Less = g.Count(r => r.Decimal < 0),
						CountDecimalN_Less = g.Count(r => r.DecimalN < 0),
						CountDouble_Less = g.Count(r => r.Double < 0),
						CountDoubleN_Less = g.Count(r => r.DoubleN < 0),

						CountInt32_GreaterEqual = g.Count(r => r.Int32 >= 0),
						Count32N_GreaterEqual = g.Count(r => r.Int32N >= 0),
						CountDecimal_GreaterEqual = g.Count(r => r.Decimal >= 0),
						CountDecimalN_GreaterEqual = g.Count(r => r.DecimalN >= 0),
						CountDouble_GreaterEqual = g.Count(r => r.Double >= 0),
						CountDoubleN_GreaterEqual = g.Count(r => r.DoubleN >= 0),

						CountInt32_LessEqual = g.Count(r => r.Int32 <= 0),
						Count32N_LessEqual = g.Count(r => r.Int32N <= 0),
						CountDecimal_LessEqual = g.Count(r => r.Decimal <= 0),
						CountDecimalN_LessEqual = g.Count(r => r.DecimalN <= 0),
						CountDouble_LessEqual = g.Count(r => r.Double <= 0),
						CountDoubleN_LessEqual = g.Count(r => r.DoubleN <= 0),
					}));

				var query = tb.Select(r => new
				{
					r.Id,

					Condition1 = r.Int32 == 0,
					Condition2 = r.Int32N == 0,
					Condition3 = r.Decimal == 0,
					Condition4 = r.DecimalN == 0,
					Condition5 = r.Double == 0,
					Condition6 = r.DoubleN == 0,
					Condition11 = (r.Int32N ?? r.Int32) == 0,
					Condition12 = (r.DecimalN ?? r.Decimal) == 0,
					Condition13 = (r.DoubleN ?? r.Double) == 0,
					Condition21 = (r.Boolean ? r.Int32N : r.Int32) == 0,
					Condition23 = (r.Boolean ? r.DecimalN : r.Decimal) == 0,
					Condition25 = (r.Boolean ? r.DoubleN : r.Double) == 0,

					Condition101 = r.Int32 > 0,
					Condition102 = r.Int32N > 0,
					Condition103 = r.Decimal > 0,
					Condition104 = r.DecimalN > 0,
					Condition105 = r.Double > 0,
					Condition106 = r.DoubleN > 0,
					Condition111 = (r.Int32N ?? r.Int32) > 0,
					Condition112 = (r.DecimalN ?? r.Decimal) > 0,
					Condition113 = (r.DoubleN ?? r.Double) > 0,
					Condition121 = (r.Boolean ? r.Int32N : r.Int32) > 0,
					Condition123 = (r.Boolean ? r.DecimalN : r.Decimal) > 0,
					Condition125 = (r.Boolean ? r.DoubleN : r.Double) > 0,

					Condition201 = r.Int32 >= 0,
					Condition202 = r.Int32N >= 0,
					Condition203 = r.Decimal >= 0,
					Condition204 = r.DecimalN >= 0,
					Condition205 = r.Double >= 0,
					Condition206 = r.DoubleN >= 0,
					Condition211 = (r.Int32N ?? r.Int32) >= 0,
					Condition212 = (r.DecimalN ?? r.Decimal) >= 0,
					Condition213 = (r.DoubleN ?? r.Double) >= 0,
					Condition221 = (r.Boolean ? r.Int32N : r.Int32) >= 0,
					Condition223 = (r.Boolean ? r.DecimalN : r.Decimal) >= 0,
					Condition225 = (r.Boolean ? r.DoubleN : r.Double) >= 0,

					Condition301 = r.Int32 < 0,
					Condition302 = r.Int32N < 0,
					Condition303 = r.Decimal < 0,
					Condition304 = r.DecimalN < 0,
					Condition305 = r.Double < 0,
					Condition306 = r.DoubleN < 0,
					Condition311 = (r.Int32N ?? r.Int32) < 0,
					Condition312 = (r.DecimalN ?? r.Decimal) < 0,
					Condition313 = (r.DoubleN ?? r.Double) < 0,
					Condition321 = (r.Boolean ? r.Int32N : r.Int32) < 0,
					Condition323 = (r.Boolean ? r.DecimalN : r.Decimal) < 0,
					Condition325 = (r.Boolean ? r.DoubleN : r.Double) < 0,

					Condition401 = r.Int32 <= 0,
					Condition402 = r.Int32N <= 0,
					Condition403 = r.Decimal <= 0,
					Condition404 = r.DecimalN <= 0,
					Condition405 = r.Double <= 0,
					Condition406 = r.DoubleN <= 0,
					Condition411 = (r.Int32N ?? r.Int32) <= 0,
					Condition412 = (r.DecimalN ?? r.Decimal) <= 0,
					Condition413 = (r.DoubleN ?? r.Double) <= 0,
					Condition421 = (r.Boolean ? r.Int32N : r.Int32) <= 0,
					Condition423 = (r.Boolean ? r.DecimalN : r.Decimal) <= 0,
					Condition425 = (r.Boolean ? r.DoubleN : r.Double) <= 0,
				});

				AssertQuery(query.Concat(query));

				var serverQuery = tb.Select(g => new
				{
					Count = Sql.Ext.Count(g.Boolean).ToValue(),
					Count_Explicit = Sql.Ext.Count(g.Boolean == true).ToValue(),

					Count_False = Sql.Ext.Count(g.Boolean == false).ToValue(),

					Count_NotTrue = Sql.Ext.Count(g.Boolean != true).ToValue(),

					Count_NotFalse = Sql.Ext.Count(g.Boolean != false).ToValue(),

					CountInt32 = Sql.Ext.Count(g.Int32 == 0).ToValue(),
					Count32N = Sql.Ext.Count(g.Int32N == 0).ToValue(),
					CountDecimal = Sql.Ext.Count(g.Decimal == 0).ToValue(),
					CountDecimalN = Sql.Ext.Count(g.DecimalN == 0).ToValue(),
					CountDouble = Sql.Ext.Count(g.Double == 0).ToValue(),
					CountDoubleN = Sql.Ext.Count(g.DoubleN == 0).ToValue(),

					CountInt32_NotEqual = Sql.Ext.Count(g.Int32 != 0).ToValue(),
					Count32N_NotEqual = Sql.Ext.Count(g.Int32N != 0).ToValue(),
					CountDecimal_NotEqual = Sql.Ext.Count(g.Decimal != 0).ToValue(),
					CountDecimalN_NotEqual = Sql.Ext.Count(g.DecimalN != 0).ToValue(),
					CountDouble_NotEqual = Sql.Ext.Count(g.Double != 0).ToValue(),
					CountDoubleN_NotEqual = Sql.Ext.Count(g.DoubleN != 0).ToValue(),

					CountInt32_Greater = Sql.Ext.Count(g.Int32 > 0).ToValue(),
					Count32N_Greater = Sql.Ext.Count(g.Int32N > 0).ToValue(),
					CountDecimal_Greater = Sql.Ext.Count(g.Decimal > 0).ToValue(),
					CountDecimalN_Greater = Sql.Ext.Count(g.DecimalN > 0).ToValue(),
					CountDouble_Greater = Sql.Ext.Count(g.Double > 0).ToValue(),
					CountDoubleN_Greater = Sql.Ext.Count(g.DoubleN > 0).ToValue(),

					CountInt32_Less = Sql.Ext.Count(g.Int32 < 0).ToValue(),
					Count32N_Less = Sql.Ext.Count(g.Int32N < 0).ToValue(),
					CountDecimal_Less = Sql.Ext.Count(g.Decimal < 0).ToValue(),
					CountDecimalN_Less = Sql.Ext.Count(g.DecimalN < 0).ToValue(),
					CountDouble_Less = Sql.Ext.Count(g.Double < 0).ToValue(),
					CountDoubleN_Less = Sql.Ext.Count(g.DoubleN < 0).ToValue(),

					CountInt32_GreaterEqual = Sql.Ext.Count(g.Int32 >= 0).ToValue(),
					Count32N_GreaterEqual = Sql.Ext.Count(g.Int32N >= 0).ToValue(),
					CountDecimal_GreaterEqual = Sql.Ext.Count(g.Decimal >= 0).ToValue(),
					CountDecimalN_GreaterEqual = Sql.Ext.Count(g.DecimalN >= 0).ToValue(),
					CountDouble_GreaterEqual = Sql.Ext.Count(g.Double >= 0).ToValue(),
					CountDoubleN_GreaterEqual = Sql.Ext.Count(g.DoubleN >= 0).ToValue(),

					CountInt32_LessEqual = Sql.Ext.Count(g.Int32 <= 0).ToValue(),
					Count32N_LessEqual = Sql.Ext.Count(g.Int32N <= 0).ToValue(),
					CountDecimal_LessEqual = Sql.Ext.Count(g.Decimal <= 0).ToValue(),
					CountDecimalN_LessEqual = Sql.Ext.Count(g.DecimalN <= 0).ToValue(),
					CountDouble_LessEqual = Sql.Ext.Count(g.Double <= 0).ToValue(),
					CountDoubleN_LessEqual = Sql.Ext.Count(g.DoubleN <= 0).ToValue(),
				});

				var clientQuery = tb.AsEnumerable().GroupBy(r => 1).Select(g => new
				{
					Count = g.Count(r => r.Boolean),
					Count_Explicit = g.Count(r => r.Boolean == true),

					Count_False = g.Count(r => r.Boolean == false),

					Count_NotTrue = g.Count(r => r.Boolean != true),

					Count_NotFalse = g.Count(r => r.Boolean != false),

					CountInt32 = g.Count(r => r.Int32 == 0),
					Count32N = g.Count(r => r.Int32N == 0),
					CountDecimal = g.Count(r => r.Decimal == 0),
					CountDecimalN = g.Count(r => r.DecimalN == 0),
					CountDouble = g.Count(r => r.Double == 0),
					CountDoubleN = g.Count(r => r.DoubleN == 0),

					CountInt32_NotEqual = g.Count(r => r.Int32 != 0),
					Count32N_NotEqual = g.Count(r => r.Int32N != 0),
					CountDecimal_NotEqual = g.Count(r => r.Decimal != 0),
					CountDecimalN_NotEqual = g.Count(r => r.DecimalN != 0),
					CountDouble_NotEqual = g.Count(r => r.Double != 0),
					CountDoubleN_NotEqual = g.Count(r => r.DoubleN != 0),

					CountInt32_Greater = g.Count(r => r.Int32 > 0),
					Count32N_Greater = g.Count(r => r.Int32N > 0),
					CountDecimal_Greater = g.Count(r => r.Decimal > 0),
					CountDecimalN_Greater = g.Count(r => r.DecimalN > 0),
					CountDouble_Greater = g.Count(r => r.Double > 0),
					CountDoubleN_Greater = g.Count(r => r.DoubleN > 0),

					CountInt32_Less = g.Count(r => r.Int32 < 0),
					Count32N_Less = g.Count(r => r.Int32N < 0),
					CountDecimal_Less = g.Count(r => r.Decimal < 0),
					CountDecimalN_Less = g.Count(r => r.DecimalN < 0),
					CountDouble_Less = g.Count(r => r.Double < 0),
					CountDoubleN_Less = g.Count(r => r.DoubleN < 0),

					CountInt32_GreaterEqual = g.Count(r => r.Int32 >= 0),
					Count32N_GreaterEqual = g.Count(r => r.Int32N >= 0),
					CountDecimal_GreaterEqual = g.Count(r => r.Decimal >= 0),
					CountDecimalN_GreaterEqual = g.Count(r => r.DecimalN >= 0),
					CountDouble_GreaterEqual = g.Count(r => r.Double >= 0),
					CountDoubleN_GreaterEqual = g.Count(r => r.DoubleN >= 0),

					CountInt32_LessEqual = g.Count(r => r.Int32 <= 0),
					Count32N_LessEqual = g.Count(r => r.Int32N <= 0),
					CountDecimal_LessEqual = g.Count(r => r.Decimal <= 0),
					CountDecimalN_LessEqual = g.Count(r => r.DecimalN <= 0),
					CountDouble_LessEqual = g.Count(r => r.Double <= 0),
					CountDoubleN_LessEqual = g.Count(r => r.DoubleN <= 0),
				});

				// TODO: https://github.com/linq2db/linq2db/issues/2842
				//AreEqual(clientQuery, serverQuery);
			}
		}

		[Test]
		public void TestAsSqlBoolTranslation([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var result = db.Select(() => Coalesce(Sql.AsSql(true), Sql.AsSql(false)));

			Assert.That(result, Is.True);
		}

		[Test]
		public void TestNoAsSqlBoolTranslation([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var result = db.Select(() => Coalesce(true, false));

			Assert.That(result, Is.True);
		}

		[Sql.Expression("IIF({0} IS NULL, {1}, {0})", CanBeNull = false, IgnoreGenericParameters = true, ServerSideOnly = true, Configuration = ProviderName.Access)]
		[Sql.Function("COALESCE", CanBeNull = false, IgnoreGenericParameters = true, ServerSideOnly = true)]
		static T Coalesce<T>(T value, T defaultValue) => throw new ServerSideOnlyException(nameof(Coalesce));

		[Sql.Function("CONTAINS", ServerSideOnly = true, CanBeNull = false, InlineParameters = false, PreferServerSide = true, IsPredicate = true)]
		static bool FtxContains(string columnOrColumns, string search) => throw new InvalidOperationException();

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4933")]
		public void IssueFunctionPredicate([IncludeDataSources(TestProvName.AllNorthwind)] string context)
		{
			using var db = new NorthwindDB(context);

			_ = db.Product.Where(p => FtxContains(p.ProductName, "some")).ToArray();

			Assert.That(db.LastQuery, Does.Not.Contain(" = "));
		}
	}
}
