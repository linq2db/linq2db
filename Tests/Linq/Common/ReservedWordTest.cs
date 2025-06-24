using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Common
{
	[TestFixture]
	public class ReservedWordTest
	{
		[Test]
		public void Test([Values("", TestProvName.AllPostgreSQL, TestProvName.AllOracle)] string providerName)
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(ReservedWords.IsReserved("select", providerName), Is.True);
				Assert.That(ReservedWords.IsReserved("SELECT", providerName), Is.True);
				Assert.That(ReservedWords.IsReserved("Select", providerName), Is.True);
			}
		}
	}
}
