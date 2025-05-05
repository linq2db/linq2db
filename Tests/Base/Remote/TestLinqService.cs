using System;

using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Remote;

namespace Tests.Remote
{
	internal sealed class TestLinqService(Func<string?, MappingSchema?, DataConnection> connectionFactory) : LinqService(), ITestLinqService
	{
		public override DataConnection CreateDataContext(string? configuration)
		{
			return connectionFactory(configuration, MappingSchema);
		}
	}
}

