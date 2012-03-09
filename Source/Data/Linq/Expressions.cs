using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

#region ReSharper disables
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable RedundantCast
// ReSharper disable PossibleInvalidOperationException
// ReSharper disable CSharpWarnings::CS0693
// ReSharper disable RedundantToStringCall
#endregion

namespace LinqToDB.Data.Linq
{
	using B = System.Boolean; //  FIXME: using B = Boolean;
	using C = System.Char;
	using S = System.String;
	using I = System.Int32;
	using O = System.Object;
	using D = System.DateTime;
	using T = System.TimeSpan;
	using F = System.Double;
	using M = System.Decimal;

	public static class Expressions
	{
		#region MapMember

		public static void MapMember(MemberInfo memberInfo, LambdaExpression expression)
		{
			MapMember("", memberInfo, expression);
		}

		public static void MapMember(string providerName, MemberInfo memberInfo, LambdaExpression expression)
		{
			Dictionary<MemberInfo,LambdaExpression> dic;

			if (!_members.TryGetValue(providerName, out dic))
				_members.Add(providerName, dic = new Dictionary<MemberInfo,LambdaExpression>());

			dic[memberInfo] = expression;
		}

		public static void MapMember(Expression<Func<object>> memberInfo, LambdaExpression expression)
		{
			MapMember("", M(memberInfo), expression);
		}

		public static void MapMember(string providerName, Expression<Func<object>> memberInfo, LambdaExpression expression)
		{
			MapMember(providerName, M(memberInfo), expression);
		}

		public static void MapMember<T>(Expression<Func<T,object>> memberInfo, LambdaExpression expression)
		{
			MapMember("", M(memberInfo), expression);
		}

		public static void MapMember<T>(string providerName, Expression<Func<T,object>> memberInfo, LambdaExpression expression)
		{
			MapMember(providerName, M(memberInfo), expression);
		}

