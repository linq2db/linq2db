using System;

namespace LinqToDB.Internal.Metadata
{
	[Flags]
	enum AiAffects
	{
		None                     = 0,
		DmlStatement             = 1 << 0,
		DdlStatement             = 1 << 1,
		QueryRoot                = 1 << 2,
		QueryStructure           = 1 << 3,
		QueryCompilation         = 1 << 4,
		JoinGraph                = 1 << 5,
		SqlSemantics             = 1 << 6,
		CommandBuilder           = 1 << 7,
		Data                     = 1 << 8,
		QueryResult              = 1 << 9,
		ExecutionContext         = 1 << 10,
		ConnectionConfiguration  = 1 << 11,
		Configuration            = 1 << 12,
		SchemaResult             = 1 << 13,
		GeneratedSql             = 1 << 14,
	}
}
