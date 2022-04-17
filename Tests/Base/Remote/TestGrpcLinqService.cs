#if !NETFRAMEWORK
using System;

namespace Tests.Remote
{
	using LinqToDB.Data;
	using LinqToDB.Interceptors;
	using LinqToDB.Mapping;
	using LinqToDB.Remote;

	using LinqToDB.Remote.Grpc;

	internal class TestGrpcLinqService : GrpcLinqService
	{
		private readonly LinqService    _linqService;

		public bool SuppressSequentialAccess { get; set; }

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
			IInterceptor? interceptor,
			bool suppressSequentialAccess)
			: base(linqService, true)
		{
			_linqService             = linqService;
			_interceptor             = interceptor;
			SuppressSequentialAccess = suppressSequentialAccess;

		}

		public DataConnection CreateDataContext(string? configuration)
		{
			var dc = _linqService.CreateDataContext(configuration);

			if (!SuppressSequentialAccess && configuration?.IsAnyOf(TestProvName.AllSqlServerSequentialAccess) == true)
				dc.AddInterceptor(SequentialAccessCommandInterceptor.Instance);

			if (_interceptor != null)
				dc.AddInterceptor(_interceptor);

			return dc;
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
