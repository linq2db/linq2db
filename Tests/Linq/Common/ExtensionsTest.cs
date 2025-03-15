using System;

using Shouldly;

using LinqToDB.Expressions;
using LinqToDB.Extensions;

using NUnit.Framework;

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

	}
}
