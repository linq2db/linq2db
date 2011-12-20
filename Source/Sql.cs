using System;
using System.Data.Linq;
using System.Globalization;
using System.Reflection;

using PN = LinqToDB.ProviderName;

namespace LinqToDB
{
	using Data.Linq;
	using Extensions;
	using SqlBuilder;

	public static class Sql
	{
		#region Common Functions

		[CLSCompliant(false)]
		[SqlExpression("{0}", 0, ServerSideOnly = true)]
		public static T AsSql<T>(T obj)
		{
			return obj;
		}

		[CLSCompliant(false)]
		[SqlExpression("{0}", 0)]
		public static T ConvertNullable<T>(T? value)
			where T : struct
		{
			return value.Value;
		}

		#endregion

		#region Guid Functions

		[SqlFunction  (PN.Oracle,   "Sys_Guid", ServerSideOnly=true)]
		[SqlFunction  (PN.Firebird, "Gen_Uuid", ServerSideOnly=true)]
		[SqlFunction  (PN.MySql,    "Uuid",     ServerSideOnly=true)]
		[SqlExpression(PN.Sybase,   "NewID(1)", ServerSideOnly=true)]
		[SqlFunction  (             "NewID",    ServerSideOnly=true)]
		public static Guid NewGuid()
		{
			return Guid.NewGuid();
		}

		#endregion

		#region Convert Functions

		[CLSCompliant(false)]
		[SqlFunction("Convert", 0, 1, ServerSideOnly = true)]
		public static TTo Convert<TTo,TFrom>(TTo to, TFrom from)
		{
			var dt = Common.ConvertTo<TTo>.From(from);
			return dt;
		}

		[CLSCompliant(false)]
		[SqlFunction("Convert", 0, 1, 2, ServerSideOnly = true)]
		public static TTo Convert<TTo, TFrom>(TTo to, TFrom from, int format)
		{
			var dt = Common.ConvertTo<TTo>.From(from);
			return dt;
		}

		[CLSCompliant(false)]
		[SqlFunction("Convert", 0, 1)]
		public static TTo Convert2<TTo,TFrom>(TTo to, TFrom from)
		{
			return Common.ConvertTo<TTo>.From(from);
		}

		[CLSCompliant(false)]
		[SqlFunction("$Convert$", 1, 2, 0)]
		public static TTo Convert<TTo,TFrom>(TFrom obj)
		{
			return Common.ConvertTo<TTo>.From(obj);
		}

		public static class ConvertTo<TTo>
		{
			[CLSCompliant(false)]
			[SqlFunction("$Convert$", 1, 2, 0)]
			public static TTo From<TFrom>(TFrom obj)
			{
				return Common.ConvertTo<TTo>.From(obj);
			}
		}

		[SqlExpression("{0}")]
		public static TimeSpan? DateToTime(DateTime? date)
		{
			return date == null ? null : (TimeSpan?)new TimeSpan(date.Value.Ticks);
		}

		[SqlProperty(PN.Informix,   "Boolean",        ServerSideOnly=true)]
		[SqlProperty(PN.PostgreSQL, "Boolean",        ServerSideOnly=true)]
		[SqlProperty(PN.MySql,      "Boolean",        ServerSideOnly=true)]
		[SqlProperty(PN.SQLite,     "Boolean",        ServerSideOnly=true)]
		[SqlProperty(               "Bit",            ServerSideOnly=true)] public static Boolean        Bit                               { get { return false; } }

		[SqlProperty(PN.Oracle,     "Number(19)",     ServerSideOnly=true)]
		[SqlProperty(               "BigInt",         ServerSideOnly=true)] public static Int64          BigInt                            { get { return 0; } }

		[SqlProperty(PN.MySql,      "Signed",         ServerSideOnly=true)]
		[SqlProperty(               "Int",            ServerSideOnly=true)] public static Int32          Int                               { get { return 0; } }

		[SqlProperty(PN.MySql,      "Signed",         ServerSideOnly=true)]
		[SqlProperty(               "SmallInt",       ServerSideOnly=true)] public static Int16          SmallInt                          { get { return 0; } }

		[SqlProperty(PN.DB2,        "SmallInt",       ServerSideOnly=true)]
		[SqlProperty(PN.Informix,   "SmallInt",       ServerSideOnly=true)]
		[SqlProperty(PN.Oracle,     "Number(3)",      ServerSideOnly=true)]
		[SqlProperty(PN.DB2,        "SmallInt",       ServerSideOnly=true)]
		[SqlProperty(PN.Firebird,   "SmallInt",       ServerSideOnly=true)]
		[SqlProperty(PN.PostgreSQL, "SmallInt",       ServerSideOnly=true)]
		[SqlProperty(PN.MySql,      "Unsigned",       ServerSideOnly=true)]
		[SqlProperty(               "TinyInt",        ServerSideOnly=true)] public static Byte           TinyInt                           { get { return 0; } }

		[SqlProperty(               "Decimal",        ServerSideOnly=true)] public static Decimal DefaultDecimal                           { get { return 0; } }
		[SqlFunction(                                 ServerSideOnly=true)] public static Decimal        Decimal(int precision)            {       return 0;   }
		[SqlFunction(                                 ServerSideOnly=true)] public static Decimal        Decimal(int precision, int scale) {       return 0;   }

