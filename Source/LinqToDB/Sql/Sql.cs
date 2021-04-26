using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;
using PN = LinqToDB.ProviderName;

// ReSharper disable CheckNamespace
// ReSharper disable RedundantNameQualifier

namespace LinqToDB
{
	using Mapping;
	using Expressions;
	using Linq;
	using SqlQuery;
	using LinqToDB.Common;

	[PublicAPI]
	public static partial class Sql
	{
		#region Common Functions

		/// <summary>
		/// Generates '*'.
		/// </summary>
		/// <returns></returns>
		[Expression("*", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Primary)]
		public static object?[] AllColumns()
		{
			throw new LinqException("'AllColumns' is only server-side method.");
		}

		/// <summary>
		/// Generates 'DEFAULT' keyword, usable in inserts.
		/// </summary>
		[Expression("DEFAULT", ServerSideOnly = true)]
		public static T Default<T>() => throw new LinqException($"Default is only server-side method.");

		/// <summary>
		/// Enforces generating SQL even if an expression can be calculated locally.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj">Expression to generate SQL.</param>
		/// <returns>Returns 'obj'.</returns>
		[CLSCompliant(false)]
		[Expression("{0}", 0, ServerSideOnly = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T AsSql<T>(T obj)
		{
			return obj;
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, ServerSideOnly = true, InlineParameters = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T ToSql<T>(T obj)
		{
			return obj;
		}

		[Extension("{array, ', '}", ServerSideOnly = true)]
		internal static T[] Spread<T>([ExprParameter] T[] array)
		{
			throw new InvalidOperationException();
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, CanBeNull = true)]
		public static T AsNullable<T>(T value)
		{
			return value;
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, CanBeNull = false)]
		public static T AsNotNull<T>(T value)
		{
			return value;
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, CanBeNull = false)]
		public static T AsNotNullable<T>(T value)
		{
			return value;
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T? ToNullable<T>(T value)
			where T : struct
		{
			return value;
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T ToNotNull<T>(T? value)
			where T : struct
		{
			return value ?? default;
		}

		[CLSCompliant(false)]
		[Expression("{0}", 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T ToNotNullable<T>(T? value)
			where T : struct
		{
			return value ?? default;
		}

		[Expression("{0} BETWEEN {1} AND {2}", PreferServerSide = true, IsPredicate = true)]
		public static bool Between<T>(this T value, T low, T high)
			where T : IComparable
		{
			return value != null && value.CompareTo(low) >= 0 && value.CompareTo(high) <= 0;
		}

		[Expression("{0} BETWEEN {1} AND {2}", PreferServerSide = true, IsPredicate = true)]
		public static bool Between<T>(this T? value, T? low, T? high)
			where T : struct, IComparable
		{
			return value != null && value.Value.CompareTo(low) >= 0 && value.Value.CompareTo(high) <= 0;
		}

		[Expression("{0} NOT BETWEEN {1} AND {2}", PreferServerSide = true, IsPredicate = true)]
		public static bool NotBetween<T>(this T value, T low, T high)
			where T : IComparable
		{
			return value != null && (value.CompareTo(low) < 0 || value.CompareTo(high) > 0);
		}

		[Expression("{0} NOT BETWEEN {1} AND {2}", PreferServerSide = true, IsPredicate = true)]
		public static bool NotBetween<T>(this T? value, T? low, T? high)
			where T : struct, IComparable
		{
			return value != null && (value.Value.CompareTo(low) < 0 || value.Value.CompareTo(high) > 0);
		}

		/// <summary>
		/// Allows access to entity property via name. Property can be dynamic or non-dynamic.
		/// </summary>
		/// <typeparam name="T">Property type.</typeparam>
		/// <param name="entity">The entity.</param>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns></returns>
		/// <exception cref="LinqException">'Property' is only server-side method.</exception>
		public static T Property<T>(object? entity, [SqlQueryDependent] string propertyName)
		{
			throw new LinqException("'Property' is only server-side method.");
		}

		/// <summary>
		/// Used internally for keeping Alias information with expression.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="alias"></param>
		/// <returns></returns>
		internal static T Alias<T>(T obj, [SqlQueryDependent] string alias)
		{
			return obj;
		}

		#endregion

		#region NoConvert

		[Function("$Convert_Remover$", ServerSideOnly = true)]
		static TR ConvertRemover<T, TR>(T input)
		{
			throw new NotImplementedException();
		}

		class NoConvertBuilder : IExtensionCallBuilder
		{
			private static readonly MethodInfo _method = MethodHelper.GetMethodInfo(ConvertRemover<int, int>, 0).GetGenericMethodDefinition();

			public void Build(ISqExtensionBuilder builder)
			{
				var expr    = builder.Arguments[0];
				var newExpr = expr.Transform(static (_, e) =>
				{
					if (e.NodeType == ExpressionType.Convert || e.NodeType == ExpressionType.ConvertChecked)
					{
						var unary  = (UnaryExpression)e;
						var method = _method.MakeGenericMethod(unary.Operand.Type, unary.Type);
						return Expression.Call(null, method, unary.Operand);
					}
					return e;
				});

				if (newExpr == expr)
				{
					builder.ResultExpression = builder.GetExpression(0);
					return;
				}

				var sqlExpr = builder.ConvertExpressionToSql(newExpr);
				sqlExpr     = ConvertVisitor.Convert(sqlExpr, (v, e) =>
				{
					if (e is SqlFunction func && func.Name == "$Convert_Remover$")
					{
						return func.Parameters[0];
					}
					return e;
				});

				builder.ResultExpression = sqlExpr;
			}
		}

		[Extension("", BuilderType = typeof(NoConvertBuilder), ServerSideOnly = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T NoConvert<T>(T expr)
		{
			return expr;
		}

		#endregion

		#region Guid Functions

		[Function  (PN.Oracle,   "Sys_Guid", ServerSideOnly = true, CanBeNull = false, IsPure = false)]
		[Function  (PN.Firebird, "Gen_Uuid", ServerSideOnly = true, CanBeNull = false, IsPure = false)]
		[Function  (PN.MySql,    "Uuid",     ServerSideOnly = true, CanBeNull = false, IsPure = false)]
		[Expression(PN.Sybase,   "NewID(1)", ServerSideOnly = true, CanBeNull = false, IsPure = false)]
		[Expression(PN.SapHana,  "SYSUUID",  ServerSideOnly = true, CanBeNull = false, IsPure = false)]
		[Function  (             "NewID",    ServerSideOnly = true, CanBeNull = false, IsPure = false)]
		public static Guid NewGuid()
		{
			return Guid.NewGuid();
		}

		#endregion

		#region Convert Functions

		[CLSCompliant(false)]
		[Function("Convert", 0, 1, ServerSideOnly = true, IsNullable = IsNullableType.SameAsSecondParameter)]
		public static TTo Convert<TTo,TFrom>(TTo to, TFrom from)
		{
			var dt = Common.ConvertTo<TTo>.From(from);
			return dt;
		}

		[CLSCompliant(false)]
		[Function("Convert", 0, 1, 2, ServerSideOnly = true, IsNullable = IsNullableType.SameAsSecondParameter)]
		public static TTo Convert<TTo, TFrom>(TTo to, TFrom from, int format)
		{
			var dt = Common.ConvertTo<TTo>.From(from);
			return dt;
		}

		[CLSCompliant(false)]
		[Function("Convert", 0, 1, IsNullable = IsNullableType.SameAsSecondParameter)]
		public static TTo Convert2<TTo,TFrom>(TTo to, TFrom from)
		{
			return Common.ConvertTo<TTo>.From(from);
		}

		[CLSCompliant(false)]
		[Function("$Convert$", 1, 2, 0)]
		public static TTo Convert<TTo,TFrom>(TFrom obj)
		{
			return Common.ConvertTo<TTo>.From(obj);
		}

		public static class ConvertTo<TTo>
		{
			[CLSCompliant(false)]
			[Function("$Convert$", 1, 2, 0)]
			public static TTo From<TFrom>(TFrom obj)
			{
				return Common.ConvertTo<TTo>.From(obj);
			}
		}

		[Expression("{0}", IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static TimeSpan? DateToTime(DateTime? date)
		{
			return date == null ? null : new TimeSpan(date.Value.Ticks);
		}

		[Property(PN.Informix,      "Boolean",        ServerSideOnly=true)]
		[Property(PN.PostgreSQL,    "Boolean",        ServerSideOnly=true)]
		[Property(PN.MySql,         "Boolean",        ServerSideOnly=true)]
		[Property(PN.SQLite,        "Boolean",        ServerSideOnly=true)]
		[Property(PN.SapHana,       "TinyInt",        ServerSideOnly=true)]
		[Property(                  "Bit",            ServerSideOnly=true)] public static bool           Bit                               { get { return false; } }

		[Property(PN.Oracle,        "Number(19)",     ServerSideOnly=true)]
		[Property(                  "BigInt",         ServerSideOnly=true)] public static long           BigInt                            { get { return 0; } }

		[Property(PN.MySql,         "Signed",         ServerSideOnly=true)]
		[Property(                  "Int",            ServerSideOnly=true)] public static int            Int                               { get { return 0; } }

		[Property(PN.MySql,         "Signed",         ServerSideOnly=true)]
		[Property(                  "SmallInt",       ServerSideOnly=true)] public static short          SmallInt                          { get { return 0; } }

		[Property(PN.DB2,           "SmallInt",       ServerSideOnly=true)]
		[Property(PN.Informix,      "SmallInt",       ServerSideOnly=true)]
		[Property(PN.Oracle,        "Number(3)",      ServerSideOnly=true)]
		[Property(PN.DB2,           "SmallInt",       ServerSideOnly=true)]
		[Property(PN.Firebird,      "SmallInt",       ServerSideOnly=true)]
		[Property(PN.PostgreSQL,    "SmallInt",       ServerSideOnly=true)]
		[Property(PN.MySql,         "Unsigned",       ServerSideOnly=true)]
		[Property(                  "TinyInt",        ServerSideOnly=true)] public static byte           TinyInt                           { get { return 0; } }

		[Property(                  "Decimal",        ServerSideOnly=true)] public static decimal DefaultDecimal                           { get { return 0; } }
		[Expression(PN.SapHana,     "Decimal({0},4)", ServerSideOnly=true)]
		[Function(                                    ServerSideOnly=true)] public static decimal        Decimal(int precision)            {       return 0;   }
		[Function(                                    ServerSideOnly=true)] public static decimal        Decimal(int precision, int scale) {       return 0;   }

		[Property(PN.Oracle,        "Number(19,4)",   ServerSideOnly=true)]
		[Property(PN.Firebird,      "Decimal(18,4)",  ServerSideOnly=true)]
		[Property(PN.PostgreSQL,    "Decimal(19,4)",  ServerSideOnly=true)]
		[Property(PN.MySql,         "Decimal(19,4)",  ServerSideOnly=true)]
		[Property(PN.SapHana,       "Decimal(19,4)",  ServerSideOnly=true)]
		[Property(                  "Money",          ServerSideOnly=true)] public static decimal        Money                             { get { return 0; } }

		[Property(PN.Informix,      "Decimal(10,4)",  ServerSideOnly=true)]
		[Property(PN.Oracle,        "Number(10,4)",   ServerSideOnly=true)]
		[Property(PN.Firebird,      "Decimal(10,4)",  ServerSideOnly=true)]
		[Property(PN.PostgreSQL,    "Decimal(10,4)",  ServerSideOnly=true)]
		[Property(PN.MySql,         "Decimal(10,4)",  ServerSideOnly=true)]
		[Property(PN.SqlCe,         "Decimal(10,4)",  ServerSideOnly=true)]
		[Property(PN.SapHana,       "Decimal(10,4)",  ServerSideOnly=true)]
		[Property(                  "SmallMoney",     ServerSideOnly=true)] public static decimal        SmallMoney                        { get { return 0; } }

		[Property(PN.MySql,         "Decimal(29,10)", ServerSideOnly=true)]
		[Property(PN.SapHana,       "Double",         ServerSideOnly=true)]
		[Property(                  "Float",          ServerSideOnly=true)] public static double         Float                             { get { return 0; } }

		[Property(PN.MySql,         "Decimal(29,10)", ServerSideOnly=true)]
		[Property(                  "Real",           ServerSideOnly=true)] public static float         Real                              { get { return 0; } }

		[Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true)]
		[Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true)]
		[Property(PN.SapHana,       "TimeStamp",      ServerSideOnly=true)]
		[Property(                  "DateTime",       ServerSideOnly=true)] public static DateTime       DateTime                          { get { return DateTime.Now; } }

		[Property(PN.SqlServer2000, "DateTime",       ServerSideOnly=true)]
		[Property(PN.SqlServer2005, "DateTime",       ServerSideOnly=true)]
		[Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true)]
		[Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true)]
		[Property(PN.MySql,         "DateTime",       ServerSideOnly=true)]
		[Property(PN.SqlCe,         "DateTime",       ServerSideOnly=true)]
		[Property(PN.Sybase,        "DateTime",       ServerSideOnly=true)]
		[Property(PN.SapHana,       "TimeStamp",      ServerSideOnly=true)]
		[Property(                  "DateTime2",      ServerSideOnly=true)] public static DateTime       DateTime2                         { get { return DateTime.Now; } }

		[Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true)]
		[Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true)]
		[Property(PN.MySql,         "DateTime",       ServerSideOnly=true)]
		[Property(PN.SqlCe,         "DateTime",       ServerSideOnly=true)]
		[Property(PN.SapHana,       "SecondDate",     ServerSideOnly=true)]
		[Property(                  "SmallDateTime",  ServerSideOnly=true)] public static DateTime       SmallDateTime                     { get { return DateTime.Now; } }

		[Property(PN.SqlServer2000, "Datetime",       ServerSideOnly=true)]
		[Property(PN.SqlServer2005, "Datetime",       ServerSideOnly=true)]
		[Property(PN.SqlCe,         "Datetime",       ServerSideOnly=true)]
		[Property(                  "Date",           ServerSideOnly=true)] public static DateTime       Date                              { get { return DateTime.Now; } }

		[Property(                  "Time",           ServerSideOnly=true)] public static DateTime       Time                              { get { return DateTime.Now; } }

		[Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true)]
		[Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true)]
		[Property(PN.SqlServer2017, "DateTimeOffset", ServerSideOnly=true)]
		[Property(PN.SqlServer2016, "DateTimeOffset", ServerSideOnly=true)]
		[Property(PN.SqlServer2012, "DateTimeOffset", ServerSideOnly=true)]
		[Property(PN.SqlServer2008, "DateTimeOffset", ServerSideOnly=true)]
		[Property(PN.SapHana,       "TimeStamp",      ServerSideOnly=true)]
		[Property(                  "DateTime",       ServerSideOnly=true)] public static DateTimeOffset DateTimeOffset                    { get { return DateTimeOffset.Now; } }

