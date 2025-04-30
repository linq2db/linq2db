using System;

using LinqToDB;
using LinqToDB.Data;
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

		[Test]
		public void MappingSchema_ShouldMapBasicDotNetTypes()
		{
			var schema = YdbMappingSchema.Instance;
			Assert.That(schema, Is.Not.Null);

			Assert.Multiple(() =>
			{
				Assert.That(schema.GetDataType(typeof(string)).Type.DataType, Is.EqualTo(DataType.VarChar));
				Assert.That(schema.GetDataType(typeof(bool)).Type.DataType, Is.EqualTo(DataType.Boolean));
				Assert.That(schema.GetDataType(typeof(Guid)).Type.DataType, Is.EqualTo(DataType.Guid));
				Assert.That(schema.GetDataType(typeof(byte[])).Type.DataType, Is.EqualTo(DataType.VarBinary));
			});
		}
	}
}
