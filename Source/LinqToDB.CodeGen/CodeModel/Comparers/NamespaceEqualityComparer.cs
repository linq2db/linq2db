using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Identifier equality comparer for namespaces (identifier sequences).
	/// </summary>
	internal class NamespaceEqualityComparer : IEqualityComparer<IEnumerable<CodeIdentifier>>
	{
		private readonly IEqualityComparer<CodeIdentifier> _comparer;

		public NamespaceEqualityComparer(IEqualityComparer<CodeIdentifier> comparer)
		{
			_comparer = comparer;
		}

		bool IEqualityComparer<IEnumerable<CodeIdentifier>>.Equals(IEnumerable<CodeIdentifier> x, IEnumerable<CodeIdentifier> y)
		{
			if (x == y) return true;

			using var xe = x.GetEnumerator();
			using var ye = y.GetEnumerator();

			var xAdvanced = true;
			var yAdvanced = true;

			// bitwise & used intentionally to invoke both iterators
			while ((xAdvanced = xe.MoveNext()) & (yAdvanced = ye.MoveNext()))
			{
				var equal = _comparer.Equals(xe.Current, ye.Current);
				if (!equal)
					return false;
			}

			return xAdvanced == yAdvanced;
		}

		int IEqualityComparer<IEnumerable<CodeIdentifier>>.GetHashCode(IEnumerable<CodeIdentifier> obj)
		{
			var hash = 0;

			foreach (var name in obj)
				hash ^= _comparer.GetHashCode(name);

			return hash;
		}
	}
}
