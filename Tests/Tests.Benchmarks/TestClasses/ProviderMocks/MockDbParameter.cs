using System;
using System.Data;
using System.Data.Common;

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
		public override string? ParameterName { get; set; }
		public override DbType  DbType        { get; set; }
		public override object? Value         { get; set; }

		public override ParameterDirection Direction { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public override bool IsNullable { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public override int Size
		{
			get => 100;
			set
			{
			}
		}

		public override string SourceColumn { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		public override bool SourceColumnNullMapping { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public override void ResetDbType()
		{
			throw new NotImplementedException();
		}
	}
}
