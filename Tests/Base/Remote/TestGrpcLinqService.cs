#if !NETFRAMEWORK
using System;
using LinqToDB;

namespace Tests.Remote
{
	using LinqToDB.Data;
	using LinqToDB.Interceptors;
	using LinqToDB.Mapping;
	using LinqToDB.Remote;

	using LinqToDB.Remote.Grpc;

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

		public TestGrpcLinqService(
			LinqService linqService,
			IInterceptor? interceptor)
			: base(linqService, true)
		{
			_linqService = linqService;
			_interceptor = interceptor;

		}

		// for now we need only one test interceptor
		private IInterceptor? _interceptor;

		public void AddInterceptor(IInterceptor interceptor)
		{
			if (_interceptor != null)
				throw new InvalidOperationException();

			_interceptor = interceptor;
		}

		public void RemoveInterceptor()
		{
			if (_interceptor == null)
				throw new InvalidOperationException();
			_interceptor = null;
		}
	}
}
#endif
