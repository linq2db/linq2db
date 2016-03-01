using System;
using System.Data.SqlTypes;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using Microsoft.SqlServer.Types;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class SqlServerTypesTest : DataProviderTestBase
	{
		[Table(Database="TestData", Name="AllTypes2")]
		public class AllTypes2
		{
			[Column(DbType="int"),   PrimaryKey, Identity] public int             ID                     { get; set; } // int
			[Column(DbType="date"),              Nullable] public DateTime?       dateDataType           { get; set; } // date
			[Column(DbType="datetimeoffset(7)"), Nullable] public DateTimeOffset? datetimeoffsetDataType { get; set; } // datetimeoffset(7)
			[Column(DbType="datetime2(7)"),      Nullable] public DateTime?       datetime2DataType      { get; set; } // datetime2(7)
			[Column(DbType="time(7)"),           Nullable] public TimeSpan?       timeDataType           { get; set; } // time(7)
			[Column(DbType="hierarchyid"),       Nullable] public SqlHierarchyId  hierarchyidDataType    { get; set; } // hierarchyid
			[Column(DbType="geography"),         Nullable] public SqlGeography    geographyDataType      { get; set; } // geography
			[Column(DbType="geometry"),          Nullable] public SqlGeometry     geometryDataType       { get; set; } // geometry
		}

		[AttributeUsage(AttributeTargets.Method)]
		class SqlServerDataContextAttribute : IncludeDataContextSourceAttribute
		{
			public SqlServerDataContextAttribute()
				: base(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014, "SqlAzure.2012")
			{
			}
		}

		[Test]
		public void TestInit()
		{
			//LinqToDB.SqlServer.Types.Configuration.Init();
		}

		[Test, SqlServerDataContext]
		public void TestHierarchyId(string context)
		{
			using (var conn = new DataConnection(context))
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

		[Test, SqlServerDataContext]
		public void CreateTest(string context)
		{
			using (var conn = new DataConnection(context))
			{
				conn.CreateTable<MyTable>();
			}
		}

		[Test, SqlServerDataContext]
		public void TestGeography(string context)
		{
			using (var conn = new DataConnection(context))
			{
				conn.InlineParameters = true;

				conn.GetTable<AllTypes2>()
					.Select(t => new
					{
						v1  = t.geographyDataType.STSrid,
						v2  = t.geographyDataType.Lat,
						v3  = t.geographyDataType.Long,
						v4  = t.geographyDataType.Z,
						v5  = t.geographyDataType.M,
						//v6  = t.geographyDataType.HasZ,
						//v7  = t.geographyDataType.HasM,
						v8  = SqlGeography.GeomFromGml(t.geographyDataType.AsGml(), 4326),
						v9  = t.geographyDataType.AsGml(),
						v10 = t.geographyDataType.ToString(),
						v11 = SqlGeography.Parse("LINESTRING(-122.360 47.656, -122.343 47.656)"),
						v12 = SqlGeography.Point(1, 1, 4326),
						v13 = SqlGeography.STGeomFromText(new SqlChars("LINESTRING(-122.360 47.656, -122.343 47.656)"), 4326),
					})
					.ToList();
			}
		}
	}
}
