using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGenerators
{
	public partial class BuildersGenerator : IIncrementalGenerator
	{
		enum BuilderKind { Any, Expr, AnyCall, Call }

		record BuilderNode(
			string Builder,
			string Key,
			BuilderKind Kind,
			string Check
		);
	}
}
