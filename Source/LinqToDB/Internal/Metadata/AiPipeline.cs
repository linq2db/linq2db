using System;

namespace LinqToDB.Internal.Metadata
{
	[Flags]
	enum AiPipeline
	{
		None            = 0,
		ExpressionTree  = 1 << 0,
		SqlAST          = 1 << 1,
		SqlText         = 1 << 2,
		Connection      = 1 << 3,
		Execution       = 1 << 4,
		BulkInsert      = 1 << 5,
	}
}
