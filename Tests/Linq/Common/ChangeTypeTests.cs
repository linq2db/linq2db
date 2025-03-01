using LinqToDB.Common;
using LinqToDB.Internal.Conversion;

using NUnit.Framework;

namespace Tests.Common
{
	[TestFixture]
	public class ChangeTypeTests
	{
		[Test]
		public void FromString1()
		{
			Assert.Multiple(() =>
			{
				Assert.That(Converter.ChangeType("11", typeof(int)), Is.EqualTo(11));
				Assert.That(Converter.ChangeType("12", typeof(int)), Is.EqualTo(12));
			});
		}

		[Test]
		public void FromString2()
		{
			Assert.Multiple(() =>
			{
				Assert.That(Converter.ChangeTypeTo<int>("11"), Is.EqualTo(11));
				Assert.That(Converter.ChangeTypeTo<int>("12"), Is.EqualTo(12));
			});
		}
	}
}