		[SqlProperty(PN.Oracle,     "Number(19,4)",   ServerSideOnly=true)]
		[SqlProperty(PN.Firebird,   "Decimal(18,4)",  ServerSideOnly=true)]
		[SqlProperty(PN.PostgreSQL, "Decimal(19,4)",  ServerSideOnly=true)]
		[SqlProperty(PN.MySql,      "Decimal(19,4)",  ServerSideOnly=true)]
		[SqlProperty(               "Money",          ServerSideOnly=true)] public static Decimal        Money                             { get { return 0; } }

		[SqlProperty(PN.Informix,   "Decimal(10,4)",  ServerSideOnly=true)]
		[SqlProperty(PN.Oracle,     "Number(10,4)",   ServerSideOnly=true)]
		[SqlProperty(PN.Firebird,   "Decimal(10,4)",  ServerSideOnly=true)]
		[SqlProperty(PN.PostgreSQL, "Decimal(10,4)",  ServerSideOnly=true)]
		[SqlProperty(PN.MySql,      "Decimal(10,4)",  ServerSideOnly=true)]
		[SqlProperty(PN.SqlCe,      "Decimal(10,4)",  ServerSideOnly=true)]
		[SqlProperty(               "SmallMoney",     ServerSideOnly=true)] public static Decimal        SmallMoney                        { get { return 0; } }

		[SqlProperty(PN.MySql,      "Decimal(29,10)", ServerSideOnly=true)]
		[SqlProperty(               "Float",          ServerSideOnly=true)] public static Double         Float                             { get { return 0; } }

		[SqlProperty(PN.MySql,      "Decimal(29,10)", ServerSideOnly=true)]
		[SqlProperty(               "Real",           ServerSideOnly=true)] public static Single         Real                              { get { return 0; } }

		[SqlProperty(PN.PostgreSQL, "TimeStamp",      ServerSideOnly=true)]
		[SqlProperty(PN.Firebird,   "TimeStamp",      ServerSideOnly=true)]
		[SqlProperty(               "DateTime",       ServerSideOnly=true)] public static DateTime       DateTime                          { get { return DateTime.Now; } }

		[SqlProperty(PN.MsSql2005,  "DateTime",       ServerSideOnly=true)]
		[SqlProperty(PN.PostgreSQL, "TimeStamp",      ServerSideOnly=true)]
		[SqlProperty(PN.Firebird,   "TimeStamp",      ServerSideOnly=true)]
		[SqlProperty(PN.MySql,      "DateTime",       ServerSideOnly=true)]
		[SqlProperty(PN.SqlCe,      "DateTime",       ServerSideOnly=true)]
		[SqlProperty(PN.Sybase,     "DateTime",       ServerSideOnly=true)]
		[SqlProperty(               "DateTime2",      ServerSideOnly=true)] public static DateTime       DateTime2                         { get { return DateTime.Now; } }

		[SqlProperty(PN.PostgreSQL, "TimeStamp",      ServerSideOnly=true)]
		[SqlProperty(PN.Firebird,   "TimeStamp",      ServerSideOnly=true)]
		[SqlProperty(PN.MySql,      "DateTime",       ServerSideOnly=true)]
		[SqlProperty(PN.SqlCe,      "DateTime",       ServerSideOnly=true)]
		[SqlProperty(               "SmallDateTime",  ServerSideOnly=true)] public static DateTime       SmallDateTime                     { get { return DateTime.Now; } }

		[SqlProperty(PN.MsSql2005,  "Datetime",       ServerSideOnly=true)]
		[SqlProperty(PN.SqlCe,      "Datetime",       ServerSideOnly=true)]
		[SqlProperty(               "Date",           ServerSideOnly=true)] public static DateTime       Date                              { get { return DateTime.Now; } }

		[SqlProperty(               "Time",           ServerSideOnly=true)] public static DateTime       Time                              { get { return DateTime.Now; } }

		[SqlProperty(PN.PostgreSQL, "TimeStamp",      ServerSideOnly=true)]
		[SqlProperty(PN.Firebird,   "TimeStamp",      ServerSideOnly=true)]
		[SqlProperty(PN.MsSql2008,  "DateTimeOffset", ServerSideOnly=true)]
		[SqlProperty(               "DateTime",       ServerSideOnly=true)] public static DateTimeOffset DateTimeOffset                    { get { return DateTimeOffset.Now; } }

		[SqlFunction(PN.SqlCe,      "NChar",          ServerSideOnly=true)]
		[SqlFunction(                                 ServerSideOnly=true)] public static String         Char(int length)                  {       return ""; }

		[SqlProperty(PN.SqlCe,      "NChar",          ServerSideOnly=true)]
		[SqlProperty(               "Char",           ServerSideOnly=true)] public static String  DefaultChar                              { get { return ""; } }

		[SqlFunction(PN.MySql,      "Char",           ServerSideOnly=true)]
		[SqlFunction(PN.SqlCe,      "NVarChar",       ServerSideOnly=true)]
		[SqlFunction(                                 ServerSideOnly=true)] public static String         VarChar(int length)               {       return ""; }

		[SqlProperty(PN.MySql,      "Char",           ServerSideOnly=true)]
		[SqlProperty(PN.SqlCe,      "NVarChar",       ServerSideOnly=true)]
		[SqlProperty(               "VarChar",        ServerSideOnly=true)] public static String  DefaultVarChar                           { get { return ""; } }

		[SqlFunction(PN.DB2,        "Char",           ServerSideOnly=true)]
		[SqlFunction(                                 ServerSideOnly=true)] public static String         NChar(int length)                 {       return ""; }

		[SqlProperty(PN.DB2,        "Char",           ServerSideOnly=true)]
		[SqlProperty(               "NChar",          ServerSideOnly=true)] public static String  DefaultNChar                             { get { return ""; } }