		[Function(PN.SqlCe,         "NChar",          ServerSideOnly=true)]
		[Function(                                    ServerSideOnly=true)] public static string         Char(int length)                  {       return ""; }

		[Property(PN.SqlCe,         "NChar",          ServerSideOnly=true)]
		[Property(                  "Char",           ServerSideOnly=true)] public static string  DefaultChar                              { get { return ""; } }

		[Function(PN.MySql,         "Char",           ServerSideOnly=true)]
		[Function(PN.SqlCe,         "NVarChar",       ServerSideOnly=true)]
		[Function(                                    ServerSideOnly=true)] public static string         VarChar(int length)               {       return ""; }

		[Property(PN.MySql,         "Char",           ServerSideOnly=true)]
		[Property(PN.SqlCe,         "NVarChar",       ServerSideOnly=true)]
		[Property(                  "VarChar",        ServerSideOnly=true)] public static string  DefaultVarChar                           { get { return ""; } }

		[Function(PN.DB2,           "Char",           ServerSideOnly=true)]
		[Function(                                    ServerSideOnly=true)] public static string         NChar(int length)                 {       return ""; }

		[Property(PN.DB2,           "Char",           ServerSideOnly=true)]
		[Property(                  "NChar",          ServerSideOnly=true)] public static string  DefaultNChar                             { get { return ""; } }

