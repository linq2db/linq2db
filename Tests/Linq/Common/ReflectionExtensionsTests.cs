using System;
using System.Reflection;

using LinqToDB.Internal.Extensions;

using NUnit.Framework;

using Shouldly;

namespace Tests.Common
{
	[TestFixture]
	public class ReflectionExtensionsTests : TestBase
	{
		class BaseItem;

		sealed class Item : BaseItem;

		sealed class Sample
		{
			public Sample(int value)
			{
			}

			public Sample(BaseItem item)
			{
			}

			public static int    Widen   (int value)    => value;
			public static int    Nullable(int? value)   => value ?? 0;
			public static int    Boxed   (object value) => 0;
			public static string Accept  (object item)  => "object";
			public static string Exact   (Item item)    => "item";

			public static string Generic<T>(BaseItem item, T value) => "generic";
			public static string Generic   (BaseItem item)          => "non-generic";
		}

		class Overloads
		{
			public string Pick  (Item item)     => "identical";
			public string Pick  (object item)   => "compatible";
			public string Tie   (BaseItem item) => "base item";
			public string Tie   (object item)   => "object";
			public string Hidden(BaseItem item) => "base";
		}

		sealed class DerivedOverloads : Overloads
		{
			public new string Hidden(BaseItem item) => "derived";
		}

		class MixedBase
		{
			public string Mixed(BaseItem item) => "base";
		}

		sealed class MixedDerived : MixedBase
		{
#pragma warning disable CA1061 // Do not hide base class methods
			public string Mixed(object item) => "derived";
#pragma warning restore CA1061 // Do not hide base class methods
		}

		const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

		[Test]
		public void GetMethodExFindsIdenticalParameters()
		{
			var method = typeof(Sample).GetMethodEx(nameof(Sample.Exact), typeof(Item));

			method.ShouldNotBeNull();
		}

		[Test]
		public void GetMethodExFindsReferenceAssignableParameters()
		{
			var method = typeof(Sample).GetMethodEx(nameof(Sample.Accept), typeof(Item));

			method.ShouldNotBeNull();
		}

		[Test]
		public void GetMethodExRejectsPrimitiveWidening()
		{
			var method = typeof(Sample).GetMethodEx(nameof(Sample.Widen), typeof(byte));

			method.ShouldBeNull();
		}

		[Test]
		public void GetMethodExRejectsNullableWrapping()
		{
			var method = typeof(Sample).GetMethodEx(nameof(Sample.Nullable), typeof(int));

			method.ShouldBeNull();
		}

		[Test]
		public void GetMethodExRejectsBoxing()
		{
			var method = typeof(Sample).GetMethodEx(nameof(Sample.Boxed), typeof(int));

			method.ShouldBeNull();
		}

		[Test]
		public void GetMethodCoercingFindsValueConversions()
		{
			var widen    = typeof(Sample).GetMethodCoercing(nameof(Sample.Widen),    typeof(byte));
			var nullable = typeof(Sample).GetMethodCoercing(nameof(Sample.Nullable), typeof(int));

			widen   .ShouldNotBeNull();
			nullable.ShouldNotBeNull();
		}

		[Test]
		public void GetConstructorExRejectsPrimitiveWidening()
		{
			var strict   = typeof(Sample).GetConstructorEx      (typeof(byte));
			var coercing = typeof(Sample).GetConstructorCoercing(typeof(byte));

			strict  .ShouldBeNull();
			coercing.ShouldNotBeNull();
		}

		[Test]
		public void GetConstructorExFindsReferenceAssignableParameters()
		{
			var ctor = typeof(Sample).GetConstructorEx(typeof(Item));

			ctor.ShouldNotBeNull();
		}

		[Test]
		public void GetMethodExWithArityFindsReferenceAssignableParameters([Values(0, 1)] int genericParametersCount)
		{
			var types  = genericParametersCount == 0 ? new[] { typeof(Item) } : new[] { typeof(Item), typeof(int) };
			var method = typeof(Sample).GetMethodEx(nameof(Sample.Generic), genericParametersCount, BindingFlags.Public | BindingFlags.Static, types);

			method.ShouldNotBeNull();

			var isGeneric = method.IsGenericMethodDefinition;

			isGeneric.ShouldBe(genericParametersCount != 0);
		}

		[Test]
		public void GetMethodExWithArityPrefersIdenticalParameters()
		{
			var method = typeof(Overloads).GetMethodEx(nameof(Overloads.Pick), 0, PublicInstance, typeof(Item));

			method.ShouldNotBeNull();

			var parameterType = method.GetParameters()[0].ParameterType;

			parameterType.ShouldBe(typeof(Item));
		}

		[Test]
		public void GetMethodExWithArityThrowsOnTie()
		{
			Action act = () => typeof(Overloads).GetMethodEx(nameof(Overloads.Tie), 0, PublicInstance, typeof(Item));

			act.ShouldThrow<AmbiguousMatchException>();
		}

		[Test]
		public void GetMethodExWithArityThrowsOnTieAcrossHierarchy()
		{
			Action act = () => typeof(MixedDerived).GetMethodEx(nameof(MixedDerived.Mixed), 0, PublicInstance, typeof(Item));

			act.ShouldThrow<AmbiguousMatchException>();
		}

		[Test]
		public void GetMethodExWithArityPrefersHidingMethod()
		{
			var method = typeof(DerivedOverloads).GetMethodEx(nameof(DerivedOverloads.Hidden), 0, PublicInstance, typeof(Item));

			method.ShouldNotBeNull();

			var declaringType = method.DeclaringType;

			declaringType.ShouldBe(typeof(DerivedOverloads));
		}
	}
}
