using System;
using System.Diagnostics.CodeAnalysis;

using DotGram;

// The parser generated from ReadOnlySql.gram keeps the working state of one parse call in [ThreadStatic] fields, so
// concurrent checks on different threads never share it and no call has to allocate it afresh. It is not application
// state: nothing in it outlives the call that uses it.
[assembly: SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Scope = "type", Target = "~T:LinqToDB.CommandLine.Commands.QueryExecution.ReadOnlySqlGrammar", Justification = "Generated parser: per-thread working state of a single parse call.")]

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Parsers of the read-only and single-statement SQL checks, generated from <c>ReadOnlySql.gram</c>.
	/// </summary>
	/// <remarks>
	/// <c>Lexical = false</c> is deliberate: the dialect conditions inside terminals and trivia need the parser to read
	/// characters. <c>true</c> would only make the generator fall back to characters with a warning.
	/// </remarks>
	[Gram("ReadOnlySql.gram", Lexical = false, MaxExtraStacks = 64)]
	internal static partial class ReadOnlySqlGrammar;
}
