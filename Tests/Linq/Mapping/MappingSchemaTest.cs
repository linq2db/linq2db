using System;
using System.Globalization;

using LinqToDB_Temp.Common;
using LinqToDB_Temp.Mapping;

using NUnit.Framework;

namespace Tests.Mapping
{
	public class MappingSchemaTest : TestBase
	{
		[Test]
		public void DefaultValue()
		{
			var ms = new MappingSchema();

			ms.SetDefaultValue(-1);

			var c = ms.GetConverter<int?,int>();

			Assert.AreEqual(-1, c(null));
		}

		[Test]
		public void BaseSchema1()
		{
			var ms1 = new MappingSchema();
			var ms2 = new MappingSchema(ms1);

			ms1.SetDefaultValue(-1);
			ms2.SetDefaultValue(-2);

			var c1 = ms1.GetConverter<int?,int>();
			var c2 = ms2.GetConverter<int?,int>();

			Assert.AreEqual(-1, c1(null));
			Assert.AreEqual(-2, c2(null));
		}

		[Test]
		public void BaseSchema2()
		{
			var ms1 = new MappingSchema();
			var ms2 = new MappingSchema(ms1);

			Convert<DateTime,string>.Lambda = d => d.ToString(DateTimeFormatInfo.InvariantInfo);
			ms1.SetConverter<DateTime,string>(d => d.ToString(new CultureInfo("en-US", false).DateTimeFormat));
			ms2.SetConverter<DateTime,string>(d => d.ToString(new CultureInfo("ru-RU", false).DateTimeFormat));

			{
				var c0 = Convert<DateTime,string>.Lambda;
				var c1 = ms1.GetConverter<DateTime,string>();
				var c2 = ms2.GetConverter<DateTime,string>();

				Assert.AreEqual("01/20/2012 16:30:40",  c0(new DateTime(2012, 1, 20, 16, 30, 40, 50, DateTimeKind.Utc)));
				Assert.AreEqual("1/20/2012 4:30:40 PM", c1(new DateTime(2012, 1, 20, 16, 30, 40, 50, DateTimeKind.Utc)));
				Assert.AreEqual("20.01.2012 16:30:40",  c2(new DateTime(2012, 1, 20, 16, 30, 40, 50, DateTimeKind.Utc)));
			}

			Convert<string,DateTime>.Expression = s => DateTime.Parse(s, DateTimeFormatInfo.InvariantInfo);
			ms1.SetConvertExpression<string,DateTime>(s => DateTime.Parse(s, new CultureInfo("en-US", false).DateTimeFormat));
			ms2.SetConvertExpression<string,DateTime>(s => DateTime.Parse(s, new CultureInfo("ru-RU", false).DateTimeFormat));

			{
				var c0 = Convert<string,DateTime>.Lambda;
				var c1 = ms1.GetConverter<string, DateTime>();
				var c2 = ms2.GetConverter<string,DateTime>();

				Assert.AreEqual(new DateTime(2012, 1, 20, 16, 30, 40), c0("01/20/2012 16:30:40"));
				Assert.AreEqual(new DateTime(2012, 1, 20, 16, 30, 40), c1("1/20/2012 4:30:40 PM"));
				Assert.AreEqual(new DateTime(2012, 1, 20, 16, 30, 40), c2("20.01.2012 16:30:40"));
			}
		}

		[Test]
		public void CultureInfo()
		{
			var ms = new MappingSchema();

			ms.ConvertInfo.SetCultureInfo(new CultureInfo("ru-RU", false));

			Assert.AreEqual("20.01.2012 16:30:40",                 ms.GetConverter<DateTime,string>()(new DateTime(2012, 1, 20, 16, 30, 40)));
			Assert.AreEqual(new DateTime(2012, 1, 20, 16, 30, 40), ms.GetConverter<string,DateTime>()("20.01.2012 16:30:40"));
			Assert.AreEqual("100000,999",                          ms.GetConverter<decimal,string> ()(100000.999m));
			Assert.AreEqual(100000.999m,                           ms.GetConverter<string,decimal> ()("100000,999"));
			Assert.AreEqual(100000.999m,                           ConvertTo<decimal>.From("100000.999"));
			Assert.AreEqual("100000,999",                          ms.GetConverter<double,string>  ()(100000.999));
			Assert.AreEqual(100000.999,                            ms.GetConverter<string,double>  ()("100000,999"));
		}

		private object OnDefaultValueGetter(Type t)
		{
			return t;
		}
	}
}
