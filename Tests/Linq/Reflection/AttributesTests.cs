using System;
using System.Reflection;

using LinqToDB.Extensions;
using LinqToDB.Internal.Mapping;
using LinqToDB.Mapping;

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

			Assert.Multiple(() =>
			{
				Assert.That(prop.HasAttribute<MyAttribute>(false), Is.False);
				Assert.That(prop.HasAttribute<MyAttribute>(true), Is.True);
			});
		}

		[Test]
		public void EventAttributeInheritanceTest()
		{
			var ev = typeof(Derived).GetEvent(nameof(Base.Event))!;

			Assert.Multiple(() =>
			{
				Assert.That(ev.HasAttribute<MyAttribute>(false), Is.False);
				Assert.That(ev.HasAttribute<MyAttribute>(true), Is.True);
			});
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4079")]
		public void DynamicColumnInfoAttributes()
		{
			var dc = new DynamicColumnInfo(typeof(string), typeof(string), "name");

#pragma warning disable RS0030 // Do not use banned APIs
			CustomAttributeExtensions.GetCustomAttribute<Attribute>(dc);
			CustomAttributeExtensions.GetCustomAttribute<Attribute>(dc, true);
			CustomAttributeExtensions.GetCustomAttribute<Attribute>(dc, false);
#pragma warning disable CA2263 // Prefer generic overload when type is known
			CustomAttributeExtensions.GetCustomAttribute(dc, typeof(Attribute));
			CustomAttributeExtensions.GetCustomAttribute(dc, typeof(Attribute), true);
			CustomAttributeExtensions.GetCustomAttribute(dc, typeof(Attribute), false);
#pragma warning restore CA2263 // Prefer generic overload when type is known

			CustomAttributeExtensions.GetCustomAttributes<Attribute>(dc);
			CustomAttributeExtensions.GetCustomAttributes<Attribute>(dc, true);
			CustomAttributeExtensions.GetCustomAttributes<Attribute>(dc, false);
			CustomAttributeExtensions.GetCustomAttributes(dc);
			CustomAttributeExtensions.GetCustomAttributes(dc, true);
			CustomAttributeExtensions.GetCustomAttributes(dc, false);
#pragma warning disable CA2263 // Prefer generic overload when type is known
			CustomAttributeExtensions.GetCustomAttributes(dc, typeof(Attribute));
			CustomAttributeExtensions.GetCustomAttributes(dc, typeof(Attribute), true);
			CustomAttributeExtensions.GetCustomAttributes(dc, typeof(Attribute), false);
#pragma warning restore CA2263 // Prefer generic overload when type is known

			AttributesExtensions.GetAttribute<Attribute>(dc, true);
			AttributesExtensions.GetAttribute<Attribute>(dc,false);

			AttributesExtensions.GetAttributes<Attribute>(dc, true);
			AttributesExtensions.GetAttributes<Attribute>(dc, false);
#pragma warning restore RS0030 // Do not use banned APIs
		}
	}
}
