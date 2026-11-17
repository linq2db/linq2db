using System;
using System.Diagnostics.CodeAnalysis;

using DotGram;

// The generated parser keeps the working state of one parse call in [ThreadStatic] fields; see ReadOnlySqlGrammar.
[assembly: SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Scope = "type", Target = "~T:Tests.LinqToDB.CLI.ShallowReadOnlySqlGrammar", Justification = "Generated parser: per-thread working state of a single parse call.")]

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// The CLI's read-only SQL grammar, compiled with a single extra stack so that a test can run a reading out of stacks.
	/// </summary>
	[Gram("ReadOnlySql.gram", Lexical = false, MaxExtraStacks = 1)]
	internal static partial class ShallowReadOnlySqlGrammar;
}
