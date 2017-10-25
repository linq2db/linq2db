using System;
using System.Linq;

using LinqToDB.Data;

using Xunit;

//[assembly: System.Security.AllowPartiallyTrustedCallers]

namespace Tests.Security
{
	using Model;
	using TestHelpers;

//	[PartialTrustFixture]
	public class PartialTrustTests : MarshalByRefObject
	{
//		[Fact]
		public void Test()
		{
			if (TestBase.UserProviders.Contains("SqlServer.2012"))
			{
				using (var db = new DataConnection("SqlServer.2012"))
				{
					var count = db.GetTable<Parent>().Count();
				}
			}
		}
	}
}
