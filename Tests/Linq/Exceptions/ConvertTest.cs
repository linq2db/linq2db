using System;

using LinqToDB;
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

		[Test, ExpectedException(typeof(LinqToDBConvertException), ExpectedMessage = "Mapping ambiguity. MapValue(1) attribute is defined for both 'Tests.Exceptions.ConvertTest+Enum1.Value1' and 'Tests.Exceptions.ConvertTest+Enum1.Value2'.")]
		public void ConvertToEnum1()
		{
			Assert.AreEqual(Enum1.Value2, ConvertTo<Enum1>.From(1));
		}
	}
}
