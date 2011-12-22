using System;

using NUnit.Framework;

namespace Tests.Exceptions
{
	using LinqToDB_Temp;

	[TestFixture]
	public class MetadataTest
	{
		[Test, ExpectedException(typeof(LinqToDBException), ExpectedMessage = "There is no corresponding TypeCode for 'Tests.Exceptions.MetadataTest'.")]
		public void TypeCodeTest()
		{
			typeof(MetadataTest).ToCode();
		}
	}
}
