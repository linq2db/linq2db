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
			Assert.True(ReservedWords.IsReserved("select", providerName));
			Assert.True(ReservedWords.IsReserved("SELECT", providerName));
			Assert.True(ReservedWords.IsReserved("Select", providerName));
		}
	}
}
