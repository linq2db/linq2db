using LinqToDB.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Exceptions
{
	sealed class ConvertTests : TestBase
	{
		enum Enum1
		{
			[MapValue(1)] Value1,
			[MapValue(1)] Value2,
		}

		[Test]
		public void ConvertToEnum1()
		{
			Assert.That(
				() => ConvertTo<Enum1>.From(1),
				Throws.TypeOf<LinqToDBConvertException>().With.Message.EqualTo("Mapping ambiguity. MapValue(1) attribute is defined for both 'Tests.Exceptions.ConvertTests+Enum1.Value1' and 'Tests.Exceptions.ConvertTests+Enum1.Value2'."));
		}
	}
}
