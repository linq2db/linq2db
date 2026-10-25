using System;

using LinqToDB.Tools;

using NUnit.Framework;

using Shouldly;

namespace Tests.Tools
{
	[TestFixture]
	public class ToDiagnosticStringTests
	{
		[Test]
		public void TestDiagnosticString1()
		{
			var str = new[] { 1, 2, 222 }.ToDiagnosticString();

			Assert.That(@"Count : 3
+-------+
| Value |
+-------+
|     1 |
|     2 |
|   222 |
+-------+".Replace("\r", "").Replace("\n", ""), Is.EqualTo(str.Replace("\r", "").Replace("\n", "")));
		}

		sealed class TestDiagnostic
		{
			public string?  StringValue;
			public DateTime DateTimeValue { get; set; }
			public decimal  DecimalValue  { get; set; }
		}

		[Test]
		[SetCulture("en-US")]
		public void TestDiagnosticString2()
		{
			var str = new[]
			{
				new TestDiagnostic { StringValue = null,                          DateTimeValue = new DateTime(2016, 11, 23), DecimalValue = 1 },
				new TestDiagnostic { StringValue = "lkajsd laskdj asd",           DateTimeValue = new DateTime(2016, 11, 13), DecimalValue = 11 },
				new TestDiagnostic { StringValue = "dakasdlkjjkasd  djkadlskdj ", DateTimeValue = new DateTime(2016, 11, 22), DecimalValue = 111.3m },
				new TestDiagnostic { StringValue = "dkjdkdjkl102398 3 1231233",   DateTimeValue = new DateTime(2016, 10, 23), DecimalValue = 1111111 },
			}.ToDiagnosticString();

			Assert.That(@"Count : 4
+---------------------+--------------+-----------------------------+
| DateTimeValue       | DecimalValue | StringValue                 |
+---------------------+--------------+-----------------------------+
| 2016-11-23 12:00:00 |            1 | <NULL>                      |
| 2016-11-13 12:00:00 |           11 | lkajsd laskdj asd           |
| 2016-11-22 12:00:00 |        111.3 | dakasdlkjjkasd  djkadlskdj  |
| 2016-10-23 12:00:00 |      1111111 | dkjdkdjkl102398 3 1231233   |
+---------------------+--------------+-----------------------------+".Replace("\r", "").Replace("\n", ""), Is.EqualTo(str.Replace("\r", "").Replace("\n", "")));
		}

		sealed class NoMembers(string text)
		{
			public override string ToString() => text;
		}

		[Test]
		public void TestDiagnosticStringNoMembers()
		{
			var str = new[] { new NoMembers("one"), new NoMembers("three") }
				.ToDiagnosticString()
				.Replace("\r", "")
				.Replace("\n", "");

			str.ShouldBe(@"Count : 2
+-----------+
| NoMembers |
+-----------+
| one       |
| three     |
+-----------+".Replace("\r", "").Replace("\n", ""));
		}

		sealed class Row
		{
			public int ID { get; set; }
		}

		[Test]
		public void TestDiagnosticStringWithoutHeader()
		{
			var str = new[] { new Row { ID = 1 }, new Row { ID = 12345 } }
				.ToDiagnosticString(addTableHeader: false)
				.Replace("\r", "")
				.Replace("\n", "");

			str.ShouldBe(@"Count : 2
+-------+
|     1 |
| 12345 |
+-------+".Replace("\r", "").Replace("\n", ""));
		}

		[Test]
		public void TestDiagnosticStringScalarWithoutHeader()
		{
			var str = new[] { 1, 222 }
				.ToDiagnosticString(addTableHeader: false)
				.Replace("\r", "")
				.Replace("\n", "");

			str.ShouldBe(@"Count : 2
+-------+
|     1 |
|   222 |
+-------+".Replace("\r", "").Replace("\n", ""));
		}
	}
}
