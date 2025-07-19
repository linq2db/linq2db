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
		// use NoInlining used to prevent JIT cheating with direct calls. We are not interested in such results
		public class TestClass : ITestClass
		{
			private static readonly DataTable _GetOleDbSchemaTableResult = new DataTable();

			public TestEnum EnumProperty 
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
				[MethodImpl(MethodImplOptions.NoInlining)] set;
			}

			public string? StringProperty
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
			public DataTable GetOleDbSchemaTable(Guid schema, object?[]? restrictions) => _GetOleDbSchemaTableResult;

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

			public string? StringProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
				[MethodImpl(MethodImplOptions.NoInlining)] set;
			}

			public bool BooleanProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
				[MethodImpl(MethodImplOptions.NoInlining)] set;
			}

			public TestClass2? WrapperProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
				[MethodImpl(MethodImplOptions.NoInlining)] set;
			}

			public TestEnum EnumProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
			}

			public Version? VersionProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
			}

			public long LongProperty
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get;
			}

			[MethodImpl(MethodImplOptions.NoInlining)] public TestEnum  TestEnumConvert (TestEnum value ) => value;
			[MethodImpl(MethodImplOptions.NoInlining)] public TestEnum2 TestEnum2Convert(TestEnum2 value) => value;
			[MethodImpl(MethodImplOptions.NoInlining)] public TestEnum3 TestEnum3Convert(TestEnum3 value) => value;
		}

		public class TestEventClass
		{
			public event TestEventHandler? TestEvent;

			public void Fire()
			{
				using var obj = new TestClass2();
				TestEvent?.Invoke(null, obj);
			}
		}

		public delegate void TestEventHandler(object? sender, TestClass2 e);

		public enum TestEnum
		{
			One   = 1,
			Two   = 2,
			Three = 3
		}

		public enum TestEnum2
		{
			One   = 1,
			Two   = 2,
			Three = 3
		}

		[Flags]
		public enum TestEnum3
		{
			One  = 1,
			Two  = 2,
			Four = 4
		}
	}

	namespace Wrapped
	{
		public static class Helper
		{
			public static TypeMapper CreateTypeMapper()
			{
				var typeMapper = new TypeMapper();

				typeMapper.RegisterTypeWrapper<TestClass       >(typeof(Original.TestClass));
				typeMapper.RegisterTypeWrapper<TestEventHandler>(typeof(Original.TestEventHandler));
				typeMapper.RegisterTypeWrapper<TestEventClass  >(typeof(Original.TestEventClass));
				typeMapper.RegisterTypeWrapper<TestClass2      >(typeof(Original.TestClass2));
				typeMapper.RegisterTypeWrapper<TestEnum        >(typeof(Original.TestEnum));
				typeMapper.RegisterTypeWrapper<TestEnum2       >(typeof(Original.TestEnum2));
				typeMapper.RegisterTypeWrapper<TestEnum3       >(typeof(Original.TestEnum3));

				typeMapper.FinalizeMappings();

				return typeMapper;
			}
		}

		[Wrapper]
		public class TestClass
		{
			public TestEnum  EnumProperty      { get; set; }
			public decimal   DecimalProperty   { get; }
			public bool      BooleanProperty   { get; }
			public int       IntProperty       { get; }
			public string?   StringProperty    { get; set; }
			public SqlDbType KnownEnumProperty { get; set; }

			public DataTable GetOleDbSchemaTable(Guid schema, object[] restrictions) => throw new NotImplementedException();

			public static void ClearAllPools() => throw new NotImplementedException();
		}

		[Wrapper]
		public delegate void TestEventHandler(object sender, TestClass2 e);

		[Wrapper]
		public class TestEventClass : TypeWrapper
		{
			private static string[] Events { get; }
				= new []
			{
				nameof(TestEvent)
			};

			public TestEventClass(object instance) : base(instance, null)
			{
			}

			private      TestEventHandler? _TestEvent;
			public event TestEventHandler?  TestEvent
			{
				add    => _TestEvent = (TestEventHandler?)Delegate.Combine(_TestEvent, value);
				remove => _TestEvent = (TestEventHandler?)Delegate.Remove (_TestEvent, value);
			}
		}

		[Wrapper]
		public class TestClass2 : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: QuoteIdentifier
				(Expression<Func<TestClass2, string, string>>)((this_, identifier) => this_.QuoteIdentifier(identifier)),
				// [1]: Add
				(Expression<Func<TestClass2, TestClass2, TestClass2>>)((this_, item) => this_.Add(item)),
				// [2]: GetEnumerator
				(Expression<Func<TestClass2, IEnumerable>>)(this_ => this_.GetEnumerator()),
				// [3]: CreateDatabase
				(Expression<Action<TestClass2>>)(this_ => this_.CreateDatabase()),
				// [4]: Dispose
				(Expression<Action<TestClass2>>)(this_ => this_.Dispose()),
				// [5]: WriteToServer
				(Expression<Action<TestClass2, IDataReader>>)((this_, rd) => this_.WriteToServer(rd)),
				// [6]: get IntProperty
				(Expression<Func<TestClass2, int>>)(this_ => this_.IntProperty),
				// [7]: get StringProperty
				(Expression<Func<TestClass2, string?>>)(this_ => this_.StringProperty),
				// [8]: get BooleanProperty
				(Expression<Func<TestClass2, bool>>)(this_ => this_.BooleanProperty),
				// [9]: get WrapperProperty
				(Expression<Func<TestClass2, TestClass2?>>)(this_ => this_.WrapperProperty),
				// [10]: get EnumProperty
				(Expression<Func<TestClass2, TestEnum>>)(this_ => this_.EnumProperty),
				// [11]: get VersionProperty
				(Expression<Func<TestClass2, Version?>>)(this_ => this_.VersionProperty),
				// [12]: get LongProperty
				(Expression<Func<TestClass2, long>>)(this_ => this_.LongProperty),
				// [13]: set IntProperty
				PropertySetter((TestClass2 this_) => this_.IntProperty),
				// [14]: set StringProperty
				PropertySetter((TestClass2 this_) => this_.StringProperty),
				// [15]: set BooleanProperty
				PropertySetter((TestClass2 this_) => this_.BooleanProperty),
				// [16]: set WrapperProperty
				PropertySetter((TestClass2 this_) => this_.WrapperProperty),
				// [17]: TestEnumConvert
				(Expression<Func<TestClass2, TestEnum, TestEnum>>)((this_, value) => this_.TestEnumConvert(value)),
				// [18]: TestEnum2Convert
				(Expression<Func<TestClass2, TestEnum2, TestEnum2>>)((this_, value) => this_.TestEnum2Convert(value)),
				// [19]: TestEnum3Convert
				(Expression<Func<TestClass2, TestEnum3, TestEnum3>>)((this_, value) => this_.TestEnum3Convert(value)),
			};

			public TestClass2(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public TestClass2()                                                                                                => throw new NotImplementedException();
			public TestClass2(string connectionString)                                                                         => throw new NotImplementedException();
			public TestClass2(TimeSpan timeSpan)                                                                               => throw new NotImplementedException();
			public TestClass2(int p1, string p2)                                                                               => throw new NotImplementedException();
			public TestClass2(string p1, string p2)                                                                            => throw new NotImplementedException();
			public TestClass2(TestClass2 p1, TestEnum p2, TestClass p3)                                                        => throw new NotImplementedException();
			public TestClass2(TestClass2 p1, TestEnum p2)                                                                      => throw new NotImplementedException();
			public TestClass2(TestClass2 p1, string p2)                                                                        => throw new NotImplementedException();
			public TestClass2(int year, int month, int day, int hour, int minute, int second, int nanosecond, string timeZone) => throw new NotImplementedException();

			public void        CreateDatabase()                   => ((Action<TestClass2>                      )CompiledWrappers[3])(this);
			public void        Dispose()                          => ((Action<TestClass2>                      )CompiledWrappers[4])(this);
			public string      QuoteIdentifier(string identifier) => ((Func<TestClass2, string, string>        )CompiledWrappers[0])(this, identifier);
#pragma warning disable RS0030 // API mapping must preserve type
			public void        WriteToServer(IDataReader rd)      => ((Action<TestClass2, IDataReader>         )CompiledWrappers[5])(this, rd);
#pragma warning restore RS0030 //  API mapping must preserve type
			public TestClass2  Add(TestClass2 p)                  => ((Func<TestClass2, TestClass2, TestClass2>)CompiledWrappers[1])(this, p);
			public IEnumerable GetEnumerator()                    => ((Func<TestClass2, IEnumerable>           )CompiledWrappers[2])(this);

			public int IntProperty
			{
				get => ((Func<TestClass2, int>)  CompiledWrappers[6])(this);
				set => ((Action<TestClass2, int>)CompiledWrappers[13])(this, value);
			}

			public string? StringProperty
			{
				get => ((Func<TestClass2, string?>)  CompiledWrappers[7])(this);
				set => ((Action<TestClass2, string?>)CompiledWrappers[14])(this, value);
			}

			public bool BooleanProperty
			{
				get => ((Func<TestClass2, bool>)  CompiledWrappers[8])(this);
				set => ((Action<TestClass2, bool>)CompiledWrappers[15])(this, value);
			}

			public TestClass2? WrapperProperty
			{
				get => ((Func<TestClass2, TestClass2?>)  CompiledWrappers[9])(this);
				set => ((Action<TestClass2, TestClass2?>)CompiledWrappers[16])(this, value);
			}

			public TestEnum EnumProperty    => ((Func<TestClass2, TestEnum>)CompiledWrappers[10])(this);
			public Version? VersionProperty => ((Func<TestClass2, Version?>)CompiledWrappers[11])(this);
			public long     LongProperty    => ((Func<TestClass2, long>    )CompiledWrappers[12])(this);

			public TestEnum  TestEnumConvert (TestEnum  value) => ((Func<TestClass2, TestEnum , TestEnum >)CompiledWrappers[17])(this, value);
			public TestEnum2 TestEnum2Convert(TestEnum2 value) => ((Func<TestClass2, TestEnum2, TestEnum2>)CompiledWrappers[18])(this, value);
			public TestEnum3 TestEnum3Convert(TestEnum3 value) => ((Func<TestClass2, TestEnum3, TestEnum3>)CompiledWrappers[19])(this, value);
		}

		[Wrapper]
		public enum TestEnum
		{
			One   = 3,
			Two   = 2,
			Three = 1,
			Four  = 4
		}

		[Wrapper]
		public enum TestEnum2
		{
			One   = 1,
			Two   = 2,
			Three = 3,
			Four  = 4
		}

		[Wrapper]
		[Flags]
		public enum TestEnum3
		{
			One   = 1,
			Two   = 2,
			Four  = 4,
			Eight = 8
		}
	}
}
