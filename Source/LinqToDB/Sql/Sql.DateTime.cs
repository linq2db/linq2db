using System;
using System.Globalization;

namespace LinqToDB
{
	using SqlQuery;
	using Expressions;

	using PN = ProviderName;

	public partial class Sql
	{
		[Enum]
		public enum DateParts
		{
			Year        =  0,
			Quarter     =  1,
			Month       =  2,
			DayOfYear   =  3,
			Day         =  4,
			/// <summary>
			/// This date part behavior depends on used database and also depends on where if calculated - in C# code or in database.
			/// Eeach database could have own week numbering logic, see notes below.
			///
			/// Current implementation uses following schemas per-provider:
			/// C# evaluation:
			/// <para>
			/// <c>CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday)</c>
			/// </para>
			/// Databases:
			/// <list type="bullet">
			/// <item>US numbering schema used by:
			/// <list type="bullet">
			/// <item>MS Access</item>
			/// <item>SQL CE</item>
			/// <item>SQL Server</item>
			/// <item>SAP/Sybase ASE</item>
			/// <item>Informix</item>
			/// </list>
			/// </item>
			/// <item>US 0-based numbering schema used by MySQL database</item>
			/// <item>ISO numbering schema with incorrect numbering of first week used by SAP HANA database</item>
			/// <item>ISO numbering schema with proper numbering of first week used by:
			/// <list type="bullet">
			/// <item>Firebird</item>
			/// <item>PostgreSQL</item>
			/// <item>ClickHouse</item>
			/// </list>
			/// </item>
			/// <item>Primitive (each 7 days counted as week) numbering schema:
			/// <list type="bullet">
			/// <item>DB2</item>
			/// <item>Oracle</item>
			/// </list>
			/// </item>
			/// <item>SQLite numbering logic cannot be classified by human being</item>
			/// </list>
			/// </summary>
			Week        =  5,
			WeekDay     =  6,
			Hour        =  7,
			Minute      =  8,
			Second      =  9,
			Millisecond = 10,
		}

		#region DatePart

		internal class DatePartBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<DateParts>("part");
				var partStr = DatePartToStr(part);
				var date    = builder.GetExpression("date");

				builder.ResultExpression = new SqlFunction(typeof(int), builder.Expression,
					new SqlExpression(partStr, Precedence.Primary), date);
			}

