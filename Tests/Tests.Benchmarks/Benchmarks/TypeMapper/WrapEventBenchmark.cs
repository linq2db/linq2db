using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;
using LinqToDB.Expressions.Types;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// shows reasonable performance degradation and allocations due to events remapping being not free
	public class WrapEventBenchmark
	{
		private Original.TestEventClass _emptyOriginalInstance = null!;
		private Original.TestEventClass _emptyOriginalWrappedInstance = null!;
		private Wrapped.TestEventClass  _emptyWrapperInstance = null!;

		private Original.TestEventClass _onOffOriginalInstance = null!;
		private Original.TestEventClass _onOffOriginalWrappedInstance = null!;
		private Wrapped.TestEventClass  _onOffWrapperInstance = null!;

		private Original.TestEventClass _subscribedOriginalInstance = null!;
		private Original.TestEventClass _subscribedOriginalWrappedInstance = null!;
		private Wrapped.TestEventClass  _subscribedWrapperInstance = null!;

		private TypeMapper _typeMapper = null!;

		[GlobalSetup]
		public void Setup()
		{
			_typeMapper = Wrapped.Helper.CreateTypeMapper();

			_emptyOriginalInstance             = new Original.TestEventClass();
			_emptyOriginalWrappedInstance      = new Original.TestEventClass();
			_emptyWrapperInstance              = _typeMapper.Wrap<Wrapped.TestEventClass>(_emptyOriginalWrappedInstance);

			_onOffOriginalInstance = new Original.TestEventClass();
			_onOffOriginalWrappedInstance      = new Original.TestEventClass();
			_onOffWrapperInstance              = _typeMapper.Wrap<Wrapped.TestEventClass>(_onOffOriginalWrappedInstance);
			
			_subscribedOriginalInstance        = new Original.TestEventClass();
			_subscribedOriginalWrappedInstance = new Original.TestEventClass();
			_subscribedWrapperInstance         = _typeMapper.Wrap<Wrapped.TestEventClass>(_subscribedOriginalWrappedInstance);

			_subscribedWrapperInstance.TestEvent  += WrappedHandler;
			_subscribedOriginalInstance.TestEvent += OriginalHandler;
		}

		[Benchmark]
		public void TypeMapperEmpty()
		{
			_emptyOriginalWrappedInstance.Fire();
		}

		[Benchmark(Baseline = true)]
		public void DirectAccessEmpty()
		{
			_emptyOriginalInstance.Fire();
		}

		[Benchmark]
		public void TypeMapperAddFireRemove()
		{
			_onOffWrapperInstance.TestEvent += WrappedHandler;
			_onOffOriginalWrappedInstance.Fire();
			_onOffWrapperInstance.TestEvent -= WrappedHandler;
		}

		[Benchmark]
		public void DirectAccessAddFireRemove()
		{
			_onOffOriginalInstance.TestEvent += OriginalHandler;
			_onOffOriginalInstance.Fire();
			_onOffOriginalInstance.TestEvent -= OriginalHandler;
		}

		[Benchmark]
		public void TypeMapperSubscribed()
		{
			_subscribedOriginalWrappedInstance.Fire();
		}

		[Benchmark]
		public void DirectAccessSubscribed()
		{
			_subscribedOriginalInstance.Fire();
		}

		[Benchmark]
		public void TypeMapperAddRemove()
		{
			var original = new Original.TestEventClass();
			var wrapper = _typeMapper.Wrap<Wrapped.TestEventClass>(original);

			wrapper.TestEvent += WrappedHandler;
			wrapper.TestEvent -= WrappedHandler;
		}

		[Benchmark]
		public void DirectAccessAddRemove()
		{
			var original = new Original.TestEventClass();
			original.TestEvent += OriginalHandler;
			original.TestEvent -= OriginalHandler;
		}

		private void WrappedHandler(object? sender, Wrapped.TestClass2 e)
		{ }

		private void OriginalHandler(object? sender, Original.TestClass2 e)
		{ }
	}
}
