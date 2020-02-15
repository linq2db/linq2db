using System;
using System.Data;
using LinqToDB.Expressions;

namespace LinqToDB.Benchmarks.TypeMapping
{
	public interface ITestClass
	{
	}

	namespace Mapped
	{
		public class TestClass : ITestClass
		{
			private static readonly DataTable _GetOleDbSchemaTableResult = new DataTable();

			public TestEnum EnumProperty { get; set; }

			public DataTable GetOleDbSchemaTable(Guid schema, object[] restrictions) => _GetOleDbSchemaTableResult;
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
