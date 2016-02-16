using System;

using LinqToDB.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Exceptions
{
	class ConvertTest : TestBase
	{
		enum Enum1
		{
			[MapValue(1)] Value1,
			[MapValue(1)] Value2,
		}

		[Test]
		public void ConvertToEnum1()
		{
			Assert.Throws(
				typeof(LinqToDBConvertException),
				() => ConvertTo<Enum1>.From(1),
				"Mapping ambiguity. MapValue(1) attribute is defined for both 'Tests.Exceptions.ConvertTest+Enum1.Value1' and 'Tests.Exceptions.ConvertTest+Enum1.Value2'.");
		}
	}
}
