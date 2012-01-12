using System;

using LinqToDB_Temp.Common;

using NUnit.Framework;

namespace Tests.Common
{
	[TestFixture]
	public class ChangeTypeTest
	{
		[Test]
		public void FromString1()
		{
			Assert.AreEqual(11, Converter.ChangeType("11", typeof(int)));
			Assert.AreEqual(12, Converter.ChangeType("12", typeof(int)));
		}

		[Test]
		public void FromString2()
		{
			Assert.AreEqual(11, Converter.ChangeTypeTo<int>("11"));
			Assert.AreEqual(12, Converter.ChangeTypeTo<int>("12"));
		}
	}
}
