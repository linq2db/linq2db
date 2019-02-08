using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Reflection;
using LinqToDB.Tools;

using NUnit.Framework;

namespace Tests.Tools
{
	[TestFixture]
	public class ComparerBuilderTests
	{
		class A
		{
			public         int Value       { get; set; }
			public virtual int Overridable { get; set; }

			public virtual void M() {}
		}

		class B : A
		{
			public override int Overridable { get; set; }

			public override void M() {}
		}

		class C : A
		{
		}

		[Test]
		public void InheritedMember0Test()
		{
			var comparer = ComparerBuilder.GetEqualityComparer<A>(o => o.Value, o => o.Overridable);
			var o1       = new A { Value = 10, Overridable = 20 };
			var o2       = new A { Value = 10, Overridable = 20 };

			Assert.That(comparer.Equals(o1, o2), Is.True);
		}

		[Test]
		public void InheritedMember1Test()
		{
			var comparer = ComparerBuilder.GetEqualityComparer<A>(o => o.Value, o => o.Overridable);
			var o1       = new A { Value = 10, Overridable = 20 };
			var o2       = new B { Value = 10, Overridable = 20 };

			Assert.That(comparer.Equals(o1, o2), Is.True);
		}

		[Test]
		public void InheritedMember2Test()
		{
			var comparer = ComparerBuilder.GetEqualityComparer<A>(o => o.Value, o => o.Overridable);
			var o1       = new B { Value = 10, Overridable = 20 };
			var o2       = new B { Value = 10, Overridable = 20 };

			Assert.That(comparer.Equals(o1, o2), Is.True);
		}

		[Test]
		public void InheritedMember3Test()
		{
			var comparer = ComparerBuilder.GetEqualityComparer<A>(o => o.Value, o => o.Overridable);
			var o1       = new B { Value = 10, Overridable = 20 };
			var o2       = new B { Value = 20, Overridable = 10 };

			Assert.That(comparer.Equals(o1, o2), Is.False);
		}

		[Test]
		public void InheritedMember4Test()
		{
			var comparer = ComparerBuilder.GetEqualityComparer<C>(o => o.Value);
			var o1       = new C { Value = 10, Overridable = 20 };
			var o2       = new C { Value = 20, Overridable = 10 };

			Assert.That(comparer.Equals(o1, o2), Is.False);
		}

		[TestCase("Test", "Data", true)]
		[TestCase("Alpha", "Beta", false)]
		public void ComplexMemberTest(string p1, string p2, bool expected)
		{
			var comparer = ComparerBuilder.GetEqualityComparer<TestClass>(c => c.Prop2.Length);
			var o1       = new TestClass { Prop2 = p1 };
			var o2       = new TestClass { Prop2 = p2 };

			Assert.That(comparer.Equals(o1, o2), Is.EqualTo(expected));
		}

#if !NETSTANDARD1_6

		[Test]
		public void MethodHandleTest()
		{
			var comparer = ComparerBuilder.GetEqualityComparer<MethodInfo>(m => m.MethodHandle);

			var a  = typeof(A).GetMethod("M");
			var b  = typeof(B).GetMethod("M");
			var b2 = b.GetBaseDefinition();

			Assert.False(a.Equals(b), "MethodInfo fails");
			Assert.True (a.Equals(b2), "MethodInfo fails");

			Assert.False(a.MethodHandle.Equals(b.MethodHandle), "MethodHandle fails");
			Assert.True(a.MethodHandle.Equals(b2.MethodHandle), "MethodHandle fails");

			Assert.False(EqualityComparer<RuntimeMethodHandle>.Default.Equals(a.MethodHandle, b.MethodHandle), "EqualityComparer fails");
			Assert.True(EqualityComparer<RuntimeMethodHandle>.Default.Equals(a.MethodHandle, b2.MethodHandle), "EqualityComparer fails");

			Assert.False(comparer.Equals(a, b), "ComparerBuilder fails");
			Assert.True(comparer.Equals(a, b2), "ComparerBuilder fails");
		}

#endif

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		class TestClass
		{
			public int    Field1;
			public string Prop2 { get; set; }

			static int _n;
			       int _field = ++_n;
		}

		[Test]
		public void GetHashCodeTest()
		{
			var eq = ComparerBuilder.GetEqualityComparer<TestClass>();

			Assert.That(eq.GetHashCode(new TestClass()), Is.Not.EqualTo(0));
			Assert.That(eq.GetHashCode(null),        Is.    EqualTo(0));
		}

		[Test]
		public void EqualsTest()
		{
			var eq = ComparerBuilder.GetEqualityComparer<TestClass>();

			Assert.That(eq.Equals(new TestClass(), new TestClass()), Is.True);
			Assert.That(eq.Equals(null, null), Is.True);
			Assert.That(eq.Equals(null, new TestClass()), Is.False);
			Assert.That(eq.Equals(new TestClass(), null), Is.False);
			Assert.That(eq.Equals(new TestClass(), new TestClass { Field1 = 1 }), Is.False);
		}

