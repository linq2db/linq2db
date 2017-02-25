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
			Assert.AreEqual(default(int),            DefaultValue<int>.           Value);
			Assert.AreEqual(default(uint),           DefaultValue<uint>.          Value);
			Assert.AreEqual(default(byte),           DefaultValue<byte>.          Value);
			Assert.AreEqual(default(char),           DefaultValue<char>.          Value);
			Assert.AreEqual(default(bool),           DefaultValue<bool>.          Value);
			Assert.AreEqual(default(sbyte),          DefaultValue<sbyte>.         Value);
			Assert.AreEqual(default(short),          DefaultValue<short>.         Value);
			Assert.AreEqual(default(long),           DefaultValue<long>.          Value);
			Assert.AreEqual(default(ushort),         DefaultValue<ushort>.        Value);
			Assert.AreEqual(default(ulong),          DefaultValue<ulong>.         Value);
			Assert.AreEqual(default(float),          DefaultValue<float>.         Value);
			Assert.AreEqual(default(double),         DefaultValue<double>.        Value);
			Assert.AreEqual(default(decimal),        DefaultValue<decimal>.       Value);
			Assert.AreEqual(default(DateTime),       DefaultValue<DateTime>.      Value);
			Assert.AreEqual(default(TimeSpan),       DefaultValue<TimeSpan>.      Value);
			Assert.AreEqual(default(DateTimeOffset), DefaultValue<DateTimeOffset>.Value);
			Assert.AreEqual(default(Guid),           DefaultValue<Guid>.          Value);
			Assert.AreEqual(default(string),         DefaultValue<string>.        Value);
		}

		[Test]
		public void Int()
		{
			Assert.AreEqual(0, DefaultValue<int>.Value);
			DefaultValue<int>.Value = 5;
			Assert.AreEqual(5, DefaultValue<int>.Value);
			DefaultValue<int>.Value = 0;
		}

		[Test]
		public void UInt()
		{
			Assert.AreEqual(0u, DefaultValue.GetValue(typeof(uint)));
			DefaultValue<uint>.Value = 10;
			Assert.AreEqual(10u, DefaultValue.GetValue(typeof(uint)));
			DefaultValue<uint>.Value = 0;
		}

		[Test]
		public void IntNullable()
		{
			Assert.AreEqual(null, DefaultValue<int?>.Value);
			DefaultValue<int?>.Value = 5;
			Assert.AreEqual(5, DefaultValue<int?>.Value);
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
			Assert.AreEqual(Enum1.Value1, DefaultValue<Enum1>.Value);
			DefaultValue<Enum1>.Value = Enum1.Value2;
			Assert.AreEqual(Enum1.Value2, DefaultValue<Enum1>.Value);
			DefaultValue<Enum1>.Value = Enum1.Value1;
		}

		[Test]
		public void EnumNullable()
		{
			Assert.AreEqual(null, DefaultValue<Enum1?>.Value);
			DefaultValue<Enum1?>.Value = Enum1.Value2;
			Assert.AreEqual(Enum1.Value2, DefaultValue<Enum1?>.Value);
			DefaultValue<Enum1?>.Value = null;
		}

		[Test]
		public void String()
		{
			Assert.AreEqual(null, DefaultValue<string>.Value);
			DefaultValue<string>.Value = "";
			Assert.AreEqual("", DefaultValue<string>.Value);
			DefaultValue<string>.Value = null;
		}
	}
}
