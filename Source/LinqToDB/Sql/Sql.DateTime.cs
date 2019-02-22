using System;
using System.Globalization;

namespace LinqToDB
{
	using SqlQuery;

	using PN = ProviderName;

	public partial class Sql
	{
		#region DatePart

		class DatePartBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string partStr;
				var part = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : partStr = "year";        break;
					case Sql.DateParts.Quarter     : partStr = "quarter";     break;
					case Sql.DateParts.Month       : partStr = "month";       break;
					case Sql.DateParts.DayOfYear   : partStr = "dayofyear";   break;
					case Sql.DateParts.Day         : partStr = "day";         break;
					case Sql.DateParts.Week        : partStr = "week";        break;
					case Sql.DateParts.WeekDay     : partStr = "weekday";     break;
					case Sql.DateParts.Hour        : partStr = "hour";        break;
					case Sql.DateParts.Minute      : partStr = "minute";      break;
					case Sql.DateParts.Second      : partStr = "second";      break;
					case Sql.DateParts.Millisecond : partStr = "millisecond"; break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderMySql: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string partStr = null;
				var part = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : partStr = "year";        break;
					case Sql.DateParts.Quarter     : partStr = "quarter";     break;
					case Sql.DateParts.Month       : partStr = "month";       break;
					case Sql.DateParts.DayOfYear   :
						builder.Expression = "DayOfYear({date})";
						break;
					case Sql.DateParts.Day         : partStr = "day";         break;
					case Sql.DateParts.Week        : partStr = "week";        break;
					case Sql.DateParts.WeekDay     :
						builder.Expression = "WeekDay(Date_Add({date}, interval 1 day))";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						return;
					case Sql.DateParts.Hour        : partStr = "hour";        break;
					case Sql.DateParts.Minute      : partStr = "minute";      break;
					case Sql.DateParts.Second      : partStr = "second";      break;
					case Sql.DateParts.Millisecond : partStr = "millisecond"; break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (partStr != null)
					builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderPostgre: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string partStr = null;
				var part = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : partStr = "year";    break;
					case Sql.DateParts.Quarter     : partStr = "quarter"; break;
					case Sql.DateParts.Month       : partStr = "month";   break;
					case Sql.DateParts.DayOfYear   : partStr = "doy";     break;
					case Sql.DateParts.Day         : partStr = "day";     break;
					case Sql.DateParts.Week        : partStr = "week";    break;
					case Sql.DateParts.WeekDay     :
						builder.Expression = "Extract(dow from {date})";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						return;
					case Sql.DateParts.Hour        : partStr = "hour";    break;
					case Sql.DateParts.Minute      : partStr = "minute";  break;
					case Sql.DateParts.Second      : partStr = "second";  break;
					case Sql.DateParts.Millisecond :
						builder.Expression = "Cast(To_Char({date}, 'MS') as int)";
						break;
					default:
						throw new ArgumentOutOfRangeException("part", part, null);
				}

