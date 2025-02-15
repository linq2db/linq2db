#if NETFRAMEWORK
using System.Data.Services.Providers;

using LinqToDB.Remote;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class DataServiceTests
	{
		[Test]
		public void Test1()
		{
			var ds = new DataService<NorthwindDB>();
			var mp = ds.GetService(typeof(IDataServiceMetadataProvider));
		}
	}
}
#endif
