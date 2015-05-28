using System;
using System.Linq;

using LinqToDB.Data;

using Xunit;

[assembly: System.Security.AllowPartiallyTrustedCallers]

namespace Tests.Security
{
	using Model;
	using TestHelpers;

	[PartialTrustFixture]
	public class PartialTrustTests : MarshalByRefObject
	{
		[Fact]
		public void Test()
		{
			//var conn = new System.Data.SQLite.SQLiteConnection();
			//var conn = new IBM.Data.Informix.IfxConnection();

			using (var db = new DataConnection("SqlServer.2012"))
			{
				var count = db.GetTable<Parent>().Count();
			}
		}
	}
}