		[SqlFunction(PN.DB2,        "Char",           ServerSideOnly=true)]
		[SqlFunction(PN.Oracle,     "VarChar2",       ServerSideOnly=true)]
		[SqlFunction(PN.Firebird,   "VarChar",        ServerSideOnly=true)]
		[SqlFunction(PN.PostgreSQL, "VarChar",        ServerSideOnly=true)]
		[SqlFunction(PN.MySql,      "Char",           ServerSideOnly=true)]
		[SqlFunction(                                 ServerSideOnly=true)] public static String         NVarChar(int length)              {       return ""; }

		[SqlProperty(PN.DB2,        "Char",           ServerSideOnly=true)]
		[SqlProperty(PN.Oracle,     "VarChar2",       ServerSideOnly=true)]
		[SqlProperty(PN.Firebird,   "VarChar",        ServerSideOnly=true)]
		[SqlProperty(PN.PostgreSQL, "VarChar",        ServerSideOnly=true)]
		[SqlProperty(PN.MySql,      "Char",           ServerSideOnly=true)]
		[SqlProperty(               "NVarChar",       ServerSideOnly=true)] public static String  DefaultNVarChar                          { get { return ""; } }

		#endregion

		#region String Functions

		[SqlFunction(                             PreferServerSide = true)]
		[SqlFunction(PN.Access,    "Len",         PreferServerSide = true)]
		[SqlFunction(PN.Firebird,  "Char_Length", PreferServerSide = true)]
		[SqlFunction(PN.MsSql2005, "Len",         PreferServerSide = true)]
		[SqlFunction(PN.MsSql2008, "Len",         PreferServerSide = true)]
		[SqlFunction(PN.SqlCe,     "Len",         PreferServerSide = true)]
		[SqlFunction(PN.Sybase,    "Len",         PreferServerSide = true)]
		public static int? Length(string str)
		{
			return str == null ? null : (int?)str.Length;
		}

		[SqlFunction]
		[SqlFunction  (PN.Access,   "Mid")]
		[SqlFunction  (PN.DB2,      "Substr")]
		[SqlFunction  (PN.Informix, "Substr")]
		[SqlFunction  (PN.Oracle,   "Substr")]
		[SqlFunction  (PN.SQLite,   "Substr")]
		[SqlExpression(PN.Firebird, "Substring({0} from {1} for {2})")]
		public static string Substring(string str, int? startIndex, int? length)
		{
			return str == null || startIndex == null || length == null ? null : str.Substring(startIndex.Value, length.Value);
		}

		[SqlFunction(ServerSideOnly = true)]
		public static bool Like(string matchExpression, string pattern)
		{
#if SILVERLIGHT
			throw new InvalidOperationException();
#else
			return matchExpression == null || pattern == null ? false : System.Data.Linq.SqlClient.SqlMethods.Like(matchExpression, pattern);
#endif
		}

		[SqlFunction(ServerSideOnly = true)]
		public static bool Like(string matchExpression, string pattern, char? escapeCharacter)
		{
#if SILVERLIGHT
			throw new InvalidOperationException();
#else
			return matchExpression == null || pattern == null || escapeCharacter == null ?
				false :
				System.Data.Linq.SqlClient.SqlMethods.Like(matchExpression, pattern, escapeCharacter.Value);
#endif
		}

		[SqlFunction]
		[SqlFunction(PN.DB2,   "Locate")]
		[SqlFunction(PN.MySql, "Locate")]
		public static int? CharIndex(string value, string str)
		{
			if (str == null || value == null)
				return null;

			return str.IndexOf(value) + 1;
		}

		[SqlFunction]
		[SqlFunction(ProviderName.DB2,   "Locate")]
		[SqlFunction(ProviderName.MySql, "Locate")]
		public static int? CharIndex(string value, string str, int? startLocation)
		{
			if (str == null || value == null || startLocation == null)
				return null;

			return str.IndexOf(value, startLocation.Value - 1) + 1;
		}

		[SqlFunction]
		[SqlFunction(PN.DB2,   "Locate")]
		[SqlFunction(PN.MySql, "Locate")]
		public static int? CharIndex(char? value, string str)
		{
			if (value == null || str == null)
				return null;

			return str.IndexOf(value.Value) + 1;
		}

		[SqlFunction]
		[SqlFunction(ProviderName.DB2,   "Locate")]
		[SqlFunction(ProviderName.MySql, "Locate")]
		public static int? CharIndex(char? value, string str, int? startLocation)
		{
			if (str == null || value == null || startLocation == null)
				return null;

			return str.IndexOf(value.Value, startLocation.Value - 1) + 1;
		}

		[SqlFunction]
		public static string Reverse(string str)
		{
			if (string.IsNullOrEmpty(str))
				return str;

			var chars = str.ToCharArray();
			Array.Reverse(chars);
			return new string(chars);
		}

		[SqlFunction]
		[SqlFunction(PN.SQLite, "LeftStr")]
		public static string Left(string str, int? length)
		{
			return length == null || str == null || str.Length < length? null: str.Substring(1, length.Value);
		}

		[SqlFunction]
		[SqlFunction(PN.SQLite, "RightStr")]
		public static string Right(string str, int? length)
		{
			return length == null || str == null || str.Length < length?
				null :
				str.Substring(str.Length - length.Value);
		}

		[SqlFunction]
		public static string Stuff(string str, int? startLocation, int? length, string value)
		{
			return str == null || value == null || startLocation == null || length == null ?
				null :
				str.Remove(startLocation.Value - 1, length.Value).Insert(startLocation.Value - 1, value);
		}

