using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Globalization;

using LinqToDB.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Common
{
	[TestFixture]
	public class ConvertTest : TestBase
	{
		[Test]
		public void SameType()
		{
			Assert.AreEqual(1,   ConvertTo<int>.        From(1));
			Assert.AreEqual("1", ConvertTo<string>.     From("1"));
			Assert.AreEqual(1,   Convert<int,int>      .From(1));
			Assert.AreEqual("1", Convert<string,string>.From("1"));
		}

		[Test]
		public void SetExpression()
		{
			Convert<int,string>.Lambda = i => (i * 2).ToString();

			Assert.AreEqual("4", Convert<int,string>.From(2));

			Convert<int,string>.Expression = i => (i * 3).ToString();

			Assert.AreEqual("9", Convert<int,string>.From(3));

			Convert<int,string>.Lambda = null;

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
		public void ParseChar()
		{
			Assert.AreEqual('\0', ConvertTo<char>.From((string)null));
			Assert.AreEqual('\0', ConvertTo<char>.From(""));
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
			const string data = "emHMhse9zrLxj7O/";
			Assert.AreEqual(Convert.FromBase64String(data), ConvertTo<Binary>.From(data).ToArray());
		}

		enum Enum2
		{
			Value1 = 1,
			Value2 = 2,
		}

		enum Enum3
		{
			Value1 = 1,
			Value2 = 2,
		}

		[Test]
		public void EnumValue()
		{
			Assert.AreEqual(Enum2.Value1, ConvertTo<Enum2>. From(Enum2.Value1));
			Assert.AreEqual(Enum2.Value2, ConvertTo<Enum2>. From(Enum3.Value2));
			Assert.AreEqual(Enum2.Value1, ConvertTo<Enum2>. From(1));
			Assert.AreEqual(Enum2.Value1, ConvertTo<Enum2>. From((int?)1));
			Assert.AreEqual(Enum2.Value1, ConvertTo<Enum2?>.From((int?)1));
			Assert.AreEqual(Enum2.Value1, ConvertTo<Enum2?>.From(1));
			Assert.AreEqual(Enum3.Value1, ConvertTo<Enum3>. From(1.0));
			Assert.AreEqual(Enum3.Value1, ConvertTo<Enum3?>.From(1.0));
			Assert.AreEqual(Enum3.Value1, ConvertTo<Enum3?>.From((double?)1.0));
			Assert.AreEqual(Enum2.Value1, ConvertTo<Enum2>. From("1"));
			Assert.AreEqual(Enum2.Value1, ConvertTo<Enum2>. From("+1"));

			Assert.AreEqual("Value1", ConvertTo<string>.From(Enum2.Value1));
			Assert.AreEqual(Enum2.Value1, ConvertTo<Enum2>. From("Value1"));
			Assert.AreEqual(Enum2.Value2, ConvertTo<Enum2>. From("value2"));
		}

		enum Enum4
		{
			[MapValue(15)]
			[MapValue("115")]
			Value1,

			[MapValue(25)]
			[MapValue("125")]
			Value2,

			[MapValue(null)]
			[MapValue(35, Configuration = "1")]
			Value3,
		}

		[Test]
		public void ConvertFromEnum1()
		{
			Assert.AreEqual(15,    ConvertTo<int>.   From(Enum4.Value1));
			Assert.AreEqual(25,    ConvertTo<int>.   From(Enum4.Value2));
			Assert.AreEqual(0,     ConvertTo<int>.   From(Enum4.Value3));

			Assert.AreEqual("115", ConvertTo<string>.From(Enum4.Value1));
			Assert.AreEqual("125", ConvertTo<string>.From(Enum4.Value2));
			Assert.AreEqual(null,  ConvertTo<string>.From(Enum4.Value3));
		}

		[Test]
		public void ConvertFromEnum2()
		{
			var cf = MappingSchema.Default.GetConverter<Enum4,int>();

			Assert.AreEqual(15, cf(Enum4.Value1));
			Assert.AreEqual(25, cf(Enum4.Value2));
			Assert.AreEqual(0,  cf(Enum4.Value3));
		}

		[Test]
		public void ConvertFromEnum3()
		{
			var cf = new MappingSchema("1").GetConverter<Enum4,int>();

			Assert.AreEqual(15, cf(Enum4.Value1));
			Assert.AreEqual(25, cf(Enum4.Value2));
			Assert.AreEqual(35, cf(Enum4.Value3));
		}

		[Test]
		public void ConvertFromEnum4()
		{
			var cf = MappingSchema.Default.GetConverter<Enum4,int>();

			Assert.AreEqual(0,  cf(Enum4.Value3));

			cf = new MappingSchema("1").GetConverter<Enum4,int>();

			Assert.AreEqual(35, cf(Enum4.Value3));
		}

		[Test]
		public void ConvertToEnum1()
		{
			Assert.AreEqual(Enum4.Value2, ConvertTo<Enum4>.From(25));
		}

		enum Enum5
		{
			[MapValue(Enum6.Value2)] Value1,
			[MapValue(Enum6.Value1)] Value2,
		}

		enum Enum6
		{
			Value1,
			Value2,
		}

		[Test]
		public void ConvertToEnum2()
		{
			Assert.AreEqual(Enum6.Value2, ConvertTo<Enum6>.From(Enum5.Value1));
		}

		[Test]
		public void ConvertToEnum3()
		{
			Assert.AreEqual(Enum5.Value2, ConvertTo<Enum5>.From(Enum6.Value1));
		}

		[Test]
		public void ConvertToEnum7()
		{
			Assert.AreEqual(Enum5.Value2, ConvertTo<Enum5>.From((Enum6?)Enum6.Value1));
		}

		enum Enum7
		{
			[MapValue(1)]   Value1,
			[MapValue("2")] Value2,
		}

		enum Enum8
		{
			[MapValue("2")] Value1,
			[MapValue(1)]   Value2,
		}

		[Test]
		public void ConvertToEnum8()
		{
			Assert.AreEqual(Enum8.Value2, ConvertTo<Enum8>.From(Enum7.Value1));
		}

		[Test]
		public void ConvertToEnum9()
		{
			Assert.AreEqual(Enum7.Value2, ConvertTo<Enum7>.From(Enum8.Value1));
		}

		enum Enum9
		{
			[MapValue(1)]
			[MapValue(10, true)]
			Value1,

			[MapValue(2)]
			Value2,
		}

		[Test]
		public void ConvertToEnum10()
		{
			Assert.AreEqual(Enum9.Value1, ConvertTo<Enum9>.From(1));
			Assert.AreEqual(Enum9.Value1, ConvertTo<Enum9>.From(10));
			Assert.AreEqual(Enum9.Value2, ConvertTo<Enum9>.From(2));
		}

		[Test]
		public void ConvertFromEnum5()
		{
			Assert.AreEqual(10, ConvertTo<int>.From(Enum9.Value1));
			Assert.AreEqual( 2, ConvertTo<int>.From(Enum9.Value2));
		}

		enum Enum10
		{
			[MapValue(1)]
			[MapValue(3)]
			Value1,

			[MapValue("2")]
			Value2,

			[MapValue('3')]
			Value3,

			[MapValue(5)]
			Value4,
		}

		enum Enum11
		{
			[MapValue("2")]
			Value1,

			[MapValue(1)]
			Value2,

			[MapValue("1", 1)]
			[MapValue("2", 5)]
			[MapValue('3')]
			Value3,
		}

		[Test]
		public void ConvertToEnum11()
		{
			Assert.AreEqual(Enum11.Value1, ConvertTo<Enum11>.From(Enum10.Value2));
			Assert.AreEqual(Enum11.Value2, ConvertTo<Enum11>.From(Enum10.Value1));
			Assert.AreEqual(Enum11.Value3, ConvertTo<Enum11>.From(Enum10.Value3));
		}

		[Test, ExpectedException(typeof(LinqToDBConvertException), ExpectedMessage = "Mapping ambiguity. 'Tests.Common.ConvertTest+Enum10.Value1' can be mapped to either 'Tests.Common.ConvertTest+Enum11.Value2' or 'Tests.Common.ConvertTest+Enum11.Value3'.")]
		public void ConvertToEnum12()
		{
			var cf = new MappingSchema("1").GetConverter<Enum10,Enum11>();

			Assert.AreEqual(Enum11.Value1, cf(Enum10.Value2));
			Assert.AreEqual(Enum11.Value2, cf(Enum10.Value1));
			Assert.AreEqual(Enum11.Value3, cf(Enum10.Value1));
		}

		[Test]
		public void ConvertToEnum13()
		{
			var cf = new MappingSchema("2").GetConverter<Enum10,Enum11>();

			Assert.AreEqual(Enum11.Value1, cf(Enum10.Value2));
			Assert.AreEqual(Enum11.Value2, cf(Enum10.Value1));
			Assert.AreEqual(Enum11.Value3, cf(Enum10.Value4));
		}

		enum Enum12
		{
			[MapValue(1)]
			[MapValue(3)]
			Value1,

			[MapValue("2")]
			[MapValue('3')]
			Value2,
		}

		enum Enum13
		{
			[MapValue("2")]
			Value1,

			[MapValue(1)]
			Value2,

			[MapValue("1", 1)]
			[MapValue('3')]
			Value3,
		}

		[Test, ExpectedException(typeof(LinqToDBConvertException), ExpectedMessage = "Mapping ambiguity. 'Tests.Common.ConvertTest+Enum12.Value2' can be mapped to either 'Tests.Common.ConvertTest+Enum13.Value1' or 'Tests.Common.ConvertTest+Enum13.Value3'.")]
		public void ConvertToEnum14()
		{
			Assert.AreEqual(Enum13.Value3, ConvertTo<Enum13>.From(Enum12.Value2));
		}

		enum Enum14
		{
			[MapValue("A")] AA,
			[MapValue("1", "C")]
			[MapValue("B")] BB,
		}

		[Test]
		public void ConvertFromNullableEnum1()
		{
			Assert.AreEqual("A",  ConvertTo<string>.From((Enum14?)Enum14.AA));
			Assert.AreEqual(null, ConvertTo<string>.From((Enum14?)null));

			Assert.AreEqual("B",  ConvertTo<string>.From((Enum14?)Enum14.BB));

			Assert.AreEqual("B",  new MappingSchema().   GetConverter<Enum14?,string>()(Enum14.BB));
			Assert.AreEqual("C",  new MappingSchema("1").GetConverter<Enum14?,string>()(Enum14.BB));
		}

		[Test]
		public void ConvertToNullableEnum1()
		{
			Assert.AreEqual(Enum14.BB,  new MappingSchema().   GetConverter<string,Enum14?>()("B"));
			Assert.AreEqual(Enum14.BB,  new MappingSchema("1").GetConverter<string,Enum14?>()("C"));
		}

		enum Enum15
		{
			[MapValue(10)] AA,
			[MapValue(20)] BB,
		}

		[Test]
		public void ConvertFromNullableEnum2()
		{
			Assert.AreEqual(10,   ConvertTo<int>. From((Enum15?)Enum15.AA));
			Assert.AreEqual(0,    ConvertTo<int>. From((Enum15?)null));
			Assert.AreEqual(null, ConvertTo<int?>.From((Enum15?)null));
		}
	}
}
