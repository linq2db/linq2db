using System;
using System.Data;
using System.Runtime.CompilerServices;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	public interface ITestClass
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

			[MethodImpl(MethodImplOptions.NoInlining)]
			public DataTable GetOleDbSchemaTable(Guid schema, object[] restrictions) => _GetOleDbSchemaTableResult;

			[MethodImpl(MethodImplOptions.NoInlining)]
			public static void ClearAllPools() { }
		}

		public class TestClass2 : IDisposable
		{
			[MethodImpl(MethodImplOptions.NoInlining)]
			public TestClass2(string connectionString)
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
		}

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

			public DataTable GetOleDbSchemaTable(Guid schema, object[] restrictions) => throw new NotImplementedException();
			public static void ClearAllPools() => throw new NotImplementedException();
		}

		[Wrapper]
		public class TestClass2 : TypeWrapper, IDisposable
		{
			public TestClass2(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public TestClass2(string connectionString) => throw new NotImplementedException();

			public void CreateDatabase() => this.WrapAction(t => t.CreateDatabase());
			public void Dispose() => this.WrapAction(t => t.Dispose());
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
