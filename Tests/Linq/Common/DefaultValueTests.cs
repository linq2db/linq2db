using System;

using LinqToDB.Mapping;

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
				Assert.That(DefaultValue<int>.Value, Is.EqualTo(default(int)));
				Assert.That(DefaultValue<uint>.Value, Is.EqualTo(default(uint)));
				Assert.That(DefaultValue<byte>.Value, Is.EqualTo(default(byte)));
				Assert.That(DefaultValue<char>.Value, Is.EqualTo(default(char)));
				Assert.That(DefaultValue<bool>.Value, Is.EqualTo(default(bool)));
				Assert.That(DefaultValue<sbyte>.Value, Is.EqualTo(default(sbyte)));
				Assert.That(DefaultValue<short>.Value, Is.EqualTo(default(short)));
				Assert.That(DefaultValue<long>.Value, Is.EqualTo(default(long)));
				Assert.That(DefaultValue<ushort>.Value, Is.EqualTo(default(ushort)));
				Assert.That(DefaultValue<ulong>.Value, Is.EqualTo(default(ulong)));
				Assert.That(DefaultValue<float>.Value, Is.EqualTo(default(float)));
				Assert.That(DefaultValue<double>.Value, Is.EqualTo(default(double)));
				Assert.That(DefaultValue<decimal>.Value, Is.EqualTo(default(decimal)));
				Assert.That(DefaultValue<DateTime>.Value, Is.EqualTo(default(DateTime)));
				Assert.That(DefaultValue<TimeSpan>.Value, Is.EqualTo(default(TimeSpan)));
				Assert.That(DefaultValue<DateTimeOffset>.Value, Is.EqualTo(default(DateTimeOffset)));
				Assert.That(DefaultValue<Guid>.Value, Is.EqualTo(default(Guid)));
				Assert.That(DefaultValue<string>.Value, Is.EqualTo(default(string)));
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
			Assert.That(DefaultValue<int?>.Value, Is.EqualTo(null));
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
			Assert.That(DefaultValue<Enum1?>.Value, Is.EqualTo(null));
			DefaultValue<Enum1?>.Value = Enum1.Value2;
			Assert.That(DefaultValue<Enum1?>.Value, Is.EqualTo(Enum1.Value2));
			DefaultValue<Enum1?>.Value = null;
		}

		[Test]
		public void String()
		{
			Assert.That(DefaultValue<string>.Value, Is.EqualTo(null));
			DefaultValue<string>.Value = "";
			Assert.That(DefaultValue<string>.Value, Is.EqualTo(""));
			DefaultValue<string?>.Value = null;
		}
	}
}