		public static void MapMember<TR>               (string providerName, Expression<Func<TR>>                memberInfo, Expression<Func<TR>>               expression) { MapMember(providerName, ReflectionHelper.MemeberInfo(memberInfo), expression); }
		public static void MapMember<TR>               (                     Expression<Func<TR>>                memberInfo, Expression<Func<TR>>               expression) { MapMember("",           ReflectionHelper.MemeberInfo(memberInfo), expression); }
		public static void MapMember<T1,TR>            (string providerName, Expression<Func<T1,TR>>             memberInfo, Expression<Func<T1,TR>>            expression) { MapMember(providerName, ReflectionHelper.MemeberInfo(memberInfo), expression); }
		public static void MapMember<T1,TR>            (                     Expression<Func<T1,TR>>             memberInfo, Expression<Func<T1,TR>>            expression) { MapMember("",           ReflectionHelper.MemeberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,TR>         (string providerName, Expression<Func<T1,T2,TR>>          memberInfo, Expression<Func<T1,T2,TR>>         expression) { MapMember(providerName, ReflectionHelper.MemeberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,TR>         (                     Expression<Func<T1,T2,TR>>          memberInfo, Expression<Func<T1,T2,TR>>         expression) { MapMember("",           ReflectionHelper.MemeberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,TR>      (string providerName, Expression<Func<T1,T2,T3,TR>>       memberInfo, Expression<Func<T1,T2,T3,TR>>      expression) { MapMember(providerName, ReflectionHelper.MemeberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,TR>      (                     Expression<Func<T1,T2,T3,TR>>       memberInfo, Expression<Func<T1,T2,T3,TR>>      expression) { MapMember("",           ReflectionHelper.MemeberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,TR>   (string providerName, Expression<Func<T1,T2,T3,T4,TR>>    memberInfo, Expression<Func<T1,T2,T3,T4,TR>>   expression) { MapMember(providerName, ReflectionHelper.MemeberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,TR>   (                     Expression<Func<T1,T2,T3,T4,TR>>    memberInfo, Expression<Func<T1,T2,T3,T4,TR>>   expression) { MapMember("",           ReflectionHelper.MemeberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,T5,TR>(string providerName, Expression<Func<T1,T2,T3,T4,T5,TR>> memberInfo, Expression<Func<T1,T2,T3,T4,T5,TR>> expression) { MapMember(providerName, ReflectionHelper.MemeberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,T5,TR>(                     Expression<Func<T1,T2,T3,T4,T5,TR>> memberInfo, Expression<Func<T1,T2,T3,T4,T5,TR>> expression) { MapMember("",           ReflectionHelper.MemeberInfo(memberInfo), expression); }

		#endregion

		#region Public Members

		public static LambdaExpression ConvertMember(string providerName, MemberInfo mi)
		{
			Dictionary<MemberInfo,LambdaExpression> dic;
			LambdaExpression                        expr;

			if (Members.TryGetValue(providerName, out dic))
				if (dic.TryGetValue(mi, out expr))
					return expr;

			if (!Members[""].TryGetValue(mi, out expr))
			{
				if (mi is MethodInfo && mi.Name == "CompareString" && mi.DeclaringType.FullName == "Microsoft.VisualBasic.CompilerServices.Operators")
				{
					lock (_members)
					{
						if (!Members[""].TryGetValue(mi, out expr))
						{
							expr = L<S,S,B,I>((s1,s2,b) => b ? string.CompareOrdinal(s1.ToUpper(), s2.ToUpper()) : string.CompareOrdinal(s1, s2));

							_members[""].Add(mi, expr);
						}
					}
				}
			}

			return expr;
		}

		#endregion

		#region Function Mapping

		#region Helpers

		static MemberInfo M<T>(Expression<Func<T,object>> func)
		{
			return ReflectionHelper.MemeberInfo(func);
		}

		static MemberInfo M(Expression<Func<object>> func)
		{
			return ReflectionHelper.MemeberInfo(func);
		}

		static LambdaExpression L<TR>                   (Expression<Func<TR>>                   func) { return func; }
		static LambdaExpression L<T1,TR>                (Expression<Func<T1,TR>>                func) { return func; }
		static LambdaExpression L<T1,T2,TR>             (Expression<Func<T1,T2,TR>>             func) { return func; }
		static LambdaExpression L<T1,T2,T3,TR>          (Expression<Func<T1,T2,T3,TR>>          func) { return func; }
		static LambdaExpression L<T1,T2,T3,T4,TR>       (Expression<Func<T1,T2,T3,T4,TR>>       func) { return func; }
		static LambdaExpression L<T1,T2,T3,T4,T5,TR>    (Expression<Func<T1,T2,T3,T4,T5,TR>>    func) { return func; }
		static LambdaExpression L<T1,T2,T3,T4,T5,T6,TR> (Expression<Func<T1,T2,T3,T4,T5,T6,TR>> func) { return func; }

		#endregion

		static public   Dictionary<string,Dictionary<MemberInfo,LambdaExpression>>  Members { get { return _members; } }
		// FIXME:  expected System.Nullable[int]+, got int in call result
		static readonly Dictionary<string,Dictionary<MemberInfo,LambdaExpression>> _members = null;
		/* new Dictionary<string,Dictionary<MemberInfo,LambdaExpression>>
		{
			{ "", new Dictionary<MemberInfo,LambdaExpression> {

				#region String

				{ M(() => "".Length               ), L<S,I>      ( obj           => Sql.Length(obj).Value) },
				{ M(() => "".Substring  (0)       ), L<S,I,S>    ((obj,p0)       => Sql.Substring(obj, p0 + 1, obj.Length - p0)) },
				{ M(() => "".Substring  (0,0)     ), L<S,I,I,S>  ((obj,p0,p1)    => Sql.Substring(obj, p0 + 1, p1)) },
				{ M(() => "".IndexOf    ("")      ), L<S,S,I>    ((obj,p0)       => p0.Length == 0                    ? 0  : (Sql.CharIndex(p0, obj)                      .Value) - 1) },
				{ M(() => "".IndexOf    ("",0)    ), L<S,S,I,I>  ((obj,p0,p1)    => p0.Length == 0 && obj.Length > p1 ? p1 : (Sql.CharIndex(p0, obj,               p1 + 1).Value) - 1) },
				{ M(() => "".IndexOf    ("",0,0)  ), L<S,S,I,I,I>((obj,p0,p1,p2) => p0.Length == 0 && obj.Length > p1 ? p1 : (Sql.CharIndex(p0, Sql.Left(obj, p2), p1)    .Value) - 1) },
				{ M(() => "".IndexOf    (' ')     ), L<S,C,I>    ((obj,p0)       =>                                          (Sql.CharIndex(p0, obj)                      .Value) - 1) },
				{ M(() => "".IndexOf    (' ',0)   ), L<S,C,I,I>  ((obj,p0,p1)    =>                                          (Sql.CharIndex(p0, obj,               p1 + 1).Value) - 1) },
				{ M(() => "".IndexOf    (' ',0,0) ), L<S,C,I,I,I>((obj,p0,p1,p2) =>                                          (Sql.CharIndex(p0, Sql.Left(obj, p2), p1)     ?? 0) - 1) },
				{ M(() => "".LastIndexOf("")      ), L<S,S,I>    ((obj,p0)       => p0.Length == 0 ? obj.Length - 1 : (Sql.CharIndex(p0, obj)                           .Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(Sql.Reverse(p0), Sql.Reverse(obj))                               .Value) - p0.Length + 1) },
				{ M(() => "".LastIndexOf("",0)    ), L<S,S,I,I>  ((obj,p0,p1)    => p0.Length == 0 ? p1             : (Sql.CharIndex(p0, obj,                    p1 + 1).Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(Sql.Reverse(p0), Sql.Reverse(obj.Substring(p1, obj.Length - p1))).Value) - p0.Length + 1) },
				{ M(() => "".LastIndexOf("",0,0)  ), L<S,S,I,I,I>((obj,p0,p1,p2) => p0.Length == 0 ? p1             : (Sql.CharIndex(p0, Sql.Left(obj, p1 + p2), p1 + 1).Value) == 0 ? -1 :    p1 + p2 - (Sql.CharIndex(Sql.Reverse(p0), Sql.Reverse(obj.Substring(p1, p2)))             .Value) - p0.Length + 1) },
				{ M(() => "".LastIndexOf(' ')     ), L<S,C,I>    ((obj,p0)       => (Sql.CharIndex(p0, obj)                           .Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(p0, Sql.Reverse(obj))                               .Value)) },
				{ M(() => "".LastIndexOf(' ',0)   ), L<S,C,I,I>  ((obj,p0,p1)    => (Sql.CharIndex(p0, obj, p1 + 1)                   .Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(p0, Sql.Reverse(obj.Substring(p1, obj.Length - p1))).Value)) },
				{ M(() => "".LastIndexOf(' ',0,0) ), L<S,C,I,I,I>((obj,p0,p1,p2) => (Sql.CharIndex(p0, Sql.Left(obj, p1 + p2), p1 + 1).Value) == 0 ? -1 : p1 + p2    - (Sql.CharIndex(p0, Sql.Reverse(obj.Substring(p1, p2)))             .Value)) },
				{ M(() => "".Insert     (0,"")    ), L<S,I,S,S>  ((obj,p0,p1)    => obj.Length == p0 ? obj + p1 : Sql.Stuff(obj, p0 + 1, 0, p1)) },
				{ M(() => "".Remove     (0)       ), L<S,I,S>    ((obj,p0)       => Sql.Left     (obj, p0)) },
				{ M(() => "".Remove     (0,0)     ), L<S,I,I,S>  ((obj,p0,p1)    => Sql.Stuff    (obj, p0 + 1, p1, "")) },
				{ M(() => "".PadLeft    (0)       ), L<S,I,S>    ((obj,p0)       => Sql.PadLeft  (obj, p0, ' ')) },
				{ M(() => "".PadLeft    (0,' ')   ), L<S,I,C,S>  ((obj,p0,p1)    => Sql.PadLeft  (obj, p0, p1)) },
				{ M(() => "".PadRight   (0)       ), L<S,I,S>    ((obj,p0)       => Sql.PadRight (obj, p0, ' ')) },
				{ M(() => "".PadRight   (0,' ')   ), L<S,I,C,S>  ((obj,p0,p1)    => Sql.PadRight (obj, p0, p1)) },
				{ M(() => "".Replace    ("","")   ), L<S,S,S,S>  ((obj,p0,p1)    => Sql.Replace  (obj, p0, p1)) },
				{ M(() => "".Replace    (' ',' ') ), L<S,C,C,S>  ((obj,p0,p1)    => Sql.Replace  (obj, p0, p1)) },
				{ M(() => "".Trim       ()        ), L<S,S>      ( obj           => Sql.Trim     (obj)) },
				{ M(() => "".TrimEnd    ()        ), L<S,C[],S>  ((obj,ch)       =>     TrimRight(obj, ch)) },
				{ M(() => "".TrimStart  ()        ), L<S,C[],S>  ((obj,ch)       =>     TrimLeft (obj, ch)) },
				{ M(() => "".ToLower    ()        ), L<S,S>      ( obj           => Sql.Lower(obj)) },
				{ M(() => "".ToUpper    ()        ), L<S,S>      ( obj           => Sql.Upper(obj)) },
				{ M(() => "".CompareTo  ("")      ), L<S,S,I>    ((obj,p0)       => ConvertToCaseCompareTo(obj, p0).Value ) },
				{ M(() => "".CompareTo  (1)       ), L<S,O,I>    ((obj,p0)       => ConvertToCaseCompareTo(obj, p0.ToString()).Value ) },

				{ M(() => string.IsNullOrEmpty ("")    ),           L<S,B>          ( p0               => p0 == null || p0.Length == 0) },
				{ M(() => string.CompareOrdinal("","")),            L<S,S,I>        ((s1,s2)           => s1.CompareTo(s2)) },
				{ M(() => string.CompareOrdinal("",0,"",0,0)),      L<S,I,S,I,I,I>  ((s1,i1,s2,i2,l)   => s1.Substring(i1, l).CompareTo(s2.Substring(i2, l))) },
				{ M(() => string.Compare       ("","")),            L<S,S,I>        ((s1,s2)           => s1.CompareTo(s2)) },
				{ M(() => string.Compare       ("",0,"",0,0)),      L<S,I,S,I,I,I>  ((s1,i1,s2,i2,l)   => s1.Substring(i1,l).CompareTo(s2.Substring(i2,l))) },
#if !SILVERLIGHT
				{ M(() => string.Compare       ("","",true)),       L<S,S,B,I>      ((s1,s2,b)         => b ? s1.ToLower().CompareTo(s2.ToLower()) : s1.CompareTo(s2)) },
				{ M(() => string.Compare       ("",0,"",0,0,true)), L<S,I,S,I,I,B,I>((s1,i1,s2,i2,l,b) => b ? s1.Substring(i1,l).ToLower().CompareTo(s2.Substring(i2, l).ToLower()) : s1.Substring(i1, l).CompareTo(s2.Substring(i2, l))) },
#endif

				{ M(() => AltStuff("",0,0,"")), L<S,I?,I?,S,S>((p0, p1,p2,p3) => Sql.Left(p0, p1 - 1) + p3 + Sql.Right(p0, p0.Length - (p1 + p2 - 1))) },

				#endregion

				#region Binary

				{ M(() => ((Binary)null).Length ), L<Binary,I>(obj => Sql.Length(obj).Value) },

				#endregion

				#region DateTime

				{ M(() => Sql.GetDate()                  ), L<D>      (()       => Sql.CurrentTimestamp2 ) },
				{ M(() => DateTime.Now                   ), L<D>      (()       => Sql.CurrentTimestamp2 ) },

				{ M(() => DateTime.Now.Year              ), L<D,I>    (obj      => Sql.DatePart(Sql.DateParts.Year,        obj).Value     ) },
				{ M(() => DateTime.Now.Month             ), L<D,I>    (obj      => Sql.DatePart(Sql.DateParts.Month,       obj).Value     ) },
				{ M(() => DateTime.Now.DayOfYear         ), L<D,I>    (obj      => Sql.DatePart(Sql.DateParts.DayOfYear,   obj).Value     ) },
				{ M(() => DateTime.Now.Day               ), L<D,I>    (obj      => Sql.DatePart(Sql.DateParts.Day,         obj).Value     ) },
				{ M(() => DateTime.Now.DayOfWeek         ), L<D,I>    (obj      => Sql.DatePart(Sql.DateParts.WeekDay,     obj).Value - 1 ) },
				{ M(() => DateTime.Now.Hour              ), L<D,I>    (obj      => Sql.DatePart(Sql.DateParts.Hour,        obj).Value     ) },
				{ M(() => DateTime.Now.Minute            ), L<D,I>    (obj      => Sql.DatePart(Sql.DateParts.Minute,      obj).Value     ) },
				{ M(() => DateTime.Now.Second            ), L<D,I>    (obj      => Sql.DatePart(Sql.DateParts.Second,      obj).Value     ) },
				{ M(() => DateTime.Now.Millisecond       ), L<D,I>    (obj      => Sql.DatePart(Sql.DateParts.Millisecond, obj).Value     ) },
				{ M(() => DateTime.Now.Date              ), L<D,D>    (obj      => Sql.Convert2(Sql.Date,                  obj)     ) },
				{ M(() => DateTime.Now.TimeOfDay         ), L<D,T>    (obj      => Sql.DateToTime(Sql.Convert2(Sql.Time,   obj)).Value    ) },
				{ M(() => DateTime.Now.AddYears       (0)), L<D,I,D>  ((obj,p0) => Sql.DateAdd(Sql.DateParts.Year,        p0, obj).Value ) },
				{ M(() => DateTime.Now.AddMonths      (0)), L<D,I,D>  ((obj,p0) => Sql.DateAdd(Sql.DateParts.Month,       p0, obj).Value ) },
				{ M(() => DateTime.Now.AddDays        (0)), L<D,F,D>  ((obj,p0) => Sql.DateAdd(Sql.DateParts.Day,         p0, obj).Value ) },
				{ M(() => DateTime.Now.AddHours       (0)), L<D,F,D>  ((obj,p0) => Sql.DateAdd(Sql.DateParts.Hour,        p0, obj).Value ) },
				{ M(() => DateTime.Now.AddMinutes     (0)), L<D,F,D>  ((obj,p0) => Sql.DateAdd(Sql.DateParts.Minute,      p0, obj).Value ) },
				{ M(() => DateTime.Now.AddSeconds     (0)), L<D,F,D>  ((obj,p0) => Sql.DateAdd(Sql.DateParts.Second,      p0, obj).Value ) },
				{ M(() => DateTime.Now.AddMilliseconds(0)), L<D,F,D>  ((obj,p0) => Sql.DateAdd(Sql.DateParts.Millisecond, p0, obj).Value ) },
				{ M(() => new DateTime(0, 0, 0)          ), L<I,I,I,D>((y,m,d)  => Sql.MakeDateTime(y, m, d).Value                    ) },

				{ M(() => Sql.MakeDateTime(0, 0, 0)          ), L<I?,I?,I?,D?>         ((y,m,d)        => Sql.Convert(Sql.Date, y.ToString() + "-" + m.ToString() + "-" + d.ToString())) },
				{ M(() => new DateTime    (0, 0, 0, 0, 0, 0) ), L<I,I,I,I,I,I,D>       ((y,m,d,h,mm,s) => Sql.MakeDateTime(y, m, d, h, mm, s).Value ) },
				{ M(() => Sql.MakeDateTime(0, 0, 0, 0, 0, 0) ), L<I?,I?,I?,I?,I?,I?,D?>((y,m,d,h,mm,s) => Sql.Convert(Sql.DateTime2,
					y.ToString() + "-" + m.ToString() + "-" + d.ToString() + " " +
					h.ToString() + ":" + mm.ToString() + ":" + s.ToString())) },

				#endregion

				#region Parse

				{ M(() => Boolean. Parse("")), L<String,Boolean> (p0 => Sql.ConvertTo<Boolean>. From(p0) ) },
				{ M(() => Byte.    Parse("")), L<String,Byte>    (p0 => Sql.ConvertTo<Byte>.    From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Char.    Parse("")), L<String,Char>    (p0 => Sql.ConvertTo<Char>.    From(p0) ) },
#endif
				{ M(() => DateTime.Parse("")), L<String,DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Decimal. Parse("")), L<String,Decimal> (p0 => Sql.ConvertTo<Decimal>. From(p0) ) },
				{ M(() => Double.  Parse("")), L<String,Double>  (p0 => Sql.ConvertTo<Double>.  From(p0) ) },
				{ M(() => Int16.   Parse("")), L<String,Int16>   (p0 => Sql.ConvertTo<Int16>.   From(p0) ) },
				{ M(() => Int32.   Parse("")), L<String,Int32>   (p0 => Sql.ConvertTo<Int32>.   From(p0) ) },
				{ M(() => Int64.   Parse("")), L<String,Int64>   (p0 => Sql.ConvertTo<Int64>.   From(p0) ) },
				{ M(() => SByte.   Parse("")), L<String,SByte>   (p0 => Sql.ConvertTo<SByte>.   From(p0) ) },
				{ M(() => Single.  Parse("")), L<String,Single>  (p0 => Sql.ConvertTo<Single>.  From(p0) ) },
				{ M(() => UInt16.  Parse("")), L<String,UInt16>  (p0 => Sql.ConvertTo<UInt16>.  From(p0) ) },
				{ M(() => UInt32.  Parse("")), L<String,UInt32>  (p0 => Sql.ConvertTo<UInt32>.  From(p0) ) },
				{ M(() => UInt64.  Parse("")), L<String,UInt64>  (p0 => Sql.ConvertTo<UInt64>.  From(p0) ) },

				#endregion

				#region ToString

				{ M(() => ((Boolean)true).ToString()), L<Boolean, String>(p0 => Sql.ConvertTo<string>.From(p0) ) },
				{ M(() => ((Byte)    0)  .ToString()), L<Byte,    String>(p0 => Sql.ConvertTo<string>.From(p0) ) },
				{ M(() => ((Char)   '0') .ToString()), L<Char,    String>(p0 => Sql.ConvertTo<string>.From(p0) ) },
				{ M(() => ((Decimal) 0)  .ToString()), L<Decimal, String>(p0 => Sql.ConvertTo<string>.From(p0) ) },
				{ M(() => ((Double)  0)  .ToString()), L<Double,  String>(p0 => Sql.ConvertTo<string>.From(p0) ) },
				{ M(() => ((Int16)   0)  .ToString()), L<Int16,   String>(p0 => Sql.ConvertTo<string>.From(p0) ) },
				{ M(() => ((Int32)   0)  .ToString()), L<Int32,   String>(p0 => Sql.ConvertTo<string>.From(p0) ) },
				{ M(() => ((Int64)   0)  .ToString()), L<Int64,   String>(p0 => Sql.ConvertTo<string>.From(p0) ) },
				{ M(() => ((SByte)   0)  .ToString()), L<SByte,   String>(p0 => Sql.ConvertTo<string>.From(p0) ) },
				{ M(() => ((Single)  0)  .ToString()), L<Single,  String>(p0 => Sql.ConvertTo<string>.From(p0) ) },
				{ M(() => ((String) "0") .ToString()), L<String,  String>(p0 => p0                             ) },
				{ M(() => ((UInt16)  0)  .ToString()), L<UInt16,  String>(p0 => Sql.ConvertTo<string>.From(p0) ) },
				{ M(() => ((UInt32)  0)  .ToString()), L<UInt32,  String>(p0 => Sql.ConvertTo<string>.From(p0) ) },
				{ M(() => ((UInt64)  0)  .ToString()), L<UInt64,  String>(p0 => Sql.ConvertTo<string>.From(p0) ) },

				#endregion

				#region Convert

				#region ToBoolean

				{ M(() => Convert.ToBoolean((Boolean)true)), L<Boolean, Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
				{ M(() => Convert.ToBoolean((Byte)    0)  ), L<Byte,    Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
				{ M(() => Convert.ToBoolean((Char)   '0') ), L<Char,    Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToBoolean(DateTime.Now) ), L<DateTime,Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
#endif
				{ M(() => Convert.ToBoolean((Decimal) 0)  ), L<Decimal, Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
				{ M(() => Convert.ToBoolean((Double)  0)  ), L<Double,  Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
				{ M(() => Convert.ToBoolean((Int16)   0)  ), L<Int16,   Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
				{ M(() => Convert.ToBoolean((Int32)   0)  ), L<Int32,   Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
				{ M(() => Convert.ToBoolean((Int64)   0)  ), L<Int64,   Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
				{ M(() => Convert.ToBoolean((Object)  0)  ), L<Object,  Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
				{ M(() => Convert.ToBoolean((SByte)   0)  ), L<SByte,   Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
				{ M(() => Convert.ToBoolean((Single)  0)  ), L<Single,  Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
				{ M(() => Convert.ToBoolean((String) "0") ), L<String,  Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
				{ M(() => Convert.ToBoolean((UInt16)  0)  ), L<UInt16,  Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
				{ M(() => Convert.ToBoolean((UInt32)  0)  ), L<UInt32,  Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },
				{ M(() => Convert.ToBoolean((UInt64)  0)  ), L<UInt64,  Boolean>(p0 => Sql.ConvertTo<Boolean>.From(p0) ) },

				#endregion

				#region ToByte

				{ M(() => Convert.ToByte((Boolean)true)), L<Boolean, Byte>(p0 => Sql.ConvertTo<Byte>.From(p0) ) },
				{ M(() => Convert.ToByte((Byte)    0)  ), L<Byte,    Byte>(p0 => Sql.ConvertTo<Byte>.From(p0) ) },
				{ M(() => Convert.ToByte((Char)   '0') ), L<Char,    Byte>(p0 => Sql.ConvertTo<Byte>.From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToByte(DateTime.Now) ), L<DateTime,Byte>(p0 => Sql.ConvertTo<Byte>.From(p0) ) },
#endif
				{ M(() => Convert.ToByte((Decimal) 0)  ), L<Decimal, Byte>(p0 => Sql.ConvertTo<Byte>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToByte((Double)  0)  ), L<Double,  Byte>(p0 => Sql.ConvertTo<Byte>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToByte((Int16)   0)  ), L<Int16,   Byte>(p0 => Sql.ConvertTo<Byte>.From(p0) ) },
				{ M(() => Convert.ToByte((Int32)   0)  ), L<Int32,   Byte>(p0 => Sql.ConvertTo<Byte>.From(p0) ) },
				{ M(() => Convert.ToByte((Int64)   0)  ), L<Int64,   Byte>(p0 => Sql.ConvertTo<Byte>.From(p0) ) },
				{ M(() => Convert.ToByte((Object)  0)  ), L<Object,  Byte>(p0 => Sql.ConvertTo<Byte>.From(p0) ) },
				{ M(() => Convert.ToByte((SByte)   0)  ), L<SByte,   Byte>(p0 => Sql.ConvertTo<Byte>.From(p0) ) },
				{ M(() => Convert.ToByte((Single)  0)  ), L<Single,  Byte>(p0 => Sql.ConvertTo<Byte>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToByte((String) "0") ), L<String,  Byte>(p0 => Sql.ConvertTo<Byte>.From(p0) ) },
				{ M(() => Convert.ToByte((UInt16)  0)  ), L<UInt16,  Byte>(p0 => Sql.ConvertTo<Byte>.From(p0) ) },
				{ M(() => Convert.ToByte((UInt32)  0)  ), L<UInt32,  Byte>(p0 => Sql.ConvertTo<Byte>.From(p0) ) },
				{ M(() => Convert.ToByte((UInt64)  0)  ), L<UInt64,  Byte>(p0 => Sql.ConvertTo<Byte>.From(p0) ) },

				#endregion

				#region ToChar

#if !SILVERLIGHT
				{ M(() => Convert.ToChar((Boolean)true)), L<Boolean, Char>(p0 => Sql.ConvertTo<Char>.From(p0) ) },
#endif
				{ M(() => Convert.ToChar((Byte)    0)  ), L<Byte,    Char>(p0 => Sql.ConvertTo<Char>.From(p0) ) },
				{ M(() => Convert.ToChar((Char)   '0') ), L<Char,    Char>(p0 => p0                       ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToChar(DateTime.Now) ), L<DateTime,Char>(p0 => Sql.ConvertTo<Char>.From(p0) ) },
#endif
				{ M(() => Convert.ToChar((Decimal) 0)  ), L<Decimal, Char>(p0 => Sql.ConvertTo<Char>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToChar((Double)  0)  ), L<Double,  Char>(p0 => Sql.ConvertTo<Char>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToChar((Int16)   0)  ), L<Int16,   Char>(p0 => Sql.ConvertTo<Char>.From(p0) ) },
				{ M(() => Convert.ToChar((Int32)   0)  ), L<Int32,   Char>(p0 => Sql.ConvertTo<Char>.From(p0) ) },
				{ M(() => Convert.ToChar((Int64)   0)  ), L<Int64,   Char>(p0 => Sql.ConvertTo<Char>.From(p0) ) },
				{ M(() => Convert.ToChar((Object)  0)  ), L<Object,  Char>(p0 => Sql.ConvertTo<Char>.From(p0) ) },
				{ M(() => Convert.ToChar((SByte)   0)  ), L<SByte,   Char>(p0 => Sql.ConvertTo<Char>.From(p0) ) },
				{ M(() => Convert.ToChar((Single)  0)  ), L<Single,  Char>(p0 => Sql.ConvertTo<Char>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToChar((String) "0") ), L<String,  Char>(p0 => Sql.ConvertTo<Char>.From(p0) ) },
				{ M(() => Convert.ToChar((UInt16)  0)  ), L<UInt16,  Char>(p0 => Sql.ConvertTo<Char>.From(p0) ) },
				{ M(() => Convert.ToChar((UInt32)  0)  ), L<UInt32,  Char>(p0 => Sql.ConvertTo<Char>.From(p0) ) },
				{ M(() => Convert.ToChar((UInt64)  0)  ), L<UInt64,  Char>(p0 => Sql.ConvertTo<Char>.From(p0) ) },

				#endregion

				#region ToDateTime

				{ M(() => Convert.ToDateTime((Object)  0)  ), L<Object,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Convert.ToDateTime((String) "0") ), L<String,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToDateTime((Boolean)true)), L<Boolean, DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Convert.ToDateTime((Byte)    0)  ), L<Byte,    DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Convert.ToDateTime((Char)   '0') ), L<Char,    DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Convert.ToDateTime(DateTime.Now) ), L<DateTime,DateTime>(p0 => p0                           ) },
				{ M(() => Convert.ToDateTime((Decimal) 0)  ), L<Decimal, DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Convert.ToDateTime((Double)  0)  ), L<Double,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Convert.ToDateTime((Int16)   0)  ), L<Int16,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Convert.ToDateTime((Int32)   0)  ), L<Int32,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Convert.ToDateTime((Int64)   0)  ), L<Int64,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Convert.ToDateTime((SByte)   0)  ), L<SByte,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Convert.ToDateTime((Single)  0)  ), L<Single,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Convert.ToDateTime((UInt16)  0)  ), L<UInt16,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Convert.ToDateTime((UInt32)  0)  ), L<UInt32,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
				{ M(() => Convert.ToDateTime((UInt64)  0)  ), L<UInt64,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
#endif

				#endregion

				#region ToDecimal

				{ M(() => Convert.ToDecimal((Boolean)true)), L<Boolean, Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((Byte)    0)  ), L<Byte,    Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((Char)   '0') ), L<Char,    Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal(DateTime.Now) ), L<DateTime,Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((Decimal) 0)  ), L<Decimal, Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((Double)  0)  ), L<Double,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((Int16)   0)  ), L<Int16,   Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((Int32)   0)  ), L<Int32,   Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((Int64)   0)  ), L<Int64,   Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((Object)  0)  ), L<Object,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((SByte)   0)  ), L<SByte,   Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((Single)  0)  ), L<Single,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((String) "0") ), L<String,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((UInt16)  0)  ), L<UInt16,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((UInt32)  0)  ), L<UInt32,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },
				{ M(() => Convert.ToDecimal((UInt64)  0)  ), L<UInt64,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0) ) },

				#endregion

				#region ToDouble

				{ M(() => Convert.ToDouble((Boolean)true)), L<Boolean, Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
				{ M(() => Convert.ToDouble((Byte)    0)  ), L<Byte,    Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
				{ M(() => Convert.ToDouble((Char)   '0') ), L<Char,    Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToDouble(DateTime.Now) ), L<DateTime,Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
#endif
				{ M(() => Convert.ToDouble((Decimal) 0)  ), L<Decimal, Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
				{ M(() => Convert.ToDouble((Double)  0)  ), L<Double,  Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
				{ M(() => Convert.ToDouble((Int16)   0)  ), L<Int16,   Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
				{ M(() => Convert.ToDouble((Int32)   0)  ), L<Int32,   Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
				{ M(() => Convert.ToDouble((Int64)   0)  ), L<Int64,   Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
				{ M(() => Convert.ToDouble((Object)  0)  ), L<Object,  Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
				{ M(() => Convert.ToDouble((SByte)   0)  ), L<SByte,   Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
				{ M(() => Convert.ToDouble((Single)  0)  ), L<Single,  Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
				{ M(() => Convert.ToDouble((String) "0") ), L<String,  Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
				{ M(() => Convert.ToDouble((UInt16)  0)  ), L<UInt16,  Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
				{ M(() => Convert.ToDouble((UInt32)  0)  ), L<UInt32,  Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },
				{ M(() => Convert.ToDouble((UInt64)  0)  ), L<UInt64,  Double>(p0 => Sql.ConvertTo<Double>.From(p0) ) },

				#endregion

				#region ToInt64

				{ M(() => Convert.ToInt64((Boolean)true)), L<Boolean, Int64>(p0 => Sql.ConvertTo<Int64>.From(p0) ) },
				{ M(() => Convert.ToInt64((Byte)    0)  ), L<Byte,    Int64>(p0 => Sql.ConvertTo<Int64>.From(p0) ) },
				{ M(() => Convert.ToInt64((Char)   '0') ), L<Char,    Int64>(p0 => Sql.ConvertTo<Int64>.From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToInt64(DateTime.Now) ), L<DateTime,Int64>(p0 => Sql.ConvertTo<Int64>.From(p0) ) },
#endif
				{ M(() => Convert.ToInt64((Decimal) 0)  ), L<Decimal, Int64>(p0 => Sql.ConvertTo<Int64>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToInt64((Double)  0)  ), L<Double,  Int64>(p0 => Sql.ConvertTo<Int64>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToInt64((Int16)   0)  ), L<Int16,   Int64>(p0 => Sql.ConvertTo<Int64>.From(p0) ) },
				{ M(() => Convert.ToInt64((Int32)   0)  ), L<Int32,   Int64>(p0 => Sql.ConvertTo<Int64>.From(p0) ) },
				{ M(() => Convert.ToInt64((Int64)   0)  ), L<Int64,   Int64>(p0 => Sql.ConvertTo<Int64>.From(p0) ) },
				{ M(() => Convert.ToInt64((Object)  0)  ), L<Object,  Int64>(p0 => Sql.ConvertTo<Int64>.From(p0) ) },
				{ M(() => Convert.ToInt64((SByte)   0)  ), L<SByte,   Int64>(p0 => Sql.ConvertTo<Int64>.From(p0) ) },
				{ M(() => Convert.ToInt64((Single)  0)  ), L<Single,  Int64>(p0 => Sql.ConvertTo<Int64>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToInt64((String) "0") ), L<String,  Int64>(p0 => Sql.ConvertTo<Int64>.From(p0) ) },
				{ M(() => Convert.ToInt64((UInt16)  0)  ), L<UInt16,  Int64>(p0 => Sql.ConvertTo<Int64>.From(p0) ) },
				{ M(() => Convert.ToInt64((UInt32)  0)  ), L<UInt32,  Int64>(p0 => Sql.ConvertTo<Int64>.From(p0) ) },
				{ M(() => Convert.ToInt64((UInt64)  0)  ), L<UInt64,  Int64>(p0 => Sql.ConvertTo<Int64>.From(p0) ) },

				#endregion

				#region ToInt32

				{ M(() => Convert.ToInt32((Boolean)true)), L<Boolean, Int32>(p0 => Sql.ConvertTo<Int32>.From(p0) ) },
				{ M(() => Convert.ToInt32((Byte)    0)  ), L<Byte,    Int32>(p0 => Sql.ConvertTo<Int32>.From(p0) ) },
				{ M(() => Convert.ToInt32((Char)   '0') ), L<Char,    Int32>(p0 => Sql.ConvertTo<Int32>.From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToInt32(DateTime.Now) ), L<DateTime,Int32>(p0 => Sql.ConvertTo<Int32>.From(p0) ) },
#endif
				{ M(() => Convert.ToInt32((Decimal) 0)  ), L<Decimal, Int32>(p0 => Sql.ConvertTo<Int32>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToInt32((Double)  0)  ), L<Double,  Int32>(p0 => Sql.ConvertTo<Int32>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToInt32((Int16)   0)  ), L<Int16,   Int32>(p0 => Sql.ConvertTo<Int32>.From(p0) ) },
				{ M(() => Convert.ToInt32((Int32)   0)  ), L<Int32,   Int32>(p0 => Sql.ConvertTo<Int32>.From(p0) ) },
				{ M(() => Convert.ToInt32((Int64)   0)  ), L<Int64,   Int32>(p0 => Sql.ConvertTo<Int32>.From(p0) ) },
				{ M(() => Convert.ToInt32((Object)  0)  ), L<Object,  Int32>(p0 => Sql.ConvertTo<Int32>.From(p0) ) },
				{ M(() => Convert.ToInt32((SByte)   0)  ), L<SByte,   Int32>(p0 => Sql.ConvertTo<Int32>.From(p0) ) },
				{ M(() => Convert.ToInt32((Single)  0)  ), L<Single,  Int32>(p0 => Sql.ConvertTo<Int32>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToInt32((String) "0") ), L<String,  Int32>(p0 => Sql.ConvertTo<Int32>.From(p0) ) },
				{ M(() => Convert.ToInt32((UInt16)  0)  ), L<UInt16,  Int32>(p0 => Sql.ConvertTo<Int32>.From(p0) ) },
				{ M(() => Convert.ToInt32((UInt32)  0)  ), L<UInt32,  Int32>(p0 => Sql.ConvertTo<Int32>.From(p0) ) },
				{ M(() => Convert.ToInt32((UInt64)  0)  ), L<UInt64,  Int32>(p0 => Sql.ConvertTo<Int32>.From(p0) ) },

				#endregion

				#region ToInt16

				{ M(() => Convert.ToInt16((Boolean)true)), L<Boolean, Int16>(p0 => Sql.ConvertTo<Int16>.From(p0) ) },
				{ M(() => Convert.ToInt16((Byte)    0)  ), L<Byte,    Int16>(p0 => Sql.ConvertTo<Int16>.From(p0) ) },
				{ M(() => Convert.ToInt16((Char)   '0') ), L<Char,    Int16>(p0 => Sql.ConvertTo<Int16>.From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToInt16(DateTime.Now) ), L<DateTime,Int16>(p0 => Sql.ConvertTo<Int16>.From(p0) ) },
#endif
				{ M(() => Convert.ToInt16((Decimal) 0)  ), L<Decimal, Int16>(p0 => Sql.ConvertTo<Int16>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToInt16((Double)  0)  ), L<Double,  Int16>(p0 => Sql.ConvertTo<Int16>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToInt16((Int16)   0)  ), L<Int16,   Int16>(p0 => Sql.ConvertTo<Int16>.From(p0) ) },
				{ M(() => Convert.ToInt16((Int32)   0)  ), L<Int32,   Int16>(p0 => Sql.ConvertTo<Int16>.From(p0) ) },
				{ M(() => Convert.ToInt16((Int64)   0)  ), L<Int64,   Int16>(p0 => Sql.ConvertTo<Int16>.From(p0) ) },
				{ M(() => Convert.ToInt16((Object)  0)  ), L<Object,  Int16>(p0 => Sql.ConvertTo<Int16>.From(p0) ) },
				{ M(() => Convert.ToInt16((SByte)   0)  ), L<SByte,   Int16>(p0 => Sql.ConvertTo<Int16>.From(p0) ) },
				{ M(() => Convert.ToInt16((Single)  0)  ), L<Single,  Int16>(p0 => Sql.ConvertTo<Int16>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToInt16((String) "0") ), L<String,  Int16>(p0 => Sql.ConvertTo<Int16>.From(p0) ) },
				{ M(() => Convert.ToInt16((UInt16)  0)  ), L<UInt16,  Int16>(p0 => Sql.ConvertTo<Int16>.From(p0) ) },
				{ M(() => Convert.ToInt16((UInt32)  0)  ), L<UInt32,  Int16>(p0 => Sql.ConvertTo<Int16>.From(p0) ) },
				{ M(() => Convert.ToInt16((UInt64)  0)  ), L<UInt64,  Int16>(p0 => Sql.ConvertTo<Int16>.From(p0) ) },

				#endregion

				#region ToSByte

				{ M(() => Convert.ToSByte((Boolean)true)), L<Boolean, SByte>(p0 => Sql.ConvertTo<SByte>.From(p0) ) },
				{ M(() => Convert.ToSByte((Byte)    0)  ), L<Byte,    SByte>(p0 => Sql.ConvertTo<SByte>.From(p0) ) },
				{ M(() => Convert.ToSByte((Char)   '0') ), L<Char,    SByte>(p0 => Sql.ConvertTo<SByte>.From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToSByte(DateTime.Now) ), L<DateTime,SByte>(p0 => Sql.ConvertTo<SByte>.From(p0) ) },
#endif
				{ M(() => Convert.ToSByte((Decimal) 0)  ), L<Decimal, SByte>(p0 => Sql.ConvertTo<SByte>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToSByte((Double)  0)  ), L<Double,  SByte>(p0 => Sql.ConvertTo<SByte>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToSByte((Int16)   0)  ), L<Int16,   SByte>(p0 => Sql.ConvertTo<SByte>.From(p0) ) },
				{ M(() => Convert.ToSByte((Int32)   0)  ), L<Int32,   SByte>(p0 => Sql.ConvertTo<SByte>.From(p0) ) },
				{ M(() => Convert.ToSByte((Int64)   0)  ), L<Int64,   SByte>(p0 => Sql.ConvertTo<SByte>.From(p0) ) },
				{ M(() => Convert.ToSByte((Object)  0)  ), L<Object,  SByte>(p0 => Sql.ConvertTo<SByte>.From(p0) ) },
				{ M(() => Convert.ToSByte((SByte)   0)  ), L<SByte,   SByte>(p0 => Sql.ConvertTo<SByte>.From(p0) ) },
				{ M(() => Convert.ToSByte((Single)  0)  ), L<Single,  SByte>(p0 => Sql.ConvertTo<SByte>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToSByte((String) "0") ), L<String,  SByte>(p0 => Sql.ConvertTo<SByte>.From(p0) ) },
				{ M(() => Convert.ToSByte((UInt16)  0)  ), L<UInt16,  SByte>(p0 => Sql.ConvertTo<SByte>.From(p0) ) },
				{ M(() => Convert.ToSByte((UInt32)  0)  ), L<UInt32,  SByte>(p0 => Sql.ConvertTo<SByte>.From(p0) ) },
				{ M(() => Convert.ToSByte((UInt64)  0)  ), L<UInt64,  SByte>(p0 => Sql.ConvertTo<SByte>.From(p0) ) },

				#endregion

				#region ToSingle

				{ M(() => Convert.ToSingle((Boolean)true)), L<Boolean, Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
				{ M(() => Convert.ToSingle((Byte)    0)  ), L<Byte,    Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
				{ M(() => Convert.ToSingle((Char)   '0') ), L<Char,    Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToSingle(DateTime.Now) ), L<DateTime,Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
#endif
				{ M(() => Convert.ToSingle((Decimal) 0)  ), L<Decimal, Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
				{ M(() => Convert.ToSingle((Double)  0)  ), L<Double,  Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
				{ M(() => Convert.ToSingle((Int16)   0)  ), L<Int16,   Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
				{ M(() => Convert.ToSingle((Int32)   0)  ), L<Int32,   Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
				{ M(() => Convert.ToSingle((Int64)   0)  ), L<Int64,   Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
				{ M(() => Convert.ToSingle((Object)  0)  ), L<Object,  Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
				{ M(() => Convert.ToSingle((SByte)   0)  ), L<SByte,   Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
				{ M(() => Convert.ToSingle((Single)  0)  ), L<Single,  Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
				{ M(() => Convert.ToSingle((String) "0") ), L<String,  Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
				{ M(() => Convert.ToSingle((UInt16)  0)  ), L<UInt16,  Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
				{ M(() => Convert.ToSingle((UInt32)  0)  ), L<UInt32,  Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },
				{ M(() => Convert.ToSingle((UInt64)  0)  ), L<UInt64,  Single>(p0 => Sql.ConvertTo<Single>.From(p0) ) },

				#endregion

				#region ToString

				{ M(() => Convert.ToString((Boolean)true)), L<Boolean, String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
				{ M(() => Convert.ToString((Byte)    0)  ), L<Byte,    String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
				{ M(() => Convert.ToString((Char)   '0') ), L<Char,    String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
				{ M(() => Convert.ToString(DateTime.Now) ), L<DateTime,String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
				{ M(() => Convert.ToString((Decimal) 0)  ), L<Decimal, String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
				{ M(() => Convert.ToString((Double)  0)  ), L<Double,  String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
				{ M(() => Convert.ToString((Int16)   0)  ), L<Int16,   String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
				{ M(() => Convert.ToString((Int32)   0)  ), L<Int32,   String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
				{ M(() => Convert.ToString((Int64)   0)  ), L<Int64,   String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
				{ M(() => Convert.ToString((Object)  0)  ), L<Object,  String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
				{ M(() => Convert.ToString((SByte)   0)  ), L<SByte,   String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
				{ M(() => Convert.ToString((Single)  0)  ), L<Single,  String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToString((String) "0") ), L<String,  String>(p0 => p0                         ) },
#endif
				{ M(() => Convert.ToString((UInt16)  0)  ), L<UInt16,  String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
				{ M(() => Convert.ToString((UInt32)  0)  ), L<UInt32,  String>(p0 => Sql.ConvertTo<String>.From(p0) ) },
				{ M(() => Convert.ToString((UInt64)  0)  ), L<UInt64,  String>(p0 => Sql.ConvertTo<String>.From(p0) ) },

				#endregion

				#region ToInt16

				{ M(() => Convert.ToUInt16((Boolean)true)), L<Boolean, UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0) ) },
				{ M(() => Convert.ToUInt16((Byte)    0)  ), L<Byte,    UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0) ) },
				{ M(() => Convert.ToUInt16((Char)   '0') ), L<Char,    UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToUInt16(DateTime.Now) ), L<DateTime,UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0) ) },
#endif
				{ M(() => Convert.ToUInt16((Decimal) 0)  ), L<Decimal, UInt16>(p0 => Sql.ConvertTo<UInt16>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToUInt16((Double)  0)  ), L<Double,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToUInt16((Int16)   0)  ), L<Int16,   UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0) ) },
				{ M(() => Convert.ToUInt16((Int32)   0)  ), L<Int32,   UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0) ) },
				{ M(() => Convert.ToUInt16((Int64)   0)  ), L<Int64,   UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0) ) },
				{ M(() => Convert.ToUInt16((Object)  0)  ), L<Object,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0) ) },
				{ M(() => Convert.ToUInt16((SByte)   0)  ), L<SByte,   UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0) ) },
				{ M(() => Convert.ToUInt16((Single)  0)  ), L<Single,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToUInt16((String) "0") ), L<String,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0) ) },
				{ M(() => Convert.ToUInt16((UInt16)  0)  ), L<UInt16,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0) ) },
				{ M(() => Convert.ToUInt16((UInt32)  0)  ), L<UInt32,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0) ) },
				{ M(() => Convert.ToUInt16((UInt64)  0)  ), L<UInt64,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0) ) },

				#endregion

				#region ToInt32

				{ M(() => Convert.ToUInt32((Boolean)true)), L<Boolean, UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0) ) },
				{ M(() => Convert.ToUInt32((Byte)    0)  ), L<Byte,    UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0) ) },
				{ M(() => Convert.ToUInt32((Char)   '0') ), L<Char,    UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToUInt32(DateTime.Now) ), L<DateTime,UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0) ) },
#endif
				{ M(() => Convert.ToUInt32((Decimal) 0)  ), L<Decimal, UInt32>(p0 => Sql.ConvertTo<UInt32>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToUInt32((Double)  0)  ), L<Double,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToUInt32((Int16)   0)  ), L<Int16,   UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0) ) },
				{ M(() => Convert.ToUInt32((Int32)   0)  ), L<Int32,   UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0) ) },
				{ M(() => Convert.ToUInt32((Int64)   0)  ), L<Int64,   UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0) ) },
				{ M(() => Convert.ToUInt32((Object)  0)  ), L<Object,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0) ) },
				{ M(() => Convert.ToUInt32((SByte)   0)  ), L<SByte,   UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0) ) },
				{ M(() => Convert.ToUInt32((Single)  0)  ), L<Single,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToUInt32((String) "0") ), L<String,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0) ) },
				{ M(() => Convert.ToUInt32((UInt16)  0)  ), L<UInt16,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0) ) },
				{ M(() => Convert.ToUInt32((UInt32)  0)  ), L<UInt32,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0) ) },
				{ M(() => Convert.ToUInt32((UInt64)  0)  ), L<UInt64,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0) ) },

				#endregion

				#region ToUInt64

				{ M(() => Convert.ToUInt64((Boolean)true)), L<Boolean, UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0) ) },
				{ M(() => Convert.ToUInt64((Byte)    0)  ), L<Byte,    UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0) ) },
				{ M(() => Convert.ToUInt64((Char)   '0') ), L<Char,    UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0) ) },
#if !SILVERLIGHT
				{ M(() => Convert.ToUInt64(DateTime.Now) ), L<DateTime,UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0) ) },
#endif
				{ M(() => Convert.ToUInt64((Decimal) 0)  ), L<Decimal, UInt64>(p0 => Sql.ConvertTo<UInt64>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToUInt64((Double)  0)  ), L<Double,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToUInt64((Int16)   0)  ), L<Int16,   UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0) ) },
				{ M(() => Convert.ToUInt64((Int32)   0)  ), L<Int32,   UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0) ) },
				{ M(() => Convert.ToUInt64((Int64)   0)  ), L<Int64,   UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0) ) },
				{ M(() => Convert.ToUInt64((Object)  0)  ), L<Object,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0) ) },
				{ M(() => Convert.ToUInt64((SByte)   0)  ), L<SByte,   UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0) ) },
				{ M(() => Convert.ToUInt64((Single)  0)  ), L<Single,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(Sql.RoundToEven(p0)) ) },
				{ M(() => Convert.ToUInt64((String) "0") ), L<String,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0) ) },
				{ M(() => Convert.ToUInt64((UInt16)  0)  ), L<UInt16,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0) ) },
				{ M(() => Convert.ToUInt64((UInt32)  0)  ), L<UInt32,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0) ) },
				{ M(() => Convert.ToUInt64((UInt64)  0)  ), L<UInt64,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0) ) },

				#endregion

				#endregion

				#region Math

				{ M(() => Math.Abs    ((Decimal)0)), L<Decimal,Decimal>(p => Sql.Abs(p).Value ) },
				{ M(() => Math.Abs    ((Double) 0)), L<Double, Double> (p => Sql.Abs(p).Value ) },
				{ M(() => Math.Abs    ((Int16)  0)), L<Int16,  Int16>  (p => Sql.Abs(p).Value ) },
				{ M(() => Math.Abs    ((Int32)  0)), L<Int32,  Int32>  (p => Sql.Abs(p).Value ) },
				{ M(() => Math.Abs    ((Int64)  0)), L<Int64,  Int64>  (p => Sql.Abs(p).Value ) },
				{ M(() => Math.Abs    ((SByte)  0)), L<SByte,  SByte>  (p => Sql.Abs(p).Value ) },
				{ M(() => Math.Abs    ((Single) 0)), L<Single, Single> (p => Sql.Abs(p).Value ) },

				{ M(() => Math.Acos   (0)   ), L<F,F>  ( p    => Sql.Acos   (p)   .Value ) },
				{ M(() => Math.Asin   (0)   ), L<F,F>  ( p    => Sql.Asin   (p)   .Value ) },
				{ M(() => Math.Atan   (0)   ), L<F,F>  ( p    => Sql.Atan   (p)   .Value ) },
				{ M(() => Math.Atan2  (0,0) ), L<F,F,F>((x,y) => Sql.Atan2  (x, y).Value ) },
#if !SILVERLIGHT
				{ M(() => Math.Ceiling((M)0)), L<M,M>  ( p    => Sql.Ceiling(p)   .Value ) },
#endif
				{ M(() => Math.Ceiling((F)0)), L<F,F>  ( p    => Sql.Ceiling(p)   .Value ) },
				{ M(() => Math.Cos    (0)   ), L<F,F>  ( p    => Sql.Cos    (p)   .Value ) },
				{ M(() => Math.Cosh   (0)   ), L<F,F>  ( p    => Sql.Cosh   (p)   .Value ) },
				{ M(() => Math.Exp    (0)   ), L<F,F>  ( p    => Sql.Exp    (p)   .Value ) },
#if !SILVERLIGHT
				{ M(() => Math.Floor  ((M)0)), L<M,M>  ( p    => Sql.Floor  (p)   .Value ) },
#endif
				{ M(() => Math.Floor  ((F)0)), L<F,F>  ( p    => Sql.Floor  (p)   .Value ) },
				{ M(() => Math.Log    (0)   ), L<F,F>  ( p    => Sql.Log    (p)   .Value ) },
				{ M(() => Math.Log    (0,0) ), L<F,F,F>((m,n) => Sql.Log    (n, m).Value ) },
				{ M(() => Math.Log10  (0)   ), L<F,F>  ( p    => Sql.Log10  (p)   .Value ) },

				{ M(() => Math.Max((Byte)   0, (Byte)   0)), L<Byte,   Byte,   Byte>   ((v1,v2) => v1 > v2 ? v1 : v2) },
				{ M(() => Math.Max((Decimal)0, (Decimal)0)), L<Decimal,Decimal,Decimal>((v1,v2) => v1 > v2 ? v1 : v2) },
				{ M(() => Math.Max((Double) 0, (Double) 0)), L<Double, Double, Double> ((v1,v2) => v1 > v2 ? v1 : v2) },
				{ M(() => Math.Max((Int16)  0, (Int16)  0)), L<Int16,  Int16,  Int16>  ((v1,v2) => v1 > v2 ? v1 : v2) },
				{ M(() => Math.Max((Int32)  0, (Int32)  0)), L<Int32,  Int32,  Int32>  ((v1,v2) => v1 > v2 ? v1 : v2) },
				{ M(() => Math.Max((Int64)  0, (Int64)  0)), L<Int64,  Int64,  Int64>  ((v1,v2) => v1 > v2 ? v1 : v2) },
				{ M(() => Math.Max((SByte)  0, (SByte)  0)), L<SByte,  SByte,  SByte>  ((v1,v2) => v1 > v2 ? v1 : v2) },
				{ M(() => Math.Max((Single) 0, (Single) 0)), L<Single, Single, Single> ((v1,v2) => v1 > v2 ? v1 : v2) },
				{ M(() => Math.Max((UInt16) 0, (UInt16) 0)), L<UInt16, UInt16, UInt16> ((v1,v2) => v1 > v2 ? v1 : v2) },
				{ M(() => Math.Max((UInt32) 0, (UInt32) 0)), L<UInt32, UInt32, UInt32> ((v1,v2) => v1 > v2 ? v1 : v2) },
				{ M(() => Math.Max((UInt64) 0, (UInt64) 0)), L<UInt64, UInt64, UInt64> ((v1,v2) => v1 > v2 ? v1 : v2) },

				{ M(() => Math.Min((Byte)   0, (Byte)   0)), L<Byte,   Byte,   Byte>   ((v1,v2) => v1 < v2 ? v1 : v2) },
				{ M(() => Math.Min((Decimal)0, (Decimal)0)), L<Decimal,Decimal,Decimal>((v1,v2) => v1 < v2 ? v1 : v2) },
				{ M(() => Math.Min((Double) 0, (Double) 0)), L<Double, Double, Double> ((v1,v2) => v1 < v2 ? v1 : v2) },
				{ M(() => Math.Min((Int16)  0, (Int16)  0)), L<Int16,  Int16,  Int16>  ((v1,v2) => v1 < v2 ? v1 : v2) },
				{ M(() => Math.Min((Int32)  0, (Int32)  0)), L<Int32,  Int32,  Int32>  ((v1,v2) => v1 < v2 ? v1 : v2) },
				{ M(() => Math.Min((Int64)  0, (Int64)  0)), L<Int64,  Int64,  Int64>  ((v1,v2) => v1 < v2 ? v1 : v2) },
				{ M(() => Math.Min((SByte)  0, (SByte)  0)), L<SByte,  SByte,  SByte>  ((v1,v2) => v1 < v2 ? v1 : v2) },
				{ M(() => Math.Min((Single) 0, (Single) 0)), L<Single, Single, Single> ((v1,v2) => v1 < v2 ? v1 : v2) },
				{ M(() => Math.Min((UInt16) 0, (UInt16) 0)), L<UInt16, UInt16, UInt16> ((v1,v2) => v1 < v2 ? v1 : v2) },
				{ M(() => Math.Min((UInt32) 0, (UInt32) 0)), L<UInt32, UInt32, UInt32> ((v1,v2) => v1 < v2 ? v1 : v2) },
				{ M(() => Math.Min((UInt64) 0, (UInt64) 0)), L<UInt64, UInt64, UInt64> ((v1,v2) => v1 < v2 ? v1 : v2) },

				{ M(() => Math.Pow        (0,0)), L<F,F,F>((x,y) => Sql.Power(x, y).Value ) },

				{ M(() => Sql.Round       (0m)  ), L<M?,M?>   ( d    => Sql.Round(d, 0)) },
				{ M(() => Sql.Round       (0.0) ), L<F?,F?>   ( d    => Sql.Round(d, 0)) },

				{ M(() => Sql.RoundToEven(0m)   ), L<M?,M?>   ( d    => d - Sql.Floor(d) == 0.5m && Sql.Floor(d) % 2 == 0? Sql.Floor(d) : Sql.Round(d)) },
				{ M(() => Sql.RoundToEven(0.0)  ), L<F?,F?>   ( d    => d - Sql.Floor(d) == 0.5  && Sql.Floor(d) % 2 == 0? Sql.Floor(d) : Sql.Round(d)) },

				{ M(() => Sql.RoundToEven(0m, 0)), L<M?,I?,M?>((d,n) => d * 2 == Sql.Round(d * 2, n) && d != Sql.Round(d, n) ? Sql.Round(d / 2, n) * 2 : Sql.Round(d, n)) },
				{ M(() => Sql.RoundToEven(0.0,0)), L<F?,I?,F?>((d,n) => d * 2 == Sql.Round(d * 2, n) && d != Sql.Round(d, n) ? Sql.Round(d / 2, n) * 2 : Sql.Round(d, n)) },

				{ M(() => Math.Round     (0m)   ), L<M,M>     ( d    => Sql.RoundToEven(d).Value ) },
				{ M(() => Math.Round     (0.0)  ), L<F,F>     ( d    => Sql.RoundToEven(d).Value ) },

				{ M(() => Math.Round     (0m, 0)), L<M,I,M>   ((d,n) => Sql.RoundToEven(d, n).Value ) },
				{ M(() => Math.Round     (0.0,0)), L<F,I,F>   ((d,n) => Sql.RoundToEven(d, n).Value ) },

#if !SILVERLIGHT
				{ M(() => Math.Round (0m,    MidpointRounding.ToEven)), L<M,  MidpointRounding,M>((d,  p) => p == MidpointRounding.ToEven ? Sql.RoundToEven(d).  Value : Sql.Round(d).  Value ) },
				{ M(() => Math.Round (0.0,   MidpointRounding.ToEven)), L<F,  MidpointRounding,F>((d,  p) => p == MidpointRounding.ToEven ? Sql.RoundToEven(d).  Value : Sql.Round(d).  Value ) },

				{ M(() => Math.Round (0m, 0, MidpointRounding.ToEven)), L<M,I,MidpointRounding,M>((d,n,p) => p == MidpointRounding.ToEven ? Sql.RoundToEven(d,n).Value : Sql.Round(d,n).Value ) },
				{ M(() => Math.Round (0.0,0, MidpointRounding.ToEven)), L<F,I,MidpointRounding,F>((d,n,p) => p == MidpointRounding.ToEven ? Sql.RoundToEven(d,n).Value : Sql.Round(d,n).Value ) },
#endif

				{ M(() => Math.Sign  ((Decimal)0)), L<Decimal,I>(p => Sql.Sign(p).Value ) },
				{ M(() => Math.Sign  ((Double) 0)), L<Double, I>(p => Sql.Sign(p).Value ) },
				{ M(() => Math.Sign  ((Int16)  0)), L<Int16,  I>(p => Sql.Sign(p).Value ) },
				{ M(() => Math.Sign  ((Int32)  0)), L<Int32,  I>(p => Sql.Sign(p).Value ) },
				{ M(() => Math.Sign  ((Int64)  0)), L<Int64,  I>(p => Sql.Sign(p).Value ) },
				{ M(() => Math.Sign  ((SByte)  0)), L<SByte,  I>(p => Sql.Sign(p).Value ) },
				{ M(() => Math.Sign  ((Single) 0)), L<Single, I>(p => Sql.Sign(p).Value ) },

				{ M(() => Math.Sin   (0)), L<F,F>( p => Sql.Sin (p).Value ) },
				{ M(() => Math.Sinh  (0)), L<F,F>( p => Sql.Sinh(p).Value ) },
				{ M(() => Math.Sqrt  (0)), L<F,F>( p => Sql.Sqrt(p).Value ) },
				{ M(() => Math.Tan   (0)), L<F,F>( p => Sql.Tan (p).Value ) },
				{ M(() => Math.Tanh  (0)), L<F,F>( p => Sql.Tanh(p).Value ) },

#if !SILVERLIGHT
				{ M(() => Math.Truncate(0m)),  L<M,M>( p => Sql.Truncate(p).Value ) },
				{ M(() => Math.Truncate(0.0)), L<F,F>( p => Sql.Truncate(p).Value ) },
#endif

				#endregion

				#region Visual Basic Compiler Services

//#if !SILVERLIGHT
//				{ M(() => Operators.CompareString("","",false)), L<S,S,B,I>((s1,s2,b) => b ? string.CompareOrdinal(s1.ToUpper(), s2.ToUpper()) : string.CompareOrdinal(s1, s2)) },
//#endif

				#endregion

			}},

			#region MsSql2008

			{ ProviderName.MsSql2008, new Dictionary<MemberInfo,LambdaExpression> {
				{ M(() => Sql.PadRight("",0,' ')),  L<S,I?,C,S>   ((p0,p1,p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length)) },
				{ M(() => Sql.PadLeft ("",0,' ')),  L<S,I?,C,S>   ((p0,p1,p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0) },
				{ M(() => Sql.Trim    ("")      ),  L<S,S>        ( p0        => Sql.TrimLeft(Sql.TrimRight(p0))) },
				{ M(() => Sql.MakeDateTime(0,0,0)), L<I?,I?,I?,D?>((y,m,d)    => DateAdd(Sql.DateParts.Month, (y.Value - 1900) * 12 + m.Value - 1, d.Value - 1)) },

				{ M(() => Sql.Cosh(0)   ), L<F?,F?>   ( v    => (Sql.Exp(v) + Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Log(0m, 0)), L<M?,M?,M?>((m,n) => Sql.Log(n) / Sql.Log(m)) },
				{ M(() => Sql.Log(0.0,0)), L<F?,F?,F?>((m,n) => Sql.Log(n) / Sql.Log(m)) },
				{ M(() => Sql.Sinh(0)   ), L<F?,F?>   ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Tanh(0)   ), L<F?,F?>   ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v))) },
			}},

			#endregion

			#region MsSql2005

			{ ProviderName.MsSql2005, new Dictionary<MemberInfo,LambdaExpression> {
				{ M(() => Sql.PadRight("",0,' ')),  L<S,I?,C,S>   ((p0,p1,p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length)) },
				{ M(() => Sql.PadLeft ("",0,' ')),  L<S,I?,C,S>   ((p0,p1,p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0) },
				{ M(() => Sql.Trim    ("")      ),  L<S,S>        ( p0        => Sql.TrimLeft(Sql.TrimRight(p0))) },
				{ M(() => Sql.MakeDateTime(0,0,0)), L<I?,I?,I?,D?>((y,m,d)    => DateAdd(Sql.DateParts.Month, (y.Value - 1900) * 12 + m.Value - 1, d.Value - 1)) },
				{ M(() => Sql.MakeDateTime(0, 0, 0, 0, 0, 0) ), L<I?,I?,I?,I?,I?,I?,D?>((y,m,d,h,mm,s) => Sql.Convert(Sql.DateTime2,
					y.ToString() + "-" + m.ToString() + "-" + d.ToString() + " " +
					h.ToString() + ":" + mm.ToString() + ":" + s.ToString(), 120)) },
				{ M(() => Sql.Cosh(0)   ), L<F?,F?>   ( v    => (Sql.Exp(v) + Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Log(0m, 0)), L<M?,M?,M?>((m,n) => Sql.Log(n) / Sql.Log(m)) },
				{ M(() => Sql.Log(0.0,0)), L<F?,F?,F?>((m,n) => Sql.Log(n) / Sql.Log(m)) },
				{ M(() => Sql.Sinh(0)   ), L<F?,F?>   ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Tanh(0)   ), L<F?,F?>   ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v))) },

				{ M(() => DateTime.Parse("")), L<String,DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) ) },
			}},

			#endregion

			#region SqlCe

			{ ProviderName.SqlCe, new Dictionary<MemberInfo,LambdaExpression> {
				{ M(() => Sql.Left    ("",0)    ), L<S,I?,S>   ((p0,p1)    => Sql.Substring(p0, 1, p1)) },
				{ M(() => Sql.Right   ("",0)    ), L<S,I?,S>   ((p0,p1)    => Sql.Substring(p0, p0.Length - p1 + 1, p1)) },
				{ M(() => Sql.PadRight("",0,' ')), L<S,I?,C?,S>((p0,p1,p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length)) },
				{ M(() => Sql.PadLeft ("",0,' ')), L<S,I?,C?,S>((p0,p1,p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0) },
				{ M(() => Sql.Trim    ("")      ), L<S,S>      ( p0        => Sql.TrimLeft(Sql.TrimRight(p0))) },

				{ M(() => Sql.Cosh(0)    ), L<F?,F?>   ( v    => (Sql.Exp(v) + Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Log (0m, 0)), L<M?,M?,M?>((m,n) => Sql.Log(n) / Sql.Log(m)) },
				{ M(() => Sql.Log (0.0,0)), L<F?,F?,F?>((m,n) => Sql.Log(n) / Sql.Log(m)) },
				{ M(() => Sql.Sinh(0)    ), L<F?,F?>   ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Tanh(0)    ), L<F?,F?>   ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v))) },
			}},

			#endregion

			#region DB2

			{ ProviderName.DB2, new Dictionary<MemberInfo,LambdaExpression> {
				{ M(() => Sql.Space   (0)        ), L<I?,S>       ( p0           => Sql.Convert(Sql.VarChar(1000), Replicate(" ", p0))) },
				{ M(() => Sql.Stuff   ("",0,0,"")), L<S,I?,I?,S,S>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3)) },
				{ M(() => Sql.PadRight("",0,' ') ), L<S,I?,C?,S>  ((p0,p1,p2)    => p0.Length > p1 ? p0 : p0 + VarChar(Replicate(p2, p1 - p0.Length), 1000)) },
				{ M(() => Sql.PadLeft ("",0,' ') ), L<S,I?,C?,S>  ((p0,p1,p2)    => p0.Length > p1 ? p0 : VarChar(Replicate(p2, p1 - p0.Length), 1000) + p0) },

				{ M(() => Sql.ConvertTo<String>.From((Decimal)0)), L<Decimal,S>(p => Sql.TrimLeft(Sql.Convert<string,Decimal>(p), '0')) },
				{ M(() => Sql.ConvertTo<String>.From(Guid.Empty)), L<Guid,   S>(p => Sql.Lower(
					Sql.Substring(Hex(p),  7,  2) + Sql.Substring(Hex(p),  5, 2) + Sql.Substring(Hex(p), 3, 2) + Sql.Substring(Hex(p), 1, 2) + "-" +
					Sql.Substring(Hex(p), 11,  2) + Sql.Substring(Hex(p),  9, 2) + "-" +
					Sql.Substring(Hex(p), 15,  2) + Sql.Substring(Hex(p), 13, 2) + "-" +
					Sql.Substring(Hex(p), 17,  4) + "-" +
					Sql.Substring(Hex(p), 21, 12))) },

				{ M(() => Sql.Log(0m, 0)), L<M?,M?,M?>((m,n) => Sql.Log(n) / Sql.Log(m)) },
				{ M(() => Sql.Log(0.0,0)), L<F?,F?,F?>((m,n) => Sql.Log(n) / Sql.Log(m)) },
			}},

			#endregion

			#region Informix

			{ ProviderName.Informix, new Dictionary<MemberInfo,LambdaExpression> {
				{ M(() => Sql.Left ("",0)     ), L<S,I?,S>     ((p0,p1)       => Sql.Substring(p0,  1, p1))                  },
				{ M(() => Sql.Right("",0)     ), L<S,I?,S>     ((p0,p1)       => Sql.Substring(p0,  p0.Length - p1 + 1, p1)) },
				{ M(() => Sql.Stuff("",0,0,"")), L<S,I?,I?,S,S>((p0,p1,p2,p3) =>     AltStuff (p0,  p1, p2, p3))             },
				{ M(() => Sql.Space(0)        ), L<I?,S>       ( p0           => Sql.PadRight (" ", p0, ' '))                },

				{ M(() => Sql.MakeDateTime(0,0,0)), L<I?,I?,I?,D?>((y,m,d) => Mdy(m, d, y)) },

				{ M(() => Sql.Cot (0)         ), L<F?,F?>      ( v            => Sql.Cos(v) / Sql.Sin(v) )        },
				{ M(() => Sql.Cosh(0)         ), L<F?,F?>      ( v            => (Sql.Exp(v) + Sql.Exp(-v)) / 2 ) },

				{ M(() => Sql.Degrees((Decimal?)0)), L<Decimal?,Decimal?>( v => (Decimal?)(v.Value * (180 / (Decimal)Math.PI))) },
				{ M(() => Sql.Degrees((Double?) 0)), L<Double?, Double?> ( v => (Double?) (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int16?)  0)), L<Int16?,  Int16?>  ( v => (Int16?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int32?)  0)), L<Int32?,  Int32?>  ( v => (Int32?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int64?)  0)), L<Int64?,  Int64?>  ( v => (Int64?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((SByte?)  0)), L<SByte?,  SByte?>  ( v => (SByte?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Single?) 0)), L<Single?, Single?> ( v => (Single?) (v.Value * (180 / Math.PI))) },

				{ M(() => Sql.Log(0m, 0)), L<M?,M?,M?>((m,n) => Sql.Log(n) / Sql.Log(m)) },
				{ M(() => Sql.Log(0.0,0)), L<F?,F?,F?>((m,n) => Sql.Log(n) / Sql.Log(m)) },

				{ M(() => Sql.Sign((Decimal?)0)), L<Decimal?,I?>(p => p > 0 ? 1 : p < 0 ? -1 : 0 ) },
				{ M(() => Sql.Sign((Double?) 0)), L<Double?, I?>(p => p > 0 ? 1 : p < 0 ? -1 : 0 ) },
				{ M(() => Sql.Sign((Int16?)  0)), L<Int16?,  I?>(p => p > 0 ? 1 : p < 0 ? -1 : 0 ) },
				{ M(() => Sql.Sign((Int32?)  0)), L<Int32?,  I?>(p => p > 0 ? 1 : p < 0 ? -1 : 0 ) },
				{ M(() => Sql.Sign((Int64?)  0)), L<Int64?,  I?>(p => p > 0 ? 1 : p < 0 ? -1 : 0 ) },
				{ M(() => Sql.Sign((SByte?)  0)), L<SByte?,  I?>(p => p > 0 ? 1 : p < 0 ? -1 : 0 ) },
				{ M(() => Sql.Sign((Single?) 0)), L<Single?, I?>(p => p > 0 ? 1 : p < 0 ? -1 : 0 ) },

				{ M(() => Sql.Sinh(0)), L<F?,F?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Tanh(0)), L<F?,F?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) +Sql.Exp(-v))) },
			}},

			#endregion

			#region Oracle

			{ ProviderName.Oracle, new Dictionary<MemberInfo,LambdaExpression> {
				{ M(() => Sql.Left ("",0)     ), L<S,I?,S>     ((p0,p1)       => Sql.Substring(p0, 1, p1)) },
				{ M(() => Sql.Right("",0)     ), L<S,I?,S>     ((p0,p1)       => Sql.Substring(p0, p0.Length - p1 + 1, p1)) },
				{ M(() => Sql.Stuff("",0,0,"")), L<S,I?,I?,S,S>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3)) },
				{ M(() => Sql.Space(0)        ), L<I?,S>       ( p0           => Sql.PadRight(" ", p0, ' ')) },

				{ M(() => Sql.ConvertTo<String>.From(Guid.Empty)), L<Guid,S>(p => Sql.Lower(
					Sql.Substring(Sql.Convert2(Sql.Char(36), p),  7,  2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p),  5, 2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p), 3, 2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p), 1, 2) + "-" +
					Sql.Substring(Sql.Convert2(Sql.Char(36), p), 11,  2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p),  9, 2) + "-" +
					Sql.Substring(Sql.Convert2(Sql.Char(36), p), 15,  2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p), 13, 2) + "-" +
					Sql.Substring(Sql.Convert2(Sql.Char(36), p), 17,  4) + "-" +
					Sql.Substring(Sql.Convert2(Sql.Char(36), p), 21, 12))) },

				{ M(() => Sql.Cot  (0)),   L<F?,F?>(v => Sql.Cos(v) / Sql.Sin(v) ) },
				{ M(() => Sql.Log10(0.0)), L<F?,F?>(v => Sql.Log(10, v)      ) },

				{ M(() => Sql.Degrees((Decimal?)0)), L<Decimal?,Decimal?>( v => (Decimal?)(v.Value * (180 / (Decimal)Math.PI))) },
				{ M(() => Sql.Degrees((Double?) 0)), L<Double?, Double?> ( v => (Double?) (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int16?)  0)), L<Int16?,  Int16?>  ( v => (Int16?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int32?)  0)), L<Int32?,  Int32?>  ( v => (Int32?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int64?)  0)), L<Int64?,  Int64?>  ( v => (Int64?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((SByte?)  0)), L<SByte?,  SByte?>  ( v => (SByte?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Single?) 0)), L<Single?, Single?> ( v => (Single?) (v.Value * (180 / Math.PI))) },
			}},

			#endregion

			#region Firebird

			{ ProviderName.Firebird, new Dictionary<MemberInfo,LambdaExpression> {
				{ M<S>(_  => Sql.Space(0         )),   L<I?,S>       ( p0           => Sql.PadRight(" ", p0, ' ')) },
				{ M<S>(s  => Sql.Stuff(s, 0, 0, s)),   L<S,I?,I?,S,S>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3)) },

				{ M(() => Sql.Degrees((Decimal?)0)), L<Decimal?,Decimal?>( v => (Decimal?)(v.Value * 180 / DecimalPI())) },
				{ M(() => Sql.Degrees((Double?) 0)), L<Double?, Double?> ( v => (Double?) (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int16?)  0)), L<Int16?,  Int16?>  ( v => (Int16?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int32?)  0)), L<Int32?,  Int32?>  ( v => (Int32?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int64?)  0)), L<Int64?,  Int64?>  ( v => (Int64?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((SByte?)  0)), L<SByte?,  SByte?>  ( v => (SByte?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Single?) 0)), L<Single?, Single?> ( v => (Single?) (v.Value * (180 / Math.PI))) },

				{ M(() => Sql.RoundToEven(0.0)  ), L<F?,F?>   ( v    => (double)Sql.RoundToEven((decimal)v))    },
				{ M(() => Sql.RoundToEven(0.0,0)), L<F?,I?,F?>((v,p) => (double)Sql.RoundToEven((decimal)v, p)) },
			}},

			#endregion

			#region MySql

			{ ProviderName.MySql, new Dictionary<MemberInfo,LambdaExpression> {
				{ M<S>(s => Sql.Stuff(s, 0, 0, s)), L<S,I?,I?,S,S>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3)) },

				{ M(() => Sql.Cosh(0)), L<F?,F?>(v => (Sql.Exp(v) + Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Sinh(0)), L<F?,F?>(v => (Sql.Exp(v) - Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Tanh(0)), L<F?,F?>(v => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v))) },
			}},

			#endregion

			#region PostgreSQL

			{ ProviderName.PostgreSQL, new Dictionary<MemberInfo,LambdaExpression> {
				{ M(() => Sql.Left ("",0)     ), L<S,I?,S>     ((p0,p1)       => Sql.Substring(p0, 1, p1)) },
				{ M(() => Sql.Right("",0)     ), L<S,I?,S>     ((p0,p1)       => Sql.Substring(p0, p0.Length - p1 + 1, p1)) },
				{ M(() => Sql.Stuff("",0,0,"")), L<S,I?,I?,S,S>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3)) },
				{ M(() => Sql.Space(0)        ), L<I?,S>       ( p0           => Replicate(" ", p0)) },

				{ M(() => Sql.Cosh(0)           ), L<F?,F?>    ( v            => (Sql.Exp(v) + Sql.Exp(-v)) / 2 ) },
				{ M(() => Sql.Round      (0.0,0)), L<F?,I?,F?> ((v,p)         => (double)Sql.Round      ((decimal)v, p)) },
				{ M(() => Sql.RoundToEven(0.0)  ), L<F?,F?>    ( v            => (double)Sql.RoundToEven((decimal)v))    },
				{ M(() => Sql.RoundToEven(0.0,0)), L<F?,I?,F?> ((v,p)         => (double)Sql.RoundToEven((decimal)v, p)) },

				{ M(() => Sql.Log  ((double)0,0)), L<F?,F?,F?> ((m,n)         => (F?)Sql.Log((M)m,(M)n).Value ) },
				{ M(() => Sql.Sinh (0)          ), L<F?,F?>    ( v            => (Sql.Exp(v) - Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Tanh (0)          ), L<F?,F?>    ( v            => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v))) },

				{ M(() => Sql.Truncate(0.0)     ), L<F?,F?>    ( v            => (double)Sql.Truncate((decimal)v)) },
			}},

			#endregion

			#region SQLite

			{ ProviderName.SQLite, new Dictionary<MemberInfo,LambdaExpression> {
				{ M(() => Sql.Stuff   ("",0,0,"")), L<S,I?,I?,S,S>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3)) },
				{ M(() => Sql.PadRight("",0,' ') ), L<S,I?,C?,S>  ((p0,p1,p2)    => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length)) },
				{ M(() => Sql.PadLeft ("",0,' ') ), L<S,I?,C?,S>  ((p0,p1,p2)    => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0) },

				{ M(() => Sql.MakeDateTime(0, 0, 0)), L<I?,I?,I?,D?>((y,m,d) => Sql.Convert(Sql.Date,
					y.ToString() + "-" +
					(m.ToString().Length == 1 ? "0" + m.ToString() : m.ToString()) + "-" +
					(d.ToString().Length == 1 ? "0" + d.ToString() : d.ToString()))) },

				{ M(() => Sql.MakeDateTime(0, 0, 0, 0, 0, 0)), L<I?,I?,I?,I?,I?,I?,D?>((y,m,d,h,i,s) => Sql.Convert(Sql.DateTime2,
					y.ToString() + "-" +
					(m.ToString().Length == 1 ? "0" + m.ToString() : m.ToString()) + "-" +
					(d.ToString().Length == 1 ? "0" + d.ToString() : d.ToString()) + " " +
					(h.ToString().Length == 1 ? "0" + h.ToString() : h.ToString()) + ":" +
					(i.ToString().Length == 1 ? "0" + i.ToString() : i.ToString()) + ":" +
					(s.ToString().Length == 1 ? "0" + s.ToString() : s.ToString()))) },

				{ M(() => Sql.ConvertTo<String>.From(Guid.Empty)), L<Guid,S>(p => Sql.Lower(
					Sql.Substring(Hex(p),  7,  2) + Sql.Substring(Hex(p),  5, 2) + Sql.Substring(Hex(p), 3, 2) + Sql.Substring(Hex(p), 1, 2) + "-" +
					Sql.Substring(Hex(p), 11,  2) + Sql.Substring(Hex(p),  9, 2) + "-" +
					Sql.Substring(Hex(p), 15,  2) + Sql.Substring(Hex(p), 13, 2) + "-" +
					Sql.Substring(Hex(p), 17,  4) + "-" +
					Sql.Substring(Hex(p), 21, 12))) },

				{ M(() => Sql.Log (0m, 0)), L<M?,M?,M?>((m,n) => Sql.Log(n) / Sql.Log(m)) },
				{ M(() => Sql.Log (0.0,0)), L<F?,F?,F?>((m,n) => Sql.Log(n) / Sql.Log(m)) },

				{ M(() => Sql.Truncate(0m)),  L<M?,M?>( v => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v)) },
				{ M(() => Sql.Truncate(0.0)), L<F?,F?>( v => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v)) },
			}},

			#endregion

			#region Sybase

			{ ProviderName.Sybase, new Dictionary<MemberInfo,LambdaExpression> {
				{ M(() => Sql.PadRight("",0,' ')),  L<S,I?,C?,S>((p0,p1,p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length)) },
				{ M(() => Sql.PadLeft ("",0,' ')),  L<S,I?,C?,S>((p0,p1,p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0) },
				{ M(() => Sql.Trim    ("")      ),  L<S,S>      ( p0        => Sql.TrimLeft(Sql.TrimRight(p0))) },

				{ M(() => Sql.Cosh(0)    ), L<F?,F?>   ( v    => (Sql.Exp(v) + Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Log (0m, 0)), L<M?,M?,M?>((m,n) => Sql.Log(n) / Sql.Log(m)) },
				{ M(() => Sql.Log (0.0,0)), L<F?,F?,F?>((m,n) => Sql.Log(n) / Sql.Log(m)) },

				{ M(() => Sql.Degrees((Decimal?)0)), L<Decimal?,Decimal?>( v => (Decimal?)(v.Value * (180 / (Decimal)Math.PI))) },
				{ M(() => Sql.Degrees((Double?) 0)), L<Double?, Double?> ( v => (Double?) (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int16?)  0)), L<Int16?,  Int16?>  ( v => (Int16?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int32?)  0)), L<Int32?,  Int32?>  ( v => (Int32?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int64?)  0)), L<Int64?,  Int64?>  ( v => (Int64?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((SByte?)  0)), L<SByte?,  SByte?>  ( v => (SByte?)  (v.Value * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Single?) 0)), L<Single?, Single?> ( v => (Single?) (v.Value * (180 / Math.PI))) },

				{ M(() => Sql.Sinh(0)), L<F?,F?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Tanh(0)), L<F?,F?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v))) },

				{ M(() => Sql.Truncate(0m)),  L<M?,M?>( v => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v)) },
				{ M(() => Sql.Truncate(0.0)), L<F?,F?>( v => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v)) },
			}},

			#endregion

			#region Access

			{ ProviderName.Access, new Dictionary<MemberInfo,LambdaExpression> {
				{ M(() => Sql.Stuff   ("",0,0,"")), L<S,I?,I?,S,S>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3)) },
				{ M(() => Sql.PadRight("",0,' ') ), L<S,I?,C?,S>  ((p0,p1,p2)    => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length)) },
				{ M(() => Sql.PadLeft ("",0,' ') ), L<S,I?,C?,S>  ((p0,p1,p2)    => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0) },
				{ M(() => Sql.MakeDateTime(0,0,0)), L<I?,I?,I?,D?>((y,m,d)       => MakeDateTime2(y, m, d))                                   },

				{ M(() => Sql.ConvertTo<String>.From(Guid.Empty)), L<Guid,S>(p => Sql.Lower(Sql.Substring(p.ToString(), 2, 36))) },

				{ M(() => Sql.Ceiling((Decimal)0)), L<Decimal?,Decimal?>(p => -Sql.Floor(-p) ) },
				{ M(() => Sql.Ceiling((Double) 0)), L<Double?, Double?> (p => -Sql.Floor(-p) ) },

				{ M(() => Sql.Cot  (0)    ), L<F?,F?>   ( v    => Sql.Cos(v) / Sql.Sin(v)       ) },
				{ M(() => Sql.Cosh (0)    ), L<F?,F?>   ( v    => (Sql.Exp(v) + Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Log  (0m, 0)), L<M?,M?,M?>((m,n) => Sql.Log(n) / Sql.Log(m)       ) },
				{ M(() => Sql.Log  (0.0,0)), L<F?,F?,F?>((m,n) => Sql.Log(n) / Sql.Log(m)       ) },
				{ M(() => Sql.Log10(0.0)  ), L<F?,F?>   ( n    => Sql.Log(n) / Sql.Log(10.0)    ) },

				{ M(() => Sql.Degrees((Decimal?)0)), L<Decimal?,Decimal?>( v => (Decimal?)         (          v.Value  * (180 / (Decimal)Math.PI))) },
				{ M(() => Sql.Degrees((Double?) 0)), L<Double?, Double?> ( v => (Double?)          (          v.Value  * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int16?)  0)), L<Int16?,  Int16?>  ( v => (Int16?)  AccessInt(AccessInt(v.Value) * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int32?)  0)), L<Int32?,  Int32?>  ( v => (Int32?)  AccessInt(AccessInt(v.Value) * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Int64?)  0)), L<Int64?,  Int64?>  ( v => (Int64?)  AccessInt(AccessInt(v.Value) * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((SByte?)  0)), L<SByte?,  SByte?>  ( v => (SByte?)  AccessInt(AccessInt(v.Value) * (180 / Math.PI))) },
				{ M(() => Sql.Degrees((Single?) 0)), L<Single?, Single?> ( v => (Single?)          (          v.Value  * (180 / Math.PI))) },

				{ M(() => Sql.Round      (0m)   ), L<M?,M?>   ( d   => d - Sql.Floor(d) == 0.5m && Sql.Floor(d) % 2 == 0? Sql.Ceiling(d) : AccessRound(d, 0)) },
				{ M(() => Sql.Round      (0.0)  ), L<F?,F?>   ( d   => d - Sql.Floor(d) == 0.5  && Sql.Floor(d) % 2 == 0? Sql.Ceiling(d) : AccessRound(d, 0)) },
				{ M(() => Sql.Round      (0m, 0)), L<M?,I?,M?>((v,p)=>
					p == 1 ? Sql.Round(v * 10) / 10 :
					p == 2 ? Sql.Round(v * 10) / 10 :
					p == 3 ? Sql.Round(v * 10) / 10 :
					p == 4 ? Sql.Round(v * 10) / 10 :
					p == 5 ? Sql.Round(v * 10) / 10 :
					         Sql.Round(v * 10) / 10) },
				{ M(() => Sql.Round      (0.0,0)), L<F?,I?,F?>((v,p)=>
					p == 1 ? Sql.Round(v * 10) / 10 :
					p == 2 ? Sql.Round(v * 10) / 10 :
					p == 3 ? Sql.Round(v * 10) / 10 :
					p == 4 ? Sql.Round(v * 10) / 10 :
					p == 5 ? Sql.Round(v * 10) / 10 :
					         Sql.Round(v * 10) / 10) },
				{ M(() => Sql.RoundToEven(0m)   ), L<M?,M?>   ( v   => AccessRound(v, 0))},
				{ M(() => Sql.RoundToEven(0.0)  ), L<F?,F?>   ( v   => AccessRound(v, 0))},
				{ M(() => Sql.RoundToEven(0m, 0)), L<M?,I?,M?>((v,p)=> AccessRound(v, p))},
				{ M(() => Sql.RoundToEven(0.0,0)), L<F?,I?,F?>((v,p)=> AccessRound(v, p))},

				{ M(() => Sql.Sinh(0)), L<F?,F?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / 2) },
				{ M(() => Sql.Tanh(0)), L<F?,F?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v))) },

				{ M(() => Sql.Truncate(0m)),  L<M?,M?>( v => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v)) },
				{ M(() => Sql.Truncate(0.0)), L<F?,F?>( v => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v)) },
			}},

			#endregion
		};*/

		#region Sql specific

		[CLSCompliant(false)]
		[SqlFunction("RTrim", 0)]
		public static string TrimRight(string str, char[] trimChars)
		{
			return str == null ? null : str.TrimEnd(trimChars);
		}

		[CLSCompliant(false)]
		[SqlFunction("LTrim", 0)]
		public static string TrimLeft(string str, char[] trimChars)
		{
			return str == null ? null : str.TrimStart(trimChars);
		}

		#endregion

		#region Provider specific functions

		[SqlFunction]
		static int? ConvertToCaseCompareTo(string str, string value)
		{
			return str == null || value == null ? (int?)null : str.CompareTo(value);
		}

		// Access, DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite
		//
		[SqlFunction]
		static string AltStuff(string str, int? startLocation, int? length, string value)
		{
			return Sql.Stuff(str, startLocation, length, value);
		}

		// DB2
		//
		[SqlFunction]
		static string VarChar(object obj, int? size)
		{
			return obj.ToString();
		}

		// DB2
		//
		[SqlFunction]
		static string Hex(Guid? guid)
		{
			return guid == null ? null : guid.ToString();
		}

#pragma warning disable 3019

		// DB2, PostgreSQL, Access, MS SQL, SqlCe
		//
		[CLSCompliant(false)]
		[SqlFunction]
		[SqlFunction(ProviderName.DB2,        "Repeat")]
		[SqlFunction(ProviderName.PostgreSQL, "Repeat")]
		[SqlFunction(ProviderName.Access,     "String", 1, 0)]
		static string Replicate(string str, int? count)
		{
			if (str == null || count == null)
				return null;

			var sb = new StringBuilder(str.Length * count.Value);

			for (var i = 0; i < count; i++)
				sb.Append(str);

			return sb.ToString();
		}

		[CLSCompliant(false)]
		[SqlFunction]
		[SqlFunction(ProviderName.DB2,        "Repeat")]
		[SqlFunction(ProviderName.PostgreSQL, "Repeat")]
		[SqlFunction(ProviderName.Access,     "String", 1, 0)]
		static string Replicate(char? ch, int? count)
		{
			if (ch == null || count == null)
				return null;

			var sb = new StringBuilder(count.Value);

			for (var i = 0; i < count; i++)
				sb.Append(ch);

			return sb.ToString();
		}

		// MSSQL
		//
		[SqlFunction]
		static DateTime? DateAdd(Sql.DateParts part, int? number, int? days)
		{
			return days == null ? null : Sql.DateAdd(part, number, new DateTime(1900, 1, days.Value + 1));
		}

		// MSSQL
		//
		[SqlFunction] static Decimal? Round(Decimal? value, int precision, int mode) { return 0; }
		[SqlFunction] static Double?  Round(Double?  value, int precision, int mode) { return 0; }

		// Access
		//
		[SqlFunction(ProviderName.Access, "DateSerial")]
		static DateTime? MakeDateTime2(int? year, int? month, int? day)
		{
			return year == null || month == null || day == null?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value);
		}

		// Access
		//
		[CLSCompliant(false)]
		[SqlFunction("Int", 0)]
		static T AccessInt<T>(T value)
		{
			return value;
		}

		// Access
		//
		[CLSCompliant(false)]
		[SqlFunction("Round", 0, 1)]
		static T AccessRound<T>(T value, int? precision) { return value; }

		// Firebird
		//
		[SqlFunction("PI", ServerSideOnly = true)] static decimal DecimalPI() { return (decimal)Math.PI; }
		[SqlFunction("PI", ServerSideOnly = true)] static double  DoublePI () { return          Math.PI; }

		// Informix
		//
		[SqlFunction]
		static DateTime? Mdy(int? month, int? day, int? year)
		{
			return year == null || month == null || day == null ?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value);
		}

		#endregion

		#endregion
	}
}
