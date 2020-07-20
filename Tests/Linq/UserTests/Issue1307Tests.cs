﻿using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Informix;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1307Tests : TestBase
	{
		public class DateTimeTestTable
		{
			public DateTime DateTimeField { get; set; }
		}

		public enum DateTimeQuantifiers
		{
			Year,
			Month,
			Day,
			Hour,
			Minute,
			Second,
			Fraction,
			Fraction1,
			Fraction2,
			Fraction3,
			Fraction4,
			Fraction5
		}

		public static IEnumerable<Tuple<DateTimeQuantifiers, DateTimeQuantifiers>> DateTimePairs
		{
			get
			{
				var values = Enum.GetValues(typeof(DateTimeQuantifiers)).Cast<int>().OrderBy(_ => _).Cast<DateTimeQuantifiers>().ToArray();

				for (var i = 0; values[i] != DateTimeQuantifiers.Fraction1; i++)
				{
					for (var j = i; j < values.Length; j++)
					{
						yield return Tuple.Create(values[i], values[j]);
					}
				}
			}
		}

		[Test]
		public void TestDateTime(
			[IncludeDataSources(true, TestProvName.AllInformix)] string context,
			[Values] bool inlineParameters,
			[ValueSource(nameof(DateTimePairs))] Tuple<DateTimeQuantifiers, DateTimeQuantifiers> quantifiers)
		{
			var ms = new MappingSchema();
			ms
				.GetFluentMappingBuilder()
					.Entity<DateTimeTestTable>()
						.Property(t => t.DateTimeField)
							.HasDbType($"datetime {GetQuantifierName(quantifiers.Item1)} to {GetQuantifierName(quantifiers.Item2)}");

			var isIDS = IsIDSProvider(context);

			using (var db = GetDataContext(context, ms))
			{
				using (var tbl = db.CreateLocalTable<DateTimeTestTable>())
				{
					db.InlineParameters = inlineParameters;

					var input    = new DateTime(2134, 5, 21, 13, 45, 43).AddTicks(1234567);
					var expected = GetExpectedDatetime(isIDS, input, quantifiers.Item1, quantifiers.Item2);

					db.GetTable<DateTimeTestTable>().Insert(() => new DateTimeTestTable()
					{
						DateTimeField = input
					});

					var actual = db.GetTable<DateTimeTestTable>().Single().DateTimeField;

					Assert.AreEqual(expected, actual);
				}
			}
		}

		private static string GetQuantifierName(DateTimeQuantifiers quantifier)
		{
			return quantifier switch
			{
				DateTimeQuantifiers.Fraction1 => "fraction(1)",
				DateTimeQuantifiers.Fraction2 => "fraction(2)",
				DateTimeQuantifiers.Fraction3 => "fraction(3)",
				DateTimeQuantifiers.Fraction4 => "fraction(4)",
				DateTimeQuantifiers.Fraction5 => "fraction(5)",
				_                             => quantifier.ToString(),
			};
		}

		private DateTime GetExpectedDatetime(bool isIDS, DateTime input, DateTimeQuantifiers largest, DateTimeQuantifiers smallest)
		{
			var year   = 1200;
			var month  = 1;
			var day    = 1;
			var hour   = 0;
			var minute = 0;
			var second = 0;
			var ticks  = 0;

			if (isIDS && largest >= DateTimeQuantifiers.Hour && smallest <= DateTimeQuantifiers.Second)
			{
				// this selectivity for default year doesn't make any sense, but this is how IDS driver behaves
				year = 1;
			}

			if (largest == DateTimeQuantifiers.Year)
			{
				year = input.Year;
			}

			if (largest <= DateTimeQuantifiers.Month && smallest >= DateTimeQuantifiers.Month)
			{
				month = input.Month;
			}

			if (largest <= DateTimeQuantifiers.Day && smallest >= DateTimeQuantifiers.Day)
			{
				day = input.Day;
			}

			if (largest <= DateTimeQuantifiers.Hour && smallest >= DateTimeQuantifiers.Hour)
			{
				hour = input.Hour;
			}

			if (largest <= DateTimeQuantifiers.Minute && smallest >= DateTimeQuantifiers.Minute)
			{
				minute = input.Minute;
			}

			if (largest <= DateTimeQuantifiers.Second && smallest >= DateTimeQuantifiers.Second)
			{
				second = input.Second;
			}

			if (largest <= DateTimeQuantifiers.Fraction5 && smallest >= DateTimeQuantifiers.Fraction)
			{
				var digits = 3;
				if (smallest != DateTimeQuantifiers.Fraction)
				{
					digits = smallest - DateTimeQuantifiers.Fraction;
				}

				ticks = ((int)(input.Ticks % 10000000) / (int)Math.Pow(10, 7 - digits)) * (int)Math.Pow(10, 7 - digits);
			}

			return new DateTime(year, month, day, hour, minute, second).AddTicks(ticks);
		}

		[Table("Issue1307Tests")]
		public class Table
		{
			[Column(IsPrimaryKey = true, DbType = "serial")]
			public int Id { get; set; }

			[Column(Length = 255, DataType = DataType.VarChar)]
			public string? Content { get; set; }
		}

		// server and client should run with DB_LOCALE=en_us.utf8;CLIENT_LOCALE=en_us.utf8 options
		// and database should be created with same locale
		//[Explicit("Could fail on non-utf8 locales")]
		[SkipCI("Used docker image needs locale configuration")]
		[Test]
		public void Test_Insert([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (var tbl = db.CreateLocalTable<Table>())
			{
				var test = new Table()
				{
					Content = "中文字中文字中文字"
				};

				db.Insert(test);
			}
		}

		//[Explicit("Could fail on non-utf8 locales")]
		[SkipCI("Used docker image needs locale configuration")]
		[Test]
		public void Test_Update([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (var tbl = db.CreateLocalTable<Table>())
			{
				var test = new Table()
				{
					Content = "中文字中文字中文字"
				};

				db.Update(test);
			}
		}

		//[Explicit("Could fail on non-utf8 locales")]
		[SkipCI("Used docker image needs locale configuration")]
		[Test]
		public void Test_InsertOrUpdate([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (var tbl = db.CreateLocalTable<Table>())
			{
				var test = new Table()
				{
					Content = "中文字中文字中文字"
				};

				db.InsertOrReplace(test);
			}
		}

		//[Explicit("Could fail on non-utf8 locales")]
		[SkipCI("Used docker image needs locale configuration")]
		[Test]
		public void Test_Inline([IncludeDataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (var tbl = db.CreateLocalTable<Table>())
			{
				var test = new Table()
				{
					Content = "中文字中文字中文字"
				};

				db.Insert(test);
			}
		}
	}
}
