using System;

using LinqToDB.Extensions;

using NUnit.Framework;

namespace Tests.Reflection
{
	[TestFixture]
	public sealed class AttributesTests : TestBase
	{
		[Test]
		public void PropertyAttributeInheritanceTest()
		{
			var prop = typeof(Derived).GetProperty(nameof(Base.Property))!;

			Assert.False(prop.HasAttribute<MyAttribute>(false));
			Assert.True(prop.HasAttribute<MyAttribute>(true));
		}

		[Test]
		public void EventAttributeInheritanceTest()
		{
			var ev = typeof(Derived).GetEvent(nameof(Base.Event))!;

			Assert.False(ev.HasAttribute<MyAttribute>(false));
			Assert.True(ev.HasAttribute<MyAttribute>(true));
		}

		public class Base
		{
			[My]
			public virtual bool Property { get; set; }

			[My]
			public virtual event Action Event { add { } remove { } }
		}

		public sealed class Derived : Base
		{
			public override bool Property { get; set; }

			public override event Action Event { add { } remove { } }
		}

		[AttributeUsage(AttributeTargets.All)]
		public sealed class MyAttribute : Attribute
		{
		}
	}
}