		[Function(PN.DB2,           "Char",           ServerSideOnly=true)]
		[Function(PN.Oracle,        "VarChar2",       ServerSideOnly=true)]
		[Function(PN.Firebird,      "VarChar",        ServerSideOnly=true)]
		[Function(PN.PostgreSQL,    "VarChar",        ServerSideOnly=true)]
		[Function(PN.MySql,         "Char",           ServerSideOnly=true)]
		[Function(                                    ServerSideOnly=true)] public static string         NVarChar(int length)              {       return ""; }

		[Property(PN.DB2,           "Char",           ServerSideOnly=true)]
		[Property(PN.Oracle,        "VarChar2",       ServerSideOnly=true)]
		[Property(PN.Firebird,      "VarChar",        ServerSideOnly=true)]
		[Property(PN.PostgreSQL,    "VarChar",        ServerSideOnly=true)]
		[Property(PN.MySql,         "Char",           ServerSideOnly=true)]
		[Property(                  "NVarChar",       ServerSideOnly=true)] public static string  DefaultNVarChar                          { get { return ""; } }

		#endregion

		#region String Functions

		[Function  (                                                   PreferServerSide = true)]
		[Function  (PN.Access,    "Len",                               PreferServerSide = true)]
		[Function  (PN.Firebird,  "Char_Length",                       PreferServerSide = true)]
		[Function  (PN.SqlServer, "Len",                               PreferServerSide = true)]
		[Function  (PN.SqlCe,     "Len",                               PreferServerSide = true)]
		[Function  (PN.Sybase,    "Len",                               PreferServerSide = true)]
		[Function  (PN.MySql,     "Char_Length",                       PreferServerSide = true)]
		[Expression(PN.DB2LUW,    "CHARACTER_LENGTH({0},CODEUNITS32)", PreferServerSide = true)]
		public static int? Length(string str)
		{
			return str?.Length;
		}

		[Function  (                                                PreferServerSide = true)]
		[Function  (PN.Access,   "Mid",                             PreferServerSide = true)]
		[Function  (PN.DB2,      "Substr",                          PreferServerSide = true)]
		[Function  (PN.Informix, "Substr",                          PreferServerSide = true)]
		[Function  (PN.Oracle,   "Substr",                          PreferServerSide = true)]
		[Function  (PN.SQLite,   "Substr",                          PreferServerSide = true)]
		[Expression(PN.Firebird, "Substring({0} from {1} for {2})", PreferServerSide = true)]
		[Function  (PN.SapHana,  "Substring",                       PreferServerSide = true)]
		public static string? Substring(string? str, int? start, int? length)
		{
			if (str == null || start == null || length == null) return null;
			if (start.Value < 1 || start.Value > str.Length) return null;
			if (length.Value < 0) return null;

			var index = start.Value - 1;
			var maxAllowedLength = Math.Min(str.Length - index, length.Value);

			return str.Substring(index, maxAllowedLength);
		}

		[Function(ServerSideOnly = true)]
		public static bool Like(string? matchExpression, string? pattern)
		{
#if !NETFRAMEWORK
			throw new InvalidOperationException();
#else
			return matchExpression != null && pattern != null &&
				System.Data.Linq.SqlClient.SqlMethods.Like(matchExpression, pattern);
#endif
		}

		[Function(ServerSideOnly = true)]
		public static bool Like(string? matchExpression, string? pattern, char? escapeCharacter)
		{
#if !NETFRAMEWORK
			throw new InvalidOperationException();
#else
			return matchExpression != null && pattern != null && escapeCharacter != null &&
				System.Data.Linq.SqlClient.SqlMethods.Like(matchExpression, pattern, escapeCharacter.Value);
#endif
		}

		[CLSCompliant(false)]
		[Function]
		[Function(PN.DB2,      "Locate")]
		[Function(PN.MySql,    "Locate")]
		[Function(PN.SapHana,  "Locate", 1, 0)]
		[Function(PN.Firebird, "Position")]
		public static int? CharIndex(string? substring, string? str)
		{
			if (str == null || substring == null) return null;

			// Database CharIndex returns:
			//  1-based position, when sequence is found
			//  0 when substring is empty
			//  0 when substring is not found

			// IndexOf returns:
			//  0 when substring is empty <= this needs to handled special way to mimic behavior.
			//  -1 when substring is not found

			return substring.Length == 0 ? 0 : str.IndexOf(substring) + 1;
		}

