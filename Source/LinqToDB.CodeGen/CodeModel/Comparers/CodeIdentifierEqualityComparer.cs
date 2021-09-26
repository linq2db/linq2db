using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Identifier equality comparer.
	/// </summary>
	internal class CodeIdentifierEqualityComparer : IEqualityComparer<CodeIdentifier>
	{
		private readonly StringComparer _comparer;

		public CodeIdentifierEqualityComparer(StringComparer comparer)
		{
			_comparer = comparer;
		}

		bool IEqualityComparer<CodeIdentifier>.Equals     (CodeIdentifier x, CodeIdentifier y) => x == y || _comparer.Equals(x.Name, y.Name);
		int  IEqualityComparer<CodeIdentifier>.GetHashCode(CodeIdentifier obj                ) => _comparer.GetHashCode(obj.Name);
	}
}