		[SqlFunction]
		public static string Space(int? length)
		{
			return length == null ? null : "".PadRight(length.Value);
		}

		[SqlFunction(Name = "LPad")]
		public static string PadLeft(string str, int? totalWidth, char? paddingChar)
		{
			return str == null || totalWidth == null || paddingChar == null ?
				null :
				str.PadLeft(totalWidth.Value, paddingChar.Value);
		}

		[SqlFunction(Name = "RPad")]
		public static string PadRight(string str, int? totalWidth, char? paddingChar)
		{
			return str == null || totalWidth == null || paddingChar == null ?
				null :
				str.PadRight(totalWidth.Value, paddingChar.Value);
		}

		[SqlFunction]
		[SqlFunction(PN.Sybase, "Str_Replace")]
		public static string Replace(string str, string oldValue, string newValue)
		{
			return str == null || oldValue == null || newValue == null ?
				null :
				str.Replace(oldValue, newValue);
		}

		[SqlFunction]
		[SqlFunction(PN.Sybase, "Str_Replace")]
		public static string Replace(string str, char? oldValue, char? newValue)
		{
			return str == null || oldValue == null || newValue == null ?
				null :
				str.Replace(oldValue.Value, newValue.Value);
		}

		[SqlFunction]
		public static string Trim(string str)
		{
			return str == null ? null : str.Trim();
		}

		[SqlFunction("LTrim")]
		public static string TrimLeft(string str)
		{
			return str == null ? null : str.TrimStart();
		}

		[SqlFunction("RTrim")]
		public static string TrimRight(string str)
		{
			return str == null ? null : str.TrimEnd();
		}

		[SqlExpression(PN.DB2, "Strip({0}, B, {1})")]
		[SqlFunction]
		public static string Trim(string str, char? ch)
		{
			return str == null || ch == null ? null : str.Trim(ch.Value);
		}

		[SqlExpression(PN.DB2, "Strip({0}, L, {1})")]
		[SqlFunction  (                  "LTrim")]
		public static string TrimLeft(string str, char? ch)
		{
			return str == null || ch == null ? null : str.TrimStart(ch.Value);
		}

		[SqlExpression(PN.DB2, "Strip({0}, T, {1})")]
		[SqlFunction  (                  "RTrim")]
		public static string TrimRight(string str, char? ch)
		{
			return str == null || ch == null ? null : str.TrimEnd(ch.Value);
		}

		[SqlFunction]
		[SqlFunction(PN.Access, "LCase")]
		public static string Lower(string str)
		{
			return str == null ? null : str.ToLower();
		}

		[SqlFunction]
		[SqlFunction(PN.Access, "UCase")]
		public static string Upper(string str)
		{
			return str == null ? null : str.ToUpper();
		}

		#endregion

		#region Binary Functions

		[SqlFunction(                              PreferServerSide = true)]
		[SqlFunction(PN.Access,    "Len",          PreferServerSide = true)]
		[SqlFunction(PN.Firebird,  "Octet_Length", PreferServerSide = true)]
		[SqlFunction(PN.MsSql2005, "DataLength",   PreferServerSide = true)]
		[SqlFunction(PN.MsSql2008, "DataLength",   PreferServerSide = true)]
		[SqlFunction(PN.SqlCe,     "DataLength",   PreferServerSide = true)]
		[SqlFunction(PN.Sybase,    "DataLength",   PreferServerSide = true)]
		public static int? Length(Binary value)
		{
			return value == null ? null : (int?)value.Length;
		}

		#endregion

		#region DateTime Functions

		[SqlProperty(             "CURRENT_TIMESTAMP")]
		[SqlProperty(PN.Informix, "CURRENT")]
		[SqlProperty(PN.Access,   "Now")]
		public static DateTime GetDate()
		{
			return DateTime.Now;
		}

		[SqlProperty(             "CURRENT_TIMESTAMP", ServerSideOnly = true)]
		[SqlProperty(PN.Informix, "CURRENT",           ServerSideOnly = true)]
		[SqlProperty(PN.Access,   "Now",               ServerSideOnly = true)]
		[SqlFunction(PN.SqlCe,    "GetDate",           ServerSideOnly = true)]
		[SqlFunction(PN.Sybase,   "GetDate",           ServerSideOnly = true)]
		public static DateTime CurrentTimestamp
		{
			get { throw new LinqException("The 'CurrentTimestamp' is server side only property."); }
		}

		[SqlProperty(             "CURRENT_TIMESTAMP")]
		[SqlProperty(PN.Informix, "CURRENT")]
		[SqlProperty(PN.Access,   "Now")]
		[SqlFunction(PN.SqlCe,    "GetDate")]
		[SqlFunction(PN.Sybase,   "GetDate")]
		public static DateTime CurrentTimestamp2
		{
			get { return DateTime.Now; }
		}

		[SqlFunction]
		public static DateTime? ToDate(int? year, int? month, int? day, int? hour, int? minute, int? second, int? millisecond)
		{
			return year == null || month == null || day == null || hour == null || minute == null || second == null || millisecond == null ?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value, hour.Value, minute.Value, second.Value, millisecond.Value);
		}

