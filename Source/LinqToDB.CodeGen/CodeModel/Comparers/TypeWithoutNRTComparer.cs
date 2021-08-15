using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Compare types without taking into account nullability for reference types.
	/// </summary>
	internal class TypeWithoutNRTComparer : IEqualityComparer<IType>
	{
		private readonly IEqualityComparer<CodeIdentifier>              _identifierComparer;
		private readonly IEqualityComparer<IEnumerable<CodeIdentifier>> _namespaceComparer;

		internal TypeWithoutNRTComparer(IEqualityComparer<CodeIdentifier> identifierComparer, IEqualityComparer<IEnumerable<CodeIdentifier>> namespaceComparer)
		{
			_identifierComparer = identifierComparer;
			_namespaceComparer  = namespaceComparer;
		}

		bool IEqualityComparer<IType>.Equals     (IType x, IType y) => EqualsImpl(x, y);
		int  IEqualityComparer<IType>.GetHashCode(IType obj       ) => GetHashCodeImpl(obj);

		private bool EqualsImpl(IType x, IType y)
		{
			if (x.Kind                != y.Kind               ) return false;
			if (x.IsValueType         != y.IsValueType        ) return false;
			if (x.External            != y.External           ) return false;
			if (x.Alias               != y.Alias              ) return false;
			if (x.OpenGenericArgCount != y.OpenGenericArgCount) return false;

			// ignore nullability for reference types
			if (x.IsValueType && x.IsNullable != y.IsNullable) return false;

			if ((x.Name             == null ^ y.Name             == null) || (x.Name             != null && !_identifierComparer.Equals(x.Name, y.Name!         ))) return false;
			if ((x.Namespace        == null ^ y.Namespace        == null) || (x.Namespace        != null && !_namespaceComparer.Equals(x.Namespace, y.Namespace!))) return false;
			if ((x.Parent           == null ^ y.Parent           == null) || (x.Parent           != null && !EqualsImpl(x.Parent, y.Parent!                     ))) return false;
			if ((x.ArrayElementType == null ^ y.ArrayElementType == null) || (x.ArrayElementType != null && !EqualsImpl(x.ArrayElementType, y.ArrayElementType! ))) return false;

			if (x.TypeArguments     == null ^ y.TypeArguments    == null) return false;
			if (x.TypeArguments != null)
			{
				if (x.TypeArguments.Length != y.TypeArguments!.Length) return false;

				for (var i = 0; i < x.TypeArguments.Length; i++)
					if (!EqualsImpl(x.TypeArguments[i], y.TypeArguments[i]))
						return false;
			}

			if (x.ArraySizes        == null ^ y.ArraySizes       == null) return false;
			if (x.ArraySizes != null)
			{
				if (x.ArraySizes.Length != y.ArraySizes!.Length) return false;

				for (var i = 0; i < x.ArraySizes.Length; i++)
					if (x.ArraySizes[i] != y.ArraySizes[i])
						return false;
			}

			return true;
		}

		private int GetHashCodeImpl(IType obj)
		{
			var hash =
				   obj.Kind                .GetHashCode()
				^  obj.IsValueType         .GetHashCode()
				^  obj.External            .GetHashCode()
				^  obj.Alias?              .GetHashCode() ?? 0
				^  obj.OpenGenericArgCount?.GetHashCode() ?? 0

				^ (obj.IsValueType              ? obj.IsNullable.GetHashCode()                  : 0)
				^ (obj.Name             != null ? _identifierComparer.GetHashCode(obj.Name)     : 0)
				^ (obj.Namespace        != null ? _namespaceComparer.GetHashCode(obj.Namespace) : 0)
				^ (obj.Parent           != null ? GetHashCodeImpl(obj.Parent)                   : 0)
				^ (obj.ArrayElementType != null ? GetHashCodeImpl(obj.ArrayElementType)         : 0)
				;

			if (obj.TypeArguments != null)
				foreach (var type in obj.TypeArguments)
					hash ^= GetHashCodeImpl(type);

			if (obj.ArraySizes != null)
				foreach (var size in obj.ArraySizes)
					hash ^= size?.GetHashCode() ?? 0;

			return hash;
		}
	}
}
