using System;
using System.Data.Services.Providers;

using LinqToDB.ServiceModel;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class DataServiceTests
	{
#if	!MONO
		[Test]
		public void Test1()
		{
			var ds = new DataService<NorthwindDB>();
			var mp = ds.GetService(typeof(IDataServiceMetadataProvider));
		}
#endif
	}
}
