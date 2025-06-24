using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

using Microsoft.SqlServer.Types;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public partial class SqlServerTypesTests : DataProviderTestBase
	{
		[Table(Name="AllTypes2")]
		sealed class AllTypes2
		{
			[Column(DbType="int"),   PrimaryKey, Identity] public int             ID                     { get; set; } // int
			[Column(DbType="date"),              Nullable] public DateTime?       dateDataType           { get; set; } // date
			[Column(DbType="datetimeoffset(7)"), Nullable] public DateTimeOffset? datetimeoffsetDataType { get; set; } // datetimeoffset(7)
			[Column(DbType="datetime2(7)"),      Nullable] public DateTime?       datetime2DataType      { get; set; } // datetime2(7)
			[Column(DbType="time(7)"),           Nullable] public TimeSpan?       timeDataType           { get; set; } // time(7)
			[Column(DbType="hierarchyid"),       Nullable] public SqlHierarchyId  hierarchyidDataType    { get; set; } // hierarchyid
			[Column(DbType="geography"),         Nullable] public SqlGeography?   geographyDataType      { get; set; } // geography
			[Column(DbType="geometry"),          Nullable] public SqlGeometry?    geometryDataType       { get; set; } // geometry
		}

		[Test]
		public void TestHierarchyId([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
#if !NETFRAMEWORK
			if (IsMsProvider(context))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");
#endif

			using (new SerializeAssemblyQualifiedName(true))
			using (var conn = GetDataContext(context))
			{
				conn.GetTable<AllTypes2>()
					.Where (t => (bool)(t.hierarchyidDataType.GetLevel() > 0))
					.Select(t => new
					{
						v1 = SqlHierarchyId.GetRoot(),
						v2 = Sql.ToSql(t.hierarchyidDataType.GetDescendant(SqlHierarchyId.Parse("/1/3/4/"), SqlHierarchyId.Parse("/1/3/5/"))),
						v3 = t.hierarchyidDataType.IsDescendantOf(SqlHierarchyId.Parse("/1/")),
						v4 = t.hierarchyidDataType.GetLevel(),
						v5 = t.hierarchyidDataType.GetAncestor(0),
						v6 = t.hierarchyidDataType.GetReparentedValue(SqlHierarchyId.Parse("/1/"), SqlHierarchyId.Parse("/2/")),
						v7 = SqlHierarchyId.Parse("/1/2/3/4/5/"),
						v8 = t.hierarchyidDataType.ToString(),
					})
					.ToList();
			}
		}

		[Table("#tmp")]
		sealed class MyTable
		{
			[Column] public SqlHierarchyId ID;
		}

		[Test]
		public void CreateTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (new SerializeAssemblyQualifiedName(true))
			using (var conn = GetDataContext(context))
			{
				conn.CreateTable<MyTable>();
			}
		}

#if NETFRAMEWORK
		[Test]
		public void TestGeographyMicrosoft([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (new SerializeAssemblyQualifiedName(true))
			using (var conn = GetDataContext(context))
			{
				conn.InlineParameters = true;

				conn.GetTable<AllTypes2>()
					.Select(t => new
					{
						v1  = t.geographyDataType!.STSrid,
						v2  = t.geographyDataType.Lat,
						v3  = t.geographyDataType.Long,
						v4  = t.geographyDataType.Z,
						v5  = t.geographyDataType.M,
						//v6  = t.geographyDataType.HasZ,
						//v7  = t.geographyDataType.HasM,
						v8 = SqlGeography.GeomFromGml(t.geographyDataType.AsGml(), 4326),
						v9  = t.geographyDataType.AsGml(),
						v10 = t.geographyDataType.ToString(),
						v11 = SqlGeography.Parse("LINESTRING(-122.360 47.656, -122.343 47.656)"),
						v12 = SqlGeography.Point(1, 1, 4326),
						v13 = SqlGeography.STGeomFromText(new SqlChars("LINESTRING(-122.360 47.656, -122.343 47.656)"), 4326),
					})
					.ToList();
			}
		}
#else
		[Test]
		public void TestGeographyDotMorten([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			if (IsMsProvider(context))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");

			using (new SerializeAssemblyQualifiedName(true))
			using (var conn = GetDataContext(context))
			{
				conn.InlineParameters = true;

				conn.GetTable<AllTypes2>()
					.Select(t => new
					{
						v1  = t.geographyDataType!.STSrid,
						v2  = t.geographyDataType.Lat,
						v3  = t.geographyDataType.Long,
						v4  = t.geographyDataType.Z,
						v5  = t.geographyDataType.M,
						//v6  = t.geographyDataType.HasZ,
						//v7  = t.geographyDataType.HasM,
						//v8 = SqlGeography.GeomFromGml(t.geographyDataType.AsGml(), 4326),
						//v9  = t.geographyDataType.AsGml(),
						v10 = t.geographyDataType.ToString(),
						v11 = SqlGeography.Parse("LINESTRING(-122.360 47.656, -122.343 47.656)"),
						v12 = SqlGeography.Point(1, 1, 4326),
						v13 = SqlGeography.STGeomFromText(new SqlChars("LINESTRING(-122.360 47.656, -122.343 47.656)"), 4326),
					})
					.ToList();
			}
		}
#endif

		[Table]
		public class SqlTypes
		{
			[Column] public int            ID;
			[Column] public SqlHierarchyId HID;

			static List<SqlTypes>? _data;
			public  static IEnumerable<SqlTypes> Data([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
			{
				if (_data == null)
					using (new DisableBaseline("test cache"))
					using (var db = new DataConnection(context.StripRemote()))
						_data = db.GetTable<SqlTypes>().ToList();

				foreach (var item in _data)
					yield return item;
			}

			public override bool Equals(object? obj)
			{
				return obj is SqlTypes st && st.ID == ID;
			}

			public override int GetHashCode()
			{
				return ID.GetHashCode();
			}
		}

		[Test]
		public void Where1([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			if (IsMsProvider(context))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");

			using (new SerializeAssemblyQualifiedName(true))
			using (var db = GetDataContext(context))
			{
				var hid = SqlHierarchyId.Parse("/1/");

				AreEqual(
					SqlTypes.Data(context)
						.Where(t => (bool)(hid.IsDescendantOf(t.HID) == SqlBoolean.True)),
					db.GetTable<SqlTypes>()
						.Where(t => (bool)(hid.IsDescendantOf(t.HID) == SqlBoolean.True)));
			}
		}

		[Test]
		public void Where2([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			if (IsMsProvider(context))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");

			using (new SerializeAssemblyQualifiedName(true))
			using (var db = GetDataContext(context))
			{
				var hid = SqlHierarchyId.Parse("/1/");

				AreEqual(
					SqlTypes.Data(context)
						.Where(t => (bool)hid.IsDescendantOf(t.HID)),
					db.GetTable<SqlTypes>()
						.Where(t => (bool)hid.IsDescendantOf(t.HID) == true));
			}
		}

		[Test]
		public void Where3([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			if (IsMsProvider(context))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");

			using (new SerializeAssemblyQualifiedName(true))
			using (var db = GetDataContext(context))
			{
				var hid = SqlHierarchyId.Parse("/1/");

				AreEqual(
					SqlTypes.Data(context)
						.Where(t => (bool)hid.IsDescendantOf(t.HID)),
					db.GetTable<SqlTypes>()
						.Where(t => (bool)hid.IsDescendantOf(t.HID)));
			}
		}

		[Test]
		public void Where4([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			if (IsMsProvider(context))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");

			using (new SerializeAssemblyQualifiedName(true))
			using (var db = GetDataContext(context))
			{
				var hid = SqlHierarchyId.Parse("/1/");

				AreEqual(
					SqlTypes.Data(context)
						.Where(t => hid.IsDescendantOf(t.HID).Value),
					db.GetTable<SqlTypes>()
						.Where(t => hid.IsDescendantOf(t.HID).Value));
			}
		}

		[Test]
		public void Where5([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			if (IsMsProvider(context))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");

			using (new SerializeAssemblyQualifiedName(true))
			using (var db = GetDataContext(context))
			{
				var hid = SqlHierarchyId.Parse("/1/");

				AreEqual(
					SqlTypes.Data(context)
						.Where(t => hid.IsDescendantOf(t.HID).IsTrue),
					db.GetTable<SqlTypes>()
						.Where(t => hid.IsDescendantOf(t.HID).IsTrue));
			}
		}

		[Test]
		public void Where6([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			if (IsMsProvider(context))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");

			using (new SerializeAssemblyQualifiedName(true))
			using (var db = GetDataContext(context))
			{
				var hid = SqlHierarchyId.Parse("/1/");

				AreEqual(
					SqlTypes.Data(context)
						.Where(t => (bool)(hid.IsDescendantOf(t.HID) == SqlBoolean.True) && t.ID != 1)
						.OrderBy(c => c.HID),
					db.GetTable<SqlTypes>()
						.Where(t => (bool)(hid.IsDescendantOf(t.HID) == SqlBoolean.True) && t.ID != 1)
						.OrderBy(c => c.HID));
			}
		}

		[Sql.Expression("{0}.IsDescendantOf({1})", ServerSideOnly = true)]
		static bool IsDescendantOf(SqlHierarchyId child, SqlHierarchyId parent)
		{
			return child.IsDescendantOf(parent).Value;
		}

		[Test]
		public void Where7([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			if (IsMsProvider(context))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");

			using (new SerializeAssemblyQualifiedName(true))
			using (var db = GetDataContext(context))
			{
				var hid = SqlHierarchyId.Parse("/1/");

				AreEqual(
					SqlTypes.Data(context)
						.Where(t => IsDescendantOf(hid, t.HID)),
					db.GetTable<SqlTypes>()
						.Where(t => IsDescendantOf(hid, t.HID)));
			}
		}

		[Test]
		public void Where8([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			if (IsMsProvider(context))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");

			using (new SerializeAssemblyQualifiedName(true))
			using (var db = GetDataContext(context))
			{
				var hid = SqlHierarchyId.Parse("/1/");

				AreEqual(
					SqlTypes.Data(context)
						.Where(t => IsDescendantOf(hid, t.HID)),
					db.GetTable<SqlTypes>()
						.Where(t => IsDescendantOf(hid, t.HID) == true));
			}
		}

		[Table]
		sealed class Issue1836
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column]
			public SqlGeography? HomeLocation { get; set; }

			public static Issue1836[] Data { get; } = new[]
			{
				new Issue1836() { Id = 1, HomeLocation = null },
				new Issue1836() { Id = 2, HomeLocation = SqlGeography.Parse("LINESTRING(-122.360 47.656, -122.343 47.656)") },
			};
		}

		private bool IsMsProvider(string context)
		{
			return ((SqlServerDataProvider)DataConnection.GetDataProvider(GetProviderName(context, out var _))).Provider == SqlServerProvider.MicrosoftDataSqlClient;
		}

		// https://github.com/linq2db/linq2db/issues/1836
		[Test]
		public void SelectSqlGeography([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			if (IsMsProvider(context))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");

			using (new SerializeAssemblyQualifiedName(true))
			using (var db = GetDataContext(context))
			using (var t  = db.CreateLocalTable(Issue1836.Data))
			{
				var records = t.OrderBy(_ => _.Id).ToList();

				Assert.That(records, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(records[0].Id, Is.EqualTo(1));
					Assert.That(records[0].HomeLocation!.IsNull, Is.True);
					Assert.That(records[1].Id, Is.EqualTo(2));
					// missing API
#if NETFRAMEWORK
					Assert.That(Issue1836.Data[1].HomeLocation!.STEquals(records[1].HomeLocation).IsTrue, Is.True);
#endif
				}
			}
		}

		public class LiteralsTestTable<TValue>
		{
			public TValue Value { get; set; } = default!;
		}

		[Test]
		public void TestLiteralsAndParameters([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var interceptor = new SaveCommandInterceptor();

			// DateTime
			Test<DateTime>(DataType.Text         , -1, TestData.DateTime, TestData.DateTime.TrimPrecision(3));
			Test<DateTime>(DataType.NText        , -1, TestData.DateTime, TestData.DateTime.TrimPrecision(3));
			Test<DateTime>(DataType.Char         , -1, TestData.DateTime, TestData.DateTime.TrimPrecision(3));
			Test<DateTime>(DataType.NChar        , -1, TestData.DateTime, TestData.DateTime.TrimPrecision(3));
			Test<DateTime>(DataType.VarChar      , -1, TestData.DateTime, TestData.DateTime.TrimPrecision(3));
			Test<DateTime>(DataType.NVarChar     , -1, TestData.DateTime, TestData.DateTime.TrimPrecision(3));
			Test<DateTime>(DataType.SmallDateTime, -1, TestData.DateTime, TestData.DateTime.TrimSeconds(1));
			Test<DateTime>(DataType.Date         , -1, TestData.DateTime, TestData.DateTime.Date);
			Test<DateTime>(DataType.DateTime     , -1, TestData.DateTime, TestData.DateTime.TrimPrecision(3));

			if (context.IsAnyOf(TestProvName.AllSqlServer2005))
				Test<DateTime>(DataType.Undefined    , -1, TestData.DateTime, TestData.DateTime.TrimPrecision(3));

			for (var i = 0; i <= 7; i++)
			{
				if (context.IsAnyOf(TestProvName.AllSqlServer2005))
					Test<DateTime>(DataType.DateTime2, i, TestData.DateTime, TestData.DateTime.TrimPrecision(Math.Min(3, i)));
				else
				{
					Test<DateTime>(DataType.DateTime2, i, TestData.DateTime, TestData.DateTime.TrimPrecision(i));
					Test<DateTime>(DataType.Undefined, i, TestData.DateTime, TestData.DateTime.TrimPrecision(i));
				}
			}

			// SqlDateTime as DateTime
			var sqlDateTime = new SqlDateTime(TestData.DateTime);
			Test<SqlDateTime>(DataType.Text         , -1, sqlDateTime, sqlDateTime);
			Test<SqlDateTime>(DataType.NText        , -1, sqlDateTime, sqlDateTime);
			Test<SqlDateTime>(DataType.Char         , -1, sqlDateTime, sqlDateTime);
			Test<SqlDateTime>(DataType.NChar        , -1, sqlDateTime, sqlDateTime);
			Test<SqlDateTime>(DataType.VarChar      , -1, sqlDateTime, sqlDateTime);
			Test<SqlDateTime>(DataType.NVarChar     , -1, sqlDateTime, sqlDateTime);
			Test<SqlDateTime>(DataType.SmallDateTime, -1, sqlDateTime, sqlDateTime.TrimSeconds(1));
			Test<SqlDateTime>(DataType.Date         , -1, sqlDateTime, (SqlDateTime)(((DateTime)sqlDateTime).Date));
			Test<SqlDateTime>(DataType.DateTime     , -1, sqlDateTime, sqlDateTime.TrimPrecision(3));
			Test<SqlDateTime>(DataType.Undefined    , -1, sqlDateTime, sqlDateTime.TrimPrecision(3));
			for (var i = 0; i <= 7; i++)
			{
				if (context.IsAnyOf(TestProvName.AllSqlServer2005))
					Test<SqlDateTime>(DataType.DateTime2, i, sqlDateTime, sqlDateTime.TrimPrecision(Math.Min(3, i)));
				else
					Test<SqlDateTime>(DataType.DateTime2, i, sqlDateTime, sqlDateTime.TrimPrecision(i));
			}

			// TimeSpan
			for (var i = 0; i <= 7; i++)
			{
				Test<TimeSpan>(DataType.Int64    , i, TestData.TimeOfDay, TestData.TimeOfDay.TrimPrecision(i));
				Test<TimeSpan>(DataType.Text     , i, TestData.TimeOfDay, TestData.TimeOfDay.TrimPrecision(i));
				Test<TimeSpan>(DataType.NText    , i, TestData.TimeOfDay, TestData.TimeOfDay.TrimPrecision(i));
				Test<TimeSpan>(DataType.Char     , i, TestData.TimeOfDay, TestData.TimeOfDay.TrimPrecision(i));
				Test<TimeSpan>(DataType.NChar    , i, TestData.TimeOfDay, TestData.TimeOfDay.TrimPrecision(i));
				Test<TimeSpan>(DataType.VarChar  , i, TestData.TimeOfDay, TestData.TimeOfDay.TrimPrecision(i));
				Test<TimeSpan>(DataType.NVarChar , i, TestData.TimeOfDay, TestData.TimeOfDay.TrimPrecision(i));
				if (!context.IsAnyOf(TestProvName.AllSqlServer2005))
				{
					// 2005: time not supported
					Test<TimeSpan>(DataType.Time     , i, TestData.TimeOfDay, TestData.TimeOfDay.TrimPrecision(i));
					// 2005: no defaults, user should explicitly specify which type he wants to use for TimeSpan mapping
					Test<TimeSpan>(DataType.Undefined, i, TestData.TimeOfDay, TestData.TimeOfDay.TrimPrecision(i));
				}
			}

			// DateTimeOffset
			Test<DateTimeOffset>(DataType.Date          , -1, TestData.DateTimeOffset, TestData.DateTimeOffset.Date);
			Test<DateTimeOffset>(DataType.DateTime      , -1, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimPrecision(3));
			Test<DateTimeOffset>(DataType.SmallDateTime , -1, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimSeconds(1));
			for (var i = 0; i <= 7; i++)
			{
				Test<DateTimeOffset>(DataType.Text          , i, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimPrecision(i));
				Test<DateTimeOffset>(DataType.NText         , i, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimPrecision(i));
				Test<DateTimeOffset>(DataType.Char          , i, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimPrecision(i));
				Test<DateTimeOffset>(DataType.NChar         , i, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimPrecision(i));
				Test<DateTimeOffset>(DataType.VarChar       , i, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimPrecision(i));
				Test<DateTimeOffset>(DataType.NVarChar      , i, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimPrecision(i));

				if (context.IsAnyOf(TestProvName.AllSqlServer2005))
				{
					Test<DateTimeOffset>(DataType.DateTime2, i, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimPrecision(Math.Min(3, i)));
					// not supported by sql2005 and we don't provide fallback
					//Test<DateTimeOffset>(DataType.DateTimeOffset, i, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimPrecision(Math.Min(3, i)));
					//Test<DateTimeOffset>(DataType.Undefined, i, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimPrecision(Math.Min(3, i)));
				}
				else
				{
					Test<DateTimeOffset>(DataType.DateTime2, i, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimPrecision(i));
					Test<DateTimeOffset>(DataType.DateTimeOffset, i, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimPrecision(i));
					Test<DateTimeOffset>(DataType.Undefined, i, TestData.DateTimeOffset, TestData.DateTimeOffset.TrimPrecision(i));
				}
			}

			void Test<TValue>(DataType dataType, int precision, TValue value, TValue expected)
			{
				var ms = new MappingSchema();

				new FluentMappingBuilder(ms)
					.Entity<LiteralsTestTable<TValue>>()
						.Property(e => e.Value)
							.HasDataType(dataType)
							.HasPrecision(precision)
					.Build();

				using var db = GetDataContext(context, ms);

				db.AddInterceptor(interceptor);

				db.InlineParameters = true;

				var data = new LiteralsTestTable<TValue>[] { new() { Value = value } }.AsQueryable(db).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Value, Is.EqualTo(expected));
					Assert.That(interceptor.Parameters, Is.Empty);
				}

				db.InlineParameters = false;

				data = (from x in db.FromSqlScalar<int>($"select 1 as one")
					   from y in new LiteralsTestTable<TValue>[] { new() { Value = value } }
					   select y).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Value, Is.EqualTo(expected));
					Assert.That(interceptor.Parameters, Has.Length.EqualTo(1));
				}
			}
		}

#if NET8_0_OR_GREATER
		[Test]
		public void TestLiteralsAndParameters_DateOnly([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var interceptor = new SaveCommandInterceptor();

			// DateOnly
			Test<DateOnly>(DataType.Text     , -1, TestData.DateOnly, TestData.DateOnly);
			Test<DateOnly>(DataType.NText    , -1, TestData.DateOnly, TestData.DateOnly);
			Test<DateOnly>(DataType.Char     , -1, TestData.DateOnly, TestData.DateOnly);
			Test<DateOnly>(DataType.NChar    , -1, TestData.DateOnly, TestData.DateOnly);
			Test<DateOnly>(DataType.VarChar  , -1, TestData.DateOnly, TestData.DateOnly);
			Test<DateOnly>(DataType.NVarChar , -1, TestData.DateOnly, TestData.DateOnly);
			Test<DateOnly>(DataType.Date     , -1, TestData.DateOnly, TestData.DateOnly);
			Test<DateOnly>(DataType.Undefined, -1, TestData.DateOnly, TestData.DateOnly);

			void Test<TValue>(DataType dataType, int precision, TValue value, TValue expected)
			{
				var ms = new MappingSchema();

				new FluentMappingBuilder(ms)
					.Entity<LiteralsTestTable<TValue>>()
						.Property(e => e.Value)
							.HasDataType(dataType)
							.HasPrecision(precision)
					.Build();

				using var db = GetDataContext(context, ms);

				db.AddInterceptor(interceptor);

				db.InlineParameters = true;

				var data = new LiteralsTestTable<TValue>[] { new() { Value = value } }.AsQueryable(db).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Value, Is.EqualTo(expected));
					Assert.That(interceptor.Parameters, Is.Empty);
				}

				db.InlineParameters = false;

				data = (from x in db.FromSqlScalar<int>($"select 1 as one")
					   from y in new LiteralsTestTable<TValue>[] { new() { Value = value } }
					   select y).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Value, Is.EqualTo(expected));
					Assert.That(interceptor.Parameters, Has.Length.EqualTo(1));
				}
			}
		}
#endif
	}
}
