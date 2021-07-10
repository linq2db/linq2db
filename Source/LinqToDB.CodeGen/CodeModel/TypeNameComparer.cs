using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	/// <summary>
	/// Cannot be used with unnamed types (e.g. arrays)
	/// </summary>
	public class TypeNameComparer : IEqualityComparer<IType>
	{
		private readonly IEqualityComparer<string> _nameComparer;

		public TypeNameComparer(IEqualityComparer<string> nameComparer)
		{
			_nameComparer = nameComparer;
		}

		bool IEqualityComparer<IType>.Equals(IType x, IType y)
		{
			using var xe = EnumerateParts(x).GetEnumerator();
			using var ye = EnumerateParts(x).GetEnumerator();

			var xMoved = true;
			var yMoved = true;

			// bitwise & used intentionally to invoke both iterators
			while ((xMoved = xe.MoveNext()) & (yMoved = ye.MoveNext()))
			{
				var equal = _nameComparer.Equals(xe.Current, ye.Current);
				if (!equal)
					return false;
			}

			return xMoved == yMoved;
		}

		int IEqualityComparer<IType>.GetHashCode(IType obj)
		{
			var hash = 0;
			foreach (var part in EnumerateParts(obj))
				hash ^= _nameComparer.GetHashCode(part);
			return hash;
		}

		private IEnumerable<string> EnumerateParts(IType type)
		{
			if (type.Namespace != null)
			{
				foreach (var ns in type.Namespace)
					yield return ns.Name;
			}
			else if (type.Parent != null)
				foreach (var part in EnumerateParts(type.Parent))
					yield return part;

			yield return type.Name!.Name;
		}
	}
}
