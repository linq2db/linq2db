using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

using LinqToDB.Internal.Mapping;

namespace LinqToDB.Internal.Reflection
{
	public sealed class MemberInfoEqualityComparer : IEqualityComparer<MemberInfo>
	{
		public static readonly MemberInfoEqualityComparer Default = new();

		// Native AOT emits synthetic MemberInfo subclasses (e.g. RuntimeSyntheticConstructorInfo
		// for lambda closures) whose MetadataToken accessor throws InvalidOperationException.
		// Cache per concrete Type so the exception cost is paid at most once per unsupported type.
		static readonly ConcurrentDictionary<Type, bool> _supportsMetadataToken = new();

		static bool SupportsMetadataToken(MemberInfo obj)
		{
			var type = obj.GetType();

			if (_supportsMetadataToken.TryGetValue(type, out var supported))
				return supported;

			try
			{
				_ = obj.MetadataToken;
				supported = true;
			}
			catch (InvalidOperationException)
			{
				supported = false;
			}

			_supportsMetadataToken.TryAdd(type, supported);
			return supported;
		}

		public bool Equals(MemberInfo? x, MemberInfo? y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (ReferenceEquals(x, null))
			{
				return false;
			}

			if (ReferenceEquals(y, null))
			{
				return false;
			}

			if (x.GetType() != y.GetType())
			{
				return false;
			}

			if (x is VirtualPropertyInfoBase xv)
			{
				return xv.Equals(y);
			}

			if (!SupportsMetadataToken(x))
				return x.Equals(y);

			return x.MetadataToken == y.MetadataToken && x.Module.Equals(y.Module);
		}

		public int GetHashCode(MemberInfo obj)
		{
			// We do not support obj.MetadataToken and obj.Module
			if (obj is VirtualPropertyInfoBase)
				return obj.GetHashCode();

			if (!SupportsMetadataToken(obj))
				return obj.GetHashCode();

			return HashCode.Combine(obj.MetadataToken, obj.Module);
		}
	}
}
