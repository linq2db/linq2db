using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Tests.Tools;

namespace Tests.xUpdate
{
	using ColumnBuilder        = Action<EntityMappingBuilder<CreateTableTypesTests.CreateTableTypes>>;
	using ValueBuilder         = Action<CreateTableTypesTests.CreateTableTypes>;
	using DefaultValueBuilder  = Action<string, CreateTableTypesTests.CreateTableTypes>;

	[TestFixture]
	public class CreateTableTypesTests : TestBase
	{
		[Table]
		public class CreateTableTypes
		{
			public int         Id                 { get; set; }
			public int         Int32              { get; set; }
			public int?        Int32Nullable      { get; set; }
			public long        Int64              { get; set; }
			public long?       Int64Nullable      { get; set; }
			public double      Double             { get; set; }
			public double?     DoubleNullable     { get; set; }
			public bool        Boolean            { get; set; }
			public bool?       BooleanNullable    { get; set; }
			public DateTime    DateTime           { get; set; } = new DateTime(2000, 1, 1); // to support narrow-ranged types
			public DateTime?   DateTimeNullable   { get; set; }
			public IntEnum     IntEnum            { get; set; }
			public IntEnum?    IntEnumNullable    { get; set; }
			public StringEnum  StringEnum         { get; set; }
			public StringEnum? StringEnumNullable { get; set; }
			public string      String             { get; set; }

			// converters test
			// see https://github.com/linq2db/linq2db/issues/1032
			public List<(uint field1, string field2)> StringConverted;

			public static IEqualityComparer<CreateTableTypes> Comparer = ComparerBuilder<CreateTableTypes>.GetEqualityComparer();

			public static List<(uint, string)> StringConvertedTestValue = new List<(uint, string)>
			{
				(1, "one"),
				(2, "two")
			};
		}

		public enum IntEnum
		{
			[MapValue(11)]
			Default,
			[MapValue(60)]
			Value = 50
		}

		public enum StringEnum
		{
			[MapValue("14")]
			Default,
			[MapValue("4")]
			Value1 = 3,
			[MapValue("40")]
			Value2 = 30
		}

