using Microsoft.CodeAnalysis;

namespace CodeGenerators
{
	public partial class BuildersGenerator : IIncrementalGenerator
	{
		enum BuilderKind { Any, Expr, AnyCall, Call }

		sealed record BuilderNode(
			string Builder,
			string Key,
			BuilderKind Kind,
			string Check
		);
	}
}
