using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeIdentifierComparer : IEqualityComparer<CodeIdentifier>
	{
		public static readonly IEqualityComparer<CodeIdentifier> Instance = new CodeIdentifierComparer();

		private CodeIdentifierComparer()
		{
		}

		bool IEqualityComparer<CodeIdentifier>.Equals(CodeIdentifier x, CodeIdentifier y)
		{
			return x.Name == y.Name;
		}

		int IEqualityComparer<CodeIdentifier>.GetHashCode(CodeIdentifier obj)
		{
			return obj.Name.GetHashCode();
		}
	}
}
