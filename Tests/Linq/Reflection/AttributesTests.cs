﻿using System;
using System.Reflection;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using Microsoft.Data.SqlClient;
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4079")]
		public void DynamicColumnInfoAttributes()
		{
			var dc = new DynamicColumnInfo(typeof(string), typeof(string), "name");

#pragma warning disable RS0030 // Do not use banned APIs
			CustomAttributeExtensions.GetCustomAttribute<Attribute>(dc);
			CustomAttributeExtensions.GetCustomAttribute<Attribute>(dc, true);
			CustomAttributeExtensions.GetCustomAttribute<Attribute>(dc, false);
			CustomAttributeExtensions.GetCustomAttribute(dc, typeof(Attribute));
			CustomAttributeExtensions.GetCustomAttribute(dc, typeof(Attribute), true);
			CustomAttributeExtensions.GetCustomAttribute(dc, typeof(Attribute), false);

			CustomAttributeExtensions.GetCustomAttributes<Attribute>(dc);
			CustomAttributeExtensions.GetCustomAttributes<Attribute>(dc, true);
			CustomAttributeExtensions.GetCustomAttributes<Attribute>(dc, false);
			CustomAttributeExtensions.GetCustomAttributes(dc);
			CustomAttributeExtensions.GetCustomAttributes(dc, true);
			CustomAttributeExtensions.GetCustomAttributes(dc, false);
			CustomAttributeExtensions.GetCustomAttributes(dc, typeof(Attribute));
			CustomAttributeExtensions.GetCustomAttributes(dc, typeof(Attribute), true);
			CustomAttributeExtensions.GetCustomAttributes(dc, typeof(Attribute), false);

			AttributesExtensions.GetAttribute<Attribute>(dc, true);
			AttributesExtensions.GetAttribute<Attribute>(dc,false);

			AttributesExtensions.GetAttributes<Attribute>(dc, true);
			AttributesExtensions.GetAttributes<Attribute>(dc, false);
#pragma warning restore RS0030 // Do not use banned APIs
		}
	}
}
