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
			// Tie this server request to the originating remote test (by provider) so its query
			// trace/baseline logging resolves that test's context, not whatever remote test is
			// currently active. AsyncLocal flows through the request's async query execution.
			CustomTestContext.SetServerProvider(configuration);

			return connectionFactory(configuration, MappingSchema);
		}
	}
}

