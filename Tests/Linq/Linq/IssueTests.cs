using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class IssueTests : TestBase
	{
		// https://github.com/linq2db/linq2db/issues/60
		//
		[Test]
		public void Issue60Test([IncludeDataSources(TestProvName.AllSqlServer, ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataConnection(context);
			var sp       = db.DataProvider.GetSchemaProvider();
			var dbSchema = sp.GetSchema(db);

			var q =
					from t in dbSchema.Tables
					from c in t.Columns
					where c.ColumnType!.StartsWith("tinyint") && c.MemberType.StartsWith("sbyte")
					select c;

			var column = q.FirstOrDefault();

			Assert.That(column, Is.Null);
		}

		[Test]
		public void Issue2823Guid([IncludeDataSources(false, TestProvName.AllFirebird)] string context, [Values] bool inlineParameters)
		{
			using var db = (DataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable<TableWithGuid>();
			Assert.That(db.LastQuery!, Does.Contain("\"String\"   CHAR(38)"));
			Assert.That(db.LastQuery!, Does.Contain("\"StringN\"  CHAR(38)"));

			if (context.IsAnyOf(TestProvName.AllFirebirdLess4))
			{
				Assert.That(db.LastQuery!, Does.Contain("\"Default\"  CHAR(16) CHARACTER SET OCTETS"));
				Assert.That(db.LastQuery!, Does.Contain("\"Binary\"   CHAR(16) CHARACTER SET OCTETS"));
				Assert.That(db.LastQuery!, Does.Contain("\"DefaultN\" CHAR(16) CHARACTER SET OCTETS"));
				Assert.That(db.LastQuery!, Does.Contain("\"BinaryN\"  CHAR(16) CHARACTER SET OCTETS"));
			}
			else
			{
				Assert.That(db.LastQuery!, Does.Contain("\"Default\"  BINARY(16)"));
				Assert.That(db.LastQuery!, Does.Contain("\"Binary\"   BINARY(16)"));
				Assert.That(db.LastQuery!, Does.Contain("\"DefaultN\" BINARY(16)"));
				Assert.That(db.LastQuery!, Does.Contain("\"BinaryN\"  BINARY(16)"));
			}

			db.InlineParameters = inlineParameters;

			table.Insert(() => new TableWithGuid
			{
				Default = TestData.Guid1,
				Binary = TestData.Guid2,
				String = TestData.Guid3,
				DefaultN = TestData.Guid4,
				BinaryN = TestData.Guid5,
				StringN = TestData.Guid6,
			});

			var data = table.ToArray();
			Assert.That(data, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0].Default, Is.EqualTo(TestData.Guid1));
				Assert.That(data[0].Binary, Is.EqualTo(TestData.Guid2));
				Assert.That(data[0].String, Is.EqualTo(TestData.Guid3));
				Assert.That(data[0].DefaultN, Is.EqualTo(TestData.Guid4));
				Assert.That(data[0].BinaryN, Is.EqualTo(TestData.Guid5));
				Assert.That(data[0].StringN, Is.EqualTo(TestData.Guid6));

				Assert.That(table.Where(x => x.Default == TestData.Guid1).Count(), Is.EqualTo(1));
				Assert.That(table.Where(x => x.Binary == TestData.Guid2).Count(), Is.EqualTo(1));
				Assert.That(table.Where(x => x.String == TestData.Guid3).Count(), Is.EqualTo(1));
				Assert.That(table.Where(x => x.DefaultN == TestData.Guid4).Count(), Is.EqualTo(1));
				Assert.That(table.Where(x => x.BinaryN == TestData.Guid5).Count(), Is.EqualTo(1));
				Assert.That(table.Where(x => x.StringN == TestData.Guid6).Count(), Is.EqualTo(1));
			}
		}

		[Table]
		sealed class TableWithGuid
		{
			[Column                           ] public Guid Default   { get; set; }
			[Column(DataType = DataType.Guid) ] public Guid Binary    { get; set; }
			[Column(DataType = DataType.Char) ] public Guid String    { get; set; }

			[Column                           ] public Guid? DefaultN { get; set; }
			[Column(DataType = DataType.Guid) ] public Guid? BinaryN  { get; set; }
			[Column(DataType = DataType.NChar)] public Guid? StringN  { get; set; }
		}
	}
}
