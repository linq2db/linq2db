using System;
using System.Globalization;

namespace LinqToDB
{
	using SqlQuery;
	using Expressions;

	using PN = ProviderName;

	public partial class Sql
	{
		[Sql.Enum]
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
			/// C# evaluation: <c>CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday)</c>
			/// Databases:
			/// US numbering schema used by: MS Access, SQL CE, SQL Server, SAP/Sybase ASE, Informix databases;
			/// US 0-based numbering schema used by MySQL database;
			/// ISO numbering schema with incorrect numbering of first week used by: SAP HANA database;
			/// ISO numbering schema with proper numbering of first week used by: Firebird, PostgreSQL databases;
			/// Primitive (each 7 days counted as week) numbering schema: DB2, Oracle databases;
			/// SQLite numbering logic cannot be classified by human being.
			/// </summary>
			Week        =  5,
			WeekDay     =  6,
			Hour        =  7,
			Minute      =  8,
			Second      =  9,
			Millisecond = 10,
		}

		#region DatePart

		internal class DatePartBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var partStr = DatePartToStr(part);
				var date    = builder.GetExpression("date");

				builder.ResultExpression = new SqlFunction(typeof(int), builder.Expression,
					new SqlExpression(partStr, Precedence.Primary), date);
			}

			public static string DatePartToStr(DateParts part)
			{
				return part switch
				{
					Sql.DateParts.Year          => "year",
					Sql.DateParts.Quarter       => "quarter",
					Sql.DateParts.Month         => "month",
					Sql.DateParts.DayOfYear     => "dayofyear",
					Sql.DateParts.Day           => "day",
					Sql.DateParts.Week          => "week",
					Sql.DateParts.WeekDay       => "weekday",
					Sql.DateParts.Hour          => "hour",
					Sql.DateParts.Minute        => "minute",
					Sql.DateParts.Second        => "second",
					Sql.DateParts.Millisecond   => "millisecond",
					_                           => throw new InvalidOperationException($"Unexpected datepart: {part}")
				};
			}
		}

		class DatePartBuilderMySql: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string? partStr = null;
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
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				if (partStr != null)
					builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderPostgre: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string? partStr = null;
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
						builder.AddExpression("part", "dow");
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						return;
					case Sql.DateParts.Hour        : partStr = "hour";    break;
					case Sql.DateParts.Minute      : partStr = "minute";  break;
					case Sql.DateParts.Second      : partStr = "second";  break;
					case Sql.DateParts.Millisecond :
						builder.Expression = "Cast(To_Char({date}, 'MS') as int)";
						break;
					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				if (partStr != null)
					builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderSqLite: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string? partStr = null;
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
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				if (partStr != null)
					builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderAccess: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var partStr = part switch
				{
					Sql.DateParts.Year      => "yyyy",
					Sql.DateParts.Quarter   => "q",
					Sql.DateParts.Month     => "m",
					Sql.DateParts.DayOfYear => "y",
					Sql.DateParts.Day       => "d",
					Sql.DateParts.Week      => "ww",
					Sql.DateParts.WeekDay   => "w",
					Sql.DateParts.Hour      => "h",
					Sql.DateParts.Minute    => "n",
					Sql.DateParts.Second    => "s",
					_ => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderIngres : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>("part");
				var partStr = part switch
				{
					Sql.DateParts.Year      => "year",
					Sql.DateParts.Month     => "month",
					Sql.DateParts.Day       => "day",
					Sql.DateParts.Hour      => "hour",
					Sql.DateParts.Minute    => "minute",
					Sql.DateParts.Second    => "seconds",
					_ => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.Expression = "DATE_PART('" + partStr + "', {date})";
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
						throw new InvalidOperationException($"Unexpected datepart: {part}");
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
						throw new InvalidOperationException($"Unexpected datepart: {part}");
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
						throw new InvalidOperationException($"Unexpected datepart: {part}");
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
						throw new InvalidOperationException($"Unexpected datepart: {part}");
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
						throw new InvalidOperationException($"Unexpected datepart: {part}");
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

		[Sql.Extension(               "DatePart",                                        ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilder))]
		[Sql.Extension(PN.DB2,        "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderDB2))] // TODO: Not checked
		[Sql.Extension(PN.Informix,   "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderInformix))] 
		[Sql.Extension(PN.MySql,      "Extract({part} from {date})",                     ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderMySql))]
		[Sql.Extension(PN.PostgreSQL, "Cast(Floor(Extract({part} from {date})) as int)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderPostgre))]
		[Sql.Extension(PN.Firebird,   "Cast(Floor(Extract({part} from {date})) as int)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderFirebird))]
		[Sql.Extension(PN.SQLite,     "Cast(StrFTime('%{part}', {date}) as int)",        ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSqLite))]
		[Sql.Extension(PN.Access,     "DatePart('{part}', {date})",                      ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderAccess))]
		[Sql.Extension(PN.SapHana,    "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSapHana))]
		[Sql.Extension(PN.Oracle,     "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderOracle))]
		[Sql.Extension(PN.Ingres,     "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderIngres))]
		public static int? DatePart([SqlQueryDependent] Sql.DateParts part, [ExprParameter] DateTime? date)
		{
			if (date == null)
				return null;

			return part switch
			{
				Sql.DateParts.Year          => date.Value.Year,
				Sql.DateParts.Quarter       => (date.Value.Month - 1) / 3 + 1,
				Sql.DateParts.Month         => date.Value.Month,
				Sql.DateParts.DayOfYear     => date.Value.DayOfYear,
				Sql.DateParts.Day           => date.Value.Day,
				Sql.DateParts.Week          => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
				Sql.DateParts.WeekDay       => ((int)date.Value.DayOfWeek + 1 + Sql.DateFirst + 6) % 7 + 1,
				Sql.DateParts.Hour          => date.Value.Hour,
				Sql.DateParts.Minute        => date.Value.Minute,
				Sql.DateParts.Second        => date.Value.Second,
				Sql.DateParts.Millisecond   => date.Value.Millisecond,
				_                           => throw new InvalidOperationException(),
			};
		}

		#endregion DatePart

		#region DateAdd

		class DateAddBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var partStr = DatePartBuilder.DatePartToStr(part);
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number", true);

				builder.ResultExpression = new SqlFunction(typeof(DateTime?), builder.Expression,
					new SqlExpression(partStr, Precedence.Primary), number, date);
			}
		}

		class DateAddBuilderOracle : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<Sql.DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				string expStr;
				switch (part)
				{
					case Sql.DateParts.Year        : expStr = "{0} * INTERVAL '1' YEAR"      ; break;
					case Sql.DateParts.Quarter     : expStr = "{0} * INTERVAL '3' MONTH"     ; break;
					case Sql.DateParts.Month       : expStr = "{0} * INTERVAL '1' MONTH"     ; break;
					case Sql.DateParts.DayOfYear   :
					case Sql.DateParts.WeekDay     :
					case Sql.DateParts.Day         : expStr = "{0} * INTERVAL '1' DAY"       ; break;
					case Sql.DateParts.Week        : expStr = "{0} * INTERVAL '7' DAY"       ; break;
					case Sql.DateParts.Hour        : expStr = "{0} * INTERVAL '1' HOUR"      ; break;
					case Sql.DateParts.Minute      : expStr = "{0} * INTERVAL '1' MINUTE"    ; break;
					case Sql.DateParts.Second      : expStr = "{0} * INTERVAL '1' SECOND"    ; break;
					case Sql.DateParts.Millisecond : expStr = "{0} * INTERVAL '0.001' SECOND"; break;
					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				builder.ResultExpression = builder.Add(
					date,
					new SqlExpression(typeof(TimeSpan?), expStr, Precedence.Multiplicative, number),
					typeof(DateTime?));
			}
		}

		class DateAddBuilderDB2 : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<Sql.DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				string expStr;

				switch (part)
				{
					case Sql.DateParts.Year        : expStr = "{0} Year";                 break;
					case Sql.DateParts.Quarter     : expStr = "({0} * 3) Month";          break;
					case Sql.DateParts.Month       : expStr = "{0} Month";                break;
					case Sql.DateParts.DayOfYear   : 
					case Sql.DateParts.WeekDay     : 
					case Sql.DateParts.Day         : expStr = "{0} Day";                  break;
					case Sql.DateParts.Week        : expStr = "({0} * 7) Day";            break;
					case Sql.DateParts.Hour        : expStr = "{0} Hour";                 break;
					case Sql.DateParts.Minute      : expStr = "{0} Minute";               break;
					case Sql.DateParts.Second      : expStr = "{0} Second";               break;
					case Sql.DateParts.Millisecond : expStr = "({0} / 1000.0) Second";    break;
					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				builder.ResultExpression = builder.Add(
					date,
					new SqlExpression(typeof(TimeSpan?), expStr, Precedence.Primary, number),
					typeof(DateTime?));
			}
		}

		class DateAddBuilderInformix : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<Sql.DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

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
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				builder.ResultExpression = new SqlExpression(typeof(DateTime?), expStr, Precedence.Additive, date, number);
			}
		}

		class DateAddBuilderPostgreSQL : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<Sql.DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				string expStr;
				switch (part)
				{
					case Sql.DateParts.Year        : expStr = "{0} * Interval '1 Year'";         break;
					case Sql.DateParts.Quarter     : expStr = "{0} * Interval '1 Month' * 3";    break;
					case Sql.DateParts.Month       : expStr = "{0} * Interval '1 Month'";        break;
					case Sql.DateParts.DayOfYear   : 
					case Sql.DateParts.WeekDay     : 
					case Sql.DateParts.Day         : expStr = "{0} * Interval '1 Day'";          break;
					case Sql.DateParts.Week        : expStr = "{0} * Interval '1 Day' * 7";      break;
					case Sql.DateParts.Hour        : expStr = "{0} * Interval '1 Hour'";         break;
					case Sql.DateParts.Minute      : expStr = "{0} * Interval '1 Minute'";       break;
					case Sql.DateParts.Second      : expStr = "{0} * Interval '1 Second'";       break;
					case Sql.DateParts.Millisecond : expStr = "{0} * Interval '1 Millisecond'";  break;
					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				builder.ResultExpression = builder.Add(
					date,
					new SqlExpression(typeof(TimeSpan?), expStr, Precedence.Multiplicative, number),
					typeof(DateTime?));
			}
		}

		class DateAddBuilderMySql : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<Sql.DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

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
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				builder.ResultExpression = new SqlFunction(typeof(DateTime?), "Date_Add", date,
					new SqlExpression(expStr, Precedence.Primary, number));
			}
		}

		class DateAddBuilderSQLite : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<Sql.DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				string expStr = "strftime('%Y-%m-%d %H:%M:%f', {0},";
				switch (part)
				{
					case Sql.DateParts.Year        : expStr +=            "{1} || ' Year')"; break;
					case Sql.DateParts.Quarter     : expStr +=       "({1}*3) || ' Month')"; break;
					case Sql.DateParts.Month       : expStr +=           "{1} || ' Month')"; break;
					case Sql.DateParts.DayOfYear   : 
					case Sql.DateParts.WeekDay     : 
					case Sql.DateParts.Day         : expStr +=             "{1} || ' Day')"; break;
					case Sql.DateParts.Week        : expStr +=         "({1}*7) || ' Day')"; break;
					case Sql.DateParts.Hour        : expStr +=            "{1} || ' Hour')"; break;
					case Sql.DateParts.Minute      : expStr +=          "{1} || ' Minute')"; break;
					case Sql.DateParts.Second      : expStr +=          "{1} || ' Second')"; break;
					case Sql.DateParts.Millisecond : expStr += "({1}/1000.0) || ' Second')"; break;
					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				builder.ResultExpression = new SqlExpression(typeof(DateTime?), expStr, Precedence.Concatenate, date, number);
			}
		}

		class DateAddBuilderIngres : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number");
				var partStr = part switch
				{
					Sql.DateParts.Year      => "{0} years",
					Sql.DateParts.Month     => "{0} months",
					Sql.DateParts.Day       => "{0} days",
					Sql.DateParts.Hour      => "{0} hours",
					Sql.DateParts.Minute    => "{0} minutes",
					Sql.DateParts.Second    => "{0} seconds",
					_                       => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.ResultExpression = builder.Add(
					date,
					new SqlExpression(typeof(TimeSpan?), partStr, Precedence.Primary, number),
					typeof(DateTime?));
			}
		}

		class DateAddBuilderAccess : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<Sql.DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				var partStr = part switch
				{
					Sql.DateParts.Year      => "yyyy",
					Sql.DateParts.Quarter   => "q",
					Sql.DateParts.Month     => "m",
					Sql.DateParts.DayOfYear => "y",
					Sql.DateParts.Day       => "d",
					Sql.DateParts.Week      => "ww",
					Sql.DateParts.WeekDay   => "w",
					Sql.DateParts.Hour      => "h",
					Sql.DateParts.Minute    => "n",
					Sql.DateParts.Second    => "s",
					_                       => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.ResultExpression = new SqlFunction(typeof(DateTime?), "DateAdd", 
					new SqlValue(partStr), number, date);
			}
		}

		class DateAddBuilderSapHana : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<Sql.DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

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
					case Sql.DateParts.Millisecond:
						function = "Add_Seconds";
						number = builder.Div(number, 1000);
						break;
					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				builder.ResultExpression = new SqlFunction(typeof(DateTime?), function, date, number);
			}
		}

		class DateAddBuilderFirebird : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<Sql.DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				switch (part)
				{
					case Sql.DateParts.Quarter   :
						part   = DateParts.Month;
						number  = builder.Mul(number, 3);
						break;
					case Sql.DateParts.DayOfYear :
					case Sql.DateParts.WeekDay   :
						part   = DateParts.Day;
						break;
					case Sql.DateParts.Week      :
						part   = DateParts.Day;
						number = builder.Mul(number, 7);
						break;
				}

				var partSql = new SqlExpression(part.ToString());

				builder.ResultExpression = new SqlFunction(typeof(DateTime?), "DateAdd", partSql, number, date);
			}
		}


 
		[Sql.Extension("DateAdd"        , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilder))]
		[Sql.Extension(PN.Oracle,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderOracle))]
		[Sql.Extension(PN.DB2,        "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderDB2))]
		[Sql.Extension(PN.Informix,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderInformix))]
		[Sql.Extension(PN.PostgreSQL, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderPostgreSQL))]
		[Sql.Extension(PN.MySql,      "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderMySql))]
		[Sql.Extension(PN.SQLite,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderSQLite))]
		[Sql.Extension(PN.Access,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderAccess))]
		[Sql.Extension(PN.SapHana,    "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderSapHana))]
		[Sql.Extension(PN.Firebird,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderFirebird))]
		[Sql.Extension(PN.Ingres,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderIngres))]
		public static DateTime? DateAdd([SqlQueryDependent] Sql.DateParts part, double? number, DateTime? date)
		{
			if (number == null || date == null)
				return null;

			return part switch
			{
				Sql.DateParts.Year          => date.Value.AddYears((int)number),
				Sql.DateParts.Quarter       => date.Value.AddMonths((int)number * 3),
				Sql.DateParts.Month         => date.Value.AddMonths((int)number),
				Sql.DateParts.DayOfYear     => date.Value.AddDays(number.Value),
				Sql.DateParts.Day           => date.Value.AddDays(number.Value),
				Sql.DateParts.Week          => date.Value.AddDays(number.Value * 7),
				Sql.DateParts.WeekDay       => date.Value.AddDays(number.Value),
				Sql.DateParts.Hour          => date.Value.AddHours(number.Value),
				Sql.DateParts.Minute        => date.Value.AddMinutes(number.Value),
				Sql.DateParts.Second        => date.Value.AddSeconds(number.Value),
				Sql.DateParts.Millisecond   => date.Value.AddMilliseconds(number.Value),
				_                           => throw new InvalidOperationException(),
			};
		}

		#endregion

		#region DateDiff

		class DateDiffBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part      = builder.GetValue<Sql.DateParts>(0);
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
				var part       = builder.GetValue<Sql.DateParts>(0);
				var startdate  = builder.GetExpression(1);
				var endDate    = builder.GetExpression(2);
				var divider    = 1;

				string funcName;
				switch (part)
				{
					case DateParts.Day        : funcName = "Days_Between";                     break;
					case DateParts.Hour       : funcName = "Seconds_Between"; divider = 3600;  break;
					case DateParts.Minute     : funcName = "Seconds_Between"; divider = 60;    break;
					case DateParts.Second     : funcName = "Seconds_Between";                  break;
					case DateParts.Millisecond: funcName = "Nano100_Between"; divider = 10000; break;
					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

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
				var part       = builder.GetValue<Sql.DateParts>(0);
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

				switch (part)
				{
					case Sql.DateParts.Day         : resultExpr = builder.Div(resultExpr, 86400); break;
					case Sql.DateParts.Hour        : resultExpr = builder.Div(resultExpr, 3600);  break;
					case Sql.DateParts.Minute      : resultExpr = builder.Div(resultExpr, 60);    break;
					case Sql.DateParts.Second      : break;
					case Sql.DateParts.Millisecond :
						resultExpr = builder.Add<int>(
							builder.Mul(resultExpr, 1000),
							builder.Div(
								builder.Sub<int>(
									new SqlFunction(typeof(int), "MICROSECOND", endDate),
									new SqlFunction(typeof(int), "MICROSECOND", startDate)),
								1000));
						break;
					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				builder.ResultExpression = resultExpr;
			}
		}

		class DateDiffBuilderSQLite : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>(0);
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
					_                     => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.ResultExpression = new SqlExpression(typeof(int), expStr, startDate, endDate );
			}
		}

		class DateDiffBuilderPostgreSql : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>(0);
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
					_                     => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.ResultExpression = new SqlExpression(typeof(int), expStr, Precedence.Multiplicative, startDate, endDate);
			}
		}

		class DateDiffBuilderAccess : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>(0);
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
					_                     => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};

				expStr += "', {0}, {1})";

				builder.ResultExpression = new SqlExpression(typeof(int), expStr, startDate, endDate);
			}
		}

		class DateDiffBuilderIngres : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>(0);
				var startDate = builder.GetExpression(1);
				var endDate = builder.GetExpression(2);

				var expStr = "INTERVAL('";

				expStr += part switch
				{
					DateParts.Year        => "year",
					DateParts.Month       => "month",
					DateParts.Day         => "day",
					DateParts.Hour        => "hour",
					DateParts.Minute      => "minute",
					DateParts.Second      => "seconds",
					DateParts.Millisecond => throw new ArgumentOutOfRangeException(nameof(part), part, "Ingres doesn't support milliseconds interval."),
					_                     => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};

				expStr += "', {0}-{1})";

				builder.ResultExpression = new SqlExpression(typeof(int), expStr, startDate, endDate);
			}
		}

		class DateDiffBuilderOracle : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>(0);
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
					_                     => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.ResultExpression = new SqlExpression(typeof(int), expStr, Precedence.Multiplicative, startDate, endDate);
			}
		}

		[CLSCompliant(false)]
		[Sql.Extension(               "DateDiff",      BuilderType = typeof(DateDiffBuilder))]
		[Sql.Extension(PN.MySql,      "TIMESTAMPDIFF", BuilderType = typeof(DateDiffBuilder))]
		[Sql.Extension(PN.DB2,        "",              BuilderType = typeof(DateDiffBuilderDB2))]
		[Sql.Extension(PN.SapHana,    "",              BuilderType = typeof(DateDiffBuilderSapHana))]
		[Sql.Extension(PN.SQLite,     "",              BuilderType = typeof(DateDiffBuilderSQLite))]
		[Sql.Extension(PN.Oracle,     "",              BuilderType = typeof(DateDiffBuilderOracle))]
		[Sql.Extension(PN.PostgreSQL, "",              BuilderType = typeof(DateDiffBuilderPostgreSql))]
		[Sql.Extension(PN.Access,     "",              BuilderType = typeof(DateDiffBuilderAccess))]
		[Sql.Extension(PN.Ingres,     "",              BuilderType = typeof(DateDiffBuilderIngres))]
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
				_                     => throw new InvalidOperationException(),
			};
		}

		#endregion
	}
}
