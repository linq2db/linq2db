using System;
using System.Collections;
using System.Data;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	public interface ITestClass
	{
	}

	public interface ITestClass2
	{
	}

	namespace Original
	{
		// use NoInlining to prevent JIT cheating with direct calls. We are not interested in such results
		public class TestClass : ITestClass
		{
			private static readonly DataTable _GetOleDbSchemaTableResult = new DataTable();

			public TestEnum EnumProperty 
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
				[MethodImpl(MethodImplOptions.NoInlining)] set;
			}

			public string StringProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
				[MethodImpl(MethodImplOptions.NoInlining)] set;
			}
			public SqlDbType KnownEnumProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
				[MethodImpl(MethodImplOptions.NoInlining)] set;
			}

			public decimal DecimalProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
			}

			public bool BooleanProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
			}

			public int IntProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public DataTable GetOleDbSchemaTable(Guid schema, object[] restrictions) => _GetOleDbSchemaTableResult;

			[MethodImpl(MethodImplOptions.NoInlining)]
			public static void ClearAllPools() { }
		}

		public class TestClass2 : IDisposable, ITestClass2
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			public TestClass2()
			{
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public TestClass2(string connectionString)
			{
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public TestClass2(TimeSpan timeSpan)
			{
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public TestClass2(int p1, string p2)
			{
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public TestClass2(string p1, string p2)
			{
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public TestClass2(TestClass2 p1, TestEnum p2)
			{
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public TestClass2(TestClass2 p1, string p2)
			{
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public TestClass2(TestClass2 p1, TestEnum p2, TestClass p3)
			{
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public TestClass2(int year, int month, int day, int hour, int minute, int second, int nanosecond, string timeZone)
			{
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public void CreateDatabase()
			{
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public void Dispose()
			{
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public string QuoteIdentifier(string input)
			{
				return input;
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			public void WriteToServer(IDataReader rd) { }

			[MethodImpl(MethodImplOptions.NoInlining)]
			public TestClass2 Add(TestClass2 p) => p;

			[MethodImpl(MethodImplOptions.NoInlining)]
			public IEnumerable GetEnumerator()
			{
				yield break;
			}

			public int IntProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
				[MethodImpl(MethodImplOptions.NoInlining)] set;
			}

			public string StringProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
				[MethodImpl(MethodImplOptions.NoInlining)] set;
			}

			public bool BooleanProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
				[MethodImpl(MethodImplOptions.NoInlining)] set;
			}

			public TestClass2 WrapperProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
				[MethodImpl(MethodImplOptions.NoInlining)] set;
			}

			public TestEnum EnumProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
			}

			public Version VersionProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
			}

			public long LongProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
			}
		}

		public class TestEventClass
		{
			public event TestEventHandler TestEvent;

			public void Fire()
			{
				TestEvent?.Invoke(null, this);
			}
		}

		public delegate void TestEventHandler(object sender, TestEventClass e);

		public enum TestEnum
		{
			One   = 1,
			Two   = 2,
			Three = 3
		}
	}

	namespace Wrapped
	{
		public static class Helper
		{
			public static TypeMapper CreateTypeMapper()
			{
				var typeMapper = new TypeMapper();

				typeMapper.RegisterTypeWrapper<TestClass>(typeof(Original.TestClass));
				typeMapper.RegisterTypeWrapper<TestEventHandler>(typeof(Original.TestEventHandler));
				typeMapper.RegisterTypeWrapper<TestEventClass>(typeof(Original.TestEventClass));
				typeMapper.RegisterTypeWrapper<TestClass2>(typeof(Original.TestClass2));
				typeMapper.RegisterTypeWrapper<TestEnum>(typeof(Original.TestEnum));

				typeMapper.FinalizeMappings();

				return typeMapper;
			}
		}

		[Wrapper]
		public class TestClass
		{
			public TestEnum EnumProperty { get; set; }
			public decimal DecimalProperty { get; }
			public bool BooleanProperty { get; }
			public int IntProperty { get; }
			public string StringProperty { get; set; }
			public SqlDbType KnownEnumProperty { get; set; }

			public DataTable GetOleDbSchemaTable(Guid schema, object[] restrictions) => throw new NotImplementedException();
			public static void ClearAllPools() => throw new NotImplementedException();
		}

		[Wrapper]
		public delegate void TestEventHandler(object sender, TestEventClass e);

		[Wrapper]
		public class TestEventClass : TypeWrapper
		{
			public TestEventClass(object instance, TypeMapper mapper) : base(instance, mapper, null)
			{
				this.WrapEvent<TestEventClass, TestEventHandler>(nameof(TestEvent));
			}

			public event TestEventHandler TestEvent
			{
				add => Events.AddHandler(nameof(TestEvent), value);
				remove => Events.RemoveHandler(nameof(TestEvent), value);
			}
		}

		[Wrapper]
		public class TestClass2 : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: QuoteIdentifier
				(Expression<Func<TestClass2, string, string>>)((TestClass2 this_, string identifier) => this_.QuoteIdentifier(identifier)),
				// [1]: Add
				(Expression<Func<TestClass2, TestClass2, TestClass2>>)((TestClass2 this_, TestClass2 item) => this_.Add(item)),
				// [2]: GetEnumerator
				(Expression<Func<TestClass2, IEnumerable>>)((TestClass2 this_) => this_.GetEnumerator()),
				// [3]: CreateDatabase
				(Expression<Action<TestClass2>>)((TestClass2 this_) => this_.CreateDatabase()),
				// [4]: Dispose
				(Expression<Action<TestClass2>>)((TestClass2 this_) => this_.Dispose()),
				// [5]: WriteToServer
				(Expression<Action<TestClass2, IDataReader>>)((TestClass2 this_, IDataReader rd) => this_.WriteToServer(rd)),
				// [6]: get IntProperty
				(Expression<Func<TestClass2, int>>)((TestClass2 this_) => this_.IntProperty),
				// [7]: get StringProperty
				(Expression<Func<TestClass2, string>>)((TestClass2 this_) => this_.StringProperty),
				// [8]: get BooleanProperty
				(Expression<Func<TestClass2, bool>>)((TestClass2 this_) => this_.BooleanProperty),
				// [9]: get WrapperProperty
				(Expression<Func<TestClass2, TestClass2>>)((TestClass2 this_) => this_.WrapperProperty),
				// [10]: get EnumProperty
				(Expression<Func<TestClass2, TestEnum>>)((TestClass2 this_) => this_.EnumProperty),
				// [11]: get VersionProperty
				(Expression<Func<TestClass2, Version>>)((TestClass2 this_) => this_.VersionProperty),
				// [12]: get LongProperty
				(Expression<Func<TestClass2, long>>)((TestClass2 this_) => this_.LongProperty),
				// [13]: set IntProperty
				PropertySetter((TestClass2 this_) => this_.IntProperty),
				// [14]: set StringProperty
				PropertySetter((TestClass2 this_) => this_.StringProperty),
				// [15]: set BooleanProperty
				PropertySetter((TestClass2 this_) => this_.BooleanProperty),
				// [16]: set WrapperProperty
				PropertySetter((TestClass2 this_) => this_.WrapperProperty),
			};

			public TestClass2(object instance, TypeMapper mapper, Delegate[] wrappers) : base(instance, mapper, wrappers)
			{
			}

			public TestClass2() => throw new NotImplementedException();

			public TestClass2(string connectionString) => throw new NotImplementedException();

			public TestClass2(TimeSpan timeSpan) => throw new NotImplementedException();

			public TestClass2(int p1, string p2) => throw new NotImplementedException();

			public TestClass2(string p1, string p2) => throw new NotImplementedException();

			public TestClass2(TestClass2 p1, TestEnum p2, TestClass p3) => throw new NotImplementedException();

			public TestClass2(TestClass2 p1, TestEnum p2) => throw new NotImplementedException();

			public TestClass2(TestClass2 p1, string p2) => throw new NotImplementedException();

			public TestClass2(int year, int month, int day, int hour, int minute, int second, int nanosecond, string timeZone) => throw new NotImplementedException();

			public void CreateDatabase() => ((Action<TestClass2>)CompiledWrappers[3])(this);

			public void Dispose() => ((Action<TestClass2>)CompiledWrappers[4])(this);

			public string QuoteIdentifier(string identifier) => ((Func<TestClass2, string, string>)CompiledWrappers[0])(this, identifier);

			public void WriteToServer(IDataReader rd) => ((Action<TestClass2, IDataReader>)CompiledWrappers[5])(this, rd);

			public TestClass2 Add(TestClass2 p) => ((Func<TestClass2, TestClass2, TestClass2>)CompiledWrappers[1])(this, p);

			public IEnumerable GetEnumerator() => ((Func<TestClass2, IEnumerable>)CompiledWrappers[2])(this);

			public int IntProperty
			{
				get => ((Func<TestClass2, int>)CompiledWrappers[6])(this);
				set => ((Action<TestClass2, int>)CompiledWrappers[13])(this, value);
			}

			public string StringProperty
			{
				get => ((Func<TestClass2, string>)CompiledWrappers[7])(this);
				set => ((Action<TestClass2, string>)CompiledWrappers[14])(this, value);
			}

			public bool BooleanProperty
			{
				get => ((Func<TestClass2, bool>)CompiledWrappers[8])(this);
				set => ((Action<TestClass2, bool>)CompiledWrappers[15])(this, value);
			}

			public TestClass2 WrapperProperty
			{
				get => ((Func<TestClass2, TestClass2>)CompiledWrappers[9])(this);
				set => ((Action<TestClass2, TestClass2>)CompiledWrappers[16])(this, value);
			}

			public TestEnum EnumProperty => ((Func<TestClass2, TestEnum>) CompiledWrappers[10])(this);

			public Version VersionProperty => ((Func<TestClass2, Version>)CompiledWrappers[11])(this);

			public long LongProperty => ((Func<TestClass2, long>)CompiledWrappers[12])(this);
		}

		[Wrapper]
		public enum TestEnum
		{
			One   = 3,
			Two   = 2,
			Three = 1
		}
	}
}
