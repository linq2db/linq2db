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
		class AllTypes2
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
#if !NET46
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
		class MyTable
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

		[Test]
		public void TestGeography([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
#if !NET46
			if (IsMsProvider(context))
				Assert.Inconclusive("Spatial types test disabled for Microsoft.Data.SqlClient");
#endif
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
						// missing API
#if NET46
						v8 = SqlGeography.GeomFromGml(t.geographyDataType.AsGml(), 4326),
						v9  = t.geographyDataType.AsGml(),
#endif
						v10 = t.geographyDataType.ToString(),
						v11 = SqlGeography.Parse("LINESTRING(-122.360 47.656, -122.343 47.656)"),
						v12 = SqlGeography.Point(1, 1, 4326),
						v13 = SqlGeography.STGeomFromText(new SqlChars("LINESTRING(-122.360 47.656, -122.343 47.656)"), 4326),
					})
					.ToList();
			}
		}

		[Table]
		public class SqlTypes
		{
			[Column] public int            ID;
			[Column] public SqlHierarchyId HID;

			static List<SqlTypes>? _data;
			public  static IEnumerable<SqlTypes> Data([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
			{
				if (_data == null)
					using (var db = new DataConnection(context.Replace(".LinqService", "")))
						_data = db.GetTable<SqlTypes>().ToList();

				foreach (var item in _data)
					yield return item;
			}

			public override bool Equals(object obj)
			{
				return obj is SqlTypes && ((SqlTypes)obj).ID == ID;
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
						.Where(t => (bool)hid.IsDescendantOf(t.HID) == true),
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
		class Issue1836
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

				Assert.AreEqual(2, records.Count);
				Assert.AreEqual(1, records[0].Id);
				Assert.True(records[0].HomeLocation!.IsNull);
				Assert.AreEqual(2, records[1].Id);
				// missing API
#if NET46
				Assert.True(Issue1836.Data[1].HomeLocation!.STEquals(records[1].HomeLocation).IsTrue);
#endif
			}
		}
	}
}
