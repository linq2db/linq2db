using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

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

		[Test, Combinatorial]
		public void TestDateTime(
			[IncludeDataSources(ProviderName.Informix)] string context,
			[Values] bool inlineParameters,
			[ValueSource(nameof(DateTimePairs))] Tuple<DateTimeQuantifiers, DateTimeQuantifiers> quantifiers)
		{
			using (var db = GetDataContext(context))
			{
				db.MappingSchema
					.GetFluentMappingBuilder()
					.Entity<DateTimeTestTable>()
					.Property(t => t.DateTimeField)
					.HasDbType($"datetime {GetQuantifierName(quantifiers.Item1)} to {GetQuantifierName(quantifiers.Item2)}");

				using (var tbl = db.CreateLocalTable<DateTimeTestTable>())
				{
					db.InlineParameters = inlineParameters;

					var input = new DateTime(2134, 5, 21, 13, 45, 43).AddTicks(1234567);
					var expected = GetExpectedDatetime(input, quantifiers.Item1, quantifiers.Item2);

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
			switch (quantifier)
			{
				case DateTimeQuantifiers.Fraction1: return "fraction(1)";
				case DateTimeQuantifiers.Fraction2: return "fraction(2)";
				case DateTimeQuantifiers.Fraction3: return "fraction(3)";
				case DateTimeQuantifiers.Fraction4: return "fraction(4)";
				case DateTimeQuantifiers.Fraction5: return "fraction(5)";
			}

			return quantifier.ToString();
		}

		private static DateTime GetExpectedDatetime(DateTime input, DateTimeQuantifiers largest, DateTimeQuantifiers smallest)
		{
			var year = 1200;
			var month = 1;
			var day = 1;
			var hour = 0;
			var minute = 0;
			var second = 0;
			var ticks = 0;

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
			public string Content { get; set; }
		}

		// server and client should run with DB_LOCALE=en_us.utf8;CLIENT_LOCALE=en_us.utf8 options
		// and database should be created with same locale
		//[Explicit("Could fail on non-utf8 locales")]
		[Test, IncludeDataContextSource(ProviderName.Informix)]
		public void Test_Insert(string context)
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
		[Test, IncludeDataContextSource(ProviderName.Informix)]
		public void Test_Update(string context)
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
		[Test, IncludeDataContextSource(ProviderName.Informix)]
		public void Test_InsertOrUpdate(string context)
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
		[Test, IncludeDataContextSource(ProviderName.Informix)]
		public void Test_Inline(string context)
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
