using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;
using LinqToDB.Expressions.Types;

namespace LinqToDB.Benchmarks.Types
{
	// shows reasonable performance degradation and allocations due to events remapping being not free
	public class WrapEventBenchmark
	{
		private TestClasses.Original.TestEventClass _emptyOriginalInstance = null!;
		private TestClasses.Original.TestEventClass _emptyOriginalWrappedInstance = null!;
		private TestClasses.Wrapped.TestEventClass  _emptyWrapperInstance = null!;

		private TestClasses.Original.TestEventClass _onOffOriginalInstance = null!;
		private TestClasses.Original.TestEventClass _onOffOriginalWrappedInstance = null!;
		private TestClasses.Wrapped.TestEventClass  _onOffWrapperInstance = null!;

		private TestClasses.Original.TestEventClass _subscribedOriginalInstance = null!;
		private TestClasses.Original.TestEventClass _subscribedOriginalWrappedInstance = null!;
		private TestClasses.Wrapped.TestEventClass  _subscribedWrapperInstance = null!;

		private TypeMapper _typeMapper = null!;

		[GlobalSetup]
		public void Setup()
		{
			_typeMapper = TestClasses.Wrapped.Helper.CreateTypeMapper();

			_emptyOriginalInstance             = new TestClasses.Original.TestEventClass();
			_emptyOriginalWrappedInstance      = new TestClasses.Original.TestEventClass();
			_emptyWrapperInstance              = _typeMapper.Wrap<TestClasses.Wrapped.TestEventClass>(_emptyOriginalWrappedInstance);

			_onOffOriginalInstance             = new TestClasses.Original.TestEventClass();
			_onOffOriginalWrappedInstance      = new TestClasses.Original.TestEventClass();
			_onOffWrapperInstance              = _typeMapper.Wrap<TestClasses.Wrapped.TestEventClass>(_onOffOriginalWrappedInstance);
			
			_subscribedOriginalInstance        = new TestClasses.Original.TestEventClass();
			_subscribedOriginalWrappedInstance = new TestClasses.Original.TestEventClass();
			_subscribedWrapperInstance         = _typeMapper.Wrap<TestClasses.Wrapped.TestEventClass>(_subscribedOriginalWrappedInstance);

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
			var original = new TestClasses.Original.TestEventClass();
			var wrapper = _typeMapper.Wrap<TestClasses.Wrapped.TestEventClass>(original);

			wrapper.TestEvent += WrappedHandler;
			wrapper.TestEvent -= WrappedHandler;
		}

		[Benchmark]
		public void DirectAccessAddRemove()
		{
			var original = new TestClasses.Original.TestEventClass();
			original.TestEvent += OriginalHandler;
			original.TestEvent -= OriginalHandler;
		}

		private void WrappedHandler(object? sender, TestClasses.Wrapped.TestClass2 e)
		{ }

		private void OriginalHandler(object? sender, TestClasses.Original.TestClass2 e)
		{ }
	}
}
