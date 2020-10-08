using System.Data.SqlClient;
using NUnit.Framework;

namespace Tests.Identity
{
	public class MiscTests
	{
		[Test]
		public void SqlServerConnectionStringBuilderTest()
		{
			var csb =
				new SqlConnectionStringBuilder(
					@"Server = (local)\SQL2012SP1; Database = master; User ID = sa; Password = Password12!")
				{
					InitialCatalog = "demo1"
				};

			var pwd = csb.Password;

			Assert.AreEqual("Password12!", pwd);
			Assert.IsTrue(csb.ConnectionString.Contains("Password12!"));

			csb.InitialCatalog = "demo";

			Assert.AreEqual("Password12!", pwd);
			Assert.IsTrue(csb.ConnectionString.Contains("Password12!"));
		}
	}
}
