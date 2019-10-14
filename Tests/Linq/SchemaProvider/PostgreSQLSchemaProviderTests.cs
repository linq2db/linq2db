using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.SchemaProvider;

using NpgsqlTypes;

using NUnit.Framework;

#pragma warning disable 618

namespace Tests.SchemaProvider
{
	using Model;

	[TestFixture]
	public class PostgreSQLTests : TestBase
	{
		public static IEnumerable<ProcedureSchema> ProcedureTestCases
		{
			get
			{
				// test table function schema
				yield return new ProcedureSchema()
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
							new ColumnSchema { ColumnName = "ID",                  ColumnType = "int4",                            MemberName = "ID",                  MemberType = "int?",            SystemType = typeof(int),                                                     IsNullable = true, // must be false, but we don't get this information from provider
							                                                                                                                                                                                                                                                                            DataType = DataType.Int32          },
							new ColumnSchema { ColumnName = "bigintDataType",      ColumnType = "int8",                            MemberName = "bigintDataType",      MemberType = "long?",           SystemType = typeof(long),                                                    IsNullable = true, DataType = DataType.Int64          },
							new ColumnSchema { ColumnName = "numericDataType",     ColumnType = "numeric(0,0)",                    MemberName = "numericDataType",     MemberType = "decimal?",        SystemType = typeof(decimal),                                                 IsNullable = true, DataType = DataType.Decimal        },
							new ColumnSchema { ColumnName = "smallintDataType",    ColumnType = "int2",                            MemberName = "smallintDataType",    MemberType = "short?",          SystemType = typeof(short),                                                   IsNullable = true, DataType = DataType.Int16          },
							new ColumnSchema { ColumnName = "intDataType",         ColumnType = "int4",                            MemberName = "intDataType",         MemberType = "int?",            SystemType = typeof(int),                                                     IsNullable = true, DataType = DataType.Int32          },
							new ColumnSchema { ColumnName = "moneyDataType",       ColumnType = "money",                           MemberName = "moneyDataType",       MemberType = "decimal?",        SystemType = typeof(decimal),                                                 IsNullable = true, DataType = DataType.Money          },
							new ColumnSchema { ColumnName = "doubleDataType",      ColumnType = "float8",                          MemberName = "doubleDataType",      MemberType = "double?",         SystemType = typeof(double),                                                  IsNullable = true, DataType = DataType.Double         },
							new ColumnSchema { ColumnName = "realDataType",        ColumnType = "float4",                          MemberName = "realDataType",        MemberType = "float?",          SystemType = typeof(float),                                                   IsNullable = true, DataType = DataType.Single         },
							new ColumnSchema { ColumnName = "timestampDataType",   ColumnType = "timestamp (0) without time zone", MemberName = "timestampDataType",   MemberType = "DateTime?",       SystemType = typeof(DateTime),       ProviderSpecificType = "NpgsqlDate",     IsNullable = true, DataType = DataType.DateTime2      },
							new ColumnSchema { ColumnName = "timestampTZDataType", ColumnType = "timestamp (0) with time zone",    MemberName = "timestampTZDataType", MemberType = "DateTimeOffset?", SystemType = typeof(DateTimeOffset), ProviderSpecificType = "NpgsqlDate",     IsNullable = true, DataType = DataType.DateTimeOffset },
							new ColumnSchema { ColumnName = "dateDataType",        ColumnType = "date",                            MemberName = "dateDataType",        MemberType = "NpgsqlDate?",     SystemType = typeof(NpgsqlDate),     ProviderSpecificType = "NpgsqlDate",     IsNullable = true, DataType = DataType.Date           },
							new ColumnSchema { ColumnName = "timeDataType",        ColumnType = "time (0) without time zone",      MemberName = "timeDataType",        MemberType = "TimeSpan?",       SystemType = typeof(TimeSpan),                                                IsNullable = true, DataType = DataType.Time           },
							new ColumnSchema { ColumnName = "timeTZDataType",      ColumnType = "time (0) with time zone",         MemberName = "timeTZDataType",      MemberType = "DateTimeOffset?", SystemType = typeof(DateTimeOffset),                                          IsNullable = true, DataType = DataType.Time           },
							new ColumnSchema { ColumnName = "intervalDataType",    ColumnType = "interval(0)",                     MemberName = "intervalDataType",    MemberType = "NpgsqlTimeSpan?", SystemType = typeof(NpgsqlTimeSpan), ProviderSpecificType = "NpgsqlTimeSpan", IsNullable = true, DataType = DataType.Time           },
							new ColumnSchema { ColumnName = "charDataType",        ColumnType = "character(1)",                    MemberName = "charDataType",        MemberType = "char?",           SystemType = typeof(char),                                                    IsNullable = true, DataType = DataType.NChar          },
							new ColumnSchema { ColumnName = "char20DataType",      ColumnType = "character(20)",                   MemberName = "char20DataType",      MemberType = "string",          SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.NChar          },
							new ColumnSchema { ColumnName = "varcharDataType",     ColumnType = "character varying(20)",           MemberName = "varcharDataType",     MemberType = "string",          SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.NVarChar       },
							new ColumnSchema { ColumnName = "textDataType",        ColumnType = "text",                            MemberName = "textDataType",        MemberType = "string",          SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.Text           },
							new ColumnSchema { ColumnName = "binaryDataType",      ColumnType = "bytea",                           MemberName = "binaryDataType",      MemberType = "byte[]",          SystemType = typeof(byte[]),                                                  IsNullable = true, DataType = DataType.Binary         },
							new ColumnSchema { ColumnName = "uuidDataType",        ColumnType = "uuid",                            MemberName = "uuidDataType",        MemberType = "Guid?",           SystemType = typeof(Guid),                                                    IsNullable = true, DataType = DataType.Guid           },
							new ColumnSchema { ColumnName = "bitDataType",         ColumnType = "bit(-1)", // TODO: must be 3, but npgsql3 doesn't return it (npgsql4 does)
							                                                                                                       MemberName = "bitDataType",         MemberType = "BitArray",        SystemType = typeof(BitArray),                                                IsNullable = true, DataType = DataType.BitArray       },
							new ColumnSchema { ColumnName = "booleanDataType",     ColumnType = "bool",                            MemberName = "booleanDataType",     MemberType = "bool?",           SystemType = typeof(bool),                                                    IsNullable = true, DataType = DataType.Boolean        },
							new ColumnSchema { ColumnName = "colorDataType",       ColumnType = "public.color",                    MemberName = "colorDataType",       MemberType = "string",          SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.Undefined      },
							new ColumnSchema { ColumnName = "pointDataType",       ColumnType = "point",                           MemberName = "pointDataType",       MemberType = "NpgsqlPoint?",    SystemType = typeof(NpgsqlPoint),    ProviderSpecificType = "NpgsqlPoint",    IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "lsegDataType",        ColumnType = "lseg",                            MemberName = "lsegDataType",        MemberType = "NpgsqlLSeg?",     SystemType = typeof(NpgsqlLSeg),     ProviderSpecificType = "NpgsqlLSeg",     IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "boxDataType",         ColumnType = "box",                             MemberName = "boxDataType",         MemberType = "NpgsqlBox?",      SystemType = typeof(NpgsqlBox),      ProviderSpecificType = "NpgsqlBox",      IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "pathDataType",        ColumnType = "path",                            MemberName = "pathDataType",        MemberType = "NpgsqlPath?",     SystemType = typeof(NpgsqlPath),     ProviderSpecificType = "NpgsqlPath",     IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "polygonDataType",     ColumnType = "polygon",                         MemberName = "polygonDataType",     MemberType = "NpgsqlPolygon?",  SystemType = typeof(NpgsqlPolygon),  ProviderSpecificType = "NpgsqlPolygon",  IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "circleDataType",      ColumnType = "circle",                          MemberName = "circleDataType",      MemberType = "NpgsqlCircle?",   SystemType = typeof(NpgsqlCircle),   ProviderSpecificType = "NpgsqlCircle",   IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "lineDataType",        ColumnType = "line",                            MemberName = "lineDataType",        MemberType = "NpgsqlLine?",     SystemType = typeof(NpgsqlLine),     ProviderSpecificType = "NpgsqlLine",     IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "inetDataType",        ColumnType = "inet",                            MemberName = "inetDataType",        MemberType = "NpgsqlInet?",     SystemType = typeof(NpgsqlInet),     ProviderSpecificType = "NpgsqlInet",     IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "cidrDataType",        ColumnType = "cidr",                            MemberName = "cidrDataType",        MemberType = "NpgsqlInet?",     SystemType = typeof(NpgsqlInet),     ProviderSpecificType = "NpgsqlInet",     IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "macaddrDataType",     ColumnType = "macaddr",                         MemberName = "macaddrDataType",     MemberType = "PhysicalAddress", SystemType = typeof(PhysicalAddress),                                         IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "macaddr8DataType",    ColumnType = "macaddr8",                        MemberName = "macaddr8DataType",    MemberType = "PhysicalAddress", SystemType = typeof(PhysicalAddress),                                         IsNullable = true, DataType = DataType.Udt            },
							new ColumnSchema { ColumnName = "jsonDataType",        ColumnType = "json",                            MemberName = "jsonDataType",        MemberType = "string",          SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.Json           },
							new ColumnSchema { ColumnName = "jsonbDataType",       ColumnType = "jsonb",                           MemberName = "jsonbDataType",       MemberType = "string",          SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.BinaryJson     },
							new ColumnSchema { ColumnName = "xmlDataType",         ColumnType = "xml",                             MemberName = "xmlDataType",         MemberType = "string",          SystemType = typeof(string),                                                  IsNullable = true, DataType = DataType.Xml            },
							new ColumnSchema { ColumnName = "varBitDataType",      ColumnType = "bit varying(-1)", // TODO: length missing from npgsql
							                                                                                                       MemberName = "varBitDataType",      MemberType = "BitArray",        SystemType = typeof(BitArray),                                                IsNullable = true, DataType = DataType.BitArray       },
						}
					},
					SimilarTables = new List<TableSchema>
					{
						new TableSchema { TableName = "AllTypes" }
					}
				};

