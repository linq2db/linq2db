using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Mapping
{
	public class MapValueTests : TestBase
	{
		public enum TestEnum1
		{
			[MapValue("F")] First,
			[MapValue("S")] Second,
			[MapValue("T")] Third
		}

		[Test]
		public void Test1([DataSources] string context, [Values] TestEnum1 val)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable([new { EnumValue = val }]);

			var value = tmp.Select(t =>
				t.EnumValue == TestEnum1.First  ? "First"  :
				t.EnumValue == TestEnum1.Second ? "Second" : t.EnumValue.ToString()
			).First();

			Assert.That(value, Is.EqualTo(val.ToString()));
		}

		public enum TestEnum2
		{
			[MapValue('F')] First,
			[MapValue('S')] Second,
			[MapValue('T')] Third
		}

		[Test]
		public void Test2([DataSources] string context, [Values] TestEnum2 val)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable([new { EnumValue = val }]);

			var value = tmp.Select(t =>
				t.EnumValue == TestEnum2.First  ? "First"  :
				t.EnumValue == TestEnum2.Second ? "Second" : t.EnumValue.ToString()
			).First();

			Assert.That(value, Is.EqualTo(val.ToString()));
		}

		[Test]
		public void Test3([DataSources] string context, [Values] TestEnum1 val)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable([new { EnumValue = val }]);

			var value = tmp.Select(t =>
				t.EnumValue == TestEnum1.Second ? "Second" :
				t.EnumValue == TestEnum1.Third  ? "Third"  : t.EnumValue.ToString()
			).First();

			Assert.That(value, Is.EqualTo(val.ToString()));
		}
	}
}
