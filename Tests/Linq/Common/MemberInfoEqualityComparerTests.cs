using System;
using System.Reflection;

using LinqToDB.Internal.Reflection;

using NUnit.Framework;

using Shouldly;

namespace Tests.Common
{
	// Regression coverage for https://github.com/linq2db/linq2db/issues/5551.
	// Native AOT emits RuntimeSyntheticConstructorInfo for lambda closures, whose
	// MetadataToken accessor throws InvalidOperationException. The equality comparer
	// must tolerate that and fall back rather than propagate the throw.
	[TestFixture]
	public class MemberInfoEqualityComparerTests
	{
		[Test]
		public void GetHashCode_DoesNotThrowWhenMetadataTokenUnavailable()
		{
			var member = new SyntheticMemberInfo();

			_ = MemberInfoEqualityComparer.Default.GetHashCode(member);
		}

		[Test]
		public void Equals_DoesNotThrowWhenMetadataTokenUnavailable()
		{
			var x = new SyntheticMemberInfo();
			var y = new SyntheticMemberInfo();

			_ = MemberInfoEqualityComparer.Default.Equals(x, y);
		}

		[Test]
		public void Equals_SameInstanceReturnsTrueWithoutMetadataTokenAccess()
		{
			var member = new SyntheticMemberInfo();

			MemberInfoEqualityComparer.Default.Equals(member, member).ShouldBeTrue();
		}

		// Mimics RuntimeSyntheticConstructorInfo from Native AOT: every member is
		// usable except MetadataToken, which throws.
		private sealed class SyntheticMemberInfo : MemberInfo
		{
			public override Type?       DeclaringType => typeof(SyntheticMemberInfo);
			public override MemberTypes MemberType    => MemberTypes.Constructor;
			public override string      Name          => "synthetic";
			public override Type?       ReflectedType => typeof(SyntheticMemberInfo);
			public override Module      Module        => typeof(SyntheticMemberInfo).Module;

			public override int MetadataToken =>
				throw new InvalidOperationException("There is no metadata token available for the given member.");

			public override object[] GetCustomAttributes(bool inherit)                     => Array.Empty<object>();
			public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<object>();
			public override bool     IsDefined(Type attributeType, bool inherit)           => false;
		}
	}
}
