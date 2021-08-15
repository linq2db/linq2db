using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Identifier comparer.
	/// </summary>
	internal class CodeIdentifierComparer : IEqualityComparer<CodeIdentifier>
	{
		private readonly StringComparer _comparer;

		public CodeIdentifierComparer(StringComparer comparer)
		{
			_comparer = comparer;
		}

		bool IEqualityComparer<CodeIdentifier>.Equals     (CodeIdentifier x, CodeIdentifier y) => _comparer.Equals(x.Name, y.Name);
		int  IEqualityComparer<CodeIdentifier>.GetHashCode(CodeIdentifier obj                ) => _comparer.GetHashCode(obj.Name);
	}
}
