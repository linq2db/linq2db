using System;
using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Identifier comparer/equality comparer for names and name sequences (e.g. namespaces).
	/// </summary>
	internal sealed class CodeIdentifierComparer
		: IComparer<CodeIdentifier>, IComparer<IEnumerable<CodeIdentifier>>,
		IEqualityComparer<CodeIdentifier>, IEqualityComparer<IEnumerable<CodeIdentifier>>
	{
		private readonly StringComparer _comparer;

		public CodeIdentifierComparer(StringComparer comparer)
		{
			_comparer = comparer;
		}

		int IComparer<CodeIdentifier>.Compare(CodeIdentifier? x, CodeIdentifier? y) => x == y
			? 0
			: x == null
				? - 1
				: y == null
					? 1
					: _comparer.Compare(x.Name, y.Name);

		int IComparer<IEnumerable<CodeIdentifier>>.Compare(IEnumerable<CodeIdentifier>? x, IEnumerable<CodeIdentifier>? y)
		{
			if (x == y   ) return  0;
			if (x == null) return -1;
			if (y == null) return  1;

			using var xe = x.GetEnumerator();
			using var ye = y.GetEnumerator();

			var xAdvanced = true;
			var yAdvanced = true;

			// bitwise & used intentionally to invoke both iterators
			while ((xAdvanced = xe.MoveNext()) & (yAdvanced = ye.MoveNext()))
			{
				var equal = _comparer.Compare(xe.Current.Name, ye.Current.Name);
				if (equal != 0)
					return equal;
			}

			if (xAdvanced != yAdvanced)
				return xAdvanced ? 1 : -1;

			return 0;
		}

		bool IEqualityComparer<CodeIdentifier>.Equals     (CodeIdentifier? x, CodeIdentifier? y) => x == y || (x != null && y != null && _comparer.Equals(x.Name, y.Name));
		int  IEqualityComparer<CodeIdentifier>.GetHashCode(CodeIdentifier obj                  ) => _comparer.GetHashCode(obj.Name);

		bool IEqualityComparer<IEnumerable<CodeIdentifier>>.Equals     (IEnumerable<CodeIdentifier>? x, IEnumerable<CodeIdentifier>? y)
		{
			if (x == y                ) return true;
			if (x == null || y == null) return false;

			using var xe = x.GetEnumerator();
			using var ye = y.GetEnumerator();

			var xAdvanced = true;
			var yAdvanced = true;

			// bitwise & used intentionally to invoke both iterators
			while ((xAdvanced = xe.MoveNext()) & (yAdvanced = ye.MoveNext()))
			{
				var equal = _comparer.Equals(xe.Current.Name, ye.Current.Name);
				if (!equal)
					return false;
			}

			return xAdvanced == yAdvanced;
		}

		int  IEqualityComparer<IEnumerable<CodeIdentifier>>.GetHashCode(IEnumerable<CodeIdentifier> obj)
		{
			var hash = 0;

			foreach (var name in obj)
				hash ^= _comparer.GetHashCode(name.Name);

			return hash;
		}
	}
}
