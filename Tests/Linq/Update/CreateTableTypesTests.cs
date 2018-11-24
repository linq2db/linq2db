using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;
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
			public DateTime    DateTime           { get; set; }
			public DateTime?   DateTimeNullable   { get; set; }
			public IntEnum     IntEnum            { get; set; }
			public IntEnum?    IntEnumNullable    { get; set; }
			public StringEnum  StringEnum         { get; set; }
			public StringEnum? StringEnumNullable { get; set; }
			public string      String             { get; set; }
			public string      StringNullable     { get; set; }

			public static IEqualityComparer<CreateTableTypes> Comparer = ComparerBuilder<CreateTableTypes>.GetEqualityComparer();
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

		// TODO: add more cases
		// TODO: add length validation to fields with length (text/binary)
		static IEnumerable<(ColumnBuilder columnBuilder, ValueBuilder valueBuilder, DefaultValueBuilder defaultValueBuilder)> TestCases
		{
			get
			{
				yield return (e => e.HasColumn(_ => _.Int32),                       v => v.Int32              = 1                                  , null);
				yield return (e => e.HasColumn(_ => _.Int32Nullable),               v => v.Int32Nullable      = 2                                  , null);
				yield return (e => e.HasColumn(_ => _.Int64),                       v => v.Int64              = 3                                  , null);
				yield return (e => e.HasColumn(_ => _.Int64Nullable),               v => v.Int64Nullable      = 4                                  , null);
				yield return (e => e.HasColumn(_ => _.Double),                      v => v.Double             = 3.14                               , null);
				yield return (e => e.HasColumn(_ => _.DoubleNullable),              v => v.DoubleNullable     = 4.13                               , null);
				yield return (e => e.HasColumn(_ => _.Boolean),                     v => v.Boolean            = true                               , null);
				yield return (e => e.HasColumn(_ => _.BooleanNullable),             v => v.BooleanNullable    = true                               , (ctx, v) => { if (ctx.Contains("Sybase")) { v.BooleanNullable = false; } });
				yield return (e => e.HasColumn(_ => _.DateTime),                    v => v.DateTime           = new DateTime(2018, 11, 24, 1, 2, 3), null);
				yield return (e => e.HasColumn(_ => _.DateTimeNullable),            v => v.DateTimeNullable   = new DateTime(2018, 11, 25, 1, 2, 3), null);
				yield return (e => e.HasColumn(_ => _.IntEnum),                     v => v.IntEnum            = IntEnum.Value                      , null);
				yield return (e => e.HasColumn(_ => _.IntEnumNullable),             v => v.IntEnumNullable    = IntEnum.Value                      , null);
				yield return (e => e.HasColumn(_ => _.StringEnum),                  v => v.StringEnum         = StringEnum.Value1                  , null);
				yield return (e => e.HasColumn(_ => _.StringEnumNullable),          v => v.StringEnumNullable = StringEnum.Value2                  , null);
				yield return (e => e.HasColumn(_ => _.String),                      v => v.String             = "test max value"                   , null);
				yield return (e => e.Property (_ => _.StringNullable).IsNullable(), v => v.StringNullable     = "test max value nullable"          , null);
				yield return (e => e.Property (_ => _.String).HasLength(10),        v => v.String             = "test 10"                          , null);
				yield return (e => e.Property (_ => _.StringNullable).HasLength(10).IsNullable(), v => v.StringNullable = "test 10 n"              , null);
			}
		}

		[Test]
		public void TestCreateTableColumnType(
			[DataSources] string context,
			[ValueSource(nameof(TestCases))] (ColumnBuilder columnBuilder, ValueBuilder valueBuilder, DefaultValueBuilder defaultValueBuilder) testCase)
		{
			Query.ClearCaches();

			var ms = new MappingSchema();
			var entity = ms.GetFluentMappingBuilder()
				.Entity<CreateTableTypes>()
				.HasColumn(e => e.Id);
			testCase.columnBuilder(entity);

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<CreateTableTypes>())
			{
				var defaultValue = new CreateTableTypes() { Id = 1 };
				var testValue    = new CreateTableTypes() { Id = 2 };

				testCase.defaultValueBuilder?.Invoke(context, defaultValue);
				testCase.valueBuilder(testValue);

				db.Insert(defaultValue);
				db.Insert(testValue);

				AreEqual(new[] { defaultValue, testValue }, table.OrderBy(_ => _.Id), CreateTableTypes.Comparer);
			}
		}
	}
}
