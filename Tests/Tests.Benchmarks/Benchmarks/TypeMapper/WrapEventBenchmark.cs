using BenchmarkDotNet.Attributes;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	// FIX: mapped event shows strange numbers...
	public class WrapEventBenchmark
	{
		private Original.TestClass2 _originalInstance;
		private Original.TestClass2 _originalWrappedInstance;
		private Wrapped.TestClass2 _wrapperInstance;

		[GlobalSetup]
		public void Setup()
		{
			var typeMapper = new TypeMapper(typeof(Original.TestClass2), typeof(Original.TestEventHandler));
			typeMapper.RegisterWrapper<Wrapped.TestClass2>();
			typeMapper.RegisterWrapper<Wrapped.TestEventHandler>();

			_originalInstance = new Original.TestClass2();
			_originalWrappedInstance = new Original.TestClass2();
			_wrapperInstance = typeMapper.Wrap<Wrapped.TestClass2>(_originalWrappedInstance);
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

		private void WrappedHandler(object sender, Wrapped.TestClass2 e)
		{ }

		private void OriginalHandler(object sender, Original.TestClass2 e)
		{ }
	}
}
