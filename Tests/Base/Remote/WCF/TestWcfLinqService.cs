#if NETFRAMEWORK
using System;

using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using LinqToDB.Remote;

using LinqToDB.Remote.Wcf;

namespace Tests.Remote
{

	internal sealed class TestWcfLinqService : WcfLinqService, ITestLinqService
	{
		private readonly LinqService    _linqService;

		public bool AllowUpdates
		{
			get => _linqService.AllowUpdates;
			set => _linqService.AllowUpdates = value;
		}

		MappingSchema? ITestLinqService.MappingSchema
		{
			get => _linqService.MappingSchema;
			set => _linqService.MappingSchema = value;
		}

		public TestWcfLinqService(LinqService linqService)
			: base(linqService, true)
		{
			_linqService = linqService;
		}
	}
}
#endif