				if (partStr != null)
					builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderSqLite: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string partStr = null;
				var part = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : partStr = "Y"; break;
					case Sql.DateParts.Quarter     :
						builder.Expression = "Cast(strFTime('%m', {date}) as int)";
						builder.ResultExpression = builder.Inc(builder.Div(builder.Dec(builder.ConvertToSqlExpression(Precedence.Primary)), 3));
						return;
					case Sql.DateParts.Month       : partStr = "m"; break;
					case Sql.DateParts.DayOfYear   : partStr = "j"; break;
					case Sql.DateParts.Day         : partStr = "d"; break;
					case Sql.DateParts.Week        : partStr = "W"; break;
					case Sql.DateParts.WeekDay     :
						builder.Expression = "Cast(strFTime('%w', {date}) as int)";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						return;
					case Sql.DateParts.Hour        : partStr = "H"; break;
					case Sql.DateParts.Minute      : partStr = "M"; break;
					case Sql.DateParts.Second      : partStr = "S"; break;
					case Sql.DateParts.Millisecond : 
						builder.Expression = "Cast(strFTime('%f', {date}) * 1000 as int) % 1000";
						builder.Extension.Precedence = Precedence.Multiplicative;
						break;
					default:
						throw new ArgumentOutOfRangeException("part", part, null);
				}

				if (partStr != null)
					builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderAccess: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string partStr;
				var part    = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : partStr = "yyyy"; break;
					case Sql.DateParts.Quarter     : partStr = "q";    break;
					case Sql.DateParts.Month       : partStr = "m";    break;
					case Sql.DateParts.DayOfYear   : partStr = "y";    break;
					case Sql.DateParts.Day         : partStr = "d";    break;
					case Sql.DateParts.Week        : partStr = "ww";   break;
					case Sql.DateParts.WeekDay     : partStr = "w";    break;
					case Sql.DateParts.Hour        : partStr = "h";    break;
					case Sql.DateParts.Minute      : partStr = "n";    break;
					case Sql.DateParts.Second      : partStr = "s";    break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.AddExpression("part", partStr);
			}
		}


		class DatePartBuilderSapHana: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string exprStr;
				var part = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : exprStr = "Year({date})";                     break;
					case Sql.DateParts.Quarter     : 
						builder.Expression = "Floor((Month({date})-1) / 3)";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression());
						return;
					case Sql.DateParts.Month       : exprStr = "Month({date})";                    break;
					case Sql.DateParts.DayOfYear   : exprStr = "DayOfYear({date})";                break;
					case Sql.DateParts.Day         : exprStr = "DayOfMonth({date})";               break;
					case Sql.DateParts.Week        : exprStr = "Week({date})";                     break;
					case Sql.DateParts.WeekDay     : 
						builder.Expression = "MOD(Weekday({date}) + 1, 7)";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression());
						return;
					case Sql.DateParts.Hour        : exprStr = "Hour({date})";                     break;
					case Sql.DateParts.Minute      : exprStr = "Minute({date})";                   break;
					case Sql.DateParts.Second      : exprStr = "Second({date})";                   break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.Expression = exprStr;
			}
		}

		class DatePartBuilderInformix: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string exprStr;
				var part = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : exprStr = "Year({date})";          break;
					case Sql.DateParts.Quarter:
						{
							builder.Expression       = "Month({date})";
							builder.ResultExpression =
								builder.Inc(builder.Div(builder.Dec(builder.ConvertToSqlExpression(Precedence.Primary)), 3));
							return;
						}
					case Sql.DateParts.Month       : exprStr = "Month({date})";         break;
					case Sql.DateParts.DayOfYear   :
						{
							var param = builder.GetExpression("date");
							builder.ResultExpression = builder.Inc(
								builder.Sub<int>(
									new SqlFunction(null, "Mdy",
										new SqlFunction(null, "Month", param),
										new SqlFunction(null, "Day", param),
										new SqlFunction(null, "Year", param)),
									new SqlFunction(null, "Mdy",
										new SqlValue(1),
										new SqlValue(1),
										new SqlFunction(null, "Year", param)))
							);
							return;
						}
					case Sql.DateParts.Day         : exprStr = "Day({date})";           break;
					case Sql.DateParts.Week        : exprStr = "((Extend({date}, year to day) - (Mdy(12, 31 - WeekDay(Mdy(1, 1, year({date}))), Year({date}) - 1) + Interval(1) day to day)) / 7 + Interval(1) day to day)::char(10)::int"; break;
					case Sql.DateParts.WeekDay     : 
						{
							builder.Expression = "weekDay({date})";
							builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
							return;
						}
					case Sql.DateParts.Hour        : exprStr = "({date}::datetime Hour to Hour)::char(3)::int";     break;
					case Sql.DateParts.Minute      : exprStr = "({date}::datetime Minute to Minute)::char(3)::int"; break;
					case Sql.DateParts.Second      : exprStr = "({date}::datetime Second to Second)::char(3)::int"; break;
					case Sql.DateParts.Millisecond : exprStr = "Millisecond({date})";                               break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.Expression = exprStr;
			}
		}

		class DatePartBuilderOracle: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string partStr;
				var part = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : partStr = "To_Number(To_Char({date}, 'YYYY'))";                  break;
					case Sql.DateParts.Quarter     : partStr = "To_Number(To_Char({date}, 'Q'))";                     break;
					case Sql.DateParts.Month       : partStr = "To_Number(To_Char({date}, 'MM'))";                    break;
					case Sql.DateParts.DayOfYear   : partStr = "To_Number(To_Char({date}, 'DDD'))";                   break;
					case Sql.DateParts.Day         : partStr = "To_Number(To_Char({date}, 'DD'))";                    break;
					case Sql.DateParts.Week        : partStr = "To_Number(To_Char({date}, 'WW'))";                    break;
					case Sql.DateParts.WeekDay:
						{
							builder.Expression = "Mod(1 + Trunc({date}) - Trunc({date}, 'IW'), 7)";
							builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
							return;
						}
					case Sql.DateParts.Hour        : partStr = "To_Number(To_Char({date}, 'HH24'))";                  break;
					case Sql.DateParts.Minute      : partStr = "To_Number(To_Char({date}, 'MI'))";                    break;
					case Sql.DateParts.Second      : partStr = "To_Number(To_Char({date}, 'SS'))";                    break;
					case Sql.DateParts.Millisecond : partStr = "To_Number(To_Char({date}, 'FF'))";                    break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.Expression = partStr;
			}
		}

		class DatePartBuilderDB2: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string partStr;
				var part = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : partStr = "To_Number(To_Char({date}, 'YYYY'))";                  break;
					case Sql.DateParts.Quarter     : partStr = "To_Number(To_Char({date}, 'Q'))";                     break;
					case Sql.DateParts.Month       : partStr = "To_Number(To_Char({date}, 'MM'))";                    break;
					case Sql.DateParts.DayOfYear   : partStr = "To_Number(To_Char({date}, 'DDD'))";                   break;
					case Sql.DateParts.Day         : partStr = "To_Number(To_Char({date}, 'DD'))";                    break;
					case Sql.DateParts.Week        : partStr = "To_Number(To_Char({date}, 'WW'))";                    break;
					case Sql.DateParts.WeekDay     : partStr = "DayOfWeek({date})";                                   break;
					case Sql.DateParts.Hour        : partStr = "To_Number(To_Char({date}, 'HH24'))";                  break;
					case Sql.DateParts.Minute      : partStr = "To_Number(To_Char({date}, 'MI'))";                    break;
					case Sql.DateParts.Second      : partStr = "To_Number(To_Char({date}, 'SS'))";                    break;
					case Sql.DateParts.Millisecond:
						{
							builder.Expression = "To_Number(To_Char({date}, 'FF'))";
							builder.ResultExpression = builder.Div(builder.ConvertToSqlExpression(Precedence.Primary), 1000);
							return;
						}
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.Expression = partStr;
			}
		}

		class DatePartBuilderFirebird: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string partStr;
				var part = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : partStr = "year";        break;
					case Sql.DateParts.Quarter     :
						builder.Expression = "Extract(Month from {date})";
						builder.ResultExpression = builder.Inc(builder.Div(builder.Dec(builder.ConvertToSqlExpression(Precedence.Primary)), 3));
						return;
					case Sql.DateParts.Month       : partStr = "month";       break;
					case Sql.DateParts.DayOfYear   : partStr = "yearday";     break;
					case Sql.DateParts.Day         : partStr = "day";         break;
					case Sql.DateParts.Week        : partStr = "week";        break;
					case Sql.DateParts.WeekDay     : partStr = "weekday";     break;
					case Sql.DateParts.Hour        : partStr = "hour";        break;
					case Sql.DateParts.Minute      : partStr = "minute";      break;
					case Sql.DateParts.Second      : partStr = "second";      break;
					case Sql.DateParts.Millisecond : partStr = "millisecond"; break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.AddExpression("part", partStr);

				switch (part)
				{
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.WeekDay:
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						break;
				}
			}
		}

		[Sql.Extension(               "DatePart({part}, {date})",                 ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilder))]
		[Sql.Extension(PN.DB2,        "",                                         ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderDB2))] // TODO: Not checked
		[Sql.Extension(PN.Informix,   "",                                         ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderInformix))] 
		[Sql.Extension(PN.MySql,      "Extract({part} from {date})",              ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderMySql))]
		[Sql.Extension(PN.PostgreSQL, "Extract({part} from {date})",              ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderPostgre))]
		[Sql.Extension(PN.Firebird,   "Extract({part} from {date})",              ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderFirebird))]
		[Sql.Extension(PN.SQLite,     "Cast(StrFTime('%{part}', {date}) as int)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSqLite))]
		[Sql.Extension(PN.Access,     "DatePart('{part}', {date})",               ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderAccess))]
		[Sql.Extension(PN.SapHana,    "",                                         ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSapHana))]
		[Sql.Extension(PN.Oracle,     "",                                         ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderOracle))]
		public static int? DatePart(Sql.DateParts part, [ExprParameter] DateTime? date)
		{
			if (date == null)
				return null;

			switch (part)
			{
				case Sql.DateParts.Year        : return date.Value.Year;
				case Sql.DateParts.Quarter     : return (date.Value.Month - 1) / 3 + 1;
				case Sql.DateParts.Month       : return date.Value.Month;
				case Sql.DateParts.DayOfYear   : return date.Value.DayOfYear;
				case Sql.DateParts.Day         : return date.Value.Day;
				case Sql.DateParts.Week        : return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
				case Sql.DateParts.WeekDay     : return ((int)date.Value.DayOfWeek + 1 + Sql.DateFirst + 6) % 7 + 1;
				case Sql.DateParts.Hour        : return date.Value.Hour;
				case Sql.DateParts.Minute      : return date.Value.Minute;
				case Sql.DateParts.Second      : return date.Value.Second;
				case Sql.DateParts.Millisecond : return date.Value.Millisecond;
			}

			throw new InvalidOperationException();
		}

		#endregion DatePart

		#region DateAdd

		class DateAddBuilderOracle : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number");
				switch (part)
				{
					case Sql.DateParts.Year  : 
						builder.ResultExpression = new SqlFunction(typeof(DateTime?), "Add_Months", date, builder.Mul(number, 12));
						break;
					case Sql.DateParts.Quarter : 
						builder.ResultExpression = new SqlFunction(typeof(DateTime?), "Add_Months", date, builder.Mul(number, 3));
						break;
					case Sql.DateParts.Month : 
						builder.ResultExpression = new SqlFunction(typeof(DateTime?), "Add_Months", builder.GetExpression("date"), builder.GetExpression("number"));
						break;
					case Sql.DateParts.DayOfYear   :
					case Sql.DateParts.WeekDay     :
					case Sql.DateParts.Day         : builder.ResultExpression = builder.Add<DateTime>(date, number);                                   break;
					case Sql.DateParts.Week        : builder.ResultExpression = builder.Add<DateTime>(date, builder.Mul(number,                   7)); break;
					case Sql.DateParts.Hour        : builder.ResultExpression = builder.Add<DateTime>(date, builder.Div(number,                  24)); break;
					case Sql.DateParts.Minute      : builder.ResultExpression = builder.Add<DateTime>(date, builder.Div(number,             60 * 24)); break;
					case Sql.DateParts.Second      : builder.ResultExpression = builder.Add<DateTime>(date, builder.Div(number,        60 * 60 * 24)); break;
					case Sql.DateParts.Millisecond : builder.ResultExpression = builder.Add<DateTime>(date, builder.Div(number, 1000 * 60 * 60 * 24)); break;	
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		class DateAddBuilderDB2 : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number");

				string expStr;

				switch (part)
				{
					case Sql.DateParts.Year        : expStr = "{0} + {1} Year";                 break;
					case Sql.DateParts.Quarter     : expStr = "{0} + ({1} * 3) Month";          break;
					case Sql.DateParts.Month       : expStr = "{0} + {1} Month";                break;
					case Sql.DateParts.DayOfYear   : 
					case Sql.DateParts.WeekDay     : 
					case Sql.DateParts.Day         : expStr = "{0} + {1} Day";                  break;
					case Sql.DateParts.Week        : expStr = "{0} + ({1} * 7) Day";            break;
					case Sql.DateParts.Hour        : expStr = "{0} + {1} Hour";                 break;
					case Sql.DateParts.Minute      : expStr = "{0} + {1} Minute";               break;
					case Sql.DateParts.Second      : expStr = "{0} + {1} Second";               break;
					case Sql.DateParts.Millisecond : expStr = "{0} + ({1} * 1000) Microsecond"; break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.ResultExpression = new SqlExpression(typeof(DateTime?), expStr, Precedence.Additive, date, number);
			}
		}

		class DateAddBuilderInformix : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number");

				string expStr;
				switch (part)
				{
					case Sql.DateParts.Year        : expStr = "{0} + Interval({1}) Year to Year";       break;
					case Sql.DateParts.Quarter     : expStr = "{0} + Interval({1}) Month to Month * 3"; break;
					case Sql.DateParts.Month       : expStr = "{0} + Interval({1}) Month to Month";     break;
					case Sql.DateParts.DayOfYear   : 
					case Sql.DateParts.WeekDay     : 
					case Sql.DateParts.Day         : expStr = "{0} + Interval({1}) Day to Day";         break;
					case Sql.DateParts.Week        : expStr = "{0} + Interval({1}) Day to Day * 7";     break;
					case Sql.DateParts.Hour        : expStr = "{0} + Interval({1}) Hour to Hour";       break;
					case Sql.DateParts.Minute      : expStr = "{0} + Interval({1}) Minute to Minute";   break;
					case Sql.DateParts.Second      : expStr = "{0} + Interval({1}) Second to Second";   break;
					case Sql.DateParts.Millisecond : expStr = "{0} + Interval({1}) Second to Fraction * 1000";  break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.ResultExpression = new SqlExpression(typeof(DateTime?), expStr, Precedence.Additive, date, number);
			}
		}

		class DateAddBuilderPostgreSQL : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number");

				string expStr;
				switch (part)
				{
					case Sql.DateParts.Year        : expStr = "{0} + {1} * Interval '1 Year'";         break;
					case Sql.DateParts.Quarter     : expStr = "{0} + {1} * Interval '1 Month' * 3";    break;
					case Sql.DateParts.Month       : expStr = "{0} + {1} * Interval '1 Month'";        break;
					case Sql.DateParts.DayOfYear   : 
					case Sql.DateParts.WeekDay     : 
					case Sql.DateParts.Day         : expStr = "{0} + {1} * Interval '1 Day'";          break;
					case Sql.DateParts.Week        : expStr = "{0} + {1} * Interval '1 Day' * 7";      break;
					case Sql.DateParts.Hour        : expStr = "{0} + {1} * Interval '1 Hour'";         break;
					case Sql.DateParts.Minute      : expStr = "{0} + {1} * Interval '1 Minute'";       break;
					case Sql.DateParts.Second      : expStr = "{0} + {1} * Interval '1 Second'";       break;
					case Sql.DateParts.Millisecond : expStr = "{0} + {1} * Interval '1 Millisecond'";  break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.ResultExpression = new SqlExpression(typeof(DateTime?), expStr, Precedence.Additive, date, number);
			}
		}

		class DateAddBuilderMySql : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number");

				string expStr;
				switch (part)
				{
					case Sql.DateParts.Year        : expStr = "Interval {0} Year"; break;
					case Sql.DateParts.Quarter     : expStr = "Interval {0} Quarter"; break;
					case Sql.DateParts.Month       : expStr = "Interval {0} Month"; break;
					case Sql.DateParts.DayOfYear   : 
					case Sql.DateParts.WeekDay     : 
					case Sql.DateParts.Day         : expStr = "Interval {0} Day";          break;
					case Sql.DateParts.Week        : expStr = "Interval {0} Week"; break;
					case Sql.DateParts.Hour        : expStr = "Interval {0} Hour"; break;
					case Sql.DateParts.Minute      : expStr = "Interval {0} Minute"; break;
					case Sql.DateParts.Second      : expStr = "Interval {0} Second"; break;
					case Sql.DateParts.Millisecond : expStr = "Interval {0} Millisecond"; break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.ResultExpression = new SqlFunction(typeof(DateTime?), "Date_Add", date,
					new SqlExpression(expStr, Precedence.Primary, number));
			}
		}

		class DateAddBuilderSQLite : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number");

				string expStr;
				switch (part)
				{
					case Sql.DateParts.Year        : expStr = "'{0} Year'"; break;
					case Sql.DateParts.Quarter     : 
						expStr = "'{0} Month'"; 
						number = builder.Mul(number, 3);
						break;
					case Sql.DateParts.Month       : expStr = "'{0} Month'"; break;
					case Sql.DateParts.DayOfYear   : 
					case Sql.DateParts.WeekDay     : 
					case Sql.DateParts.Day         : expStr = "'{0} Day'";          break;
					case Sql.DateParts.Week        : 
						expStr = "'{0} Day'"; 
						number = builder.Mul(number, 7);
						break;
					case Sql.DateParts.Hour        : expStr = "'{0} Hour'"; break;
					case Sql.DateParts.Minute      : expStr = "'{0} Minute'"; break;
					case Sql.DateParts.Second      : expStr = "'{0} Second'"; break;
					case Sql.DateParts.Millisecond : expStr = "'{0} Millisecond'"; break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.ResultExpression = new SqlFunction(typeof(DateTime?), "DateTime", date,
					new SqlExpression(expStr, Precedence.Additive, number));
			}
		}

		class DateAddBuilderAccess : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number");

				string partStr;
				switch (part)
				{
					case Sql.DateParts.Year        : partStr = "yyyy"; break;
					case Sql.DateParts.Quarter     : partStr = "q";    break;
					case Sql.DateParts.Month       : partStr = "m";    break;
					case Sql.DateParts.DayOfYear   : partStr = "y";    break; 
					case Sql.DateParts.Day         : partStr = "d";    break;
					case Sql.DateParts.Week        : partStr = "ww";   break;
					case Sql.DateParts.WeekDay     : partStr = "w";    break;
					case Sql.DateParts.Hour        : partStr = "h";    break;
					case Sql.DateParts.Minute      : partStr = "n";    break;
					case Sql.DateParts.Second      : partStr = "s";    break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.ResultExpression = new SqlFunction(typeof(DateTime?), "DateAdd", 
					new SqlValue(partStr), number, date);
			}
		}

		class DateAddBuilderSapHana : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number");

				string function;
				switch (part)
				{
					case Sql.DateParts.Year        : function = "Add_Years";   break;
					case Sql.DateParts.Quarter     : 
						function = "Add_Months";
						number   = builder.Mul(number, 3);  
						break;
					case Sql.DateParts.Month       : function = "Add_Months";  break;
					case Sql.DateParts.DayOfYear   : 
					case Sql.DateParts.Day         : 
					case Sql.DateParts.WeekDay     : function = "Add_Days";    break;
					case Sql.DateParts.Week        : 
						function = "Add_Days";   
						number   = builder.Mul(number, 7);  
						break;
					case Sql.DateParts.Hour        : 
						function = "Add_Seconds";
						number   = builder.Mul(number, 3600);
						break;
					case Sql.DateParts.Minute      : 
						function = "Add_Seconds";
						number   = builder.Mul(number, 60);
						break;
					case Sql.DateParts.Second      : function = "Add_Seconds"; break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.ResultExpression = new SqlFunction(typeof(DateTime?), function, date, number);
			}
		}

		class DateAddBuilderFirebird : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number");

				ISqlExpression partSql = null;
				switch (part)
				{
					case Sql.DateParts.Quarter   :
						partSql = new SqlValue(Sql.DateParts.Month);
						number  = builder.Mul(number, 3);
						break;
					case Sql.DateParts.DayOfYear :
					case Sql.DateParts.WeekDay   :
						partSql = new SqlValue(Sql.DateParts.Day);
						break;
					case Sql.DateParts.Week      :
						partSql = new SqlValue(Sql.DateParts.Day);
						number = builder.Mul(number, 7);
						break;
				}

				partSql = partSql ?? new SqlValue(part);

				builder.ResultExpression = new SqlFunction(typeof(DateTime?), "DateAdd", partSql, number, date);
			}
		}


		[Sql.Function] 
		[Sql.Extension(PN.Oracle,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderOracle))]
		[Sql.Extension(PN.DB2,        "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderDB2))]
		[Sql.Extension(PN.Informix,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderInformix))]
		[Sql.Extension(PN.PostgreSQL, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderPostgreSQL))]
		[Sql.Extension(PN.MySql,      "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderMySql))]
		[Sql.Extension(PN.SQLite,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderSQLite))]
		[Sql.Extension(PN.Access,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderAccess))]
		[Sql.Extension(PN.SapHana,    "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderSapHana))]
		[Sql.Extension(PN.Firebird,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderFirebird))]
		public static DateTime? DateAdd(Sql.DateParts part, double? number, DateTime? date)
		{
			if (number == null || date == null)
				return null;

			switch (part)
			{
				case Sql.DateParts.Year        : return date.Value.AddYears       ((int)number);
				case Sql.DateParts.Quarter     : return date.Value.AddMonths      ((int)number * 3);
				case Sql.DateParts.Month       : return date.Value.AddMonths      ((int)number);
				case Sql.DateParts.DayOfYear   : return date.Value.AddDays        (number.Value);
				case Sql.DateParts.Day         : return date.Value.AddDays        (number.Value);
				case Sql.DateParts.Week        : return date.Value.AddDays        (number.Value * 7);
				case Sql.DateParts.WeekDay     : return date.Value.AddDays        (number.Value);
				case Sql.DateParts.Hour        : return date.Value.AddHours       (number.Value);
				case Sql.DateParts.Minute      : return date.Value.AddMinutes     (number.Value);
				case Sql.DateParts.Second      : return date.Value.AddSeconds     (number.Value);
				case Sql.DateParts.Millisecond : return date.Value.AddMilliseconds(number.Value);
			}

			throw new InvalidOperationException();
		}

		#endregion
	}
}