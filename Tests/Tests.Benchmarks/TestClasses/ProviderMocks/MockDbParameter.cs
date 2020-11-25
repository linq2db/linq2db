using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Benchmarks.TestProvider
{
	public class MockDbParameter : DbParameter
	{
		public MockDbParameter()
		{ 
		}

		public MockDbParameter(string name, object? value)
		{
			ParameterName = name;
			Value = value;
		}

		[AllowNull]
		public override string  ParameterName { get; set; } = null!;
		public override DbType  DbType        { get; set; }
		public override object? Value         { get; set; }

		public override ParameterDirection Direction { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public override bool IsNullable { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public override int Size { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		[AllowNull]
		public override string SourceColumn { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public override bool SourceColumnNullMapping { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public override void ResetDbType()
		{
			throw new NotImplementedException();
		}
	}
}
