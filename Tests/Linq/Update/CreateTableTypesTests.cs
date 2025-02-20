using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using JetBrains.Annotations;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using ColumnBuilder = System.Action<LinqToDB.Mapping.EntityMappingBuilder<Tests.xUpdate.CreateTableTypesTests.CreateTableTypes>>;
using DefaultValueBuilder = System.Action<string, Tests.xUpdate.CreateTableTypesTests.CreateTableTypes>;
using ValueBuilder = System.Action<Tests.xUpdate.CreateTableTypesTests.CreateTableTypes>;

// ReSharper disable once CheckNamespace
namespace Tests.xUpdate
{
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
			public string?     String             { get; set; }

			// converters test
			// see https://github.com/linq2db/linq2db/issues/1032
			public List<(uint field1, string field2)> StringConverted = null!;

			public static IEqualityComparer<CreateTableTypes> Comparer = ComparerBuilder.GetEqualityComparer<CreateTableTypes>();

			public static List<(uint, string)> StringConvertedTestValue = new ()
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
		static IEnumerable<TestCaseData> NamedTestCases
		{
			get => TestCases.Select((test, idx) => new TestCaseData(test).SetArgDisplayNames($"TestCreateTableColumnType#{idx + 1}"));
		}

		public class TestCreateTableColumnTypeParameters
		{
			public TestCreateTableColumnTypeParameters(
				string               name,
				ColumnBuilder        columnBuilder,
				ValueBuilder         valueBuilder,
				DefaultValueBuilder? defaultValueBuilder,
				Func<string, bool>?  skipAssert,
				Func<string, bool>?  skipCase)
			{
				Name                = name;
				ColumnBuilder       = columnBuilder;
				ValueBuilder        = valueBuilder;
				DefaultValueBuilder = defaultValueBuilder;
				SkipAssert          = skipAssert;
				SkipCase            = skipCase;
			}

			public string               Name                { get; }
			public ColumnBuilder        ColumnBuilder       { get; }
			public ValueBuilder         ValueBuilder        { get; }
			public DefaultValueBuilder? DefaultValueBuilder { get; }
			public Func<string, bool>?  SkipAssert          { get; }
			public Func<string, bool>?  SkipCase            { get; }

			public override string ToString()
			{
				return Name;
			}
		}

