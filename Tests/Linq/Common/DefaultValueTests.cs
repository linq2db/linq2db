using System;

using LinqToDB.Common;

using NUnit.Framework;

namespace Tests.Common
{
	[TestFixture]
	public class DefaultValueTests : TestBase
	{
		[Test]
		public void BaseTypes()
		{
			Assert.Multiple(() =>
			{
				Assert.That(DefaultValue<int>.Value, Is.Default);
				Assert.That(DefaultValue<uint>.Value, Is.Default);
				Assert.That(DefaultValue<byte>.Value, Is.Default);
				Assert.That(DefaultValue<char>.Value, Is.Default);
				Assert.That(DefaultValue<bool>.Value, Is.Default);
				Assert.That(DefaultValue<sbyte>.Value, Is.Default);
				Assert.That(DefaultValue<short>.Value, Is.Default);
				Assert.That(DefaultValue<long>.Value, Is.Default);
				Assert.That(DefaultValue<ushort>.Value, Is.Default);
				Assert.That(DefaultValue<ulong>.Value, Is.Default);
				Assert.That(DefaultValue<float>.Value, Is.Default);
				Assert.That(DefaultValue<double>.Value, Is.Default);
				Assert.That(DefaultValue<decimal>.Value, Is.Default);
				Assert.That(DefaultValue<DateTime>.Value, Is.EqualTo(default(DateTime)));
				Assert.That(DefaultValue<TimeSpan>.Value, Is.EqualTo(default(TimeSpan)));
				Assert.That(DefaultValue<DateTimeOffset>.Value, Is.EqualTo(default(DateTimeOffset)));
				Assert.That(DefaultValue<Guid>.Value, Is.EqualTo(default(Guid)));
				Assert.That(DefaultValue<string>.Value, Is.Null);
			});
		}

		[Test]
		public void Int()
		{
			Assert.That(DefaultValue<int>.Value, Is.EqualTo(0));
			DefaultValue<int>.Value = 5;
			Assert.That(DefaultValue<int>.Value, Is.EqualTo(5));
			DefaultValue<int>.Value = 0;
		}

		[Test]
		public void UInt()
		{
			Assert.That(DefaultValue.GetValue(typeof(uint)), Is.EqualTo(0u));
			DefaultValue<uint>.Value = 10;
			Assert.That(DefaultValue.GetValue(typeof(uint)), Is.EqualTo(10u));
			DefaultValue<uint>.Value = 0;
		}

		[Test]
		public void IntNullable()
		{
			Assert.That(DefaultValue<int?>.Value, Is.Null);
			DefaultValue<int?>.Value = 5;
			Assert.That(DefaultValue<int?>.Value, Is.EqualTo(5));
			DefaultValue<int?>.Value = null;
		}

		enum Enum1
		{
			Value1,
			Value2
		}

		[Test]
		public void Enum()
		{
			Assert.That(DefaultValue<Enum1>.Value, Is.EqualTo(Enum1.Value1));
			DefaultValue<Enum1>.Value = Enum1.Value2;
			Assert.That(DefaultValue<Enum1>.Value, Is.EqualTo(Enum1.Value2));
			DefaultValue<Enum1>.Value = Enum1.Value1;
		}

		[Test]
		public void EnumNullable()
		{
			Assert.That(DefaultValue<Enum1?>.Value, Is.Null);
			DefaultValue<Enum1?>.Value = Enum1.Value2;
			Assert.That(DefaultValue<Enum1?>.Value, Is.EqualTo(Enum1.Value2));
			DefaultValue<Enum1?>.Value = null;
		}

		[Test]
		public void String()
		{
			Assert.That(DefaultValue<string>.Value, Is.Null);
			DefaultValue<string>.Value = "";
			Assert.That(DefaultValue<string>.Value, Is.EqualTo(""));
			DefaultValue<string?>.Value = null;
		}
	}
}
