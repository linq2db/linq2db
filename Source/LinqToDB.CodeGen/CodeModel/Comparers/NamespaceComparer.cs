using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Identifier comparer for namespaces (identifier sequences).
	/// </summary>
	internal class NamespaceComparer : IComparer<IEnumerable<CodeIdentifier>>
	{
		private readonly IComparer<CodeIdentifier> _comparer;

		public NamespaceComparer(IComparer<CodeIdentifier> comparer)
		{
			_comparer = comparer;
		}

		int IComparer<IEnumerable<CodeIdentifier>>.Compare(IEnumerable<CodeIdentifier> x, IEnumerable<CodeIdentifier> y)
		{
			if (x == y) return 0;

			using var xe = x.GetEnumerator();
			using var ye = y.GetEnumerator();

			var xAdvanced = true;
			var yAdvanced = true;

			// bitwise & used intentionally to invoke both iterators
			while ((xAdvanced = xe.MoveNext()) & (yAdvanced = ye.MoveNext()))
			{
				var equal = _comparer.Compare(xe.Current, ye.Current);
				if (equal != 0)
					return equal;
			}

			if (xAdvanced != yAdvanced)
				return xAdvanced ? 1 : -1;

			return 0;
		}
	}
}