		static IEnumerable<TestCreateTableColumnTypeParameters> TestCases
		{
			[UsedImplicitly]
			get
			{
				yield return new TestCreateTableColumnTypeParameters("Int32"                          , e => e.HasColumn(_ => _.Int32),                                  v => v.Int32              = 1                                   , null,                                                                                                                        null,                            null);
				yield return new TestCreateTableColumnTypeParameters("Int32Nullable"                  , e => e.HasColumn(_ => _.Int32Nullable),                          v => v.Int32Nullable      = 2                                   , null,                                                                                                                        null,                            null);
				// Access doesn't have 64bit integer type
				yield return new TestCreateTableColumnTypeParameters("Int64"                          , e => e.HasColumn(_ => _.Int64),                                  v => v.Int64              = 3                                   , null,                                                                                                                        null,                            ctx => ctx.IsAnyOf(TestProvName.AllAccess));
				// Access doesn't have 64bit integer type
				yield return new TestCreateTableColumnTypeParameters("Int64Nullable"                  , e => e.HasColumn(_ => _.Int64Nullable),                          v => v.Int64Nullable      = 4                                   , null,                                                                                                                        null,                            ctx => ctx.IsAnyOf(TestProvName.AllAccess));
				// Firebird looses precision of double
				yield return new TestCreateTableColumnTypeParameters("Double"                         , e => e.HasColumn(_ => _.Double),                                 v => v.Double             = 3.14                                , null,                                                                                                                        ctx => ctx.IsAnyOf(TestProvName.AllFirebird), null);
				// Firebird looses precision of double
				yield return new TestCreateTableColumnTypeParameters("DoubleNullable"                 , e => e.HasColumn(_ => _.DoubleNullable),                         v => v.DoubleNullable     = 4.13                                , null,                                                                                                                        ctx => ctx.IsAnyOf(TestProvName.AllFirebird), null);
				yield return new TestCreateTableColumnTypeParameters("Boolean"                        , e => e.HasColumn(_ => _.Boolean),                                v => v.Boolean            = true                                , null,                                                                                                                        null, null);
				// Sybase doesn't support nullable bits
				// Access allows you to define nullable bits, but returns null as false
				yield return new TestCreateTableColumnTypeParameters("BooleanNullable"                , e => e.HasColumn(_ => _.BooleanNullable),                        v => v.BooleanNullable    = true                                , (ctx, v) => { if (ctx.IsAnyOf(TestProvName.AllAccess)) { v.BooleanNullable = false; } },                                                  null,                            ctx => ctx.IsAnyOf(TestProvName.AllSybase));
				yield return new TestCreateTableColumnTypeParameters("DateTime"                       , e => e.HasColumn(_ => _.DateTime),                               v => v.DateTime           = new DateTime(2018, 11, 24 , 1, 2, 3), null,                                                                                                                        null,                            null);
				yield return new TestCreateTableColumnTypeParameters("DateTimeNullable"               , e => e.HasColumn(_ => _.DateTimeNullable),                       v => v.DateTimeNullable   = new DateTime(2018, 11, 25 , 1, 2, 3), null,                                                                                                                        null,                            null);
				yield return new TestCreateTableColumnTypeParameters("IntEnum"                        , e => e.HasColumn(_ => _.IntEnum),                                v => v.IntEnum            = IntEnum.Value                       , null,                                                                                                                        null,                            null);
				yield return new TestCreateTableColumnTypeParameters("IntEnumNullable"                , e => e.HasColumn(_ => _.IntEnumNullable),                        v => v.IntEnumNullable    = IntEnum.Value                       , null,                                                                                                                        null,                            null);
				yield return new TestCreateTableColumnTypeParameters("StringEnum"                     , e => e.HasColumn(_ => _.StringEnum),                             v => v.StringEnum         = StringEnum.Value1                   , null,                                                                                                                        null,                            null);
				yield return new TestCreateTableColumnTypeParameters("StringEnumNullable"             , e => e.HasColumn(_ => _.StringEnumNullable),                     v => v.StringEnumNullable = StringEnum.Value2                   , null,                                                                                                                        null,                            null);
				// Oracle treats empty string as null in this context
				// Sybase roundtrips empty string to " " (WAT?)
				yield return new TestCreateTableColumnTypeParameters("String"                         , e => e.Property(_ => _.String).IsNullable(false),                v => v.String             = "test max value"                    , (ctx, v) => { if (ctx.IsAnyOf(TestProvName.AllOracle) || ctx.IsAnyOf(TestProvName.AllSybase)) { v.String = " "; } else { v.String = string.Empty; } }, null,                            null);
				yield return new TestCreateTableColumnTypeParameters("StringNullable"                 , e => e.Property (_ => _.String).IsNullable(),                    v => v.String = "test max value nullable"                       , null,                                                                                                                        null,                            null);
				// Oracle treats empty string as null in this context
				// Sybase roundtrips empty string to " " (WAT?)
				yield return new TestCreateTableColumnTypeParameters("String10"                       , e => e.Property (_ => _.String).IsNullable(false).HasLength(10), v => v.String             = "test 10"                           , (ctx, v) => { if (ctx.IsAnyOf(TestProvName.AllOracle) || ctx.IsAnyOf(TestProvName.AllSybase)) { v.String = " "; } else { v.String = string.Empty; } }, ctx => ctx.IsAnyOf(TestProvName.AllOracleNative), null);
				yield return new TestCreateTableColumnTypeParameters("String10Nullable"               , e => e.Property (_ => _.String).HasLength(10),                   v => v.String = "test 10 n"                                     , null,                                                                                                                        ctx => ctx.IsAnyOf(TestProvName.AllOracleNative), null);
				// https://github.com/linq2db/linq2db/issues/1032 with DataType specified
				yield return new TestCreateTableColumnTypeParameters("StringConvertedNVarChar"        , e => e.Property(_ => _.StringConverted).IsNullable(false).HasDataType(DataType.NVarChar), v => v.StringConverted = CreateTableTypes.StringConvertedTestValue, null,                                                                                             null,                            null);
				yield return new TestCreateTableColumnTypeParameters("StringConvertedNVarCharNullable", e => e.Property(_ => _.StringConverted).HasDataType(DataType.NVarChar), v => v.StringConverted = CreateTableTypes.StringConvertedTestValue, null,                                                                                                               null,                            null);
				// https://github.com/linq2db/linq2db/issues/1032 without DataType specified
				yield return new TestCreateTableColumnTypeParameters("StringConverted"                , e => e.Property(_ => _.StringConverted).IsNullable(false),       v => v.StringConverted = CreateTableTypes.StringConvertedTestValue, null,                                                                                                                      null,                            null);
				yield return new TestCreateTableColumnTypeParameters("StringConvertedNullable"        , e => e.HasColumn(_ => _.StringConverted),                        v => v.StringConverted = CreateTableTypes.StringConvertedTestValue, null,                                                                                                                      null,                            null);
			}
		}

		// TODO: fix
		// oracle native tests could fail due to bug in provider:
		// InitialLONGFetchSize option makes it read garbage for String/StringNullable testcases
		[Test]
		public void TestCreateTableColumnType(
			[DataSources] string context,
			[ValueSource(nameof(TestCases))] TestCreateTableColumnTypeParameters testCase)
		{
			if (testCase.SkipCase?.Invoke(context) == true)
			{
				Assert.Ignore("test case is not valid");
			}

			Query.ClearCaches();

			var ms = new MappingSchema();
			var entity = new FluentMappingBuilder(ms)
				.Entity<CreateTableTypes>()
				.HasColumn(e => e.Id);

			testCase.ColumnBuilder(entity);

			entity.Build();

			var options = new JsonSerializerOptions () { IncludeFields = true };

			ms.SetConverter<List<(uint, string)>, string>(_ => JsonSerializer.Serialize(_, options));
			ms.SetConverter<List<(uint, string)>, DataParameter>(x =>
				new DataParameter()
				{
					Value = JsonSerializer.Serialize(x, options),
					DataType = DataType.NVarChar
				});
			ms.SetConverter<string, List<(uint, string)>?>(_ => JsonSerializer.Deserialize<List<(uint, string)>>(_, options));

			using (var db    = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<CreateTableTypes>())
			{
				var defaultValue = new CreateTableTypes { Id = 1 };
				var testValue    = new CreateTableTypes { Id = 2 };

				testCase.DefaultValueBuilder?.Invoke(context, defaultValue);
				testCase.ValueBuilder(testValue);

				db.Insert(defaultValue, table.TableName);
				db.Insert(testValue,    table.TableName);

				if (testCase.SkipAssert?.Invoke(context) != true)
					AreEqual(new[] { defaultValue, testValue }, table.OrderBy(_ => _.Id), CreateTableTypes.Comparer);
			}
		}
	}
}
