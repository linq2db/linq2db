using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.SchemaProvider;

using NpgsqlTypes;

using NUnit.Framework;

namespace Tests.SchemaProvider
{
	[TestFixture]
	public class PostgreSQLTests : TestBase
	{
		public class ProcedureTestCase
		{
			public ProcedureTestCase(ProcedureSchema schema)
			{
				Schema = schema;
			}

			public ProcedureSchema Schema { get; }

			public override string ToString() => Schema.ProcedureName;
		}

		public static IEnumerable<ProcedureTestCase> ProcedureTestCases
		{
			get
			{
				// test table function schema
				yield return new ProcedureTestCase(new ProcedureSchema()
				{
					CatalogName         = "SET_BY_TEST",
					SchemaName          = "public",
					ProcedureName       = "TestTableFunctionSchema",
					MemberName          = "TestTableFunctionSchema",
					IsFunction          = true,
					IsTableFunction     = true,
					IsAggregateFunction = false,
					IsDefaultSchema     = true,
					IsLoaded            = true,
					Parameters          = new List<ParameterSchema>(),
					ResultTable         = new TableSchema
					{
						IsProcedureResult = true,
						TypeName          = "TestTableFunctionSchemaResult",
						Columns           = new List<ColumnSchema>
						{
							new ColumnSchema { ColumnName = "ID",                  ColumnType = "int4",                            MemberName = "ID",                  MemberType = "int?",                        SystemType = typeof(int),                                                     IsNullable = true, // must be false, but we don't get this information from provider
							                                                                                                                                                                                                                                                                            DataType = DataType.Int32          },
							new ColumnSchema { ColumnName = "bigintDataType",      ColumnType = "int8",                            MemberName = "bigintDataType",      MemberType = "long?",                       SystemType = typeof(long),                                                    IsNullable = true, DataType = DataType.Int64          },
							new ColumnSchema { ColumnName = "numericDataType",     ColumnType = "numeric(0,0)",                    MemberName = "numericDataType",     MemberType = "decimal?",                    SystemType = typeof(decimal),                                                 IsNullable = true, DataType = DataType.Decimal        },
							new ColumnSchema { ColumnName = "smallintDataType",    ColumnType = "int2",                            MemberName = "smallintDataType",    MemberType = "short?",                      SystemType = typeof(short),                                                   IsNullable = true, DataType = DataType.Int16          },
							new ColumnSchema { ColumnName = "intDataType",         ColumnType = "int4",                            MemberName = "intDataType",         MemberType = "int?",                        SystemType = typeof(int),                                                     IsNullable = true, DataType = DataType.Int32          },
							new ColumnSchema { ColumnName = "moneyDataType",       ColumnType = "money",                           MemberName = "moneyDataType",       MemberType = "decimal?",                    SystemType = typeof(decimal),                                                 IsNullable = true, DataType = DataType.Money          },
							new ColumnSchema { ColumnName = "doubleDataType",      ColumnType = "float8",                          MemberName = "doubleDataType",      MemberType = "double?",                     SystemType = typeof(double),                                                  IsNullable = true, DataType = DataType.Double         },
							new ColumnSchema { ColumnName = "realDataType",        ColumnType = "float4",                          MemberName = "realDataType",        MemberType = "float?",                      SystemType = typeof(float),                                                   IsNullable = true, DataType = DataType.Single         },
							new ColumnSchema { ColumnName = "timestampDataType",   ColumnType = "timestamp (0) without time zone", MemberName = "timestampDataType",   MemberType = "DateTime?",                   SystemType = typeof(DateTime),                                                IsNullable = true, DataType = DataType.DateTime2      },
							new ColumnSchema { ColumnName = "timestampTZDataType", ColumnType = "timestamp (0) with time zone",    MemberName = "timestampTZDataType", MemberType = "DateTimeOffset?",             SystemType = typeof(DateTimeOffset),                                          IsNullable = true, DataType = DataType.DateTimeOffset },
							new ColumnSchema { ColumnName = "dateDataType",        ColumnType = "date",                            MemberName = "dateDataType",        MemberType = "DateTime?",                   SystemType = typeof(DateTime),                                                IsNullable = true, DataType = DataType.Date           },
							new ColumnSchema { ColumnName = "timeDataType",        ColumnType = "time (0) without time zone",      MemberName = "timeDataType",        MemberType = "TimeSpan?",                   SystemType = typeof(TimeSpan),                                                IsNullable = true, DataType = DataType.Time           },
							new ColumnSchema { ColumnName = "timeTZDataType",      ColumnType = "time (0) with time zone",         MemberName = "timeTZDataType",      MemberType = "DateTimeOffset?",             SystemType = typeof(DateTimeOffset),                                          IsNullable = true, DataType = DataType.Time           },
							new ColumnSchema { ColumnName = "intervalDataType",    ColumnType = "interval",                        MemberName = "intervalDataType",    MemberType = "TimeSpan?",                   SystemType = typeof(TimeSpan),       ProviderSpecificType = "NpgsqlInterval", IsNullable = true, DataType = DataType.Interval       },
							new ColumnSchema { ColumnName = "intervalDataType2",   ColumnType = "interval",                        MemberName = "intervalDataType2",   MemberType = "TimeSpan?",                   SystemType = typeof(TimeSpan),       ProviderSpecificType = "NpgsqlInterval", IsNullable = true, DataType = DataType.Interval       },
							new ColumnSchema { ColumnName = "charDataType",        ColumnType = "character(1)",                    MemberName = "charDataType",        MemberType = "char?",                       SystemType = typeof(char),                                                    IsNullable = true, DataType = DataType.NChar          },
							new ColumnSchema { ColumnName = "char20DataType",      ColumnType = "character(20)",                   MemberName = "char20DataType",      MemberType = "string",                      SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.NChar          },
							new ColumnSchema { ColumnName = "varcharDataType",     ColumnType = "character varying(20)",           MemberName = "varcharDataType",     MemberType = "string",                      SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.NVarChar       },
							new ColumnSchema { ColumnName = "textDataType",        ColumnType = "text",                            MemberName = "textDataType",        MemberType = "string",                      SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.Text           },
							new ColumnSchema { ColumnName = "binaryDataType",      ColumnType = "bytea",                           MemberName = "binaryDataType",      MemberType = "byte[]",                      SystemType = typeof(byte[]),                                                  IsNullable = true, DataType = DataType.Binary         },
							new ColumnSchema { ColumnName = "uuidDataType",        ColumnType = "uuid",                            MemberName = "uuidDataType",        MemberType = "Guid?",                       SystemType = typeof(Guid),                                                    IsNullable = true, DataType = DataType.Guid           },
							new ColumnSchema { ColumnName = "bitDataType",         ColumnType = "bit(-1)", // TODO: must be 3, but npgsql3 doesn't return it (npgsql4 does)
							                                                                                                       MemberName = "bitDataType",         MemberType = "BitArray",                    SystemType = typeof(BitArray),                                                IsNullable = true, DataType = DataType.BitArray       },
							new ColumnSchema { ColumnName = "booleanDataType",     ColumnType = "bool",                            MemberName = "booleanDataType",     MemberType = "bool?",                       SystemType = typeof(bool),                                                    IsNullable = true, DataType = DataType.Boolean        },
							new ColumnSchema { ColumnName = "colorDataType",       ColumnType = "public.color",                    MemberName = "colorDataType",       MemberType = "string",                      SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.Undefined      },
							new ColumnSchema { ColumnName = "pointDataType",       ColumnType = "point",                           MemberName = "pointDataType",       MemberType = "NpgsqlPoint?",                SystemType = typeof(NpgsqlPoint),                ProviderSpecificType = "NpgsqlPoint",    IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "lsegDataType",        ColumnType = "lseg",                            MemberName = "lsegDataType",        MemberType = "NpgsqlLSeg?",                 SystemType = typeof(NpgsqlLSeg),                 ProviderSpecificType = "NpgsqlLSeg",     IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "boxDataType",         ColumnType = "box",                             MemberName = "boxDataType",         MemberType = "NpgsqlBox?",                  SystemType = typeof(NpgsqlBox),                  ProviderSpecificType = "NpgsqlBox",      IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "pathDataType",        ColumnType = "path",                            MemberName = "pathDataType",        MemberType = "NpgsqlPath?",                 SystemType = typeof(NpgsqlPath),                 ProviderSpecificType = "NpgsqlPath",     IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "polygonDataType",     ColumnType = "polygon",                         MemberName = "polygonDataType",     MemberType = "NpgsqlPolygon?",              SystemType = typeof(NpgsqlPolygon),              ProviderSpecificType = "NpgsqlPolygon",  IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "circleDataType",      ColumnType = "circle",                          MemberName = "circleDataType",      MemberType = "NpgsqlCircle?",               SystemType = typeof(NpgsqlCircle),               ProviderSpecificType = "NpgsqlCircle",   IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "lineDataType",        ColumnType = "line",                            MemberName = "lineDataType",        MemberType = "NpgsqlLine?",                 SystemType = typeof(NpgsqlLine),                 ProviderSpecificType = "NpgsqlLine",     IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "inetDataType",        ColumnType = "inet",                            MemberName = "inetDataType",        MemberType = "IPAddress",                   SystemType = typeof(IPAddress),                  ProviderSpecificType = "NpgsqlInet",     IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "cidrDataType",        ColumnType = "cidr",                            MemberName = "cidrDataType",        MemberType = "ValueTuple<IPAddress, byte>?", SystemType = typeof(ValueTuple<IPAddress, byte>), ProviderSpecificType = "NpgsqlCidr",   IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "macaddrDataType",     ColumnType = "macaddr",                         MemberName = "macaddrDataType",     MemberType = "PhysicalAddress",             SystemType = typeof(PhysicalAddress),                                         IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "macaddr8DataType",    ColumnType = "macaddr8",                        MemberName = "macaddr8DataType",    MemberType = "PhysicalAddress",             SystemType = typeof(PhysicalAddress),                                         IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "jsonDataType",        ColumnType = "json",                            MemberName = "jsonDataType",        MemberType = "string",                      SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.Json           },
							new ColumnSchema { ColumnName = "jsonbDataType",       ColumnType = "jsonb",                           MemberName = "jsonbDataType",       MemberType = "string",                      SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.BinaryJson     },
							new ColumnSchema { ColumnName = "xmlDataType",         ColumnType = "xml",                             MemberName = "xmlDataType",         MemberType = "string",                      SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.Xml            },
							new ColumnSchema { ColumnName = "varBitDataType",      ColumnType = "bit varying(-1)", // TODO: length missing from npgsql
							                                                                                                       MemberName = "varBitDataType",      MemberType = "BitArray",                    SystemType = typeof(BitArray),                                                IsNullable = true, DataType = DataType.BitArray       },
							new ColumnSchema { ColumnName = "strarray",            ColumnType = "text[]",                          MemberName = "strarray",            MemberType = "string[]",                    SystemType = typeof(string[]),                                                IsNullable = true, DataType = DataType.Undefined      },
							new ColumnSchema { ColumnName = "intarray",            ColumnType = "integer[]",                       MemberName = "intarray",            MemberType = "int[]",                       SystemType = typeof(int[]),                                                   IsNullable = true, DataType = DataType.Undefined      },
							new ColumnSchema { ColumnName = "int2darray",          ColumnType = "integer[]",                       MemberName = "int2darray",          MemberType = "int[]",                       SystemType = typeof(int[]),                                                   IsNullable = true, DataType = DataType.Undefined      },
							new ColumnSchema { ColumnName = "longarray",           ColumnType = "bigint[]",                        MemberName = "longarray",           MemberType = "long[]",                      SystemType = typeof(long[]),                                                IsNullable = true, DataType = DataType.Undefined      },
							new ColumnSchema { ColumnName = "doublearray",         ColumnType = "double precision[]",              MemberName = "doublearray",         MemberType = "double[]",                    SystemType = typeof(double[]),                                               IsNullable = true, DataType = DataType.Undefined      },
							new ColumnSchema { ColumnName = "intervalarray",       ColumnType = "interval[]",                      MemberName = "intervalarray",       MemberType = "TimeSpan[]",                  SystemType = typeof(TimeSpan[]),                                              IsNullable = true, DataType = DataType.Undefined      },
							new ColumnSchema { ColumnName = "numericarray",        ColumnType = "numeric[]",                       MemberName = "numericarray",        MemberType = "decimal[]",                   SystemType = typeof(decimal[]),                                               IsNullable = true, DataType = DataType.Undefined      },
							new ColumnSchema { ColumnName = "decimalarray",        ColumnType = "numeric[]",                       MemberName = "decimalarray",        MemberType = "decimal[]",                   SystemType = typeof(decimal[]),                                               IsNullable = true, DataType = DataType.Undefined      },
						}
					},
					SimilarTables = new List<TableSchema>
					{
						new TableSchema { TableName = "AllTypes" }
					}
				});

				// test parameters directions
				yield return new ProcedureTestCase(new ProcedureSchema
				{
					CatalogName         = "SET_BY_TEST",
					SchemaName          = "public",
					ProcedureName       = "TestFunctionParameters",
					MemberName          = "TestFunctionParameters",
					IsFunction          = true,
					IsTableFunction     = false,
					IsAggregateFunction = false,
					IsDefaultSchema     = true,
					IsLoaded            = false,
					Parameters          = new List<ParameterSchema>()
					{
						new ParameterSchema { SchemaName = "param1", SchemaType = "integer", IsIn  = true,               ParameterName = "param1", ParameterType = "int?", SystemType = typeof(int), DataType = DataType.Int32 },
						new ParameterSchema { SchemaName = "param2", SchemaType = "integer", IsIn  = true, IsOut = true, ParameterName = "param2", ParameterType = "int?", SystemType = typeof(int), DataType = DataType.Int32 },
						new ParameterSchema { SchemaName = "param3", SchemaType = "integer",               IsOut = true, ParameterName = "param3", ParameterType = "int?", SystemType = typeof(int), DataType = DataType.Int32 }
					}
				});

				// table function with single column result
				yield return new ProcedureTestCase(new ProcedureSchema
				{
					CatalogName         = "SET_BY_TEST",
					SchemaName          = "public",
					ProcedureName       = "TestTableFunction",
					MemberName          = "TestTableFunction",
					IsFunction          = true,
					IsTableFunction     = true,
					IsAggregateFunction = false,
					IsDefaultSchema     = true,
					IsLoaded            = true,
					Parameters          = new List<ParameterSchema>
					{
						new ParameterSchema { SchemaName = "param1", SchemaType = "integer", IsIn  = true, ParameterName = "param1", ParameterType = "int?", SystemType = typeof(int), DataType = DataType.Int32 },
						new ParameterSchema { SchemaName = "param2", SchemaType = "integer", IsOut = true, ParameterName = "param2", ParameterType = "int?", SystemType = typeof(int), DataType = DataType.Int32 }
					},
					ResultTable = new TableSchema
					{
						IsProcedureResult = true,
						TypeName          = "TestTableFunctionResult",
						Columns           = new List<ColumnSchema>
						{
							new ColumnSchema { ColumnName = "param2", ColumnType = "int4", MemberName = "param2", MemberType = "int?", SystemType = typeof(int), IsNullable = true, DataType = DataType.Int32 }
						}
					}
				});

				// table function with multiple columns result
				yield return new ProcedureTestCase(new ProcedureSchema
				{
					CatalogName         = "SET_BY_TEST",
					SchemaName          = "public",
					ProcedureName       = "TestTableFunction1",
					MemberName          = "TestTableFunction1",
					IsFunction          = true,
					IsTableFunction     = true,
					IsAggregateFunction = false,
					IsDefaultSchema     = true,
					IsLoaded            = true,
					Parameters          = new List<ParameterSchema>()
					{
						new ParameterSchema { SchemaName = "param1", SchemaType = "integer", IsIn  = true, ParameterName = "param1", ParameterType = "int?", SystemType = typeof(int), DataType = DataType.Int32 },
						new ParameterSchema { SchemaName = "param2", SchemaType = "integer", IsIn  = true, ParameterName = "param2", ParameterType = "int?", SystemType = typeof(int), DataType = DataType.Int32 },
						new ParameterSchema { SchemaName = "param3", SchemaType = "integer", IsOut = true, ParameterName = "param3", ParameterType = "int?", SystemType = typeof(int), DataType = DataType.Int32 },
						new ParameterSchema { SchemaName = "param4", SchemaType = "integer", IsOut = true, ParameterName = "param4", ParameterType = "int?", SystemType = typeof(int), DataType = DataType.Int32 }
					},
					ResultTable = new TableSchema
					{
						IsProcedureResult = true,
						TypeName          = "TestTableFunction1Result",
						Columns           = new List<ColumnSchema>
						{
							new ColumnSchema { ColumnName = "param3", ColumnType = "int4", MemberName = "param3", MemberType = "int?", SystemType = typeof(int), IsNullable = true, DataType = DataType.Int32 },
							new ColumnSchema { ColumnName = "param4", ColumnType = "int4", MemberName = "param4", MemberType = "int?", SystemType = typeof(int), IsNullable = true, DataType = DataType.Int32 }
						}
					}
				});

				// scalar function
				yield return new ProcedureTestCase(new ProcedureSchema
				{
					CatalogName         = "SET_BY_TEST",
					SchemaName          = "public",
					ProcedureName       = "TestScalarFunction",
					MemberName          = "TestScalarFunction",
					IsFunction          = true,
					IsTableFunction     = false,
					IsAggregateFunction = false,
					IsDefaultSchema     = true,
					IsLoaded            = false,
					Parameters          = new List<ParameterSchema>
					{
						new ParameterSchema
						{
							SchemaType    = "character varying",
							IsResult      = true,
							ParameterName = "__skip", // name is dynamic as it is generated by schema loader
							ParameterType = "string",
							SystemType    = typeof(string),
							DataType      = DataType.NVarChar
						},
						new ParameterSchema
						{
							SchemaName    = "param",
							SchemaType    = "integer",
							IsIn          = true,
							ParameterName = "param",
							ParameterType = "int?",
							SystemType    = typeof(int),
							DataType      = DataType.Int32
						}
					}
				});

				// scalar function
				yield return new ProcedureTestCase(new ProcedureSchema()
				{
					CatalogName         = "SET_BY_TEST",
					SchemaName          = "public",
					ProcedureName       = "TestSingleOutParameterFunction",
					MemberName          = "TestSingleOutParameterFunction",
					IsFunction          = true,
					IsTableFunction     = false,
					IsAggregateFunction = false,
					IsDefaultSchema     = true,
					IsLoaded            = false,
					Parameters          = new List<ParameterSchema>
					{
						new ParameterSchema { SchemaName = "param1", SchemaType = "integer", IsIn  = true, ParameterName = "param1", ParameterType = "int?", SystemType = typeof(int), DataType = DataType.Int32 },
						new ParameterSchema { SchemaName = "param2", SchemaType = "integer", IsOut = true, ParameterName = "param2", ParameterType = "int?", SystemType = typeof(int), DataType = DataType.Int32 }
					}
				});

				// custom aggregate
				yield return new ProcedureTestCase(new ProcedureSchema()
				{
					CatalogName         = "SET_BY_TEST",
					SchemaName          = "public",
					ProcedureName       = "test_avg",
					MemberName          = "test_avg",
					IsFunction          = true,
					IsTableFunction     = false,
					IsAggregateFunction = true,
					IsDefaultSchema     = true,
					IsLoaded            = false,
					Parameters          = new List<ParameterSchema>()
					{
						new ParameterSchema { SchemaType = "double precision", IsResult = true, ParameterName = "__skip", ParameterType = "double?", SystemType = typeof(double), DataType = DataType.Double },
						new ParameterSchema { SchemaType = "double precision", IsIn     = true, ParameterName = "__skip", ParameterType = "double?", SystemType = typeof(double), DataType = DataType.Double }
					}
				});
			}
		}

