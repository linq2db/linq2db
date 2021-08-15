using System.Collections.Generic;
using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.CodeGeneration
{
	public sealed class CodeIdentifierComparer : IComparer<CodeIdentifier[]>
	{
		// TODO: language-specific
		public static readonly IComparer<CodeIdentifier[]> Instance = new CodeIdentifierComparer();

		private CodeIdentifierComparer()
		{
		}

		int IComparer<CodeIdentifier[]>.Compare(CodeIdentifier[] x, CodeIdentifier[] y)
		{
			for (var i = 0; i < x.Length; i++)
			{
				if (i == y.Length)
					return 1;

				var cmp = string.CompareOrdinal(x[i].Name, y[i].Name);
				if (cmp == 0)
					continue;
				return cmp;
			}

			if (x.Length < y.Length)
				return -1;

			return 0;
		}
	}
}