		class NoMemberClass
		{
		}

		[Test]
		public void NoMemberTest()
		{
			var eq = ComparerBuilder.GetEqualityComparer<NoMemberClass>();

			Assert.That(eq.GetHashCode(new NoMemberClass()), Is.Not.EqualTo(0));

			Assert.That(eq.Equals(new NoMemberClass(), new NoMemberClass()), Is.True);
			Assert.That(eq.Equals(new NoMemberClass(), null), Is.False);
		}

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		class OneMemberClass
		{
			public int Field1;
		}

		[Test]
		public void OneMemberTest()
		{
			var eq = ComparerBuilder.GetEqualityComparer<OneMemberClass>();

			Assert.That(eq.GetHashCode(new OneMemberClass()), Is.Not.EqualTo(0));

			Assert.That(eq.Equals(new OneMemberClass(), new OneMemberClass()), Is.True);
			Assert.That(eq.Equals(new OneMemberClass(), null), Is.False);
			Assert.That(eq.Equals(new OneMemberClass(), new OneMemberClass { Field1 = 1 }), Is.False);
		}

		[Test]
		public void DistinctTest()
		{
			var eq = ComparerBuilder.GetEqualityComparer<TestClass>();
			var arr = new[]
			{
				new TestClass { Field1 = 1, Prop2 = "2"  },
				new TestClass { Field1 = 1, Prop2 = null },
				null,
				new TestClass { Field1 = 2, Prop2 = "1"  },
				new TestClass { Field1 = 2, Prop2 = "2"  },
				new TestClass { Field1 = 2, Prop2 = "2"  },
				null
			};

			Assert.That(arr.Distinct(eq).Count(), Is.EqualTo(5));
		}

		[Test]
		public void DistinctByMember1Test()
		{
			var eq = ComparerBuilder.GetEqualityComparer<TestClass>(t => t.Field1);
			var arr = new[]
			{
				new TestClass { Field1 = 1, Prop2 = "2"  },
				new TestClass { Field1 = 1, Prop2 = null },
				null,
				new TestClass { Field1 = 2, Prop2 = "1"  },
				new TestClass { Field1 = 2, Prop2 = "2"  },
				new TestClass { Field1 = 2, Prop2 = "2"  },
				null
			};

			Assert.That(arr.Distinct(eq).Count(), Is.EqualTo(3));
		}

		[Test]
		public void DistinctByMember2Test()
		{
			var eq = ComparerBuilder.GetEqualityComparer<TestClass>(t => t.Field1, t => t.Prop2);
			var arr = new[]
			{
				new TestClass { Field1 = 1, Prop2 = "2"  },
				new TestClass { Field1 = 1, Prop2 = null },
				null,
				new TestClass { Field1 = 2, Prop2 = "1"  },
				new TestClass { Field1 = 2, Prop2 = "2"  },
				new TestClass { Field1 = 2, Prop2 = "2"  },
				null
			};

			Assert.That(arr.Distinct(eq).Count(), Is.EqualTo(5));
		}

		[Test]
		public void DistinctByMember3Test()
		{
			var eq = ComparerBuilder.GetEqualityComparer<TestClass>(ta => ta.Members.Where(m => m.Name.EndsWith("1")));
			var arr = new[]
			{
				new TestClass { Field1 = 1, Prop2 = "2"  },
				new TestClass { Field1 = 1, Prop2 = null },
				null,
				new TestClass { Field1 = 2, Prop2 = "1"  },
				new TestClass { Field1 = 2, Prop2 = "2"  },
				new TestClass { Field1 = 2, Prop2 = "2"  },
				null
			};

			Assert.That(arr.Distinct(eq).Count(), Is.EqualTo(3));
		}

		[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
		class IdentifierAttribute : Attribute
		{
		}

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		class TestClass2
		{
			[Identifier]
			public int EntityType { get; set; }
			[Identifier]
			public int EntityID { get; set; }

			public string Name { get; set; }
		}

		static IEnumerable<MemberAccessor> GetIdentifiers([NotNull] TypeAccessor typeAccessor)
		{
			foreach (var member in typeAccessor.Members)
				if (member.MemberInfo.GetCustomAttribute<IdentifierAttribute>() != null)
					yield return member;
		}

		[Test]
		public void AttributeTest()
		{
			var eq = ComparerBuilder.GetEqualityComparer<TestClass2>(GetIdentifiers);
			var arr = new[]
			{
				null,
				new TestClass2 { EntityType = 1, EntityID = 1, Name = "1"  },
				new TestClass2 { EntityType = 1, EntityID = 2, Name = null },
				new TestClass2 { EntityType = 2, EntityID = 1, Name = "2"  },
				new TestClass2 { EntityType = 2, EntityID = 1, Name = "3"  },
				new TestClass2 { EntityType = 2, EntityID = 2, Name = "4"  },
				null
			};

			Assert.That(arr.Distinct(eq).Count(), Is.EqualTo(5));
		}
	}
}