		[Test]
		public void ProceduresSchemaProviderTest(
			[IncludeDataSources(TestProvName.AllPostgreSQL)] string context,
			[ValueSource(nameof(ProcedureTestCases))] ProcedureTestCase testCase)
		{
			var macaddr8Supported = context.IsAnyOf(TestProvName.AllPostgreSQL10Plus);
			var jsonbSupported    = context.IsAnyOf(TestProvName.AllPostgreSQL95Plus);

			using (var db = (DataConnection)GetDataContext(context))
			{
				var expectedProc = testCase.Schema;
				expectedProc.CatalogName = TestUtils.GetDatabaseName(db, context);

				// schema load takes too long if system schema included
				// added SchemaProceduresLoadedTest to test system schema
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(
					db,
					new GetSchemaOptions() { ExcludedSchemas = new[] { "pg_catalog" } });

				var procedures = schema.Procedures.Where(_ => _.ProcedureName == expectedProc.ProcedureName).ToList();

				Assert.That(procedures, Has.Count.EqualTo(1));

				var procedure = procedures[0];

				Assert.Multiple(() =>
				{
					Assert.That(procedure.CatalogName, Is.EqualTo(expectedProc.CatalogName));
					Assert.That(procedure.SchemaName, Is.EqualTo(expectedProc.SchemaName));
					Assert.That(procedure.MemberName, Is.EqualTo(expectedProc.MemberName));
					Assert.That(procedure.IsTableFunction, Is.EqualTo(expectedProc.IsTableFunction));
					Assert.That(procedure.IsAggregateFunction, Is.EqualTo(expectedProc.IsAggregateFunction));
					Assert.That(procedure.IsDefaultSchema, Is.EqualTo(expectedProc.IsDefaultSchema));
					Assert.That(procedure.IsFunction, Is.EqualTo(expectedProc.IsFunction));
					Assert.That(procedure.IsLoaded, Is.EqualTo(expectedProc.IsLoaded));
					Assert.That(procedure.IsResultDynamic, Is.EqualTo(expectedProc.IsResultDynamic));

					Assert.That(procedure.ResultException, Is.Null);

					Assert.That(procedure.Parameters, Has.Count.EqualTo(expectedProc.Parameters.Count));
				});

				for (var i = 0; i < procedure.Parameters.Count; i++)
				{
					var actualParam = procedure.Parameters[i];
					var expectedParam = expectedProc.Parameters[i];

					Assert.Multiple(() =>
					{
						Assert.That(expectedParam, Is.Not.Null);

						Assert.That(actualParam.SchemaName, Is.EqualTo(expectedParam.SchemaName));
					});
					if (expectedParam.ParameterName != "__skip")
						Assert.That(actualParam.ParameterName, Is.EqualTo(expectedParam.ParameterName));
					Assert.Multiple(() =>
					{
						Assert.That(actualParam.SchemaType, Is.EqualTo(expectedParam.SchemaType));
						Assert.That(actualParam.IsIn, Is.EqualTo(expectedParam.IsIn));
						Assert.That(actualParam.IsOut, Is.EqualTo(expectedParam.IsOut));
						Assert.That(actualParam.IsResult, Is.EqualTo(expectedParam.IsResult));
						Assert.That(actualParam.Size, Is.EqualTo(expectedParam.Size));
						Assert.That(actualParam.ParameterType, Is.EqualTo(expectedParam.ParameterType));
						Assert.That(actualParam.SystemType, Is.EqualTo(expectedParam.SystemType));
						Assert.That(actualParam.DataType, Is.EqualTo(expectedParam.DataType));
						Assert.That(actualParam.ProviderSpecificType, Is.EqualTo(expectedParam.ProviderSpecificType));
					});
				}

				if (expectedProc.ResultTable == null)
				{
					Assert.Multiple(() =>
					{
						Assert.That(procedure.ResultTable, Is.Null);

						// maybe it is worth changing
						Assert.That(procedure.SimilarTables, Is.Null);
					});
				}
				else
				{
					Assert.That(procedure.ResultTable, Is.Not.Null);

					var expectedTable = expectedProc.ResultTable;
					var actualTable = procedure.ResultTable;

					Assert.Multiple(() =>
					{
						Assert.That(actualTable!.ID, Is.EqualTo(expectedTable.ID));
						Assert.That(actualTable.CatalogName, Is.EqualTo(expectedTable.CatalogName));
						Assert.That(actualTable.SchemaName, Is.EqualTo(expectedTable.SchemaName));
						Assert.That(actualTable.TableName, Is.EqualTo(expectedTable.TableName));
						Assert.That(actualTable.Description, Is.EqualTo(expectedTable.Description));
						Assert.That(actualTable.IsDefaultSchema, Is.EqualTo(expectedTable.IsDefaultSchema));
						Assert.That(actualTable.IsView, Is.EqualTo(expectedTable.IsView));
						Assert.That(actualTable.IsProcedureResult, Is.EqualTo(expectedTable.IsProcedureResult));
						Assert.That(actualTable.TypeName, Is.EqualTo(expectedTable.TypeName));
						Assert.That(actualTable.IsProviderSpecific, Is.EqualTo(expectedTable.IsProviderSpecific));

						Assert.That(actualTable.ForeignKeys, Is.Not.Null);
					});
					Assert.That(actualTable.ForeignKeys, Is.Empty);

					var expectedColumns = expectedTable.Columns;
					if (!jsonbSupported)
						expectedColumns = expectedColumns.Where(_ => _.ColumnType != "jsonb").ToList();
					if (!macaddr8Supported)
						expectedColumns = expectedColumns.Where(_ => _.ColumnType != "macaddr8").ToList();

					Assert.That(actualTable.Columns, Has.Count.EqualTo(expectedColumns.Count));

					foreach (var actualColumn in actualTable.Columns)
					{
						var expectedColumn = expectedColumns
							.Where(_ => _.ColumnName == actualColumn.ColumnName)
							.SingleOrDefault()!;

						Assert.That(expectedColumn, Is.Not.Null);

						// npgsql4 uses more standard names like 'integer' instead of 'int4'
						// and we don't have proper type synonyms support/normalization in 2.x
						if (expectedColumn.ColumnType == "int4")
							Assert.That(new[] { "int4", "integer" }, Does.Contain(actualColumn.ColumnType));
						else if (expectedColumn.ColumnType == "int8")
							Assert.That(new[] { "int8", "bigint" }, Does.Contain(actualColumn.ColumnType));
						else if (expectedColumn.ColumnType == "int2")
							Assert.That(new[] { "int2", "smallint" }, Does.Contain(actualColumn.ColumnType));
						else if (expectedColumn.ColumnType == "float8")
							Assert.That(new[] { "float8", "double precision" }, Does.Contain(actualColumn.ColumnType));
						else if (expectedColumn.ColumnType == "float4")
							Assert.That(new[] { "float4", "real" }, Does.Contain(actualColumn.ColumnType));
						else if (expectedColumn.ColumnType == "character(1)")
							Assert.That(new[] { "character(1)", "character" }, Does.Contain(actualColumn.ColumnType));
						else if (expectedColumn.ColumnType == "bit(-1)")
							Assert.That(new[] { "bit(-1)", "bit(3)" }, Does.Contain(actualColumn.ColumnType));
						else if (expectedColumn.ColumnType == "bool")
							Assert.That(new[] { "bool", "boolean" }, Does.Contain(actualColumn.ColumnType));
						else if (expectedColumn.ColumnType == "time (0) with time zone")
							Assert.That(new[] { "time (0) with time zone", "time with time zone" }, Does.Contain(actualColumn.ColumnType));
						else if (expectedColumn.ColumnType == "time (0) without time zone")
							Assert.That(new[] { "time (0) without time zone", "time without time zone" }, Does.Contain(actualColumn.ColumnType));
						else if (expectedColumn.ColumnType == "timestamp (0) with time zone")
							Assert.That(new[] { "timestamp (0) with time zone", "timestamp with time zone" }, Does.Contain(actualColumn.ColumnType));
						else if (expectedColumn.ColumnType == "timestamp (0) without time zone")
							Assert.That(new[] { "timestamp (0) without time zone", "timestamp without time zone" }, Does.Contain(actualColumn.ColumnType));
						else
							Assert.That(actualColumn.ColumnType, Is.EqualTo(expectedColumn.ColumnType));

						Assert.Multiple(() =>
						{
							Assert.That(actualColumn.IsNullable, Is.EqualTo(expectedColumn.IsNullable));
							Assert.That(actualColumn.IsIdentity, Is.EqualTo(expectedColumn.IsIdentity));
							Assert.That(actualColumn.IsPrimaryKey, Is.EqualTo(expectedColumn.IsPrimaryKey));
							Assert.That(actualColumn.PrimaryKeyOrder, Is.EqualTo(expectedColumn.PrimaryKeyOrder));
							Assert.That(actualColumn.Description, Is.EqualTo(expectedColumn.Description));
							Assert.That(actualColumn.MemberName, Is.EqualTo(expectedColumn.MemberName));
							Assert.That(actualColumn.MemberType, Is.EqualTo(expectedColumn.MemberType));
							Assert.That(actualColumn.ProviderSpecificType, Is.EqualTo(expectedColumn.ProviderSpecificType));
							Assert.That(actualColumn.SystemType, Is.EqualTo(expectedColumn.SystemType));
							Assert.That(actualColumn.DataType, Is.EqualTo(expectedColumn.DataType));
							Assert.That(actualColumn.SkipOnInsert, Is.EqualTo(expectedColumn.SkipOnInsert));
							Assert.That(actualColumn.SkipOnUpdate, Is.EqualTo(expectedColumn.SkipOnUpdate));
							Assert.That(actualColumn.Length, Is.EqualTo(expectedColumn.Length));
						});
						Assert.Multiple(() =>
						{
							Assert.That(actualColumn.Precision, Is.EqualTo(expectedColumn.Precision));
							Assert.That(actualColumn.Scale, Is.EqualTo(expectedColumn.Scale));
							Assert.That(actualColumn.Table, Is.EqualTo(actualTable));
						});
					}

					Assert.That(procedure.SimilarTables, Is.Not.Null);

					foreach (var table in procedure.SimilarTables!)
					{
						var tbl = expectedProc.SimilarTables!
							.Where(_ => _.TableName == table.TableName)
							.SingleOrDefault();

						Assert.That(tbl, Is.Not.Null);
					}
				}
			}
		}

