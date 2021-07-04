using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public class NamespaceEqualityComparer : IEqualityComparer<CodeIdentifier[]>
	{
		private readonly IEqualityComparer<string> _identifierComparer;
		public NamespaceEqualityComparer(IEqualityComparer<string> identifierComparer)
		{
			_identifierComparer = identifierComparer;
		}

		bool IEqualityComparer<CodeIdentifier[]>.Equals(CodeIdentifier[] x, CodeIdentifier[] y)
		{
			if (x.Length != y.Length)
				return false;

			for (var i = 0; i < x.Length; i++)
				if (!_identifierComparer.Equals(x[i].Name, y[i].Name))
					return false;

			return true;
		}

		int IEqualityComparer<CodeIdentifier[]>.GetHashCode(CodeIdentifier[] obj)
		{
			var hash = obj.Length.GetHashCode();

			foreach (var name in obj)
				hash ^= _identifierComparer.GetHashCode(name.Name);

			return hash;
		}
	}
}
