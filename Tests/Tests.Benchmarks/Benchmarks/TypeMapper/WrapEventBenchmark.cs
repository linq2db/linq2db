using BenchmarkDotNet.Attributes;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// FIX: mapped event should be reimplemented
	public class WrapEventBenchmark
	{
		private Original.TestEventClass _originalInstance;
		private Original.TestEventClass _originalWrappedInstance;
		private Wrapped.TestEventClass _wrapperInstance;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = Wrapped.Helper.CreateTypeMapper();

			_originalInstance = new Original.TestEventClass();
			_originalWrappedInstance = new Original.TestEventClass();
			_wrapperInstance = typeMapper.Wrap<Wrapped.TestEventClass>(_originalWrappedInstance);
		}

		[Benchmark]
		public void TypeMapperEmpty()
		{
			_originalWrappedInstance.Fire();
		}

		[Benchmark(Baseline = true)]
		public void DirectAccessEmpty()
		{
			_originalInstance.Fire();
		}

		[Benchmark]
		public void TypeMapperSubscribed()
		{
			_wrapperInstance.TestEvent += WrappedHandler;
			_originalWrappedInstance.Fire();
			_wrapperInstance.TestEvent -= WrappedHandler;
		}

		[Benchmark]
		public void DirectAccessSubscribed()
		{
			_originalInstance.TestEvent += OriginalHandler;
			_originalInstance.Fire();
			_originalInstance.TestEvent -= OriginalHandler;
		}

		private void WrappedHandler(object sender, Wrapped.TestEventClass e)
		{ }

		private void OriginalHandler(object sender, Original.TestEventClass e)
		{ }
	}
}