		[SqlFunction]
		public static DateTime? ToDate(int? year, int? month, int? day, int? hour, int? minute, int? second)
		{
			return year == null || month == null || day == null || hour == null || minute == null || second == null ?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value, hour.Value, minute.Value, second.Value);
		}

		[SqlFunction]
		public static DateTime? ToDate(int? year, int? month, int? day)
		{
			return year == null || month == null || day == null ?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value);
		}

		[SqlEnum]
		public enum DateParts
		{
			Year        =  0,
			Quarter     =  1,
			Month       =  2,
			DayOfYear   =  3,
			Day         =  4,
			Week        =  5,
			WeekDay     =  6,
			Hour        =  7,
			Minute      =  8,
			Second      =  9,
			Millisecond = 10,
		}

		class DatePartAttribute : SqlExpressionAttribute
		{
			public DatePartAttribute(string sqlProvider, string expression, int datePartIndex, params int[] argIndices)
				: this(sqlProvider, expression, LinqToDB.SqlBuilder.Precedence.Primary, false, null, datePartIndex, argIndices)
			{
			}

			public DatePartAttribute(string sqlProvider, string expression, bool isExpression, int datePartIndex, params int[] argIndices)
				: this(sqlProvider, expression, LinqToDB.SqlBuilder.Precedence.Primary, isExpression, null, datePartIndex, argIndices)
			{
			}

			public DatePartAttribute(string sqlProvider, string expression, bool isExpression, string[] partMapping, int datePartIndex, params int[] argIndices)
				: this(sqlProvider, expression, LinqToDB.SqlBuilder.Precedence.Primary, isExpression, partMapping, datePartIndex, argIndices)
			{
			}

			public DatePartAttribute(string sqlProvider, string expression, int precedence, bool isExpression, string[] partMapping, int datePartIndex, params int[] argIndices)
				: base(sqlProvider, expression, argIndices)
			{
				_isExpression  = isExpression;
				_partMapping   = partMapping;
				_datePartIndex = datePartIndex;
				Precedence     = precedence;
			}

			readonly bool     _isExpression;
			readonly string[] _partMapping;
			readonly int      _datePartIndex;

			public override ISqlExpression GetExpression(MemberInfo member, params ISqlExpression[] args)
			{
				var part = (DateParts)((SqlValue)args[_datePartIndex]).Value;
				var pstr = _partMapping != null ? _partMapping[(int)part] : part.ToString();
				var str  = string.Format(Expression, pstr ?? part.ToString());
				var type = member.GetMemberType();


				return _isExpression ?
					                new SqlExpression(type, str, Precedence, ConvertArgs(member, args)) :
					(ISqlExpression)new SqlFunction  (type, str, ConvertArgs(member, args));
			}
		}

		[CLSCompliant(false)]
		[SqlFunction] // FIXME: LinqToDB.Sql.DatePartAttribute -> DatePart
		[LinqToDB.Sql.DatePartAttribute(PN.Oracle, "Add{0}", false, 0, 2, 1)]
		[LinqToDB.Sql.DatePartAttribute(PN.DB2, "{{1}} + {0}", Precedence.Additive, true, new[] { "{0} Year", "({0} * 3) Month", "{0} Month", "{0} Day", "{0} Day", "({0} * 7) Day", "{0} Day", "{0} Hour", "{0} Minute", "{0} Second", "({0} * 1000) Microsecond" }, 0, 1, 2)]
		[LinqToDB.Sql.DatePartAttribute(PN.Informix, "{{1}} + Interval({0}", Precedence.Additive, true, new[] { "{0}) Year to Year", "{0}) Month to Month * 3", "{0}) Month to Month", "{0}) Day to Day", "{0}) Day to Day", "{0}) Day to Day * 7", "{0}) Day to Day", "{0}) Hour to Hour", "{0}) Minute to Minute", "{0}) Second to Second", null }, 0, 1, 2)]
		[LinqToDB.Sql.DatePartAttribute(PN.PostgreSQL, "{{1}} + Interval '{{0}} {0}", Precedence.Additive, true, new[] { "Year'", "Month' * 3", "Month'", "Day'", "Day'", "Day' * 7", "Day'", "Hour'", "Minute'", "Second'", "Millisecond'" }, 0, 1, 2)]
		[LinqToDB.Sql.DatePartAttribute(PN.MySql, "Date_Add({{1}}, Interval {{0}} {0})", true, new[] { null, null, null, "Day", null, null, "Day", null, null, null, null }, 0, 1, 2)]
		[LinqToDB.Sql.DatePartAttribute(PN.SQLite, "DateTime({{1}}, '{{0}} {0}')", true, new[] { null, null, null, "Day", null, null, "Day", null, null, null, null }, 0, 1, 2)]
		[LinqToDB.Sql.DatePartAttribute(PN.Access, "DateAdd({0}, {{0}}, {{1}})", true, new[] { "'yyyy'", "'q'", "'m'", "'y'", "'d'", "'ww'", "'w'", "'h'", "'n'", "'s'", null }, 0, 1, 2)]
		public static DateTime? DateAdd(DateParts part, double? number, DateTime? date)
		{
			if (number == null || date == null)
				return null;

			switch (part)
			{
				case DateParts.Year        : return date.Value.AddYears       ((int)number);
				case DateParts.Quarter     : return date.Value.AddMonths      ((int)number * 3);
				case DateParts.Month       : return date.Value.AddMonths      ((int)number);
				case DateParts.DayOfYear   : return date.Value.AddDays        (number.Value);
				case DateParts.Day         : return date.Value.AddDays        (number.Value);
				case DateParts.Week        : return date.Value.AddDays        (number.Value * 7);
				case DateParts.WeekDay     : return date.Value.AddDays        (number.Value);
				case DateParts.Hour        : return date.Value.AddHours       (number.Value);
				case DateParts.Minute      : return date.Value.AddMinutes     (number.Value);
				case DateParts.Second      : return date.Value.AddSeconds     (number.Value);
				case DateParts.Millisecond : return date.Value.AddMilliseconds(number.Value);
			}

			throw new InvalidOperationException();
		}

		[CLSCompliant(false)]
		[SqlFunction]
		[LinqToDB.Sql.DatePartAttribute(PN.DB2, "{0}", false, new[] { null, null, null, null, null, null, "DayOfWeek", null, null, null, null }, 0, 1)]
		[LinqToDB.Sql.DatePartAttribute(PN.Informix, "{0}", 0, 1)]
		[LinqToDB.Sql.DatePartAttribute(PN.MySql, "Extract({0} from {{0}})", true, 0, 1)]
		[LinqToDB.Sql.DatePartAttribute(PN.PostgreSQL, "Extract({0} from {{0}})", true, new[] { null, null, null, "DOY", null, null, "DOW", null, null, null, null }, 0, 1)]
		[LinqToDB.Sql.DatePartAttribute(PN.Firebird, "Extract({0} from {{0}})", true, new[] { null, null, null, "YearDay", null, null, null, null, null, null, null }, 0, 1)]
		[LinqToDB.Sql.DatePartAttribute(PN.Oracle, "To_Number(To_Char({{0}}, {0}))", true, new[] { "'YYYY'", "'Q'", "'MM'", "'DDD'", "'DD'", "'WW'", "'D'", "'HH'", "'MI'", "'SS'", "'FF'" }, 0, 1)]
		[LinqToDB.Sql.DatePartAttribute(PN.SQLite, "Cast(StrFTime({0}, {{0}}) as int)", true, new[] { "'%Y'", null, "'%m'", "'%j'", "'%d'", "'%W'", "'%w'", "'%H'", "'%M'", "'%S'", "'%f'" }, 0, 1)]
		[LinqToDB.Sql.DatePartAttribute(PN.Access, "DatePart({0}, {{0}})", true, new[] { "'yyyy'", "'q'", "'m'", "'y'", "'d'", "'ww'", "'w'", "'h'", "'n'", "'s'", null }, 0, 1)]
		public static int? DatePart(DateParts part, DateTime? date)
		{
			if (date == null)
				return null;

			switch (part)
			{
				case DateParts.Year        : return date.Value.Year;
				case DateParts.Quarter     : return (date.Value.Month - 1) / 3 + 1;
				case DateParts.Month       : return date.Value.Month;
				case DateParts.DayOfYear   : return date.Value.DayOfYear;
				case DateParts.Day         : return date.Value.Day;
				case DateParts.Week        : return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
				case DateParts.WeekDay     : return ((int)date.Value.DayOfWeek + 1 + DateFirst + 6) % 7 + 1;
				case DateParts.Hour        : return date.Value.Hour;
				case DateParts.Minute      : return date.Value.Minute;
				case DateParts.Second      : return date.Value.Second;
				case DateParts.Millisecond : return date.Value.Millisecond;
			}

			throw new InvalidOperationException();
		}

		[CLSCompliant(false)]
		[SqlFunction]
		[SqlFunction(PN.MySql, "TIMESTAMPDIFF")]
		public static int? DateDiff(DateParts part, DateTime? startDate, DateTime? endDate)
		{
			if (startDate == null || endDate == null)
				return null;

			switch (part)
			{
				case DateParts.Day         : return (int)(endDate - startDate).Value.TotalDays;
				case DateParts.Hour        : return (int)(endDate - startDate).Value.TotalHours;
				case DateParts.Minute      : return (int)(endDate - startDate).Value.TotalMinutes;
				case DateParts.Second      : return (int)(endDate - startDate).Value.TotalSeconds;
				case DateParts.Millisecond : return (int)(endDate - startDate).Value.TotalMilliseconds;
			}

			throw new InvalidOperationException();
		}

		[SqlProperty("@@DATEFIRST")]
		public static int DateFirst
		{
			get { return 7; }
		}

		[SqlFunction]
		public static DateTime? MakeDateTime(int? year, int? month, int? day)
		{
			return year == null || month == null || day == null ?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value);
		}

		[SqlFunction]
		public static DateTime? MakeDateTime(int? year, int? month, int? day, int? hour, int? minute, int? second)
		{
			return year == null || month == null || day == null || hour == null || minute == null || second == null ?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value, hour.Value, minute.Value, second.Value);
		}

		#endregion

		#region Math Functions

		[SqlFunction] public static Decimal? Abs    (Decimal? value) { return value == null ? null : (Decimal?)Math.Abs    (value.Value); }
		[SqlFunction] public static Double?  Abs    (Double?  value) { return value == null ? null : (Double?) Math.Abs    (value.Value); }
		[SqlFunction] public static Int16?   Abs    (Int16?   value) { return value == null ? null : (Int16?)  Math.Abs    (value.Value); }
		[SqlFunction] public static Int32?   Abs    (Int32?   value) { return value == null ? null : (Int32?)  Math.Abs    (value.Value); }
		[SqlFunction] public static Int64?   Abs    (Int64?   value) { return value == null ? null : (Int64?)  Math.Abs    (value.Value); }
		[CLSCompliant(false)]
		[SqlFunction] public static SByte?   Abs    (SByte?   value) { return value == null ? null : (SByte?)  Math.Abs    (value.Value); }
		[SqlFunction] public static Single?  Abs    (Single?  value) { return value == null ? null : (Single?) Math.Abs    (value.Value); }

		[SqlFunction] public static Double?  Acos   (Double?  value) { return value == null ? null : (Double?) Math.Acos   (value.Value); }
		[SqlFunction] public static Double?  Asin   (Double?  value) { return value == null ? null : (Double?) Math.Asin   (value.Value); }

		[SqlFunction(PN.Access, "Atn")]
		[SqlFunction] public static Double?  Atan   (Double?  value) { return value == null ? null : (Double?) Math.Atan   (value.Value); }

		[CLSCompliant(false)]
		[SqlFunction(PN.MsSql2008, "Atn2")]
		[SqlFunction(PN.MsSql2005, "Atn2")]
		[SqlFunction(PN.DB2,       "Atan2", 1, 0)]
		[SqlFunction(PN.SqlCe,     "Atn2")]
		[SqlFunction(PN.Sybase,    "Atn2")]
		[SqlFunction] public static Double?  Atan2  (Double? x, Double? y) { return x == null || y == null? null : (Double?)Math.Atan2(x.Value, y.Value); }

		[SqlFunction(PN.Informix, "Ceil")]
		[SqlFunction(PN.Oracle,   "Ceil")]
		[SqlFunction] public static Decimal? Ceiling(Decimal? value) { return value == null ? null : (Decimal?)decimal.Ceiling(value.Value); }

		[SqlFunction(PN.Informix, "Ceil")]
		[SqlFunction(PN.Oracle,   "Ceil")]
		[SqlFunction] public static Double?  Ceiling(Double?  value) { return value == null ? null : (Double?)Math.Ceiling(value.Value); }

		[SqlFunction] public static Double?  Cos    (Double?  value) { return value == null ? null : (Double?)Math.Cos    (value.Value); }

		[SqlFunction] public static Double?  Cosh   (Double?  value) { return value == null ? null : (Double?)Math.Cosh   (value.Value); }

		[SqlFunction] public static Double?  Cot    (Double?  value) { return value == null ? null : (Double?)Math.Cos(value.Value) / Math.Sin(value.Value); }

		[SqlFunction] public static Decimal? Degrees(Decimal? value) { return value == null ? null : (Decimal?)(value.Value * 180m / (Decimal)Math.PI); }
		[SqlFunction] public static Double?  Degrees(Double?  value) { return value == null ? null : (Double?) (value * 180 / Math.PI); }
		[SqlFunction] public static Int16?   Degrees(Int16?   value) { return value == null ? null : (Int16?)  (value * 180 / Math.PI); }
		[SqlFunction] public static Int32?   Degrees(Int32?   value) { return value == null ? null : (Int32?)  (value * 180 / Math.PI); }
		[SqlFunction] public static Int64?   Degrees(Int64?   value) { return value == null ? null : (Int64?)  (value * 180 / Math.PI); }
		[CLSCompliant(false)]
		[SqlFunction] public static SByte?   Degrees(SByte?   value) { return value == null ? null : (SByte?)  (value * 180 / Math.PI); }
		[SqlFunction] public static Single?  Degrees(Single?  value) { return value == null ? null : (Single?) (value * 180 / Math.PI); }

		[SqlFunction] public static Double?  Exp    (Double?  value) { return value == null ? null : (Double?)Math.Exp    (value.Value); }

		[SqlFunction(PN.Access, "Int")]
		[SqlFunction] public static Decimal? Floor  (Decimal? value) { return value == null ? null : (Decimal?)decimal.Floor(value.Value); }
		[SqlFunction(PN.Access, "Int")]
		[SqlFunction] public static Double?  Floor  (Double?  value) { return value == null ? null : (Double?) Math.   Floor(value.Value); }

		[SqlFunction(PN.Informix,   "LogN")]
		[SqlFunction(PN.Oracle,     "Ln")]
		[SqlFunction(PN.Firebird,   "Ln")]
		[SqlFunction(PN.PostgreSQL, "Ln")]
		[SqlFunction] public static Decimal? Log    (Decimal? value) { return value == null ? null : (Decimal?)Math.Log     ((Double)value.Value); }
		[SqlFunction(PN.Informix,   "LogN")]
		[SqlFunction(PN.Oracle,     "Ln")]
		[SqlFunction(PN.Firebird,   "Ln")]
		[SqlFunction(PN.PostgreSQL, "Ln")]
		[SqlFunction] public static Double?  Log    (Double?  value) { return value == null ? null : (Double?) Math.Log     (value.Value); }

		[SqlFunction(PN.PostgreSQL, "Log")]
		[SqlFunction] public static Double?  Log10  (Double?  value) { return value == null ? null : (Double?) Math.Log10   (value.Value); }

		[SqlFunction]
		public static double?  Log(double? newBase, double? value)
		{
			return value == null || newBase == null ? null : (Double?)Math.Log(value.Value, newBase.Value);
		}

		[SqlFunction]
		public static decimal? Log(decimal? newBase, decimal? value)
		{
			return value == null || newBase == null ? null : (decimal?)Math.Log((double)value.Value, (double)newBase.Value);
		}

		[SqlExpression(PN.Access, "{0} ^ {1}", Precedence = Precedence.Multiplicative)]
		[SqlFunction]
		public static Double?  Power(Double? x, Double? y)
		{
			return x == null || y == null ? null : (Double?)Math.Pow(x.Value, y.Value);
		}

		[SqlFunction]
		public static Decimal? RoundToEven(Decimal? value)
		{
#if SILVERLIGHT
			return value == null ? null : (Decimal?)Math.Round(value.Value);
#else
			return value == null ? null : (Decimal?)Math.Round(value.Value, MidpointRounding.ToEven);
#endif
		}

		[SqlFunction]
		public static Double? RoundToEven(Double? value)
		{
#if SILVERLIGHT
			return value == null ? null : (Double?) Math.Round(value.Value);
#else
			return value == null ? null : (Double?) Math.Round(value.Value, MidpointRounding.ToEven);
#endif
		}

		[SqlFunction] public static Decimal? Round(Decimal? value) { return Round(value, 0); }
		[SqlFunction] public static Double?  Round(Double?  value) { return Round(value, 0); }

		[SqlFunction]
		public static Decimal? Round(Decimal? value, int? precision)
		{
#if SILVERLIGHT
			throw new NotImplementedException();
#else
			return value == null || precision == null? null : (Decimal?)Math.Round(value.Value, precision.Value, MidpointRounding.AwayFromZero);
#endif
		}

		[SqlFunction]
		public static Double? Round(Double? value, int? precision)
		{
#if SILVERLIGHT
			throw new NotImplementedException();
#else
			return value == null || precision == null? null : (Double?) Math.Round(value.Value, precision.Value, MidpointRounding.AwayFromZero);
#endif
		}

		[SqlFunction]
		public static Decimal? RoundToEven(Decimal? value, int? precision)
		{
#if SILVERLIGHT
			return value == null || precision == null? null : (Decimal?)Math.Round(value.Value, precision.Value);
#else
			return value == null || precision == null? null : (Decimal?)Math.Round(value.Value, precision.Value, MidpointRounding.ToEven);
#endif
		}

		[SqlFunction]
		public static Double? RoundToEven(Double?  value, int? precision)
		{
#if SILVERLIGHT
			return value == null || precision == null? null : (Double?) Math.Round(value.Value, precision.Value);
#else
			return value == null || precision == null? null : (Double?) Math.Round(value.Value, precision.Value, MidpointRounding.ToEven);
#endif
		}

		[SqlFunction(PN.Access, "Sgn"), SqlFunction] public static int? Sign(Decimal? value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[SqlFunction(PN.Access, "Sgn"), SqlFunction] public static int? Sign(Double?  value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[SqlFunction(PN.Access, "Sgn"), SqlFunction] public static int? Sign(Int16?   value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[SqlFunction(PN.Access, "Sgn"), SqlFunction] public static int? Sign(Int32?   value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[SqlFunction(PN.Access, "Sgn"), SqlFunction] public static int? Sign(Int64?   value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[CLSCompliant(false)]
		[SqlFunction(PN.Access, "Sgn"), SqlFunction] public static int? Sign(SByte?   value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[SqlFunction(PN.Access, "Sgn"), SqlFunction] public static int? Sign(Single?  value) { return value == null ? null : (int?)Math.Sign(value.Value); }

		[SqlFunction] public static Double?  Sin     (Double?  value) { return value == null ? null : (Double?)Math.Sin (value.Value); }
		[SqlFunction] public static Double?  Sinh    (Double?  value) { return value == null ? null : (Double?)Math.Sinh(value.Value); }
		[SqlFunction(PN.Access, "Sqr")]
		[SqlFunction] public static Double?  Sqrt    (Double?  value) { return value == null ? null : (Double?)Math.Sqrt(value.Value); }
		[SqlFunction] public static Double?  Tan     (Double?  value) { return value == null ? null : (Double?)Math.Tan (value.Value); }
		[SqlFunction] public static Double?  Tanh    (Double?  value) { return value == null ? null : (Double?)Math.Tanh(value.Value); }

		[SqlExpression(PN.MsSql2008,  "Round({0}, 0, 1)")]
		[SqlExpression(PN.MsSql2005,  "Round({0}, 0, 1)")]
		[SqlExpression(PN.DB2,        "Truncate({0}, 0)")]
		[SqlExpression(PN.Informix,   "Trunc({0}, 0)")]
		[SqlExpression(PN.Oracle,     "Trunc({0}, 0)")]
		[SqlExpression(PN.Firebird,   "Trunc({0}, 0)")]
		[SqlExpression(PN.PostgreSQL, "Trunc({0}, 0)")]
		[SqlExpression(PN.MySql,      "Truncate({0}, 0)")]
		[SqlExpression(PN.SqlCe,      "Round({0}, 0, 1)")]
		[SqlFunction]
		public static Decimal? Truncate(Decimal? value)
		{
#if SILVERLIGHT
			throw new NotImplementedException();
#else
			return value == null ? null : (Decimal?)decimal.Truncate(value.Value);
#endif
		}

		[SqlExpression(PN.MsSql2008,  "Round({0}, 0, 1)")]
		[SqlExpression(PN.MsSql2005,  "Round({0}, 0, 1)")]
		[SqlExpression(PN.DB2,        "Truncate({0}, 0)")]
		[SqlExpression(PN.Informix,   "Trunc({0}, 0)")]
		[SqlExpression(PN.Oracle,     "Trunc({0}, 0)")]
		[SqlExpression(PN.Firebird,   "Trunc({0}, 0)")]
		[SqlExpression(PN.PostgreSQL, "Trunc({0}, 0)")]
		[SqlExpression(PN.MySql,      "Truncate({0}, 0)")]
		[SqlExpression(PN.SqlCe,      "Round({0}, 0, 1)")]
		[SqlFunction]
		public static Double? Truncate(Double? value)
		{
#if SILVERLIGHT
			throw new NotImplementedException();
#else
			return value == null ? null : (Double?) Math.Truncate(value.Value);
#endif
		}

		#endregion
	}
}
