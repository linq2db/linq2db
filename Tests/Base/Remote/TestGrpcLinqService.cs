#if !NETFRAMEWORK
using System;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using LinqToDB.Remote;

using LinqToDB.Remote.Grpc;

namespace Tests.Remote
{
	internal sealed class TestGrpcLinqService : GrpcLinqService
	{
		private readonly LinqService    _linqService;

		public bool AllowUpdates
		{
			get => _linqService.AllowUpdates;
			set => _linqService.AllowUpdates = value;
		}

		public MappingSchema? MappingSchema
		{
			get => _linqService.MappingSchema;
			set => _linqService.MappingSchema = value;
		}

		public TestGrpcLinqService(LinqService linqService)
			: base(linqService, true)
		{
			_linqService = linqService;

		}
	}
}
#endif
