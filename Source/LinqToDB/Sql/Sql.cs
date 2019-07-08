using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

using PN = LinqToDB.ProviderName;

// ReSharper disable CheckNamespace
// ReSharper disable RedundantNameQualifier

namespace LinqToDB
{
	using Extensions;
	using Expressions;
	using Linq;
	using SqlQuery;

	[PublicAPI]
	public static partial class Sql
	{
		#region Common Functions

		/// <summary>
		/// Generates '*'.
		/// </summary>
		/// <returns></returns>
		[Sql.Expression("*", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Primary)]
		public static object[] AllColumns()
		{
			throw new LinqException("'AllColumns' is only server-side method.");
		}

		/// <summary>
		/// Enforces generating SQL even if an expression can be calculated locally.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj">Expression to generate SQL.</param>
		/// <returns>Returns 'obj'.</returns>
		[CLSCompliant(false)]
		[Sql.Expression("{0}", 0, ServerSideOnly = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T AsSql<T>(T obj)
		{
			return obj;
		}

		[CLSCompliant(false)]
		[Sql.Expression("{0}", 0, ServerSideOnly = true, InlineParameters = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T ToSql<T>(T obj)
		{
			return obj;
		}

		[Sql.Extension("{array, ', '}", ServerSideOnly = true)]
		internal static T[] Spread<T>([ExprParameter] T[] array)
		{
			throw new InvalidOperationException();
		}

		[CLSCompliant(false)]
		[Sql.Expression("{0}", 0, CanBeNull = true)]
		public static T AsNullable<T>(T value)
		{
			return value;
		}

		[CLSCompliant(false)]
		[Sql.Expression("{0}", 0, CanBeNull = false)]
		public static T AsNotNull<T>(T value)
		{
			return value;
		}

		[CLSCompliant(false)]
		[Sql.Expression("{0}", 0, CanBeNull = false)]
		public static T AsNotNullable<T>(T value)
		{
			return value;
		}

		[CLSCompliant(false)]
		[Sql.Expression("{0}", 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T? ToNullable<T>(T value)
			where T : struct
		{
			return value;
		}

		[Obsolete("Use ToNotNullable instead.")]
		[CLSCompliant(false)]
		[Sql.Expression("{0}", 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T ConvertNullable<T>(T? value)
			where T : struct
		{
			return value ?? default;
		}

		[CLSCompliant(false)]
		[Sql.Expression("{0}", 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T ToNotNull<T>(T? value)
			where T : struct
		{
			return value ?? default;
		}

		[CLSCompliant(false)]
		[Sql.Expression("{0}", 0, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T ToNotNullable<T>(T? value)
			where T : struct
		{
			return value ?? default;
		}

		[Sql.Expression("{0} BETWEEN {1} AND {2}", PreferServerSide = true, IsPredicate = true)]
		public static bool Between<T>(this T value, T low, T high)
			where T : IComparable
		{
			return value != null && value.CompareTo(low) >= 0 && value.CompareTo(high) <= 0;
		}

		[Sql.Expression("{0} BETWEEN {1} AND {2}", PreferServerSide = true, IsPredicate = true)]
		public static bool Between<T>(this T? value, T? low, T? high)
			where T : struct, IComparable
		{
			return value != null && value.Value.CompareTo(low) >= 0 && value.Value.CompareTo(high) <= 0;
		}

		[Sql.Expression("{0} NOT BETWEEN {1} AND {2}", PreferServerSide = true, IsPredicate = true)]
		public static bool NotBetween<T>(this T value, T low, T high)
			where T : IComparable
		{
			return value != null && (value.CompareTo(low) < 0 || value.CompareTo(high) > 0);
		}

		[Sql.Expression("{0} NOT BETWEEN {1} AND {2}", PreferServerSide = true, IsPredicate = true)]
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
		public static T Property<T>(object entity, [SqlQueryDependent] string propertyName)
		{
			throw new LinqException("'Property' is only server-side method.");
		}

		#endregion

		#region NoConvert

		[Sql.Function("$Convert_Remover$", ServerSideOnly = true)]
		static TR ConvertRemover<T, TR>(T input)
		{
			throw new NotImplementedException();
		}

		class NoConvertBuilder : Sql.IExtensionCallBuilder
		{
			private static readonly MethodInfo _method = MethodHelper.GetMethodInfo(ConvertRemover<int, int>, 0).GetGenericMethodDefinition();

			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var expr    = builder.Arguments[0];
				var newExpr = expr.Transform(e =>
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
				sqlExpr     = new QueryVisitor().Convert(sqlExpr, e =>
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

		[Sql.Extension("", BuilderType = typeof(NoConvertBuilder), ServerSideOnly = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static T NoConvert<T>(T expr)
		{
			return expr;
		}

		#endregion

		#region Guid Functions

		[Sql.Function  (PN.Oracle,   "Sys_Guid", ServerSideOnly=true, CanBeNull = false)]
		[Sql.Function  (PN.Firebird, "Gen_Uuid", ServerSideOnly=true, CanBeNull = false)]
		[Sql.Function  (PN.MySql,    "Uuid",     ServerSideOnly=true, CanBeNull = false)]
		[Sql.Expression(PN.Sybase,   "NewID(1)", ServerSideOnly=true, CanBeNull = false)]
		[Sql.Expression(PN.SapHana,  "SYSUUID",  ServerSideOnly=true, CanBeNull = false)]
		[Sql.Function  (             "NewID",    ServerSideOnly=true, CanBeNull = false)]
		public static Guid NewGuid()
		{
			return Guid.NewGuid();
		}

		#endregion

		#region Convert Functions

		[CLSCompliant(false)]
		[Sql.Function("Convert", 0, 1, ServerSideOnly = true, IsNullable = IsNullableType.SameAsSecondParameter)]
		public static TTo Convert<TTo,TFrom>(TTo to, TFrom from)
		{
			var dt = Common.ConvertTo<TTo>.From(from);
			return dt;
		}

		[CLSCompliant(false)]
		[Sql.Function("Convert", 0, 1, 2, ServerSideOnly = true, IsNullable = IsNullableType.SameAsSecondParameter)]
		public static TTo Convert<TTo, TFrom>(TTo to, TFrom from, int format)
		{
			var dt = Common.ConvertTo<TTo>.From(from);
			return dt;
		}

		[CLSCompliant(false)]
		[Sql.Function("Convert", 0, 1, IsNullable = IsNullableType.SameAsSecondParameter)]
		public static TTo Convert2<TTo,TFrom>(TTo to, TFrom from)
		{
			return Common.ConvertTo<TTo>.From(from);
		}

		[CLSCompliant(false)]
		[Sql.Function("$Convert$", 1, 2, 0)]
		public static TTo Convert<TTo,TFrom>(TFrom obj)
		{
			return Common.ConvertTo<TTo>.From(obj);
		}

		public static class ConvertTo<TTo>
		{
			[CLSCompliant(false)]
			[Sql.Function("$Convert$", 1, 2, 0)]
			public static TTo From<TFrom>(TFrom obj)
			{
				return Common.ConvertTo<TTo>.From(obj);
			}
		}

		[Sql.Expression("{0}", IsNullable = IsNullableType.IfAnyParameterNullable)]
		public static TimeSpan? DateToTime(DateTime? date)
		{
			return date == null ? null : (TimeSpan?)new TimeSpan(date.Value.Ticks);
		}

		[Sql.Property(PN.Informix,      "Boolean",        ServerSideOnly=true)]
		[Sql.Property(PN.PostgreSQL,    "Boolean",        ServerSideOnly=true)]
		[Sql.Property(PN.MySql,         "Boolean",        ServerSideOnly=true)]
		[Sql.Property(PN.SQLite,        "Boolean",        ServerSideOnly=true)]
		[Sql.Property(PN.SapHana,       "TinyInt",        ServerSideOnly=true)]
		[Sql.Property(                  "Bit",            ServerSideOnly=true)] public static Boolean        Bit                               { get { return false; } }

		[Sql.Property(PN.Oracle,        "Number(19)",     ServerSideOnly=true)]
		[Sql.Property(                  "BigInt",         ServerSideOnly=true)] public static Int64          BigInt                            { get { return 0; } }

		[Sql.Property(PN.MySql,         "Signed",         ServerSideOnly=true)]
		[Sql.Property(                  "Int",            ServerSideOnly=true)] public static Int32          Int                               { get { return 0; } }

		[Sql.Property(PN.MySql,         "Signed",         ServerSideOnly=true)]
		[Sql.Property(                  "SmallInt",       ServerSideOnly=true)] public static Int16          SmallInt                          { get { return 0; } }

		[Sql.Property(PN.DB2,           "SmallInt",       ServerSideOnly=true)]
		[Sql.Property(PN.Informix,      "SmallInt",       ServerSideOnly=true)]
		[Sql.Property(PN.Oracle,        "Number(3)",      ServerSideOnly=true)]
		[Sql.Property(PN.DB2,           "SmallInt",       ServerSideOnly=true)]
		[Sql.Property(PN.Firebird,      "SmallInt",       ServerSideOnly=true)]
		[Sql.Property(PN.PostgreSQL,    "SmallInt",       ServerSideOnly=true)]
		[Sql.Property(PN.MySql,         "Unsigned",       ServerSideOnly=true)]
		[Sql.Property(                  "TinyInt",        ServerSideOnly=true)] public static Byte           TinyInt                           { get { return 0; } }

		[Sql.Property(                  "Decimal",        ServerSideOnly=true)] public static Decimal DefaultDecimal                           { get { return 0; } }
		[Sql.Expression(PN.SapHana,     "Decimal({0},4)", ServerSideOnly=true)]
		[Sql.Function(                                    ServerSideOnly=true)] public static Decimal        Decimal(int precision)            {       return 0;   }
		[Sql.Function(                                    ServerSideOnly=true)] public static Decimal        Decimal(int precision, int scale) {       return 0;   }

		[Sql.Property(PN.Oracle,        "Number(19,4)",   ServerSideOnly=true)]
		[Sql.Property(PN.Firebird,      "Decimal(18,4)",  ServerSideOnly=true)]
		[Sql.Property(PN.PostgreSQL,    "Decimal(19,4)",  ServerSideOnly=true)]
		[Sql.Property(PN.MySql,         "Decimal(19,4)",  ServerSideOnly=true)]
		[Sql.Property(PN.SapHana,       "Decimal(19,4)",  ServerSideOnly=true)]
		[Sql.Property(                  "Money",          ServerSideOnly=true)] public static Decimal        Money                             { get { return 0; } }

		[Sql.Property(PN.Informix,      "Decimal(10,4)",  ServerSideOnly=true)]
		[Sql.Property(PN.Oracle,        "Number(10,4)",   ServerSideOnly=true)]
		[Sql.Property(PN.Firebird,      "Decimal(10,4)",  ServerSideOnly=true)]
		[Sql.Property(PN.PostgreSQL,    "Decimal(10,4)",  ServerSideOnly=true)]
		[Sql.Property(PN.MySql,         "Decimal(10,4)",  ServerSideOnly=true)]
		[Sql.Property(PN.SqlCe,         "Decimal(10,4)",  ServerSideOnly=true)]
		[Sql.Property(PN.SapHana,       "Decimal(10,4)",  ServerSideOnly=true)]
		[Sql.Property(                  "SmallMoney",     ServerSideOnly=true)] public static Decimal        SmallMoney                        { get { return 0; } }

		[Sql.Property(PN.MySql,         "Decimal(29,10)", ServerSideOnly=true)]
		[Sql.Property(PN.SapHana,       "Double",         ServerSideOnly=true)]
		[Sql.Property(                  "Float",          ServerSideOnly=true)] public static Double         Float                             { get { return 0; } }

		[Sql.Property(PN.MySql,         "Decimal(29,10)", ServerSideOnly=true)]
		[Sql.Property(                  "Real",           ServerSideOnly=true)] public static Single         Real                              { get { return 0; } }

		[Sql.Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true)]
		[Sql.Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true)]
		[Sql.Property(PN.SapHana,       "TimeStamp",      ServerSideOnly=true)]
		[Sql.Property(                  "DateTime",       ServerSideOnly=true)] public static DateTime       DateTime                          { get { return DateTime.Now; } }

		[Sql.Property(PN.SqlServer2000, "DateTime",       ServerSideOnly=true)]
		[Sql.Property(PN.SqlServer2005, "DateTime",       ServerSideOnly=true)]
		[Sql.Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true)]
		[Sql.Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true)]
		[Sql.Property(PN.MySql,         "DateTime",       ServerSideOnly=true)]
		[Sql.Property(PN.SqlCe,         "DateTime",       ServerSideOnly=true)]
		[Sql.Property(PN.Sybase,        "DateTime",       ServerSideOnly=true)]
		[Sql.Property(PN.SapHana,       "TimeStamp",      ServerSideOnly=true)]
		[Sql.Property(                  "DateTime2",      ServerSideOnly=true)] public static DateTime       DateTime2                         { get { return DateTime.Now; } }

		[Sql.Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true)]
		[Sql.Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true)]
		[Sql.Property(PN.MySql,         "DateTime",       ServerSideOnly=true)]
		[Sql.Property(PN.SqlCe,         "DateTime",       ServerSideOnly=true)]
		[Sql.Property(PN.SapHana,       "SecondDate",     ServerSideOnly=true)]
		[Sql.Property(                  "SmallDateTime",  ServerSideOnly=true)] public static DateTime       SmallDateTime                     { get { return DateTime.Now; } }

		[Sql.Property(PN.SqlServer2000, "Datetime",       ServerSideOnly=true)]
		[Sql.Property(PN.SqlServer2005, "Datetime",       ServerSideOnly=true)]
		[Sql.Property(PN.SqlCe,         "Datetime",       ServerSideOnly=true)]
		[Sql.Property(                  "Date",           ServerSideOnly=true)] public static DateTime       Date                              { get { return DateTime.Now; } }

		[Sql.Property(                  "Time",           ServerSideOnly=true)] public static DateTime       Time                              { get { return DateTime.Now; } }

		[Sql.Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true)]
		[Sql.Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true)]
		[Sql.Property(PN.SqlServer2017, "DateTimeOffset", ServerSideOnly=true)]
		[Sql.Property(PN.SqlServer2012, "DateTimeOffset", ServerSideOnly=true)]
		[Sql.Property(PN.SqlServer2008, "DateTimeOffset", ServerSideOnly=true)]
		[Sql.Property(PN.SapHana,       "TimeStamp",      ServerSideOnly=true)]
		[Sql.Property(                  "DateTime",       ServerSideOnly=true)] public static DateTimeOffset DateTimeOffset                    { get { return DateTimeOffset.Now; } }

		[Sql.Function(PN.SqlCe,         "NChar",          ServerSideOnly=true)]
		[Sql.Function(                                    ServerSideOnly=true)] public static String         Char(int length)                  {       return ""; }

		[Sql.Property(PN.SqlCe,         "NChar",          ServerSideOnly=true)]
		[Sql.Property(                  "Char",           ServerSideOnly=true)] public static String  DefaultChar                              { get { return ""; } }

		[Sql.Function(PN.MySql,         "Char",           ServerSideOnly=true)]
		[Sql.Function(PN.SqlCe,         "NVarChar",       ServerSideOnly=true)]
		[Sql.Function(                                    ServerSideOnly=true)] public static String         VarChar(int length)               {       return ""; }

		[Sql.Property(PN.MySql,         "Char",           ServerSideOnly=true)]
		[Sql.Property(PN.SqlCe,         "NVarChar",       ServerSideOnly=true)]
		[Sql.Property(                  "VarChar",        ServerSideOnly=true)] public static String  DefaultVarChar                           { get { return ""; } }

		[Sql.Function(PN.DB2,           "Char",           ServerSideOnly=true)]
		[Sql.Function(                                    ServerSideOnly=true)] public static String         NChar(int length)                 {       return ""; }

		[Sql.Property(PN.DB2,           "Char",           ServerSideOnly=true)]
		[Sql.Property(                  "NChar",          ServerSideOnly=true)] public static String  DefaultNChar                             { get { return ""; } }

		[Sql.Function(PN.DB2,           "Char",           ServerSideOnly=true)]
		[Sql.Function(PN.Oracle,        "VarChar2",       ServerSideOnly=true)]
		[Sql.Function(PN.Firebird,      "VarChar",        ServerSideOnly=true)]
		[Sql.Function(PN.PostgreSQL,    "VarChar",        ServerSideOnly=true)]
		[Sql.Function(PN.MySql,         "Char",           ServerSideOnly=true)]
		[Sql.Function(                                    ServerSideOnly=true)] public static String         NVarChar(int length)              {       return ""; }

		[Sql.Property(PN.DB2,           "Char",           ServerSideOnly=true)]
		[Sql.Property(PN.Oracle,        "VarChar2",       ServerSideOnly=true)]
		[Sql.Property(PN.Firebird,      "VarChar",        ServerSideOnly=true)]
		[Sql.Property(PN.PostgreSQL,    "VarChar",        ServerSideOnly=true)]
		[Sql.Property(PN.MySql,         "Char",           ServerSideOnly=true)]
		[Sql.Property(                  "NVarChar",       ServerSideOnly=true)] public static String  DefaultNVarChar                          { get { return ""; } }

		#endregion

		#region String Functions

		[Sql.Function  (                                                   PreferServerSide = true)]
		[Sql.Function  (PN.Access,    "Len",                               PreferServerSide = true)]
		[Sql.Function  (PN.Firebird,  "Char_Length",                       PreferServerSide = true)]
		[Sql.Function  (PN.SqlServer, "Len",                               PreferServerSide = true)]
		[Sql.Function  (PN.SqlCe,     "Len",                               PreferServerSide = true)]
		[Sql.Function  (PN.Sybase,    "Len",                               PreferServerSide = true)]
		[Sql.Function  (PN.MySql,     "Char_Length",                       PreferServerSide = true)]
		[Sql.Expression(PN.DB2LUW,    "CHARACTER_LENGTH({0},CODEUNITS32)", PreferServerSide = true)]
		public static int? Length(string str)
		{
			return str?.Length;
		}

		[Sql.Function  (                                                PreferServerSide = true)]
		[Sql.Function  (PN.Access,   "Mid",                             PreferServerSide = true)]
		[Sql.Function  (PN.DB2,      "Substr",                          PreferServerSide = true)]
		[Sql.Function  (PN.Informix, "Substr",                          PreferServerSide = true)]
		[Sql.Function  (PN.Oracle,   "Substr",                          PreferServerSide = true)]
		[Sql.Function  (PN.SQLite,   "Substr",                          PreferServerSide = true)]
		[Sql.Expression(PN.Firebird, "Substring({0} from {1} for {2})", PreferServerSide = true)]
		[Sql.Function  (PN.SapHana,  "Substring",                       PreferServerSide = true)]
		public static string Substring(string str, int? startIndex, int? length)
		{
			return str == null || startIndex == null || length == null ? null : str.Substring(startIndex.Value - 1, length.Value);
		}

		[Sql.Function(ServerSideOnly = true)]
		public static bool Like(string matchExpression, string pattern)
		{
#if NETSTANDARD1_6 || NETSTANDARD2_0
			throw new InvalidOperationException();
#else
			return matchExpression != null && pattern != null &&
				System.Data.Linq.SqlClient.SqlMethods.Like(matchExpression, pattern);
#endif
		}

		[Sql.Function(ServerSideOnly = true)]
		public static bool Like(string matchExpression, string pattern, char? escapeCharacter)
		{
#if NETSTANDARD1_6 || NETSTANDARD2_0
			throw new InvalidOperationException();
#else
			return matchExpression != null && pattern != null && escapeCharacter != null &&
				System.Data.Linq.SqlClient.SqlMethods.Like(matchExpression, pattern, escapeCharacter.Value);
#endif
		}

		[CLSCompliant(false)]
		[Sql.Function]
		[Sql.Function(PN.DB2,      "Locate")]
		[Sql.Function(PN.MySql,    "Locate")]
		[Sql.Function(PN.SapHana,  "Locate", 1, 0)]
		[Sql.Function(PN.Firebird, "Position")]
		public static int? CharIndex(string value, string str)
		{
			if (str == null || value == null)
				return null;

			return str.IndexOf(value) + 1;
		}

		[Sql.Function]
		[Sql.Function  (PN.DB2,      "Locate")]
		[Sql.Function  (PN.MySql,    "Locate")]
		[Sql.Function  (PN.Firebird, "Position")]
		[Sql.Expression(PN.SapHana,  "Locate(Substring({1},{2} + 1),{0}) + {2}")]
		public static int? CharIndex(string value, string str, int? startLocation)
		{
			if (str == null || value == null || startLocation == null)
				return null;

			return str.IndexOf(value, startLocation.Value - 1) + 1;
		}

		[Sql.Function]
		[Sql.Function(PN.DB2,     "Locate")]
		[Sql.Function(PN.MySql,   "Locate")]
		[Sql.Function(PN.SapHana, "Locate")]
		public static int? CharIndex(char? value, string str)
		{
			if (value == null || str == null)
				return null;

			return str.IndexOf(value.Value) + 1;
		}

		[Sql.Function]
		[Sql.Function(PN.DB2,     "Locate")]
		[Sql.Function(PN.MySql,   "Locate")]
		[Sql.Function(PN.SapHana, "Locate")]
		public static int? CharIndex(char? value, string str, int? startLocation)
		{
			if (str == null || value == null || startLocation == null)
				return null;

			return str.IndexOf(value.Value, startLocation.Value - 1) + 1;
		}

		[Sql.Function]
		public static string Reverse(string str)
		{
			if (string.IsNullOrEmpty(str))
				return str;

			var chars = str.ToCharArray();
			Array.Reverse(chars);
			return new string(chars);
		}

		[Sql.Function(                      PreferServerSide = true)]
		[Sql.Function(PN.SQLite, "LeftStr", PreferServerSide = true)]
		public static string Left(string str, int? length)
		{
			return length == null || str == null || str.Length < length? null: str.Substring(1, length.Value);
		}

		[Sql.Function(                       PreferServerSide = true)]
		[Sql.Function(PN.SQLite, "RightStr", PreferServerSide = true)]
		public static string Right(string str, int? length)
		{
			return length == null || str == null || str.Length < length?
				null :
				str.Substring(str.Length - length.Value);
		}

		[Sql.Function]
		public static string Stuff(string str, int? startLocation, int? length, string value)
		{
			return str == null || value == null || startLocation == null || length == null ?
				null :
				str.Remove(startLocation.Value - 1, length.Value).Insert(startLocation.Value - 1, value);
		}

		[Sql.Function(ServerSideOnly = true)]
		public static string Stuff(IEnumerable<string> characterExpression, int? start, int? length, string replaceWithExpression)
		{
			throw new NotImplementedException();
		}

		[Sql.Function]
		[Sql.Expression(ProviderName.SapHana, "Lpad('',{0},' ')")]
		public static string Space(int? length)
		{
			return length == null ? null : "".PadRight(length.Value);
		}

		[Sql.Function(Name = "LPad")]
		public static string PadLeft(string str, int? totalWidth, char? paddingChar)
		{
			return str == null || totalWidth == null || paddingChar == null ?
				null :
				str.PadLeft(totalWidth.Value, paddingChar.Value);
		}

		[Sql.Function(Name = "RPad")]
		public static string PadRight(string str, int? totalWidth, char? paddingChar)
		{
			return str == null || totalWidth == null || paddingChar == null ?
				null :
				str.PadRight(totalWidth.Value, paddingChar.Value);
		}

		[Sql.Function]
		[Sql.Function(PN.Sybase, "Str_Replace")]
		public static string Replace(string str, string oldValue, string newValue)
		{
			return str == null || oldValue == null || newValue == null ?
				null :
				str.Replace(oldValue, newValue);
		}

		[Sql.Function]
		[Sql.Function(PN.Sybase, "Str_Replace")]
		public static string Replace(string str, char? oldValue, char? newValue)
		{
			return str == null || oldValue == null || newValue == null ?
				null :
				str.Replace(oldValue.Value, newValue.Value);
		}

		[Sql.Function]
		public static string Trim(string str)
		{
			return str?.Trim();
		}

		[Sql.Function("LTrim")]
		public static string TrimLeft(string str)
		{
			return str?.TrimStart();
		}

		[Sql.Function("RTrim")]
		public static string TrimRight(string str)
		{
			return str?.TrimEnd();
		}

		[Sql.Function]
		[Sql.Expression(PN.DB2, "Strip({0}, B, {1})")]
		public static string Trim(string str, char? ch)
		{
			return str == null || ch == null ? null : str.Trim(ch.Value);
		}

		[Sql.Expression(PN.DB2, "Strip({0}, L, {1})")]
		[Sql.Function  (        "LTrim")]
		public static string TrimLeft(string str, char? ch)
		{
			return str == null || ch == null ? null : str.TrimStart(ch.Value);
		}

		[Sql.Expression(PN.DB2, "Strip({0}, T, {1})")]
		[Sql.Function  (        "RTrim")]
		public static string TrimRight(string str, char? ch)
		{
			return str == null || ch == null ? null : str.TrimEnd(ch.Value);
		}

		[Sql.Function(                    ServerSideOnly = true)]
		[Sql.Function(PN.Access, "LCase", ServerSideOnly = true)]
		public static string Lower(string str)
		{
			return str?.ToLower();
		}

		[Sql.Function(                    ServerSideOnly = true)]
		[Sql.Function(PN.Access, "UCase", ServerSideOnly = true)]
		public static string Upper(string str)
		{
			return str?.ToUpper();
		}

		class ConcatAttribute : Sql.ExpressionAttribute
		{
			public ConcatAttribute() : base("")
			{
			}

			public override ISqlExpression GetExpression(MemberInfo member, params ISqlExpression[] args)
			{
				var arr = new ISqlExpression[args.Length];

				for (var i = 0; i < args.Length; i++)
				{
					var arg = args[i];

					if (arg.SystemType == typeof(string))
					{
						arr[i] = arg;
					}
					else
					{
						var len = arg.SystemType == null || arg.SystemType == typeof(object) ?
							100 :
							SqlDataType.GetMaxDisplaySize(SqlDataType.GetDataType(arg.SystemType).DataType);

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

		[ConcatAttribute]
		public static string Concat(params object[] args)
		{
			return string.Concat(args);
		}

		[ConcatAttribute]
		public static string Concat(params string[] args)
		{
			return string.Concat(args);
		}

		#endregion

		#region Binary Functions

		[Sql.Function(                              PreferServerSide = true)]
		[Sql.Function(PN.Access,    "Len",          PreferServerSide = true)]
		[Sql.Function(PN.Firebird,  "Octet_Length", PreferServerSide = true)]
		[Sql.Function(PN.SqlServer, "DataLength",   PreferServerSide = true)]
		[Sql.Function(PN.SqlCe,     "DataLength",   PreferServerSide = true)]
		[Sql.Function(PN.Sybase,    "DataLength",   PreferServerSide = true)]
		public static int? Length(Binary value)
		{
			return value == null ? null : (int?)value.Length;
		}

		#endregion

		#region DateTime Functions

		[Sql.Property(             "CURRENT_TIMESTAMP", CanBeNull = false)]
		[Sql.Property(PN.Informix, "CURRENT",           CanBeNull = false)]
		[Sql.Property(PN.Access,   "Now",               CanBeNull = false)]
		public static DateTime GetDate()
		{
			return DateTime.Now;
		}

		[Sql.Property(             "CURRENT_TIMESTAMP", ServerSideOnly = true, CanBeNull = false)]
		[Sql.Property(PN.Informix, "CURRENT",           ServerSideOnly = true, CanBeNull = false)]
		[Sql.Property(PN.Access,   "Now",               ServerSideOnly = true, CanBeNull = false)]
		[Sql.Function(PN.SqlCe,    "GetDate",           ServerSideOnly = true, CanBeNull = false)]
		[Sql.Function(PN.Sybase,   "GetDate",           ServerSideOnly = true, CanBeNull = false)]
		public static DateTime CurrentTimestamp => throw new LinqException("'CurrentTimestamp' is server side only property.");

		[Sql.Function  (PN.SqlServer , "SYSUTCDATETIME"                      , ServerSideOnly = true, CanBeNull = false)]
		[Sql.Function  (PN.Sybase    , "GETUTCDATE"                          , ServerSideOnly = true, CanBeNull = false)]
		[Sql.Expression(PN.SQLite    , "DATETIME('now')"                     , ServerSideOnly = true, CanBeNull = false)]
		[Sql.Function  (PN.MySql     , "UTC_TIMESTAMP"                       , ServerSideOnly = true, CanBeNull = false)]
		[Sql.Expression(PN.PostgreSQL, "timezone('UTC', now())"              , ServerSideOnly = true, CanBeNull = false)]
		[Sql.Expression(PN.DB2       , "CURRENT TIMESTAMP - CURRENT TIMEZONE", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Subtraction)]
		[Sql.Expression(PN.Oracle    , "SYS_EXTRACT_UTC(SYSTIMESTAMP)"       , ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Additive)]
		[Sql.Property  (PN.SapHana   , "CURRENT_UTCTIMESTAMP"                , ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Additive)]
		[Sql.Expression(PN.Informix  , "datetime(1970-01-01 00:00:00) year to second + (dbinfo('utc_current')/86400)::int::char(9)::interval day(9) to day + (mod(dbinfo('utc_current'), 86400))::char(5)::interval second(5) to second", ServerSideOnly = true, CanBeNull = false, Precedence = Precedence.Additive)]
		public static DateTime CurrentTimestampUtc => DateTime.UtcNow;

		[Sql.Property(             "CURRENT_TIMESTAMP", CanBeNull = false)]
		[Sql.Property(PN.Informix, "CURRENT",           CanBeNull = false)]
		[Sql.Property(PN.Access,   "Now",               CanBeNull = false)]
		[Sql.Function(PN.SqlCe,    "GetDate",           CanBeNull = false)]
		[Sql.Function(PN.Sybase,   "GetDate",           CanBeNull = false)]
		public static DateTime CurrentTimestamp2 => DateTime.Now;

		[Sql.Function]
		public static DateTime? ToDate(int? year, int? month, int? day, int? hour, int? minute, int? second, int? millisecond)
		{
			return year == null || month == null || day == null || hour == null || minute == null || second == null || millisecond == null ?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value, hour.Value, minute.Value, second.Value, millisecond.Value);
		}

		[Sql.Function]
		public static DateTime? ToDate(int? year, int? month, int? day, int? hour, int? minute, int? second)
		{
			return year == null || month == null || day == null || hour == null || minute == null || second == null ?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value, hour.Value, minute.Value, second.Value);
		}

		[Sql.Function]
		public static DateTime? ToDate(int? year, int? month, int? day)
		{
			return year == null || month == null || day == null ?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value);
		}

		[Sql.Property("@@DATEFIRST", CanBeNull = false)]
		public static int DateFirst => 7;

		[Sql.Function]
		public static DateTime? MakeDateTime(int? year, int? month, int? day)
		{
			return year == null || month == null || day == null ?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value);
		}

		[Sql.Function]
		public static DateTime? MakeDateTime(int? year, int? month, int? day, int? hour, int? minute, int? second)
		{
			return year == null || month == null || day == null || hour == null || minute == null || second == null ?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value, hour.Value, minute.Value, second.Value);
		}

		#endregion

		#region Math Functions

		[Sql.Function] public static Decimal? Abs    (Decimal? value) { return value == null ? null : (Decimal?)Math.Abs    (value.Value); }
		[Sql.Function] public static Double?  Abs    (Double?  value) { return value == null ? null : (Double?) Math.Abs    (value.Value); }
		[Sql.Function] public static Int16?   Abs    (Int16?   value) { return value == null ? null : (Int16?)  Math.Abs    (value.Value); }
		[Sql.Function] public static Int32?   Abs    (Int32?   value) { return value == null ? null : (Int32?)  Math.Abs    (value.Value); }
		[Sql.Function] public static Int64?   Abs    (Int64?   value) { return value == null ? null : (Int64?)  Math.Abs    (value.Value); }
		[CLSCompliant(false)]
		[Sql.Function] public static SByte?   Abs    (SByte?   value) { return value == null ? null : (SByte?)  Math.Abs    (value.Value); }
		[Sql.Function] public static Single?  Abs    (Single?  value) { return value == null ? null : (Single?) Math.Abs    (value.Value); }

		[Sql.Function] public static Double?  Acos   (Double?  value) { return value == null ? null : (Double?) Math.Acos   (value.Value); }
		[Sql.Function] public static Double?  Asin   (Double?  value) { return value == null ? null : (Double?) Math.Asin   (value.Value); }

		[Sql.Function(PN.Access, "Atn")]
		[Sql.Function] public static Double?  Atan   (Double?  value) { return value == null ? null : (Double?) Math.Atan   (value.Value); }

		[CLSCompliant(false)]
		[Sql.Function(PN.SqlServer, "Atn2")]
		[Sql.Function(PN.DB2,       "Atan2", 1, 0)]
		[Sql.Function(PN.SqlCe,     "Atn2")]
		[Sql.Function(PN.Sybase,    "Atn2")]
		[Sql.Function] public static Double?  Atan2  (Double? x, Double? y) { return x == null || y == null? null : (Double?)Math.Atan2(x.Value, y.Value); }

		[Sql.Function(PN.Informix, "Ceil")]
		[Sql.Function(PN.Oracle,   "Ceil")]
		[Sql.Function(PN.SapHana,  "Ceil")]
		[Sql.Function] public static Decimal? Ceiling(Decimal? value) { return value == null ? null : (Decimal?)decimal.Ceiling(value.Value); }

		[Sql.Function(PN.Informix, "Ceil")]
		[Sql.Function(PN.Oracle,   "Ceil")]
		[Sql.Function(PN.SapHana,  "Ceil")]
		[Sql.Function] public static Double?  Ceiling(Double?  value) { return value == null ? null : (Double?)Math.Ceiling(value.Value); }

		[Sql.Function] public static Double?  Cos    (Double?  value) { return value == null ? null : (Double?)Math.Cos    (value.Value); }

		[Sql.Function] public static Double?  Cosh   (Double?  value) { return value == null ? null : (Double?)Math.Cosh   (value.Value); }

		[Sql.Function] public static Double?  Cot    (Double?  value) { return value == null ? null : (Double?)Math.Cos(value.Value) / Math.Sin(value.Value); }

		[Sql.Function] public static Decimal? Degrees(Decimal? value) { return value == null ? null : (Decimal?)(value.Value * 180m / (Decimal)Math.PI); }
		[Sql.Function] public static Double?  Degrees(Double?  value) { return value == null ? null : (Double?) (value.Value * 180 / Math.PI); }
		[Sql.Function] public static Int16?   Degrees(Int16?   value) { return value == null ? null : (Int16?)  (value.Value * 180 / Math.PI); }
		[Sql.Function] public static Int32?   Degrees(Int32?   value) { return value == null ? null : (Int32?)  (value.Value * 180 / Math.PI); }
		[Sql.Function] public static Int64?   Degrees(Int64?   value) { return value == null ? null : (Int64?)  (value.Value * 180 / Math.PI); }
		[CLSCompliant(false)]
		[Sql.Function] public static SByte?   Degrees(SByte?   value) { return value == null ? null : (SByte?)  (value.Value * 180 / Math.PI); }
		[Sql.Function] public static Single?  Degrees(Single?  value) { return value == null ? null : (Single?) (value.Value * 180 / Math.PI); }

		[Sql.Function] public static Double?  Exp    (Double?  value) { return value == null ? null : (Double?)Math.Exp    (value.Value); }

		[Sql.Function(PN.Access, "Int")]
		[Sql.Function] public static Decimal? Floor  (Decimal? value) { return value == null ? null : (Decimal?)decimal.Floor(value.Value); }

		[Sql.Function(PN.Access, "Int")]
		[Sql.Function] public static Double?  Floor  (Double?  value) { return value == null ? null : (Double?) Math.   Floor(value.Value); }

		[Sql.Function(PN.Informix,   "LogN")]
		[Sql.Function(PN.Oracle,     "Ln")]
		[Sql.Function(PN.Firebird,   "Ln")]
		[Sql.Function(PN.PostgreSQL, "Ln")]
		[Sql.Function(PN.SapHana,    "Ln")]
		[Sql.Function] public static Decimal? Log    (Decimal? value) { return value == null ? null : (Decimal?)Math.Log     ((Double)value.Value); }

		[Sql.Function(PN.Informix,   "LogN")]
		[Sql.Function(PN.Oracle,     "Ln")]
		[Sql.Function(PN.Firebird,   "Ln")]
		[Sql.Function(PN.PostgreSQL, "Ln")]
		[Sql.Function(PN.SapHana,    "Ln")]
		[Sql.Function] public static Double?  Log    (Double?  value) { return value == null ? null : (Double?) Math.Log     (value.Value); }

		[Sql.Function(PN.PostgreSQL, "Log")]
		[Sql.Expression(PN.SapHana,  "Log(10,{0})")]
		[Sql.Function] public static Double?  Log10  (Double?  value) { return value == null ? null : (Double?) Math.Log10   (value.Value); }

		[Sql.Function]
		public static double?  Log(double? newBase, double? value)
		{
			return value == null || newBase == null ? null : (Double?)Math.Log(value.Value, newBase.Value);
		}

		[Sql.Function]
		public static decimal? Log(decimal? newBase, decimal? value)
		{
			return value == null || newBase == null ? null : (decimal?)Math.Log((double)value.Value, (double)newBase.Value);
		}

		[Sql.Expression(PN.Access, "{0} ^ {1}", Precedence = Precedence.Multiplicative)]
		[Sql.Function]
		public static Double?  Power(Double? x, Double? y)
		{
			return x == null || y == null ? null : (Double?)Math.Pow(x.Value, y.Value);
		}

		[Sql.Function]
		public static Decimal? RoundToEven(Decimal? value)
		{
			return value == null ? null : (Decimal?)Math.Round(value.Value, MidpointRounding.ToEven);
		}

		[Sql.Function]
		public static Double? RoundToEven(Double? value)
		{
			return value == null ? null : (Double?) Math.Round(value.Value, MidpointRounding.ToEven);
		}

		[Sql.Function] public static Decimal? Round(Decimal? value) { return Round(value, 0); }
		[Sql.Function] public static Double?  Round(Double?  value) { return Round(value, 0); }

		[Sql.Function]
		public static Decimal? Round(Decimal? value, int? precision)
		{
			return value == null || precision == null? null : (Decimal?)Math.Round(value.Value, precision.Value, MidpointRounding.AwayFromZero);
		}

		[Sql.Function]
		public static Double? Round(Double? value, int? precision)
		{
			return value == null || precision == null? null : (Double?) Math.Round(value.Value, precision.Value, MidpointRounding.AwayFromZero);
		}

		[Sql.Function]
		public static Decimal? RoundToEven(Decimal? value, int? precision)
		{
			return value == null || precision == null? null : (Decimal?)Math.Round(value.Value, precision.Value, MidpointRounding.ToEven);
		}

		[Sql.Function]
		public static Double? RoundToEven(Double?  value, int? precision)
		{
			return value == null || precision == null? null : (Double?) Math.Round(value.Value, precision.Value, MidpointRounding.ToEven);
		}

		[Sql.Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(Decimal? value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[Sql.Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(Double?  value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[Sql.Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(Int16?   value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[Sql.Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(Int32?   value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[Sql.Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(Int64?   value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[CLSCompliant(false)]
		[Sql.Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(SByte?   value) { return value == null ? null : (int?)Math.Sign(value.Value); }
		[Sql.Function(PN.Access, "Sgn"), Sql.Function] public static int? Sign(Single?  value) { return value == null ? null : (int?)Math.Sign(value.Value); }

		[Sql.Function] public static Double?  Sin     (Double?  value) { return value == null ? null : (Double?)Math.Sin (value.Value); }
		[Sql.Function] public static Double?  Sinh    (Double?  value) { return value == null ? null : (Double?)Math.Sinh(value.Value); }
		[Sql.Function(PN.Access, "Sqr")]
		[Sql.Function] public static Double?  Sqrt    (Double?  value) { return value == null ? null : (Double?)Math.Sqrt(value.Value); }
		[Sql.Function] public static Double?  Tan     (Double?  value) { return value == null ? null : (Double?)Math.Tan (value.Value); }
		[Sql.Function] public static Double?  Tanh    (Double?  value) { return value == null ? null : (Double?)Math.Tanh(value.Value); }

		[Sql.Expression(PN.SqlServer,  "Round({0}, 0, 1)")]
		[Sql.Expression(PN.DB2,        "Truncate({0}, 0)")]
		[Sql.Expression(PN.Informix,   "Trunc({0}, 0)")]
		[Sql.Expression(PN.Oracle,     "Trunc({0}, 0)")]
		[Sql.Expression(PN.Firebird,   "Trunc({0}, 0)")]
		[Sql.Expression(PN.PostgreSQL, "Trunc({0}, 0)")]
		[Sql.Expression(PN.MySql,      "Truncate({0}, 0)")]
		[Sql.Expression(PN.SqlCe,      "Round({0}, 0, 1)")]
		[Sql.Expression(PN.SapHana,    "Round({0}, 0, ROUND_DOWN)")]
		[Sql.Function]
		public static Decimal? Truncate(Decimal? value)
		{
			return value == null ? null : (Decimal?)decimal.Truncate(value.Value);
		}

		[Sql.Expression(PN.SqlServer,  "Round({0}, 0, 1)")]
		[Sql.Expression(PN.DB2,        "Truncate({0}, 0)")]
		[Sql.Expression(PN.Informix,   "Trunc({0}, 0)")]
		[Sql.Expression(PN.Oracle,     "Trunc({0}, 0)")]
		[Sql.Expression(PN.Firebird,   "Trunc({0}, 0)")]
		[Sql.Expression(PN.PostgreSQL, "Trunc({0}, 0)")]
		[Sql.Expression(PN.MySql,      "Truncate({0}, 0)")]
		[Sql.Expression(PN.SqlCe,      "Round({0}, 0, 1)")]
		[Sql.Expression(PN.SapHana,    "Round({0}, 0, ROUND_DOWN)")]
		[Sql.Function]
		public static Double? Truncate(Double? value)
		{
			return value == null ? null : (Double?) Math.Truncate(value.Value);
		}

		#endregion

		#region Text Functions

		[Obsolete("Use Sql.Ext.SqlServer().FreeText methods")]
		[Sql.Expression("FREETEXT({0}, {1})", ServerSideOnly = true, IsPredicate = true)]
		public static bool FreeText(object table, string text)
		{
			throw new LinqException("'FreeText' is only server-side method.");
		}

		#endregion
	}
}
