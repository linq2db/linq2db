using System;

using LinqToDB.Expressions;
using LinqToDB.Internal.Extensions;

using NUnit.Framework;

using Shouldly;

namespace Tests.Common
{
	[TestFixture]
	public class ExtensionsTest
	{
		private class BaseEntity
		{
			public         int PropNonVirtual { get; set; }
			public virtual int PropVirtual    { get; set; }
		}

		private sealed class DerivedEntity : BaseEntity
		{
			public new Guid PropVirtual    { get; set; }
			public new Guid PropNonVirtual { get; set; }
		}

		private interface IBaseInterface
		{
			int Value { get; }
		}

		private interface IDerivedInterface : IBaseInterface
		{
		}

		private sealed class InterfaceImplementation : IDerivedInterface
		{
			public int Value { get; set; }
		}

		[Test]
		public void VirtualPropDerived()
		{
			var prop = MemberHelper.PropertyOf<DerivedEntity>(x => x.PropVirtual);
			typeof(DerivedEntity).GetMemberEx(prop).ShouldBe(prop);
		}

		[Test]
		public void NonVirtualPropDerived()
		{
			var prop = MemberHelper.PropertyOf<DerivedEntity>(x => x.PropNonVirtual);
			typeof(DerivedEntity).GetMemberEx(prop).ShouldBe(prop);
		}

		[Test]
		public void VirtualProp()
		{
			var prop = MemberHelper.PropertyOf<DerivedEntity>(x => x.PropVirtual);
			typeof(BaseEntity).GetMemberEx(prop).ShouldNotBe(prop);
		}

		[Test]
		public void NonVirtualProp()
		{
			var prop = MemberHelper.PropertyOf<DerivedEntity>(x => x.PropNonVirtual);
			typeof(BaseEntity).GetMemberEx(prop).ShouldNotBe(prop);
		}

		[Test]
		public void GetInterfaceMapExReturnsMapForConcreteImplementation()
		{
			var map = typeof(InterfaceImplementation).GetInterfaceMapEx(typeof(IBaseInterface));

			map.TargetType.             ShouldBe(typeof(InterfaceImplementation));
			map.InterfaceType.          ShouldBe(typeof(IBaseInterface));
			map.TargetMethods.Length.   ShouldBe(1);
			map.InterfaceMethods.Length.ShouldBe(1);
		}

		[Test]
		public void GetInterfaceMapExReturnsEmptyMapForInterfaceImplementation()
		{
			var map = typeof(IDerivedInterface).GetInterfaceMapEx(typeof(IBaseInterface));

			map.TargetType.      ShouldBe(typeof(IDerivedInterface));
			map.InterfaceType.   ShouldBe(typeof(IBaseInterface));
			map.TargetMethods.   ShouldBeEmpty();
			map.InterfaceMethods.ShouldBeEmpty();
		}

	}
}
