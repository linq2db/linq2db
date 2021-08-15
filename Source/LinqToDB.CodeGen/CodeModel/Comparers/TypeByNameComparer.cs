using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Compare types by name (including namespace and parent type names) e.g. in name conflict resolution logic.
	/// Should be used only for types that are visible in global namespace like regular and generic types,
	/// which means it cannot be used with types without own names like arrays.
	/// </summary>
	internal class TypeByNameComparer : IEqualityComparer<IType>
	{
		private readonly IEqualityComparer<CodeIdentifier> _identifierComparer;

		internal TypeByNameComparer(IEqualityComparer<CodeIdentifier> identifierComparer)
		{
			_identifierComparer = identifierComparer;
		}

		bool IEqualityComparer<IType>.Equals(IType x, IType y)
		{
			using var xe = EnumerateParts(x).GetEnumerator();
			using var ye = EnumerateParts(x).GetEnumerator();

			var xAdvanced = true;
			var yAdvanced = true;

			// bitwise & used intentionally to invoke both iterators
			while ((xAdvanced = xe.MoveNext()) & (yAdvanced = ye.MoveNext()))
			{
				var equal = _identifierComparer.Equals(xe.Current, ye.Current);
				if (!equal)
					return false;
			}

			return xAdvanced == yAdvanced;
		}

		int IEqualityComparer<IType>.GetHashCode(IType obj)
		{
			var hash = 0;
			foreach (var part in EnumerateParts(obj))
				hash ^= _identifierComparer.GetHashCode(part);
			return hash;
		}

		private IEnumerable<CodeIdentifier> EnumerateParts(IType type)
		{
			if (type.Namespace != null)
			{
				foreach (var ns in type.Namespace)
					yield return ns;
			}
			else if (type.Parent != null)
			{
				foreach (var part in EnumerateParts(type.Parent))
					yield return part;
			}

			yield return type.Name!;
		}
	}
}