		// TODO: add more cases: other types, different DataType values
		// TODO: add length validation to fields with length (text/binary)
		static IEnumerable<(ColumnBuilder, ValueBuilder, DefaultValueBuilder, Func<string, bool>, Func<string, bool>)> TestCases
		{
			get
			{
				yield return (e => e.HasColumn(_ => _.Int32),                                  v => v.Int32              = 1                                   , null,                                                                                                                        null,                            null);
				yield return (e => e.HasColumn(_ => _.Int32Nullable),                          v => v.Int32Nullable      = 2                                   , null,                                                                                                                        null,                            null);
				// Access doesn't have 64bit integer type
				yield return (e => e.HasColumn(_ => _.Int64),                                  v => v.Int64              = 3                                   , null,                                                                                                                        null,                            ctx => ctx.Contains("Access"));
				// Access doesn't have 64bit integer type
				yield return (e => e.HasColumn(_ => _.Int64Nullable),                          v => v.Int64Nullable      = 4                                   , null,                                                                                                                        null,                            ctx => ctx.Contains("Access"));
				// Firebird looses precision of double
				yield return (e => e.HasColumn(_ => _.Double),                                 v => v.Double             = 3.14                                , null,                                                                                                                        ctx => ctx.Contains("Firebird"), null);
				// Firebird looses precision of double
				yield return (e => e.HasColumn(_ => _.DoubleNullable),                         v => v.DoubleNullable     = 4.13                                , null,                                                                                                                        ctx => ctx.Contains("Firebird"), null);
				yield return (e => e.HasColumn(_ => _.Boolean),                                v => v.Boolean            = true                                , null,                                                                                                                        null,                            null);
				// Sybase doesn't support nullable bits
				// Access allows you to define nullable bits, but returns null as false
				yield return (e => e.HasColumn(_ => _.BooleanNullable),                        v => v.BooleanNullable    = true                                , (ctx, v) => { if (ctx.Contains("Access")) { v.BooleanNullable = false; } },                                                  null,                            ctx => ctx.Contains("Sybase"));
				yield return (e => e.HasColumn(_ => _.DateTime),                               v => v.DateTime           = new DateTime(2018, 11, 24 , 1, 2, 3), null,                                                                                                                        null,                            null);
				yield return (e => e.HasColumn(_ => _.DateTimeNullable),                       v => v.DateTimeNullable   = new DateTime(2018, 11, 25 , 1, 2, 3), null,                                                                                                                        null,                            null);
				yield return (e => e.HasColumn(_ => _.IntEnum),                                v => v.IntEnum            = IntEnum.Value                       , null,                                                                                                                        null,                            null);
				yield return (e => e.HasColumn(_ => _.IntEnumNullable),                        v => v.IntEnumNullable    = IntEnum.Value                       , null,                                                                                                                        null,                            null);
				yield return (e => e.HasColumn(_ => _.StringEnum),                             v => v.StringEnum         = StringEnum.Value1                   , null,                                                                                                                        null,                            null);
				yield return (e => e.HasColumn(_ => _.StringEnumNullable),                     v => v.StringEnumNullable = StringEnum.Value2                   , null,                                                                                                                        null,                            null);
				// Oracle treats empty string as null in this context
				// Sybase roundtrips empty string to " " (WAT?)
				yield return (e => e.Property(_ => _.String).IsNullable(false),                v => v.String             = "test max value"                    , (ctx, v) => { if (ctx.Contains("Oracle") || ctx.Contains("Sybase")) { v.String = " "; } else { v.String = string.Empty; } }, null,                            null);
				yield return (e => e.Property (_ => _.String).IsNullable(),                    v => v.String = "test max value nullable"                       , null,                                                                                                                        null,                            null);
				// Oracle treats empty string as null in this context
				// Sybase roundtrips empty string to " " (WAT?)
				yield return (e => e.Property (_ => _.String).IsNullable(false).HasLength(10), v => v.String             = "test 10"                           , (ctx, v) => { if (ctx.Contains("Oracle") || ctx.Contains("Sybase")) { v.String = " "; } else { v.String = string.Empty; } }, null,                            null);
				yield return (e => e.Property (_ => _.String).HasLength(10),                   v => v.String = "test 10 n"                                     , null,                                                                                                                        null,                            null);
				// https://github.com/linq2db/linq2db/issues/1032 with DataType specified
				yield return (e => e.Property(_ => _.StringConverted).IsNullable(false).HasDataType(DataType.NVarChar), v => v.StringConverted = CreateTableTypes.StringConvertedTestValue, null,                                                                                             null,                            null);
				yield return (e => e.Property(_ => _.StringConverted).HasDataType(DataType.NVarChar), v => v.StringConverted = CreateTableTypes.StringConvertedTestValue, null,                                                                                                               null,                            null);
				// https://github.com/linq2db/linq2db/issues/1032 without DataType specified
				yield return (e => e.Property(_ => _.StringConverted).IsNullable(false),       v => v.StringConverted = CreateTableTypes.StringConvertedTestValue, null,                                                                                                                      null,                            null);
				yield return (e => e.HasColumn(_ => _.StringConverted),                        v => v.StringConverted = CreateTableTypes.StringConvertedTestValue, null,                                                                                                                      null,                            null);
			}
		}

		[Test]
		public void TestCreateTableColumnType(
			[DataSources] string context,
			[ValueSource(nameof(TestCases))] (
				ColumnBuilder columnBuilder,
				ValueBuilder valueBuilder,
				DefaultValueBuilder defaultValueBuilder,
				Func<string, bool> skipAssert,
				Func<string, bool> skipCase) testCase)
		{
			if (testCase.skipCase?.Invoke(context) == true)
			{
				Assert.Ignore("test case is not valid");
			}

			Query.ClearCaches();

			var ms = new MappingSchema();
			var entity = ms.GetFluentMappingBuilder()
				.Entity<CreateTableTypes>()
				.HasColumn(e => e.Id);
			testCase.columnBuilder(entity);

			MappingSchema.Default.SetConverter<List<(uint, string)>, string>(JsonConvert.SerializeObject);
			MappingSchema.Default.SetConverter<List<(uint, string)>, DataParameter>(x =>
				new DataParameter()
				{
					Value = JsonConvert.SerializeObject(x),
					DataType = DataType.NVarChar
				});
			MappingSchema.Default.SetConverter<string, List<(uint, string)>>(JsonConvert.DeserializeObject<List<(uint, string)>>);

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<CreateTableTypes>())
			{
				var defaultValue = new CreateTableTypes() { Id = 1 };
				var testValue    = new CreateTableTypes() { Id = 2 };

				testCase.defaultValueBuilder?.Invoke(context, defaultValue);
				testCase.valueBuilder(testValue);

				db.Insert(defaultValue);
				db.Insert(testValue);

				if (testCase.skipAssert?.Invoke(context) != true)
					AreEqual(new[] { defaultValue, testValue }, table.OrderBy(_ => _.Id), CreateTableTypes.Comparer);
			}
		}
	}
}
