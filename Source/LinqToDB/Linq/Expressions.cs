using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

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
	[PublicAPI]
	public static class Expressions
	{
		#region MapMember

		static MemberHelper.MemberInfoWithType NormalizeMemeberInfo(MemberHelper.MemberInfoWithType memberInfoWithType)
		{
			if (memberInfoWithType.Type == null || memberInfoWithType.Type == memberInfoWithType.MemberInfo.ReflectedType)
			{
				return memberInfoWithType;
			}

			return new MemberHelper.MemberInfoWithType(memberInfoWithType.Type,
				memberInfoWithType.Type.GetMemberEx(memberInfoWithType.MemberInfo) ?? memberInfoWithType.MemberInfo);
		}

		public static void MapMember(MemberInfo memberInfo, LambdaExpression expression)
		{
			MapMember("", memberInfo, expression);
		}

		public static void MapMember(Type objectType, MemberInfo memberInfo, LambdaExpression expression)
		{
			MapMember("", objectType, memberInfo, expression);
		}

		public static void MapMember(MemberInfo memberInfo, IExpressionInfo expressionInfo)
		{
			MapMember("", memberInfo, expressionInfo);
		}

		public static void MapMember(string providerName, MemberHelper.MemberInfoWithType memberInfoWithType, LambdaExpression expression)
		{
			memberInfoWithType = NormalizeMemeberInfo(memberInfoWithType);

			if (!Members.TryGetValue(providerName, out var dic))
				Members.Add(providerName, dic = new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo>());

			var expr = new LazyExpressionInfo();

			expr.SetExpression(expression);

			dic[memberInfoWithType] = expr;

			_checkUserNamespace = false;
		}

		public static void MapMember(string providerName, MemberInfo memberInfo, LambdaExpression expression)
		{
			MapMember(providerName, new MemberHelper.MemberInfoWithType(memberInfo.ReflectedType, memberInfo), expression);
		}

		public static void MapMember(string providerName, MemberInfo memberInfo, IExpressionInfo expressionInfo)
		{
			if (!Members.TryGetValue(providerName, out var dic))
				Members.Add(providerName, dic = new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo>());

			dic[new MemberHelper.MemberInfoWithType(memberInfo.ReflectedType, memberInfo)] = expressionInfo;

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

		public static void MapMember<T>(Expression<Func<T,object?>> memberInfo, LambdaExpression expression)
		{
			MapMember("", M(memberInfo), expression);
		}

		public static void MapMember<T>(string providerName, Expression<Func<T,object?>> memberInfo, LambdaExpression expression)
		{
			MapMember(providerName, M(memberInfo), expression);
		}

		public static void MapMember<TR>               (string providerName, Expression<Func<TR>>                memberInfo, Expression<Func<TR>>                expression) { MapMember(providerName, MemberHelper.GetMemberInfoWithType(memberInfo), expression); }
		public static void MapMember<TR>               (                     Expression<Func<TR>>                memberInfo, Expression<Func<TR>>                expression) { MapMember("",           MemberHelper.GetMemberInfoWithType(memberInfo), expression); }
		public static void MapMember<T1,TR>            (string providerName, Expression<Func<T1,TR>>             memberInfo, Expression<Func<T1,TR>>             expression) { MapMember(providerName, MemberHelper.GetMemberInfoWithType(memberInfo), expression); }
		public static void MapMember<T1,TR>            (                     Expression<Func<T1,TR>>             memberInfo, Expression<Func<T1,TR>>             expression) { MapMember("",           MemberHelper.GetMemberInfoWithType(memberInfo), expression); }
		public static void MapMember<T1,T2,TR>         (string providerName, Expression<Func<T1,T2,TR>>          memberInfo, Expression<Func<T1,T2,TR>>          expression) { MapMember(providerName, MemberHelper.GetMemberInfoWithType(memberInfo), expression); }
		public static void MapMember<T1,T2,TR>         (                     Expression<Func<T1,T2,TR>>          memberInfo, Expression<Func<T1,T2,TR>>          expression) { MapMember("",           MemberHelper.GetMemberInfoWithType(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,TR>      (string providerName, Expression<Func<T1,T2,T3,TR>>       memberInfo, Expression<Func<T1,T2,T3,TR>>       expression) { MapMember(providerName, MemberHelper.GetMemberInfoWithType(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,TR>      (                     Expression<Func<T1,T2,T3,TR>>       memberInfo, Expression<Func<T1,T2,T3,TR>>       expression) { MapMember("",           MemberHelper.GetMemberInfoWithType(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,TR>   (string providerName, Expression<Func<T1,T2,T3,T4,TR>>    memberInfo, Expression<Func<T1,T2,T3,T4,TR>>    expression) { MapMember(providerName, MemberHelper.GetMemberInfoWithType(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,TR>   (                     Expression<Func<T1,T2,T3,T4,TR>>    memberInfo, Expression<Func<T1,T2,T3,T4,TR>>    expression) { MapMember("",           MemberHelper.GetMemberInfoWithType(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,T5,TR>(string providerName, Expression<Func<T1,T2,T3,T4,T5,TR>> memberInfo, Expression<Func<T1,T2,T3,T4,T5,TR>> expression) { MapMember(providerName, MemberHelper.GetMemberInfoWithType(memberInfo), expression); }
		public static void MapMember<T1,T2,T3,T4,T5,TR>(                     Expression<Func<T1,T2,T3,T4,T5,TR>> memberInfo, Expression<Func<T1,T2,T3,T4,T5,TR>> expression) { MapMember("",           MemberHelper.GetMemberInfoWithType(memberInfo), expression); }

		#endregion

		#region MapBinary

		static BinaryExpression GetBinaryNode(Expression expr)
		{
			while (expr.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs)
				expr = ((UnaryExpression)expr).Operand;

			if (expr is BinaryExpression binary)
				return binary;

			throw new ArgumentException($"Expression '{expr}' is not BinaryExpression node.");
		}

		/// <summary>
		/// Maps specific BinaryExpression to another Lambda expression during SQL generation.
		/// </summary>
		/// <param name="providerName">Name of database provider to use with this connection. <see cref="ProviderName"/> class for list of providers.</param>
		/// <param name="nodeType">NodeType of BinaryExpression <see cref="ExpressionType"/> which needs mapping.</param>
		/// <param name="leftType">Exact type of <see cref="BinaryExpression.Left"/> member.</param>
		/// <param name="rightType">Exact type of  <see cref="BinaryExpression.Right"/> member.</param>
		/// <param name="expression">Lambda expression which has to replace <see cref="BinaryExpression"/></param>
		/// <remarks>Note that method is not thread safe and has to be used only in Application's initialization section.</remarks>
		public static void MapBinary(
			string           providerName,
			ExpressionType   nodeType,
			Type             leftType,
			Type             rightType,
			LambdaExpression expression)
		{
			if (providerName == null) throw new ArgumentNullException(nameof(providerName));
			if (leftType     == null) throw new ArgumentNullException(nameof(leftType));
			if (rightType    == null) throw new ArgumentNullException(nameof(rightType));
			if (expression   == null) throw new ArgumentNullException(nameof(expression));

			if (!_binaries.Value.TryGetValue(providerName, out var dic))
				_binaries.Value.Add(providerName, dic = new Dictionary<Tuple<ExpressionType,Type,Type>,IExpressionInfo>());

			var expr = new LazyExpressionInfo();

			expr.SetExpression(expression);

			dic[Tuple.Create(nodeType, leftType, rightType)] = expr;

			_checkUserNamespace = false;
		}

		/// <summary>
		/// Maps specific <see cref="BinaryExpression"/> to another <see cref="LambdaExpression"/> during SQL generation.
		/// </summary>
		/// <param name="nodeType">NodeType of BinaryExpression <see cref="ExpressionType"/> which needs mapping.</param>
		/// <param name="leftType">Exact type of <see cref="BinaryExpression.Left"/> member.</param>
		/// <param name="rightType">Exact type of  <see cref="BinaryExpression.Right"/> member.</param>
		/// <param name="expression">Lambda expression which has to replace <see cref="BinaryExpression"/>.</param>
		/// <remarks>Note that method is not thread safe and has to be used only in Application's initialization section.</remarks>
		public static void MapBinary(
			ExpressionType   nodeType,
			Type             leftType,
			Type             rightType,
			LambdaExpression expression)
		{
			MapBinary("", nodeType, leftType, rightType, expression);
		}

		/// <summary>
		/// Maps specific <see cref="BinaryExpression"/> to another <see cref="LambdaExpression"/> during SQL generation.
		/// </summary>
		/// <typeparam name="TLeft">Exact type of  <see cref="BinaryExpression.Left"/> member.</typeparam>
		/// <typeparam name="TRight">Exact type of  <see cref="BinaryExpression.Right"/> member.</typeparam>
		/// <typeparam name="TR">Result type of <paramref name="binaryExpression"/>.</typeparam>
		/// <param name="providerName">Name of database provider to use with this connection. <see cref="ProviderName"/> class for list of providers.</param>
		/// <param name="binaryExpression">Expression which has to be replaced.</param>
		/// <param name="expression">Lambda expression which has to replace <paramref name="binaryExpression"/>.</param>
		/// <remarks>Note that method is not thread safe and has to be used only in Application's initialization section.</remarks>
		public static void MapBinary<TLeft,TRight,TR>(
			string                            providerName,
			Expression<Func<TLeft,TRight,TR>> binaryExpression,
			Expression<Func<TLeft,TRight,TR>> expression)
		{
			MapBinary(providerName, GetBinaryNode(binaryExpression.Body).NodeType, typeof(TLeft), typeof(TRight), expression);
		}

		/// <summary>
		/// Maps specific <see cref="BinaryExpression"/> to another <see cref="LambdaExpression"/> during SQL generation.
		/// </summary>
		/// <typeparam name="TLeft">Exact type of  <see cref="BinaryExpression.Left"/> member.</typeparam>
		/// <typeparam name="TRight">Exact type of  <see cref="BinaryExpression.Right"/> member.</typeparam>
		/// <typeparam name="TR">Result type of <paramref name="binaryExpression"/>.</typeparam>
		/// <param name="binaryExpression">Expression which has to be replaced.</param>
		/// <param name="expression">Lambda expression which has to replace <paramref name="binaryExpression"/>.</param>
		/// <remarks>Note that method is not thread safe and has to be used only in Application's initialization section.</remarks>
		public static void MapBinary<TLeft,TRight,TR>(
			Expression<Func<TLeft,TRight,TR>> binaryExpression,
			Expression<Func<TLeft,TRight,TR>> expression)
		{
			MapBinary("", binaryExpression, expression);
		}

		#endregion

		#region IGenericInfoProvider

		static readonly Dictionary<Type,List<Type[]>> _genericConvertProviders = new Dictionary<Type,List<Type[]>>();

		static bool InitGenericConvertProvider(Type[] types, MappingSchema mappingSchema)
		{
			var changed = false;

			lock (_genericConvertProviders)
			{
				foreach (var type in _genericConvertProviders)
				{
					var args = type.Key.GetGenericArguments();

					if (args.Length == types.Length)
					{
						if (type.Value.Aggregate(false, (cur,ts) => cur || ts.SequenceEqual(types)))
							continue;

						var gtype    = type.Key.MakeGenericType(types);
						var provider = ActivatorExt.CreateInstance<IGenericInfoProvider>(gtype);

						provider.SetInfo(mappingSchema);

						type.Value.Add(types);

						changed = true;
					}
				}
			}

			return changed;
		}

		public static void SetGenericInfoProvider(Type type)
		{
			if (!type.IsGenericTypeDefinition)
				throw new LinqToDBException($"'{type}' must be a generic type.");

			if (!typeof(IGenericInfoProvider).IsSameOrParentOf(type))
				throw new LinqToDBException($"'{type}' must inherit from 'IGenericInfoProvider'.");

			if (!_genericConvertProviders.ContainsKey(type))
				lock (_genericConvertProviders)
					if (!_genericConvertProviders.ContainsKey(type))
						_genericConvertProviders[type] = new List<Type[]>();
		}

		#endregion

		#region Public Members

		static bool _checkUserNamespace = true;

		public static LambdaExpression? ConvertMember(MappingSchema mappingSchema, Type? objectType, MemberInfo mi)
		{
			if (objectType != null)
			{
				mi = objectType.GetMemberEx(mi) ?? mi;
			}

			var result = ConvertMemberInternal(mappingSchema, objectType, mi);

			return result;
		}

		static LambdaExpression? ConvertMemberInternal(MappingSchema mappingSchema, Type? objectType, MemberInfo mi)
		{
			if (_checkUserNamespace)
			{
				if (IsUserNamespace(mi.DeclaringType!.Namespace))
					return null;

				_checkUserNamespace = false;
			}

			IExpressionInfo? expr;

			var memberKey = new MemberHelper.MemberInfoWithType(objectType ?? mi.DeclaringType, mi);

			foreach (var configuration in mappingSchema.ConfigurationList)
				if (Members.TryGetValue(configuration, out var dic))
					if (dic.TryGetValue(memberKey, out expr))
						return expr.GetExpression(mappingSchema);

			Type[]? args = null;

			if (mi is MethodInfo mm)
			{
				var isTypeGeneric   = mm.DeclaringType!.IsGenericType && !mm.DeclaringType.IsGenericTypeDefinition;
				var isMethodGeneric = mm.IsGenericMethod && !mm.IsGenericMethodDefinition;

				if (isTypeGeneric || isMethodGeneric)
				{
					var typeGenericArgs   = isTypeGeneric   ? mm.DeclaringType.GetGenericArguments() : [];
					var methodGenericArgs = isMethodGeneric ? mm.GetGenericArguments()               : [];

					args = typeGenericArgs.SequenceEqual(methodGenericArgs) ?
						typeGenericArgs : typeGenericArgs.Concat(methodGenericArgs).ToArray();
				}
			}

			if (args != null && InitGenericConvertProvider(args, mappingSchema))
				foreach (var configuration in mappingSchema.ConfigurationList)
					if (Members.TryGetValue(configuration, out var dic))
						if (dic.TryGetValue(memberKey, out expr))
							return expr.GetExpression(mappingSchema);

			if (!Members[""].TryGetValue(memberKey, out expr))
			{
				if (mi is MethodInfo && mi.Name == "CompareString" && mi.DeclaringType!.FullName!.StartsWith("Microsoft.VisualBasic.CompilerServices."))
				{
					lock (_memberSync)
					{
						if (!Members[""].TryGetValue(memberKey, out expr))
						{
							expr = new LazyExpressionInfo();

#pragma warning disable CA1304, MA0011 // use CultureInfo
							((LazyExpressionInfo)expr).SetExpression(L<string,string,bool,int>((s1,s2,b) => b ? string.CompareOrdinal(s1.ToUpper(), s2.ToUpper()) : string.CompareOrdinal(s1, s2)));
#pragma warning restore CA1304, MA0011 // use CultureInfo

							Members[""].Add(memberKey, expr);
						}
					}
				}
			}

			if (expr == null)
			{
				if (mi is MethodInfo method &&
				    (method.IsVirtual || method.DeclaringType != null && method.ReflectedType != null && method.DeclaringType.IsAssignableFrom(method.ReflectedType)))
				{
					// walking up through hierarchy to find registered converter
					//
					if (method.ReflectedType is not null && method.ReflectedType != typeof(object) &&
					    method.ReflectedType.BaseType is not null)
					{
						var baseType = method.ReflectedType.BaseType;
						var newMi    = baseType.GetMemberEx(mi);

						if (newMi != null && newMi.ReflectedType != mi.ReflectedType)
						{
							var converted = ConvertMemberInternal(mappingSchema, objectType, newMi);
							if (converted != null)
								return converted;
						}
					}
				}

				if (memberKey.Type != mi.ReflectedType)
				{
					var converted = ConvertMemberInternal(mappingSchema, mi.ReflectedType, mi);
					if (converted != null)
						return converted;
				}
			}

			return expr?.GetExpression(mappingSchema);
		}

		/// <summary>
		/// Searches for registered BinaryExpression mapping and returns LambdaExpression which has to replace this expression.
		/// </summary>
		/// <param name="mappingSchema">Current mapping schema.</param>
		/// <param name="binaryExpression">Expression which has to be replaced.</param>
		/// <returns>Returns registered LambdaExpression or <see langword="null"/>.</returns>
		public static LambdaExpression? ConvertBinary(MappingSchema mappingSchema, BinaryExpression binaryExpression)
		{
			if (!_binaries.IsValueCreated)
				return null;

			IExpressionInfo? expr;
			Dictionary<Tuple<ExpressionType,Type,Type>,IExpressionInfo>? dic;

			var binaries = _binaries.Value;
			var key      = Tuple.Create(binaryExpression.NodeType, binaryExpression.Left.Type, binaryExpression.Right.Type);

			foreach (var configuration in mappingSchema.ConfigurationList)
			{
				if (binaries.TryGetValue(configuration, out dic))
				{
					if (dic.TryGetValue(key, out expr))
						return expr.GetExpression(mappingSchema);
				}
			}

			if (binaries.TryGetValue("", out dic))
			{
				if (dic.TryGetValue(key, out expr))
					return expr.GetExpression(mappingSchema);
			}

			return null;
		}

		#endregion

		#region Function Mapping

		#region Helpers

		static bool IsUserNamespace(string? typeNamespace)
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

		public static MemberHelper.MemberInfoWithType M<T>(Expression<Func<T,object?>> func)
		{
			return NormalizeMemeberInfo(MemberHelper.GetMemberInfoWithType(func));
		}

		public static MemberHelper.MemberInfoWithType M<T>(Expression<Func<T>> func)
		{
			return NormalizeMemeberInfo(MemberHelper.GetMemberInfoWithType(func));
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
			public Func<LambdaExpression>? Lambda;

			LambdaExpression? _expression;

			public LambdaExpression GetExpression(MappingSchema mappingSchema)
			{
				return _expression ??= Lambda!();
			}

			public void SetExpression(LambdaExpression expression)
			{
				_expression = expression;
			}
		}

		static Dictionary<string,Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo>> Members
		{
			get
			{
				if (field == null)
				{
					lock (_memberSync)
						field ??= LoadMembers();
				}

				return field;
			}
		}

		#region Mapping

		private static readonly Lock _memberSync = new();

		static readonly Lazy<Dictionary<string,Dictionary<Tuple<ExpressionType,Type,Type>,IExpressionInfo>>> _binaries =
			new Lazy<Dictionary<string,Dictionary<Tuple<ExpressionType,Type,Type>,IExpressionInfo>>>(() => new Dictionary<string,Dictionary<Tuple<ExpressionType,Type,Type>,IExpressionInfo>>());

		#region Common

#pragma warning disable CS0618 // Type or member is obsolete
		static readonly Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> _commonMembers = new()
		{
			#region string

			{ M(() => "".Substring  (0)       ), N(() => L<string?,int,string?>            ((obj,p0)       => Sql.Substring(obj, p0 + 1, obj!.Length - p0))) },
			{ M(() => "".Substring  (0,0)     ), N(() => L<string?,int,int,string?>        ((obj,p0,p1)    => Sql.Substring(obj, p0 + 1, p1))) },
#pragma warning disable RS0030 // Do not used banned APIs
			{ M(() => "".IndexOf    ("")      ), N(() => L<string,string,int>              ((obj,p0)       => p0.Length == 0                    ? 0  : (Sql.CharIndex(p0, obj)!                      .Value) - 1)) },
			{ M(() => "".IndexOf    ("",0)    ), N(() => L<string,string,int,int>          ((obj,p0,p1)    => p0.Length == 0 && obj.Length > p1 ? p1 : (Sql.CharIndex(p0, obj,               p1 + 1)!.Value) - 1)) },
			{ M(() => "".IndexOf    ("",0,0)  ), N(() => L<string,string,int,int,int>      ((obj,p0,p1,p2) => p0.Length == 0 && obj.Length > p1 ? p1 : (Sql.CharIndex(p0, Sql.Left(obj, p2), p1)!    .Value) - 1)) },
			{ M(() => "".IndexOf    (' ')     ), N(() => L<string,char,int>                ((obj,p0)       =>                                          (Sql.CharIndex(p0, obj)!                      .Value) - 1)) },
			{ M(() => "".IndexOf    (' ',0)   ), N(() => L<string,char,int,int>            ((obj,p0,p1)    =>                                          (Sql.CharIndex(p0, obj,               p1 + 1)!.Value) - 1)) },
			{ M(() => "".IndexOf    (' ',0,0) ), N(() => L<string,char,int,int,int>        ((obj,p0,p1,p2) =>                                          (Sql.CharIndex(p0, Sql.Left(obj, p2), p1)     ?? 0) - 1)) },
			{ M(() => "".LastIndexOf("")      ), N(() => L<string,string,int>              ((obj,p0)       => p0.Length == 0 ? obj.Length - 1 : (Sql.CharIndex(p0, obj)!                           .Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(Sql.Reverse(p0), Sql.Reverse(obj))!                              .Value) - p0.Length + 1)) },
#pragma warning disable CA1514 // CA1514: Avoid redundant length argument
			{ M(() => "".LastIndexOf("",0)    ), N(() => L<string,string,int,int>          ((obj,p0,p1)    => p0.Length == 0 ? p1             : (Sql.CharIndex(p0, obj,                    p1 + 1)!.Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(Sql.Reverse(p0), Sql.Reverse(obj.Substring(p1, obj.Length - p1)))!.Value) - p0.Length + 1)) },
#pragma warning restore CA1514 // CA1514: Avoid redundant length argument
			{ M(() => "".LastIndexOf("",0,0)  ), N(() => L<string,string,int,int,int>      ((obj,p0,p1,p2) => p0.Length == 0 ? p1             : (Sql.CharIndex(p0, Sql.Left(obj, p1 + p2), p1 + 1)!.Value) == 0 ? -1 :    p1 + p2 - (Sql.CharIndex(Sql.Reverse(p0), Sql.Reverse(obj.Substring(p1, p2)))!             .Value) - p0.Length + 1)) },
			{ M(() => "".LastIndexOf(' ')     ), N(() => L<string,char,int>                ((obj,p0)       => (Sql.CharIndex(p0, obj)!                           .Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(p0, Sql.Reverse(obj))!                               .Value))) },
#pragma warning disable CA1514 // CA1514: Avoid redundant length argument
			{ M(() => "".LastIndexOf(' ',0)   ), N(() => L<string,char,int,int>            ((obj,p0,p1)    => (Sql.CharIndex(p0, obj, p1 + 1)!                   .Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(p0, Sql.Reverse(obj.Substring(p1, obj.Length - p1)))!.Value))) },
#pragma warning restore CA1514 // CA1514: Avoid redundant length argument
			{ M(() => "".LastIndexOf(' ',0,0) ), N(() => L<string,char,int,int,int>        ((obj,p0,p1,p2) => (Sql.CharIndex(p0, Sql.Left(obj, p1 + p2), p1 + 1)!.Value) == 0 ? -1 : p1 + p2    - (Sql.CharIndex(p0, Sql.Reverse(obj.Substring(p1, p2)))!             .Value))) },
#pragma warning restore RS0030 // Do not used banned APIs
			{ M(() => "".Insert     (0,"")    ), N(() => L<string?,int,string?,string?>    ((obj,p0,p1)    => obj!.Length == p0 ? obj + p1 : Sql.Stuff(obj, p0 + 1, 0, p1))) },
			{ M(() => "".Remove     (0)       ), N(() => L<string?,int,string?>            ((obj,p0)       => Sql.Left     (obj, p0))) },
			{ M(() => "".Remove     (0,0)     ), N(() => L<string?,int,int,string?>        ((obj,p0,p1)    => Sql.Stuff    (obj, p0 + 1, p1, ""))) },
			{ M(() => "".PadRight   (0)       ), N(() => L<string?,int,string?>            ((obj,p0)       => Sql.PadRight (obj, p0, ' '))) },
			{ M(() => "".PadRight   (0,' ')   ), N(() => L<string?,int,char,string?>       ((obj,p0,p1)    => Sql.PadRight (obj, p0, p1))) },
			{ M(() => "".Trim       ()        ), N(() => L<string?,string?>                (obj            => Sql.Trim     (obj))) },

#if NET8_0_OR_GREATER
			{ M(() => "".TrimEnd    ()        ), N(() => L<string,string?>                 (obj      => TrimRight(obj)))     },
			{ M(() => "".TrimEnd    (' ')     ), N(() => L<string,char,string?>            ((obj,ch) => TrimRight(obj, ch))) },
			{ M(() => "".TrimStart  ()        ), N(() => L<string,string?>                 (obj      => TrimLeft (obj)))     },
			{ M(() => "".TrimStart  (' ')     ), N(() => L<string,char,string?>            ((obj,ch) => TrimLeft (obj, ch))) },
#endif
			{ M(() => "".TrimEnd    ((char[])null!)), N(() => L<string,char[],string?>     ((obj,ch) => TrimRight(obj, ch))) },
			{ M(() => "".TrimStart  ((char[])null!)), N(() => L<string,char[],string?>     ((obj,ch) => TrimLeft (obj, ch))) },
#pragma warning disable CA1304, MA0011 // use CultureInfo
			{ M(() => "".ToLower    ()        ), N(() => L<string?,string?>                (obj => Sql.Lower(obj))) },
			{ M(() => "".ToUpper    ()        ), N(() => L<string?,string?>                (obj => Sql.Upper(obj))) },
#pragma warning restore CA1304, MA0011 // use CultureInfo
			{ M(() => "".CompareTo  ("")      ), N(() => L<string,string,int>              ((obj,p0) => ConvertToCaseCompareTo(obj, p0)!.Value)) },
#pragma warning disable MA0107 // object.ToString is bad, m'kay?
			{ M(() => "".CompareTo  (1)       ), N(() => L<string,object,int>              ((obj,p0) => ConvertToCaseCompareTo(obj, p0.ToString())!.Value)) },

			{ M(() => string.Concat((object)null!)                             ), N(() => L<object,string?>                    (p0         => p0.ToString()))           },
			{ M(() => string.Concat((object)null!,(object)null!)               ), N(() => L<object,object,string>              ((p0,p1)    => p0.ToString() + p1))      },
			{ M(() => string.Concat((object)null!,(object)null!,(object)null!) ), N(() => L<object,object,object,string>       ((p0,p1,p2) => p0.ToString() + p1 + p2)) },
#pragma warning restore MA0107 // object.ToString is bad, m'kay?
			{ M(() => string.Concat((object[])null!)                           ), N(() => L<object?[],string?>                 (ps            => Sql.Concat(ps)))          },
			{ M(() => string.Concat("","")                                     ), N(() => L<string,string,string>              ((p0,p1)       => p0 + p1))                 },
			{ M(() => string.Concat("","","")                                  ), N(() => L<string,string,string,string>       ((p0,p1,p2)    => p0 + p1 + p2))            },
			{ M(() => string.Concat("","","","")                               ), N(() => L<string,string,string,string,string>((p0,p1,p2,p3) => p0 + p1 + p2 + p3))       },
			{ M(() => string.Concat((string[])null!)                           ), N(() => L<string?[],string?>                 (ps            => Sql.Concat(ps)))          },

			{ M(() => string.IsNullOrEmpty ("")    ),                                         N(() => L<string,bool>                                   (p0                 => p0 == null || p0.Length == 0)) },
			{ M(() => string.IsNullOrWhiteSpace("")),                                         N(() => L<string,bool>                                   (p0                 => Sql.IsNullOrWhiteSpace(p0))) },
			{ M(() => string.CompareOrdinal("","")),                                          N(() => L<string,string,int>                             ((s1,s2)            => s1.CompareTo(s2))) },
			{ M(() => string.CompareOrdinal("",0,"",0,0)),                                    N(() => L<string,int,string,int,int,int>                 ((s1,i1,s2,i2,l)    => s1.Substring(i1, l).CompareTo(s2.Substring(i2, l)))) },
			{ M(() => string.Compare       ("","")),                                          N(() => L<string,string,int>                             ((s1,s2)            => s1.CompareTo(s2))) },
			{ M(() => string.Compare       ("",0,"",0,0)),                                    N(() => L<string,int,string,int,int,int>                 ((s1,i1,s2,i2,l)    => s1.Substring(i1,l).CompareTo(s2.Substring(i2,l)))) },
#pragma warning disable CA1304, MA0011 // use CultureInfo
			{ M(() => string.Compare       ("","",true)),                                     N(() => L<string,string,bool,int>                        ((s1,s2,b)          => b ? s1.ToLower().CompareTo(s2.ToLower()) : s1.CompareTo(s2))) },
			{ M(() => string.Compare       ("",0,"",0,0,true)),                               N(() => L<string,int,string,int,int,bool,int>            ((s1,i1,s2,i2,l,b)  => b ? s1.Substring(i1,l).ToLower().CompareTo(s2.Substring(i2, l).ToLower()) : s1.Substring(i1, l).CompareTo(s2.Substring(i2, l)))) },
			{ M(() => string.Compare       ("",0,"",0,0,StringComparison.OrdinalIgnoreCase)), N(() => L<string,int,string,int,int,StringComparison,int>((s1,i1,s2,i2,l,sc) => sc == StringComparison.CurrentCultureIgnoreCase || sc==StringComparison.OrdinalIgnoreCase ? s1.Substring(i1,l).ToLower().CompareTo(s2.Substring(i2, l).ToLower()) : s1.Substring(i1, l).CompareTo(s2.Substring(i2, l)))) },
			{ M(() => string.Compare       ("","",StringComparison.OrdinalIgnoreCase)),       N(() => L<string,string,StringComparison,int>            ((s1,s2,sc)         => sc == StringComparison.CurrentCultureIgnoreCase || sc==StringComparison.OrdinalIgnoreCase ? s1.ToLower().CompareTo(s2.ToLower()) : s1.CompareTo(s2))) },
#pragma warning restore CA1304, MA0011 // use CultureInfo

			{ M(() => AltStuff("",0,0,"")), N(() => L<string,int?,int?,string,string>((p0,p1,p2,p3) => Sql.Left(p0, p1 - 1) + p3 + Sql.Right(p0, p0.Length - (p1 + p2 - 1)))) },

			#endregion

			#region Binary

			{ M(() => ((Binary)null!).Length ), N(() => L<Binary,int>(obj => Sql.Length(obj)!.Value)) },

			#endregion

			#region Byte[]

			{ M(() => ((byte[])null!).Length ), N(() => L<byte[],int>(obj => Sql.Length(obj)!.Value)) },

			#endregion

			#region ConvertTo

			{ M(() => Sql.ConvertTo<string>.From(Guid.Empty)), N(() => L<Guid,string?>(p => p.ToString())) },

			#endregion

			#region Parse

			{ M(() => bool.    Parse("")), N(() => L<string,bool>    (p0 => Sql.ConvertTo<bool>.    From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => byte.    Parse("")), N(() => L<string,byte>    (p0 => Sql.ConvertTo<byte>.    From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => char.    Parse("")), N(() => L<string,char>    (p0 => Sql.ConvertTo<char>.    From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => DateTime.Parse("")), N(() => L<string,DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => decimal. Parse("")), N(() => L<string,decimal> (p0 => Sql.ConvertTo<decimal>. From(p0))) },
			{ M(() => double.  Parse("")), N(() => L<string,double>  (p0 => Sql.ConvertTo<double>.  From(p0))) },
			{ M(() => short.   Parse("")), N(() => L<string,short>   (p0 => Sql.ConvertTo<short>.   From(p0))) },
			{ M(() => int.     Parse("")), N(() => L<string,int>     (p0 => Sql.ConvertTo<int>.     From(p0))) },
			{ M(() => long.    Parse("")), N(() => L<string,long>    (p0 => Sql.ConvertTo<long>.    From(p0))) },
			{ M(() => sbyte.   Parse("")), N(() => L<string,sbyte>   (p0 => Sql.ConvertTo<sbyte>.   From(p0))) },
			{ M(() => float.   Parse("")), N(() => L<string,float>   (p0 => Sql.ConvertTo<float>.   From(p0))) },
			{ M(() => ushort.  Parse("")), N(() => L<string,ushort>  (p0 => Sql.ConvertTo<ushort>.  From(p0))) },
			{ M(() => uint.    Parse("")), N(() => L<string,uint>    (p0 => Sql.ConvertTo<uint>.    From(p0))) },
			{ M(() => ulong.   Parse("")), N(() => L<string,ulong>   (p0 => Sql.ConvertTo<ulong>.   From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs

#if SUPPORTS_DATEONLY
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => DateOnly.Parse("")), N(() => L<string,DateOnly>(p0 => Sql.ConvertTo<DateOnly>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
#endif

			#endregion

			#region ToString

#pragma warning disable MA0044 // Remove useless ToString call
			{ M(() => ((string) "0") .ToString()), N(() => L<string,  string>(p0 => p0)) },
#pragma warning restore MA0044 // Remove useless ToString call

			#endregion

			#region Convert

			#region ToByte

			{ M(() => Convert.ToByte((bool)   true)), N(() => L<bool,    byte>(p0 => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((byte)    0)  ), N(() => L<byte,    byte>(p0 => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((char)   '0') ), N(() => L<char,    byte>(p0 => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte(DateTime.Now) ), N(() => L<DateTime,byte>(p0 => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((decimal) 0)  ), N(() => L<decimal, byte>(p0 => Sql.ConvertTo<byte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToByte((double)  0)  ), N(() => L<double,  byte>(p0 => Sql.ConvertTo<byte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToByte((short)   0)  ), N(() => L<short,   byte>(p0 => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((int)     0)  ), N(() => L<int,     byte>(p0 => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((long)    0)  ), N(() => L<long,    byte>(p0 => Sql.ConvertTo<byte>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToByte((object)  0)  ), N(() => L<object,  byte>(p0 => Sql.ConvertTo<byte>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToByte((sbyte)   0)  ), N(() => L<sbyte,   byte>(p0 => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((float)   0)  ), N(() => L<float,   byte>(p0 => Sql.ConvertTo<byte>.From(Sql.RoundToEven(p0)))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToByte((string) "0") ), N(() => L<string,  byte>(p0 => Sql.ConvertTo<byte>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToByte((ushort)  0)  ), N(() => L<ushort,  byte>(p0 => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((uint)    0)  ), N(() => L<uint,    byte>(p0 => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((ulong)   0)  ), N(() => L<ulong,   byte>(p0 => Sql.ConvertTo<byte>.From(p0))) },

#endregion

			#region ToChar

			{ M(() => Convert.ToChar((bool)   true)), N(() => L<bool,    char>(p0 => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((byte)    0)  ), N(() => L<byte,    char>(p0 => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((char)   '0') ), N(() => L<char,    char>(p0 => p0                          )) },
			{ M(() => Convert.ToChar(DateTime.Now) ), N(() => L<DateTime,char>(p0 => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((decimal) 0)  ), N(() => L<decimal, char>(p0 => Sql.ConvertTo<char>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToChar((double)  0)  ), N(() => L<double,  char>(p0 => Sql.ConvertTo<char>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToChar((short)   0)  ), N(() => L<short,   char>(p0 => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((int)     0)  ), N(() => L<int,     char>(p0 => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((long)    0)  ), N(() => L<long,    char>(p0 => Sql.ConvertTo<char>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToChar((object)  0)  ), N(() => L<object,  char>(p0 => Sql.ConvertTo<char>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToChar((sbyte)   0)  ), N(() => L<sbyte,   char>(p0 => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((float)   0)  ), N(() => L<float,   char>(p0 => Sql.ConvertTo<char>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToChar((string) "0") ), N(() => L<string,  char>(p0 => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((ushort)  0)  ), N(() => L<ushort,  char>(p0 => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((uint)    0)  ), N(() => L<uint,    char>(p0 => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((ulong)   0)  ), N(() => L<ulong,   char>(p0 => Sql.ConvertTo<char>.From(p0))) },

			#endregion

			#region ToDateTime

#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToDateTime((object)  0)  ), N(() => L<object,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((string) "0") ), N(() => L<string,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToDateTime((bool)   true)), N(() => L<bool,    DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((byte)    0)  ), N(() => L<byte,    DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((char)   '0') ), N(() => L<char,    DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime(DateTime.Now) ), N(() => L<DateTime,DateTime>(p0 => p0                              )) },
			{ M(() => Convert.ToDateTime((decimal) 0)  ), N(() => L<decimal, DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((double)  0)  ), N(() => L<double,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((short)   0)  ), N(() => L<short,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((int)     0)  ), N(() => L<int,   DateTime>(p0   => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((long)    0)  ), N(() => L<long,   DateTime>(p0  => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((sbyte)   0)  ), N(() => L<sbyte,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((float)   0)  ), N(() => L<float,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((ushort)  0)  ), N(() => L<ushort,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((uint)    0)  ), N(() => L<uint,  DateTime>(p0   => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((ulong)   0)  ), N(() => L<ulong,  DateTime>(p0  => Sql.ConvertTo<DateTime>.From(p0))) },

			#endregion

			#region ToDecimal

			{ M(() => Convert.ToDecimal((bool)   true)), N(() => L<bool,    decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((byte)    0)  ), N(() => L<byte,    decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((char)   '0') ), N(() => L<char,    decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal(DateTime.Now) ), N(() => L<DateTime,decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((decimal) 0)  ), N(() => L<decimal, decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((double)  0)  ), N(() => L<double,  decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((short)   0)  ), N(() => L<short,   decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((int)     0)  ), N(() => L<int,     decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((long)    0)  ), N(() => L<long,    decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToDecimal((object)  0)  ), N(() => L<object,  decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToDecimal((sbyte)   0)  ), N(() => L<sbyte,   decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((float)   0)  ), N(() => L<float,   decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToDecimal((string) "0") ), N(() => L<string,  decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToDecimal((ushort)  0)  ), N(() => L<ushort,  decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((uint)    0)  ), N(() => L<uint,    decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((ulong)   0)  ), N(() => L<ulong,   decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },

#endregion

			#region ToDouble

			{ M(() => Convert.ToDouble((bool)   true)), N(() => L<bool,    double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble((byte)    0)  ), N(() => L<byte,    double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble((char)   '0') ), N(() => L<char,    double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble(DateTime.Now) ), N(() => L<DateTime,double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble((decimal) 0)  ), N(() => L<decimal, double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble((double)  0)  ), N(() => L<double,  double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble((short)   0)  ), N(() => L<short,   double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble((int)     0)  ), N(() => L<int,     double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble((long)    0)  ), N(() => L<long,    double>(p0 => Sql.ConvertTo<double>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToDouble((object)  0)  ), N(() => L<object,  double>(p0 => Sql.ConvertTo<double>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToDouble((sbyte)   0)  ), N(() => L<sbyte,   double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble((float)   0)  ), N(() => L<float,   double>(p0 => Sql.ConvertTo<double>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToDouble((string) "0") ), N(() => L<string,  double>(p0 => Sql.ConvertTo<double>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToDouble((ushort)  0)  ), N(() => L<ushort,  double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble((uint)    0)  ), N(() => L<uint,    double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble((ulong)   0)  ), N(() => L<ulong,   double>(p0 => Sql.ConvertTo<double>.From(p0))) },

			#endregion

			#region ToInt64

			{ M(() => Convert.ToInt64((bool)   true)), N(() => L<bool,    long>(p0 => Sql.ConvertTo<long>.From(p0))) },
			{ M(() => Convert.ToInt64((byte)    0)  ), N(() => L<byte,    long>(p0 => Sql.ConvertTo<long>.From(p0))) },
			{ M(() => Convert.ToInt64((char)   '0') ), N(() => L<char,    long>(p0 => Sql.ConvertTo<long>.From(p0))) },
			{ M(() => Convert.ToInt64(DateTime.Now) ), N(() => L<DateTime,long>(p0 => Sql.ConvertTo<long>.From(p0))) },
			{ M(() => Convert.ToInt64((decimal) 0)  ), N(() => L<decimal, long>(p0 => Sql.ConvertTo<long>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt64((double)  0)  ), N(() => L<double,  long>(p0 => Sql.ConvertTo<long>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt64((short)   0)  ), N(() => L<short,   long>(p0 => Sql.ConvertTo<long>.From(p0))) },
			{ M(() => Convert.ToInt64((int)     0)  ), N(() => L<int,     long>(p0 => Sql.ConvertTo<long>.From(p0))) },
			{ M(() => Convert.ToInt64((long)    0)  ), N(() => L<long,    long>(p0 => Sql.ConvertTo<long>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToInt64((object)  0)  ), N(() => L<object,  long>(p0 => Sql.ConvertTo<long>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToInt64((sbyte)   0)  ), N(() => L<sbyte,   long>(p0 => Sql.ConvertTo<long>.From(p0))) },
			{ M(() => Convert.ToInt64((float)   0)  ), N(() => L<float,   long>(p0 => Sql.ConvertTo<long>.From(Sql.RoundToEven(p0)))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToInt64((string) "0") ), N(() => L<string,  long>(p0 => Sql.ConvertTo<long>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToInt64((ushort)  0)  ), N(() => L<ushort,  long>(p0 => Sql.ConvertTo<long>.From(p0))) },
			{ M(() => Convert.ToInt64((uint)    0)  ), N(() => L<uint,    long>(p0 => Sql.ConvertTo<long>.From(p0))) },
			{ M(() => Convert.ToInt64((ulong)   0)  ), N(() => L<ulong,   long>(p0 => Sql.ConvertTo<long>.From(p0))) },

			#endregion

			#region ToInt32

			{ M(() => Convert.ToInt32((bool)   true)), N(() => L<bool,    int>(p0 => Sql.ConvertTo<int>.From(p0))) },
			{ M(() => Convert.ToInt32((byte)    0)  ), N(() => L<byte,    int>(p0 => Sql.ConvertTo<int>.From(p0))) },
			{ M(() => Convert.ToInt32((char)   '0') ), N(() => L<char,    int>(p0 => Sql.ConvertTo<int>.From(p0))) },
			{ M(() => Convert.ToInt32(DateTime.Now) ), N(() => L<DateTime,int>(p0 => Sql.ConvertTo<int>.From(p0))) },
			{ M(() => Convert.ToInt32((decimal) 0)  ), N(() => L<decimal, int>(p0 => Sql.ConvertTo<int>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt32((double)  0)  ), N(() => L<double,  int>(p0 => Sql.ConvertTo<int>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt32((short)   0)  ), N(() => L<short,   int>(p0 => Sql.ConvertTo<int>.From(p0))) },
			{ M(() => Convert.ToInt32((int)     0)  ), N(() => L<int,     int>(p0 => Sql.ConvertTo<int>.From(p0))) },
			{ M(() => Convert.ToInt32((long)    0)  ), N(() => L<long,    int>(p0 => Sql.ConvertTo<int>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToInt32((object)  0)  ), N(() => L<object,  int>(p0 => Sql.ConvertTo<int>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToInt32((sbyte)   0)  ), N(() => L<sbyte,   int>(p0 => Sql.ConvertTo<int>.From(p0))) },
			{ M(() => Convert.ToInt32((float)   0)  ), N(() => L<float,   int>(p0 => Sql.ConvertTo<int>.From(Sql.RoundToEven(p0)))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToInt32((string) "0") ), N(() => L<string,  int>(p0 => Sql.ConvertTo<int>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToInt32((ushort)  0)  ), N(() => L<ushort,  int>(p0 => Sql.ConvertTo<int>.From(p0))) },
			{ M(() => Convert.ToInt32((uint)    0)  ), N(() => L<uint,    int>(p0 => Sql.ConvertTo<int>.From(p0))) },
			{ M(() => Convert.ToInt32((ulong)   0)  ), N(() => L<ulong,   int>(p0 => Sql.ConvertTo<int>.From(p0))) },

			#endregion

			#region ToInt16

			{ M(() => Convert.ToInt16((bool)   true)), N(() => L<bool,    short>(p0 => Sql.ConvertTo<short>.From(p0))) },
			{ M(() => Convert.ToInt16((byte)    0)  ), N(() => L<byte,    short>(p0 => Sql.ConvertTo<short>.From(p0))) },
			{ M(() => Convert.ToInt16((char)   '0') ), N(() => L<char,    short>(p0 => Sql.ConvertTo<short>.From(p0))) },
			{ M(() => Convert.ToInt16(DateTime.Now) ), N(() => L<DateTime,short>(p0 => Sql.ConvertTo<short>.From(p0))) },
			{ M(() => Convert.ToInt16((decimal) 0)  ), N(() => L<decimal, short>(p0 => Sql.ConvertTo<short>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt16((double)  0)  ), N(() => L<double,  short>(p0 => Sql.ConvertTo<short>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt16((short)   0)  ), N(() => L<short,   short>(p0 => Sql.ConvertTo<short>.From(p0))) },
			{ M(() => Convert.ToInt16((int)     0)  ), N(() => L<int,     short>(p0 => Sql.ConvertTo<short>.From(p0))) },
			{ M(() => Convert.ToInt16((long)    0)  ), N(() => L<long,    short>(p0 => Sql.ConvertTo<short>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToInt16((object)  0)  ), N(() => L<object,  short>(p0 => Sql.ConvertTo<short>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToInt16((sbyte)   0)  ), N(() => L<sbyte,   short>(p0 => Sql.ConvertTo<short>.From(p0))) },
			{ M(() => Convert.ToInt16((float)   0)  ), N(() => L<float,   short>(p0 => Sql.ConvertTo<short>.From(Sql.RoundToEven(p0)))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToInt16((string) "0") ), N(() => L<string,  short>(p0 => Sql.ConvertTo<short>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToInt16((ushort)  0)  ), N(() => L<ushort,  short>(p0 => Sql.ConvertTo<short>.From(p0))) },
			{ M(() => Convert.ToInt16((uint)    0)  ), N(() => L<uint,    short>(p0 => Sql.ConvertTo<short>.From(p0))) },
			{ M(() => Convert.ToInt16((ulong)   0)  ), N(() => L<ulong,   short>(p0 => Sql.ConvertTo<short>.From(p0))) },

			#endregion

			#region ToSByte

			{ M(() => Convert.ToSByte((bool)   true)), N(() => L<bool,    sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
			{ M(() => Convert.ToSByte((byte)    0)  ), N(() => L<byte,    sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
			{ M(() => Convert.ToSByte((char)   '0') ), N(() => L<char,    sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
			{ M(() => Convert.ToSByte(DateTime.Now) ), N(() => L<DateTime,sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
			{ M(() => Convert.ToSByte((decimal) 0)  ), N(() => L<decimal, sbyte>(p0 => Sql.ConvertTo<sbyte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToSByte((double)  0)  ), N(() => L<double,  sbyte>(p0 => Sql.ConvertTo<sbyte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToSByte((short)   0)  ), N(() => L<short,   sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
			{ M(() => Convert.ToSByte((int)     0)  ), N(() => L<int,     sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
			{ M(() => Convert.ToSByte((long)    0)  ), N(() => L<long,    sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToSByte((object)  0)  ), N(() => L<object,  sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToSByte((sbyte)   0)  ), N(() => L<sbyte,   sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
			{ M(() => Convert.ToSByte((float)   0)  ), N(() => L<float,   sbyte>(p0 => Sql.ConvertTo<sbyte>.From(Sql.RoundToEven(p0)))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToSByte((string) "0") ), N(() => L<string,  sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToSByte((ushort)  0)  ), N(() => L<ushort,  sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
			{ M(() => Convert.ToSByte((uint)    0)  ), N(() => L<uint,    sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
			{ M(() => Convert.ToSByte((ulong)   0)  ), N(() => L<ulong,   sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },

			#endregion

			#region ToSingle

			{ M(() => Convert.ToSingle((bool)   true)), N(() => L<bool,    float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((byte)    0)  ), N(() => L<byte,    float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((char)   '0') ), N(() => L<char,    float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle(DateTime.Now) ), N(() => L<DateTime,float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((decimal) 0)  ), N(() => L<decimal, float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((double)  0)  ), N(() => L<double,  float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((short)   0)  ), N(() => L<short,   float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((int)     0)  ), N(() => L<int,     float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((long)    0)  ), N(() => L<long,    float>(p0 => Sql.ConvertTo<float>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToSingle((object)  0)  ), N(() => L<object,  float>(p0 => Sql.ConvertTo<float>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToSingle((sbyte)   0)  ), N(() => L<sbyte,   float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((float)   0)  ), N(() => L<float,   float>(p0 => Sql.ConvertTo<float>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToSingle((string) "0") ), N(() => L<string,  float>(p0 => Sql.ConvertTo<float>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToSingle((ushort)  0)  ), N(() => L<ushort,  float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((uint)    0)  ), N(() => L<uint,    float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((ulong)   0)  ), N(() => L<ulong,   float>(p0 => Sql.ConvertTo<float>.From(p0))) },

			#endregion

			#region ToString

			{ M(() => Convert.ToString((bool)   true)), N(() => L<bool,    string>(p0 => Sql.ConvertTo<string>.From(p0))) },
#pragma warning disable RS0030, CA1305 // Do not used banned APIs
			{ M(() => Convert.ToString((byte)    0)  ), N(() => L<byte,    string>(p0 => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => Convert.ToString((char)   '0') ), N(() => L<char,    string>(p0 => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => Convert.ToString(DateTime.Now) ), N(() => L<DateTime,string>(p0 => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => Convert.ToString((decimal) 0)  ), N(() => L<decimal, string>(p0 => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => Convert.ToString((double)  0)  ), N(() => L<double,  string>(p0 => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => Convert.ToString((short)   0)  ), N(() => L<short,   string>(p0 => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => Convert.ToString((int)     0)  ), N(() => L<int,     string>(p0 => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => Convert.ToString((long)    0)  ), N(() => L<long,    string>(p0 => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => Convert.ToString((object)  0)  ), N(() => L<object,  string>(p0 => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => Convert.ToString((sbyte)   0)  ), N(() => L<sbyte,   string>(p0 => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => Convert.ToString((float)   0)  ), N(() => L<float,   string>(p0 => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => Convert.ToString((string) "0") ), N(() => L<string,  string>(p0 => p0                            )) },
			{ M(() => Convert.ToString((ushort)  0)  ), N(() => L<ushort,  string>(p0 => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => Convert.ToString((uint)    0)  ), N(() => L<uint,    string>(p0 => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => Convert.ToString((ulong)   0)  ), N(() => L<ulong,   string>(p0 => Sql.ConvertTo<string>.From(p0))) },
#pragma warning restore RS0030, CA1305 // Do not used banned APIs
			#endregion

			#region ToUInt16

			{ M(() => Convert.ToUInt16((bool)   true)), N(() => L<bool,    ushort>(p0 => Sql.ConvertTo<ushort>.From(p0))) },
			{ M(() => Convert.ToUInt16((byte)    0)  ), N(() => L<byte,    ushort>(p0 => Sql.ConvertTo<ushort>.From(p0))) },
			{ M(() => Convert.ToUInt16((char)   '0') ), N(() => L<char,    ushort>(p0 => Sql.ConvertTo<ushort>.From(p0))) },
			{ M(() => Convert.ToUInt16(DateTime.Now) ), N(() => L<DateTime,ushort>(p0 => Sql.ConvertTo<ushort>.From(p0))) },
			{ M(() => Convert.ToUInt16((decimal) 0)  ), N(() => L<decimal, ushort>(p0 => Sql.ConvertTo<ushort>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt16((double)  0)  ), N(() => L<double,  ushort>(p0 => Sql.ConvertTo<ushort>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt16((short)   0)  ), N(() => L<short,   ushort>(p0 => Sql.ConvertTo<ushort>.From(p0))) },
			{ M(() => Convert.ToUInt16((int)     0)  ), N(() => L<int,     ushort>(p0 => Sql.ConvertTo<ushort>.From(p0))) },
			{ M(() => Convert.ToUInt16((long)    0)  ), N(() => L<long,    ushort>(p0 => Sql.ConvertTo<ushort>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToUInt16((object)  0)  ), N(() => L<object,  ushort>(p0 => Sql.ConvertTo<ushort>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToUInt16((sbyte)   0)  ), N(() => L<sbyte,   ushort>(p0 => Sql.ConvertTo<ushort>.From(p0))) },
			{ M(() => Convert.ToUInt16((float)   0)  ), N(() => L<float,   ushort>(p0 => Sql.ConvertTo<ushort>.From(Sql.RoundToEven(p0)))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToUInt16((string) "0") ), N(() => L<string,  ushort>(p0 => Sql.ConvertTo<ushort>.From(p0)) ) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToUInt16((ushort)  0)  ), N(() => L<ushort,  ushort>(p0 => Sql.ConvertTo<ushort>.From(p0)) ) },
			{ M(() => Convert.ToUInt16((uint)    0)  ), N(() => L<uint,    ushort>(p0 => Sql.ConvertTo<ushort>.From(p0)) ) },
			{ M(() => Convert.ToUInt16((ulong)   0)  ), N(() => L<ulong,   ushort>(p0 => Sql.ConvertTo<ushort>.From(p0)) ) },

			#endregion

			#region ToUInt32

			{ M(() => Convert.ToUInt32((bool)   true)), N(() => L<bool,    uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
			{ M(() => Convert.ToUInt32((byte)    0)  ), N(() => L<byte,    uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
			{ M(() => Convert.ToUInt32((char)   '0') ), N(() => L<char,    uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
			{ M(() => Convert.ToUInt32(DateTime.Now) ), N(() => L<DateTime,uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
			{ M(() => Convert.ToUInt32((decimal) 0)  ), N(() => L<decimal, uint>(p0 => Sql.ConvertTo<uint>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt32((double)  0)  ), N(() => L<double,  uint>(p0 => Sql.ConvertTo<uint>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt32((short)   0)  ), N(() => L<short,   uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
			{ M(() => Convert.ToUInt32((int)     0)  ), N(() => L<int,     uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
			{ M(() => Convert.ToUInt32((long)    0)  ), N(() => L<long,    uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToUInt32((object)  0)  ), N(() => L<object,  uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToUInt32((sbyte)   0)  ), N(() => L<sbyte,   uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
			{ M(() => Convert.ToUInt32((float)   0)  ), N(() => L<float,   uint>(p0 => Sql.ConvertTo<uint>.From(Sql.RoundToEven(p0)))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToUInt32((string) "0") ), N(() => L<string,  uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToUInt32((ushort)  0)  ), N(() => L<ushort,  uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
			{ M(() => Convert.ToUInt32((uint)    0)  ), N(() => L<uint,    uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
			{ M(() => Convert.ToUInt32((ulong)   0)  ), N(() => L<ulong,   uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },

			#endregion

			#region ToUInt64

			{ M(() => Convert.ToUInt64((bool)   true)), N(() => L<bool,    ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64((byte)    0)  ), N(() => L<byte,    ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64((char)   '0') ), N(() => L<char,    ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64(DateTime.Now) ), N(() => L<DateTime,ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64((decimal) 0)  ), N(() => L<decimal, ulong>(p0 => Sql.ConvertTo<ulong>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt64((double)  0)  ), N(() => L<double,  ulong>(p0 => Sql.ConvertTo<ulong>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt64((short)   0)  ), N(() => L<short,   ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64((int)     0)  ), N(() => L<int,     ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64((long)    0)  ), N(() => L<long,    ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToUInt64((object)  0)  ), N(() => L<object,  ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToUInt64((sbyte)   0)  ), N(() => L<sbyte,   ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64((float)   0)  ), N(() => L<float,   ulong>(p0 => Sql.ConvertTo<ulong>.From(Sql.RoundToEven(p0)))) },
#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToUInt64((string) "0") ), N(() => L<string,  ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs
			{ M(() => Convert.ToUInt64((ushort)  0)  ), N(() => L<ushort,  ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64((uint)    0)  ), N(() => L<uint,    ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64((ulong)   0)  ), N(() => L<ulong,   ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },

			#endregion

			#endregion

			#region Math

			{ M(() => Math.Acos   (0)   ),       N(() => L<double,double>       (p     => Sql.Acos   (p)!   .Value )) },
			{ M(() => Math.Asin   (0)   ),       N(() => L<double,double>       (p     => Sql.Asin   (p)!   .Value )) },
			{ M(() => Math.Atan   (0)   ),       N(() => L<double,double>       (p     => Sql.Atan   (p)!   .Value )) },
			{ M(() => Math.Atan2  (0,0) ),       N(() => L<double,double,double>((x,y) => Sql.Atan2  (x, y)!.Value )) },
			{ M(() => Math.Ceiling((decimal)0)), N(() => L<decimal,decimal>     (p     => Sql.Ceiling(p)!   .Value )) },
			{ M(() => Math.Ceiling((double)0)),  N(() => L<double,double>       (p     => Sql.Ceiling(p)!   .Value )) },
			{ M(() => Math.Cos            (0)),  N(() => L<double,double>       (p     => Sql.Cos    (p)!   .Value )) },
			{ M(() => Math.Cosh           (0)),  N(() => L<double,double>       (p     => Sql.Cosh   (p)!   .Value )) },
			{ M(() => Math.Exp            (0)),  N(() => L<double,double>       (p     => Sql.Exp    (p)!   .Value )) },
			{ M(() => Math.Floor ((decimal)0)),  N(() => L<decimal,decimal>     (p     => Sql.Floor  (p)!   .Value )) },
			{ M(() => Math.Floor  ((double)0)),  N(() => L<double,double>       (p     => Sql.Floor  (p)!   .Value )) },
			{ M(() => Math.Log            (0)),  N(() => L<double,double>       (p     => Sql.Log    (p)!   .Value )) },
			{ M(() => Math.Log          (0,0)),  N(() => L<double,double,double>((m,n) => Sql.Log    (n, m)!.Value )) },
			{ M(() => Math.Log10          (0)),  N(() => L<double,double>       (p     => Sql.Log10  (p)!   .Value )) },

			{ M(() => Math.Pow        (0,0) ), N(() => L<double,double,double>    ((x,y) => Sql.Power(x, y)!.Value )) },

			{ M(() => Math.Sign  ((decimal)0)), N(() => L<decimal,int>(p => Sql.Sign(p)!.Value )) },
			{ M(() => Math.Sign  ((double) 0)), N(() => L<double, int>(p => Sql.Sign(p)!.Value )) },
			{ M(() => Math.Sign  ((short)  0)), N(() => L<short,  int>(p => Sql.Sign(p)!.Value )) },
			{ M(() => Math.Sign  ((int)    0)), N(() => L<int,    int>(p => Sql.Sign(p)!.Value )) },
			{ M(() => Math.Sign  ((long)   0)), N(() => L<long,   int>(p => Sql.Sign(p)!.Value )) },
			{ M(() => Math.Sign  ((sbyte)  0)), N(() => L<sbyte,  int>(p => Sql.Sign(p)!.Value )) },
			{ M(() => Math.Sign  ((float)  0)), N(() => L<float,  int>(p => Sql.Sign(p)!.Value )) },

			{ M(() => Math.Sin   (0)), N(() => L<double,double>(p => Sql.Sin (p)!.Value )) },
			{ M(() => Math.Sinh  (0)), N(() => L<double,double>(p => Sql.Sinh(p)!.Value )) },
			{ M(() => Math.Sqrt  (0)), N(() => L<double,double>(p => Sql.Sqrt(p)!.Value )) },
			{ M(() => Math.Tan   (0)), N(() => L<double,double>(p => Sql.Tan (p)!.Value )) },
			{ M(() => Math.Tanh  (0)), N(() => L<double,double>(p => Sql.Tanh(p)!.Value )) },

			{ M(() => Math.Truncate(0m)),  N(() => L<decimal,decimal>(p => Sql.Truncate(p)!.Value )) },
			{ M(() => Math.Truncate(0.0)), N(() => L<double,double>  (p => Sql.Truncate(p)!.Value )) },

			#endregion

			#region SqlTypes

			{ M(() => new SqlBoolean().Value),   N(() => L<SqlBoolean,bool>(obj => (bool)obj))          },
			{ M(() => new SqlBoolean().IsFalse), N(() => L<SqlBoolean,bool>(obj => (bool)obj == false)) },
			{ M(() => new SqlBoolean().IsTrue),  N(() => L<SqlBoolean,bool>(obj => (bool)obj == true))  },
			{ M(() => SqlBoolean.True),          N(() => L                 (()               => true))  },
			{ M(() => SqlBoolean.False),         N(() => L                 (()               => false)) },

			#endregion
		};
#pragma warning restore CS0618 // Type or member is obsolete

		#endregion

#pragma warning disable CS0618 // Type or member is obsolete
		static Dictionary<string,Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo>> LoadMembers()
		{
			var members = new Dictionary<string,Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo>>
			{
				{ "", _commonMembers },

				#region SqlServer

				{ ProviderName.SqlServer, new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> {
					{ M(() => Sql.PadRight("",0,' ') ), N(() => L<string,int?,char,string>       ((p0,p1,p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ') ), N(() => L<string,int?,char,string>       ((p0,p1,p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },
					{ M(() => Sql.Trim    ("")       ), N(() => L<string,string?>                (p0         => Sql.TrimLeft(Sql.TrimRight(p0)))) },
					{ M(() => Sql.Cosh(0)            ), N(() => L<double?,double?>               ( v         => (Sql.Exp(v) + Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Log(0m, 0)         ), N(() => L<decimal?,decimal?,decimal?>    ((m,n)      => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log(0.0,0)         ), N(() => L<double?,double?,double?>       ((m,n)      => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Sinh(0)            ), N(() => L<double?,double?>               ( v         => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)            ), N(() => L<double?,double?>               ( v         => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },
				}},

				#endregion

				#region SqlCe

				{ ProviderName.SqlCe, new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> {
					{ M(() => Sql.Left    ("",0)    ), N(() => L<string?,int?,string?>    ((p0,p1)    => Sql.Substring(p0, 1, p1))) },
					{ M(() => Sql.Right   ("",0)    ), N(() => L<string?,int?,string?>    ((p0,p1)    => Sql.Substring(p0, p0!.Length - p1 + 1, p1))) },
					{ M(() => Sql.PadRight("",0,' ')), N(() => L<string,int?,char?,string>((p0,p1,p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ')), N(() => L<string,int?,char?,string>((p0,p1,p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },
					{ M(() => Sql.Trim    ("")      ), N(() => L<string?,string?>      (p0            => Sql.TrimLeft(Sql.TrimRight(p0)))) },

					{ M(() => Sql.Cosh(0)    ), N(() => L<double?,double?>           ( v    => (Sql.Exp(v) + Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Log (0m, 0)), N(() => L<decimal?,decimal?,decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log (0.0,0)), N(() => L<double?,double?,double?>   ((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Sinh(0)    ), N(() => L<double?,double?>           ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)    ), N(() => L<double?,double?>           ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },
				}},

				#endregion

				#region DB2

				{ ProviderName.DB2, new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> {
					{ M(() => Sql.Space   (0)        ), N(() => L<int?,string>       ( p0                          => Sql.Convert(Sql.Types.VarChar(1000), Replicate(" ", p0)))) },
					{ M(() => Sql.Stuff   ("",0,0,"")), N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.PadRight("",0,' ') ), N(() => L<string,int?,char?,string>  ((p0,p1,p2)           => p0.Length > p1 ? p0 : p0 + VarChar(Replicate(p2, p1 - p0.Length)!, 1000))) },
					{ M(() => Sql.PadLeft ("",0,' ') ), N(() => L<string,int?,char?,string>  ((p0,p1,p2)           => p0.Length > p1 ? p0 : VarChar(Replicate(p2, p1 - p0.Length)!, 1000) + p0)) },

					{ M(() => Sql.ConvertTo<string>.From((decimal)0)), N(() => L<decimal,string?>(p => Sql.TrimLeft(Sql.Convert<string,decimal>(p), '0'))) },

					{ M(() => Sql.Log(0m, 0)), N(() => L<decimal?,decimal?,decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log(0.0,0)), N(() => L<double?,double?,double?>   ((m,n) => Sql.Log(n) / Sql.Log(m))) },
				}},

				#endregion

				#region Informix

				{ ProviderName.Informix, new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> {
					{ M(() => Sql.Left ("",0)     ), N(() => L<string?,int?,string?>             ((p0,p1)       => Sql.Substring(p0,  1, p1)))                   },
					{ M(() => Sql.Right("",0)     ), N(() => L<string?,int?,string?>             ((p0,p1)       => Sql.Substring(p0,  p0!.Length - p1 + 1, p1))) },
					{ M(() => Sql.Stuff("",0,0,"")), N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) =>     AltStuff (p0,  p1, p2, p3)))              },
					{ M(() => Sql.Space(0)        ), N(() => L<int?,string?>                     (p0            => Sql.PadRight (" ", p0, ' ')))                 },

					{ M(() => Sql.Cot (0)         ), N(() => L<double?,double?>(v => Sql.Cos(v) / Sql.Sin(v) ))        },
					{ M(() => Sql.Cosh(0)         ), N(() => L<double?,double?>(v => (Sql.Exp(v) + Sql.Exp(-v)) / 2 )) },

					{ M(() => Sql.Degrees((decimal?)0)), N(() => L<decimal?,decimal?>(v => (decimal?)(v!.Value * (180 / (decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((double?) 0)), N(() => L<double?, double?> (v => (double?) (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((short?)  0)), N(() => L<short?,  short?>  (v => (short?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((int?)    0)), N(() => L<int?,    int?>    (v => (int?)    (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((long?)   0)), N(() => L<long?,   long?>   (v => (long?)   (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((sbyte?)  0)), N(() => L<sbyte?,  sbyte?>  (v => (sbyte?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((float?)  0)), N(() => L<float?,  float?>  (v => (float?)  (v!.Value * (180 / Math.PI)))) },

					{ M(() => Sql.Log(0m, 0)), N(() => L<decimal?,decimal?,decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log(0.0,0)), N(() => L<double?,double?,double?>((m,n) => Sql.Log(n) / Sql.Log(m))) },

					{ M(() => Sql.Sign((decimal?)0)), N(() => L<decimal?,int?>(p => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((double?) 0)), N(() => L<double?, int?>(p => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((short?)  0)), N(() => L<short?,  int?>(p => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((int?)    0)), N(() => L<int?,    int?>(p => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((long?)   0)), N(() => L<long?,   int?>(p => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((sbyte?)  0)), N(() => L<sbyte?,  int?>(p => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((float?)  0)), N(() => L<float?,  int?>(p => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },

					{ M(() => Sql.Sinh(0)), N(() => L<double?,double?>(v => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)), N(() => L<double?,double?>(v => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) +Sql.Exp(-v)))) },
				}},

				#endregion

				#region Oracle

				{ ProviderName.Oracle, new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> {
					{ M(() => Sql.Left ("",0)     ), N(() => L<string?,int?,string?>             ((p0,p1)       => Sql.Substring(p0, 1, p1))) },
					{ M(() => Sql.Right("",0)     ), N(() => L<string?,int?,string?>             ((p0,p1)       => Sql.Substring(p0, p0!.Length - p1 + 1, p1))) },
					{ M(() => Sql.Stuff("",0,0,"")), N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.Space(0)        ), N(() => L<int?,string?>                     (p0            => Sql.PadRight(" ", p0, ' '))) },

					{ M(() => Sql.Cot  (0)),   N(() => L<double?,double?>(v => Sql.Cos(v) / Sql.Sin(v) )) },
					{ M(() => Sql.Log10(0.0)), N(() => L<double?,double?>(v => Sql.Log(10, v)          )) },

					{ M(() => Sql.Degrees((decimal?)0)), N(() => L<decimal?,decimal?>( v => (decimal?)(v!.Value * (180 / (decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((double?) 0)), N(() => L<double?, double?> ( v => (double?) (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((short?)  0)), N(() => L<short?,  short?>  ( v => (short?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((int?)    0)), N(() => L<int?,    int?>    ( v => (int?)    (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((long?)   0)), N(() => L<long?,   long?>   ( v => (long?)   (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((sbyte?)  0)), N(() => L<sbyte?,  sbyte?>  ( v => (sbyte?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((float?)  0)), N(() => L<float?,  float?>  ( v => (float?)  (v!.Value * (180 / Math.PI)))) },
				}},

				#endregion

				#region Firebird

				{ ProviderName.Firebird, new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> {
					{ M<string?>(_  => Sql.Space(0         )), N(() => L<int?,string?>                     ( p0           => Sql.PadRight(" ", p0, ' '))) },
					{ M<string?>(s  => Sql.Stuff(s, 0, 0, s)), N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3)))   },

					{ M(() => Sql.Degrees((decimal?)0)), N(() => L<decimal?,decimal?>(v => (decimal?)(v!.Value * 180 / DecimalPI()))) },
					{ M(() => Sql.Degrees((double?) 0)), N(() => L<double?, double?> (v => (double?) (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((short?)  0)), N(() => L<short?,  short?>  (v => (short?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((int?)    0)), N(() => L<int?,    int?>    (v => (int?)    (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((long?)   0)), N(() => L<long?,   long?>   (v => (long?)   (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((sbyte?)  0)), N(() => L<sbyte?,  sbyte?>  (v => (sbyte?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((float?)  0)), N(() => L<float?,  float?>  (v => (float?)  (v!.Value * (180 / Math.PI)))) },

					{ M(() => Sql.RoundToEven(0.0)  ), N(() => L<double?,double?>     (v     => (double?)Sql.RoundToEven((decimal)v!)))    },
					{ M(() => Sql.RoundToEven(0.0,0)), N(() => L<double?,int?,double?>((v,p) => (double?)Sql.RoundToEven((decimal)v!, p))) },
				}},

				#endregion

				#region MySql

				{ ProviderName.MySql, new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> {
					{ M<string>(s => Sql.Stuff(s, 0, 0, s)), N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },

					{ M(() => Sql.Cosh(0)), N(() => L<double?,double?>(v => (Sql.Exp(v) + Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Sinh(0)), N(() => L<double?,double?>(v => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)), N(() => L<double?,double?>(v => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },
				}},

				#endregion

				#region PostgreSQL

				{ ProviderName.PostgreSQL, new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> {
					{ M(() => Sql.Left ("",0)     ), N(() => L<string?,int?,string?>             ((p0,p1)       => Sql.Substring(p0, 1, p1))) },
					{ M(() => Sql.Right("",0)     ), N(() => L<string?,int?,string?>             ((p0,p1)       => Sql.Substring(p0, p0!.Length - p1 + 1, p1))) },
					{ M(() => Sql.Stuff("",0,0,"")), N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.Space(0)        ), N(() => L<int?,string?>                     (p0            => Replicate(" ", p0))) },

					{ M(() => Sql.Cosh(0)           ), N(() => L<double?,double?>     (v     => (Sql.Exp(v) + Sql.Exp(-v)) / 2 )) },
					{ M(() => Sql.Round      (0.0,0)), N(() => L<double?,int?,double?>((v,p) => (double?)Sql.Round      ((decimal)v!, p))) },
					{ M(() => Sql.RoundToEven(0.0)  ), N(() => L<double?,double?>     (v     => (double?)Sql.RoundToEven((decimal)v!)))    },
					{ M(() => Sql.RoundToEven(0.0,0)), N(() => L<double?,int?,double?>((v,p) => (double?)Sql.RoundToEven((decimal)v!, p))) },

					{ M(() => Sql.Log  ((double)0,0)), N(() => L<double?,double?,double?>((m,n) => (double?)Sql.Log((decimal)m!,(decimal)n!)!.Value)) },
					{ M(() => Sql.Sinh (0)          ), N(() => L<double?,double?>        (v     => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh (0)          ), N(() => L<double?,double?>        (v     => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },

					{ M(() => Sql.Truncate(0.0)     ), N(() => L<double?,double?>(v => (double?)Sql.Truncate((decimal)v!))) },
				}},

				#endregion

				#region SQLite

				{ ProviderName.SQLite, new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> {
					{ M(() => Sql.Stuff   ("",0,0,"")), N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.PadRight("",0,' ') ), N(() => L<string,int?,char?,string>         ((p0,p1,p2)    => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ') ), N(() => L<string,int?,char?,string>         ((p0,p1,p2)    => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },

					{ M(() => Sql.Log (0m, 0)), N(() => L<decimal?,decimal?,decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log (0.0,0)), N(() => L<double?,double?,double?>   ((m,n) => Sql.Log(n) / Sql.Log(m))) },

					{ M(() => Sql.Truncate(0m)),  N(() => L<decimal?,decimal?>(v => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
					{ M(() => Sql.Truncate(0.0)), N(() => L<double?,double?>  (v => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
				}},

				{ ProviderName.SQLiteMS, new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> {
					{ M(() => Math.Floor((decimal)0)), N(() => L<decimal,decimal>(x => x > 0 ? (int)x : (int)(x-0.9999999999999999m) )) },
					{ M(() => Math.Floor ((double)0)), N(() => L<double,double>  (x => x > 0 ? (int)x : (int)(x-0.9999999999999999) )) },
				}},

				#endregion

				#region Sybase

				{ ProviderName.Sybase, new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> {
					{ M(() => Sql.PadRight("",0,' ')), N(() => L<string,int?,char?,string>((p0,p1,p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ')), N(() => L<string,int?,char?,string>((p0,p1,p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },
					{ M(() => Sql.Trim    ("")      ), N(() => L<string?,string?>         ( p0        => Sql.TrimLeft(Sql.TrimRight(p0)))) },

					{ M(() => Sql.Cosh(0)    ),          N(() => L<double?,double?>           ( v    => (Sql.Exp(v) + Sql.Exp(-v)) / 2))  },
					{ M(() => Sql.Log (0m, 0)),          N(() => L<decimal?,decimal?,decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log (0.0,0)),          N(() => L<double?,double?,double?>   ((m,n) => Sql.Log(n) / Sql.Log(m)))    },

					{ M(() => Sql.Degrees((decimal?)0)), N(() => L<decimal?,decimal?>(v => (decimal?)(v!.Value * (180 / (decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((double?) 0)), N(() => L<double?, double?> (v => (double?) (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((short?)  0)), N(() => L<short?,  short?>  (v => (short?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((int?)    0)), N(() => L<int?,    int?>    (v => (int?)    (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((long?)   0)), N(() => L<long?,   long?>   (v => (long?)   (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((sbyte?)  0)), N(() => L<sbyte?,  sbyte?>  (v => (sbyte?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((float?)  0)), N(() => L<float?,  float?>  (v => (float?)  (v!.Value * (180 / Math.PI)))) },

					{ M(() => Sql.Sinh(0)), N(() => L<double?,double?>(v => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)), N(() => L<double?,double?>(v => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },

					{ M(() => Sql.Truncate(0m)),  N(() => L<decimal?,decimal?>(v => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
					{ M(() => Sql.Truncate(0.0)), N(() => L<double?,double?>  (v => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
				}},

				#endregion

				#region Access

				{ ProviderName.Access, new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> {
					{ M(() => Sql.Stuff   ("",0,0,"")), N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.PadRight("",0,' ') ), N(() => L<string,int?,char?,string>         ((p0,p1,p2)    => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ') ), N(() => L<string,int?,char?,string>         ((p0,p1,p2)    => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },

					{ M(() => Sql.Ceiling((decimal)0)), N(() => L<decimal?,decimal?>(p => -Sql.Floor(-p) )) },
					{ M(() => Sql.Ceiling((double) 0)), N(() => L<double?, double?> (p => -Sql.Floor(-p) )) },

					{ M(() => Sql.Cot  (0)    ), N(() => L<double?,double?>           (v => Sql.Cos(v) / Sql.Sin(v)       )) },
					{ M(() => Sql.Cosh (0)    ), N(() => L<double?,double?>           (v => (Sql.Exp(v) + Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Log  (0m, 0)), N(() => L<decimal?,decimal?,decimal?>((m,n) => Sql.Log(n) / Sql.Log(m)       )) },
					{ M(() => Sql.Log  (0.0,0)), N(() => L<double?,double?,double?>   ((m,n)   => Sql.Log(n) / Sql.Log(m)       )) },
					{ M(() => Sql.Log10(0.0)  ), N(() => L<double?,double?>           (n => Sql.Log(n) / Sql.Log(10.0)    )) },

					{ M(() => Sql.Degrees((decimal?)0)), N(() => L<decimal?,decimal?>(v => (decimal?)         (          v!.Value  * (180 / (decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((double?) 0)), N(() => L<double?, double?> (v => (double?)          (          v!.Value  * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((short?)  0)), N(() => L<short?,  short?>  (v => (short?)  AccessInt(AccessInt(v!.Value) * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((int?)    0)), N(() => L<int?,    int?>    (v => (int?)    AccessInt(AccessInt(v!.Value) * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((long?)   0)), N(() => L<long?,   long?>   (v => (long?)   AccessInt(AccessInt(v!.Value) * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((sbyte?)  0)), N(() => L<sbyte?,  sbyte?>  (v => (sbyte?)  AccessInt(AccessInt(v!.Value) * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((float?)  0)), N(() => L<float?,  float?>  (v => (float?)           (          v!.Value  * (180 / Math.PI)))) },

					{ M(() => Sql.Round      (0m)   ), N(() => L<decimal?,decimal?>  (d => d - Sql.Floor(d) == 0.5m && Sql.Floor(d) % 2 == 0? Sql.Ceiling(d) : AccessRound(d, 0))) },
					{ M(() => Sql.Round      (0.0)  ), N(() => L<double?, double?>   (d => d - Sql.Floor(d) == 0.5  && Sql.Floor(d) % 2 == 0? Sql.Ceiling(d) : AccessRound(d, 0))) },
					{ M(() => Sql.Round      (0m, 0)), N(() => L<decimal?,int?,decimal?>((v,p)=> (decimal?)(
						p == 1 ? Sql.Round(v * 10) / 10 :
						p == 2 ? Sql.Round(v * 10) / 10 :
						p == 3 ? Sql.Round(v * 10) / 10 :
						p == 4 ? Sql.Round(v * 10) / 10 :
						p == 5 ? Sql.Round(v * 10) / 10 :
								 Sql.Round(v * 10) / 10))) },
					{ M(() => Sql.Round      (0.0,0)), N(() => L<double?,int?,double?>((v,p) => (double?)(
						p == 1 ? Sql.Round(v * 10) / 10 :
						p == 2 ? Sql.Round(v * 10) / 10 :
						p == 3 ? Sql.Round(v * 10) / 10 :
						p == 4 ? Sql.Round(v * 10) / 10 :
						p == 5 ? Sql.Round(v * 10) / 10 :
								 Sql.Round(v * 10) / 10))) },
					{ M(() => Sql.RoundToEven(0m)   ), N(() => L<decimal?,decimal?>     ( v   => AccessRound(v, 0))) },
					{ M(() => Sql.RoundToEven(0.0)  ), N(() => L<double?, double?>      ( v   => AccessRound(v, 0))) },
					{ M(() => Sql.RoundToEven(0m, 0)), N(() => L<decimal?,int?,decimal?>((v,p)=> AccessRound(v, p))) },
					{ M(() => Sql.RoundToEven(0.0,0)), N(() => L<double?, int?,double?> ((v,p)=> AccessRound(v, p))) },

					{ M(() => Sql.Sinh(0)), N(() => L<double?,double?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)), N(() => L<double?,double?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },

					{ M(() => Sql.Truncate(0m)),  N(() => L<decimal?,decimal?>(v => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
					{ M(() => Sql.Truncate(0.0)), N(() => L<double?,double?>  (v => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
				}},

				#endregion

				#region SapHana

				{ ProviderName.SapHana, new Dictionary<MemberHelper.MemberInfoWithType,IExpressionInfo> {
					{ M(() => Sql.Degrees((decimal?)0)), N(() => L<decimal?,decimal?>(v => (decimal?) (v!.Value * (180 / (decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((double?) 0)), N(() => L<double?, double?> (v => (double?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((short?)  0)), N(() => L<short?,  short?>  (v => (short?)   (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((int?)    0)), N(() => L<int?,    int?>    (v => (int?)     (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((long?)   0)), N(() => L<long?,   long?>   (v => (long?)    (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((sbyte?)  0)), N(() => L<sbyte?,  sbyte?>  (v => (sbyte?)   (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((float?)  0)), N(() => L<float?,  float?>  (v => (float?)   (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.RoundToEven(0.0)  ),   N(() => L<double?,double?>       (v => (double?)Sql.RoundToEven((decimal)v!)))    },
					{ M(() => Sql.RoundToEven(0.0,0)),   N(() => L<double?,int?,double?>((v,p) => (double?)Sql.RoundToEven((decimal)v!, p))) },
					{ M(() => Sql.Stuff("",0,0,"")),     N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) => AltStuff (p0,  p1, p2, p3))) },
				}},

				#endregion
			};

#if BUGCHECK

			foreach (var membersValue in members.Values)
			{
				foreach (var member in membersValue)
				{
					int pCount;

					if (member.Key.MemberInfo is MethodInfo method)
					{
						pCount = method.GetParameters().Length;

						if (!method.IsStatic)
							pCount++;
					}
					else if (member.Key.MemberInfo is ConstructorInfo ctor)
					{
						pCount = ctor.GetParameters().Length;
					}
					else if (member.Key.MemberInfo is PropertyInfo prop)
					{
						pCount = prop.GetGetMethod()?.IsStatic == true ? 0 : 1;
					}
					else if (member.Key.MemberInfo is FieldInfo field)
					{
						pCount = field.IsStatic ? 0 : 1;
					}
					else
						throw new InvalidOperationException($"Unknown member {member.Key}");

					var lambda = member.Value.GetExpression(MappingSchema.Default);

					if (pCount != lambda.Parameters.Count)
						throw new InvalidOperationException(
							$"Invalid number of parameters for '{member.Key}' and '{lambda}'.");
				}
			}

#endif

			return members;
		}
#pragma warning restore CS0618 // Type or member is obsolete

		#endregion

		public static void MapMember(string providerName, Type objectType, MemberInfo memberInfo, LambdaExpression expression)
		{
			if (!Members.TryGetValue(providerName, out var dic))
				Members.Add(providerName, dic = new Dictionary<MemberHelper.MemberInfoWithType, IExpressionInfo>());

			var expr = new LazyExpressionInfo();

			expr.SetExpression(expression);

			dic[new MemberHelper.MemberInfoWithType(objectType, memberInfo)] = expr;

			_checkUserNamespace = false;
		}

		public static void MapMember(string providerName, Type objectType, MemberInfo memberInfo, IExpressionInfo expressionInfo)
		{
			if (!Members.TryGetValue(providerName, out var dic))
				Members.Add(providerName, dic = new Dictionary<MemberHelper.MemberInfoWithType, IExpressionInfo>());

			dic[new MemberHelper.MemberInfoWithType(objectType, memberInfo)] = expressionInfo;

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

		#region Sql specific

		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		// Missing support for trimChars: Access, SqlCe, SybaseASE
		// Firebird/MySQL - chars parameter treated as string, not as set of characters
		[CLSCompliant(false)]
		[Sql.Extension(ProviderName.Firebird      , "TRIM(TRAILING {1} FROM {0})", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(TrailingRTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.ClickHouse    , "trim(TRAILING {1} FROM {0})", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(RTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.SqlServer     , "RTRIM({0})"                 , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(RTrimCharactersBuilderNoTrimCharacters))]
		[Sql.Extension(ProviderName.SqlCe         , "RTRIM({0})"                 , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(RTrimCharactersBuilderNoTrimCharacters))]
		[Sql.Extension(ProviderName.SqlServer2022 , "RTRIM({0}, {1})"            , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(RTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.SqlServer2025 , "RTRIM({0}, {1})"            , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(RTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.DB2           , "RTRIM({0}, {1})"            , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(RTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.Informix      , "RTRIM({0}, {1})"            , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(RTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.Oracle        , "RTRIM({0}, {1})"            , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(RTrimCharactersBuilder), IsNullable = Sql.IsNullableType.Nullable)]
		[Sql.Extension(ProviderName.PostgreSQL    , "RTRIM({0}, {1})"            , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(RTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.SapHana       , "RTRIM({0}, {1})"            , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(RTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.SQLite        , "RTRIM({0}, {1})"            , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(RTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.Access        , "RTRIM({0}, {1})"            , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(RTrimCharactersBuilderNoTrimCharacters))]
		[Sql.Extension(ProviderName.MySql         , "TRIM(TRAILING {1} FROM {0})", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(TrailingRTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.Sybase        , "RTRIM({0})"                 , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(RTrimCharactersBuilderNoTrimCharacters))]
		public static string? TrimRight(string? str, [SqlQueryDependent] params char[] trimChars)
		{
			return str?.TrimEnd(trimChars);
		}

		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		// Missing support for trimChars: Access, SqlCe, SybaseASE
		// Firebird/MySQL - chars parameter treated as string, not as set of characters
		[CLSCompliant(false)]
		[Sql.Expression(ProviderName.Firebird     , "TRIM(LEADING FROM {0})"    , ServerSideOnly = false, PreferServerSide = false)]
		[Sql.Extension(ProviderName.ClickHouse    , "trim(LEADING {1} FROM {0})", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.SqlServer2022 , "LTRIM({0}, {1})"           , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.SqlServer2025 , "LTRIM({0}, {1})"           , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.DB2           , "LTRIM({0}, {1})"           , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.Informix      , "LTRIM({0}, {1})"           , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.Oracle        , "LTRIM({0}, {1})"           , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder), IsNullable = Sql.IsNullableType.Nullable)]
		[Sql.Extension(ProviderName.PostgreSQL    , "LTRIM({0}, {1})"           , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.SapHana       , "LTRIM({0}, {1})"           , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.SQLite        , "LTRIM({0}, {1})"           , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.MySql         , "LTRIM({0}, {1})"           , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.Access        , "LTRIM({0}, {1})"           , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.SqlServer     , "LTRIM({0}, {1})"           , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.SqlCe         , "LTRIM({0}, {1})"           , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder))]
		[Sql.Extension(ProviderName.Sybase        , "LTRIM({0})"                , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(LTrimCharactersBuilder))]
		public static string? TrimLeft(string? str, [SqlQueryDependent] params char[] trimChars)
		{
			return str?.TrimStart(trimChars);
		}

		sealed class LTrimCharactersBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var stringExpression = builder.GetExpression("str")!;
				var chars            = builder.GetValue<char[]>("trimChars");
				if (chars == null || chars.Length == 0)
				{
					builder.ResultExpression = new SqlFunction(
						builder.Mapping.GetDbDataType(typeof(string)),
						(string)"LTRIM",
						stringExpression);
					return;
				}

				builder.ResultExpression = new SqlExpression(
					builder.Mapping.GetDbDataType(typeof(string)),
					builder.Expression,
					Precedence.Primary,
					stringExpression,
					new SqlExpression(builder.Mapping.GetDbDataType(typeof(string)), "{0}", new SqlValue(new string(chars))));
			}
		}

		sealed class TrailingRTrimCharactersBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var stringExpression = builder.GetExpression("str")!;
				var chars            = builder.GetValue<char[]>("trimChars");
				if (chars == null || chars.Length == 0)
				{
					builder.ResultExpression = new SqlExpression(
						builder.Mapping.GetDbDataType(typeof(string)),
						"TRIM(TRAILING FROM {0})",
						stringExpression);
					return;
				}

				ISqlExpression result = stringExpression;

				//TODO: Not accurate, we have to find way
				foreach (var c in chars)
				{
					result = new SqlExpression(
						builder.Mapping.GetDbDataType(typeof(string)),
						builder.Expression,
						Precedence.Primary,
						result,
						new SqlValue(c.ToString()));
				}

				builder.ResultExpression = result;
			}
		}

		sealed class RTrimCharactersBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var stringExpression = builder.GetExpression("str")!;
				var chars            = builder.GetValue<char[]>("trimChars");
				if (chars == null || chars.Length == 0)
				{
					builder.ResultExpression = new SqlFunction(
						builder.Mapping.GetDbDataType(typeof(string)),
						"RTRIM",
						stringExpression);
					return;
				}

				builder.ResultExpression = new SqlExpression(
					builder.Mapping.GetDbDataType(typeof(string)),
					builder.Expression,
					Precedence.Primary,
					stringExpression,
					new SqlExpression(builder.Mapping.GetDbDataType(typeof(string)), "{0}", Precedence.Primary, new SqlValue(new string(chars))));
			}
		}

		sealed class RTrimCharactersBuilderNoTrimCharacters : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var stringExpression = builder.GetExpression("str")!;
				var chars            = builder.GetValue<char[]>("trimChars");
				if (chars == null || chars.Length == 0)
				{
					builder.ResultExpression = new SqlFunction(
						builder.Mapping.GetDbDataType(typeof(string)),
						"RTRIM",
						stringExpression);
				}
				else
				{
					builder.IsConvertible = false;
				}
			}
		}

		#endregion

		#region Provider specific functions

		sealed class ConvertToCaseCompareToBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var str   = builder.GetExpression("str")!;
				var value = builder.GetExpression("value")!;

				builder.ResultExpression = new SqlCompareToExpression(str, value);
			}
		}

		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[Sql.Extension(builderType: typeof(ConvertToCaseCompareToBuilder))]
		public static int? ConvertToCaseCompareTo(string? str, string? value)
		{
			return str == null || value == null ? (int?)null : str.CompareTo(value);
		}

		// Access, DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite
		//
		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[Sql.Function(IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		public static string? AltStuff(string? str, int? startLocation, int? length, string? value)
		{
			return Sql.Stuff(str, startLocation, length, value);
		}

		// DB2
		//
		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[Sql.Function(IsNullable = Sql.IsNullableType.SameAsFirstParameter)]
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mapping")]
		public static string? VarChar(object? obj, int? size)
		{
			return obj == null ? null : string.Format(CultureInfo.InvariantCulture, "{0}", obj);
		}

		// DB2
		//
		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[Sql.Function(IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		public static string? Hex(Guid? guid)
		{
			return guid == null ? null : guid.ToString();
		}

		// DB2, PostgreSQL, Access, MS SQL, SqlCe
		//
		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[CLSCompliant(false)]
		[Sql.Function(                                         IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		[Sql.Function(ProviderName.DB2,        "Repeat",       IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		[Sql.Function(ProviderName.PostgreSQL, "Repeat",       IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		[Sql.Function(ProviderName.Access,     "string", 1, 0, IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		[Sql.Expression(ProviderName.SQLite, "REPLACE(HEX(ZEROBLOB({1})), '00', {0})", IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		public static string? Replicate(string? str, int? count)
		{
			if (str == null || count == null)
				return null;

			using var sb = Pools.StringBuilder.Allocate();

			for (var i = 0; i < count; i++)
				sb.Value.Append(str);

			return sb.Value.ToString();
		}

		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[CLSCompliant(false)]
		[Sql.Function(                                         IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		[Sql.Function(ProviderName.DB2,        "Repeat",       IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		[Sql.Function(ProviderName.PostgreSQL, "Repeat",       IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		[Sql.Function(ProviderName.Access,     "string", 1, 0, IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		[Sql.Expression(ProviderName.SQLite, "REPLACE(HEX(ZEROBLOB({1})), '00', {0})", IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
		public static string? Replicate(char? ch, int? count)
		{
			if (ch == null || count == null)
				return null;

			return new string(ch.Value, count.Value);
		}

		// SqlServer
		//

		// MSSQL
		//
		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[Sql.Function(IsNullable = Sql.IsNullableType.SameAsFirstParameter)]
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mapping")]
		public static decimal? Round(decimal? value, int precision, int mode) => 0;

		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[Sql.Function(IsNullable = Sql.IsNullableType.SameAsFirstParameter)]
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used by mapping")]
		public static double?  Round(double?  value, int precision, int mode) => 0;

		// Access
		//

		// Access
		//
		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[CLSCompliant(false)]
		[Sql.Function("Int", 0)]
		public static T AccessInt<T>(T value)
		{
			return value;
		}

		// Access
		//
		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[CLSCompliant(false)]
		[Sql.Function("Round", 0, 1)]
		public static double? AccessRound(double? value, int? precision)
		{
			if (value is null)
				return null;
			if (precision is null)
				return value;

			return (double?)Math.Round((decimal)value.Value, precision.Value);
		}

		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[CLSCompliant(false)]
		[Sql.Function("Round", 0, 1)]
		public static decimal? AccessRound(decimal? value, int? precision)
		{
			if (value is null)
				return null;
			if (precision is null)
				return value;

			return Math.Round((decimal)value.Value, precision.Value);
		}

		// Firebird
		//
		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[Sql.Function("PI", ServerSideOnly = true, CanBeNull = false)]
		public static decimal DecimalPI() { return (decimal)Math.PI; }
		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[Sql.Function("PI", ServerSideOnly = true, CanBeNull = false)]
		public static double  DoublePI () { return          Math.PI; }

		// Informix
		//
		// TODO: Made private or remove in v7
		[Obsolete("This API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		[Sql.Function(IsNullable = Sql.IsNullableType.IfAnyParameterNullable)]
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
