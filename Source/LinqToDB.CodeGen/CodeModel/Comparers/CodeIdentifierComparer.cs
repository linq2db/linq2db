using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Identifier comparer.
	/// </summary>
	internal class CodeIdentifierComparer : IComparer<CodeIdentifier>
	{
		private readonly StringComparer _comparer;

		public CodeIdentifierComparer(StringComparer comparer)
		{
			_comparer = comparer;
		}

		int IComparer<CodeIdentifier>.Compare(CodeIdentifier x, CodeIdentifier y) => x == y ? 0 : _comparer.Compare(x.Name, y.Name);
	}
}