				// test parameters directions
				yield return new ProcedureSchema
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
				};

				// table function with single column result
				yield return new ProcedureSchema
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
				};

				// table function with multiple columns result
				yield return new ProcedureSchema
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
				};

				// scalar function
				yield return new ProcedureSchema
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
				};

				// scalar function
				yield return new ProcedureSchema()
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
				};

				// custom aggregate
				yield return new ProcedureSchema()
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
				};
			}
		}

		[Test]
		public void ProceduresSchemaProviderTest(
			[IncludeDataSources(TestProvName.AllPostgreSQL)] string context,
			[ValueSource(nameof(ProcedureTestCases))] ProcedureSchema expectedProc)
		{
			var macaddr8Supported =  context.Contains(TestProvName.PostgreSQL10) || context.Contains(TestProvName.PostgreSQL11);
			var jsonbSupported    = !context.Contains(ProviderName.PostgreSQL92) && !context.Contains(ProviderName.PostgreSQL93);
			using (var db = (DataConnection)GetDataContext(context))
			{
				expectedProc.CatalogName = TestUtils.GetDatabaseName(db);

				// schema load takes too long if system schema included
				// added SchemaProceduresLoadedTest to test system schema
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(
					db,
					TestUtils.GetDefaultSchemaOptions(context, new GetSchemaOptions() { ExcludedSchemas = new[] { "pg_catalog" } }));

				var procedures = schema.Procedures.Where(_ => _.ProcedureName == expectedProc.ProcedureName).ToList();

				Assert.AreEqual(1, procedures.Count);

				var procedure = procedures[0];

				Assert.AreEqual(expectedProc.CatalogName, procedure.CatalogName);
				Assert.AreEqual(expectedProc.SchemaName, procedure.SchemaName);
				Assert.AreEqual(expectedProc.MemberName, procedure.MemberName);
				Assert.AreEqual(expectedProc.IsTableFunction, procedure.IsTableFunction);
				Assert.AreEqual(expectedProc.IsAggregateFunction, procedure.IsAggregateFunction);
				Assert.AreEqual(expectedProc.IsDefaultSchema, procedure.IsDefaultSchema);
				Assert.AreEqual(expectedProc.IsFunction, procedure.IsFunction);
				Assert.AreEqual(expectedProc.IsLoaded, procedure.IsLoaded);
				Assert.AreEqual(expectedProc.IsResultDynamic, procedure.IsResultDynamic);

				Assert.IsNull(procedure.ResultException);

				Assert.AreEqual(expectedProc.Parameters.Count, procedure.Parameters.Count);

				for (var i = 0; i < procedure.Parameters.Count; i++)
				{
					var actualParam = procedure.Parameters[i];
					var expectedParam = expectedProc.Parameters[i];

					Assert.IsNotNull(expectedParam);

					Assert.AreEqual(expectedParam.SchemaName, actualParam.SchemaName);
					if (expectedParam.ParameterName != "__skip")
						Assert.AreEqual(expectedParam.ParameterName, actualParam.ParameterName);
					Assert.AreEqual(expectedParam.SchemaType, actualParam.SchemaType);
					Assert.AreEqual(expectedParam.IsIn, actualParam.IsIn);
					Assert.AreEqual(expectedParam.IsOut, actualParam.IsOut);
					Assert.AreEqual(expectedParam.IsResult, actualParam.IsResult);
					Assert.AreEqual(expectedParam.Size, actualParam.Size);
					Assert.AreEqual(expectedParam.ParameterType, actualParam.ParameterType);
					Assert.AreEqual(expectedParam.SystemType, actualParam.SystemType);
					Assert.AreEqual(expectedParam.DataType, actualParam.DataType);
					Assert.AreEqual(expectedParam.ProviderSpecificType, actualParam.ProviderSpecificType);
				}

				if (expectedProc.ResultTable == null)
				{
					Assert.IsNull(procedure.ResultTable);

					// maybe it is worth changing
					Assert.IsNull(procedure.SimilarTables);
				}
				else
				{
					Assert.IsNotNull(procedure.ResultTable);

					var expectedTable = expectedProc.ResultTable;
					var actualTable = procedure.ResultTable;

					Assert.AreEqual(expectedTable.ID, actualTable.ID);
					Assert.AreEqual(expectedTable.CatalogName, actualTable.CatalogName);
					Assert.AreEqual(expectedTable.SchemaName, actualTable.SchemaName);
					Assert.AreEqual(expectedTable.TableName, actualTable.TableName);
					Assert.AreEqual(expectedTable.Description, actualTable.Description);
					Assert.AreEqual(expectedTable.IsDefaultSchema, actualTable.IsDefaultSchema);
					Assert.AreEqual(expectedTable.IsView, actualTable.IsView);
					Assert.AreEqual(expectedTable.IsProcedureResult, actualTable.IsProcedureResult);
					Assert.AreEqual(expectedTable.TypeName, actualTable.TypeName);
					Assert.AreEqual(expectedTable.IsProviderSpecific, actualTable.IsProviderSpecific);

					Assert.IsNotNull(actualTable.ForeignKeys);
					Assert.IsEmpty(actualTable.ForeignKeys);

					var expectedColumns = expectedTable.Columns;
					if (!jsonbSupported)
						expectedColumns = expectedColumns.Where(_ => _.ColumnType != "jsonb").ToList();
					if (!macaddr8Supported)
						expectedColumns = expectedColumns.Where(_ => _.ColumnType != "macaddr8").ToList();

					Assert.AreEqual(expectedColumns.Count, actualTable.Columns.Count);

					foreach (var actualColumn in actualTable.Columns)
					{
						var expectedColumn = expectedColumns
							.Where(_ => _.ColumnName == actualColumn.ColumnName)
							.SingleOrDefault();

						Assert.IsNotNull(expectedColumn);

						// npgsql4 uses more standard names like 'integer' instead of 'int4'
						// and we don't have proper type synonyms support/normalization in 2.x
						if (expectedColumn.ColumnType == "int4")
							Assert.Contains(actualColumn.ColumnType, new[] { "int4", "integer" });
						else if (expectedColumn.ColumnType == "int8")
							Assert.Contains(actualColumn.ColumnType, new[] { "int8", "bigint" });
						else if (expectedColumn.ColumnType == "int2")
							Assert.Contains(actualColumn.ColumnType, new[] { "int2", "smallint" });
						else if (expectedColumn.ColumnType == "float8")
							Assert.Contains(actualColumn.ColumnType, new[] { "float8", "double precision" });
						else if (expectedColumn.ColumnType == "float4")
							Assert.Contains(actualColumn.ColumnType, new[] { "float4", "real" });
						else if (expectedColumn.ColumnType == "character(1)")
							Assert.Contains(actualColumn.ColumnType, new[] { "character(1)", "character" });
						else if (expectedColumn.ColumnType == "bit(-1)")
							Assert.Contains(actualColumn.ColumnType, new[] { "bit(-1)", "bit(3)" });
						else if (expectedColumn.ColumnType == "bool")
							Assert.Contains(actualColumn.ColumnType, new[] { "bool", "boolean" });
						else
						Assert.AreEqual(expectedColumn.ColumnType, actualColumn.ColumnType);

						Assert.AreEqual(expectedColumn.IsNullable, actualColumn.IsNullable);
						Assert.AreEqual(expectedColumn.IsIdentity, actualColumn.IsIdentity);
						Assert.AreEqual(expectedColumn.IsPrimaryKey, actualColumn.IsPrimaryKey);
						Assert.AreEqual(expectedColumn.PrimaryKeyOrder, actualColumn.PrimaryKeyOrder);
						Assert.AreEqual(expectedColumn.Description, actualColumn.Description);
						Assert.AreEqual(expectedColumn.MemberName, actualColumn.MemberName);
						Assert.AreEqual(expectedColumn.MemberType, actualColumn.MemberType);
						Assert.AreEqual(expectedColumn.ProviderSpecificType, actualColumn.ProviderSpecificType);
						Assert.AreEqual(expectedColumn.SystemType, actualColumn.SystemType);
						Assert.AreEqual(expectedColumn.DataType, actualColumn.DataType);
						Assert.AreEqual(expectedColumn.SkipOnInsert, actualColumn.SkipOnInsert);
						Assert.AreEqual(expectedColumn.SkipOnUpdate, actualColumn.SkipOnUpdate);
						Assert.AreEqual(expectedColumn.Length, actualColumn.Length);
						Assert.AreEqual(expectedColumn.Precision, actualColumn.Precision);
						Assert.AreEqual(expectedColumn.Scale, actualColumn.Scale);
						Assert.AreEqual(actualTable, actualColumn.Table);
					}

					Assert.IsNotNull(procedure.SimilarTables);

					foreach (var table in procedure.SimilarTables)
					{
						var tbl = expectedProc.SimilarTables
							.Where(_ => _.TableName == table.TableName)
							.SingleOrDefault();

						Assert.IsNotNull(tbl);
					}
				}
			}
		}

		[Test]
		public void DescriptionTest([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

				var table  = schema.Tables.First(t => t.TableName == "Person");
				var column = table.Columns.First(t => t.ColumnName == "PersonID");

				Assert.That(table. Description, Is.EqualTo("This is the Person table"));
				Assert.That(column.Description, Is.EqualTo("This is the Person.PersonID column"));
			}
		}
	}
}
