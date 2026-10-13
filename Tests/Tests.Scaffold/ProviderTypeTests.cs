using System;
using System.Data.SqlTypes;
using System.Linq;

using LinqToDB;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

using Tests.Scaffold.Harness;

using static Tests.Scaffold.Harness.TestSchema;

namespace Tests.Scaffold
{
	// generation-only assertions: SQL Server and PostgreSQL are not available to this project
	[TestFixture]
	public sealed class ProviderTypeTests
	{
		[Test]
		public void SqlServerProviderTypes([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SqlServer.CreateSchema();

			schema.AddTable(
				new SqlObjectName("Types", Schema: "dbo"),
				[
					Col("Id"          , "int"),
					Col("IntValue"    , "sql_int"           , nullable: true),
					Col("StringValue" , "sql_nvarchar"      , nullable: true),
					Col("GuidValue"   , "sql_guid"          , nullable: true),
					Col("DecimalValue", "sql_decimal"       , nullable: true),
					Col("DateValue"   , "sql_datetime"      , nullable: true),
					Col("BinaryValue" , "sql_varbinary"     , nullable: true),
					Col("BoolValue"   , "sql_bit"           , nullable: true),
					Col("XmlValue"    , "sql_xml"           , nullable: true),
					Col("Hierarchy"   , "hierarchyid"       , nullable: true),
					Col("Geography"   , "geography"         , nullable: true),
					Col("Geometry"    , "geometry"          , nullable: true),
				],
				primaryKey: ["Id"]);

			var mappings = ProviderProfile.SqlServer.CreateTypeMappings()
				.Map<SqlInt32>  ("sql_int"      , DataType.Int32)
				.Map<SqlString> ("sql_nvarchar" , DataType.NVarChar)
				.Map<SqlGuid>   ("sql_guid"     , DataType.Guid)
				.Map<SqlDecimal>("sql_decimal"  , DataType.Decimal)
				.Map<SqlDateTime>("sql_datetime", DataType.DateTime)
				.Map<SqlBinary> ("sql_varbinary", DataType.VarBinary)
				.Map<SqlBoolean>("sql_bit"      , DataType.Boolean)
				.Map<SqlXml>    ("sql_xml"      , DataType.Xml);

			var model = ScaffoldHarness.Build(nameof(SqlServerProviderTypes), language, ProviderProfile.SqlServer, schema, typeMappings: mappings);

			var entity = model.GetType("Type");

			string TypeName(string property)
			{
				var type = entity.GetProperty(property)!.PropertyType;

				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Microsoft.FSharp.Core.FSharpOption<>))
					type = type.GetGenericArguments()[0];

				return (Nullable.GetUnderlyingType(type) ?? type).FullName!;
			}

			TypeName("IntValue")    .ShouldBe(typeof(SqlInt32).FullName);
			TypeName("StringValue") .ShouldBe(typeof(SqlString).FullName);
			TypeName("GuidValue")   .ShouldBe(typeof(SqlGuid).FullName);
			TypeName("DecimalValue").ShouldBe(typeof(SqlDecimal).FullName);
			TypeName("DateValue")   .ShouldBe(typeof(SqlDateTime).FullName);
			TypeName("BinaryValue") .ShouldBe(typeof(SqlBinary).FullName);
			TypeName("BoolValue")   .ShouldBe(typeof(SqlBoolean).FullName);
			TypeName("XmlValue")    .ShouldBe(typeof(SqlXml).FullName);
			TypeName("Hierarchy")   .ShouldBe("Microsoft.SqlServer.Types.SqlHierarchyId");
			TypeName("Geography")   .ShouldBe("Microsoft.SqlServer.Types.SqlGeography");
			TypeName("Geometry")    .ShouldBe("Microsoft.SqlServer.Types.SqlGeometry");

			using var db = model.CreateContext(ProviderProfile.SqlServer.CreateOfflineOptions());

			SqlShape.GetSql((IQueryable)model.GetTables(db).Single().GetTable()).ShouldContain("[Hierarchy]");
		}

		[Test]
		[ActiveIssue(3749, Details = "Identity.Sequence not emitted as SequenceName", ErrorMessage = "SequenceName")]
		public void PostgreSqlProviderTypes([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.PostgreSQL.CreateSchema();

			schema.AddTable(
				new SqlObjectName("types", Schema: "public"),
				[
					Col("id"      , "int"),
					Col("interval", "interval", nullable: true),
					Col("inet"    , "inet"    , nullable: true),
					Col("point"   , "point"   , nullable: true),
					Col("polygon" , "polygon" , nullable: true),
				],
				primaryKey      : ["id"],
				identity        : "id",
				identitySequence: new SqlObjectName("types_id_seq", Schema: "public"));

			var model = ScaffoldHarness.Build(nameof(PostgreSqlProviderTypes), language, ProviderProfile.PostgreSQL, schema);

			var entity = model.GetType("Type");

			string TypeName(string property)
			{
				var type = entity.GetProperty(property)!.PropertyType;

				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Microsoft.FSharp.Core.FSharpOption<>))
					type = type.GetGenericArguments()[0];

				return (Nullable.GetUnderlyingType(type) ?? type).FullName!;
			}

			TypeName("Interval").ShouldBe("NpgsqlTypes.NpgsqlInterval");
			TypeName("Inet")    .ShouldBe("NpgsqlTypes.NpgsqlInet");
			TypeName("Point")   .ShouldBe("NpgsqlTypes.NpgsqlPoint");
			TypeName("Polygon") .ShouldBe("NpgsqlTypes.NpgsqlPolygon");

			using var db = model.CreateContext(ProviderProfile.PostgreSQL.CreateOfflineOptions());

			var descriptor = db.MappingSchema.GetEntityDescriptor(entity);

			descriptor.Columns.Single(static c => c.ColumnName == "id").SequenceName.ShouldNotBeNull("SequenceName").SequenceName.ShouldBe("types_id_seq");
		}
	}
}