		[Test]
		public void DescriptionTest([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

				var table  = schema.Tables.First(t => t.TableName == "Person");
				var column = table.Columns.First(t => t.ColumnName == "PersonID");

				Assert.Multiple(() =>
				{
					Assert.That(table.Description, Is.EqualTo("This is the Person table"));
					Assert.That(column.Description, Is.EqualTo("This is the Person.PersonID column"));
				});
			}
		}

		[Test]
		public void TestMaterializedViewSchema([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

				var view = schema.Tables.FirstOrDefault(t => t.TableName == "Issue2023");

				if (context.Contains("9.2"))
				{
					// test that schema load is not broken by materialized view support for old versions
					Assert.That(view, Is.Null);
					return;
				}

				Assert.That(view, Is.Not.Null);

				Assert.Multiple(() =>
				{
					Assert.That(view!.ID, Is.EqualTo(view.CatalogName + ".public.Issue2023"));
					Assert.That(view.CatalogName, Is.Not.Null);
					Assert.That(view.SchemaName, Is.EqualTo("public"));
					Assert.That(view.TableName, Is.EqualTo("Issue2023"));
					Assert.That(view.Description, Is.EqualTo("This is the Issue2023 matview"));
					Assert.That(view.IsDefaultSchema, Is.EqualTo(true));
					Assert.That(view.IsView, Is.EqualTo(true));
					Assert.That(view.IsProcedureResult, Is.EqualTo(false));
					Assert.That(view.TypeName, Is.EqualTo("Issue2023"));
					Assert.That(view.IsProviderSpecific, Is.EqualTo(false));
					Assert.That(view.ForeignKeys, Is.Empty);
					Assert.That(view.Columns, Has.Count.EqualTo(5));
				});

				Assert.Multiple(() =>
				{
					Assert.That(view.Columns[0].ColumnName, Is.EqualTo("PersonID"));
					Assert.That(view.Columns[0].ColumnType, Is.EqualTo("integer"));
					Assert.That(view.Columns[0].IsNullable, Is.EqualTo(true));
					Assert.That(view.Columns[0].IsIdentity, Is.EqualTo(false));
					Assert.That(view.Columns[0].IsPrimaryKey, Is.EqualTo(false));
					Assert.That(view.Columns[0].PrimaryKeyOrder, Is.EqualTo(-1));
					Assert.That(view.Columns[0].Description, Is.EqualTo("This is the Issue2023.PersonID column"));
					Assert.That(view.Columns[0].MemberName, Is.EqualTo("PersonID"));
					Assert.That(view.Columns[0].MemberType, Is.EqualTo("int?"));
					Assert.That(view.Columns[0].ProviderSpecificType, Is.EqualTo(null));
					Assert.That(view.Columns[0].SystemType, Is.EqualTo(typeof(int)));
					Assert.That(view.Columns[0].DataType, Is.EqualTo(DataType.Int32));
					Assert.That(view.Columns[0].SkipOnInsert, Is.EqualTo(true));
					Assert.That(view.Columns[0].SkipOnUpdate, Is.EqualTo(true));
					Assert.That(view.Columns[0].Length, Is.EqualTo(null));
				});
				Assert.Multiple(() =>
				{
					// TODO: maybe we should fix it?
					Assert.That(view.Columns[0].Precision, Is.EqualTo(32));
					Assert.That(view.Columns[0].Scale, Is.EqualTo(0));
					Assert.That(view.Columns[0].Table, Is.EqualTo(view));

					Assert.That(view.Columns[1].ColumnName, Is.EqualTo("FirstName"));
					Assert.That(view.Columns[1].ColumnType, Is.EqualTo("character varying(50)"));
					Assert.That(view.Columns[1].IsNullable, Is.EqualTo(true));
					Assert.That(view.Columns[1].IsIdentity, Is.EqualTo(false));
					Assert.That(view.Columns[1].IsPrimaryKey, Is.EqualTo(false));
					Assert.That(view.Columns[1].PrimaryKeyOrder, Is.EqualTo(-1));
					Assert.That(view.Columns[1].Description, Is.Null);
					Assert.That(view.Columns[1].MemberName, Is.EqualTo("FirstName"));
					Assert.That(view.Columns[1].MemberType, Is.EqualTo("string"));
					Assert.That(view.Columns[1].ProviderSpecificType, Is.EqualTo(null));
					Assert.That(view.Columns[1].SystemType, Is.EqualTo(typeof(string)));
					Assert.That(view.Columns[1].DataType, Is.EqualTo(DataType.NVarChar));
					Assert.That(view.Columns[1].SkipOnInsert, Is.EqualTo(true));
					Assert.That(view.Columns[1].SkipOnUpdate, Is.EqualTo(true));
					Assert.That(view.Columns[1].Length, Is.EqualTo(50));
				});
				Assert.Multiple(() =>
				{
					Assert.That(view.Columns[1].Precision, Is.EqualTo(null));
					Assert.That(view.Columns[1].Scale, Is.EqualTo(null));
					Assert.That(view.Columns[1].Table, Is.EqualTo(view));

					Assert.That(view.Columns[2].ColumnName, Is.EqualTo("LastName"));
					Assert.That(view.Columns[2].ColumnType, Is.EqualTo("character varying(50)"));
					Assert.That(view.Columns[2].IsNullable, Is.EqualTo(true));
					Assert.That(view.Columns[2].IsIdentity, Is.EqualTo(false));
					Assert.That(view.Columns[2].IsPrimaryKey, Is.EqualTo(false));
					Assert.That(view.Columns[2].PrimaryKeyOrder, Is.EqualTo(-1));
					Assert.That(view.Columns[2].Description, Is.Null);
					Assert.That(view.Columns[2].MemberName, Is.EqualTo("LastName"));
					Assert.That(view.Columns[2].MemberType, Is.EqualTo("string"));
					Assert.That(view.Columns[2].ProviderSpecificType, Is.EqualTo(null));
					Assert.That(view.Columns[2].SystemType, Is.EqualTo(typeof(string)));
					Assert.That(view.Columns[2].DataType, Is.EqualTo(DataType.NVarChar));
					Assert.That(view.Columns[2].SkipOnInsert, Is.EqualTo(true));
					Assert.That(view.Columns[2].SkipOnUpdate, Is.EqualTo(true));
					Assert.That(view.Columns[2].Length, Is.EqualTo(50));
				});
				Assert.Multiple(() =>
				{
					Assert.That(view.Columns[2].Precision, Is.EqualTo(null));
					Assert.That(view.Columns[2].Scale, Is.EqualTo(null));
					Assert.That(view.Columns[2].Table, Is.EqualTo(view));

					Assert.That(view.Columns[3].ColumnName, Is.EqualTo("MiddleName"));
					Assert.That(view.Columns[3].ColumnType, Is.EqualTo("character varying(50)"));
					Assert.That(view.Columns[3].IsNullable, Is.EqualTo(true));
					Assert.That(view.Columns[3].IsIdentity, Is.EqualTo(false));
					Assert.That(view.Columns[3].IsPrimaryKey, Is.EqualTo(false));
					Assert.That(view.Columns[3].PrimaryKeyOrder, Is.EqualTo(-1));
					Assert.That(view.Columns[3].Description, Is.Null);
					Assert.That(view.Columns[3].MemberName, Is.EqualTo("MiddleName"));
					Assert.That(view.Columns[3].MemberType, Is.EqualTo("string"));
					Assert.That(view.Columns[3].ProviderSpecificType, Is.EqualTo(null));
					Assert.That(view.Columns[3].SystemType, Is.EqualTo(typeof(string)));
					Assert.That(view.Columns[3].DataType, Is.EqualTo(DataType.NVarChar));
					Assert.That(view.Columns[3].SkipOnInsert, Is.EqualTo(true));
					Assert.That(view.Columns[3].SkipOnUpdate, Is.EqualTo(true));
					Assert.That(view.Columns[3].Length, Is.EqualTo(50));
				});
				Assert.Multiple(() =>
				{
					Assert.That(view.Columns[3].Precision, Is.EqualTo(null));
					Assert.That(view.Columns[3].Scale, Is.EqualTo(null));
					Assert.That(view.Columns[3].Table, Is.EqualTo(view));

					Assert.That(view.Columns[4].ColumnName, Is.EqualTo("Gender"));
					Assert.That(view.Columns[4].ColumnType, Is.EqualTo("character(1)"));
					Assert.That(view.Columns[4].IsNullable, Is.EqualTo(true));
					Assert.That(view.Columns[4].IsIdentity, Is.EqualTo(false));
					Assert.That(view.Columns[4].IsPrimaryKey, Is.EqualTo(false));
					Assert.That(view.Columns[4].PrimaryKeyOrder, Is.EqualTo(-1));
					Assert.That(view.Columns[4].Description, Is.Null);
					Assert.That(view.Columns[4].MemberName, Is.EqualTo("Gender"));
					Assert.That(view.Columns[4].MemberType, Is.EqualTo("char?"));
					Assert.That(view.Columns[4].ProviderSpecificType, Is.EqualTo(null));
					Assert.That(view.Columns[4].SystemType, Is.EqualTo(typeof(char)));
					Assert.That(view.Columns[4].DataType, Is.EqualTo(DataType.NChar));
					Assert.That(view.Columns[4].SkipOnInsert, Is.EqualTo(true));
					Assert.That(view.Columns[4].SkipOnUpdate, Is.EqualTo(true));
					Assert.That(view.Columns[4].Length, Is.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(view.Columns[4].Precision, Is.EqualTo(null));
					Assert.That(view.Columns[4].Scale, Is.EqualTo(null));
					Assert.That(view.Columns[4].Table, Is.EqualTo(view));
				});
			}
		}
	}
}
