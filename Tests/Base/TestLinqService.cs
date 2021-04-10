#if NET472
using System;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using LinqToDB.ServiceModel;

namespace Tests
{
	internal class TestLinqService : LinqService
	{
		public bool SuppressSequentialAccess { get; set; }

		public TestLinqService(MappingSchema? ms, IInterceptor? interceptor, bool suppressSequentialAccess)
			: base(ms)
		{
			_interceptor             = interceptor;
			SuppressSequentialAccess = suppressSequentialAccess;
		}

		public override DataConnection CreateDataContext(string? configuration)
		{
			var dc = base.CreateDataContext(configuration);

			if (!SuppressSequentialAccess && configuration?.Contains(TestProvName.SqlServer2019SequentialAccess) == true)
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
