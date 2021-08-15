using System;

namespace LinqToDB.CodeGen.Metadata
{
	[Flags]
	public enum SchemaObjects
	{
		Table = 1,
		View = 2,
		StoredProcedure = 4,
		TableFunction = 8,
		ScalarFunction = 16,
		AggregateFunction = 32,
		ForeignKey = 64
	}
}
