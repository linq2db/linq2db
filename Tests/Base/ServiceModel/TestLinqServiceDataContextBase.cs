#if NET472
using System;

using LinqToDB;
using LinqToDB.ServiceModel;

namespace Tests.ServiceModel
{
	public class TestLinqServiceDataContextBase : RemoteDataContextBase
	{
		public TestLinqServiceDataContextBase(LinqService linqService)
		{
			_linqService = linqService;
		}

		readonly LinqService _linqService;

		protected override ILinqClient GetClient()
		{
			return new TestLinqServiceClient(_linqService);
		}

		protected override IDataContext Clone()
		{
			return new TestLinqServiceDataContextBase(_linqService)
			{
				MappingSchema = MappingSchema,
				Configuration = Configuration,
			};
		}

		protected override string ContextIDPrefix => "TestLinqService";
	}
}
#endif
