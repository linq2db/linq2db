using System;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Target languages a scaffold option applies to. Used to render per-option "Supported in:" help and to
	/// reject options that are not applicable to the selected <c>--target-language</c>.
	/// </summary>
	[Flags]
	internal enum TargetLanguages
	{
		/// <summary>
		/// Option applies to C# code generation.
		/// </summary>
		CSharp = 1,
		/// <summary>
		/// Option applies to F# code generation.
		/// </summary>
		FSharp = 2,
		/// <summary>
		/// Option applies to every target language (default).
		/// </summary>
		All    = CSharp | FSharp,
	}
}