			public static string DatePartToStr(DateParts part)
			{
				return part switch
				{
					DateParts.Year          => "year",
					DateParts.Quarter       => "quarter",
					DateParts.Month         => "month",
					DateParts.DayOfYear     => "dayofyear",
					DateParts.Day           => "day",
					DateParts.Week          => "week",
					DateParts.WeekDay       => "weekday",
					DateParts.Hour          => "hour",
					DateParts.Minute        => "minute",
					DateParts.Second        => "second",
					DateParts.Millisecond   => "millisecond",
					_                       => ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}")
				};
			}
		}

		class DatePartBuilderMySql: IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<DateParts>("part");
				string? partStr = null;
				switch (part)
				{
					case DateParts.Year        : partStr = "year";        break;
					case DateParts.Quarter     : partStr = "quarter";     break;
					case DateParts.Month       : partStr = "month";       break;
					case DateParts.DayOfYear   :
						builder.Expression = "DayOfYear({date})";
						break;
					case DateParts.Day         : partStr = "day";         break;
					case DateParts.Week        : partStr = "week";        break;
					case DateParts.WeekDay     :
						builder.Expression = "WeekDay(Date_Add({date}, interval 1 day))";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						return;
					case DateParts.Hour        : partStr = "hour";        break;
					case DateParts.Minute      : partStr = "minute";      break;
					case DateParts.Second      : partStr = "second";      break;
					case DateParts.Millisecond : partStr = "millisecond"; break;
					default:
						ThrowHelper.ThrowInvalidOperationException($"Unexpected datepart: {part}");
						break;
				}

				if (partStr != null)
					builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderPostgre: IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				string? partStr = null;
				var part = builder.GetValue<DateParts>("part");
				switch (part)
				{
					case DateParts.Year        : partStr = "year";    break;
					case DateParts.Quarter     : partStr = "quarter"; break;
					case DateParts.Month       : partStr = "month";   break;
					case DateParts.DayOfYear   : partStr = "doy";     break;
					case DateParts.Day         : partStr = "day";     break;
					case DateParts.Week        : partStr = "week";    break;
					case DateParts.WeekDay     :
						builder.AddExpression("part", "dow");
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						return;
					case DateParts.Hour        : partStr = "hour";    break;
					case DateParts.Minute      : partStr = "minute";  break;
					case DateParts.Second      : partStr = "second";  break;
					case DateParts.Millisecond :
						builder.Expression = "Cast(To_Char({date}, 'MS') as int)";
						break;
					default:
						ThrowHelper.ThrowInvalidOperationException($"Unexpected datepart: {part}");
						break;
				}

				if (partStr != null)
					builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderSqLite: IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				string? partStr = null;
				var part = builder.GetValue<DateParts>("part");
				switch (part)
				{
					case DateParts.Year        : partStr = "Y"; break;
					case DateParts.Quarter     :
						builder.Expression = "Cast(strFTime('%m', {date}) as int)";
						builder.ResultExpression = builder.Inc(builder.Div(builder.Dec(builder.ConvertToSqlExpression(Precedence.Primary)), 3));
						return;
					case DateParts.Month       : partStr = "m"; break;
					case DateParts.DayOfYear   : partStr = "j"; break;
					case DateParts.Day         : partStr = "d"; break;
					case DateParts.Week        : partStr = "W"; break;
					case DateParts.WeekDay     :
						builder.Expression = "Cast(strFTime('%w', {date}) as int)";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						return;
					case DateParts.Hour        : partStr = "H"; break;
					case DateParts.Minute      : partStr = "M"; break;
					case DateParts.Second      : partStr = "S"; break;
					case DateParts.Millisecond :
						builder.Expression = "Cast(strFTime('%f', {date}) * 1000 as int) % 1000";
						builder.Extension.Precedence = Precedence.Multiplicative;
						break;
					default:
						ThrowHelper.ThrowInvalidOperationException($"Unexpected datepart: {part}");
						break;
				}

				if (partStr != null)
					builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderAccess: IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<DateParts>("part");
				var partStr = part switch
				{
					DateParts.Year      => "yyyy",
					DateParts.Quarter   => "q",
					DateParts.Month     => "m",
					DateParts.DayOfYear => "y",
					DateParts.Day       => "d",
					DateParts.Week      => "ww",
					DateParts.WeekDay   => "w",
					DateParts.Hour      => "h",
					DateParts.Minute    => "n",
					DateParts.Second    => "s",
					_ => ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}"),
				};
				builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderSapHana: IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				string exprStr;
				var part = builder.GetValue<DateParts>("part");
				switch (part)
				{
					case DateParts.Year        : exprStr = "Year({date})";                     break;
					case DateParts.Quarter     :
					{
						builder.Expression = "Floor((Month({date})-1) / 3)";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression());
						return;
					}
					case DateParts.Month       : exprStr = "Month({date})";                    break;
					case DateParts.DayOfYear   : exprStr = "DayOfYear({date})";                break;
					case DateParts.Day         : exprStr = "DayOfMonth({date})";               break;
					case DateParts.Week        : exprStr = "Week({date})";                     break;
					case DateParts.WeekDay     :
					{
						builder.Expression = "MOD(Weekday({date}) + 1, 7)";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression());
						return;
					}
					case DateParts.Hour        : exprStr = "Hour({date})";                     break;
					case DateParts.Minute      : exprStr = "Minute({date})";                   break;
					case DateParts.Second      : exprStr = "Second({date})";                   break;
					default:
					{
						ThrowHelper.ThrowInvalidOperationException($"Unexpected datepart: {part}");
						return;
					}
				}

				builder.Expression = exprStr;
			}
		}

		class DatePartBuilderInformix: IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				string exprStr;
				var part = builder.GetValue<DateParts>("part");
				switch (part)
				{
					case DateParts.Year        : exprStr = "Year({date})";          break;
					case DateParts.Quarter:
					{
						builder.Expression       = "Month({date})";
						builder.ResultExpression =
							builder.Inc(builder.Div(builder.Dec(builder.ConvertToSqlExpression(Precedence.Primary)), 3));
						return;
					}
					case DateParts.Month       : exprStr = "Month({date})";         break;
					case DateParts.DayOfYear   :
					{
						var param = builder.GetExpression("date");
						builder.ResultExpression = builder.Inc(
							builder.Sub<int>(
								new SqlFunction(typeof(DateTime?), "Mdy",
									new SqlFunction(typeof(int?), "Month", param),
									new SqlFunction(typeof(int?), "Day",   param),
									new SqlFunction(typeof(int?), "Year",  param)),
								new SqlFunction(typeof(DateTime?), "Mdy",
									new SqlValue(1),
									new SqlValue(1),
									new SqlFunction(typeof(int?), "Year", param)))
						);
						return;
					}
					case DateParts.Day         : exprStr = "Day({date})";           break;
					case DateParts.Week        : exprStr = "((Extend({date}, year to day) - (Mdy(12, 31 - WeekDay(Mdy(1, 1, year({date}))), Year({date}) - 1) + Interval(1) day to day)) / 7 + Interval(1) day to day)::char(10)::int"; break;
					case DateParts.WeekDay     :
					{
						builder.Expression = "weekDay({date})";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						return;
					}
					case DateParts.Hour        : exprStr = "({date}::datetime Hour to Hour)::char(3)::int";     break;
					case DateParts.Minute      : exprStr = "({date}::datetime Minute to Minute)::char(3)::int"; break;
					case DateParts.Second      : exprStr = "({date}::datetime Second to Second)::char(3)::int"; break;
					case DateParts.Millisecond : exprStr = "Millisecond({date})";                               break;
					default:
						ThrowHelper.ThrowInvalidOperationException($"Unexpected datepart: {part}");
						return;
				}

				builder.Expression = exprStr;
			}
		}

		class DatePartBuilderOracle: IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				string partStr;
				var part = builder.GetValue<DateParts>("part");
				switch (part)
				{
					case DateParts.Year        : partStr = "To_Number(To_Char({date}, 'YYYY'))";                  break;
					case DateParts.Quarter     : partStr = "To_Number(To_Char({date}, 'Q'))";                     break;
					case DateParts.Month       : partStr = "To_Number(To_Char({date}, 'MM'))";                    break;
					case DateParts.DayOfYear   : partStr = "To_Number(To_Char({date}, 'DDD'))";                   break;
					case DateParts.Day         : partStr = "To_Number(To_Char({date}, 'DD'))";                    break;
					case DateParts.Week        : partStr = "To_Number(To_Char({date}, 'WW'))";                    break;
					case DateParts.WeekDay:
					{
						builder.Expression = "Mod(1 + Trunc({date}) - Trunc({date}, 'IW'), 7)";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						return;
					}
					case DateParts.Hour        : partStr = "To_Number(To_Char({date}, 'HH24'))";                  break;
					case DateParts.Minute      : partStr = "To_Number(To_Char({date}, 'MI'))";                    break;
					case DateParts.Second      : partStr = "To_Number(To_Char({date}, 'SS'))";                    break;
					case DateParts.Millisecond : partStr = "To_Number(To_Char({date}, 'FF'))";                    break;
					default:
						partStr = ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}");
						break;
				}

				builder.Expression = partStr;
			}
		}

		class DatePartBuilderDB2: IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				string partStr;
				var part = builder.GetValue<DateParts>("part");
				switch (part)
				{
					case DateParts.Year        : partStr = "To_Number(To_Char({date}, 'YYYY'))";                  break;
					case DateParts.Quarter     : partStr = "To_Number(To_Char({date}, 'Q'))";                     break;
					case DateParts.Month       : partStr = "To_Number(To_Char({date}, 'MM'))";                    break;
					case DateParts.DayOfYear   : partStr = "To_Number(To_Char({date}, 'DDD'))";                   break;
					case DateParts.Day         : partStr = "To_Number(To_Char({date}, 'DD'))";                    break;
					case DateParts.Week        : partStr = "To_Number(To_Char({date}, 'WW'))";                    break;
					case DateParts.WeekDay     : partStr = "DayOfWeek({date})";                                   break;
					case DateParts.Hour        : partStr = "To_Number(To_Char({date}, 'HH24'))";                  break;
					case DateParts.Minute      : partStr = "To_Number(To_Char({date}, 'MI'))";                    break;
					case DateParts.Second      : partStr = "To_Number(To_Char({date}, 'SS'))";                    break;
					case DateParts.Millisecond:
					{
						builder.Expression = "To_Number(To_Char({date}, 'FF'))";
						builder.ResultExpression = builder.Div(builder.ConvertToSqlExpression(Precedence.Primary), 1000);
						return;
					}
					default:
					{ 
						partStr = ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}");
						break;
					}
				}

				builder.Expression = partStr;
			}
		}

		class DatePartBuilderFirebird: IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				string partStr;
				var part = builder.GetValue<DateParts>("part");
				switch (part)
				{
					case DateParts.Year        : partStr = "year";        break;
					case DateParts.Quarter     :
					{ 
						builder.Expression = "Extract(Month from {date})";
						builder.ResultExpression = builder.Inc(builder.Div(builder.Dec(builder.ConvertToSqlExpression(Precedence.Primary)), 3));
						return;
					}
					case DateParts.Month       : partStr = "month";       break;
					case DateParts.DayOfYear   : partStr = "yearday";     break;
					case DateParts.Day         : partStr = "day";         break;
					case DateParts.Week        : partStr = "week";        break;
					case DateParts.WeekDay     : partStr = "weekday";     break;
					case DateParts.Hour        : partStr = "hour";        break;
					case DateParts.Minute      : partStr = "minute";      break;
					case DateParts.Second      : partStr = "second";      break;
					case DateParts.Millisecond : partStr = "millisecond"; break;
					default:
					{
						partStr = ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}");
						break;
					}
				}

				builder.AddExpression("part", partStr);

				switch (part)
				{
					case DateParts.DayOfYear:
					case DateParts.WeekDay:
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						break;
				}
			}
		}

		class DatePartBuilderClickHouse : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				string exprStr;
				var part = builder.GetValue<DateParts>("part");

				switch (part)
				{
					case DateParts.Year       : exprStr = "YEAR({date})"                      ; break;
					case DateParts.Quarter    : exprStr = "QUARTER({date})"                   ; break;
					case DateParts.Month      : exprStr = "MONTH({date})"                     ; break;
					case DateParts.DayOfYear  : exprStr = "DAYOFYEAR({date})"                 ; break;
					case DateParts.Day        : exprStr = "DAY({date})"                       ; break;
					case DateParts.Week       : exprStr = "toISOWeek(toDateTime64({date}, 0))"; break;
					case DateParts.Hour       : exprStr = "HOUR({date})"                      ; break;
					case DateParts.Minute     : exprStr = "MINUTE({date})"                    ; break;
					case DateParts.Second     : exprStr = "SECOND({date})"                    ; break;
					case DateParts.WeekDay    :
						builder.Expression = "DAYOFWEEK(addDays({date}, 1))";
						builder.Extension.Precedence = Precedence.Additive;
						return;
					case DateParts.Millisecond:
						builder.Expression = "toUnixTimestamp64Milli({date}) % 1000";
						builder.Extension.Precedence = Precedence.Multiplicative;
						return;
					default:
						ThrowHelper.ThrowInvalidOperationException($"Unexpected datepart: {part}");
						return;
				}

				builder.Expression = exprStr;
			}
		}

		[Extension(               "DatePart",                                        ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilder))]
		[Extension(PN.DB2,        "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderDB2))] // TODO: Not checked
		[Extension(PN.Informix,   "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderInformix))]
		[Extension(PN.MySql,      "Extract({part} from {date})",                     ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderMySql))]
		[Extension(PN.PostgreSQL, "Cast(Floor(Extract({part} from {date})) as int)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderPostgre))]
		[Extension(PN.Firebird,   "Cast(Floor(Extract({part} from {date})) as int)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderFirebird))]
		[Extension(PN.SQLite,     "Cast(StrFTime('%{part}', {date}) as int)",        ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSqLite))]
		[Extension(PN.Access,     "DatePart('{part}', {date})",                      ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderAccess))]
		[Extension(PN.SapHana,    "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSapHana))]
		[Extension(PN.Oracle,     "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderOracle))]
		[Extension(PN.ClickHouse, "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderClickHouse))]
		public static int? DatePart([SqlQueryDependent] DateParts part, [ExprParameter] DateTime? date)
		{
			if (date == null)
				return null;

			return part switch
			{
				DateParts.Year          => date.Value.Year,
				DateParts.Quarter       => (date.Value.Month - 1) / 3 + 1,
				DateParts.Month         => date.Value.Month,
				DateParts.DayOfYear     => date.Value.DayOfYear,
				DateParts.Day           => date.Value.Day,
				DateParts.Week          => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
				DateParts.WeekDay       => ((int)date.Value.DayOfWeek + 1 + DateFirst + 6) % 7 + 1,
				DateParts.Hour          => date.Value.Hour,
				DateParts.Minute        => date.Value.Minute,
				DateParts.Second        => date.Value.Second,
				DateParts.Millisecond   => date.Value.Millisecond,
				_                       => ThrowHelper.ThrowInvalidOperationException<int>(),
			};
		}

		#endregion DatePart

		#region DateAdd

		class DateAddBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<DateParts>("part");
				var partStr = DatePartBuilder.DatePartToStr(part);
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number", true);

				builder.ResultExpression = new SqlFunction(typeof(DateTime?), builder.Expression,
					new SqlExpression(partStr, Precedence.Primary), number, date);
			}
		}

		class DateAddBuilderOracle : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				var expStr = part switch
				{
					DateParts.Year        => "{0} * INTERVAL '1' YEAR",
					DateParts.Quarter     => "{0} * INTERVAL '3' MONTH",
					DateParts.Month       => "{0} * INTERVAL '1' MONTH",
					DateParts.DayOfYear   or 
					DateParts.WeekDay     or 
					DateParts.Day         => "{0} * INTERVAL '1' DAY",
					DateParts.Week        => "{0} * INTERVAL '7' DAY",
					DateParts.Hour        => "{0} * INTERVAL '1' HOUR",
					DateParts.Minute      => "{0} * INTERVAL '1' MINUTE",
					DateParts.Second      => "{0} * INTERVAL '1' SECOND",
					DateParts.Millisecond => "{0} * INTERVAL '0.001' SECOND",
					_ => ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}"),
				};

				builder.ResultExpression = builder.Add(
					date,
					new SqlExpression(typeof(TimeSpan?), expStr, Precedence.Multiplicative, number),
					typeof(DateTime?));
			}
		}

		class DateAddBuilderDB2 : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				var expStr = part switch
				{
					DateParts.Year        => "{0} Year",
					DateParts.Quarter     => "({0} * 3) Month",
					DateParts.Month       => "{0} Month",
					DateParts.DayOfYear   or
					DateParts.WeekDay     or
					DateParts.Day         => "{0} Day",
					DateParts.Week        => "({0} * 7) Day",
					DateParts.Hour        => "{0} Hour",
					DateParts.Minute      => "{0} Minute",
					DateParts.Second      => "{0} Second",
					DateParts.Millisecond => "({0} / 1000.0) Second",
					_ => ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}"),
				};

				builder.ResultExpression = builder.Add(
					date,
					new SqlExpression(typeof(TimeSpan?), expStr, Precedence.Primary, number),
					typeof(DateTime?));
			}
		}

		class DateAddBuilderInformix : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				var expStr = part switch
				{
					DateParts.Year        => "{0} + Interval({1}) Year to Year",
					DateParts.Quarter     => "{0} + Interval({1}) Month to Month * 3",
					DateParts.Month       => "{0} + Interval({1}) Month to Month",
					DateParts.DayOfYear   or
					DateParts.WeekDay     or
					DateParts.Day         => "{0} + Interval({1}) Day to Day",
					DateParts.Week        => "{0} + Interval({1}) Day to Day * 7",
					DateParts.Hour        => "{0} + Interval({1}) Hour to Hour",
					DateParts.Minute      => "{0} + Interval({1}) Minute to Minute",
					DateParts.Second      => "{0} + Interval({1}) Second to Second",
					DateParts.Millisecond => "{0} + Interval({1}) Second to Fraction * 1000",
					_ => ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}"),
				};

				builder.ResultExpression = new SqlExpression(typeof(DateTime?), expStr, Precedence.Additive, date, number);
			}
		}

		class DateAddBuilderPostgreSQL : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				var expStr = part switch
				{
					DateParts.Year        => "{0} * Interval '1 Year'",
					DateParts.Quarter     => "{0} * Interval '1 Month' * 3",
					DateParts.Month       => "{0} * Interval '1 Month'",
					DateParts.DayOfYear   or
					DateParts.WeekDay     or
					DateParts.Day         => "{0} * Interval '1 Day'",
					DateParts.Week        => "{0} * Interval '1 Day' * 7",
					DateParts.Hour        => "{0} * Interval '1 Hour'",
					DateParts.Minute      => "{0} * Interval '1 Minute'",
					DateParts.Second      => "{0} * Interval '1 Second'",
					DateParts.Millisecond => "{0} * Interval '1 Millisecond'",
					_ => ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}"),
				};

				builder.ResultExpression = builder.Add(
					date,
					new SqlExpression(typeof(TimeSpan?), expStr, Precedence.Multiplicative, number),
					typeof(DateTime?));
			}
		}

		class DateAddBuilderMySql : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				var expStr = part switch 
				{
					DateParts.Year        => "Interval {0} Year",
					DateParts.Quarter     => "Interval {0} Quarter",
					DateParts.Month       => "Interval {0} Month",
					DateParts.DayOfYear   or
					DateParts.WeekDay     or
					DateParts.Day         => "Interval {0} Day",
					DateParts.Week        => "Interval {0} Week",
					DateParts.Hour        => "Interval {0} Hour",
					DateParts.Minute      => "Interval {0} Minute",
					DateParts.Second      => "Interval {0} Second",
					DateParts.Millisecond => "Interval {0} Millisecond",
					_ => ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}"),
				};

				builder.ResultExpression = new SqlFunction(typeof(DateTime?), "Date_Add", date,
					new SqlExpression(expStr, Precedence.Primary, number));
			}
		}

		class DateAddBuilderSQLite : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				string expStr = "strftime('%Y-%m-%d %H:%M:%f', {0}," +
					part switch
					{
						DateParts.Year        =>            "{1} || ' Year')",
						DateParts.Quarter     =>       "({1}*3) || ' Month')",
						DateParts.Month       =>           "{1} || ' Month')",
						DateParts.DayOfYear   or
						DateParts.WeekDay     or
						DateParts.Day         =>             "{1} || ' Day')",
						DateParts.Week        =>         "({1}*7) || ' Day')",
						DateParts.Hour        =>            "{1} || ' Hour')",
						DateParts.Minute      =>          "{1} || ' Minute')",
						DateParts.Second      =>          "{1} || ' Second')",
						DateParts.Millisecond => "({1}/1000.0) || ' Second')",
						_ => ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}"),
					};

				builder.ResultExpression = new SqlExpression(typeof(DateTime?), expStr, Precedence.Concatenate, date, number);
			}
		}

		class DateAddBuilderAccess : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				var partStr = part switch
				{
					DateParts.Year      => "yyyy",
					DateParts.Quarter   => "q",
					DateParts.Month     => "m",
					DateParts.DayOfYear => "y",
					DateParts.Day       => "d",
					DateParts.Week      => "ww",
					DateParts.WeekDay   => "w",
					DateParts.Hour      => "h",
					DateParts.Minute    => "n",
					DateParts.Second    => "s",
					_                       => ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}"),
				};
				builder.ResultExpression = new SqlFunction(typeof(DateTime?), "DateAdd",
					new SqlValue(partStr), number, date);
			}
		}

		class DateAddBuilderSapHana : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				string function;
				switch (part)
				{
					case DateParts.Year        : function = "Add_Years";   break;
					case DateParts.Quarter     :
						function = "Add_Months";
						number   = builder.Mul(number, 3);
						break;
					case DateParts.Month       : function = "Add_Months";  break;
					case DateParts.DayOfYear   :
					case DateParts.Day         :
					case DateParts.WeekDay     : function = "Add_Days";    break;
					case DateParts.Week        :
						function = "Add_Days";
						number   = builder.Mul(number, 7);
						break;
					case DateParts.Hour        :
						function = "Add_Seconds";
						number   = builder.Mul(number, 3600);
						break;
					case DateParts.Minute      :
						function = "Add_Seconds";
						number   = builder.Mul(number, 60);
						break;
					case DateParts.Second      : function = "Add_Seconds"; break;
					case DateParts.Millisecond:
						function = "Add_Seconds";
						number = builder.Div(number, 1000);
						break;
					default:
						ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}");
						return;
				}

				builder.ResultExpression = new SqlFunction(typeof(DateTime?), function, date, number);
			}
		}

		class DateAddBuilderFirebird : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				switch (part)
				{
					case DateParts.Quarter   :
						part   = DateParts.Month;
						number  = builder.Mul(number, 3);
						break;
					case DateParts.DayOfYear :
					case DateParts.WeekDay   :
						part   = DateParts.Day;
						break;
					case DateParts.Week      :
						part   = DateParts.Day;
						number = builder.Mul(number, 7);
						break;
				}

				var partSql = new SqlExpression(part.ToString());

				builder.ResultExpression = new SqlFunction(typeof(DateTime?), "DateAdd", partSql, number, date);
			}
		}

		class DateAddBuilderClickHouse : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				string? function = null;
				switch (part)
				{
					case DateParts.Year       : function = "addYears";    break;
					case DateParts.Quarter    : function = "addQuarters"; break;
					case DateParts.Month      : function = "addMonths";   break;
					case DateParts.DayOfYear  :
					case DateParts.Day        :
					case DateParts.WeekDay    : function = "addDays";     break;
					case DateParts.Week       : function = "addWeeks";    break;
					case DateParts.Hour       : function = "addHours";    break;
					case DateParts.Minute     : function = "addMinutes";  break;
					case DateParts.Second     : function = "addSeconds";  break;
					case DateParts.Millisecond:
					{
						builder.ResultExpression = new SqlExpression(
							typeof(DateTime?),
							"fromUnixTimestamp64Nano(toInt64(toUnixTimestamp64Nano(toDateTime64({0}, 9)) + toInt64({1}) * 1000000))",
							Precedence.Primary,
							date,
							number);
						break;
					}
					default:
					{
						ThrowHelper.ThrowInvalidOperationException($"Unexpected datepart: {part}");
						return;
					}
				}

				if (function != null)
					builder.ResultExpression = new SqlFunction(typeof(DateTime?), function, date, number);
			}
		}

		[Extension("DateAdd"        , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilder))]
		[Extension(PN.Oracle,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderOracle))]
		[Extension(PN.DB2,        "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderDB2))]
		[Extension(PN.Informix,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderInformix))]
		[Extension(PN.PostgreSQL, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderPostgreSQL))]
		[Extension(PN.MySql,      "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderMySql))]
		[Extension(PN.SQLite,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderSQLite))]
		[Extension(PN.Access,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderAccess))]
		[Extension(PN.SapHana,    "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderSapHana))]
		[Extension(PN.Firebird,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderFirebird))]
		[Extension(PN.ClickHouse, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderClickHouse))]
		public static DateTime? DateAdd([SqlQueryDependent] DateParts part, double? number, DateTime? date)
		{
			if (number == null || date == null)
				return null;

			return part switch
			{
				DateParts.Year          => date.Value.AddYears((int)number),
				DateParts.Quarter       => date.Value.AddMonths((int)number * 3),
				DateParts.Month         => date.Value.AddMonths((int)number),
				DateParts.DayOfYear     => date.Value.AddDays(number.Value),
				DateParts.Day           => date.Value.AddDays(number.Value),
				DateParts.Week          => date.Value.AddDays(number.Value * 7),
				DateParts.WeekDay       => date.Value.AddDays(number.Value),
				DateParts.Hour          => date.Value.AddHours(number.Value),
				DateParts.Minute        => date.Value.AddMinutes(number.Value),
				DateParts.Second        => date.Value.AddSeconds(number.Value),
				DateParts.Millisecond   => date.Value.AddMilliseconds(number.Value),
				_                       => ThrowHelper.ThrowInvalidOperationException<DateTime?>(),
			};
		}

		#endregion

		#region DateDiff

		class DateDiffBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part      = builder.GetValue<DateParts>(0);
				var startdate = builder.GetExpression(1);
				var endDate   = builder.GetExpression(2);
				var partSql   = new SqlExpression(DatePartBuilder.DatePartToStr(part), Precedence.Primary);

				builder.ResultExpression = new SqlFunction(typeof(int), builder.Expression, partSql, startdate, endDate);
			}
		}

		class DateDiffBuilderSapHana : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part       = builder.GetValue<DateParts>(0);
				var startdate  = builder.GetExpression(1);
				var endDate    = builder.GetExpression(2);

				var (funcName, divider) = part switch
				{
					DateParts.Day         => ("Days_Between"   , 1),
					DateParts.Hour        => ("Seconds_Between", 3600),
					DateParts.Minute      => ("Seconds_Between", 60),
					DateParts.Second      => ("Seconds_Between", 1),
					DateParts.Millisecond => ("Nano100_Between", 10000),
					_ => ThrowHelper.ThrowInvalidOperationException<(string, int)>($"Unexpected datepart: {part}"),
				};

				ISqlExpression func = new SqlFunction(typeof(int), funcName, startdate, endDate);
				if (divider != 1)
					func = builder.Div(func, divider);

				builder.ResultExpression = func;
			}
		}

		class DateDiffBuilderDB2 : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part       = builder.GetValue<DateParts>(0);
				var startDate  = builder.GetExpression(1);
				var endDate    = builder.GetExpression(2);

				var secondsExpr = builder.Mul<int>(builder.Sub<int>(
						new SqlFunction(typeof(int), "Days", endDate),
						new SqlFunction(typeof(int), "Days", startDate)),
					new SqlValue(86400));

				var midnight = builder.Sub<int>(
					new SqlFunction(typeof(int), "MIDNIGHT_SECONDS", endDate),
					new SqlFunction(typeof(int), "MIDNIGHT_SECONDS", startDate));

				var resultExpr = builder.Add<int>(secondsExpr, midnight);

				resultExpr = part switch
				{
					DateParts.Day         => builder.Div(resultExpr, 86400),
					DateParts.Hour        => builder.Div(resultExpr, 3600),
					DateParts.Minute      => builder.Div(resultExpr, 60),
					DateParts.Second      => resultExpr,
					DateParts.Millisecond =>
						resultExpr = builder.Add<int>(
							builder.Mul(resultExpr, 1000),
							builder.Div(
								builder.Sub<int>(
									new SqlFunction(typeof(int), "MICROSECOND", endDate),
									new SqlFunction(typeof(int), "MICROSECOND", startDate)),
								1000)),
					_ => ThrowHelper.ThrowInvalidOperationException<ISqlExpression>($"Unexpected datepart: {part}"),
				};

				builder.ResultExpression = resultExpr;
			}
		}

		class DateDiffBuilderSQLite : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<DateParts>(0);
				var startDate = builder.GetExpression(1);
				var endDate = builder.GetExpression(2);

				var expStr = "round((julianday({1}) - julianday({0}))";
				expStr += part switch
				{
					DateParts.Day         => ")",
					DateParts.Hour        => " * 24)",
					DateParts.Minute      => " * 1440)",
					DateParts.Second      => " * 86400)",
					DateParts.Millisecond => " * 86400000)",
					_                     => ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}"),
				};
				builder.ResultExpression = new SqlExpression(typeof(int), expStr, startDate, endDate );
			}
		}

		class DateDiffBuilderPostgreSql : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<DateParts>(0);
				var startDate = builder.GetExpression(1);
				var endDate = builder.GetExpression(2);
				var expStr = part switch
				{
					DateParts.Year        => "(DATE_PART('year', {1}::date) - DATE_PART('year', {0}::date))",
					DateParts.Month       => "((DATE_PART('year', {1}::date) - DATE_PART('year', {0}::date)) * 12 + (DATE_PART('month', {1}'::date) - DATE_PART('month', {0}::date)))",
					DateParts.Week        => "TRUNC(DATE_PART('day', {1}::timestamp - {0}::timestamp) / 7)",
					DateParts.Day         => "EXTRACT(EPOCH FROM ({1}::timestamp - {0}::timestamp)) / 86400",
					DateParts.Hour        => "EXTRACT(EPOCH FROM ({1}::timestamp - {0}::timestamp)) / 3600",
					DateParts.Minute      => "EXTRACT(EPOCH FROM ({1}::timestamp - {0}::timestamp)) / 60",
					DateParts.Second      => "EXTRACT(EPOCH FROM ({1}::timestamp - {0}::timestamp))",
					DateParts.Millisecond => "ROUND(EXTRACT(EPOCH FROM ({1}::timestamp - {0}::timestamp)) * 1000)",
					_                     => ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}"),
				};
				builder.ResultExpression = new SqlExpression(typeof(int), expStr, Precedence.Multiplicative, startDate, endDate);
			}
		}

		class DateDiffBuilderAccess : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<DateParts>(0);
				var startDate = builder.GetExpression(1);
				var endDate = builder.GetExpression(2);

				var expStr = "DATEDIFF('";

				expStr += part switch
				{
					DateParts.Year        => "yyyy",
					DateParts.Quarter     => "q",
					DateParts.Month       => "m",
					DateParts.DayOfYear   => "y",
					DateParts.Day         => "d",
					DateParts.WeekDay     => "w",
					DateParts.Week        => "ww",
					DateParts.Hour        => "h",
					DateParts.Minute      => "n",
					DateParts.Second      => "s",
					DateParts.Millisecond => throw new ArgumentOutOfRangeException(nameof(part), part, "Access doesn't support milliseconds interval."),
					_                     => ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}"),
				};

				expStr += "', {0}, {1})";

				builder.ResultExpression = new SqlExpression(typeof(int), expStr, startDate, endDate);
			}
		}

		class DateDiffBuilderOracle : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<DateParts>(0);
				var startDate = builder.GetExpression(1);
				var endDate = builder.GetExpression(2);
				var expStr = part switch
				{
					// DateParts.Year        => "({1} - {0}) / 365",
					// DateParts.Month       => "({1} - {0}) / 30",
					DateParts.Week        => "(CAST ({1} as DATE) - CAST ({0} as DATE)) / 7",
					DateParts.Day         => "(CAST ({1} as DATE) - CAST ({0} as DATE))",
					DateParts.Hour        => "(CAST ({1} as DATE) - CAST ({0} as DATE)) * 24",
					DateParts.Minute      => "(CAST ({1} as DATE) - CAST ({0} as DATE)) * 1440",
					DateParts.Second      => "(CAST ({1} as DATE) - CAST ({0} as DATE)) * 86400",

					// this is tempting to use but leads to precision loss on big intervals
					//DateParts.Millisecond => "1000 * (EXTRACT(SECOND FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP)) + (CAST ({1} as DATE) - CAST ({0} as DATE)) * 86400)",

					// could be really ugly on big start/end expressions
					DateParts.Millisecond => "1000 * (EXTRACT(SECOND FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					                       + " + 60 * (EXTRACT(MINUTE FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					                       + " + 60 * (EXTRACT(HOUR FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					                       + " + 24 * EXTRACT(DAY FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP)))))",
					_                     => ThrowHelper.ThrowInvalidOperationException<string>($"Unexpected datepart: {part}"),
				};
				builder.ResultExpression = new SqlExpression(typeof(int), expStr, Precedence.Multiplicative, startDate, endDate);
			}
		}

		class DateDiffBuilderClickHouse : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part       = builder.GetValue<DateParts>(0);
				var startDate  = builder.GetExpression(1);
				var endDate    = builder.GetExpression(2);

				string? unit = null;
				switch (part)
				{
					case DateParts.Year   : unit = "year"   ; break;
					case DateParts.Quarter: unit = "quarter"; break;
					case DateParts.Month  : unit = "month"  ; break;
					case DateParts.Week   : unit = "week"   ; break;
					case DateParts.Day    : unit = "day"    ; break;
					case DateParts.Hour   : unit = "hour"   ; break;
					case DateParts.Minute : unit = "minute" ; break;
					case DateParts.Second : unit = "second" ; break;

					case DateParts.Millisecond:
						builder.ResultExpression = new SqlExpression(
							typeof(long?),
							"toUnixTimestamp64Milli(toDateTime64({1}, 3)) - toUnixTimestamp64Milli(toDateTime64({0}, 3))",
							Precedence.Subtraction,
							startDate,
							endDate);
						break;

					default:
						ThrowHelper.ThrowInvalidOperationException($"Unexpected datepart: {part}");
						break;
				}

				if (unit != null)
					builder.ResultExpression = new SqlFunction(typeof(int), "date_diff", new SqlValue(unit), startDate, endDate);
			}
		}

		[CLSCompliant(false)]
		[Extension(               "DateDiff",      BuilderType = typeof(DateDiffBuilder))]
		[Extension(PN.MySql,      "TIMESTAMPDIFF", BuilderType = typeof(DateDiffBuilder))]
		[Extension(PN.DB2,        "",              BuilderType = typeof(DateDiffBuilderDB2))]
		[Extension(PN.SapHana,    "",              BuilderType = typeof(DateDiffBuilderSapHana))]
		[Extension(PN.SQLite,     "",              BuilderType = typeof(DateDiffBuilderSQLite))]
		[Extension(PN.Oracle,     "",              BuilderType = typeof(DateDiffBuilderOracle))]
		[Extension(PN.PostgreSQL, "",              BuilderType = typeof(DateDiffBuilderPostgreSql))]
		[Extension(PN.Access,     "",              BuilderType = typeof(DateDiffBuilderAccess))]
		[Extension(PN.ClickHouse, "",              BuilderType = typeof(DateDiffBuilderClickHouse))]
		public static int? DateDiff(DateParts part, DateTime? startDate, DateTime? endDate)
		{
			if (startDate == null || endDate == null)
				return null;

			return part switch
			{
				DateParts.Day         => (int)(endDate - startDate).Value.TotalDays,
				DateParts.Hour        => (int)(endDate - startDate).Value.TotalHours,
				DateParts.Minute      => (int)(endDate - startDate).Value.TotalMinutes,
				DateParts.Second      => (int)(endDate - startDate).Value.TotalSeconds,
				DateParts.Millisecond => (int)(endDate - startDate).Value.TotalMilliseconds,
				_                     => ThrowHelper.ThrowInvalidOperationException<int>(),
			};
		}

		#endregion
	}
}