		[Function]
		[Function  (PN.DB2,      "Locate")]
		[Function  (PN.MySql,    "Locate")]
		[Function  (PN.Firebird, "Position")]
		[Expression(PN.SapHana,  "Locate(Substring({1},{2} + 1),{0}) + {2}")]
		public static int? CharIndex(string? substring, string? str, int? start)
		{
			if (str == null || substring == null || start == null) return null;

			var index = start.Value < 1 ? 0 : start.Value > str.Length ? str.Length - 1 : start.Value - 1;
			return substring.Length == 0 ? 0 : str.IndexOf(substring, index) + 1;
		}

		[Function]
		[Function(PN.DB2,     "Locate")]
		[Function(PN.MySql,   "Locate")]
		[Function(PN.SapHana, "Locate")]
		public static int? CharIndex(char? value, string? str)
		{
			if (value == null || str == null) return null;

			return str.IndexOf(value.Value) + 1;
		}

		[Function]
		[Function(PN.DB2,     "Locate")]
		[Function(PN.MySql,   "Locate")]
		[Function(PN.SapHana, "Locate")]
		public static int? CharIndex(char? value, string? str, int? start)
		{
			if (str == null || value == null || start == null) return null;

			var index = start.Value < 1 ? 0 : start.Value > str.Length ? str.Length - 1 : start.Value - 1;
			return str.IndexOf(value.Value, index) + 1;
		}

		[Function]
		public static string? Reverse(string? str)
		{
			if (string.IsNullOrEmpty(str)) return str;

			var chars = str!.ToCharArray();
			Array.Reverse(chars);
			return new string(chars);
		}

		[Function(                      PreferServerSide = true)]
		[Function(PN.SQLite, "LeftStr", PreferServerSide = true)]
		public static string? Left(string? str, int? length)
		{
			if (length == null || str == null) return null;
			if (length.Value < 0)              return null;
			if (length.Value > str.Length)     return str;

			return str.Substring(0, length.Value);
		}

		[Function(                       PreferServerSide = true)]
		[Function(PN.SQLite, "RightStr", PreferServerSide = true)]
		public static string? Right(string? str, int? length)
		{
			if (length == null || str == null) return null;
			if (length.Value < 0)              return null;
			if (length.Value > str.Length)     return str;

			return str.Substring(str.Length - length.Value);
		}

		[Function]
		public static string? Stuff(string? str, int? start, int? length, string? newString)
		{
			if (str == null || start == null || length == null || newString == null) return null;
			if (start.Value < 1 || start.Value > str.Length)                         return null;
			if (length.Value < 0)                                                    return null;

			var index = start.Value - 1;
			var maxAllowedLength = Math.Min(str.Length - index, length.Value);

			return str.Remove(index, maxAllowedLength).Insert(index, newString);
		}

		[Function(ServerSideOnly = true)]
		public static string Stuff(IEnumerable<string> characterExpression, int? start, int? length, string replaceWithExpression)
		{
			throw new NotImplementedException();
		}

		[Function]
		[Expression(PN.SapHana, "Lpad('',{0},' ')")]
		public static string? Space(int? length)
		{
			return length == null || length.Value < 0 ? null : "".PadRight(length.Value);
		}

		[Function(Name = "LPad")]
		public static string? PadLeft(string? str, int? length, char? paddingChar)
		{
			if (str == null || length == null || paddingChar == null) return null;
			if (length.Value < 0)                                     return null;
			if (length.Value <= str.Length)                           return str.Substring(0, length.Value);

			return str.PadLeft(length.Value, paddingChar.Value);
		}

		[Function(Name = "RPad")]
		public static string? PadRight(string? str, int? length, char? paddingChar)
		{
			if (str == null || length == null || paddingChar == null) return null;
			if (length.Value < 0) return null;
			if (length.Value <= str.Length) return str.Substring(0, length.Value);

			return str.PadRight(length.Value, paddingChar.Value);
		}

		[Function("$Replace$")]
		public static string? Replace(string? str, string? oldValue, string? newValue)
		{
			if (str == null || oldValue == null || newValue == null) return null;
			if (str.Length == 0)                                     return str;
			if (oldValue.Length == 0)                                return str; // Replace raises exception here.

			return str.Replace(oldValue, newValue);
		}

		[Function]
		[Function(PN.Sybase, "Str_Replace")]
		public static string? Replace(string? str, char? oldValue, char? newValue)
		{
			if (str == null || oldValue == null || newValue == null) return null;
			if (str.Length == 0)                                     return str;

			return str.Replace(oldValue.Value, newValue.Value);
		}

		#region IsNullOrWhiteSpace
		// set of all White_Space characters per Unicode v13
		const string WHITESPACES = "\x09\x0A\x0B\x0C\x0D\x20\x85\xA0\x1680\x2000\x2001\x2002\x2003\x2004\x2005\x2006\x2007\x2008\x2009\x200A\x2028\x2029\x205F\x3000";
		const string ASCII_WHITESPACES = "\x09\x0A\x0B\x0C\x0D\x20\x85\xA0";

		/*
		 * marked internal as we don't have plans now to expose it directly (used by string.IsNullOrWhiteSpace mapping)
		 * 
		 * implementation tries to mimic .NET implementation of string.IsNullOrWhiteSpace (except null check part):
		 * return true if string doesn't contain any symbols except White_Space codepoints from Unicode.
		 * 
		 * Known limitations:
		 * 1. [Access] we handle only following WS:
		 * - 0x20 (SPACE)
		 * - 0x1680 (OGHAM SPACE MARK)
		 * - 0x205F (MEDIUM MATHEMATICAL SPACE)
		 * - 0x3000 (IDEOGRAPHIC SPACE)
		 * Proper implementation will be same as we use for SqlCe, but Replace function is not exposed to SQL by default
		 * and requires sandbox mode: https://support.microsoft.com/en-us/office/turn-sandbox-mode-on-or-off-to-disable-macros-8cc7bad8-38c2-4a7a-a604-43e9a7bbc4fb
		 * 2. [Informix} implementation use only ASCII whitespaces which probably will not work in some cases for WS outside of
		 * ASCII range (currently works in our tests, but it could be that it depends on used encodings)
		 */
		[Extension(                  typeof(IsNullOrWhiteSpaceDefaultBuilder),       IsPredicate = true)]
		[Extension(PN.Oracle,        typeof(IsNullOrWhiteSpaceOracleBuilder),        IsPredicate = true)]
		[Extension(PN.Informix,      typeof(IsNullOrWhiteSpaceInformixBuilder),      IsPredicate = true)]
		[Extension(PN.SqlServer,     typeof(IsNullOrWhiteSpaceSqlServerBuilder),     IsPredicate = true)]
		[Extension(PN.SqlServer2017, typeof(IsNullOrWhiteSpaceSqlServer2017Builder), IsPredicate = true)]
		[Extension(PN.Access,        typeof(IsNullOrWhiteSpaceAccessBuilder),        IsPredicate = true)]
		[Extension(PN.Sybase,        typeof(IsNullOrWhiteSpaceSybaseBuilder),        IsPredicate = true)]
		[Extension(PN.MySql,         typeof(IsNullOrWhiteSpaceMySqlBuilder),         IsPredicate = true)]
		[Extension(PN.Firebird,      typeof(IsNullOrWhiteSpaceFirebirdBuilder),      IsPredicate = true)]
		[Extension(PN.SqlCe,         typeof(IsNullOrWhiteSpaceSqlCeBuilder),         IsPredicate = true)]
		internal static bool IsNullOrWhiteSpace(string? str) => string.IsNullOrWhiteSpace(str);

