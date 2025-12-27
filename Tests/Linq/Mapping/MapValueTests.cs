using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Mapping
{
	public class MapValueTests : TestBase
	{
		enum TestEnum1
		{
			[MapValue("F")] First,
			[MapValue("S")] Second,
			[MapValue("T")] Third
		}

		[Test]
		public void Test1([DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable([new { EnumValue = TestEnum1.First }]);

			var value = tmp.Select(t =>
				t.EnumValue == TestEnum1.First  ? "First"  :
				t.EnumValue == TestEnum1.Second ? "Second" : t.EnumValue.ToString()
			).First();

			Assert.That(value, Is.EqualTo("First"));
		}

		enum TestEnum2
		{
			[MapValue('F')] First,
			[MapValue('S')] Second,
			[MapValue('T')] Third
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable([new { EnumValue = TestEnum2.First }]);

			var value = tmp.Select(t =>
				t.EnumValue == TestEnum2.First  ? "First"  :
				t.EnumValue == TestEnum2.Second ? "Second" : t.EnumValue.ToString()
			).First();

			Assert.That(value, Is.EqualTo("First"));
		}

		[Test]
		public void Test3([DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable([new { EnumValue = TestEnum1.First }]);

			var value = tmp.Select(t =>
				t.EnumValue == TestEnum1.Second ? "Second" :
				t.EnumValue == TestEnum1.Third  ? "Third"  : t.EnumValue.ToString()
			).First();

			Assert.That(value, Is.EqualTo("First"));
		}
	}
}
