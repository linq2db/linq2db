using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using LinqToDB;
using LinqToDB.Expressions.Internal;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using PN = LinqToDB.ProviderName;

namespace Tests.Linq
{
	public static class TestedExtensions
	{
		sealed class DatePartBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>("part");
				var partStr = part switch
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
					_ => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.AddExpression("part", partStr);
			}
		}

		sealed class DatePartBuilderMySql : Sql.IExtensionCallBuilder
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
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary)!);
						break;
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

		sealed class DatePartBuilderPostgre : Sql.IExtensionCallBuilder
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
						builder.Expression = "Extract(dow from {date})";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary)!);
						break;
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

		sealed class DatePartBuilderSqLite : Sql.IExtensionCallBuilder
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
						builder.ResultExpression = builder.Inc(builder.Div(builder.Dec(builder.ConvertToSqlExpression(Precedence.Primary)!), 3));
						break;
					case Sql.DateParts.Month       : partStr = "m"; break;
					case Sql.DateParts.DayOfYear   : partStr = "j"; break;
					case Sql.DateParts.Day         : partStr = "d"; break;
					case Sql.DateParts.Week        : partStr = "W"; break;
					case Sql.DateParts.WeekDay     :
						builder.Expression = "Cast(strFTime('%w', {date}) as int)";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary)!);
						break;
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

		sealed class DatePartBuilderAccess : Sql.IExtensionCallBuilder
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

		sealed class DatePartBuilderSapHana : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>("part");
				var partStr = part switch
				{
					Sql.DateParts.Year      => "Year({date})",
					Sql.DateParts.Quarter   => "Floor((Month({date})-1) / 3) + 1",
					Sql.DateParts.Month     => "Month({date})",
					Sql.DateParts.DayOfYear => "DayOfYear({date})",
					Sql.DateParts.Day       => "DayOfMonth({date})",
					Sql.DateParts.Week      => "Week({date})",
					Sql.DateParts.WeekDay   => "MOD(Weekday({date}) + 1, 7) + 1",
					Sql.DateParts.Hour      => "Hour({date})",
					Sql.DateParts.Minute    => "Minute({date})",
					Sql.DateParts.Second    => "Second({date})",
					_ => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.Expression = partStr;
			}
		}

		sealed class DatePartBuilderInformix : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>("part");
				var partStr = part switch
				{
					Sql.DateParts.Year          => "Year({date})",
					Sql.DateParts.Quarter       => "((Month({date}) - 1) / 3 + 1)",
					Sql.DateParts.Month         => "Month({date})",
					Sql.DateParts.DayOfYear     => "(Mdy(Month({date}), Day({date}), Year({date})) - Mdy(1, 1, Year({date})) + 1)",
					Sql.DateParts.Day           => "Day({date})",
					Sql.DateParts.Week          => "((Extend({date}, year to day) - (Mdy(12, 31 - WeekDay(Mdy(1, 1, year({date}))), Year({date}) - 1) + Interval(1) day to day)) / 7 + Interval(1) day to day)::char(10)::int",
					Sql.DateParts.WeekDay       => "(weekDay({date}) + 1)",
					Sql.DateParts.Hour          => "({date}::datetime Hour to Hour)::char(3)::int",
					Sql.DateParts.Minute        => "({date}::datetime Minute to Minute)::char(3)::int",
					Sql.DateParts.Second        => "({date}::datetime Second to Second)::char(3)::int",
					Sql.DateParts.Millisecond   => "Millisecond({date})",
					_ => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.Expression = partStr;
			}
		}

		sealed class DatePartBuilderOracle : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>("part");
				var partStr = part switch
				{
					Sql.DateParts.Year          => "To_Number(To_Char({date}, 'YYYY'))",
					Sql.DateParts.Quarter       => "To_Number(To_Char({date}, 'Q'))",
					Sql.DateParts.Month         => "To_Number(To_Char({date}, 'MM'))",
					Sql.DateParts.DayOfYear     => "To_Number(To_Char({date}, 'DDD'))",
					Sql.DateParts.Day           => "To_Number(To_Char({date}, 'DD'))",
					Sql.DateParts.Week          => "To_Number(To_Char({date}, 'WW'))",
					Sql.DateParts.WeekDay       => "Mod(1 + Trunc({date}) - Trunc({date}, 'IW'), 7) + 1",
					Sql.DateParts.Hour          => "To_Number(To_Char({date}, 'HH24'))",
					Sql.DateParts.Minute        => "To_Number(To_Char({date}, 'MI'))",
					Sql.DateParts.Second        => "To_Number(To_Char({date}, 'SS'))",
					Sql.DateParts.Millisecond   => "To_Number(To_Char({date}, 'FF'))",
					_ => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.Expression = partStr;
			}
		}

		sealed class DatePartBuilderClickHouse : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string exprStr;
				var part = builder.GetValue<Sql.DateParts>("part");

				switch (part)
				{
					case Sql.DateParts.Year       : exprStr = "YEAR({date})"                      ; break;
					case Sql.DateParts.Quarter    : exprStr = "QUARTER({date})"                   ; break;
					case Sql.DateParts.Month      : exprStr = "MONTH({date})"                     ; break;
					case Sql.DateParts.DayOfYear  : exprStr = "DAYOFYEAR({date})"                 ; break;
					case Sql.DateParts.Day        : exprStr = "DAY({date})"                       ; break;
					case Sql.DateParts.Week       : exprStr = "toISOWeek(toDateTime64({date}, 0))"; break;
					case Sql.DateParts.Hour       : exprStr = "HOUR({date})"                      ; break;
					case Sql.DateParts.Minute     : exprStr = "MINUTE({date})"                    ; break;
					case Sql.DateParts.Second     : exprStr = "SECOND({date})"                    ; break;
					case Sql.DateParts.WeekDay    :
						builder.Expression = "DAYOFWEEK(addDays({date}, 1))";
						builder.Extension.Precedence = Precedence.Additive;
						return;
					case Sql.DateParts.Millisecond:
						builder.Expression = "toUnixTimestamp64Milli({date}) % 1000";
						builder.Extension.Precedence = Precedence.Multiplicative;
						return;
					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				builder.Expression = exprStr;
			}
		}

		sealed class DatePartBuilderDB2 : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>("part");
				var partStr = part switch
				{
					Sql.DateParts.Year          => "To_Number(To_Char({date}, 'YYYY'))",
					Sql.DateParts.Quarter       => "To_Number(To_Char({date}, 'Q'))",
					Sql.DateParts.Month         => "To_Number(To_Char({date}, 'MM'))",
					Sql.DateParts.DayOfYear     => "To_Number(To_Char({date}, 'DDD'))",
					Sql.DateParts.Day           => "To_Number(To_Char({date}, 'DD'))",
					Sql.DateParts.Week          => "To_Number(To_Char({date}, 'WW'))",
					Sql.DateParts.WeekDay       => "DayOfWeek({date})",
					Sql.DateParts.Hour          => "To_Number(To_Char({date}, 'HH24'))",
					Sql.DateParts.Minute        => "To_Number(To_Char({date}, 'MI'))",
					Sql.DateParts.Second        => "To_Number(To_Char({date}, 'SS'))",
					Sql.DateParts.Millisecond   => "To_Number(To_Char({date}, 'FF')) / 1000",
					_ => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.Expression = partStr;
			}
		}

		sealed class DatePartBuilderFirebird : Sql.IExtensionCallBuilder
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
						builder.ResultExpression = builder.Inc(builder.Div(builder.Dec(builder.ConvertToSqlExpression(Precedence.Primary)!), 3));
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
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary)!);
						break;
				}
			}
		}

		[Sql.Extension(               "DatePart({part}, {date})",                 ServerSideOnly = false, BuilderType = typeof(DatePartBuilder))]
		[Sql.Extension(PN.DB2,        "",                                         ServerSideOnly = false, BuilderType = typeof(DatePartBuilderDB2))] // TODO: Not checked
		[Sql.Extension(PN.Informix,   "",                                         ServerSideOnly = false, BuilderType = typeof(DatePartBuilderInformix))] // TODO: Not checked
		[Sql.Extension(PN.MySql,      "Extract({part} from {date})",              ServerSideOnly = false, BuilderType = typeof(DatePartBuilderMySql))]
		[Sql.Extension(PN.PostgreSQL, "Extract({part} from {date})",              ServerSideOnly = false, BuilderType = typeof(DatePartBuilderPostgre))]
		[Sql.Extension(PN.Firebird,   "Extract({part} from {date})",              ServerSideOnly = false, BuilderType = typeof(DatePartBuilderFirebird))]
		[Sql.Extension(PN.SQLite,     "Cast(StrFTime('%{part}', {date}) as int)", ServerSideOnly = false, BuilderType = typeof(DatePartBuilderSqLite))]
		[Sql.Extension(PN.Access,     "DatePart('{part}', {date})",               ServerSideOnly = false, BuilderType = typeof(DatePartBuilderAccess))]
		[Sql.Extension(PN.SapHana,    "",                                         ServerSideOnly = false, BuilderType = typeof(DatePartBuilderSapHana))]
		[Sql.Extension(PN.Oracle,     "",                                         ServerSideOnly = false, BuilderType = typeof(DatePartBuilderOracle))]
		[Sql.Extension(PN.ClickHouse, "",                                         ServerSideOnly = false, BuilderType = typeof(DatePartBuilderClickHouse))]
		public static int? DatePart(this Sql.ISqlExtension? ext, Sql.DateParts part, [ExprParameter] DateTime? date)
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
				_ => throw new InvalidOperationException(),
			};
		}
	}

	public class SqlExtensionTests : TestBase
	{
		#region DatePart

		[Test]
		public void DatePartYear([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.Ext.DatePart(Sql.DateParts.Year, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Year, t.DateTimeValue)));
		}

		[Test]
		public void DatePartQuarter([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.Ext.DatePart(Sql.DateParts.Quarter, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Quarter, t.DateTimeValue)));
		}

		[Test]
		public void DatePartMonth([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.Ext.DatePart(Sql.DateParts.Month, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Month, t.DateTimeValue)));
		}

		[Test]
		public void DatePartDayOfYear([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.Ext.DatePart(Sql.DateParts.DayOfYear, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.DayOfYear, t.DateTimeValue)));
		}

		[Test]
		public void DatePartDay([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.Ext.DatePart(Sql.DateParts.Day, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Day, t.DateTimeValue)));
		}

		[Test]
		public void DatePartWeek([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				(from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Week, t.DateTimeValue))).ToList();
		}

		[Test]
		public void DatePartWeekDay([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.Ext.DatePart(Sql.DateParts.WeekDay, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.WeekDay, t.DateTimeValue)));
		}

		[Test]
		public void DatePartHour([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.Ext.DatePart(Sql.DateParts.Hour, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Hour, t.DateTimeValue)));
		}

		[Test]
		public void DatePartMinute([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.Ext.DatePart(Sql.DateParts.Minute, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Minute, t.DateTimeValue)));
		}

		[Test]
		public void DatePartSecond([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.Ext.DatePart(Sql.DateParts.Second, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Second, t.DateTimeValue)));
		}

		[Test]
		public void DatePartMillisecond([DataSources(TestProvName.AllInformix, TestProvName.AllMySql, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.Ext.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue)));
		}

		#endregion

		#region Issue 4222

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4222")]
		public void Issue4222Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Entry>();

			var ek = new EntryKey("default", 2007);
			var result = tb.Where(e => KeyEquals(e, ek)).ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4222")]
		public void Issue4222Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Entry>();

			var ek = new EntryKey[]
			{
				new EntryKey("default", 2007),
				new EntryKey("other", 2008)
			};
			var result = tb.Where(e => KeysEquals(e, ek)).ToArray();
		}

		[Sql.Extension(typeof(CompositeKeyEqualsExtensionBuilder), IsPredicate = true, ServerSideOnly = true)]
		static bool KeyEquals<TKey>(IKeyProvider<TKey> entity, TKey value)
			where TKey : notnull
			=> throw new ServerSideOnlyException(nameof(KeyEquals));

		[Sql.Extension(typeof(CompositeKeyEqualsExtensionBuilder), IsPredicate = true, ServerSideOnly = true)]
		static bool KeysEquals<TKey>(IKeyProvider<TKey> entity, TKey[] values)
			where TKey : notnull
			=> throw new ServerSideOnlyException(nameof(KeyEquals));

		class CompositeKeyEqualsExtensionBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var entityType = builder.Arguments[0].Type;
				var entityDescriptor = builder.DataContext.MappingSchema.GetEntityDescriptor(entityType);
				var tableExp = builder.GetExpression(0)!;

				ISqlTableSource? table = null;

				if (tableExp is SqlField field)
				{
					if (field.Table is SqlTable sqlTable)
						table = sqlTable;
					else if (field.Table is SelectQuery select)
						table = select.From.Tables[0].Source;
				}

				if (table == null)
				{
					builder.IsConvertible = false;
					return;
				}

				var keyProviderType = entityType
					.GetInterfaces()
					.First(static i => i.IsConstructedGenericType && i.GetGenericTypeDefinition() == typeof(IKeyProvider<>));
				var keyType = keyProviderType.GetGenericArguments().First();
				var value = builder.Arguments[1].EvaluateExpression();

				var left = new SqlRowExpression(
					keyType.GetRuntimeProperties()
						.Where(prop => !prop.HasAttribute<NotColumnAttribute>())
						.Select(prop => entityType.GetProperties().First(eProp => eProp.Name.Equals(prop.Name)))
						.Select(prop => new SqlField(entityDescriptor.Columns.First(c => c.MemberInfo == prop))
						{
							Table = table
						})
					// .Select(fieldExp => new SqlExpression("{0}.{1}", tableExp, fieldExp)) // trying to make table.Field
						.Cast<ISqlExpression>()
						.ToArray());

				if (value!.GetType().IsArray)
				{
					var values = new List<SqlRowExpression>();
					foreach (var v in (IEnumerable)value)
					{
						var row = new SqlRowExpression(
						keyType.GetRuntimeProperties()
							.Where(prop => !prop.HasAttribute<NotColumnAttribute>())
							.Select(prop => new SqlValue(prop.PropertyType, prop.GetValue(v)))
							.Cast<ISqlExpression>()
							.ToArray());
						values.Add(row);
					}

					builder.ResultExpression = new SqlSearchCondition(false, canBeUnknown: null, new SqlPredicate.InList(left, null, false, values));
				}
				else
				{
					var right = new SqlRowExpression(
						keyType.GetRuntimeProperties()
							.Where(prop => !prop.HasAttribute<NotColumnAttribute>())
							.Select(prop => new SqlValue(prop.PropertyType, prop.GetValue(value)))
							.Cast<ISqlExpression>()
							.ToArray());
					builder.ResultExpression = new SqlSearchCondition(false, canBeUnknown: null, new SqlPredicate.ExprExpr(left, SqlPredicate.Operator.Equal, right, null));
				}
			}
		}

		interface IEntryKey
		{
			public string RecSrc { get; }
			public int Value { get; }
		}

		record EntryKey(string RecSrc, int Value) : IEntryKey;

		interface IKeyProvider<out TKey>
			where TKey : notnull
		{
			[NotColumn]
			public TKey Key { get; }
		}

		sealed class Entry : IKeyProvider<IEntryKey>, IEntryKey
		{
			public Guid Id { get; set; }
			public string RecSrc { get; set; } = "default";
			public int Value { get; set; }

			public IEntryKey Key => this;
		}
		#endregion
	}
}