		// str IS NULL OR REPLACE...(str, WHITEPACES, '') == ''
		internal class IsNullOrWhiteSpaceSqlCeBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str");

				var condition = new SqlCondition(
					false,
					new SqlPredicate.ExprExpr(
						new SqlExpression(
							typeof(string),
							"REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE({0}, '\x09', ''), '\x0a', ''), '\x0b', ''), '\x0c', ''), '\x0d', ''), '\x20', ''), '\x85', ''), '\xa0', ''), '\x1680', ''), '\x2000', ''), '\x2001', ''), '\x2002', ''), '\x2003', ''), '\x2004', ''), '\x2005', ''), '\x2006', ''), '\x2007', ''), '\x2008', ''), '\x2009', ''), '\x200a', ''), '\x2028', ''), '\x2029', ''), '\x205f', ''), '\x3000', '')",
							str),
						SqlPredicate.Operator.Equal,
						new SqlValue(typeof(string), string.Empty), false),
					true);

				if (str.CanBeNull)
					builder.ResultExpression = new SqlSearchCondition(
						new SqlCondition(false, new SqlPredicate.IsNull(str, false), true),
						condition);
				else
					builder.ResultExpression = new SqlSearchCondition(condition);
			}
		}

		// str IS NULL OR NOT(str SIMILAR TO _utf8 x'%[^WHITESPACES_UTF8]%')
		internal class IsNullOrWhiteSpaceFirebirdBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str");

				var condition = new SqlCondition(
					false,
					new SqlPredicate.NotExpr(
						new SqlExpression(
							typeof(bool),
							"{0} SIMILAR TO {1}",
							Precedence.Comparison,
							SqlFlags.IsPredicate,
							str,
							new SqlValue(typeof(string), $"%[^{WHITESPACES}]%")),
						true,
						Precedence.LogicalNegation),
					true);

				if (str.CanBeNull)
					builder.ResultExpression = new SqlSearchCondition(
						new SqlCondition(false, new SqlPredicate.IsNull(str, false), true),
						condition);
				else
					builder.ResultExpression = new SqlSearchCondition(condition);
			}
		}

		// str IS NULL OR NOT(str RLIKE '%[^WHITESPACES]%')
		internal class IsNullOrWhiteSpaceMySqlBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str");

				var condition = new SqlCondition(
					false,
					new SqlPredicate.NotExpr(
						new SqlExpression(
							typeof(bool),
							"{0} RLIKE {1}",
							Precedence.Comparison,
							SqlFlags.IsPredicate,
							str,
							new SqlValue(typeof(string), $"[^{WHITESPACES}]")),
						true,
						Precedence.LogicalNegation),
					true);

				if (str.CanBeNull)
					builder.ResultExpression = new SqlSearchCondition(
						new SqlCondition(false, new SqlPredicate.IsNull(str, false), true),
						condition);
				else
					builder.ResultExpression = new SqlSearchCondition(condition);
			}
		}

		// str IS NULL OR str NOT LIKE '%[^WHITESPACES]%'
		internal class IsNullOrWhiteSpaceSybaseBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str");

				var condition = new SqlCondition(
					false,
					new SqlPredicate.Like(
						str,
						true,
						new SqlValue(typeof(string), $"%[^{WHITESPACES}]%"),
						null),
					true);

				if (str.CanBeNull)
					builder.ResultExpression = new SqlSearchCondition(
						new SqlCondition(false, new SqlPredicate.IsNull(str, false), true),
						condition);
				else
					builder.ResultExpression = new SqlSearchCondition(condition);
			}
		}

		// str IS NULL OR str NOT LIKE N'%[^WHITESPACES]%'
		internal class IsNullOrWhiteSpaceSqlServerBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str");

				var condition = new SqlCondition(
					false,
					new SqlPredicate.Like(
						str,
						true,
						new SqlValue(new DbDataType(typeof(string), DataType.NVarChar), $"%[^{WHITESPACES}]%"),
						null),
					true);

				if (str.CanBeNull)
					builder.ResultExpression = new SqlSearchCondition(
						new SqlCondition(false, new SqlPredicate.IsNull(str, false), true),
						condition);
				else
					builder.ResultExpression = new SqlSearchCondition(condition);
			}
		}

		// str IS NULL OR LTRIM(str, '') = ''
		internal class IsNullOrWhiteSpaceAccessBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str");

				var condition = new SqlCondition(
					false,
					new SqlPredicate.ExprExpr(
						new SqlFunction(typeof(string), "LTRIM", str),
						SqlPredicate.Operator.Equal,
						new SqlValue(typeof(string), string.Empty), false),
					true);

				if (str.CanBeNull)
					builder.ResultExpression = new SqlSearchCondition(
						new SqlCondition(false, new SqlPredicate.IsNull(str, false), true),
						condition);
				else
					builder.ResultExpression = new SqlSearchCondition(condition);
			}
		}

		// str IS NULL OR TRIM(N'WHITESPACES FROM str) = ''
		internal class IsNullOrWhiteSpaceSqlServer2017Builder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str");

				var condition = new SqlCondition(
					false,
					new SqlPredicate.ExprExpr(
						new SqlExpression(typeof(string), "TRIM({1} FROM {0})", str, new SqlValue(new DbDataType(typeof(string), DataType.NVarChar), WHITESPACES)),
						SqlPredicate.Operator.Equal,
						new SqlValue(typeof(string), string.Empty), false),
					true);

				if (str.CanBeNull)
					builder.ResultExpression = new SqlSearchCondition(
						new SqlCondition(false, new SqlPredicate.IsNull(str, false), true),
						condition);
				else
					builder.ResultExpression = new SqlSearchCondition(condition);
			}
		}

		// str IS NULL OR LTRIM(str, WHITESPACES) IS NULL
		internal class IsNullOrWhiteSpaceOracleBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str");

				var condition = new SqlCondition(
					false,
					new SqlPredicate.IsNull(new SqlFunction(typeof(string), "LTRIM", str, new SqlValue(typeof(string), WHITESPACES)), false),
					true);

				if (str.CanBeNull)
					builder.ResultExpression = new SqlSearchCondition(
						new SqlCondition(false, new SqlPredicate.IsNull(str, false), true),
						condition);
				else
					builder.ResultExpression = new SqlSearchCondition(condition);
			}
		}

		// str IS NULL OR LTRIM(str, ASCII_WHITESPACES) = ''
		internal class IsNullOrWhiteSpaceInformixBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str");

				var condition = new SqlCondition(
					false,
					new SqlPredicate.ExprExpr(
						new SqlFunction(typeof(string), "LTRIM", str, new SqlValue(typeof(string), ASCII_WHITESPACES)),
						SqlPredicate.Operator.Equal,
						new SqlValue(typeof(string), string.Empty), false),
					true);

				if (str.CanBeNull)
					builder.ResultExpression = new SqlSearchCondition(
						new SqlCondition(false, new SqlPredicate.IsNull(str, false), true),
						condition);
				else
					builder.ResultExpression = new SqlSearchCondition(condition);
			}
		}

		// str IS NULL OR LTRIM(str, WHITESPACES) = ''
		internal class IsNullOrWhiteSpaceDefaultBuilder : IExtensionCallBuilder
		{
			void IExtensionCallBuilder.Build(ISqExtensionBuilder builder)
			{
				var str = builder.GetExpression("str");

				var condition = new SqlCondition(
					false,
					new SqlPredicate.ExprExpr(
						new SqlFunction(typeof(string), "LTRIM", str, new SqlValue(typeof(string), WHITESPACES)),
						SqlPredicate.Operator.Equal,
						new SqlValue(typeof(string), string.Empty), false),
					true);

				if (str.CanBeNull)
					builder.ResultExpression = new SqlSearchCondition(
						new SqlCondition(false, new SqlPredicate.IsNull(str, false), true),
						condition);
				else
					builder.ResultExpression = new SqlSearchCondition(condition);
			}
		}
		#endregion

		[Function]
		public static string? Trim(string? str)
		{
			return str?.Trim();
		}

		[Expression(PN.Firebird, "TRIM(LEADING FROM {0})")]
		[Function("LTrim")]
		public static string? TrimLeft(string? str)
		{
			return str?.TrimStart();
		}

		[Expression(PN.Firebird, "TRIM(TRAILING FROM {0})")]
		[Function("RTrim")]
		public static string? TrimRight(string? str)
		{
			return str?.TrimEnd();
		}

		[Function]
		[Expression(PN.DB2, "Strip({0}, B, {1})")]
		public static string? Trim(string? str, char? ch)
		{
			return str == null || ch == null ? null : str.Trim(ch.Value);
		}

		[Expression(PN.Firebird, "TRIM(LEADING {1} FROM {0})")]
		[Expression(PN.DB2, "Strip({0}, L, {1})")]
		[Function  (        "LTrim")]
		public static string? TrimLeft(string? str, char? ch)
		{
			return str == null || ch == null ? null : str.TrimStart(ch.Value);
		}

		[Expression(PN.Firebird, "TRIM(TRAILING {1} FROM {0})")]
		[Expression(PN.DB2, "Strip({0}, T, {1})")]
		[Function  (        "RTrim")]
		public static string? TrimRight(string? str, char? ch)
		{
			return str == null || ch == null ? null : str.TrimEnd(ch.Value);
		}

		[Function("$ToLower$", ServerSideOnly = true)]
		public static string? Lower(string? str)
		{
			return str?.ToLower();
		}

		[Function("$ToUpper$", ServerSideOnly = true)]
		public static string? Upper(string? str)
		{
			return str?.ToUpper();
		}

		class ConcatAttribute : ExpressionAttribute
		{
			public ConcatAttribute() : base("")
			{
			}

			public override ISqlExpression? GetExpression(IDataContext dataContext, SelectQuery query, Expression expression, Func<Expression, ColumnDescriptor?, ISqlExpression> converter)
			{
				var expressionStr = Expression;
				PrepareParameterValues(expression, ref expressionStr, true, out var knownExpressions, out _);

				var arr = new ISqlExpression[knownExpressions.Count];

				for (var i = 0; i < knownExpressions.Count; i++)
				{
					var arg = converter(knownExpressions[i]!, null);

					if (arg.SystemType == typeof(string))
					{
						arr[i] = arg;
					}
					else
					{
						var len = arg.SystemType == null || arg.SystemType == typeof(object) ?
							100 :
							SqlDataType.GetMaxDisplaySize(SqlDataType.GetDataType(arg.SystemType).Type.DataType);

						arr[i] = new SqlFunction(typeof(string), "Convert", new SqlDataType(DataType.VarChar, len), arg);
					}
				}

				if (arr.Length == 1)
					return arr[0];

				var expr = new SqlBinaryExpression(typeof(string), arr[0], "+", arr[1]);

				for (var i = 2; i < arr.Length; i++)
					expr = new SqlBinaryExpression(typeof (string), expr, "+", arr[i]);

				return expr;
			}
		}

		[Concat]
		public static string Concat(params object[] args)
		{
			return string.Concat(args);
		}

		[Concat]
		public static string Concat(params string[] args)
		{
			return string.Concat(args);
		}

		#endregion

		#region Binary Functions

		[Function(                              PreferServerSide = true)]
		[Function(PN.Access,    "Len",          PreferServerSide = true)]
		[Function(PN.Firebird,  "Octet_Length", PreferServerSide = true)]
		[Function(PN.SqlServer, "DataLength",   PreferServerSide = true)]
		[Function(PN.SqlCe,     "DataLength",   PreferServerSide = true)]
		[Function(PN.Sybase,    "DataLength",   PreferServerSide = true)]
		[Function(PN.SQLite,    "Length",       PreferServerSide = true)]
		public static int? Length(Binary? value)
		{
			return value == null ? null : value.Length;
		}

		#endregion

		#region Byte[] Functions

		[Function(                              PreferServerSide = true)]
		[Function(PN.Access,    "Len",          PreferServerSide = true)]
		[Function(PN.Firebird,  "Octet_Length", PreferServerSide = true)]
		[Function(PN.SqlServer, "DataLength",   PreferServerSide = true)]
		[Function(PN.SqlCe,     "DataLength",   PreferServerSide = true)]
		[Function(PN.Sybase,    "DataLength",   PreferServerSide = true)]
		[Function(PN.SQLite,    "Length",       PreferServerSide = true)]
		public static int? Length(byte[]? value)
		{
			return value == null ? null : value.Length;
		}

		#endregion

		#region DateTime Functions

		[Property(             "CURRENT_TIMESTAMP", CanBeNull = false)]
		[Property(PN.Informix, "CURRENT",           CanBeNull = false)]
		[Property(PN.Access,   "Now",               CanBeNull = false)]
		public static DateTime GetDate()
		{
			return DateTime.Now;
		}

		[Property(             "CURRENT_TIMESTAMP", ServerSideOnly = true, CanBeNull = false)]
		[Property(PN.Firebird, "LOCALTIMESTAMP",    ServerSideOnly = true, CanBeNull = false)]
		[Property(PN.Informix, "CURRENT",           ServerSideOnly = true, CanBeNull = false)]
		[Property(PN.Access,   "Now",               ServerSideOnly = true, CanBeNull = false)]
		[Function(PN.SqlCe,    "GetDate",           ServerSideOnly = true, CanBeNull = false)]
		[Function(PN.Sybase,   "GetDate",           ServerSideOnly = true, CanBeNull = false)]
		public static DateTime CurrentTimestamp => throw new LinqException("'CurrentTimestamp' is server side only property.");

		[Function  (PN.SqlServer , "SYSUTCDATETIME"                      , ServerSideOnly = true, CanBeNull = false)]
		[Function  (PN.Sybase    , "GETUTCDATE"                          , ServerSideOnly = true, CanBeNull = false)]
		[Expression(PN.SQLite    , "DATETIME('now')"                     , ServerSideOnly = true, CanBeNull = false)]
		[Function  (PN.MySql     , "UTC_TIMESTAMP"                       , ServerSideOnly = true, CanBeNull = false)]
		[Expression(PN.PostgreSQL, "timezone('UTC', now())"              , ServerSideOnly = true, CanBeNull = false)]
		[Expression(PN.DB2       , "CURRENT TIMESTAMP - CURRENT TIMEZONE", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Subtraction)]
		[Expression(PN.Oracle    , "SYS_EXTRACT_UTC(SYSTIMESTAMP)"       , ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Additive)]
		[Property  (PN.SapHana   , "CURRENT_UTCTIMESTAMP"                , ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Additive)]
		[Expression(PN.Informix  , "datetime(1970-01-01 00:00:00) year to second + (dbinfo('utc_current')/86400)::int::char(9)::interval day(9) to day + (mod(dbinfo('utc_current'), 86400))::char(5)::interval second(5) to second", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Additive)]
		public static DateTime CurrentTimestampUtc => DateTime.UtcNow;

		[Property(             "CURRENT_TIMESTAMP", CanBeNull = false)]
		[Property(PN.Informix, "CURRENT",           CanBeNull = false)]
		[Property(PN.Access,   "Now",               CanBeNull = false)]
		[Function(PN.SqlCe,    "GetDate",           CanBeNull = false)]
		[Function(PN.Sybase,   "GetDate",           CanBeNull = false)]
		public static DateTime CurrentTimestamp2 => DateTime.Now;

		[Function(PN.SqlServer , "SYSDATETIMEOFFSET", ServerSideOnly = true, CanBeNull = false)]
		[Function(PN.PostgreSQL, "now"              , ServerSideOnly = true, CanBeNull = false)]
		[Property(PN.Oracle    , "SYSTIMESTAMP"     , ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Additive)]
		public static DateTimeOffset CurrentTzTimestamp => DateTimeOffset.Now;

		[Function]
		public static DateTime? ToDate(int? year, int? month, int? day, int? hour, int? minute, int? second, int? millisecond)
		{
			return year == null || month == null || day == null || hour == null || minute == null || second == null || millisecond == null ?
				null :
				new DateTime(year.Value, month.Value, day.Value, hour.Value, minute.Value, second.Value, millisecond.Value);
		}

		[Function]
		public static DateTime? ToDate(int? year, int? month, int? day, int? hour, int? minute, int? second)
		{
			return year == null || month == null || day == null || hour == null || minute == null || second == null ?
				null :
				new DateTime(year.Value, month.Value, day.Value, hour.Value, minute.Value, second.Value);
		}

		[Function]
		public static DateTime? ToDate(int? year, int? month, int? day)
		{
			return year == null || month == null || day == null ?
				null :
				new DateTime(year.Value, month.Value, day.Value);
		}

		[Property("@@DATEFIRST", CanBeNull = false)]
		public static int DateFirst => 7;

		[Function]
		public static DateTime? MakeDateTime(int? year, int? month, int? day)
		{
			return year == null || month == null || day == null ?
				null :
				new DateTime(year.Value, month.Value, day.Value);
		}

		[Function]
		public static DateTime? MakeDateTime(int? year, int? month, int? day, int? hour, int? minute, int? second)
		{
			return year == null || month == null || day == null || hour == null || minute == null || second == null ?
				null :
				new DateTime(year.Value, month.Value, day.Value, hour.Value, minute.Value, second.Value);
		}

		#endregion

		#region Math Functions

		[Function] public static decimal? Abs    (decimal? value) { return value == null ? null : (decimal?)Math.Abs    (value.Value); }
		[Function] public static double?  Abs    (double?  value) { return value == null ? null : (double?) Math.Abs    (value.Value); }
		[Function] public static short?   Abs    (short?   value) { return value == null ? null : (short?)  Math.Abs    (value.Value); }
		[Function] public static int?     Abs    (int?     value) { return value == null ? null : (int?)    Math.Abs    (value.Value); }
		[Function] public static long?    Abs    (long?    value) { return value == null ? null : (long?)   Math.Abs    (value.Value); }
		[CLSCompliant(false)]
		[Function] public static sbyte?   Abs    (sbyte?   value) { return value == null ? null : (sbyte?)  Math.Abs    (value.Value); }
		[Function] public static float?   Abs    (float?   value) { return value == null ? null : (float?)  Math.Abs    (value.Value); }

		[Function] public static double?  Acos   (double?  value) { return value == null ? null : (double?) Math.Acos   (value.Value); }
		[Function] public static double?  Asin   (double?  value) { return value == null ? null : (double?) Math.Asin   (value.Value); }

		[Function(PN.Access, "Atn")]
		[Function] public static double?  Atan   (double?  value) { return value == null ? null : (double?) Math.Atan   (value.Value); }

		[CLSCompliant(false)]
		[Function(PN.SqlServer, "Atn2")]
		[Function(PN.DB2,       "Atan2", 1, 0)]
		[Function(PN.SqlCe,     "Atn2")]
		[Function(PN.Sybase,    "Atn2")]
		[Function] public static double?  Atan2  (double? x, double? y) { return x == null || y == null? null : (double?)Math.Atan2(x.Value, y.Value); }

		[Function(PN.Informix, "Ceil")]
		[Function(PN.Oracle,   "Ceil")]
		[Function(PN.SapHana,  "Ceil")]
		[Function] public static decimal? Ceiling(decimal? value) { return value == null ? null : (decimal?)decimal.Ceiling(value.Value); }

		[Function(PN.Informix, "Ceil")]
		[Function(PN.Oracle,   "Ceil")]
		[Function(PN.SapHana,  "Ceil")]
		[Function] public static double?  Ceiling(double?  value) { return value == null ? null : (double?)Math.Ceiling(value.Value); }

		[Function] public static double?  Cos    (double?  value) { return value == null ? null : (double?)Math.Cos    (value.Value); }

		[Function] public static double?  Cosh   (double?  value) { return value == null ? null : (double?)Math.Cosh   (value.Value); }

		[Function] public static double?  Cot    (double?  value) { return value == null ? null : (double?)Math.Cos(value.Value) / Math.Sin(value.Value); }

		[Function] public static decimal? Degrees(decimal? value) { return value == null ? null : (decimal?)(value.Value * 180m / (decimal)Math.PI); }
		[Function] public static double?  Degrees(double?  value) { return value == null ? null : (double?) (value.Value * 180 / Math.PI); }
		[Function] public static short?   Degrees(short?   value) { return value == null ? null : (short?)  (value.Value * 180 / Math.PI); }
		[Function] public static int?     Degrees(int?     value) { return value == null ? null : (int?)    (value.Value * 180 / Math.PI); }
		[Function] public static long?    Degrees(long?    value) { return value == null ? null : (long?)   (value.Value * 180 / Math.PI); }
		[CLSCompliant(false)]
		[Function] public static sbyte?   Degrees(sbyte?   value) { return value == null ? null : (sbyte?)  (value.Value * 180 / Math.PI); }
		[Function] public static float?   Degrees(float?   value) { return value == null ? null : (float?)  (value.Value * 180 / Math.PI); }

		[Function] public static double?  Exp    (double?  value) { return value == null ? null : (double?)Math.Exp    (value.Value); }

		[Function(PN.Access, "Int")]
		[Function] public static decimal? Floor  (decimal? value) { return value == null ? null : (decimal?)decimal.Floor(value.Value); }

		[Function(PN.Access, "Int")]
		[Function] public static double?  Floor  (double?  value) { return value == null ? null : (double?) Math.   Floor(value.Value); }

		[Function(PN.Informix,   "LogN")]
		[Function(PN.Oracle,     "Ln")]
		[Function(PN.Firebird,   "Ln")]
		[Function(PN.PostgreSQL, "Ln")]
		[Function(PN.SapHana,    "Ln")]
		[Function] public static decimal? Log    (decimal? value) { return value == null ? null : (decimal?)Math.Log     ((double)value.Value); }

		[Function(PN.Informix,   "LogN")]
		[Function(PN.Oracle,     "Ln")]
		[Function(PN.Firebird,   "Ln")]
		[Function(PN.PostgreSQL, "Ln")]
		[Function(PN.SapHana,    "Ln")]
		[Function] public static double?  Log    (double?  value) { return value == null ? null : (double?) Math.Log     (value.Value); }

		[Function(PN.PostgreSQL, "Log")]
		[Expression(PN.SapHana,  "Log(10,{0})")]
		[Function] public static double?  Log10  (double?  value) { return value == null ? null : (double?) Math.Log10   (value.Value); }

		[Function]
		public static double?  Log(double? newBase, double? value)
		{
			return value == null || newBase == null ? null : (double?)Math.Log(value.Value, newBase.Value);
		}

		[Function]
		public static decimal? Log(decimal? newBase, decimal? value)
		{
			return value == null || newBase == null ? null : (decimal?)Math.Log((double)value.Value, (double)newBase.Value);
		}

		[Expression(PN.Access, "{0} ^ {1}", Precedence = Precedence.Multiplicative)]
		[Function]
		public static double?  Power(double? x, double? y)
		{
			return x == null || y == null ? null : (double?)Math.Pow(x.Value, y.Value);
		}

		[Function]
		public static decimal? RoundToEven(decimal? value)
		{
			return value == null ? null : (decimal?)Math.Round(value.Value, MidpointRounding.ToEven);
		}

		[Function]
		public static double? RoundToEven(double? value)
		{
			return value == null ? null : (double?) Math.Round(value.Value, MidpointRounding.ToEven);
		}

		[Function] public static decimal? Round(decimal? value) { return Round(value, 0); }
		[Function] public static double?  Round(double?  value) { return Round(value, 0); }

		[Function]
		public static decimal? Round(decimal? value, int? precision)
		{
			return value == null || precision == null? null : (decimal?)Math.Round(value.Value, precision.Value, MidpointRounding.AwayFromZero);
		}

		[Function]
		public static double? Round(double? value, int? precision)
		{
			return value == null || precision == null? null : (double?) Math.Round(value.Value, precision.Value, MidpointRounding.AwayFromZero);
		}

		[Function]
		public static decimal? RoundToEven(decimal? value, int? precision)
		{
			return value == null || precision == null? null : (decimal?)Math.Round(value.Value, precision.Value, MidpointRounding.ToEven);
		}

		[Function]
		public static double? RoundToEven(double?  value, int? precision)
		{
			return value == null || precision == null? null : (double?) Math.Round(value.Value, precision.Value, MidpointRounding.ToEven);
		}

		[Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(decimal? value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(double?  value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(short?   value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(int?     value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(long?    value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[CLSCompliant(false)]
		[Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(sbyte?   value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(float?   value) { return value == null ? null : (int?)Math.Sign(value.Value); }

		[Function] public static double?  Sin     (double?  value) { return value == null ? null : (double?)Math.Sin (value.Value); }
		[Function] public static double?  Sinh    (double?  value) { return value == null ? null : (double?)Math.Sinh(value.Value); }
		[Function(PN.Access, "Sqr")]
		[Function] public static double?  Sqrt    (double?  value) { return value == null ? null : (double?)Math.Sqrt(value.Value); }
		[Function] public static double?  Tan     (double?  value) { return value == null ? null : (double?)Math.Tan (value.Value); }
		[Function] public static double?  Tanh    (double?  value) { return value == null ? null : (double?)Math.Tanh(value.Value); }

		[Expression(PN.SqlServer,  "Round({0}, 0, 1)")]
		[Expression(PN.DB2,        "Truncate({0}, 0)")]
		[Expression(PN.Informix,   "Trunc({0}, 0)")]
		[Expression(PN.Oracle,     "Trunc({0}, 0)")]
		[Expression(PN.Firebird,   "Trunc({0}, 0)")]
		[Expression(PN.PostgreSQL, "Trunc({0}, 0)")]
		[Expression(PN.MySql,      "Truncate({0}, 0)")]
		[Expression(PN.SqlCe,      "Round({0}, 0, 1)")]
		[Expression(PN.SapHana,    "Round({0}, 0, ROUND_DOWN)")]
		[Function]
		public static decimal? Truncate(decimal? value)
		{
			return value == null ? null : (decimal?)decimal.Truncate(value.Value);
		}

		[Expression(PN.SqlServer,  "Round({0}, 0, 1)")]
		[Expression(PN.DB2,        "Truncate({0}, 0)")]
		[Expression(PN.Informix,   "Trunc({0}, 0)")]
		[Expression(PN.Oracle,     "Trunc({0}, 0)")]
		[Expression(PN.Firebird,   "Trunc({0}, 0)")]
		[Expression(PN.PostgreSQL, "Trunc({0}, 0)")]
		[Expression(PN.MySql,      "Truncate({0}, 0)")]
		[Expression(PN.SqlCe,      "Round({0}, 0, 1)")]
		[Expression(PN.SapHana,    "Round({0}, 0, ROUND_DOWN)")]
		[Function]
		public static double? Truncate(double? value)
		{
			return value == null ? null : (double?) Math.Truncate(value.Value);
		}

		#endregion
	}
}
