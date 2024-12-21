using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Globalization;

using LinqToDB.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace Tests.Common
{
	[TestFixture]
	public class ConvertTests : TestBase
	{
		[Test]
		public void SameType()
		{
			Assert.Multiple(() =>
			{
				Assert.That(ConvertTo<int>.From(1), Is.EqualTo(1));
				Assert.That(ConvertTo<string>.From("1"), Is.EqualTo("1"));
				Assert.That(Convert<int, int>.From(1), Is.EqualTo(1));
				Assert.That(Convert<string, string>.From("1"), Is.EqualTo("1"));
			});
		}

		[Test]
		public void SetExpression()
		{
			Convert<int,string>.Lambda = i => (i * 2).ToString();

			Assert.That(Convert<int,string>.From(2), Is.EqualTo("4"));

			Convert<int,string>.Expression = i => (i * 3).ToString();

			Assert.That(Convert<int,string>.From(3), Is.EqualTo("9"));

			Convert<int,string>.Lambda = null;

			Assert.That(Convert<int,string>.From(1), Is.EqualTo("1"));
		}

		[Test]
		public void ObjectToString()
		{
			Assert.That(Convert<int,string>.From(1), Is.EqualTo("1"));
		}

		[Test]
		public void ToObject()
		{
			Assert.That(ConvertTo<object>.From(1), Is.EqualTo(1));
		}

		enum Enum1
		{
			Value1,
			Value2
		}

		[Test]
		public void Nullable()
		{
			Assert.Multiple(() =>
			{
				Assert.That(Convert<int?, Enum1?>.From(null), Is.EqualTo(null));
				Assert.That(Convert<int?, int>.From(null), Is.EqualTo(0));
				Assert.That(Convert<int, int?>.From(10), Is.EqualTo(10));
				Assert.That(Convert<int?, Enum1>.From(null), Is.EqualTo(Enum1.Value1));
				Assert.That(Convert<Enum1, int?>.From(Enum1.Value2), Is.EqualTo(1));
				Assert.That(Convert<Enum1?, int?>.From(null), Is.EqualTo(null));
			});
		}

		[Test]
		public void Ctor()
		{
			Assert.That(Convert<int,decimal>.From(10), Is.EqualTo(10m));
		}

		sealed class TestData1
		{
			public int Value;

			public static implicit operator TestData1(int i)
			{
				return new TestData1 { Value = i };
			}
		}

		sealed class TestData2
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
			Assert.Multiple(() =>
			{
				Assert.That(ConvertTo<TestData1>.From(10).Value, Is.EqualTo(10));
				Assert.That(ConvertTo<TestData2>.From(10).Value, Is.EqualTo(10));
			});
		}

		[Test]
		public void Conversion()
		{
			Assert.Multiple(() =>
			{
				Assert.That(ConvertTo<int>.From(10.0), Is.EqualTo(10));
				Assert.That(ConvertTo<byte>.From(100), Is.EqualTo(100));
				Assert.That(ConvertTo<char>.From(0x10), Is.EqualTo('\x10'));
				Assert.That(ConvertTo<int>.From('\x10'), Is.EqualTo(0x10));
			});
		}

		[Test]
		public void Parse()
		{
			Assert.Multiple(() =>
			{
				Assert.That(ConvertTo<int>.From("10"), Is.EqualTo(10));
				Assert.That(ConvertTo<DateTime>.From("2012-1-1"), Is.EqualTo(new DateTime(2012, 1, 1)));
			});
		}

		[Test]
		public void ParseChar()
		{
			Assert.Multiple(() =>
			{
				Assert.That(ConvertTo<char>.From((string?)null), Is.EqualTo('\0'));
				Assert.That(ConvertTo<char>.From(""), Is.EqualTo('\0'));
			});
		}

		[Test]
		public void ToStringTest()
		{
			Convert<DateTime,string>.Expression = d => d.ToString(DateTimeFormatInfo.InvariantInfo);

			Assert.Multiple(() =>
			{
				Assert.That(ConvertTo<string>.From(10), Is.EqualTo("10"));
				Assert.That(ConvertTo<string>.From(new DateTime(2012, 1, 20, 16, 20, 30, 40, DateTimeKind.Utc)), Is.EqualTo("01/20/2012 16:20:30"));
			});

			Convert<DateTime,string>.Expression = null;
		}

		[Test]
		public void FromValue()
		{
			Assert.That(ConvertTo<int>.From(new SqlInt32(10)), Is.EqualTo(10));
		}

		[Test]
		public void ToBinary()
		{
			const string data = "emHMhse9zrLxj7O/";
			Assert.That(ConvertTo<Binary>.From(data).ToArray(), Is.EqualTo(Convert.FromBase64String(data)));
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
			Assert.Multiple(() =>
			{
				Assert.That(ConvertTo<Enum2?>.From((int?)1), Is.EqualTo(Enum2.Value1));
				Assert.That(ConvertTo<Enum2>.From(Enum2.Value1), Is.EqualTo(Enum2.Value1));
				Assert.That(ConvertTo<Enum2>.From(Enum3.Value2), Is.EqualTo(Enum2.Value2));
				Assert.That(ConvertTo<Enum2>.From(1), Is.EqualTo(Enum2.Value1));
				Assert.That(ConvertTo<Enum2>.From((int?)1), Is.EqualTo(Enum2.Value1));
				Assert.That(ConvertTo<Enum2?>.From(1), Is.EqualTo(Enum2.Value1));
				Assert.That(ConvertTo<Enum3>.From(1.0), Is.EqualTo(Enum3.Value1));
				Assert.That(ConvertTo<Enum3?>.From(1.0), Is.EqualTo(Enum3.Value1));
				Assert.That(ConvertTo<Enum3?>.From((double?)1.0), Is.EqualTo(Enum3.Value1));
				Assert.That(ConvertTo<Enum2>.From("1"), Is.EqualTo(Enum2.Value1));
				Assert.That(ConvertTo<Enum2>.From("+1"), Is.EqualTo(Enum2.Value1));

				Assert.That(ConvertTo<string>.From(Enum2.Value1), Is.EqualTo("Value1"));
				Assert.That(ConvertTo<Enum2>.From("Value1"), Is.EqualTo(Enum2.Value1));
				Assert.That(ConvertTo<Enum2>.From("value2"), Is.EqualTo(Enum2.Value2));
			});
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
			Assert.Multiple(() =>
			{
				Assert.That(ConvertTo<int>.From(Enum4.Value1), Is.EqualTo(15));
				Assert.That(ConvertTo<int>.From(Enum4.Value2), Is.EqualTo(25));
				Assert.That(ConvertTo<int>.From(Enum4.Value3), Is.EqualTo(0));

				Assert.That(ConvertTo<string>.From(Enum4.Value1), Is.EqualTo("115"));
				Assert.That(ConvertTo<string>.From(Enum4.Value2), Is.EqualTo("125"));
				Assert.That(ConvertTo<string>.From(Enum4.Value3), Is.EqualTo(null));
			});
		}

		[Test]
		public void ConvertFromEnum2()
		{
			var cf = MappingSchema.Default.GetConverter<Enum4,int>()!;

			Assert.Multiple(() =>
			{
				Assert.That(cf(Enum4.Value1), Is.EqualTo(15));
				Assert.That(cf(Enum4.Value2), Is.EqualTo(25));
				Assert.That(cf(Enum4.Value3), Is.EqualTo(0));
			});
		}

		[Test]
		public void ConvertFromEnum3()
		{
			var cf = new MappingSchema("1").GetConverter<Enum4,int>()!;

			Assert.Multiple(() =>
			{
				Assert.That(cf(Enum4.Value1), Is.EqualTo(15));
				Assert.That(cf(Enum4.Value2), Is.EqualTo(25));
				Assert.That(cf(Enum4.Value3), Is.EqualTo(35));
			});
		}

		[Test]
		public void ConvertFromEnum4()
		{
			var cf = MappingSchema.Default.GetConverter<Enum4,int>()!;

			Assert.That(cf(Enum4.Value3), Is.EqualTo(0));

			cf = new MappingSchema("1").GetConverter<Enum4,int>()!;

			Assert.That(cf(Enum4.Value3), Is.EqualTo(35));
		}

		[Test]
		public void ConvertToEnum1()
		{
			Assert.That(ConvertTo<Enum4>.From(25), Is.EqualTo(Enum4.Value2));
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
			Assert.That(ConvertTo<Enum6>.From(Enum5.Value1), Is.EqualTo(Enum6.Value2));
		}

		[Test]
		public void ConvertToEnum3()
		{
			Assert.That(ConvertTo<Enum5>.From(Enum6.Value1), Is.EqualTo(Enum5.Value2));
		}

		[Test]
		public void ConvertToEnum7()
		{
			Assert.That(ConvertTo<Enum5>.From((Enum6?)Enum6.Value1), Is.EqualTo(Enum5.Value2));
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
			Assert.That(ConvertTo<Enum8>.From(Enum7.Value1), Is.EqualTo(Enum8.Value2));
		}

		[Test]
		public void ConvertToEnum9()
		{
			Assert.That(ConvertTo<Enum7>.From(Enum8.Value1), Is.EqualTo(Enum7.Value2));
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
			Assert.Multiple(() =>
			{
				Assert.That(ConvertTo<Enum9>.From(1), Is.EqualTo(Enum9.Value1));
				Assert.That(ConvertTo<Enum9>.From(10), Is.EqualTo(Enum9.Value1));
				Assert.That(ConvertTo<Enum9>.From(2), Is.EqualTo(Enum9.Value2));
			});
		}

		[Test]
		public void ConvertFromEnum5()
		{
			Assert.Multiple(() =>
			{
				Assert.That(ConvertTo<int>.From(Enum9.Value1), Is.EqualTo(10));
				Assert.That(ConvertTo<int>.From(Enum9.Value2), Is.EqualTo(2));
			});
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
			Assert.Multiple(() =>
			{
				Assert.That(ConvertTo<Enum11>.From(Enum10.Value2), Is.EqualTo(Enum11.Value1));
				Assert.That(ConvertTo<Enum11>.From(Enum10.Value1), Is.EqualTo(Enum11.Value2));
				Assert.That(ConvertTo<Enum11>.From(Enum10.Value3), Is.EqualTo(Enum11.Value3));
			});
		}

		[Test]
		public void ConvertToEnum12()
		{
			var cf = new MappingSchema("1").GetConverter<Enum10,Enum11>()!;

			Assert.Throws<LinqToDBConvertException>(
				() => cf(Enum10.Value2),
				"Mapping ambiguity. 'Tests.Common.ConvertTest+Enum10.Value1' can be mapped to either 'Tests.Common.ConvertTest+Enum11.Value2' or 'Tests.Common.ConvertTest+Enum11.Value3'.");
			Assert.Throws<LinqToDBConvertException>(
				() => cf(Enum10.Value1),
				"Mapping ambiguity. 'Tests.Common.ConvertTest+Enum10.Value1' can be mapped to either 'Tests.Common.ConvertTest+Enum11.Value2' or 'Tests.Common.ConvertTest+Enum11.Value3'.");
		}

		[Test]
		public void ConvertToEnum13()
		{
			var cf = new MappingSchema("2").GetConverter<Enum10,Enum11>()!;

			Assert.Multiple(() =>
			{
				Assert.That(cf(Enum10.Value2), Is.EqualTo(Enum11.Value1));
				Assert.That(cf(Enum10.Value1), Is.EqualTo(Enum11.Value2));
				Assert.That(cf(Enum10.Value4), Is.EqualTo(Enum11.Value3));
			});
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

		[Test]
		public void ConvertToEnum14()
		{
			Assert.Throws<LinqToDBConvertException>(
				() => ConvertTo<Enum13>.From(Enum12.Value2),
				"Mapping ambiguity. 'Tests.Common.ConvertTest+Enum12.Value2' can be mapped to either 'Tests.Common.ConvertTest+Enum13.Value1' or 'Tests.Common.ConvertTest+Enum13.Value3'.");
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
			Assert.Multiple(() =>
			{
				Assert.That(ConvertTo<string>.From((Enum14?)Enum14.AA), Is.EqualTo("A"));
				Assert.That(ConvertTo<string>.From((Enum14?)null), Is.EqualTo(null));

				Assert.That(ConvertTo<string>.From((Enum14?)Enum14.BB), Is.EqualTo("B"));

				Assert.That(new MappingSchema().GetConverter<Enum14?, string>()!(Enum14.BB), Is.EqualTo("B"));
				Assert.That(new MappingSchema("1").GetConverter<Enum14?, string>()!(Enum14.BB), Is.EqualTo("C"));
			});
		}

		[Test]
		public void ConvertToNullableEnum1()
		{
			Assert.Multiple(() =>
			{
				Assert.That(new MappingSchema().GetConverter<string, Enum14?>()!("B"), Is.EqualTo(Enum14.BB));
				Assert.That(new MappingSchema("1").GetConverter<string, Enum14?>()!("C"), Is.EqualTo(Enum14.BB));
			});
		}

		enum Enum15
		{
			[MapValue(10)] AA,
			[MapValue(20)] BB,
		}

		[Test]
		public void ConvertFromNullableEnum2()
		{
			Assert.Multiple(() =>
			{
				Assert.That(ConvertTo<int>.From((Enum15?)Enum15.AA), Is.EqualTo(10));
				Assert.That(ConvertTo<int>.From((Enum15?)null), Is.EqualTo(0));
				Assert.That(ConvertTo<int?>.From((Enum15?)null), Is.EqualTo(null));
			});
		}

		[Test]
		public void NullableParameterInOperatorConvert()
		{
			var (convertFromDecimalLambdaExpression1, convertFromDecimalLambdaExpression2, b1)
				= ConvertBuilder.GetConverter(null, typeof(decimal), typeof(CustomMoneyType));

			var convertFromDecimalFunc1 = (Func<decimal, CustomMoneyType>)convertFromDecimalLambdaExpression1.CompileExpression();
			var convertFromDecimalFunc2 = (Func<decimal, CustomMoneyType>)convertFromDecimalLambdaExpression2!.CompileExpression();

			Assert.Multiple(() =>
			{
				Assert.That(convertFromDecimalFunc1(1.11m), Is.EqualTo(new CustomMoneyType { Amount = 1.11m }));
				Assert.That(convertFromDecimalFunc2(1.11m), Is.EqualTo(new CustomMoneyType { Amount = 1.11m }));
			});

			var (convertFromNullableDecimalLambdaExpression1, convertFromNullableDecimalLambdaExpression2, b2)
				= ConvertBuilder.GetConverter(null, typeof(decimal?), typeof(CustomMoneyType));

			var convertFromNullableDecimalFunc1 = (Func<decimal?, CustomMoneyType>)convertFromNullableDecimalLambdaExpression1.CompileExpression();
			var convertFromNullableDecimalFunc2 = (Func<decimal?, CustomMoneyType>)convertFromNullableDecimalLambdaExpression2!.CompileExpression();

			Assert.Multiple(() =>
			{
				Assert.That(convertFromNullableDecimalFunc1(1.11m), Is.EqualTo(new CustomMoneyType { Amount = 1.11m }));
				Assert.That(convertFromNullableDecimalFunc2(1.11m), Is.EqualTo(new CustomMoneyType { Amount = 1.11m }));

				Assert.That(convertFromNullableDecimalFunc1(null), Is.EqualTo(new CustomMoneyType { Amount = null }));
				Assert.That(convertFromNullableDecimalFunc2(null), Is.EqualTo(new CustomMoneyType { Amount = null }));
			});
		}

		private struct CustomMoneyType
		{
			public decimal? Amount;

			public static explicit operator CustomMoneyType(decimal? amount) => new () { Amount = amount };
		}
	}
}
