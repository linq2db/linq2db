﻿using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

#if !SILVERLIGHT && !NETFX_CORE
using System.Data.SqlTypes;
#endif

using JetBrains.Annotations;

#region ReSharper disables
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable RedundantCast
// ReSharper disable PossibleInvalidOperationException
// ReSharper disable CSharpWarnings::CS0693
// ReSharper disable RedundantToStringCall
// ReSharper disable RedundantLambdaParameterType
#endregion

namespace LinqToDB.Linq
{
	using Common;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;

	[PublicAPI]
	public static class Expressions
	{
		#region MapMember

		public static void MapMember(MemberInfo memberInfo, LambdaExpression expression)
		{
			MapMember("", memberInfo, expression);
		}

		public static void MapMember(MemberInfo memberInfo, IExpressionInfo expressionInfo)
		{
			MapMember("", memberInfo, expressionInfo);
		}

		public static void MapMember(string providerName, MemberInfo memberInfo, LambdaExpression expression)
		{
			Dictionary<MemberInfo,IExpressionInfo> dic;

			if (!Members.TryGetValue(providerName, out dic))
				Members.Add(providerName, dic = new Dictionary<MemberInfo,IExpressionInfo>());

			var expr = new LazyExpressionInfo();

			expr.SetExpression(expression);

			dic[memberInfo] = expr;

			_checkUserNamespace = false;
		}

