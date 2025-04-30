using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LinqToDB.DataProvider.Ydb;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class YdbTests: DataProviderTestBase
	{
		[Test]
		public void GetInstance_ShouldCreateAdapterSuccessfully()
		{
			// Act
			var adapter = YdbProviderAdapter.GetInstance();

			// Assert
			Assert.That(adapter, Is.Not.Null);
			Assert.Multiple(() =>
			{
				Assert.That(adapter.ConnectionType, Is.Not.Null);
				Assert.That(adapter.DataReaderType, Is.Not.Null);
				Assert.That(adapter.ParameterType, Is.Not.Null);
				Assert.That(adapter.CommandType, Is.Not.Null);
				Assert.That(adapter.MappingSchema, Is.Not.Null);
			});
		}

	}
}
