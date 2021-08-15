using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.CodeGen.Model;

namespace LinqToDB.CodeGen.CodeGeneration
{
	public class TypeComparer : IEqualityComparer<IType>
	{
		public static readonly IEqualityComparer<IType> Exact = new TypeComparer(false);
		public static readonly IEqualityComparer<IType> IgnoreNRT = new TypeComparer(true);

		private readonly bool _ignoreNRT;

		private TypeComparer(bool ignoreNRT)
		{
			_ignoreNRT = ignoreNRT;
		}

		public bool Equals(IType x, IType y)
		{
			if (x.Kind != y.Kind)
				return false;

			if (x.IsValueType != y.IsValueType)
				return false;

			if (x.OpenGenericArgCount != y.OpenGenericArgCount)
				return false;

			if (x.External != y.External)
				return false;

			if ((!_ignoreNRT || x.IsValueType) && x.IsNullable != y.IsNullable)
				return false;

			if (x.Name?.Name != y.Name?.Name)
				return false;

			if ((x.Namespace == null && y.Namespace != null)
				|| (x.Namespace != null && y.Namespace == null))
				return false;
			if (x.Namespace != null
				&& (x.Namespace.Length != y.Namespace!.Length
				|| !x.Namespace.Select(_ => _.Name).SequenceEqual(y.Namespace.Select(_ => _.Name))))
				return false;

			if ((x.Parent == null && y.Parent != null)
				|| (x.Parent != null && y.Parent == null))
				return false;
			if (x.Parent != null
				&& !Equals(x.Parent, y.Parent!))
				return false;

			if ((x.ArrayElementType == null && y.ArrayElementType != null)
				|| (x.ArrayElementType != null && y.ArrayElementType == null))
				return false;
			if (x.ArrayElementType != null
				&& !Equals(x.ArrayElementType, y.ArrayElementType!))
				return false;

			if ((x.TypeArguments == null && y.TypeArguments != null)
				|| (x.TypeArguments != null && y.TypeArguments == null))
				return false;
			if (x.TypeArguments != null)
			{
				if (x.TypeArguments.Length != y.TypeArguments!.Length)
					return false;

				for (var i = 0; i < x.TypeArguments.Length; i++)
					if (!Equals(x.TypeArguments[i], y.TypeArguments[i]))
						return false;
			}

			if ((x.ArraySizes == null && y.ArraySizes != null) || (x.ArraySizes != null && y.ArraySizes == null))
				return false;
			if (x.ArraySizes != null
				&& !x.ArraySizes.SequenceEqual(y.ArraySizes))
				return false;

			return true;
		}

		public int GetHashCode(IType obj)
		{
			var hashCode = obj.Kind.GetHashCode();

			hashCode ^= obj.IsValueType.GetHashCode();
			hashCode ^= obj.OpenGenericArgCount.GetHashCode();
			hashCode ^= obj.External.GetHashCode();

			if (!_ignoreNRT || obj.IsValueType)
				hashCode ^= obj.IsValueType.GetHashCode();

			if (obj.Name != null)
				hashCode ^= obj.Name.Name.GetHashCode();

			if (obj.Namespace != null)
			{
				foreach (var ns in obj.Namespace)
				hashCode ^= ns.Name.GetHashCode();
			}

			if (obj.Parent != null)
				hashCode ^= GetHashCode(obj.Parent);

			if (obj.ArrayElementType != null)
				hashCode ^= GetHashCode(obj.ArrayElementType);

			if (obj.TypeArguments != null)
			{
				foreach (var type in obj.TypeArguments)
					hashCode ^= GetHashCode(type);
			}

			if (obj.ArraySizes != null)
			{
				foreach (var size in obj.ArraySizes)
					hashCode ^= size.GetHashCode();
			}

			return hashCode;
		}
	}
}
