using System;

using Microsoft.CodeAnalysis;

namespace CodeGenerators
{
	public partial class BuildersGenerator : IIncrementalGenerator
	{
		enum BuilderKind { Any, Expr, AnyCall, Call }

		[Flags]
		enum CallParams { None = 0, Call = 1, Info = 2, Builder = 4 }

		sealed record BuilderNode(
			string Builder,
			string Key,
			BuilderKind Kind,
			string Check,
			CallParams Params
		);
	}
}