		public static void MapMember(string providerName, MemberInfo memberInfo, IExpressionInfo expressionInfo)
		{
			Dictionary<MemberInfo,IExpressionInfo> dic;

			if (!Members.TryGetValue(providerName, out dic))
				Members.Add(providerName, dic = new Dictionary<MemberInfo,IExpressionInfo>());

			dic[memberInfo] = expressionInfo;

			_checkUserNamespace = false;
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

		public static void MapMember<TR>               (string providerName, Expression<Func<TR>>                memberInfo, Expression<Func<TR>>                expression) { MapMember(providerName, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<TR>               (                     Expression<Func<TR>>                memberInfo, Expression<Func<TR>>                expression) { MapMember("",           MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,TR>            (string providerName, Expression<Func<T1,TR>>             memberInfo, Expression<Func<T1,TR>>             expression) { MapMember(providerName, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,TR>            (                     Expression<Func<T1,TR>>             memberInfo, Expression<Func<T1,TR>>             expression) { MapMember("",           MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,TR>         (string providerName, Expression<Func<T1,T2,TR>>          memberInfo, Expression<Func<T1,T2,TR>>          expression) { MapMember(providerName, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,TR>         (                     Expression<Func<T1,T2,TR>>          memberInfo, Expression<Func<T1,T2,TR>>          expression) { MapMember("",           MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,TR>      (string providerName, Expression<Func<T1,T2,T3,TR>>       memberInfo, Expression<Func<T1,T2,T3,TR>>       expression) { MapMember(providerName, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,TR>      (                     Expression<Func<T1,T2,T3,TR>>       memberInfo, Expression<Func<T1,T2,T3,TR>>       expression) { MapMember("",           MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,TR>   (string providerName, Expression<Func<T1,T2,T3,T4,TR>>    memberInfo, Expression<Func<T1,T2,T3,T4,TR>>    expression) { MapMember(providerName, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,TR>   (                     Expression<Func<T1,T2,T3,T4,TR>>    memberInfo, Expression<Func<T1,T2,T3,T4,TR>>    expression) { MapMember("",           MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,T5,TR>(string providerName, Expression<Func<T1,T2,T3,T4,T5,TR>> memberInfo, Expression<Func<T1,T2,T3,T4,T5,TR>> expression) { MapMember(providerName, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,T5,TR>(                     Expression<Func<T1,T2,T3,T4,T5,TR>> memberInfo, Expression<Func<T1,T2,T3,T4,T5,TR>> expression) { MapMember("",           MemberHelper.GetMemberInfo(memberInfo), expression); }

		#endregion

		#region IGenericInfoProvider

		static volatile Dictionary<Type,List<Type[]>> _genericConvertProviders = new Dictionary<Type,List<Type[]>>();

		static bool InitGenericConvertProvider(Type[] types, MappingSchema mappingSchema)
		{
			var changed = false;

			lock (_genericConvertProviders)
			{
				foreach (var type in _genericConvertProviders)
				{
					var args = type.Key.GetGenericArgumentsEx();

					if (args.Length == types.Length)
					{
						if (type.Value.Aggregate(false, (cur,ts) => cur || ts.SequenceEqual(types)))
							continue;

						var gtype    = type.Key.MakeGenericType(types);
						var provider = (IGenericInfoProvider)Activator.CreateInstance(gtype);

						provider.SetInfo(new MappingSchema(mappingSchema));

						type.Value.Add(types);

						changed = true;
					}
				}
			}

			return changed;
		}

		public static void SetGenericInfoProvider(Type type)
		{
			if (!type.IsGenericTypeDefinitionEx())
				throw new LinqToDBException("'{0}' must be a generic type.".Args(type));

			if (!typeof(IGenericInfoProvider).IsSameOrParentOf(type))
				throw new LinqToDBException("'{0}' must inherit from 'IGenericInfoProvider'.".Args(type));

			if (!_genericConvertProviders.ContainsKey(type))
				lock (_genericConvertProviders)
					if (!_genericConvertProviders.ContainsKey(type))
						_genericConvertProviders[type] = new List<Type[]>();
		}

		#endregion

		#region Public Members

		static bool _checkUserNamespace = true;

		public static LambdaExpression ConvertMember(MappingSchema mappingSchema, Type objectType, MemberInfo mi)
		{
			if (_checkUserNamespace)
			{
				if (IsUserNamespace(mi.DeclaringType.Namespace))
					return null;

				_checkUserNamespace = false;
			}

			IExpressionInfo expr;

			{
				Dictionary<MemberInfo,IExpressionInfo> dic;

				foreach (var configuration in mappingSchema.ConfigurationList)
					if (Members.TryGetValue(configuration, out dic))
						if (dic.TryGetValue(mi, out expr))
							return expr.GetExpression(mappingSchema);

				Type[] args = null;

				if (mi is MethodInfo)
				{
					var mm = (MethodInfo)mi;

					var isTypeGeneric   = mm.DeclaringType.IsGenericTypeEx() && !mm.DeclaringType.IsGenericTypeDefinitionEx();
					var isMethodGeneric = mm.IsGenericMethod && !mm.IsGenericMethodDefinition;

					if (isTypeGeneric || isMethodGeneric)
					{
						var typeGenericArgs   = isTypeGeneric   ? mm.DeclaringType.GetGenericArgumentsEx() : Array<Type>.Empty;
						var methodGenericArgs = isMethodGeneric ? mm.GetGenericArguments()                 : Array<Type>.Empty;

						args = typeGenericArgs.SequenceEqual(methodGenericArgs) ?
							typeGenericArgs : typeGenericArgs.Concat(methodGenericArgs).ToArray();
					}
				}

				if (args != null && InitGenericConvertProvider(args, mappingSchema))
					foreach (var configuration in mappingSchema.ConfigurationList)
						if (Members.TryGetValue(configuration, out dic))
							if (dic.TryGetValue(mi, out expr))
								return expr.GetExpression(mappingSchema);

				if (!Members[""].TryGetValue(mi, out expr))
				{
					if (mi is MethodInfo && mi.Name == "CompareString" && mi.DeclaringType.FullName == "Microsoft.VisualBasic.CompilerServices.Operators")
					{
						lock (_memberSync)
						{
							if (!Members[""].TryGetValue(mi, out expr))
							{
								expr = new LazyExpressionInfo();

								((LazyExpressionInfo)expr).SetExpression(L<String,String,Boolean,Int32>((s1,s2,b) => b ? string.CompareOrdinal(s1.ToUpper(), s2.ToUpper()) : string.CompareOrdinal(s1, s2)));

								Members[""].Add(mi, expr);
							}
						}
					}
				}
			}

			if (expr == null && objectType != null)
			{
				Dictionary<TypeMember,IExpressionInfo> dic;

				var key = new TypeMember(objectType, mi.Name);

				foreach (var configuration in mappingSchema.ConfigurationList)
					if (_typeMembers.TryGetValue(configuration, out dic))
						if (dic.TryGetValue(key, out expr))
							return expr.GetExpression(mappingSchema);

				_typeMembers[""].TryGetValue(key, out expr);
			}

			return expr == null ? null : expr.GetExpression(mappingSchema);
		}

		#endregion

		#region Function Mapping

		#region Helpers

		static bool IsUserNamespace(string typeNamespace)
		{
			if (typeNamespace == null)
				return true;

			var dotIndex = typeNamespace.IndexOf('.');
			var root     = dotIndex != -1 ? typeNamespace.Substring(0, dotIndex) : typeNamespace;

			// Startup optimization.
			//
			switch (root)
			{
				case "LinqToDB" :
				case "System"   :
				case "Microsoft": return false;
				default         : return true;
			}
		}

		public static MemberInfo M<T>(Expression<Func<T,object>> func)
		{
			return MemberHelper.GetMemberInfo(func);
		}

		public static MemberInfo M<T>(Expression<Func<T>> func)
		{
			return MemberHelper.GetMemberInfo(func);
		}

		public static LambdaExpression L<TR>                   (Expression<Func<TR>>                   func) { return func; }
		public static LambdaExpression L<T1,TR>                (Expression<Func<T1,TR>>                func) { return func; }
		public static LambdaExpression L<T1,T2,TR>             (Expression<Func<T1,T2,TR>>             func) { return func; }
		public static LambdaExpression L<T1,T2,T3,TR>          (Expression<Func<T1,T2,T3,TR>>          func) { return func; }
		public static LambdaExpression L<T1,T2,T3,T4,TR>       (Expression<Func<T1,T2,T3,T4,TR>>       func) { return func; }
		public static LambdaExpression L<T1,T2,T3,T4,T5,TR>    (Expression<Func<T1,T2,T3,T4,T5,TR>>    func) { return func; }
		public static LambdaExpression L<T1,T2,T3,T4,T5,T6,TR> (Expression<Func<T1,T2,T3,T4,T5,T6,TR>> func) { return func; }
		public static LazyExpressionInfo N (Func<LambdaExpression> func) { return new LazyExpressionInfo { Lambda = func }; }

		#endregion

		public class LazyExpressionInfo : IExpressionInfo
		{
			public Func<LambdaExpression> Lambda;

			LambdaExpression _expression;

			public LambdaExpression GetExpression(MappingSchema mappingSchema)
			{
				return _expression ?? (_expression = Lambda());
			}

			public void SetExpression(LambdaExpression expression)
			{
				_expression = expression;
			}
		}

		static Dictionary<string,Dictionary<MemberInfo,IExpressionInfo>> Members
		{
			get
			{
				if (_members == null)
					lock (_memberSync)
						if (_members == null)
							_members = LoadMembers();

				return _members;
			}
		}

		interface ISetInfo
		{
			void SetInfo();
		}

		class GetValueOrDefaultExpressionInfo<T1> : IExpressionInfo, ISetInfo
			where T1 : struct
		{
			static T1? _member = null;

			public LambdaExpression GetExpression(MappingSchema mappingSchema)
			{
				var p = Expression.Parameter(typeof(T1?), "p");

				return Expression.Lambda<Func<T1?,T1>>(
					Expression.Coalesce(p, Expression.Constant(mappingSchema.GetDefaultValue(typeof(T1)))),
					p);
			}

			public void SetInfo()
			{
				_members[""][M(() => _member.GetValueOrDefault() )] = this; // N(() => L<T1?,T1>((T1? obj) => obj ?? default(T1)));
			}
		}

		class GenericInfoProvider<T> : IGenericInfoProvider
		{
			public void SetInfo(MappingSchema mappingSchema)
			{
				if (!typeof(T).IsClassEx() && !typeof(T).IsInterfaceEx() && !typeof(T).IsNullable())
				{
					var gtype    = typeof(GetValueOrDefaultExpressionInfo<>).MakeGenericType(typeof(T));
					var provider = (ISetInfo)Activator.CreateInstance(gtype);

					provider.SetInfo();
				}
			}
		}

		#region Mapping

		static Dictionary<string,Dictionary<MemberInfo,IExpressionInfo>> _members;
		static readonly object                                           _memberSync = new object();

		#region Common

		static readonly Dictionary<MemberInfo,IExpressionInfo> _commonMembers = new Dictionary<MemberInfo,IExpressionInfo>
		{
			#region String

			{ M(() => "".Length               ), N(() => L<String,Int32>                   ((String obj)                              => Sql.Length(obj).Value)) },
			{ M(() => "".Substring  (0)       ), N(() => L<String,Int32,String>            ((String obj,Int32  p0)                    => Sql.Substring(obj, p0 + 1, obj.Length - p0))) },
			{ M(() => "".Substring  (0,0)     ), N(() => L<String,Int32,Int32,String>      ((String obj,Int32  p0,Int32  p1)          => Sql.Substring(obj, p0 + 1, p1))) },
			{ M(() => "".IndexOf    ("")      ), N(() => L<String,String,Int32>            ((String obj,String p0)                    => p0.Length == 0                    ? 0  : (Sql.CharIndex(p0, obj)                      .Value) - 1)) },
			{ M(() => "".IndexOf    ("",0)    ), N(() => L<String,String,Int32,Int32>      ((String obj,String p0,Int32  p1)          => p0.Length == 0 && obj.Length > p1 ? p1 : (Sql.CharIndex(p0, obj,               p1 + 1).Value) - 1)) },
			{ M(() => "".IndexOf    ("",0,0)  ), N(() => L<String,String,Int32,Int32,Int32>((String obj,String p0,Int32  p1,Int32 p2) => p0.Length == 0 && obj.Length > p1 ? p1 : (Sql.CharIndex(p0, Sql.Left(obj, p2), p1)    .Value) - 1)) },
			{ M(() => "".IndexOf    (' ')     ), N(() => L<String,Char,Int32>              ((String obj,Char   p0)                    =>                                          (Sql.CharIndex(p0, obj)                      .Value) - 1)) },
			{ M(() => "".IndexOf    (' ',0)   ), N(() => L<String,Char,Int32,Int32>        ((String obj,Char   p0,Int32  p1)          =>                                          (Sql.CharIndex(p0, obj,               p1 + 1).Value) - 1)) },
			{ M(() => "".IndexOf    (' ',0,0) ), N(() => L<String,Char,Int32,Int32,Int32>  ((String obj,Char   p0,Int32  p1,Int32 p2) =>                                          (Sql.CharIndex(p0, Sql.Left(obj, p2), p1)     ?? 0) - 1)) },
			{ M(() => "".LastIndexOf("")      ), N(() => L<String,String,Int32>            ((String obj,String p0)                    => p0.Length == 0 ? obj.Length - 1 : (Sql.CharIndex(p0, obj)                           .Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(Sql.Reverse(p0), Sql.Reverse(obj))                               .Value) - p0.Length + 1)) },
			{ M(() => "".LastIndexOf("",0)    ), N(() => L<String,String,Int32,Int32>      ((String obj,String p0,Int32  p1)          => p0.Length == 0 ? p1             : (Sql.CharIndex(p0, obj,                    p1 + 1).Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(Sql.Reverse(p0), Sql.Reverse(obj.Substring(p1, obj.Length - p1))).Value) - p0.Length + 1)) },
			{ M(() => "".LastIndexOf("",0,0)  ), N(() => L<String,String,Int32,Int32,Int32>((String obj,String p0,Int32  p1,Int32 p2) => p0.Length == 0 ? p1             : (Sql.CharIndex(p0, Sql.Left(obj, p1 + p2), p1 + 1).Value) == 0 ? -1 :    p1 + p2 - (Sql.CharIndex(Sql.Reverse(p0), Sql.Reverse(obj.Substring(p1, p2)))             .Value) - p0.Length + 1)) },
			{ M(() => "".LastIndexOf(' ')     ), N(() => L<String,Char,Int32>              ((String obj,Char   p0)                    => (Sql.CharIndex(p0, obj)                           .Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(p0, Sql.Reverse(obj))                               .Value))) },
			{ M(() => "".LastIndexOf(' ',0)   ), N(() => L<String,Char,Int32,Int32>        ((String obj,Char   p0,Int32  p1)          => (Sql.CharIndex(p0, obj, p1 + 1)                   .Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(p0, Sql.Reverse(obj.Substring(p1, obj.Length - p1))).Value))) },
			{ M(() => "".LastIndexOf(' ',0,0) ), N(() => L<String,Char,Int32,Int32,Int32>  ((String obj,Char   p0,Int32  p1,Int32 p2) => (Sql.CharIndex(p0, Sql.Left(obj, p1 + p2), p1 + 1).Value) == 0 ? -1 : p1 + p2    - (Sql.CharIndex(p0, Sql.Reverse(obj.Substring(p1, p2)))             .Value))) },
			{ M(() => "".Insert     (0,"")    ), N(() => L<String,Int32,String,String>     ((String obj,Int32  p0,String p1)          => obj.Length == p0 ? obj + p1 : Sql.Stuff(obj, p0 + 1, 0, p1))) },
			{ M(() => "".Remove     (0)       ), N(() => L<String,Int32,String>            ((String obj,Int32  p0)                    => Sql.Left     (obj, p0))) },
			{ M(() => "".Remove     (0,0)     ), N(() => L<String,Int32,Int32,String>      ((String obj,Int32  p0,Int32  p1)          => Sql.Stuff    (obj, p0 + 1, p1, ""))) },
			{ M(() => "".PadLeft    (0)       ), N(() => L<String,Int32,String>            ((String obj,Int32  p0)                    => Sql.PadLeft  (obj, p0, ' '))) },
			{ M(() => "".PadLeft    (0,' ')   ), N(() => L<String,Int32,Char,String>       ((String obj,Int32  p0,Char   p1)          => Sql.PadLeft  (obj, p0, p1))) },
			{ M(() => "".PadRight   (0)       ), N(() => L<String,Int32,String>            ((String obj,Int32  p0)                    => Sql.PadRight (obj, p0, ' '))) },
			{ M(() => "".PadRight   (0,' ')   ), N(() => L<String,Int32,Char,String>       ((String obj,Int32  p0,Char   p1)          => Sql.PadRight (obj, p0, p1))) },
			{ M(() => "".Replace    ("","")   ), N(() => L<String,String,String,String>    ((String obj,String p0,String p1)          => Sql.Replace  (obj, p0, p1))) },
			{ M(() => "".Replace    (' ',' ') ), N(() => L<String,Char,Char,String>        ((String obj,Char   p0,Char   p1)          => Sql.Replace  (obj, p0, p1))) },
			{ M(() => "".Trim       ()        ), N(() => L<String,String>                  ((String obj)                              => Sql.Trim     (obj))) },
			{ M(() => "".TrimEnd    ()        ), N(() => L<String,Char[],String>           ((String obj,Char[] ch)                    =>     TrimRight(obj, ch))) },
			{ M(() => "".TrimStart  ()        ), N(() => L<String,Char[],String>           ((String obj,Char[] ch)                    =>     TrimLeft (obj, ch))) },
			{ M(() => "".ToLower    ()        ), N(() => L<String,String>                  ((String obj)                              => Sql.Lower(obj))) },
			{ M(() => "".ToUpper    ()        ), N(() => L<String,String>                  ((String obj)                              => Sql.Upper(obj))) },
			{ M(() => "".CompareTo  ("")      ), N(() => L<String,String,Int32>            ((String obj,String p0)                    => ConvertToCaseCompareTo(obj, p0).Value)) },
#if !NETFX_CORE && !NETSTANDARD
			{ M(() => "".CompareTo  (1)       ), N(() => L<String,Object,Int32>            ((String obj,Object p0)                    => ConvertToCaseCompareTo(obj, p0.ToString()).Value)) },
#endif

			{ M(() => string.Concat((object)null)                           ), N(() => L<Object,String>                     ((Object p0)                               => p0.ToString()))           },
			{ M(() => string.Concat((object)null,(object)null)              ), N(() => L<Object,Object,String>              ((Object p0,Object p1)                     => p0.ToString() + p1))      },
			{ M(() => string.Concat((object)null,(object)null,(object)null) ), N(() => L<Object,Object,Object,String>       ((Object p0,Object p1,Object p2)           => p0.ToString() + p1 + p2)) },
			{ M(() => string.Concat((object[])null)                         ), N(() => L<Object[],String>                   ((Object[] ps)                             => Sql.Concat(ps)))          },
			{ M(() => string.Concat("","")                                  ), N(() => L<String,String,String>              ((String p0,String p1)                     => p0 + p1))                 },
			{ M(() => string.Concat("","","")                               ), N(() => L<String,String,String,String>       ((String p0,String p1,String p2)           => p0 + p1 + p2))            },
			{ M(() => string.Concat("","","","")                            ), N(() => L<String,String,String,String,String>((String p0,String p1,String p2,String p3) => p0 + p1 + p2 + p3))       },
			{ M(() => string.Concat((string[])null)                         ), N(() => L<String[],String>                   ((String[] ps)                             => Sql.Concat(ps)))          },

			{ M(() => string.IsNullOrEmpty ("")    ),                                         N(() => L<String,Boolean>                                        ((String p0)                                                         => p0 == null || p0.Length == 0)) },
			{ M(() => string.CompareOrdinal("","")),                                          N(() => L<String,String,Int32>                                   ((String s1,String s2)                                               => s1.CompareTo(s2))) },
			{ M(() => string.CompareOrdinal("",0,"",0,0)),                                    N(() => L<String,Int32,String,Int32,Int32,Int32>                 ((String s1,Int32 i1,String s2,Int32 i2,Int32 l)                     => s1.Substring(i1, l).CompareTo(s2.Substring(i2, l)))) },
			{ M(() => string.Compare       ("","")),                                          N(() => L<String,String,Int32>                                   ((String s1,String s2)                                               => s1.CompareTo(s2))) },
			{ M(() => string.Compare       ("",0,"",0,0)),                                    N(() => L<String,Int32,String,Int32,Int32,Int32>                 ((String s1,Int32 i1,String s2,Int32 i2,Int32 l)                     => s1.Substring(i1,l).CompareTo(s2.Substring(i2,l)))) },
#if !SILVERLIGHT && !NETFX_CORE										                                 														           															            
			{ M(() => string.Compare       ("","",true)),                                     N(() => L<String,String,Boolean,Int32>                           ((String s1,String s2,Boolean b)                                     => b ? s1.ToLower().CompareTo(s2.ToLower()) : s1.CompareTo(s2))) },
#endif						
#if !SILVERLIGHT && !NETFX_CORE && !NETSTANDARD
			{ M(() => string.Compare       ("",0,"",0,0,true)),                               N(() => L<String,Int32,String,Int32,Int32,Boolean,Int32>         ((String s1,Int32 i1,String s2,Int32 i2,Int32 l,Boolean b)           => b ? s1.Substring(i1,l).ToLower().CompareTo(s2.Substring(i2, l).ToLower()) : s1.Substring(i1, l).CompareTo(s2.Substring(i2, l)))) },
#endif
			{ M(() => string.Compare       ("",0,"",0,0,StringComparison.OrdinalIgnoreCase)), N(() => L<String,Int32,String,Int32,Int32,StringComparison,Int32>((String s1,Int32 i1,String s2,Int32 i2,Int32 l,StringComparison sc) => sc == StringComparison.CurrentCultureIgnoreCase || sc==StringComparison.OrdinalIgnoreCase ? s1.Substring(i1,l).ToLower().CompareTo(s2.Substring(i2, l).ToLower()) : s1.Substring(i1, l).CompareTo(s2.Substring(i2, l)))) },
			{ M(() => string.Compare       ("","",StringComparison.OrdinalIgnoreCase)),       N(() => L<String,String,StringComparison,Int32>                  ((String s1,String s2,StringComparison sc)                           => sc == StringComparison.CurrentCultureIgnoreCase || sc==StringComparison.OrdinalIgnoreCase ? s1.ToLower().CompareTo(s2.ToLower()) : s1.CompareTo(s2))) },

			{ M(() => AltStuff("",0,0,"")), N(() => L<String,Int32?,Int32?,String,String>((String p0,Int32? p1,Int32 ?p2,String p3) => Sql.Left(p0, p1 - 1) + p3 + Sql.Right(p0, p0.Length - (p1 + p2 - 1)))) },

			#endregion

			#region Binary

#pragma warning disable 1720
			{ M(() => ((Binary)null).Length ), N(() => L<Binary,Int32>((Binary obj) => Sql.Length(obj).Value)) },
#pragma warning restore 1720

			#endregion

			#region DateTime

			{ M(() => Sql.GetDate()                  ), N(() => L<DateTime>                  (()                        => Sql.CurrentTimestamp2)) },
			{ M(() => DateTime.Now                   ), N(() => L<DateTime>                  (()                        => Sql.CurrentTimestamp2)) },

			{ M(() => DateTime.Now.Year              ), N(() => L<DateTime,Int32>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Year,        obj).Value    )) },
			{ M(() => DateTime.Now.Month             ), N(() => L<DateTime,Int32>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Month,       obj).Value    )) },
			{ M(() => DateTime.Now.DayOfYear         ), N(() => L<DateTime,Int32>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.DayOfYear,   obj).Value    )) },
			{ M(() => DateTime.Now.Day               ), N(() => L<DateTime,Int32>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Day,         obj).Value    )) },
			{ M(() => DateTime.Now.DayOfWeek         ), N(() => L<DateTime,Int32>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.WeekDay,     obj).Value - 1)) },
			{ M(() => DateTime.Now.Hour              ), N(() => L<DateTime,Int32>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Hour,        obj).Value    )) },
			{ M(() => DateTime.Now.Minute            ), N(() => L<DateTime,Int32>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Minute,      obj).Value    )) },
			{ M(() => DateTime.Now.Second            ), N(() => L<DateTime,Int32>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Second,      obj).Value    )) },
			{ M(() => DateTime.Now.Millisecond       ), N(() => L<DateTime,Int32>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Millisecond, obj).Value    )) },
			{ M(() => DateTime.Now.Date              ), N(() => L<DateTime,DateTime>         ((DateTime obj)            => Sql.Convert2(Sql.Date,                  obj)          )) },
			{ M(() => DateTime.Now.TimeOfDay         ), N(() => L<DateTime,TimeSpan>         ((DateTime obj)            => Sql.DateToTime(Sql.Convert2(Sql.Time,   obj)).Value   )) },
			{ M(() => DateTime.Now.AddYears       (0)), N(() => L<DateTime,Int32,DateTime>   ((DateTime obj,Int32 p0)   => Sql.DateAdd(Sql.DateParts.Year,        p0, obj).Value )) },
			{ M(() => DateTime.Now.AddMonths      (0)), N(() => L<DateTime,Int32,DateTime>   ((DateTime obj,Int32 p0)   => Sql.DateAdd(Sql.DateParts.Month,       p0, obj).Value )) },
			{ M(() => DateTime.Now.AddDays        (0)), N(() => L<DateTime,Double,DateTime>  ((DateTime obj,Double p0)  => Sql.DateAdd(Sql.DateParts.Day,         p0, obj).Value )) },
			{ M(() => DateTime.Now.AddHours       (0)), N(() => L<DateTime,Double,DateTime>  ((DateTime obj,Double p0)  => Sql.DateAdd(Sql.DateParts.Hour,        p0, obj).Value )) },
			{ M(() => DateTime.Now.AddMinutes     (0)), N(() => L<DateTime,Double,DateTime>  ((DateTime obj,Double p0)  => Sql.DateAdd(Sql.DateParts.Minute,      p0, obj).Value )) },
			{ M(() => DateTime.Now.AddSeconds     (0)), N(() => L<DateTime,Double,DateTime>  ((DateTime obj,Double p0)  => Sql.DateAdd(Sql.DateParts.Second,      p0, obj).Value )) },
			{ M(() => DateTime.Now.AddMilliseconds(0)), N(() => L<DateTime,Double,DateTime>  ((DateTime obj,Double p0)  => Sql.DateAdd(Sql.DateParts.Millisecond, p0, obj).Value )) },
			{ M(() => new DateTime(0, 0, 0)          ), N(() => L<Int32,Int32,Int32,DateTime>((Int32 y,Int32 m,Int32 d) => Sql.MakeDateTime(y, m, d).Value                       )) },

			{ M(() => Sql.MakeDateTime(0, 0, 0)          ), N(() => L<Int32?,Int32?,Int32?,DateTime?>                     ((Int32? y,Int32? m,Int32? d)                             => (DateTime?)Sql.Convert(Sql.Date, y.ToString() + "-" + m.ToString() + "-" + d.ToString()))) },
			{ M(() => new DateTime    (0, 0, 0, 0, 0, 0) ), N(() => L<Int32,Int32,Int32,Int32,Int32,Int32,DateTime>       ((Int32  y,Int32  m,Int32  d,Int32  h,Int32  mm,Int32 s)  => Sql.MakeDateTime(y, m, d, h, mm, s).Value )) },
			{ M(() => Sql.MakeDateTime(0, 0, 0, 0, 0, 0) ), N(() => L<Int32?,Int32?,Int32?,Int32?,Int32?,Int32?,DateTime?>((Int32? y,Int32? m,Int32? d,Int32? h,Int32? mm,Int32? s) => (DateTime?)Sql.Convert(Sql.DateTime2,
				y.ToString() + "-" + m.ToString() + "-" + d.ToString() + " " +
				h.ToString() + ":" + mm.ToString() + ":" + s.ToString()))) },

			#endregion

			#region Parse

			{ M(() => Boolean. Parse("")), N(() => L<String,Boolean> ((String p0) => Sql.ConvertTo<Boolean>. From(p0))) },
			{ M(() => Byte.    Parse("")), N(() => L<String,Byte>    ((String p0) => Sql.ConvertTo<Byte>.    From(p0))) },
#if !SILVERLIGHT && !NETFX_CORE || NETSTANDARD
			{ M(() => Char.    Parse("")), N(() => L<String,Char>    ((String p0) => Sql.ConvertTo<Char>.    From(p0))) },
#endif
			{ M(() => DateTime.Parse("")), N(() => L<String,DateTime>((String p0) => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Decimal. Parse("")), N(() => L<String,Decimal> ((String p0) => Sql.ConvertTo<Decimal>. From(p0))) },
			{ M(() => Double.  Parse("")), N(() => L<String,Double>  ((String p0) => Sql.ConvertTo<Double>.  From(p0))) },
			{ M(() => Int16.   Parse("")), N(() => L<String,Int16>   ((String p0) => Sql.ConvertTo<Int16>.   From(p0))) },
			{ M(() => Int32.   Parse("")), N(() => L<String,Int32>   ((String p0) => Sql.ConvertTo<Int32>.   From(p0))) },
			{ M(() => Int64.   Parse("")), N(() => L<String,Int64>   ((String p0) => Sql.ConvertTo<Int64>.   From(p0))) },
			{ M(() => SByte.   Parse("")), N(() => L<String,SByte>   ((String p0) => Sql.ConvertTo<SByte>.   From(p0))) },
			{ M(() => Single.  Parse("")), N(() => L<String,Single>  ((String p0) => Sql.ConvertTo<Single>.  From(p0))) },
			{ M(() => UInt16.  Parse("")), N(() => L<String,UInt16>  ((String p0) => Sql.ConvertTo<UInt16>.  From(p0))) },
			{ M(() => UInt32.  Parse("")), N(() => L<String,UInt32>  ((String p0) => Sql.ConvertTo<UInt32>.  From(p0))) },
			{ M(() => UInt64.  Parse("")), N(() => L<String,UInt64>  ((String p0) => Sql.ConvertTo<UInt64>.  From(p0))) },

			#endregion

			#region ToString

//			{ M(() => ((Boolean)true).ToString()), N(() => L<Boolean, String>((Boolean p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((Byte)    0)  .ToString()), N(() => L<Byte,    String>((Byte    p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((Char)   '0') .ToString()), N(() => L<Char,    String>((Char    p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((Decimal) 0)  .ToString()), N(() => L<Decimal, String>((Decimal p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((Double)  0)  .ToString()), N(() => L<Double,  String>((Double  p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((Int16)   0)  .ToString()), N(() => L<Int16,   String>((Int16   p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((Int32)   0)  .ToString()), N(() => L<Int32,   String>((Int32   p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((Int64)   0)  .ToString()), N(() => L<Int64,   String>((Int64   p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((SByte)   0)  .ToString()), N(() => L<SByte,   String>((SByte   p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((Single)  0)  .ToString()), N(() => L<Single,  String>((Single  p0) => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => ((String) "0") .ToString()), N(() => L<String,  String>((String  p0) => p0                            )) },
//			{ M(() => ((UInt16)  0)  .ToString()), N(() => L<UInt16,  String>((UInt16  p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((UInt32)  0)  .ToString()), N(() => L<UInt32,  String>((UInt32  p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((UInt64)  0)  .ToString()), N(() => L<UInt64,  String>((UInt64  p0) => Sql.ConvertTo<string>.From(p0))) },

			#endregion

			#region Convert

			#region ToBoolean

			{ M(() => Convert.ToBoolean((Boolean)true)), N(() => L<Boolean, Boolean>((Boolean  p0) => Sql.ConvertTo<Boolean>.From(p0))) },
			{ M(() => Convert.ToBoolean((Byte)    0)  ), N(() => L<Byte,    Boolean>((Byte     p0) => Sql.ConvertTo<Boolean>.From(p0))) },
#if !NETSTANDARD
			{ M(() => Convert.ToBoolean((Char)   '0') ), N(() => L<Char,    Boolean>((Char     p0) => Sql.ConvertTo<Boolean>.From(p0))) },
#if !SILVERLIGHT
			{ M(() => Convert.ToBoolean(DateTime.Now) ), N(() => L<DateTime,Boolean>((DateTime p0) => Sql.ConvertTo<Boolean>.From(p0))) },
#endif
#endif
			{ M(() => Convert.ToBoolean((Decimal) 0)  ), N(() => L<Decimal, Boolean>((Decimal  p0) => Sql.ConvertTo<Boolean>.From(p0))) },
			{ M(() => Convert.ToBoolean((Double)  0)  ), N(() => L<Double,  Boolean>((Double   p0) => Sql.ConvertTo<Boolean>.From(p0))) },
			{ M(() => Convert.ToBoolean((Int16)   0)  ), N(() => L<Int16,   Boolean>((Int16    p0) => Sql.ConvertTo<Boolean>.From(p0))) },
			{ M(() => Convert.ToBoolean((Int32)   0)  ), N(() => L<Int32,   Boolean>((Int32    p0) => Sql.ConvertTo<Boolean>.From(p0))) },
			{ M(() => Convert.ToBoolean((Int64)   0)  ), N(() => L<Int64,   Boolean>((Int64    p0) => Sql.ConvertTo<Boolean>.From(p0))) },
			{ M(() => Convert.ToBoolean((Object)  0)  ), N(() => L<Object,  Boolean>((Object   p0) => Sql.ConvertTo<Boolean>.From(p0))) },
			{ M(() => Convert.ToBoolean((SByte)   0)  ), N(() => L<SByte,   Boolean>((SByte    p0) => Sql.ConvertTo<Boolean>.From(p0))) },
			{ M(() => Convert.ToBoolean((Single)  0)  ), N(() => L<Single,  Boolean>((Single   p0) => Sql.ConvertTo<Boolean>.From(p0))) },
			{ M(() => Convert.ToBoolean((String) "0") ), N(() => L<String,  Boolean>((String   p0) => Sql.ConvertTo<Boolean>.From(p0))) },
			{ M(() => Convert.ToBoolean((UInt16)  0)  ), N(() => L<UInt16,  Boolean>((UInt16   p0) => Sql.ConvertTo<Boolean>.From(p0))) },
			{ M(() => Convert.ToBoolean((UInt32)  0)  ), N(() => L<UInt32,  Boolean>((UInt32   p0) => Sql.ConvertTo<Boolean>.From(p0))) },
			{ M(() => Convert.ToBoolean((UInt64)  0)  ), N(() => L<UInt64,  Boolean>((UInt64   p0) => Sql.ConvertTo<Boolean>.From(p0))) },

			#endregion

			#region ToByte

			{ M(() => Convert.ToByte((Boolean)true)), N(() => L<Boolean, Byte>((Boolean  p0) => Sql.ConvertTo<Byte>.From(p0))) },
			{ M(() => Convert.ToByte((Byte)    0)  ), N(() => L<Byte,    Byte>((Byte     p0) => Sql.ConvertTo<Byte>.From(p0))) },
#if !NETSTANDARD
			{ M(() => Convert.ToByte((Char)   '0') ), N(() => L<Char,    Byte>((Char     p0) => Sql.ConvertTo<Byte>.From(p0))) },
#if !SILVERLIGHT
			{ M(() => Convert.ToByte(DateTime.Now) ), N(() => L<DateTime,Byte>((DateTime p0) => Sql.ConvertTo<Byte>.From(p0))) },
#endif
#endif
			{ M(() => Convert.ToByte((Decimal) 0)  ), N(() => L<Decimal, Byte>((Decimal  p0) => Sql.ConvertTo<Byte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToByte((Double)  0)  ), N(() => L<Double,  Byte>((Double   p0) => Sql.ConvertTo<Byte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToByte((Int16)   0)  ), N(() => L<Int16,   Byte>((Int16    p0) => Sql.ConvertTo<Byte>.From(p0))) },
			{ M(() => Convert.ToByte((Int32)   0)  ), N(() => L<Int32,   Byte>((Int32    p0) => Sql.ConvertTo<Byte>.From(p0))) },
			{ M(() => Convert.ToByte((Int64)   0)  ), N(() => L<Int64,   Byte>((Int64    p0) => Sql.ConvertTo<Byte>.From(p0))) },
			{ M(() => Convert.ToByte((Object)  0)  ), N(() => L<Object,  Byte>((Object   p0) => Sql.ConvertTo<Byte>.From(p0))) },
			{ M(() => Convert.ToByte((SByte)   0)  ), N(() => L<SByte,   Byte>((SByte    p0) => Sql.ConvertTo<Byte>.From(p0))) },
			{ M(() => Convert.ToByte((Single)  0)  ), N(() => L<Single,  Byte>((Single   p0) => Sql.ConvertTo<Byte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToByte((String) "0") ), N(() => L<String,  Byte>((String   p0) => Sql.ConvertTo<Byte>.From(p0))) },
			{ M(() => Convert.ToByte((UInt16)  0)  ), N(() => L<UInt16,  Byte>((UInt16   p0) => Sql.ConvertTo<Byte>.From(p0))) },
			{ M(() => Convert.ToByte((UInt32)  0)  ), N(() => L<UInt32,  Byte>((UInt32   p0) => Sql.ConvertTo<Byte>.From(p0))) },
			{ M(() => Convert.ToByte((UInt64)  0)  ), N(() => L<UInt64,  Byte>((UInt64   p0) => Sql.ConvertTo<Byte>.From(p0))) },

			#endregion

			#region ToChar

#if !SILVERLIGHT && !NETSTANDARD
			{ M(() => Convert.ToChar((Boolean)true)), N(() => L<Boolean, Char>((Boolean  p0) => Sql.ConvertTo<Char>.From(p0))) },
#endif
			{ M(() => Convert.ToChar((Byte)    0)  ), N(() => L<Byte,    Char>((Byte     p0) => Sql.ConvertTo<Char>.From(p0))) },
#if !NETSTANDARD
			{ M(() => Convert.ToChar((Char)   '0') ), N(() => L<Char,    Char>((Char     p0) => p0                          )) },
#if !SILVERLIGHT
			{ M(() => Convert.ToChar(DateTime.Now) ), N(() => L<DateTime,Char>((DateTime p0) => Sql.ConvertTo<Char>.From(p0))) },
#endif
			{ M(() => Convert.ToChar((Decimal) 0)  ), N(() => L<Decimal, Char>((Decimal  p0) => Sql.ConvertTo<Char>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToChar((Double)  0)  ), N(() => L<Double,  Char>((Double   p0) => Sql.ConvertTo<Char>.From(Sql.RoundToEven(p0)))) },
#endif
			{ M(() => Convert.ToChar((Int16)   0)  ), N(() => L<Int16,   Char>((Int16    p0) => Sql.ConvertTo<Char>.From(p0))) },
			{ M(() => Convert.ToChar((Int32)   0)  ), N(() => L<Int32,   Char>((Int32    p0) => Sql.ConvertTo<Char>.From(p0))) },
			{ M(() => Convert.ToChar((Int64)   0)  ), N(() => L<Int64,   Char>((Int64    p0) => Sql.ConvertTo<Char>.From(p0))) },
			{ M(() => Convert.ToChar((Object)  0)  ), N(() => L<Object,  Char>((Object   p0) => Sql.ConvertTo<Char>.From(p0))) },
			{ M(() => Convert.ToChar((SByte)   0)  ), N(() => L<SByte,   Char>((SByte    p0) => Sql.ConvertTo<Char>.From(p0))) },
#if !NETSTANDARD
			{ M(() => Convert.ToChar((Single)  0)  ), N(() => L<Single,  Char>((Single   p0) => Sql.ConvertTo<Char>.From(Sql.RoundToEven(p0)))) },
#endif
			{ M(() => Convert.ToChar((String) "0") ), N(() => L<String,  Char>((String   p0) => Sql.ConvertTo<Char>.From(p0))) },
			{ M(() => Convert.ToChar((UInt16)  0)  ), N(() => L<UInt16,  Char>((UInt16   p0) => Sql.ConvertTo<Char>.From(p0))) },
			{ M(() => Convert.ToChar((UInt32)  0)  ), N(() => L<UInt32,  Char>((UInt32   p0) => Sql.ConvertTo<Char>.From(p0))) },
			{ M(() => Convert.ToChar((UInt64)  0)  ), N(() => L<UInt64,  Char>((UInt64   p0) => Sql.ConvertTo<Char>.From(p0))) },

			#endregion

			#region ToDateTime

			{ M(() => Convert.ToDateTime((Object)  0)  ), N(() => L<Object,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((String) "0") ), N(() => L<String,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
#if !SILVERLIGHT && !NETSTANDARD
			{ M(() => Convert.ToDateTime((Boolean)true)), N(() => L<Boolean, DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((Byte)    0)  ), N(() => L<Byte,    DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((Char)   '0') ), N(() => L<Char,    DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime(DateTime.Now) ), N(() => L<DateTime,DateTime>(p0 => p0                              )) },
			{ M(() => Convert.ToDateTime((Decimal) 0)  ), N(() => L<Decimal, DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((Double)  0)  ), N(() => L<Double,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((Int16)   0)  ), N(() => L<Int16,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((Int32)   0)  ), N(() => L<Int32,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((Int64)   0)  ), N(() => L<Int64,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((SByte)   0)  ), N(() => L<SByte,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((Single)  0)  ), N(() => L<Single,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((UInt16)  0)  ), N(() => L<UInt16,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((UInt32)  0)  ), N(() => L<UInt32,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((UInt64)  0)  ), N(() => L<UInt64,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
#endif

			#endregion

			#region ToDecimal

			{ M(() => Convert.ToDecimal((Boolean)true)), N(() => L<Boolean, Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((Byte)    0)  ), N(() => L<Byte,    Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
#if !NETSTANDARD
			{ M(() => Convert.ToDecimal((Char)   '0') ), N(() => L<Char,    Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal(DateTime.Now) ), N(() => L<DateTime,Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
#endif
			{ M(() => Convert.ToDecimal((Decimal) 0)  ), N(() => L<Decimal, Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((Double)  0)  ), N(() => L<Double,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((Int16)   0)  ), N(() => L<Int16,   Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((Int32)   0)  ), N(() => L<Int32,   Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((Int64)   0)  ), N(() => L<Int64,   Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((Object)  0)  ), N(() => L<Object,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((SByte)   0)  ), N(() => L<SByte,   Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((Single)  0)  ), N(() => L<Single,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((String) "0") ), N(() => L<String,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((UInt16)  0)  ), N(() => L<UInt16,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((UInt32)  0)  ), N(() => L<UInt32,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((UInt64)  0)  ), N(() => L<UInt64,  Decimal>(p0 => Sql.ConvertTo<Decimal>.From(p0))) },

			#endregion

			#region ToDouble

			{ M(() => Convert.ToDouble((Boolean)true)), N(() => L<Boolean, Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
			{ M(() => Convert.ToDouble((Byte)    0)  ), N(() => L<Byte,    Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
#if !NETSTANDARD
			{ M(() => Convert.ToDouble((Char)   '0') ), N(() => L<Char,    Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
#if !SILVERLIGHT
			{ M(() => Convert.ToDouble(DateTime.Now) ), N(() => L<DateTime,Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
#endif
#endif
			{ M(() => Convert.ToDouble((Decimal) 0)  ), N(() => L<Decimal, Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
			{ M(() => Convert.ToDouble((Double)  0)  ), N(() => L<Double,  Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
			{ M(() => Convert.ToDouble((Int16)   0)  ), N(() => L<Int16,   Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
			{ M(() => Convert.ToDouble((Int32)   0)  ), N(() => L<Int32,   Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
			{ M(() => Convert.ToDouble((Int64)   0)  ), N(() => L<Int64,   Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
			{ M(() => Convert.ToDouble((Object)  0)  ), N(() => L<Object,  Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
			{ M(() => Convert.ToDouble((SByte)   0)  ), N(() => L<SByte,   Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
			{ M(() => Convert.ToDouble((Single)  0)  ), N(() => L<Single,  Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
			{ M(() => Convert.ToDouble((String) "0") ), N(() => L<String,  Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
			{ M(() => Convert.ToDouble((UInt16)  0)  ), N(() => L<UInt16,  Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
			{ M(() => Convert.ToDouble((UInt32)  0)  ), N(() => L<UInt32,  Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },
			{ M(() => Convert.ToDouble((UInt64)  0)  ), N(() => L<UInt64,  Double>(p0 => Sql.ConvertTo<Double>.From(p0))) },

			#endregion

			#region ToInt64

			{ M(() => Convert.ToInt64((Boolean)true)), N(() => L<Boolean, Int64>(p0 => Sql.ConvertTo<Int64>.From(p0))) },
			{ M(() => Convert.ToInt64((Byte)    0)  ), N(() => L<Byte,    Int64>(p0 => Sql.ConvertTo<Int64>.From(p0))) },
#if !NETSTANDARD
			{ M(() => Convert.ToInt64((Char)   '0') ), N(() => L<Char,    Int64>(p0 => Sql.ConvertTo<Int64>.From(p0))) },
#if !SILVERLIGHT
			{ M(() => Convert.ToInt64(DateTime.Now) ), N(() => L<DateTime,Int64>(p0 => Sql.ConvertTo<Int64>.From(p0))) },
#endif
#endif
			{ M(() => Convert.ToInt64((Decimal) 0)  ), N(() => L<Decimal, Int64>(p0 => Sql.ConvertTo<Int64>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt64((Double)  0)  ), N(() => L<Double,  Int64>(p0 => Sql.ConvertTo<Int64>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt64((Int16)   0)  ), N(() => L<Int16,   Int64>(p0 => Sql.ConvertTo<Int64>.From(p0))) },
			{ M(() => Convert.ToInt64((Int32)   0)  ), N(() => L<Int32,   Int64>(p0 => Sql.ConvertTo<Int64>.From(p0))) },
			{ M(() => Convert.ToInt64((Int64)   0)  ), N(() => L<Int64,   Int64>(p0 => Sql.ConvertTo<Int64>.From(p0))) },
			{ M(() => Convert.ToInt64((Object)  0)  ), N(() => L<Object,  Int64>(p0 => Sql.ConvertTo<Int64>.From(p0))) },
			{ M(() => Convert.ToInt64((SByte)   0)  ), N(() => L<SByte,   Int64>(p0 => Sql.ConvertTo<Int64>.From(p0))) },
			{ M(() => Convert.ToInt64((Single)  0)  ), N(() => L<Single,  Int64>(p0 => Sql.ConvertTo<Int64>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt64((String) "0") ), N(() => L<String,  Int64>(p0 => Sql.ConvertTo<Int64>.From(p0))) },
			{ M(() => Convert.ToInt64((UInt16)  0)  ), N(() => L<UInt16,  Int64>(p0 => Sql.ConvertTo<Int64>.From(p0))) },
			{ M(() => Convert.ToInt64((UInt32)  0)  ), N(() => L<UInt32,  Int64>(p0 => Sql.ConvertTo<Int64>.From(p0))) },
			{ M(() => Convert.ToInt64((UInt64)  0)  ), N(() => L<UInt64,  Int64>(p0 => Sql.ConvertTo<Int64>.From(p0))) },

			#endregion

			#region ToInt32

			{ M(() => Convert.ToInt32((Boolean)true)), N(() => L<Boolean, Int32>(p0 => Sql.ConvertTo<Int32>.From(p0))) },
			{ M(() => Convert.ToInt32((Byte)    0)  ), N(() => L<Byte,    Int32>(p0 => Sql.ConvertTo<Int32>.From(p0))) },
#if !NETSTANDARD
			{ M(() => Convert.ToInt32((Char)   '0') ), N(() => L<Char,    Int32>(p0 => Sql.ConvertTo<Int32>.From(p0))) },
#if !SILVERLIGHT
			{ M(() => Convert.ToInt32(DateTime.Now) ), N(() => L<DateTime,Int32>(p0 => Sql.ConvertTo<Int32>.From(p0))) },
#endif
#endif
			{ M(() => Convert.ToInt32((Decimal) 0)  ), N(() => L<Decimal, Int32>(p0 => Sql.ConvertTo<Int32>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt32((Double)  0)  ), N(() => L<Double,  Int32>(p0 => Sql.ConvertTo<Int32>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt32((Int16)   0)  ), N(() => L<Int16,   Int32>(p0 => Sql.ConvertTo<Int32>.From(p0))) },
			{ M(() => Convert.ToInt32((Int32)   0)  ), N(() => L<Int32,   Int32>(p0 => Sql.ConvertTo<Int32>.From(p0))) },
			{ M(() => Convert.ToInt32((Int64)   0)  ), N(() => L<Int64,   Int32>(p0 => Sql.ConvertTo<Int32>.From(p0))) },
			{ M(() => Convert.ToInt32((Object)  0)  ), N(() => L<Object,  Int32>(p0 => Sql.ConvertTo<Int32>.From(p0))) },
			{ M(() => Convert.ToInt32((SByte)   0)  ), N(() => L<SByte,   Int32>(p0 => Sql.ConvertTo<Int32>.From(p0))) },
			{ M(() => Convert.ToInt32((Single)  0)  ), N(() => L<Single,  Int32>(p0 => Sql.ConvertTo<Int32>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt32((String) "0") ), N(() => L<String,  Int32>(p0 => Sql.ConvertTo<Int32>.From(p0))) },
			{ M(() => Convert.ToInt32((UInt16)  0)  ), N(() => L<UInt16,  Int32>(p0 => Sql.ConvertTo<Int32>.From(p0))) },
			{ M(() => Convert.ToInt32((UInt32)  0)  ), N(() => L<UInt32,  Int32>(p0 => Sql.ConvertTo<Int32>.From(p0))) },
			{ M(() => Convert.ToInt32((UInt64)  0)  ), N(() => L<UInt64,  Int32>(p0 => Sql.ConvertTo<Int32>.From(p0))) },

			#endregion

			#region ToInt16

			{ M(() => Convert.ToInt16((Boolean)true)), N(() => L<Boolean, Int16>(p0 => Sql.ConvertTo<Int16>.From(p0))) },
			{ M(() => Convert.ToInt16((Byte)    0)  ), N(() => L<Byte,    Int16>(p0 => Sql.ConvertTo<Int16>.From(p0))) },
#if !NETSTANDARD
			{ M(() => Convert.ToInt16((Char)   '0') ), N(() => L<Char,    Int16>(p0 => Sql.ConvertTo<Int16>.From(p0))) },
#if !SILVERLIGHT
			{ M(() => Convert.ToInt16(DateTime.Now) ), N(() => L<DateTime,Int16>(p0 => Sql.ConvertTo<Int16>.From(p0))) },
#endif
#endif
			{ M(() => Convert.ToInt16((Decimal) 0)  ), N(() => L<Decimal, Int16>(p0 => Sql.ConvertTo<Int16>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt16((Double)  0)  ), N(() => L<Double,  Int16>(p0 => Sql.ConvertTo<Int16>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt16((Int16)   0)  ), N(() => L<Int16,   Int16>(p0 => Sql.ConvertTo<Int16>.From(p0))) },
			{ M(() => Convert.ToInt16((Int32)   0)  ), N(() => L<Int32,   Int16>(p0 => Sql.ConvertTo<Int16>.From(p0))) },
			{ M(() => Convert.ToInt16((Int64)   0)  ), N(() => L<Int64,   Int16>(p0 => Sql.ConvertTo<Int16>.From(p0))) },
			{ M(() => Convert.ToInt16((Object)  0)  ), N(() => L<Object,  Int16>(p0 => Sql.ConvertTo<Int16>.From(p0))) },
			{ M(() => Convert.ToInt16((SByte)   0)  ), N(() => L<SByte,   Int16>(p0 => Sql.ConvertTo<Int16>.From(p0))) },
			{ M(() => Convert.ToInt16((Single)  0)  ), N(() => L<Single,  Int16>(p0 => Sql.ConvertTo<Int16>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt16((String) "0") ), N(() => L<String,  Int16>(p0 => Sql.ConvertTo<Int16>.From(p0))) },
			{ M(() => Convert.ToInt16((UInt16)  0)  ), N(() => L<UInt16,  Int16>(p0 => Sql.ConvertTo<Int16>.From(p0))) },
			{ M(() => Convert.ToInt16((UInt32)  0)  ), N(() => L<UInt32,  Int16>(p0 => Sql.ConvertTo<Int16>.From(p0))) },
			{ M(() => Convert.ToInt16((UInt64)  0)  ), N(() => L<UInt64,  Int16>(p0 => Sql.ConvertTo<Int16>.From(p0))) },

			#endregion

			#region ToSByte

			{ M(() => Convert.ToSByte((Boolean)true)), N(() => L<Boolean, SByte>(p0 => Sql.ConvertTo<SByte>.From(p0))) },
			{ M(() => Convert.ToSByte((Byte)    0)  ), N(() => L<Byte,    SByte>(p0 => Sql.ConvertTo<SByte>.From(p0))) },
#if !NETSTANDARD
			{ M(() => Convert.ToSByte((Char)   '0') ), N(() => L<Char,    SByte>(p0 => Sql.ConvertTo<SByte>.From(p0))) },
#if !SILVERLIGHT
			{ M(() => Convert.ToSByte(DateTime.Now) ), N(() => L<DateTime,SByte>(p0 => Sql.ConvertTo<SByte>.From(p0))) },
#endif
#endif
			{ M(() => Convert.ToSByte((Decimal) 0)  ), N(() => L<Decimal, SByte>(p0 => Sql.ConvertTo<SByte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToSByte((Double)  0)  ), N(() => L<Double,  SByte>(p0 => Sql.ConvertTo<SByte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToSByte((Int16)   0)  ), N(() => L<Int16,   SByte>(p0 => Sql.ConvertTo<SByte>.From(p0))) },
			{ M(() => Convert.ToSByte((Int32)   0)  ), N(() => L<Int32,   SByte>(p0 => Sql.ConvertTo<SByte>.From(p0))) },
			{ M(() => Convert.ToSByte((Int64)   0)  ), N(() => L<Int64,   SByte>(p0 => Sql.ConvertTo<SByte>.From(p0))) },
			{ M(() => Convert.ToSByte((Object)  0)  ), N(() => L<Object,  SByte>(p0 => Sql.ConvertTo<SByte>.From(p0))) },
			{ M(() => Convert.ToSByte((SByte)   0)  ), N(() => L<SByte,   SByte>(p0 => Sql.ConvertTo<SByte>.From(p0))) },
			{ M(() => Convert.ToSByte((Single)  0)  ), N(() => L<Single,  SByte>(p0 => Sql.ConvertTo<SByte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToSByte((String) "0") ), N(() => L<String,  SByte>(p0 => Sql.ConvertTo<SByte>.From(p0))) },
			{ M(() => Convert.ToSByte((UInt16)  0)  ), N(() => L<UInt16,  SByte>(p0 => Sql.ConvertTo<SByte>.From(p0))) },
			{ M(() => Convert.ToSByte((UInt32)  0)  ), N(() => L<UInt32,  SByte>(p0 => Sql.ConvertTo<SByte>.From(p0))) },
			{ M(() => Convert.ToSByte((UInt64)  0)  ), N(() => L<UInt64,  SByte>(p0 => Sql.ConvertTo<SByte>.From(p0))) },

			#endregion

			#region ToSingle

			{ M(() => Convert.ToSingle((Boolean)true)), N(() => L<Boolean, Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
			{ M(() => Convert.ToSingle((Byte)    0)  ), N(() => L<Byte,    Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
#if !NETSTANDARD
			{ M(() => Convert.ToSingle((Char)   '0') ), N(() => L<Char,    Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
#if !SILVERLIGHT
			{ M(() => Convert.ToSingle(DateTime.Now) ), N(() => L<DateTime,Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
#endif
#endif
			{ M(() => Convert.ToSingle((Decimal) 0)  ), N(() => L<Decimal, Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
			{ M(() => Convert.ToSingle((Double)  0)  ), N(() => L<Double,  Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
			{ M(() => Convert.ToSingle((Int16)   0)  ), N(() => L<Int16,   Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
			{ M(() => Convert.ToSingle((Int32)   0)  ), N(() => L<Int32,   Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
			{ M(() => Convert.ToSingle((Int64)   0)  ), N(() => L<Int64,   Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
			{ M(() => Convert.ToSingle((Object)  0)  ), N(() => L<Object,  Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
			{ M(() => Convert.ToSingle((SByte)   0)  ), N(() => L<SByte,   Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
			{ M(() => Convert.ToSingle((Single)  0)  ), N(() => L<Single,  Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
			{ M(() => Convert.ToSingle((String) "0") ), N(() => L<String,  Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
			{ M(() => Convert.ToSingle((UInt16)  0)  ), N(() => L<UInt16,  Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
			{ M(() => Convert.ToSingle((UInt32)  0)  ), N(() => L<UInt32,  Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },
			{ M(() => Convert.ToSingle((UInt64)  0)  ), N(() => L<UInt64,  Single>(p0 => Sql.ConvertTo<Single>.From(p0))) },

			#endregion

			#region ToString

			{ M(() => Convert.ToString((Boolean)true)), N(() => L<Boolean, String>(p0 => Sql.ConvertTo<String>.From(p0))) },
			{ M(() => Convert.ToString((Byte)    0)  ), N(() => L<Byte,    String>(p0 => Sql.ConvertTo<String>.From(p0))) },
			{ M(() => Convert.ToString((Char)   '0') ), N(() => L<Char,    String>(p0 => Sql.ConvertTo<String>.From(p0))) },
			{ M(() => Convert.ToString(DateTime.Now) ), N(() => L<DateTime,String>(p0 => Sql.ConvertTo<String>.From(p0))) },
			{ M(() => Convert.ToString((Decimal) 0)  ), N(() => L<Decimal, String>(p0 => Sql.ConvertTo<String>.From(p0))) },
			{ M(() => Convert.ToString((Double)  0)  ), N(() => L<Double,  String>(p0 => Sql.ConvertTo<String>.From(p0))) },
			{ M(() => Convert.ToString((Int16)   0)  ), N(() => L<Int16,   String>(p0 => Sql.ConvertTo<String>.From(p0))) },
			{ M(() => Convert.ToString((Int32)   0)  ), N(() => L<Int32,   String>(p0 => Sql.ConvertTo<String>.From(p0))) },
			{ M(() => Convert.ToString((Int64)   0)  ), N(() => L<Int64,   String>(p0 => Sql.ConvertTo<String>.From(p0))) },
			{ M(() => Convert.ToString((Object)  0)  ), N(() => L<Object,  String>(p0 => Sql.ConvertTo<String>.From(p0))) },
			{ M(() => Convert.ToString((SByte)   0)  ), N(() => L<SByte,   String>(p0 => Sql.ConvertTo<String>.From(p0))) },
			{ M(() => Convert.ToString((Single)  0)  ), N(() => L<Single,  String>(p0 => Sql.ConvertTo<String>.From(p0))) },
#if !SILVERLIGHT && !NETSTANDARD
			{ M(() => Convert.ToString((String) "0") ), N(() => L<String,  String>(p0 => p0                            )) },
#endif
			{ M(() => Convert.ToString((UInt16)  0)  ), N(() => L<UInt16,  String>(p0 => Sql.ConvertTo<String>.From(p0))) },
			{ M(() => Convert.ToString((UInt32)  0)  ), N(() => L<UInt32,  String>(p0 => Sql.ConvertTo<String>.From(p0))) },
			{ M(() => Convert.ToString((UInt64)  0)  ), N(() => L<UInt64,  String>(p0 => Sql.ConvertTo<String>.From(p0))) },

			#endregion

			#region ToUInt16

			{ M(() => Convert.ToUInt16((Boolean)true)), N(() => L<Boolean, UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0))) },
			{ M(() => Convert.ToUInt16((Byte)    0)  ), N(() => L<Byte,    UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0))) },
			{ M(() => Convert.ToUInt16((Char)   '0') ), N(() => L<Char,    UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0))) },
#if !SILVERLIGHT && !NETSTANDARD
			{ M(() => Convert.ToUInt16(DateTime.Now) ), N(() => L<DateTime,UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0))) },
#endif
			{ M(() => Convert.ToUInt16((Decimal) 0)  ), N(() => L<Decimal, UInt16>(p0 => Sql.ConvertTo<UInt16>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt16((Double)  0)  ), N(() => L<Double,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt16((Int16)   0)  ), N(() => L<Int16,   UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0))) },
			{ M(() => Convert.ToUInt16((Int32)   0)  ), N(() => L<Int32,   UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0))) },
			{ M(() => Convert.ToUInt16((Int64)   0)  ), N(() => L<Int64,   UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0))) },
			{ M(() => Convert.ToUInt16((Object)  0)  ), N(() => L<Object,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0))) },
			{ M(() => Convert.ToUInt16((SByte)   0)  ), N(() => L<SByte,   UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0))) },
			{ M(() => Convert.ToUInt16((Single)  0)  ), N(() => L<Single,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt16((String) "0") ), N(() => L<String,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0)) ) },
			{ M(() => Convert.ToUInt16((UInt16)  0)  ), N(() => L<UInt16,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0)) ) },
			{ M(() => Convert.ToUInt16((UInt32)  0)  ), N(() => L<UInt32,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0)) ) },
			{ M(() => Convert.ToUInt16((UInt64)  0)  ), N(() => L<UInt64,  UInt16>(p0 => Sql.ConvertTo<UInt16>.From(p0)) ) },

			#endregion

			#region ToUInt32

			{ M(() => Convert.ToUInt32((Boolean)true)), N(() => L<Boolean, UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0))) },
			{ M(() => Convert.ToUInt32((Byte)    0)  ), N(() => L<Byte,    UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0))) },
			{ M(() => Convert.ToUInt32((Char)   '0') ), N(() => L<Char,    UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0))) },
#if !SILVERLIGHT && !NETSTANDARD
			{ M(() => Convert.ToUInt32(DateTime.Now) ), N(() => L<DateTime,UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0))) },
#endif
			{ M(() => Convert.ToUInt32((Decimal) 0)  ), N(() => L<Decimal, UInt32>(p0 => Sql.ConvertTo<UInt32>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt32((Double)  0)  ), N(() => L<Double,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt32((Int16)   0)  ), N(() => L<Int16,   UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0))) },
			{ M(() => Convert.ToUInt32((Int32)   0)  ), N(() => L<Int32,   UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0))) },
			{ M(() => Convert.ToUInt32((Int64)   0)  ), N(() => L<Int64,   UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0))) },
			{ M(() => Convert.ToUInt32((Object)  0)  ), N(() => L<Object,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0))) },
			{ M(() => Convert.ToUInt32((SByte)   0)  ), N(() => L<SByte,   UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0))) },
			{ M(() => Convert.ToUInt32((Single)  0)  ), N(() => L<Single,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt32((String) "0") ), N(() => L<String,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0))) },
			{ M(() => Convert.ToUInt32((UInt16)  0)  ), N(() => L<UInt16,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0))) },
			{ M(() => Convert.ToUInt32((UInt32)  0)  ), N(() => L<UInt32,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0))) },
			{ M(() => Convert.ToUInt32((UInt64)  0)  ), N(() => L<UInt64,  UInt32>(p0 => Sql.ConvertTo<UInt32>.From(p0))) },

			#endregion

			#region ToUInt64

			{ M(() => Convert.ToUInt64((Boolean)true)), N(() => L<Boolean, UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0))) },
			{ M(() => Convert.ToUInt64((Byte)    0)  ), N(() => L<Byte,    UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0))) },
			{ M(() => Convert.ToUInt64((Char)   '0') ), N(() => L<Char,    UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0))) },
#if !SILVERLIGHT && !NETSTANDARD
			{ M(() => Convert.ToUInt64(DateTime.Now) ), N(() => L<DateTime,UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0))) },
#endif
			{ M(() => Convert.ToUInt64((Decimal) 0)  ), N(() => L<Decimal, UInt64>(p0 => Sql.ConvertTo<UInt64>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt64((Double)  0)  ), N(() => L<Double,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt64((Int16)   0)  ), N(() => L<Int16,   UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0))) },
			{ M(() => Convert.ToUInt64((Int32)   0)  ), N(() => L<Int32,   UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0))) },
			{ M(() => Convert.ToUInt64((Int64)   0)  ), N(() => L<Int64,   UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0))) },
			{ M(() => Convert.ToUInt64((Object)  0)  ), N(() => L<Object,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0))) },
			{ M(() => Convert.ToUInt64((SByte)   0)  ), N(() => L<SByte,   UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0))) },
			{ M(() => Convert.ToUInt64((Single)  0)  ), N(() => L<Single,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt64((String) "0") ), N(() => L<String,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0))) },
			{ M(() => Convert.ToUInt64((UInt16)  0)  ), N(() => L<UInt16,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0))) },
			{ M(() => Convert.ToUInt64((UInt32)  0)  ), N(() => L<UInt32,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0))) },
			{ M(() => Convert.ToUInt64((UInt64)  0)  ), N(() => L<UInt64,  UInt64>(p0 => Sql.ConvertTo<UInt64>.From(p0))) },

			#endregion

			#endregion

			#region Math

			{ M(() => Math.Abs    ((Decimal)0)), N(() => L<Decimal,Decimal>((Decimal p) => Sql.Abs(p).Value )) },
			{ M(() => Math.Abs    ((Double) 0)), N(() => L<Double, Double> ((Double  p) => Sql.Abs(p).Value )) },
			{ M(() => Math.Abs    ((Int16)  0)), N(() => L<Int16,  Int16>  ((Int16   p) => Sql.Abs(p).Value )) },
			{ M(() => Math.Abs    ((Int32)  0)), N(() => L<Int32,  Int32>  ((Int32   p) => Sql.Abs(p).Value )) },
			{ M(() => Math.Abs    ((Int64)  0)), N(() => L<Int64,  Int64>  ((Int64   p) => Sql.Abs(p).Value )) },
			{ M(() => Math.Abs    ((SByte)  0)), N(() => L<SByte,  SByte>  ((SByte   p) => Sql.Abs(p).Value )) },
			{ M(() => Math.Abs    ((Single) 0)), N(() => L<Single, Single> ((Single  p) => Sql.Abs(p).Value )) },

			{ M(() => Math.Acos   (0)   ), N(() => L<Double,Double>  ((Double p)     => Sql.Acos   (p)   .Value )) },
			{ M(() => Math.Asin   (0)   ), N(() => L<Double,Double>  ((Double p)     => Sql.Asin   (p)   .Value )) },
			{ M(() => Math.Atan   (0)   ), N(() => L<Double,Double>  ((Double p)     => Sql.Atan   (p)   .Value )) },
			{ M(() => Math.Atan2  (0,0) ), N(() => L<Double,Double,Double>((Double x,Double y) => Sql.Atan2  (x, y).Value )) },
#if !SILVERLIGHT
			{ M(() => Math.Ceiling((Decimal)0)), N(() => L<Decimal,Decimal>  ((Decimal p)     => Sql.Ceiling(p)   .Value )) },
#endif
			{ M(() => Math.Ceiling((Double)0)), N(() => L<Double,Double>  ((Double p)     => Sql.Ceiling(p)   .Value )) },
			{ M(() => Math.Cos            (0)), N(() => L<Double,Double>  ((Double p)     => Sql.Cos    (p)   .Value )) },
			{ M(() => Math.Cosh           (0)), N(() => L<Double,Double>  ((Double p)     => Sql.Cosh   (p)   .Value )) },
			{ M(() => Math.Exp            (0)), N(() => L<Double,Double>  ((Double p)     => Sql.Exp    (p)   .Value )) },
#if !SILVERLIGHT
			{ M(() => Math.Floor ((Decimal)0)), N(() => L<Decimal,Decimal>((Decimal p)    => Sql.Floor  (p)   .Value )) },
#endif
			{ M(() => Math.Floor  ((Double)0)), N(() => L<Double,Double>       ((Double p)          => Sql.Floor  (p)   .Value )) },
			{ M(() => Math.Log            (0)), N(() => L<Double,Double>       ((Double p)          => Sql.Log    (p)   .Value )) },
			{ M(() => Math.Log          (0,0)), N(() => L<Double,Double,Double>((Double m,Double n) => Sql.Log    (n, m).Value )) },
			{ M(() => Math.Log10          (0)), N(() => L<Double,Double>       ((Double p)          => Sql.Log10  (p)   .Value )) },

			{ M(() => Math.Max((Byte)   0, (Byte)   0)), N(() => L<Byte,   Byte,   Byte>   ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((Decimal)0, (Decimal)0)), N(() => L<Decimal,Decimal,Decimal>((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((Double) 0, (Double) 0)), N(() => L<Double, Double, Double> ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((Int16)  0, (Int16)  0)), N(() => L<Int16,  Int16,  Int16>  ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((Int32)  0, (Int32)  0)), N(() => L<Int32,  Int32,  Int32>  ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((Int64)  0, (Int64)  0)), N(() => L<Int64,  Int64,  Int64>  ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((SByte)  0, (SByte)  0)), N(() => L<SByte,  SByte,  SByte>  ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((Single) 0, (Single) 0)), N(() => L<Single, Single, Single> ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((UInt16) 0, (UInt16) 0)), N(() => L<UInt16, UInt16, UInt16> ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((UInt32) 0, (UInt32) 0)), N(() => L<UInt32, UInt32, UInt32> ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((UInt64) 0, (UInt64) 0)), N(() => L<UInt64, UInt64, UInt64> ((v1,v2) => v1 > v2 ? v1 : v2)) },

			{ M(() => Math.Min((Byte)   0, (Byte)   0)), N(() => L<Byte,   Byte,   Byte>   ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((Decimal)0, (Decimal)0)), N(() => L<Decimal,Decimal,Decimal>((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((Double) 0, (Double) 0)), N(() => L<Double, Double, Double> ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((Int16)  0, (Int16)  0)), N(() => L<Int16,  Int16,  Int16>  ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((Int32)  0, (Int32)  0)), N(() => L<Int32,  Int32,  Int32>  ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((Int64)  0, (Int64)  0)), N(() => L<Int64,  Int64,  Int64>  ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((SByte)  0, (SByte)  0)), N(() => L<SByte,  SByte,  SByte>  ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((Single) 0, (Single) 0)), N(() => L<Single, Single, Single> ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((UInt16) 0, (UInt16) 0)), N(() => L<UInt16, UInt16, UInt16> ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((UInt32) 0, (UInt32) 0)), N(() => L<UInt32, UInt32, UInt32> ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((UInt64) 0, (UInt64) 0)), N(() => L<UInt64, UInt64, UInt64> ((v1,v2) => v1 < v2 ? v1 : v2)) },

			{ M(() => Math.Pow        (0,0) ), N(() => L<Double,Double,Double>    ((Double x,Double y) => Sql.Power(x, y).Value )) },

			{ M(() => Sql.Round       (0m)  ), N(() => L<Decimal?,Decimal?>       ((Decimal? d)          => Sql.Round(d, 0))) },
			{ M(() => Sql.Round       (0.0) ), N(() => L<Double?, Double?>        ((Double?  d)          => Sql.Round(d, 0))) },

			{ M(() => Sql.RoundToEven(0m)   ), N(() => L<Decimal?,Decimal?>       ((Decimal? d)          => d - Sql.Floor(d) == 0.5m && Sql.Floor(d) % 2 == 0? Sql.Floor(d) : Sql.Round(d))) },
			{ M(() => Sql.RoundToEven(0.0)  ), N(() => L<Double?, Double?>        ((Double?  d)          => d - Sql.Floor(d) == 0.5  && Sql.Floor(d) % 2 == 0? Sql.Floor(d) : Sql.Round(d))) },

			{ M(() => Sql.RoundToEven(0m, 0)), N(() => L<Decimal?,Int32?,Decimal?>((Decimal? d,Int32? n) => d * 2 == Sql.Round(d * 2, n) && d != Sql.Round(d, n) ? Sql.Round(d / 2, n) * 2 : Sql.Round(d, n))) },
			{ M(() => Sql.RoundToEven(0.0,0)), N(() => L<Double?, Int32?,Double?> ((Double?  d,Int32? n) => d * 2 == Sql.Round(d * 2, n) && d != Sql.Round(d, n) ? Sql.Round(d / 2, n) * 2 : Sql.Round(d, n))) },

			{ M(() => Math.Round     (0m)   ), N(() => L<Decimal,Decimal>         ( d    => Sql.RoundToEven(d).Value )) },
			{ M(() => Math.Round     (0.0)  ), N(() => L<Double, Double>          ( d    => Sql.RoundToEven(d).Value )) },

			{ M(() => Math.Round     (0m, 0)), N(() => L<Decimal,Int32,Decimal>   ((d,n) => Sql.RoundToEven(d, n).Value )) },
			{ M(() => Math.Round     (0.0,0)), N(() => L<Double, Int32,Double>    ((d,n) => Sql.RoundToEven(d, n).Value )) },

#if !SILVERLIGHT
			{ M(() => Math.Round (0m,    MidpointRounding.ToEven)), N(() => L<Decimal,MidpointRounding,Decimal>      ((d,  p) => p == MidpointRounding.ToEven ? Sql.RoundToEven(d).  Value : Sql.Round(d).  Value )) },
			{ M(() => Math.Round (0.0,   MidpointRounding.ToEven)), N(() => L<Double, MidpointRounding,Double>       ((d,  p) => p == MidpointRounding.ToEven ? Sql.RoundToEven(d).  Value : Sql.Round(d).  Value )) },

			{ M(() => Math.Round (0m, 0, MidpointRounding.ToEven)), N(() => L<Decimal,Int32,MidpointRounding,Decimal>((d,n,p) => p == MidpointRounding.ToEven ? Sql.RoundToEven(d,n).Value : Sql.Round(d,n).Value )) },
			{ M(() => Math.Round (0.0,0, MidpointRounding.ToEven)), N(() => L<Double, Int32,MidpointRounding,Double> ((d,n,p) => p == MidpointRounding.ToEven ? Sql.RoundToEven(d,n).Value : Sql.Round(d,n).Value )) },
#endif

			{ M(() => Math.Sign  ((Decimal)0)), N(() => L<Decimal,Int32>(p => Sql.Sign(p).Value )) },
			{ M(() => Math.Sign  ((Double) 0)), N(() => L<Double, Int32>(p => Sql.Sign(p).Value )) },
			{ M(() => Math.Sign  ((Int16)  0)), N(() => L<Int16,  Int32>(p => Sql.Sign(p).Value )) },
			{ M(() => Math.Sign  ((Int32)  0)), N(() => L<Int32,  Int32>(p => Sql.Sign(p).Value )) },
			{ M(() => Math.Sign  ((Int64)  0)), N(() => L<Int64,  Int32>(p => Sql.Sign(p).Value )) },
			{ M(() => Math.Sign  ((SByte)  0)), N(() => L<SByte,  Int32>(p => Sql.Sign(p).Value )) },
			{ M(() => Math.Sign  ((Single) 0)), N(() => L<Single, Int32>(p => Sql.Sign(p).Value )) },

			{ M(() => Math.Sin   (0)), N(() => L<Double,Double>((Double p) => Sql.Sin (p).Value )) },
			{ M(() => Math.Sinh  (0)), N(() => L<Double,Double>((Double p) => Sql.Sinh(p).Value )) },
			{ M(() => Math.Sqrt  (0)), N(() => L<Double,Double>((Double p) => Sql.Sqrt(p).Value )) },
			{ M(() => Math.Tan   (0)), N(() => L<Double,Double>((Double p) => Sql.Tan (p).Value )) },
			{ M(() => Math.Tanh  (0)), N(() => L<Double,Double>((Double p) => Sql.Tanh(p).Value )) },

#if !SILVERLIGHT
			{ M(() => Math.Truncate(0m)),  N(() => L<Decimal,Decimal>((Decimal p) => Sql.Truncate(p).Value )) },
			{ M(() => Math.Truncate(0.0)), N(() => L<Double,Double>  ((Double  p) => Sql.Truncate(p).Value )) },
#endif

			#endregion

			#region Visual Basic Compiler Services

//#if !SILVERLIGHT
//				{ M(() => Operators.CompareString("","",false)), L<S,S,B,I>((s1,s2,b) => b ? string.CompareOrdinal(s1.ToUpper(), s2.ToUpper()) : string.CompareOrdinal(s1, s2)) },
//#endif

			#endregion

			#region SqlTypes

#if !SILVERLIGHT && !NETFX_CORE
			{ M(() => new SqlBoolean().Value),   N(() => L<SqlBoolean,bool>((SqlBoolean obj) => (bool)obj))          },
			{ M(() => new SqlBoolean().IsFalse), N(() => L<SqlBoolean,bool>((SqlBoolean obj) => (bool)obj == false)) },
			{ M(() => new SqlBoolean().IsTrue),  N(() => L<SqlBoolean,bool>((SqlBoolean obj) => (bool)obj == true))  },
			{ M(() => SqlBoolean.True),          N(() => L<bool>           (()               => true))  },
			{ M(() => SqlBoolean.False),         N(() => L<bool>           (()               => false)) },
#endif

			#endregion
		};

		#endregion

		static Dictionary<string,Dictionary<MemberInfo,IExpressionInfo>> LoadMembers()
		{
			SetGenericInfoProvider(typeof(GenericInfoProvider<>));

			return new Dictionary<string,Dictionary<MemberInfo,IExpressionInfo>>
			{
				{ "", _commonMembers },

				#region SqlServer

				{ ProviderName.SqlServer, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.PadRight("",0,' ') ), N(() => L<String,Int32?,Char,String>     ((String p0,Int32? p1,Char p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ') ), N(() => L<String,Int32?,Char,String>     ((String p0,Int32? p1,Char p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },
					{ M(() => Sql.Trim    ("")       ), N(() => L<String,String>                 ((String p0)                   => Sql.TrimLeft(Sql.TrimRight(p0)))) },
					{ M(() => Sql.MakeDateTime(0,0,0)), N(() => L<Int32?,Int32?,Int32?,DateTime?>((Int32? y,Int32? m,Int32? d)  => DateAdd(Sql.DateParts.Month, (y.Value - 1900) * 12 + m.Value - 1, d.Value - 1))) },
					{ M(() => Sql.Cosh(0)            ), N(() => L<Double?,Double?>               ( v    => (Sql.Exp(v) + Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Log(0m, 0)         ), N(() => L<Decimal?,Decimal?,Decimal?>    ((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log(0.0,0)         ), N(() => L<Double?,Double?,Double?>       ((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Sinh(0)            ), N(() => L<Double?,Double?>               ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)            ), N(() => L<Double?,Double?>               ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },
				}},

				#endregion

				#region SqlServer2000

				{ ProviderName.SqlServer2000, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.MakeDateTime(0, 0, 0, 0, 0, 0) ), N(() => L<Int32?,Int32?,Int32?,Int32?,Int32?,Int32?,DateTime?>((y,m,d,h,mm,s) => Sql.Convert(Sql.DateTime2,
						y.ToString() + "-" + m.ToString() + "-" + d.ToString() + " " +
						h.ToString() + ":" + mm.ToString() + ":" + s.ToString(), 120))) },
					{ M(() => DateTime.Parse("")),   N(() => L<String,DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) )) },
					{ M(() => Sql.RoundToEven(0m) ), N(() => L<Decimal?,Decimal?>((Decimal? d) => d - Sql.Floor(d) == 0.5m && (long)Sql.Floor(d) % 2 == 0? Sql.Floor(d) : Sql.Round(d))) },
					{ M(() => Sql.RoundToEven(0.0)), N(() => L<Double?, Double?> ((Double?  d) => d - Sql.Floor(d) == 0.5  && (long)Sql.Floor(d) % 2 == 0? Sql.Floor(d) : Sql.Round(d))) },
				}},

				#endregion

				#region SqlServer2005

				{ ProviderName.SqlServer2005, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.MakeDateTime(0, 0, 0, 0, 0, 0) ), N(() => L<Int32?,Int32?,Int32?,Int32?,Int32?,Int32?,DateTime?>((y,m,d,h,mm,s) => Sql.Convert(Sql.DateTime2,
						y.ToString() + "-" + m.ToString() + "-" + d.ToString() + " " +
						h.ToString() + ":" + mm.ToString() + ":" + s.ToString(), 120))) },
					{ M(() => DateTime.Parse("")), N(() => L<String,DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) )) },
				}},

				#endregion

				#region SqlCe

				{ ProviderName.SqlCe, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Left    ("",0)    ), N(() => L<String,Int32?,String>   ((String p0,Int32? p1)       => Sql.Substring(p0, 1, p1))) },
					{ M(() => Sql.Right   ("",0)    ), N(() => L<String,Int32?,String>   ((String p0,Int32? p1)       => Sql.Substring(p0, p0.Length - p1 + 1, p1))) },
					{ M(() => Sql.PadRight("",0,' ')), N(() => L<String,Int32?,Char?,String>((String p0,Int32? p1,Char? p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ')), N(() => L<String,Int32?,Char?,String>((String p0,Int32? p1,Char? p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },
					{ M(() => Sql.Trim    ("")      ), N(() => L<String,String>      ((String p0)             => Sql.TrimLeft(Sql.TrimRight(p0)))) },

					{ M(() => Sql.Cosh(0)    ), N(() => L<Double?,Double?>   ( v    => (Sql.Exp(v) + Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Log (0m, 0)), N(() => L<Decimal?,Decimal?,Decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log (0.0,0)), N(() => L<Double?,Double?,Double?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Sinh(0)    ), N(() => L<Double?,Double?>   ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)    ), N(() => L<Double?,Double?>   ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },
				}},

				#endregion

				#region DB2

				{ ProviderName.DB2, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Space   (0)        ), N(() => L<Int32?,String>       ( p0           => Sql.Convert(Sql.VarChar(1000), Replicate(" ", p0)))) },
					{ M(() => Sql.Stuff   ("",0,0,"")), N(() => L<String,Int32?,Int32?,String,String>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.PadRight("",0,' ') ), N(() => L<String,Int32?,Char?,String>  ((p0,p1,p2)    => p0.Length > p1 ? p0 : p0 + VarChar(Replicate(p2, p1 - p0.Length), 1000))) },
					{ M(() => Sql.PadLeft ("",0,' ') ), N(() => L<String,Int32?,Char?,String>  ((p0,p1,p2)    => p0.Length > p1 ? p0 : VarChar(Replicate(p2, p1 - p0.Length), 1000) + p0)) },

					{ M(() => Sql.ConvertTo<String>.From((Decimal)0)), N(() => L<Decimal,String>((Decimal p) => Sql.TrimLeft(Sql.Convert<string,Decimal>(p), '0'))) },
					{ M(() => Sql.ConvertTo<String>.From(Guid.Empty)), N(() => L<Guid,   String>((Guid    p) => Sql.Lower(
						Sql.Substring(Hex(p),  7,  2) + Sql.Substring(Hex(p),  5, 2) + Sql.Substring(Hex(p), 3, 2) + Sql.Substring(Hex(p), 1, 2) + "-" +
						Sql.Substring(Hex(p), 11,  2) + Sql.Substring(Hex(p),  9, 2) + "-" +
						Sql.Substring(Hex(p), 15,  2) + Sql.Substring(Hex(p), 13, 2) + "-" +
						Sql.Substring(Hex(p), 17,  4) + "-" +
						Sql.Substring(Hex(p), 21, 12)))) },

					{ M(() => Sql.Log(0m, 0)), N(() => L<Decimal?,Decimal?,Decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log(0.0,0)), N(() => L<Double?,Double?,Double?>   ((m,n) => Sql.Log(n) / Sql.Log(m))) },
				}},

				#endregion

				#region Informix

				{ ProviderName.Informix, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Left ("",0)     ), N(() => L<String,Int32?,String>     ((String p0,Int32? p1)            => Sql.Substring(p0,  1, p1)))                  },
					{ M(() => Sql.Right("",0)     ), N(() => L<String,Int32?,String>     ((String p0,Int32? p1)            => Sql.Substring(p0,  p0.Length - p1 + 1, p1))) },
					{ M(() => Sql.Stuff("",0,0,"")), N(() => L<String,Int32?,Int32?,String,String>((String p0,Int32? p1,Int32? p2,String p3) =>     AltStuff (p0,  p1, p2, p3)))             },
					{ M(() => Sql.Space(0)        ), N(() => L<Int32?,String>       ((Int32? p0)                 => Sql.PadRight (" ", p0, ' ')))                },

					{ M(() => Sql.MakeDateTime(0,0,0)), N(() => L<Int32?,Int32?,Int32?,DateTime?>((y,m,d) => Mdy(m, d, y))) },

					{ M(() => Sql.Cot (0)         ), N(() => L<Double?,Double?>      ( v            => Sql.Cos(v) / Sql.Sin(v) ))        },
					{ M(() => Sql.Cosh(0)         ), N(() => L<Double?,Double?>      ( v            => (Sql.Exp(v) + Sql.Exp(-v)) / 2 )) },

					{ M(() => Sql.Degrees((Decimal?)0)), N(() => L<Decimal?,Decimal?>( v => (Decimal?)(v.Value * (180 / (Decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((Double?) 0)), N(() => L<Double?, Double?> ( v => (Double?) (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int16?)  0)), N(() => L<Int16?,  Int16?>  ( v => (Int16?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int32?)  0)), N(() => L<Int32?,  Int32?>  ( v => (Int32?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int64?)  0)), N(() => L<Int64?,  Int64?>  ( v => (Int64?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((SByte?)  0)), N(() => L<SByte?,  SByte?>  ( v => (SByte?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Single?) 0)), N(() => L<Single?, Single?> ( v => (Single?) (v.Value * (180 / Math.PI)))) },

					{ M(() => Sql.Log(0m, 0)), N(() => L<Decimal?,Decimal?,Decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log(0.0,0)), N(() => L<Double?,Double?,Double?>((m,n) => Sql.Log(n) / Sql.Log(m))) },

					{ M(() => Sql.Sign((Decimal?)0)), N(() => L<Decimal?,Int32?>((Decimal? p) => (Int32?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((Double?) 0)), N(() => L<Double?, Int32?>((Double?  p) => (Int32?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((Int16?)  0)), N(() => L<Int16?,  Int32?>((Int16?   p) => (Int32?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((Int32?)  0)), N(() => L<Int32?,  Int32?>((Int32?   p) => (Int32?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((Int64?)  0)), N(() => L<Int64?,  Int32?>((Int64?   p) => (Int32?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((SByte?)  0)), N(() => L<SByte?,  Int32?>((SByte?   p) => (Int32?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((Single?) 0)), N(() => L<Single?, Int32?>((Single?  p) => (Int32?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },

					{ M(() => Sql.Sinh(0)), N(() => L<Double?,Double?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)), N(() => L<Double?,Double?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) +Sql.Exp(-v)))) },
				}},

				#endregion

				#region Oracle

				{ ProviderName.Oracle, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Left ("",0)     ), N(() => L<String,Int32?,String>     ((String p0,Int32? p1)            => Sql.Substring(p0, 1, p1))) },
					{ M(() => Sql.Right("",0)     ), N(() => L<String,Int32?,String>     ((String p0,Int32? p1)            => Sql.Substring(p0, p0.Length - p1 + 1, p1))) },
					{ M(() => Sql.Stuff("",0,0,"")), N(() => L<String,Int32?,Int32?,String,String>((String p0,Int32? p1,Int32? p2,String p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.Space(0)        ), N(() => L<Int32?,String>       ((Int32? p0)                 => Sql.PadRight(" ", p0, ' '))) },

					{ M(() => Sql.ConvertTo<String>.From(Guid.Empty)), N(() => L<Guid,String>(p => Sql.Lower(
						Sql.Substring(Sql.Convert2(Sql.Char(36), p),  7,  2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p),  5, 2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p), 3, 2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p), 1, 2) + "-" +
						Sql.Substring(Sql.Convert2(Sql.Char(36), p), 11,  2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p),  9, 2) + "-" +
						Sql.Substring(Sql.Convert2(Sql.Char(36), p), 15,  2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p), 13, 2) + "-" +
						Sql.Substring(Sql.Convert2(Sql.Char(36), p), 17,  4) + "-" +
						Sql.Substring(Sql.Convert2(Sql.Char(36), p), 21, 12)))) },

					{ M(() => Sql.Cot  (0)),   N(() => L<Double?,Double?>(v => Sql.Cos(v) / Sql.Sin(v) )) },
					{ M(() => Sql.Log10(0.0)), N(() => L<Double?,Double?>(v => Sql.Log(10, v)          )) },

					{ M(() => Sql.Degrees((Decimal?)0)), N(() => L<Decimal?,Decimal?>( v => (Decimal?)(v.Value * (180 / (Decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((Double?) 0)), N(() => L<Double?, Double?> ( v => (Double?) (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int16?)  0)), N(() => L<Int16?,  Int16?>  ( v => (Int16?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int32?)  0)), N(() => L<Int32?,  Int32?>  ( v => (Int32?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int64?)  0)), N(() => L<Int64?,  Int64?>  ( v => (Int64?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((SByte?)  0)), N(() => L<SByte?,  SByte?>  ( v => (SByte?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Single?) 0)), N(() => L<Single?, Single?> ( v => (Single?) (v.Value * (180 / Math.PI)))) },
				}},

				#endregion

				#region Firebird

				{ ProviderName.Firebird, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M<String>(_  => Sql.Space(0         )), N(() => L<Int32?,String>       ( p0           => Sql.PadRight(" ", p0, ' '))) },
					{ M<String>(s  => Sql.Stuff(s, 0, 0, s)), N(() => L<String,Int32?,Int32?,String,String>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },

					{ M(() => Sql.Degrees((Decimal?)0)), N(() => L<Decimal?,Decimal?>((Decimal? v) => (Decimal?)(v.Value * 180 / DecimalPI()))) },
					{ M(() => Sql.Degrees((Double?) 0)), N(() => L<Double?, Double?> ((Double?  v) => (Double?) (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int16?)  0)), N(() => L<Int16?,  Int16?>  ((Int16?   v) => (Int16?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int32?)  0)), N(() => L<Int32?,  Int32?>  ((Int32?   v) => (Int32?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int64?)  0)), N(() => L<Int64?,  Int64?>  ((Int64?   v) => (Int64?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((SByte?)  0)), N(() => L<SByte?,  SByte?>  ((SByte?   v) => (SByte?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Single?) 0)), N(() => L<Single?, Single?> ((Single?  v) => (Single?) (v.Value * (180 / Math.PI)))) },

					{ M(() => Sql.RoundToEven(0.0)  ), N(() => L<Double?,Double?>       ((Double? v)          => (double?)Sql.RoundToEven((decimal)v)))    },
					{ M(() => Sql.RoundToEven(0.0,0)), N(() => L<Double?,Int32?,Double?>((Double? v,Int32? p) => (double?)Sql.RoundToEven((decimal)v, p))) },
				}},

				#endregion

				#region MySql

				{ ProviderName.MySql, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M<String>(s => Sql.Stuff(s, 0, 0, s)), N(() => L<String,Int32?,Int32?,String,String>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },

					{ M(() => Sql.Cosh(0)), N(() => L<Double?,Double?>(v => (Sql.Exp(v) + Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Sinh(0)), N(() => L<Double?,Double?>(v => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)), N(() => L<Double?,Double?>(v => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },
				}},

				#endregion

				#region PostgreSQL

				{ ProviderName.PostgreSQL, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Left ("",0)     ), N(() => L<String,Int32?,String>              ((p0,p1)                                   => Sql.Substring(p0, 1, p1))) },
					{ M(() => Sql.Right("",0)     ), N(() => L<String,Int32?,String>              ((String p0,Int32? p1)                     => Sql.Substring(p0, p0.Length - p1 + 1, p1))) },
					{ M(() => Sql.Stuff("",0,0,"")), N(() => L<String,Int32?,Int32?,String,String>((String p0,Int32? p1,Int32? p2,String p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.Space(0)        ), N(() => L<Int32?,String>                     ((Int32? p0)                               => Replicate(" ", p0))) },

					{ M(() => Sql.Cosh(0)           ), N(() => L<Double?,Double?>       ((Double? v)          => (Sql.Exp(v) + Sql.Exp(-v)) / 2 )) },
					{ M(() => Sql.Round      (0.0,0)), N(() => L<Double?,Int32?,Double?>((Double? v,Int32? p) => (double?)Sql.Round      ((decimal)v, p))) },
					{ M(() => Sql.RoundToEven(0.0)  ), N(() => L<Double?,Double?>       ((Double? v)          => (double?)Sql.RoundToEven((decimal)v)))    },
					{ M(() => Sql.RoundToEven(0.0,0)), N(() => L<Double?,Int32?,Double?>((Double? v,Int32? p) => (double?)Sql.RoundToEven((decimal)v, p))) },

					{ M(() => Sql.Log  ((double)0,0)), N(() => L<Double?,Double?,Double?> ((Double? m,Double? n)             => (Double?)Sql.Log((Decimal)m,(Decimal)n).Value)) },
					{ M(() => Sql.Sinh (0)          ), N(() => L<Double?,Double?>    ((Double? v)                  => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh (0)          ), N(() => L<Double?,Double?>    ((Double? v)                  => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },

					{ M(() => Sql.Truncate(0.0)     ), N(() => L<Double?,Double?>    ((Double? v)                  => (double?)Sql.Truncate((decimal)v))) },
				}},

				#endregion

				#region SQLite

				{ ProviderName.SQLite, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Stuff   ("",0,0,"")), N(() => L<String,Int32?,Int32?,String,String>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.PadRight("",0,' ') ), N(() => L<String,Int32?,Char?,String>  ((p0,p1,p2)    => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ') ), N(() => L<String,Int32?,Char?,String>  ((p0,p1,p2)    => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },

					{ M(() => Sql.MakeDateTime(0, 0, 0)), N(() => L<Int32?,Int32?,Int32?,DateTime?>((y,m,d) => Sql.Convert(Sql.Date,
						y.ToString() + "-" +
						(m.ToString().Length == 1 ? "0" + m.ToString() : m.ToString()) + "-" +
						(d.ToString().Length == 1 ? "0" + d.ToString() : d.ToString())))) },

					{ M(() => Sql.MakeDateTime(0, 0, 0, 0, 0, 0)), N(() => L<Int32?,Int32?,Int32?,Int32?,Int32?,Int32?,DateTime?>((y,m,d,h,i,s) => Sql.Convert(Sql.DateTime2,
						y.ToString() + "-" +
						(m.ToString().Length == 1 ? "0" + m.ToString() : m.ToString()) + "-" +
						(d.ToString().Length == 1 ? "0" + d.ToString() : d.ToString()) + " " +
						(h.ToString().Length == 1 ? "0" + h.ToString() : h.ToString()) + ":" +
						(i.ToString().Length == 1 ? "0" + i.ToString() : i.ToString()) + ":" +
						(s.ToString().Length == 1 ? "0" + s.ToString() : s.ToString())))) },

					{ M(() => Sql.ConvertTo<String>.From(Guid.Empty)), N(() => L<Guid,String>((Guid p) => Sql.Lower(
						Sql.Substring(Hex(p),  7,  2) + Sql.Substring(Hex(p),  5, 2) + Sql.Substring(Hex(p), 3, 2) + Sql.Substring(Hex(p), 1, 2) + "-" +
						Sql.Substring(Hex(p), 11,  2) + Sql.Substring(Hex(p),  9, 2) + "-" +
						Sql.Substring(Hex(p), 15,  2) + Sql.Substring(Hex(p), 13, 2) + "-" +
						Sql.Substring(Hex(p), 17,  4) + "-" +
						Sql.Substring(Hex(p), 21, 12)))) },

					{ M(() => Sql.Log (0m, 0)), N(() => L<Decimal?,Decimal?,Decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log (0.0,0)), N(() => L<Double?,Double?,Double?>   ((m,n) => Sql.Log(n) / Sql.Log(m))) },

					{ M(() => Sql.Truncate(0m)),  N(() => L<Decimal?,Decimal?>((Decimal? v) => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
					{ M(() => Sql.Truncate(0.0)), N(() => L<Double?,Double?>  ((Double?  v) => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
				}},

				#endregion

				#region Sybase

				{ ProviderName.Sybase, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.PadRight("",0,' ')), N(() => L<String,Int32?,Char?,String>((p0,p1,p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ')), N(() => L<String,Int32?,Char?,String>((p0,p1,p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },
					{ M(() => Sql.Trim    ("")      ), N(() => L<String,String>      ( p0        => Sql.TrimLeft(Sql.TrimRight(p0)))) },

					{ M(() => Sql.Cosh(0)    ),          N(() => L<Double?,Double?>   ( v    => (Sql.Exp(v) + Sql.Exp(-v)) / 2))  },
					{ M(() => Sql.Log (0m, 0)),          N(() => L<Decimal?,Decimal?,Decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log (0.0,0)),          N(() => L<Double?,Double?,Double?>((m,n) => Sql.Log(n) / Sql.Log(m)))    },

					{ M(() => Sql.Degrees((Decimal?)0)), N(() => L<Decimal?,Decimal?>( v => (Decimal?)(v.Value * (180 / (Decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((Double?) 0)), N(() => L<Double?, Double?> ( v => (Double?) (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int16?)  0)), N(() => L<Int16?,  Int16?>  ( v => (Int16?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int32?)  0)), N(() => L<Int32?,  Int32?>  ( v => (Int32?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int64?)  0)), N(() => L<Int64?,  Int64?>  ( v => (Int64?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((SByte?)  0)), N(() => L<SByte?,  SByte?>  ( v => (SByte?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Single?) 0)), N(() => L<Single?, Single?> ( v => (Single?) (v.Value * (180 / Math.PI)))) },

					{ M(() => Sql.Sinh(0)), N(() => L<Double?,Double?>((Double? v) => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)), N(() => L<Double?,Double?>((Double? v) => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },

					{ M(() => Sql.Truncate(0m)),  N(() => L<Decimal?,Decimal?>((Decimal? v) => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
					{ M(() => Sql.Truncate(0.0)), N(() => L<Double?,Double?>  ((Double?  v) => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
				}},

				#endregion

				#region Access

				{ ProviderName.Access, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Stuff   ("",0,0,"")), N(() => L<String,Int32?,Int32?,String,String>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.PadRight("",0,' ') ), N(() => L<String,Int32?,Char?,String>        ((p0,p1,p2)    => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ') ), N(() => L<String,Int32?,Char?,String>        ((p0,p1,p2)    => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },
					{ M(() => Sql.MakeDateTime(0,0,0)), N(() => L<Int32?,Int32?,Int32?,DateTime?>    ((y,m,d)       => MakeDateTime2(y, m, d)))                                   },

					{ M(() => Sql.ConvertTo<String>.From(Guid.Empty)), N(() => L<Guid,String>(p => Sql.Lower(Sql.Substring(p.ToString(), 2, 36)))) },

					{ M(() => Sql.Ceiling((Decimal)0)), N(() => L<Decimal?,Decimal?>(p => -Sql.Floor(-p) )) },
					{ M(() => Sql.Ceiling((Double) 0)), N(() => L<Double?, Double?> (p => -Sql.Floor(-p) )) },

					{ M(() => Sql.Cot  (0)    ), N(() => L<Double?,Double?>           ((Double? v)             => Sql.Cos(v) / Sql.Sin(v)       )) },
					{ M(() => Sql.Cosh (0)    ), N(() => L<Double?,Double?>           ((Double? v)             => (Sql.Exp(v) + Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Log  (0m, 0)), N(() => L<Decimal?,Decimal?,Decimal?>((Decimal? m,Decimal? n) => Sql.Log(n) / Sql.Log(m)       )) },
					{ M(() => Sql.Log  (0.0,0)), N(() => L<Double?,Double?,Double?>   ((Double? m,Double? n)   => Sql.Log(n) / Sql.Log(m)       )) },
					{ M(() => Sql.Log10(0.0)  ), N(() => L<Double?,Double?>           ((Double? n)             => Sql.Log(n) / Sql.Log(10.0)    )) },

					{ M(() => Sql.Degrees((Decimal?)0)), N(() => L<Decimal?,Decimal?>((Decimal? v) => (Decimal?)         (          v.Value  * (180 / (Decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((Double?) 0)), N(() => L<Double?, Double?> ((Double?  v) => (Double?)          (          v.Value  * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int16?)  0)), N(() => L<Int16?,  Int16?>  ((Int16?   v) => (Int16?)  AccessInt(AccessInt(v.Value) * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int32?)  0)), N(() => L<Int32?,  Int32?>  ((Int32?   v) => (Int32?)  AccessInt(AccessInt(v.Value) * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int64?)  0)), N(() => L<Int64?,  Int64?>  ((Int64?   v) => (Int64?)  AccessInt(AccessInt(v.Value) * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((SByte?)  0)), N(() => L<SByte?,  SByte?>  ((SByte?   v) => (SByte?)  AccessInt(AccessInt(v.Value) * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Single?) 0)), N(() => L<Single?, Single?> ((Single?  v) => (Single?)          (          v.Value  * (180 / Math.PI)))) },

					{ M(() => Sql.Round      (0m)   ), N(() => L<Decimal?,Decimal?>  ((Decimal? d) => d - Sql.Floor(d) == 0.5m && Sql.Floor(d) % 2 == 0? Sql.Ceiling(d) : AccessRound(d, 0))) },
					{ M(() => Sql.Round      (0.0)  ), N(() => L<Double?, Double?>   ((Double?  d) => d - Sql.Floor(d) == 0.5  && Sql.Floor(d) % 2 == 0? Sql.Ceiling(d) : AccessRound(d, 0))) },
					{ M(() => Sql.Round      (0m, 0)), N(() => L<Decimal?,Int32?,Decimal?>((Decimal? v,Int32? p)=> (Decimal?)(
						p == 1 ? Sql.Round(v * 10) / 10 :
						p == 2 ? Sql.Round(v * 10) / 10 :
						p == 3 ? Sql.Round(v * 10) / 10 :
						p == 4 ? Sql.Round(v * 10) / 10 :
						p == 5 ? Sql.Round(v * 10) / 10 :
								 Sql.Round(v * 10) / 10))) },
					{ M(() => Sql.Round      (0.0,0)), N(() => L<Double?,Int32?,Double?>((Double? v,Int32? p) => (Double?)(
						p == 1 ? Sql.Round(v * 10) / 10 :
						p == 2 ? Sql.Round(v * 10) / 10 :
						p == 3 ? Sql.Round(v * 10) / 10 :
						p == 4 ? Sql.Round(v * 10) / 10 :
						p == 5 ? Sql.Round(v * 10) / 10 :
								 Sql.Round(v * 10) / 10))) },
					{ M(() => Sql.RoundToEven(0m)   ), N(() => L<Decimal?,Decimal?>       ( v   => AccessRound(v, 0))) },
					{ M(() => Sql.RoundToEven(0.0)  ), N(() => L<Double?, Double?>        ( v   => AccessRound(v, 0))) },
					{ M(() => Sql.RoundToEven(0m, 0)), N(() => L<Decimal?,Int32?,Decimal?>((v,p)=> AccessRound(v, p))) },
					{ M(() => Sql.RoundToEven(0.0,0)), N(() => L<Double?, Int32?,Double?> ((v,p)=> AccessRound(v, p))) },

					{ M(() => Sql.Sinh(0)), N(() => L<Double?,Double?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)), N(() => L<Double?,Double?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },

					{ M(() => Sql.Truncate(0m)),  N(() => L<Decimal?,Decimal?>((Decimal? v) => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
					{ M(() => Sql.Truncate(0.0)), N(() => L<Double?,Double?>  ((Double?  v) => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
				}},

				#endregion

				#region SapHana

				{ ProviderName.SapHana, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Degrees((Decimal?)0)), N(() => L<Decimal?,Decimal?>((Decimal? v) => (Decimal?) (v.Value * (180 / (Decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((Double?) 0)), N(() => L<Double?, Double?> ((Double?  v) => (Double?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int16?)  0)), N(() => L<Int16?,  Int16?>  ((Int16?   v) => (Int16?)   (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int32?)  0)), N(() => L<Int32?,  Int32?>  ((Int32?   v) => (Int32?)   (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Int64?)  0)), N(() => L<Int64?,  Int64?>  ((Int64?   v) => (Int64?)   (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((SByte?)  0)), N(() => L<SByte?,  SByte?>  ((SByte?   v) => (SByte?)   (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((Single?) 0)), N(() => L<Single?, Single?> ((Single?  v) => (Single?)  (v.Value * (180 / Math.PI)))) },
					{ M(() => Sql.RoundToEven(0.0)  ), N(() => L<Double?,Double?>       ((Double? v)          => (double?)Sql.RoundToEven((decimal)v)))    },
					{ M(() => Sql.RoundToEven(0.0,0)), N(() => L<Double?,Int32?,Double?>((Double? v,Int32? p) => (double?)Sql.RoundToEven((decimal)v, p))) },
					{ M(() => Sql.Stuff("",0,0,"")), N(() => L<String,Int32?,Int32?,String,String>((String p0,Int32? p1,Int32? p2,String p3) => AltStuff (p0,  p1, p2, p3)))             },
				}},

				#endregion
			};
		}

		#endregion

		class TypeMember
		{
			public TypeMember(Type type, string member)
			{
				Type   = type;
				Member = member;
			}

			public readonly Type   Type;
			public readonly string Member;

			public override bool Equals(object obj)
			{
				var other = (TypeMember)obj;
				return Type == other.Type && string.Equals(Member, other.Member);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Type.GetHashCode() * 397) ^ Member.GetHashCode();
				}
			}
		}

		public static void MapMember(string providerName, Type objectType, MemberInfo memberInfo, LambdaExpression expression)
		{
			Dictionary<TypeMember,IExpressionInfo> dic;

			if (!_typeMembers.TryGetValue(providerName, out dic))
				_typeMembers.Add(providerName, dic = new Dictionary<TypeMember,IExpressionInfo>());

			var expr = new LazyExpressionInfo();

			expr.SetExpression(expression);

			dic[new TypeMember(objectType, memberInfo.Name)] = expr;

			_checkUserNamespace = false;
		}

		public static void MapMember(string providerName, Type objectType, MemberInfo memberInfo, IExpressionInfo expressionInfo)
		{
			Dictionary<TypeMember,IExpressionInfo> dic;

			if (!_typeMembers.TryGetValue(providerName, out dic))
				_typeMembers.Add(providerName, dic = new Dictionary<TypeMember,IExpressionInfo>());

			dic[new TypeMember(objectType, memberInfo.Name)] = expressionInfo;

			_checkUserNamespace = false;
		}

		public static void MapMember<TR>               (string providerName, Type objectType, Expression<Func<TR>>                memberInfo, Expression<Func<TR>>                expression) { MapMember(providerName, objectType, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<TR>               (                     Type objectType, Expression<Func<TR>>                memberInfo, Expression<Func<TR>>                expression) { MapMember("",           objectType, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,TR>            (string providerName, Type objectType, Expression<Func<T1,TR>>             memberInfo, Expression<Func<T1,TR>>             expression) { MapMember(providerName, objectType, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,TR>            (                     Type objectType, Expression<Func<T1,TR>>             memberInfo, Expression<Func<T1,TR>>             expression) { MapMember("",           objectType, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,TR>         (string providerName, Type objectType, Expression<Func<T1,T2,TR>>          memberInfo, Expression<Func<T1,T2,TR>>          expression) { MapMember(providerName, objectType, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,TR>         (                     Type objectType, Expression<Func<T1,T2,TR>>          memberInfo, Expression<Func<T1,T2,TR>>          expression) { MapMember("",           objectType, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,TR>      (string providerName, Type objectType, Expression<Func<T1,T2,T3,TR>>       memberInfo, Expression<Func<T1,T2,T3,TR>>       expression) { MapMember(providerName, objectType, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,TR>      (                     Type objectType, Expression<Func<T1,T2,T3,TR>>       memberInfo, Expression<Func<T1,T2,T3,TR>>       expression) { MapMember("",           objectType, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,TR>   (string providerName, Type objectType, Expression<Func<T1,T2,T3,T4,TR>>    memberInfo, Expression<Func<T1,T2,T3,T4,TR>>    expression) { MapMember(providerName, objectType, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,TR>   (                     Type objectType, Expression<Func<T1,T2,T3,T4,TR>>    memberInfo, Expression<Func<T1,T2,T3,T4,TR>>    expression) { MapMember("",           objectType, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,T5,TR>(string providerName, Type objectType, Expression<Func<T1,T2,T3,T4,T5,TR>> memberInfo, Expression<Func<T1,T2,T3,T4,T5,TR>> expression) { MapMember(providerName, objectType, MemberHelper.GetMemberInfo(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,T5,TR>(                     Type objectType, Expression<Func<T1,T2,T3,T4,T5,TR>> memberInfo, Expression<Func<T1,T2,T3,T4,T5,TR>> expression) { MapMember("",           objectType, MemberHelper.GetMemberInfo(memberInfo), expression); }

		static TypeMember MT<T>(Expression<Func<object>> func)
		{
			return new TypeMember(typeof(T), MemberHelper.GetMemberInfo(func).Name);
		}

		static readonly Dictionary<string,Dictionary<TypeMember,IExpressionInfo>> _typeMembers = new Dictionary<string,Dictionary<TypeMember,IExpressionInfo>>
		{
			{ "", new Dictionary<TypeMember,IExpressionInfo> {

				#region ToString

				{ MT<Boolean>(() => ((Boolean)true).ToString()), N(() => L<Boolean, String>((Boolean p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<Byte   >(() => ((Byte)    0)  .ToString()), N(() => L<Byte,    String>((Byte    p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<Char   >(() => ((Char)   '0') .ToString()), N(() => L<Char,    String>((Char    p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<Decimal>(() => ((Decimal) 0)  .ToString()), N(() => L<Decimal, String>((Decimal p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<Double >(() => ((Double)  0)  .ToString()), N(() => L<Double,  String>((Double  p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<Int16  >(() => ((Int16)   0)  .ToString()), N(() => L<Int16,   String>((Int16   p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<Int32  >(() => ((Int32)   0)  .ToString()), N(() => L<Int32,   String>((Int32   p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<Int64  >(() => ((Int64)   0)  .ToString()), N(() => L<Int64,   String>((Int64   p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<SByte  >(() => ((SByte)   0)  .ToString()), N(() => L<SByte,   String>((SByte   p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<Single >(() => ((Single)  0)  .ToString()), N(() => L<Single,  String>((Single  p0) => Sql.ConvertTo<string>.From(p0) )) },
//				{ MT<String >(() => ((String) "0") .ToString()), N(() => L<String,  String>((String  p0) => p0                             )) },
				{ MT<UInt16 >(() => ((UInt16)  0)  .ToString()), N(() => L<UInt16,  String>((UInt16  p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<UInt32 >(() => ((UInt32)  0)  .ToString()), N(() => L<UInt32,  String>((UInt32  p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<UInt64 >(() => ((UInt64)  0)  .ToString()), N(() => L<UInt64,  String>((UInt64  p0) => Sql.ConvertTo<string>.From(p0) )) },

				#endregion

			}},
		};

		#region Sql specific

		[CLSCompliant(false)]
		[Sql.Function("RTrim", 0)]
		public static string TrimRight(string str, char[] trimChars)
		{
			return str == null ? null : str.TrimEnd(trimChars);
		}

		[CLSCompliant(false)]
		[Sql.Function("LTrim", 0)]
		public static string TrimLeft(string str, char[] trimChars)
		{
			return str == null ? null : str.TrimStart(trimChars);
		}

		#endregion

		#region Provider specific functions

		[Sql.Function]
		public static int? ConvertToCaseCompareTo(string str, string value)
		{
			return str == null || value == null ? (int?)null : str.CompareTo(value);
		}

		// Access, DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite
		//
		[Sql.Function]
		public static string AltStuff(string str, int? startLocation, int? length, string value)
		{
			return Sql.Stuff(str, startLocation, length, value);
		}

		// DB2
		//
		[Sql.Function]
		public static string VarChar(object obj, int? size)
		{
			return obj.ToString();
		}

		// DB2
		//
		[Sql.Function]
		public static string Hex(Guid? guid)
		{
			return guid == null ? null : guid.ToString();
		}

#pragma warning disable 3019

		// DB2, PostgreSQL, Access, MS SQL, SqlCe
		//
		[CLSCompliant(false)]
		[Sql.Function]
		[Sql.Function(ProviderName.DB2,        "Repeat")]
		[Sql.Function(ProviderName.PostgreSQL, "Repeat")]
		[Sql.Function(ProviderName.Access,     "String", 1, 0)]
		public static string Replicate(string str, int? count)
		{
			if (str == null || count == null)
				return null;

			var sb = new StringBuilder(str.Length * count.Value);

			for (var i = 0; i < count; i++)
				sb.Append(str);

			return sb.ToString();
		}

		[CLSCompliant(false)]
		[Sql.Function]
		[Sql.Function(ProviderName.DB2,        "Repeat")]
		[Sql.Function(ProviderName.PostgreSQL, "Repeat")]
		[Sql.Function(ProviderName.Access,     "String", 1, 0)]
		public static string Replicate(char? ch, int? count)
		{
			if (ch == null || count == null)
				return null;

			var sb = new StringBuilder(count.Value);

			for (var i = 0; i < count; i++)
				sb.Append(ch);

			return sb.ToString();
		}

		// SqlServer
		//
		[Sql.Function]
		public static DateTime? DateAdd(Sql.DateParts part, int? number, int? days)
		{
			return days == null ? null : Sql.DateAdd(part, number, new DateTime(1900, 1, days.Value + 1));
		}

		// MSSQL
		//
		[Sql.Function]
		public static Decimal? Round(Decimal? value, int precision, int mode) { return 0; }
		[Sql.Function]
		public static Double?  Round(Double?  value, int precision, int mode) { return 0; }

		// Access
		//
		[Sql.Function(ProviderName.Access, "DateSerial")]
		public static DateTime? MakeDateTime2(int? year, int? month, int? day)
		{
			return year == null || month == null || day == null?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value);
		}

		// Access
		//
		[CLSCompliant(false)]
		[Sql.Function("Int", 0)]
		public static T AccessInt<T>(T value)
		{
			return value;
		}

		// Access
		//
		[CLSCompliant(false)]
		[Sql.Function("Round", 0, 1)]
		public static T AccessRound<T>(T value, int? precision) { return value; }

		// Firebird
		//
		[Sql.Function("PI", ServerSideOnly = true)]
		public static decimal DecimalPI() { return (decimal)Math.PI; }
		[Sql.Function("PI", ServerSideOnly = true)]
		public static double  DoublePI () { return          Math.PI; }

		// Informix
		//
		[Sql.Function]
		public static DateTime? Mdy(int? month, int? day, int? year)
		{
			return year == null || month == null || day == null ?
				(DateTime?)null :
				new DateTime(year.Value, month.Value, day.Value);
		}

		#endregion

		#endregion
	}
}
