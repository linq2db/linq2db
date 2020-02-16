using System;
using System.Collections;
using System.Data;
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

			public event TestEventHandler TestEvent;

			public void Fire()
			{
				TestEvent?.Invoke(null, this);
			}
		}

		public delegate void TestEventHandler(object sender, TestClass2 e);

		public enum TestEnum
		{
			One   = 1,
			Two   = 2,
			Three = 3
		}
	}

	namespace Wrapped
	{
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
		public delegate void TestEventHandler(object sender, TestClass2 e);

		[Wrapper]
		public class TestClass2 : TypeWrapper, IDisposable
		{
			public TestClass2(object instance, TypeMapper mapper) : base(instance, mapper)
			{
				this.WrapEvent<TestClass2, TestEventHandler>(nameof(TestEvent));
			}

			public event TestEventHandler TestEvent
			{
				add => Events.AddHandler(nameof(TestEvent), value);
				remove => Events.RemoveHandler(nameof(TestEvent), value);
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

			public void CreateDatabase() => this.WrapAction(t => t.CreateDatabase());

			public void Dispose() => this.WrapAction(t => ((IDisposable)t).Dispose());

			public string QuoteIdentifier(string identitier) => this.Wrap(t => t.QuoteIdentifier(identitier));

			public void WriteToServer(IDataReader rd) => this.WrapAction(t => t.WriteToServer(rd));

			public TestClass2 Add(TestClass2 p) => this.Wrap(t => t.Add(p));

			public IEnumerable GetEnumerator() => this.Wrap(t => t.GetEnumerator());

			public int IntProperty
			{
				get => this.Wrap(t => t.IntProperty);
				set => this.SetPropValue(t => t.IntProperty, value);
			}

			public string StringProperty
			{
				get => this.Wrap(t => t.StringProperty);
				set => this.SetPropValue(t => t.StringProperty, value);
			}

			public bool BooleanProperty
			{
				get => this.Wrap(t => t.BooleanProperty);
				set => this.SetPropValue(t => t.BooleanProperty, value);
			}

			public TestClass2 WrapperProperty
			{
				get => this.Wrap(t => t.WrapperProperty);
				set => this.SetPropValue(t => t.WrapperProperty, value);
			}

			public TestEnum EnumProperty => this.Wrap(t => t.EnumProperty);

			public Version VersionProperty => this.Wrap(t => t.VersionProperty);

			public long LongProperty => this.Wrap(t => t.LongProperty);
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
