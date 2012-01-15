using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Globalization;
using System.Text;

using LinqToDB_Temp.Common;

using NUnit.Framework;

namespace Tests.Common
{
	[TestFixture]
	public class ConvertTest : TestBase
	{
		[Test]
		public void SameType()
		{
			Assert.AreEqual(1,   ConvertTo<int>.From(1));
			Assert.AreEqual("1", ConvertTo<string>.From("1"));
			Assert.AreEqual(1,   Convert<int,int>.From(1));
			Assert.AreEqual("1", Convert<string,string>.From("1"));
		}

		[Test]
		public void SetExpression()
		{
			Convert<int, string>.Lambda = i => (i * 2).ToString();

			Assert.AreEqual("4", Convert<int,string>.From(2));

			Convert<int, string>.Expression = i => (i * 3).ToString();

			Assert.AreEqual("9", Convert<int,string>.From(3));

			Convert<int, string>.Lambda = null;

			Assert.AreEqual("1", Convert<int,string>.From(1));
		}

		[Test]
		public void ObjectToString()
		{
			Assert.AreEqual("1", Convert<int,string>.From(1));
		}

		[Test]
		public void ToObject()
		{
			Assert.AreEqual(1, ConvertTo<object>.From(1));
		}

		enum Enum1
		{
			Value1,
			Value2
		}

		[Test]
		public void Nullable()
		{
			Assert.AreEqual(null,         Convert<int?,  Enum1?>.From(null));
			Assert.AreEqual(0,            Convert<int?,  int>.   From(null));
			Assert.AreEqual(10,           Convert<int,   int?>.  From(10));
			Assert.AreEqual(Enum1.Value1, Convert<int?,  Enum1>. From(null));
			Assert.AreEqual(1,            Convert<Enum1, int?>.  From(Enum1.Value2));
			Assert.AreEqual(null,         Convert<Enum1?,int?>.  From(null));
		}

		[Test]
		public void Ctor()
		{
			Assert.AreEqual(10m, Convert<int,decimal>.From(10));
		}

		class TestData1
		{
			public int Value;

			public static implicit operator TestData1(int i)
			{
				return new TestData1 { Value = i };
			}
		}

		class TestData2
		{
			public int Value;

			public static explicit operator TestData2(int i)
			{
				return new TestData2 { Value = i };
			}
		}

		[Test]
		public void ConvertTo()
		{
			Assert.AreEqual(10, ConvertTo<TestData1>.From(10).Value);
			Assert.AreEqual(10, ConvertTo<TestData2>.From(10).Value);
		}

		[Test]
		public void Conversion()
		{
			Assert.AreEqual(10,     ConvertTo<int>. From(10.0));
			Assert.AreEqual(100,    ConvertTo<byte>.From(100));
			Assert.AreEqual('\x10', ConvertTo<char>.From(0x10));
			Assert.AreEqual(0x10,   ConvertTo<int>. From('\x10'));
		}

		[Test]
		public void Parse()
		{
			Assert.AreEqual(10,                       ConvertTo<int>.     From("10"));
			Assert.AreEqual(new DateTime(2012, 1, 1), ConvertTo<DateTime>.From("2012-1-1"));
		}

		[Test]
		public void ToStringTest()
		{
			Convert<DateTime,string>.Expression = d => d.ToString(DateTimeFormatInfo.InvariantInfo);

			Assert.AreEqual("10",                  ConvertTo<string>.From(10));
			Assert.AreEqual("01/20/2012 16:20:30", ConvertTo<string>.From(new DateTime(2012, 1, 20, 16, 20, 30, 40, DateTimeKind.Utc)));

			Convert<DateTime,string>.Expression = null;
		}

		[Test]
		public void FromValue()
		{
			Assert.AreEqual(10, ConvertTo<int>.From(new SqlInt32(10)));
		}

		[Test]
		public void ToBinary()
		{
			const string data = "za\u0306\u01FD\u03B2\uD8FF\uDCFF";
			Assert.AreEqual(Encoding.UTF8.GetBytes(data), ConvertTo<Binary>.From(data).ToArray());
		}
	}
}
