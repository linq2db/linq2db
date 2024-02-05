using System;
using System.Linq;
using System.Linq.Expressions;
using System.Globalization;

namespace LinqToDB
{
	using Linq;
	using SqlQuery;
	using Expressions;

	using PN = ProviderName;

	public partial class Sql
	{
		[Enum]
		public enum TimeSpanParts
		{
			Days				=  0,
			TotalDays			=  1,
			Hours				=  2,
			TotalHours			=  3,
			Minutes				=  4,
			TotalMinutes        =  5,
			Seconds				=  6,
			TotalSeconds        =  7,
			Milliseconds		=  8,
			TotalMilliseconds   =  9,
			Microseconds		=  10,
			TotalMicroseconds	=  11,
			Nanoseconds			=  12,
			TotalNanoseconds    =  13
		}

		#region TimeSpanPart

		internal sealed class TimeSpanPartBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part     = builder.GetValue<TimeSpanParts>("part");
				var partStr  = TimeSpanPartToStr(part);
				var timeSpan = builder.GetExpression("timeSpan");

				builder.ResultExpression = new SqlExpression(typeof(long), "{0} " + partStr, timeSpan);
			}

			public static string TimeSpanPartToStr(TimeSpanParts part)
			{
				return part switch
				{
					TimeSpanParts.TotalNanoseconds	=> "* 0.01",
					TimeSpanParts.TotalMicroseconds => "/ 10",
					TimeSpanParts.TotalMilliseconds => "/ 10000",
					TimeSpanParts.TotalSeconds		=> "/ 10000000",
					TimeSpanParts.TotalMinutes		=> "/ 600000000",
					TimeSpanParts.TotalHours		=> "/ 36000000000",
					TimeSpanParts.TotalDays         => "/ 864000000000",
					_ => throw new InvalidOperationException($"Unexpected timespanpart: {part}")
				};
			}
		}

		internal sealed class TimeSpanPartBuilderSqlite : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part     = builder.GetValue<TimeSpanParts>("part");
				var partStr  = TimeSpanPartBuilder.TimeSpanPartToStr(part);
				var timeSpan = builder.GetExpression("timeSpan");

				builder.ResultExpression = new SqlExpression(typeof(long), "round({0} " + partStr + ")", timeSpan);
			}
		}

		sealed class TimeSpanPartBuilderIntervalType : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part     = builder.GetValue<TimeSpanParts>("part");
				var partStr  = TimeSpanPartToStr(part);
				var timeSpan = builder.GetExpression("timeSpan");

				var tp = timeSpan.GetExpressionType();
				var dt = tp.DataType;
					
				if (dt == DataType.Int64)
				{
					partStr = TimeSpanPartBuilder.TimeSpanPartToStr(part);
					builder.ResultExpression = new SqlExpression(typeof(long), "{0} " + partStr, timeSpan);
				}
				else
				{
					builder.ResultExpression = new SqlExpression(typeof(long), builder.Expression + partStr, timeSpan);
				}
			}

			public static string TimeSpanPartToStr(TimeSpanParts part)
			{
				return part switch
				{
					TimeSpanParts.TotalNanoseconds	=> " * 1000000000",
					TimeSpanParts.TotalMicroseconds => " * 1000000",
					TimeSpanParts.TotalMilliseconds => " * 1000",
					TimeSpanParts.TotalSeconds		=> "",
					TimeSpanParts.TotalMinutes		=> " / 60",
					TimeSpanParts.TotalHours		=> " / 3600",
					TimeSpanParts.TotalDays         => " / 86400",
					_ => throw new InvalidOperationException($"Unexpected timespanpart: {part}")
				};
			}
		}

		[Extension(               "",                        ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(TimeSpanPartBuilder))]
		[Extension(PN.Oracle, "(extract(second from {0}) + extract(minute from {0}) * 60 + extract(hour from {0}) * 3600 + extract(day from {0}) * 86400)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(TimeSpanPartBuilderIntervalType))]
		[Extension(PN.PostgreSQL, "(extract(second from {0}) + extract(minute from {0}) * 60 + extract(hour from {0}) * 3600 + extract(day from {0}) * 86400)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(TimeSpanPartBuilderIntervalType))]
		[Extension(PN.SQLite,     "",                        ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(TimeSpanPartBuilderSqlite))]
		public static long? TimeSpanPart([SqlQueryDependent] TimeSpanParts part, [ExprParameter] TimeSpan? timeSpan)
		{
			if (timeSpan == null)
				return null;

			return part switch
			{
				TimeSpanParts.TotalDays			=> (long)timeSpan.Value.TotalDays,
				TimeSpanParts.Days				=> (long)timeSpan.Value.Days,
				TimeSpanParts.TotalHours		=> (long)timeSpan.Value.TotalHours,
				TimeSpanParts.Hours				=> (long)timeSpan.Value.Hours,
				TimeSpanParts.TotalMinutes		=> (long)timeSpan.Value.TotalMinutes,
				TimeSpanParts.Minutes			=> (long)timeSpan.Value.Minutes,
				TimeSpanParts.TotalSeconds		=> (long)timeSpan.Value.TotalSeconds,
				TimeSpanParts.Seconds			=> (long)timeSpan.Value.Seconds,
				TimeSpanParts.TotalMilliseconds => (long)timeSpan.Value.TotalMilliseconds,
				TimeSpanParts.Milliseconds		=> (long)timeSpan.Value.Milliseconds,
#if NET7_0_OR_GREATER
				TimeSpanParts.TotalMicroseconds => (long)timeSpan.Value.TotalMicroseconds,
				TimeSpanParts.Microseconds		=> (long)timeSpan.Value.Microseconds,
				TimeSpanParts.TotalNanoseconds	=> (long)timeSpan.Value.TotalNanoseconds,
				TimeSpanParts.Nanoseconds		=> (long)timeSpan.Value.Nanoseconds,
#else
				TimeSpanParts.TotalMicroseconds => (long)timeSpan.Value.Ticks / 10,
				TimeSpanParts.TotalNanoseconds	=> (long)timeSpan.Value.Ticks * 100,
#endif
				_ => throw new InvalidOperationException(),
			};
			throw new NotImplementedException();
		}

		#endregion


		internal sealed class DateTimeAddIntervalBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var p = Expression.Call(
						null,
						MethodHelper.GetMethodInfo(TimeSpanPart, TimeSpanParts.TotalNanoseconds, (TimeSpan?)TimeSpan.Zero),
						Expression.Constant(TimeSpanParts.TotalNanoseconds),
						builder.Arguments[1]
					);

				var e = Expression.Call(
						null,
						MethodHelper.GetMethodInfo(DateAdd, DateParts.Nanosecond, (double?)0, (DateTime?)DateTime.MinValue),
						Expression.Constant(DateParts.Nanosecond),
					 	Expression.Convert(p, typeof(double?)),
						builder.Arguments[0]
					);

				var exp = builder.ConvertExpressionToSql(e, true);
				builder.ResultExpression = exp;
			}
		}

		internal sealed class DateTimeAddIntervalBuilderOracle : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var date   = builder.GetExpression("date");
				var timeSpan = builder.GetExpression("timeSpan", true);

				var tp = timeSpan.GetExpressionType();
				var dt = tp.DataType;

				if (dt == DataType.Int64)
				{
					var p = Expression.Call(
						null,
						MethodHelper.GetMethodInfo(TimeSpanPart, TimeSpanParts.TotalMilliseconds, (TimeSpan?)TimeSpan.Zero),
						Expression.Constant(TimeSpanParts.TotalMilliseconds),
						builder.Arguments[1]
					);

					var e = Expression.Call(
						null,
						MethodHelper.GetMethodInfo(DateAdd, DateParts.Millisecond, (double?)0, (DateTime?)DateTime.MinValue),
						Expression.Constant(DateParts.Millisecond),
					 	Expression.Convert(p, typeof(double?)),
						builder.Arguments[0]
					);

					var exp = builder.ConvertExpressionToSql(e, true);
					builder.ResultExpression = exp;
				}
				else
				{
					builder.ResultExpression = builder.Add(date, timeSpan, typeof(DateTime));
				}
			}
		}

		internal sealed class DateTimeAddIntervalBuilderPostgreSQL : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var date   = builder.GetExpression("date");
				var timeSpan = builder.GetExpression("timeSpan", true);

				var tp = timeSpan.GetExpressionType();
				var dt = tp.DataType;

				if (dt == DataType.Int64)
				{
					var p = Expression.Call(
						null,
						MethodHelper.GetMethodInfo(TimeSpanPart, TimeSpanParts.TotalMicroseconds, (TimeSpan?)TimeSpan.Zero),
						Expression.Constant(TimeSpanParts.TotalMicroseconds),
						builder.Arguments[1]
					);

					var e = Expression.Call(
						null,
						MethodHelper.GetMethodInfo(DateAdd, DateParts.Microsecond, (double?)0, (DateTime?)DateTime.MinValue),
						Expression.Constant(DateParts.Microsecond),
					 	Expression.Convert(p, typeof(double?)),
						builder.Arguments[0]
					);

					var exp = builder.ConvertExpressionToSql(e, true);
					builder.ResultExpression = exp;
				}
				else
				{
					builder.ResultExpression = builder.Add(date, timeSpan, typeof(DateTime));
				}
			}
		}

		internal sealed class DateTimeAddIntervalBuilderSQLite : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var date   = builder.GetExpression("date");
				var timeSpan = builder.GetExpression("timeSpan", true);
				var expStr = "strftime('%Y-%m-%d %H:%M:%f', {0}, ({1}/1000.0) || ' Second')";

				builder.ResultExpression = new SqlExpression(typeof(DateTime?), expStr, Precedence.Concatenate, date, timeSpan);
			}
		}

		[Extension("DateAdd", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateTimeAddIntervalBuilder))]
		[Extension(PN.Oracle, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateTimeAddIntervalBuilderOracle))]
		[Extension(PN.PostgreSQL, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateTimeAddIntervalBuilderPostgreSQL))]
		[Extension(PN.SQLite, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateTimeAddIntervalBuilderSQLite))]
		public static DateTime? DateAdd(DateTime? date, TimeSpan? timeSpan)
		{
			if (date == null || timeSpan == null)
				return null;

			return date + timeSpan;
		}

		[Extension("DateAdd", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateTimeAddIntervalBuilder))]
		[Extension(PN.Oracle, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateTimeAddIntervalBuilderOracle))]
		[Extension(PN.PostgreSQL, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateTimeAddIntervalBuilderPostgreSQL))]
		[Extension(PN.SQLite, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateTimeAddIntervalBuilderSQLite))]
		public static DateTimeOffset? DateAdd(DateTimeOffset? date, TimeSpan? timeSpan)
		{
			if (date == null || timeSpan == null)
				return null;

			return date + timeSpan;
		}

		internal sealed class NegateIntervalBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var timeSpan = builder.GetExpression("timeSpan", true);
				builder.ResultExpression = builder.Mul<TimeSpan>(timeSpan, new SqlValue(-1));
			}
		}

		[Extension("", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(NegateIntervalBuilder))]
		public static TimeSpan? NegateInterval(TimeSpan? timeSpan)
		{
			if (timeSpan == null)
				return null;

			return -timeSpan.Value;
		}
	}
}
