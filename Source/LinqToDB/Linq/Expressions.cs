using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

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
			if (!Members.TryGetValue(providerName, out var dic))
				Members.Add(providerName, dic = new Dictionary<MemberInfo,IExpressionInfo>());

			var expr = new LazyExpressionInfo();

			expr.SetExpression(expression);

			dic[memberInfo] = expr;

			_checkUserNamespace = false;
		}

		public static void MapMember(string providerName, MemberInfo memberInfo, IExpressionInfo expressionInfo)
		{
			if (!Members.TryGetValue(providerName, out var dic))
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

		public static void MapMember<T>(Expression<Func<T,object?>> memberInfo, LambdaExpression expression)
		{
			MapMember("", M(memberInfo), expression);
		}

		public static void MapMember<T>(string providerName, Expression<Func<T,object?>> memberInfo, LambdaExpression expression)
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

		#region MapBinary

		static BinaryExpression GetBinaryNode(Expression expr)
		{
			while (expr.NodeType == ExpressionType.Convert || expr.NodeType == ExpressionType.ConvertChecked || expr.NodeType == ExpressionType.TypeAs)
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
						var provider = (IGenericInfoProvider)Activator.CreateInstance(gtype)!;

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
			if (_checkUserNamespace)
			{
				if (IsUserNamespace(mi.DeclaringType!.Namespace))
					return null;

				_checkUserNamespace = false;
			}

#if DEBUG1
#if NET45
			var targetFramework = "net45";
#elif NET46
			var targetFramework = "net46";
#elif NETCOREAPP2_1
			var targetFramework = "netcoreapp2.1";
#elif NETSTANDARD2_0
			var targetFramework = "netstandard2.0";
#elif NETCOREAPP3_1
			var targetFramework = "netcoreapp3.1";
#elif NETSTANDARD2_1
			var targetFramework = "netstandard2.1";
#endif
#endif

			IExpressionInfo? expr;

			foreach (var configuration in mappingSchema.ConfigurationList)
				if (Members.TryGetValue(configuration, out var dic))
					if (dic.TryGetValue(mi, out expr))
						return expr.GetExpression(mappingSchema);

			Type[]? args = null;

			if (mi is MethodInfo mm)
			{
				var isTypeGeneric   = mm.DeclaringType!.IsGenericType && !mm.DeclaringType.IsGenericTypeDefinition;
				var isMethodGeneric = mm.IsGenericMethod && !mm.IsGenericMethodDefinition;

				if (isTypeGeneric || isMethodGeneric)
				{
					var typeGenericArgs   = isTypeGeneric   ? mm.DeclaringType.GetGenericArguments() : Array<Type>.Empty;
					var methodGenericArgs = isMethodGeneric ? mm.GetGenericArguments()                 : Array<Type>.Empty;

					args = typeGenericArgs.SequenceEqual(methodGenericArgs) ?
						typeGenericArgs : typeGenericArgs.Concat(methodGenericArgs).ToArray();
				}
			}

			if (args != null && InitGenericConvertProvider(args, mappingSchema))
				foreach (var configuration in mappingSchema.ConfigurationList)
					if (Members.TryGetValue(configuration, out var dic))
						if (dic.TryGetValue(mi, out expr))
							return expr.GetExpression(mappingSchema);

			if (!Members[""].TryGetValue(mi, out expr))
			{
				if (mi is MethodInfo && mi.Name == "CompareString" && mi.DeclaringType!.FullName!.StartsWith("Microsoft.VisualBasic.CompilerServices."))
				{
					lock (_memberSync)
					{
						if (!Members[""].TryGetValue(mi, out expr))
						{
							expr = new LazyExpressionInfo();

							((LazyExpressionInfo)expr).SetExpression(L<string,string,bool,int>((s1,s2,b) => b ? string.CompareOrdinal(s1.ToUpper(), s2.ToUpper()) : string.CompareOrdinal(s1, s2)));

							Members[""].Add(mi, expr);
						}
					}
				}
			}

			if (expr == null && objectType != null)
			{
				var key = new TypeMember(objectType, mi.Name);

				foreach (var configuration in mappingSchema.ConfigurationList)
					if (_typeMembers.TryGetValue(configuration, out var dic))
						if (dic.TryGetValue(key, out expr))
							return expr.GetExpression(mappingSchema);

				_typeMembers[""].TryGetValue(key, out expr);
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

		public static MemberInfo M<T>(Expression<Func<T,object?>> func)
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
#pragma warning disable CS0649 // Field is never assigned to...
			static T1? _member;
#pragma warning restore CS0649 // Field is never assigned to...

			public LambdaExpression GetExpression(MappingSchema mappingSchema)
			{
				var p = Expression.Parameter(typeof(T1?), "p");

				return Expression.Lambda<Func<T1?,T1>>(
					Expression.Coalesce(p, Expression.Constant(mappingSchema.GetDefaultValue(typeof(T1)))),
					p);
			}

			public void SetInfo()
			{
				Members[""][M(() => _member.GetValueOrDefault() )] = this; // N(() => L<T1?,T1>((T1? obj) => obj ?? default(T1)));
			}
		}

		class GenericInfoProvider<T> : IGenericInfoProvider
		{
			public void SetInfo(MappingSchema mappingSchema)
			{
				if (!typeof(T).IsClass && !typeof(T).IsInterface && !typeof(T).IsNullable())
				{
					var gtype    = typeof(GetValueOrDefaultExpressionInfo<>).MakeGenericType(typeof(T));
					var provider = (ISetInfo)Activator.CreateInstance(gtype)!;

					provider.SetInfo();
				}
			}
		}

		#region Mapping

		static Dictionary<string,Dictionary<MemberInfo,IExpressionInfo>>? _members;
		static readonly object                                            _memberSync = new object();

		static readonly Lazy<Dictionary<string,Dictionary<Tuple<ExpressionType,Type,Type>,IExpressionInfo>>> _binaries = 
			new Lazy<Dictionary<string,Dictionary<Tuple<ExpressionType,Type,Type>,IExpressionInfo>>>(() => new Dictionary<string,Dictionary<Tuple<ExpressionType,Type,Type>,IExpressionInfo>>());

		#region Common

		static readonly Dictionary<MemberInfo,IExpressionInfo> _commonMembers = new Dictionary<MemberInfo,IExpressionInfo>
		{
			#region string

			{ M(() => "".Length               ), N(() => L<string,int>                     ((string obj)                              => Sql.Length(obj)!.Value)) },
			{ M(() => "".Substring  (0)       ), N(() => L<string?,int,string?>            ((string? obj,int  p0)                     => Sql.Substring(obj, p0 + 1, obj!.Length - p0))) },
			{ M(() => "".Substring  (0,0)     ), N(() => L<string?,int,int,string?>        ((string? obj,int  p0,int  p1)             => Sql.Substring(obj, p0 + 1, p1))) },
			{ M(() => "".IndexOf    ("")      ), N(() => L<string,string,int>              ((string obj,string p0)                    => p0.Length == 0                    ? 0  : (Sql.CharIndex(p0, obj)!                      .Value) - 1)) },
			{ M(() => "".IndexOf    ("",0)    ), N(() => L<string,string,int,int>          ((string obj,string p0,int  p1)            => p0.Length == 0 && obj.Length > p1 ? p1 : (Sql.CharIndex(p0, obj,               p1 + 1)!.Value) - 1)) },
			{ M(() => "".IndexOf    ("",0,0)  ), N(() => L<string,string,int,int,int>      ((string obj,string p0,int  p1,int p2)     => p0.Length == 0 && obj.Length > p1 ? p1 : (Sql.CharIndex(p0, Sql.Left(obj, p2), p1)!    .Value) - 1)) },
			{ M(() => "".IndexOf    (' ')     ), N(() => L<string,char,int>                ((string obj,char   p0)                    =>                                          (Sql.CharIndex(p0, obj)!                      .Value) - 1)) },
			{ M(() => "".IndexOf    (' ',0)   ), N(() => L<string,char,int,int>            ((string obj,char   p0,int  p1)            =>                                          (Sql.CharIndex(p0, obj,               p1 + 1)!.Value) - 1)) },
			{ M(() => "".IndexOf    (' ',0,0) ), N(() => L<string,char,int,int,int>        ((string obj,char   p0,int  p1,int p2)     =>                                          (Sql.CharIndex(p0, Sql.Left(obj, p2), p1)     ?? 0) - 1)) },
			{ M(() => "".LastIndexOf("")      ), N(() => L<string,string,int>              ((string obj,string p0)                    => p0.Length == 0 ? obj.Length - 1 : (Sql.CharIndex(p0, obj)!                           .Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(Sql.Reverse(p0), Sql.Reverse(obj))!                              .Value) - p0.Length + 1)) },
			{ M(() => "".LastIndexOf("",0)    ), N(() => L<string,string,int,int>          ((string obj,string p0,int  p1)            => p0.Length == 0 ? p1             : (Sql.CharIndex(p0, obj,                    p1 + 1)!.Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(Sql.Reverse(p0), Sql.Reverse(obj.Substring(p1, obj.Length - p1)))!.Value) - p0.Length + 1)) },
			{ M(() => "".LastIndexOf("",0,0)  ), N(() => L<string,string,int,int,int>      ((string obj,string p0,int  p1,int p2)     => p0.Length == 0 ? p1             : (Sql.CharIndex(p0, Sql.Left(obj, p1 + p2), p1 + 1)!.Value) == 0 ? -1 :    p1 + p2 - (Sql.CharIndex(Sql.Reverse(p0), Sql.Reverse(obj.Substring(p1, p2)))!             .Value) - p0.Length + 1)) },
			{ M(() => "".LastIndexOf(' ')     ), N(() => L<string,char,int>                ((string obj,char   p0)                    => (Sql.CharIndex(p0, obj)!                           .Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(p0, Sql.Reverse(obj))!                               .Value))) },
			{ M(() => "".LastIndexOf(' ',0)   ), N(() => L<string,char,int,int>            ((string obj,char   p0,int  p1)            => (Sql.CharIndex(p0, obj, p1 + 1)!                   .Value) == 0 ? -1 : obj.Length - (Sql.CharIndex(p0, Sql.Reverse(obj.Substring(p1, obj.Length - p1)))!.Value))) },
			{ M(() => "".LastIndexOf(' ',0,0) ), N(() => L<string,char,int,int,int>        ((string obj,char   p0,int  p1,int p2)     => (Sql.CharIndex(p0, Sql.Left(obj, p1 + p2), p1 + 1)!.Value) == 0 ? -1 : p1 + p2    - (Sql.CharIndex(p0, Sql.Reverse(obj.Substring(p1, p2)))!             .Value))) },
			{ M(() => "".Insert     (0,"")    ), N(() => L<string?,int,string?,string?>    ((string? obj,int  p0,string? p1)          => obj!.Length == p0 ? obj + p1 : Sql.Stuff(obj, p0 + 1, 0, p1))) },
			{ M(() => "".Remove     (0)       ), N(() => L<string?,int,string?>            ((string? obj,int  p0)                     => Sql.Left     (obj, p0))) },
			{ M(() => "".Remove     (0,0)     ), N(() => L<string?,int,int,string?>        ((string? obj,int  p0,int  p1)             => Sql.Stuff    (obj, p0 + 1, p1, ""))) },
			{ M(() => "".PadLeft    (0)       ), N(() => L<string?,int,string?>            ((string? obj,int  p0)                     => Sql.PadLeft  (obj, p0, ' '))) },
			{ M(() => "".PadLeft    (0,' ')   ), N(() => L<string?,int,char,string?>       ((string? obj,int  p0,char   p1)           => Sql.PadLeft  (obj, p0, p1))) },
			{ M(() => "".PadRight   (0)       ), N(() => L<string?,int,string?>            ((string? obj,int  p0)                     => Sql.PadRight (obj, p0, ' '))) },
			{ M(() => "".PadRight   (0,' ')   ), N(() => L<string?,int,char,string?>       ((string? obj,int  p0,char   p1)           => Sql.PadRight (obj, p0, p1))) },
			{ M(() => "".Replace    ("","")   ), N(() => L<string?,string?,string?,string?>((string? obj,string? p0,string? p1)       => Sql.Replace  (obj, p0, p1))) },
			{ M(() => "".Replace    (' ',' ') ), N(() => L<string?,char,char,string?>      ((string? obj,char   p0,char   p1)         => Sql.Replace  (obj, p0, p1))) },
			{ M(() => "".Trim       ()        ), N(() => L<string?,string?>                ((string? obj)                             => Sql.Trim     (obj))) },

#if NETCOREAPP
			{ M(() => "".TrimEnd    ()        ), N(() => L<string,string?>                 ((string obj)                              =>     TrimRight(obj))) },
			{ M(() => "".TrimStart  ()        ), N(() => L<string,string?>                 ((string obj)                              =>     TrimLeft (obj))) },
#else
			{ M(() => "".TrimEnd    ()        ), N(() => L<string,char[],string?>          ((string obj,char[] ch)                    =>     TrimRight(obj, ch))) },
			{ M(() => "".TrimStart  ()        ), N(() => L<string,char[],string?>          ((string obj,char[] ch)                    =>     TrimLeft (obj, ch))) },
#endif
			{ M(() => "".ToLower    ()        ), N(() => L<string?,string?>                ((string? obj)                             => Sql.Lower(obj))) },
			{ M(() => "".ToUpper    ()        ), N(() => L<string?,string?>                ((string? obj)                             => Sql.Upper(obj))) },
			{ M(() => "".CompareTo  ("")      ), N(() => L<string,string,int>              ((string obj,string p0)                    => ConvertToCaseCompareTo(obj, p0)!.Value)) },
			{ M(() => "".CompareTo  (1)       ), N(() => L<string,object,int>              ((string obj,object p0)                    => ConvertToCaseCompareTo(obj, p0.ToString())!.Value)) },

			{ M(() => string.Concat((object)null!)                             ), N(() => L<object,string?>                    ((object p0)                               => p0.ToString()))           },
			{ M(() => string.Concat((object)null!,(object)null!)               ), N(() => L<object,object,string>              ((object p0,object p1)                     => p0.ToString() + p1))      },
			{ M(() => string.Concat((object)null!,(object)null!,(object)null!) ), N(() => L<object,object,object,string>       ((object p0,object p1,object p2)           => p0.ToString() + p1 + p2)) },
			{ M(() => string.Concat((object[])null!)                           ), N(() => L<object[],string>                   ((object[] ps)                             => Sql.Concat(ps)))          },
			{ M(() => string.Concat("","")                                     ), N(() => L<string,string,string>              ((string p0,string p1)                     => p0 + p1))                 },
			{ M(() => string.Concat("","","")                                  ), N(() => L<string,string,string,string>       ((string p0,string p1,string p2)           => p0 + p1 + p2))            },
			{ M(() => string.Concat("","","","")                               ), N(() => L<string,string,string,string,string>((string p0,string p1,string p2,string p3) => p0 + p1 + p2 + p3))       },
			{ M(() => string.Concat((string[])null!)                           ), N(() => L<string[],string>                   ((string[] ps)                             => Sql.Concat(ps)))          },

			{ M(() => string.IsNullOrEmpty ("")    ),                                         N(() => L<string,bool>                                   ((string p0)                                                   => p0 == null || p0.Length == 0)) },
			{ M(() => string.IsNullOrWhiteSpace("")),                                         N(() => L<string,bool>                                   ((string p0)                                                   => Sql.IsNullOrWhiteSpace(p0))) },
			{ M(() => string.CompareOrdinal("","")),                                          N(() => L<string,string,int>                             ((string s1,string s2)                                         => s1.CompareTo(s2))) },
			{ M(() => string.CompareOrdinal("",0,"",0,0)),                                    N(() => L<string,int,string,int,int,int>                 ((string s1,int i1,string s2,int i2,int l)                     => s1.Substring(i1, l).CompareTo(s2.Substring(i2, l)))) },
			{ M(() => string.Compare       ("","")),                                          N(() => L<string,string,int>                             ((string s1,string s2)                                         => s1.CompareTo(s2))) },
			{ M(() => string.Compare       ("",0,"",0,0)),                                    N(() => L<string,int,string,int,int,int>                 ((string s1,int i1,string s2,int i2,int l)                     => s1.Substring(i1,l).CompareTo(s2.Substring(i2,l)))) },
			{ M(() => string.Compare       ("","",true)),                                     N(() => L<string,string,bool,int>                        ((string s1,string s2,bool b)                                  => b ? s1.ToLower().CompareTo(s2.ToLower()) : s1.CompareTo(s2))) },
			{ M(() => string.Compare       ("",0,"",0,0,true)),                               N(() => L<string,int,string,int,int,bool,int>            ((string s1,int i1,string s2,int i2,int l,bool b)              => b ? s1.Substring(i1,l).ToLower().CompareTo(s2.Substring(i2, l).ToLower()) : s1.Substring(i1, l).CompareTo(s2.Substring(i2, l)))) },
			{ M(() => string.Compare       ("",0,"",0,0,StringComparison.OrdinalIgnoreCase)), N(() => L<string,int,string,int,int,StringComparison,int>((string s1,int i1,string s2,int i2,int l,StringComparison sc) => sc == StringComparison.CurrentCultureIgnoreCase || sc==StringComparison.OrdinalIgnoreCase ? s1.Substring(i1,l).ToLower().CompareTo(s2.Substring(i2, l).ToLower()) : s1.Substring(i1, l).CompareTo(s2.Substring(i2, l)))) },
			{ M(() => string.Compare       ("","",StringComparison.OrdinalIgnoreCase)),       N(() => L<string,string,StringComparison,int>            ((string s1,string s2,StringComparison sc)                     => sc == StringComparison.CurrentCultureIgnoreCase || sc==StringComparison.OrdinalIgnoreCase ? s1.ToLower().CompareTo(s2.ToLower()) : s1.CompareTo(s2))) },

			{ M(() => AltStuff("",0,0,"")), N(() => L<string,int?,int?,string,string>((string p0,int? p1,int ?p2,string p3) => Sql.Left(p0, p1 - 1) + p3 + Sql.Right(p0, p0.Length - (p1 + p2 - 1)))) },

			#endregion

			#region Binary

			{ M(() => ((Binary)null!).Length ), N(() => L<Binary,int>((Binary obj) => Sql.Length(obj)!.Value)) },

			#endregion

			#region Byte[]

			{ M(() => ((byte[])null!).Length ), N(() => L<byte[],int>((byte[] obj) => Sql.Length(obj)!.Value)) },

			#endregion

			#region DateTime

			{ M(() => Sql.GetDate()                  ), N(() => L<DateTime>                (()                        => Sql.CurrentTimestamp2)) },
			{ M(() => DateTime.Now                   ), N(() => L<DateTime>                (()                        => Sql.CurrentTimestamp2)) },

			{ M(() => DateTime.Now.Year              ), N(() => L<DateTime,int>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Year,        obj)!.Value    )) },
			{ M(() => DateTime.Now.Month             ), N(() => L<DateTime,int>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Month,       obj)!.Value    )) },
			{ M(() => DateTime.Now.DayOfYear         ), N(() => L<DateTime,int>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.DayOfYear,   obj)!.Value    )) },
			{ M(() => DateTime.Now.Day               ), N(() => L<DateTime,int>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Day,         obj)!.Value    )) },
			{ M(() => DateTime.Now.DayOfWeek         ), N(() => L<DateTime,int>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.WeekDay,     obj)!.Value - 1)) },
			{ M(() => DateTime.Now.Hour              ), N(() => L<DateTime,int>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Hour,        obj)!.Value    )) },
			{ M(() => DateTime.Now.Minute            ), N(() => L<DateTime,int>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Minute,      obj)!.Value    )) },
			{ M(() => DateTime.Now.Second            ), N(() => L<DateTime,int>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Second,      obj)!.Value    )) },
			{ M(() => DateTime.Now.Millisecond       ), N(() => L<DateTime,int>            ((DateTime obj)            => Sql.DatePart(Sql.DateParts.Millisecond, obj)!.Value    )) },
			{ M(() => DateTime.Now.Date              ), N(() => L<DateTime,DateTime>       ((DateTime obj)            => Sql.Convert2(Sql.Date,                  obj)           )) },
			{ M(() => DateTime.Now.TimeOfDay         ), N(() => L<DateTime,TimeSpan>       ((DateTime obj)            => Sql.DateToTime(Sql.Convert2(Sql.Time,   obj))!.Value   )) },
			{ M(() => DateTime.Now.AddYears       (0)), N(() => L<DateTime,int,DateTime>   ((DateTime obj,int p0)     => Sql.DateAdd(Sql.DateParts.Year,        p0, obj)!.Value )) },
			{ M(() => DateTime.Now.AddMonths      (0)), N(() => L<DateTime,int,DateTime>   ((DateTime obj,int p0)     => Sql.DateAdd(Sql.DateParts.Month,       p0, obj)!.Value )) },
			{ M(() => DateTime.Now.AddDays        (0)), N(() => L<DateTime,double,DateTime>((DateTime obj,double p0)  => Sql.DateAdd(Sql.DateParts.Day,         p0, obj)!.Value )) },
			{ M(() => DateTime.Now.AddHours       (0)), N(() => L<DateTime,double,DateTime>((DateTime obj,double p0)  => Sql.DateAdd(Sql.DateParts.Hour,        p0, obj)!.Value )) },
			{ M(() => DateTime.Now.AddMinutes     (0)), N(() => L<DateTime,double,DateTime>((DateTime obj,double p0)  => Sql.DateAdd(Sql.DateParts.Minute,      p0, obj)!.Value )) },
			{ M(() => DateTime.Now.AddSeconds     (0)), N(() => L<DateTime,double,DateTime>((DateTime obj,double p0)  => Sql.DateAdd(Sql.DateParts.Second,      p0, obj)!.Value )) },
			{ M(() => DateTime.Now.AddMilliseconds(0)), N(() => L<DateTime,double,DateTime>((DateTime obj,double p0)  => Sql.DateAdd(Sql.DateParts.Millisecond, p0, obj)!.Value )) },
			{ M(() => new DateTime(0, 0, 0)          ), N(() => L<int,int,int,DateTime>((int y,int m,int d) => Sql.MakeDateTime(y, m, d)!.Value                       )) },

			{ M(() => Sql.MakeDateTime(0, 0, 0)          ), N(() => L<int?,int?,int?,DateTime?>               ((int? y,int? m,int? d)                       => (DateTime?)Sql.Convert(Sql.Date, y.ToString() + "-" + m.ToString() + "-" + d.ToString()))) },
			{ M(() => new DateTime    (0, 0, 0, 0, 0, 0) ), N(() => L<int,int,int,int,int,int,DateTime>       ((int  y,int  m,int  d,int  h,int  mm,int s)  => Sql.MakeDateTime(y, m, d, h, mm, s)!.Value )) },
			{ M(() => Sql.MakeDateTime(0, 0, 0, 0, 0, 0) ), N(() => L<int?,int?,int?,int?,int?,int?,DateTime?>((int? y,int? m,int? d,int? h,int? mm,int? s) => (DateTime?)Sql.Convert(Sql.DateTime2,
				y.ToString() + "-" + m.ToString() + "-" + d.ToString() + " " +
				h.ToString() + ":" + mm.ToString() + ":" + s.ToString()))) },

			#endregion

			#region DateTimeOffset

			// Disabled for now. See #2512 (https://github.com/linq2db/linq2db/issues/2512)
			// { M(() => DateTimeOffset.Now                   ), N(() => L<DateTimeOffset>                      (()                              => Sql.CurrentTzTimestamp                                 )) },
			
			{ M(() => DateTimeOffset.Now.Year              ), N(() => L<DateTimeOffset,int>                  ((DateTimeOffset obj)            => Sql.DatePart(Sql.DateParts.Year,        obj)!.Value    )) },
			{ M(() => DateTimeOffset.Now.Month             ), N(() => L<DateTimeOffset,int>                  ((DateTimeOffset obj)            => Sql.DatePart(Sql.DateParts.Month,       obj)!.Value    )) },
			{ M(() => DateTimeOffset.Now.DayOfYear         ), N(() => L<DateTimeOffset,int>                  ((DateTimeOffset obj)            => Sql.DatePart(Sql.DateParts.DayOfYear,   obj)!.Value    )) },
			{ M(() => DateTimeOffset.Now.Day               ), N(() => L<DateTimeOffset,int>                  ((DateTimeOffset obj)            => Sql.DatePart(Sql.DateParts.Day,         obj)!.Value    )) },
			{ M(() => DateTimeOffset.Now.DayOfWeek         ), N(() => L<DateTimeOffset,int>                  ((DateTimeOffset obj)            => Sql.DatePart(Sql.DateParts.WeekDay,     obj)!.Value - 1)) },
			{ M(() => DateTimeOffset.Now.Hour              ), N(() => L<DateTimeOffset,int>                  ((DateTimeOffset obj)            => Sql.DatePart(Sql.DateParts.Hour,        obj)!.Value    )) },
			{ M(() => DateTimeOffset.Now.Minute            ), N(() => L<DateTimeOffset,int>                  ((DateTimeOffset obj)            => Sql.DatePart(Sql.DateParts.Minute,      obj)!.Value    )) },
			{ M(() => DateTimeOffset.Now.Second            ), N(() => L<DateTimeOffset,int>                  ((DateTimeOffset obj)            => Sql.DatePart(Sql.DateParts.Second,      obj)!.Value    )) },
			{ M(() => DateTimeOffset.Now.Millisecond       ), N(() => L<DateTimeOffset,int>                  ((DateTimeOffset obj)            => Sql.DatePart(Sql.DateParts.Millisecond, obj)!.Value    )) },
			{ M(() => DateTimeOffset.Now.Date              ), N(() => L<DateTimeOffset,DateTime>             ((DateTimeOffset obj)            => Sql.Convert2(Sql.Date,                  obj)           )) },
			{ M(() => DateTimeOffset.Now.TimeOfDay         ), N(() => L<DateTimeOffset,TimeSpan>             ((DateTimeOffset obj)            => Sql.DateToTime(Sql.Convert2(Sql.Time,   obj))!.Value   )) },
			{ M(() => DateTimeOffset.Now.AddYears       (0)), N(() => L<DateTimeOffset,int,DateTimeOffset>   ((DateTimeOffset obj,int p0)     => Sql.DateAdd(Sql.DateParts.Year,        p0, obj)!.Value )) },
			{ M(() => DateTimeOffset.Now.AddMonths      (0)), N(() => L<DateTimeOffset,int,DateTimeOffset>   ((DateTimeOffset obj,int p0)     => Sql.DateAdd(Sql.DateParts.Month,       p0, obj)!.Value )) },
			{ M(() => DateTimeOffset.Now.AddDays        (0)), N(() => L<DateTimeOffset,double,DateTimeOffset>((DateTimeOffset obj,double p0)  => Sql.DateAdd(Sql.DateParts.Day,         p0, obj)!.Value )) },
			{ M(() => DateTimeOffset.Now.AddHours       (0)), N(() => L<DateTimeOffset,double,DateTimeOffset>((DateTimeOffset obj,double p0)  => Sql.DateAdd(Sql.DateParts.Hour,        p0, obj)!.Value )) },
			{ M(() => DateTimeOffset.Now.AddMinutes     (0)), N(() => L<DateTimeOffset,double,DateTimeOffset>((DateTimeOffset obj,double p0)  => Sql.DateAdd(Sql.DateParts.Minute,      p0, obj)!.Value )) },
			{ M(() => DateTimeOffset.Now.AddSeconds     (0)), N(() => L<DateTimeOffset,double,DateTimeOffset>((DateTimeOffset obj,double p0)  => Sql.DateAdd(Sql.DateParts.Second,      p0, obj)!.Value )) },
			{ M(() => DateTimeOffset.Now.AddMilliseconds(0)), N(() => L<DateTimeOffset,double,DateTimeOffset>((DateTimeOffset obj,double p0)  => Sql.DateAdd(Sql.DateParts.Millisecond, p0, obj)!.Value )) },

			#endregion

			#region Parse

			{ M(() => bool.    Parse("")), N(() => L<string,bool>    ((string p0) => Sql.ConvertTo<bool>.    From(p0))) },
			{ M(() => byte.    Parse("")), N(() => L<string,byte>    ((string p0) => Sql.ConvertTo<byte>.    From(p0))) },
			{ M(() => char.    Parse("")), N(() => L<string,char>    ((string p0) => Sql.ConvertTo<char>.    From(p0))) },
			{ M(() => DateTime.Parse("")), N(() => L<string,DateTime>((string p0) => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => decimal. Parse("")), N(() => L<string,decimal> ((string p0) => Sql.ConvertTo<decimal>. From(p0))) },
			{ M(() => double.  Parse("")), N(() => L<string,double>  ((string p0) => Sql.ConvertTo<double>.  From(p0))) },
			{ M(() => short.   Parse("")), N(() => L<string,short>   ((string p0) => Sql.ConvertTo<short>.   From(p0))) },
			{ M(() => int.     Parse("")), N(() => L<string,int>     ((string p0) => Sql.ConvertTo<int>.     From(p0))) },
			{ M(() => long.    Parse("")), N(() => L<string,long>    ((string p0) => Sql.ConvertTo<long>.    From(p0))) },
			{ M(() => sbyte.   Parse("")), N(() => L<string,sbyte>   ((string p0) => Sql.ConvertTo<sbyte>.   From(p0))) },
			{ M(() => float.   Parse("")), N(() => L<string,float>   ((string p0) => Sql.ConvertTo<float>.   From(p0))) },
			{ M(() => ushort.  Parse("")), N(() => L<string,ushort>  ((string p0) => Sql.ConvertTo<ushort>.  From(p0))) },
			{ M(() => uint.    Parse("")), N(() => L<string,uint>    ((string p0) => Sql.ConvertTo<uint>.    From(p0))) },
			{ M(() => ulong.   Parse("")), N(() => L<string,ulong>   ((string p0) => Sql.ConvertTo<ulong>.   From(p0))) },

			#endregion

			#region ToString

//			{ M(() => ((bool)   true).ToString()), N(() => L<bool,    string>((bool    p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((byte)    0)  .ToString()), N(() => L<byte,    string>((byte    p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((char)   '0') .ToString()), N(() => L<char,    string>((char    p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((decimal) 0)  .ToString()), N(() => L<decimal, string>((decimal p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((double)  0)  .ToString()), N(() => L<double,  string>((double  p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((short)   0)  .ToString()), N(() => L<short,   string>((short   p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((int)   0)    .ToString()), N(() => L<int,     string>((int     p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((long)   0)   .ToString()), N(() => L<long,    string>((long    p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((sbyte)   0)  .ToString()), N(() => L<sbyte,   string>((sbyte   p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((float)   0)  .ToString()), N(() => L<float,   string>((float   p0) => Sql.ConvertTo<string>.From(p0))) },
			{ M(() => ((string) "0") .ToString()), N(() => L<string,  string>((string  p0) => p0                            )) },
//			{ M(() => ((ushort)  0)  .ToString()), N(() => L<ushort,  string>((ushort  p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((uint)  0)    .ToString()), N(() => L<uint,    string>((uint    p0) => Sql.ConvertTo<string>.From(p0))) },
//			{ M(() => ((ulong)  0)  .ToString()), N(() => L<ulong,    string>((ulong   p0) => Sql.ConvertTo<string>.From(p0))) },

			#endregion

			#region Convert

			#region ToBoolean

			{ M(() => Convert.ToBoolean((bool)true  )), N(() => L<bool,    bool>((bool     p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((byte)0     )), N(() => L<byte,    bool>((byte     p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((char)   '0')), N(() => L<char,    bool>((char     p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean(DateTime.Now)), N(() => L<DateTime,bool>((DateTime p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((decimal) 0 )), N(() => L<decimal, bool>((decimal  p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((double)  0 )), N(() => L<double,  bool>((double   p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((short)   0 )), N(() => L<short,   bool>((short    p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((int)     0 )), N(() => L<int,     bool>((int      p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((long)    0 )), N(() => L<long,    bool>((long     p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((object)  0 )), N(() => L<object,  bool>((object   p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((sbyte)   0 )), N(() => L<sbyte,   bool>((sbyte    p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((float)   0 )), N(() => L<float,   bool>((float    p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((string) "0")), N(() => L<string,  bool>((string   p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((ushort)  0 )), N(() => L<ushort,  bool>((ushort   p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((uint)    0 )), N(() => L<uint,    bool>((uint     p0) => Sql.ConvertTo<bool>.From(p0))) },
			{ M(() => Convert.ToBoolean((ulong)   0 )), N(() => L<ulong,   bool>((ulong    p0) => Sql.ConvertTo<bool>.From(p0))) },

			#endregion

			#region ToByte

			{ M(() => Convert.ToByte((bool)   true)), N(() => L<bool,    byte>((bool     p0) => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((byte)    0)  ), N(() => L<byte,    byte>((byte     p0) => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((char)   '0') ), N(() => L<char,    byte>((char     p0) => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte(DateTime.Now) ), N(() => L<DateTime,byte>((DateTime p0) => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((decimal) 0)  ), N(() => L<decimal, byte>((decimal  p0) => Sql.ConvertTo<byte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToByte((double)  0)  ), N(() => L<double,  byte>((double   p0) => Sql.ConvertTo<byte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToByte((short)   0)  ), N(() => L<short,   byte>((short    p0) => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((int)     0)  ), N(() => L<int,     byte>((int      p0) => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((long)    0)  ), N(() => L<long,    byte>((long     p0) => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((object)  0)  ), N(() => L<object,  byte>((object   p0) => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((sbyte)   0)  ), N(() => L<sbyte,   byte>((sbyte    p0) => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((float)   0)  ), N(() => L<float,   byte>((float    p0) => Sql.ConvertTo<byte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToByte((string) "0") ), N(() => L<string,  byte>((string   p0) => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((ushort)  0)  ), N(() => L<ushort,  byte>((ushort   p0) => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((uint)    0)  ), N(() => L<uint,    byte>((uint     p0) => Sql.ConvertTo<byte>.From(p0))) },
			{ M(() => Convert.ToByte((ulong)   0)  ), N(() => L<ulong,   byte>((ulong    p0) => Sql.ConvertTo<byte>.From(p0))) },

#endregion

			#region ToChar

			{ M(() => Convert.ToChar((bool)   true)), N(() => L<bool,    char>((bool     p0) => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((byte)    0)  ), N(() => L<byte,    char>((byte     p0) => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((char)   '0') ), N(() => L<char,    char>((char     p0) => p0                          )) },
			{ M(() => Convert.ToChar(DateTime.Now) ), N(() => L<DateTime,char>((DateTime p0) => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((decimal) 0)  ), N(() => L<decimal, char>((decimal  p0) => Sql.ConvertTo<char>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToChar((double)  0)  ), N(() => L<double,  char>((double   p0) => Sql.ConvertTo<char>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToChar((short)   0)  ), N(() => L<short,   char>((short    p0) => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((int)     0)  ), N(() => L<int,     char>((int      p0) => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((long)    0)  ), N(() => L<long,    char>((long     p0) => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((object)  0)  ), N(() => L<object,  char>((object   p0) => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((sbyte)   0)  ), N(() => L<sbyte,   char>((sbyte    p0) => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((float)   0)  ), N(() => L<float,   char>((float    p0) => Sql.ConvertTo<char>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToChar((string) "0") ), N(() => L<string,  char>((string   p0) => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((ushort)  0)  ), N(() => L<ushort,  char>((ushort   p0) => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((uint)    0)  ), N(() => L<uint,    char>((uint     p0) => Sql.ConvertTo<char>.From(p0))) },
			{ M(() => Convert.ToChar((ulong)   0)  ), N(() => L<ulong,   char>((ulong    p0) => Sql.ConvertTo<char>.From(p0))) },

			#endregion

			#region ToDateTime

			{ M(() => Convert.ToDateTime((object)  0)  ), N(() => L<object,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((string) "0") ), N(() => L<string,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((bool)   true)), N(() => L<bool,    DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((byte)    0)  ), N(() => L<byte,    DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((char)   '0') ), N(() => L<char,    DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime(DateTime.Now) ), N(() => L<DateTime,DateTime>(p0 => p0                              )) },
			{ M(() => Convert.ToDateTime((decimal) 0)  ), N(() => L<decimal, DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((double)  0)  ), N(() => L<double,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((short)   0)  ), N(() => L<short,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((int)     0)  ), N(() => L<int,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((long)    0)  ), N(() => L<long,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((sbyte)   0)  ), N(() => L<sbyte,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((float)   0)  ), N(() => L<float,   DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((ushort)  0)  ), N(() => L<ushort,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((uint)    0)  ), N(() => L<uint,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },
			{ M(() => Convert.ToDateTime((ulong)   0)  ), N(() => L<ulong,  DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0))) },

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
			{ M(() => Convert.ToDecimal((object)  0)  ), N(() => L<object,  decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((sbyte)   0)  ), N(() => L<sbyte,   decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((float)   0)  ), N(() => L<float,   decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
			{ M(() => Convert.ToDecimal((string) "0") ), N(() => L<string,  decimal>(p0 => Sql.ConvertTo<decimal>.From(p0))) },
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
			{ M(() => Convert.ToDouble((object)  0)  ), N(() => L<object,  double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble((sbyte)   0)  ), N(() => L<sbyte,   double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble((float)   0)  ), N(() => L<float,   double>(p0 => Sql.ConvertTo<double>.From(p0))) },
			{ M(() => Convert.ToDouble((string) "0") ), N(() => L<string,  double>(p0 => Sql.ConvertTo<double>.From(p0))) },
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
			{ M(() => Convert.ToInt64((object)  0)  ), N(() => L<object,  long>(p0 => Sql.ConvertTo<long>.From(p0))) },
			{ M(() => Convert.ToInt64((sbyte)   0)  ), N(() => L<sbyte,   long>(p0 => Sql.ConvertTo<long>.From(p0))) },
			{ M(() => Convert.ToInt64((float)   0)  ), N(() => L<float,   long>(p0 => Sql.ConvertTo<long>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt64((string) "0") ), N(() => L<string,  long>(p0 => Sql.ConvertTo<long>.From(p0))) },
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
			{ M(() => Convert.ToInt32((object)  0)  ), N(() => L<object,  int>(p0 => Sql.ConvertTo<int>.From(p0))) },
			{ M(() => Convert.ToInt32((sbyte)   0)  ), N(() => L<sbyte,   int>(p0 => Sql.ConvertTo<int>.From(p0))) },
			{ M(() => Convert.ToInt32((float)   0)  ), N(() => L<float,   int>(p0 => Sql.ConvertTo<int>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt32((string) "0") ), N(() => L<string,  int>(p0 => Sql.ConvertTo<int>.From(p0))) },
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
			{ M(() => Convert.ToInt16((object)  0)  ), N(() => L<object,  short>(p0 => Sql.ConvertTo<short>.From(p0))) },
			{ M(() => Convert.ToInt16((sbyte)   0)  ), N(() => L<sbyte,   short>(p0 => Sql.ConvertTo<short>.From(p0))) },
			{ M(() => Convert.ToInt16((float)   0)  ), N(() => L<float,   short>(p0 => Sql.ConvertTo<short>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToInt16((string) "0") ), N(() => L<string,  short>(p0 => Sql.ConvertTo<short>.From(p0))) },
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
			{ M(() => Convert.ToSByte((object)  0)  ), N(() => L<object,  sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
			{ M(() => Convert.ToSByte((sbyte)   0)  ), N(() => L<sbyte,   sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
			{ M(() => Convert.ToSByte((float)   0)  ), N(() => L<float,   sbyte>(p0 => Sql.ConvertTo<sbyte>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToSByte((string) "0") ), N(() => L<string,  sbyte>(p0 => Sql.ConvertTo<sbyte>.From(p0))) },
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
			{ M(() => Convert.ToSingle((object)  0)  ), N(() => L<object,  float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((sbyte)   0)  ), N(() => L<sbyte,   float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((float)   0)  ), N(() => L<float,   float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((string) "0") ), N(() => L<string,  float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((ushort)  0)  ), N(() => L<ushort,  float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((uint)    0)  ), N(() => L<uint,    float>(p0 => Sql.ConvertTo<float>.From(p0))) },
			{ M(() => Convert.ToSingle((ulong)   0)  ), N(() => L<ulong,   float>(p0 => Sql.ConvertTo<float>.From(p0))) },

			#endregion

			#region ToString

			{ M(() => Convert.ToString((bool)   true)), N(() => L<bool,    string>(p0 => Sql.ConvertTo<string>.From(p0))) },
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
			{ M(() => Convert.ToUInt16((object)  0)  ), N(() => L<object,  ushort>(p0 => Sql.ConvertTo<ushort>.From(p0))) },
			{ M(() => Convert.ToUInt16((sbyte)   0)  ), N(() => L<sbyte,   ushort>(p0 => Sql.ConvertTo<ushort>.From(p0))) },
			{ M(() => Convert.ToUInt16((float)   0)  ), N(() => L<float,   ushort>(p0 => Sql.ConvertTo<ushort>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt16((string) "0") ), N(() => L<string,  ushort>(p0 => Sql.ConvertTo<ushort>.From(p0)) ) },
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
			{ M(() => Convert.ToUInt32((object)  0)  ), N(() => L<object,  uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
			{ M(() => Convert.ToUInt32((sbyte)   0)  ), N(() => L<sbyte,   uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
			{ M(() => Convert.ToUInt32((float)   0)  ), N(() => L<float,   uint>(p0 => Sql.ConvertTo<uint>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt32((string) "0") ), N(() => L<string,  uint>(p0 => Sql.ConvertTo<uint>.From(p0))) },
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
			{ M(() => Convert.ToUInt64((object)  0)  ), N(() => L<object,  ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64((sbyte)   0)  ), N(() => L<sbyte,   ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64((float)   0)  ), N(() => L<float,   ulong>(p0 => Sql.ConvertTo<ulong>.From(Sql.RoundToEven(p0)))) },
			{ M(() => Convert.ToUInt64((string) "0") ), N(() => L<string,  ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64((ushort)  0)  ), N(() => L<ushort,  ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64((uint)    0)  ), N(() => L<uint,    ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },
			{ M(() => Convert.ToUInt64((ulong)   0)  ), N(() => L<ulong,   ulong>(p0 => Sql.ConvertTo<ulong>.From(p0))) },

			#endregion

			#endregion

			#region Math

			{ M(() => Math.Abs    ((decimal)0)), N(() => L<decimal,decimal>((decimal p) => Sql.Abs(p)!.Value )) },
			{ M(() => Math.Abs    ((double) 0)), N(() => L<double, double> ((double  p) => Sql.Abs(p)!.Value )) },
			{ M(() => Math.Abs    ((short)  0)), N(() => L<short,  short>  ((short   p) => Sql.Abs(p)!.Value )) },
			{ M(() => Math.Abs    ((int)    0)), N(() => L<int,    int>    ((int     p) => Sql.Abs(p)!.Value )) },
			{ M(() => Math.Abs    ((long)   0)), N(() => L<long,    long>  ((long    p) => Sql.Abs(p)!.Value )) },
			{ M(() => Math.Abs    ((sbyte)  0)), N(() => L<sbyte,  sbyte>  ((sbyte   p) => Sql.Abs(p)!.Value )) },
			{ M(() => Math.Abs    ((float)  0)), N(() => L<float,  float>  ((float   p) => Sql.Abs(p)!.Value )) },

			{ M(() => Math.Acos   (0)   ),       N(() => L<double,double>       ((double p)          => Sql.Acos   (p)!   .Value )) },
			{ M(() => Math.Asin   (0)   ),       N(() => L<double,double>       ((double p)          => Sql.Asin   (p)!   .Value )) },
			{ M(() => Math.Atan   (0)   ),       N(() => L<double,double>       ((double p)          => Sql.Atan   (p)!   .Value )) },
			{ M(() => Math.Atan2  (0,0) ),       N(() => L<double,double,double>((double x,double y) => Sql.Atan2  (x, y)!.Value )) },
			{ M(() => Math.Ceiling((decimal)0)), N(() => L<decimal,decimal>     ((decimal p)         => Sql.Ceiling(p)!   .Value )) },
			{ M(() => Math.Ceiling((double)0)),  N(() => L<double,double>       ((double p)          => Sql.Ceiling(p)!   .Value )) },
			{ M(() => Math.Cos            (0)),  N(() => L<double,double>       ((double p)          => Sql.Cos    (p)!   .Value )) },
			{ M(() => Math.Cosh           (0)),  N(() => L<double,double>       ((double p)          => Sql.Cosh   (p)!   .Value )) },
			{ M(() => Math.Exp            (0)),  N(() => L<double,double>       ((double p)          => Sql.Exp    (p)!   .Value )) },
			{ M(() => Math.Floor ((decimal)0)),  N(() => L<decimal,decimal>     ((decimal p)         => Sql.Floor  (p)!   .Value )) },
			{ M(() => Math.Floor  ((double)0)),  N(() => L<double,double>       ((double p)          => Sql.Floor  (p)!   .Value )) },
			{ M(() => Math.Log            (0)),  N(() => L<double,double>       ((double p)          => Sql.Log    (p)!   .Value )) },
			{ M(() => Math.Log          (0,0)),  N(() => L<double,double,double>((double m,double n) => Sql.Log    (n, m)!.Value )) },
			{ M(() => Math.Log10          (0)),  N(() => L<double,double>       ((double p)          => Sql.Log10  (p)!   .Value )) },

			{ M(() => Math.Max((byte)   0, (byte)   0)), N(() => L<byte,   byte,   byte>   ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((decimal)0, (decimal)0)), N(() => L<decimal,decimal,decimal>((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((double) 0, (double) 0)), N(() => L<double, double, double> ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((short)  0, (short)  0)), N(() => L<short,  short,  short>  ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((int)    0, (int)    0)), N(() => L<int,    int,    int>    ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((long)   0, (long)   0)), N(() => L<long,   long,   long>   ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((sbyte)  0, (sbyte)  0)), N(() => L<sbyte,  sbyte,  sbyte>  ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((float)  0, (float)  0)), N(() => L<float,  float,  float>  ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((ushort) 0, (ushort) 0)), N(() => L<ushort, ushort, ushort> ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((uint)   0, (uint)   0)), N(() => L<uint,   uint,   uint>   ((v1,v2) => v1 > v2 ? v1 : v2)) },
			{ M(() => Math.Max((ulong)  0, (ulong)  0)), N(() => L<ulong,  ulong,  ulong>  ((v1,v2) => v1 > v2 ? v1 : v2)) },

			{ M(() => Math.Min((byte)   0, (byte)   0)), N(() => L<byte,   byte,   byte>   ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((decimal)0, (decimal)0)), N(() => L<decimal,decimal,decimal>((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((double) 0, (double) 0)), N(() => L<double, double, double> ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((short)  0, (short)  0)), N(() => L<short,  short,  short>  ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((int)    0, (int)    0)), N(() => L<int,    int,    int>    ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((long)   0, (long)   0)), N(() => L<long,   long,   long>   ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((sbyte)  0, (sbyte)  0)), N(() => L<sbyte,  sbyte,  sbyte>  ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((float)  0, (float)  0)), N(() => L<float,  float,  float>  ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((ushort) 0, (ushort) 0)), N(() => L<ushort, ushort, ushort> ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((uint)   0, (uint)   0)), N(() => L<uint,   uint,   uint>   ((v1,v2) => v1 < v2 ? v1 : v2)) },
			{ M(() => Math.Min((ulong)  0, (ulong)  0)), N(() => L<ulong,  ulong,  ulong>  ((v1,v2) => v1 < v2 ? v1 : v2)) },

			{ M(() => Math.Pow        (0,0) ), N(() => L<double,double,double>    ((double x,double y) => Sql.Power(x, y)!.Value )) },

			{ M(() => Sql.Round       (0m)  ), N(() => L<decimal?,decimal?>       ((decimal? d)          => Sql.Round(d, 0))) },
			{ M(() => Sql.Round       (0.0) ), N(() => L<double?, double?>        ((double?  d)          => Sql.Round(d, 0))) },

			{ M(() => Sql.RoundToEven(0m)   ), N(() => L<decimal?,decimal?>       ((decimal? d)          => d - Sql.Floor(d) == 0.5m && Sql.Floor(d) % 2 == 0? Sql.Floor(d) : Sql.Round(d))) },
			{ M(() => Sql.RoundToEven(0.0)  ), N(() => L<double?, double?>        ((double?  d)          => d - Sql.Floor(d) == 0.5  && Sql.Floor(d) % 2 == 0? Sql.Floor(d) : Sql.Round(d))) },

			{ M(() => Sql.RoundToEven(0m, 0)), N(() => L<decimal?,int?,decimal?>((decimal? d,int? n) => d * 2 == Sql.Round(d * 2, n) && d != Sql.Round(d, n) ? Sql.Round(d / 2, n) * 2 : Sql.Round(d, n))) },
			{ M(() => Sql.RoundToEven(0.0,0)), N(() => L<double?, int?,double?> ((double?  d,int? n) => d * 2 == Sql.Round(d * 2, n) && d != Sql.Round(d, n) ? Sql.Round(d / 2, n) * 2 : Sql.Round(d, n))) },

			{ M(() => Math.Round     (0m)   ), N(() => L<decimal,decimal>         ( d    => Sql.RoundToEven(d)!.Value )) },
			{ M(() => Math.Round     (0.0)  ), N(() => L<double, double>          ( d    => Sql.RoundToEven(d)!.Value )) },

			{ M(() => Math.Round     (0m, 0)), N(() => L<decimal,int,decimal>   ((d,n) => Sql.RoundToEven(d, n)!.Value )) },
			{ M(() => Math.Round     (0.0,0)), N(() => L<double, int,double>    ((d,n) => Sql.RoundToEven(d, n)!.Value )) },

			{ M(() => Math.Round (0m,    MidpointRounding.ToEven)), N(() => L<decimal,MidpointRounding,decimal>      ((d,  p) => p == MidpointRounding.ToEven ? Sql.RoundToEven(d)!.  Value : Sql.Round(d)!.  Value )) },
			{ M(() => Math.Round (0.0,   MidpointRounding.ToEven)), N(() => L<double, MidpointRounding,double>       ((d,  p) => p == MidpointRounding.ToEven ? Sql.RoundToEven(d)!.  Value : Sql.Round(d)!.  Value )) },

			{ M(() => Math.Round (0m, 0, MidpointRounding.ToEven)), N(() => L<decimal,int,MidpointRounding,decimal>((d,n,p) => p == MidpointRounding.ToEven ? Sql.RoundToEven(d,n)!.Value : Sql.Round(d,n)!.Value )) },
			{ M(() => Math.Round (0.0,0, MidpointRounding.ToEven)), N(() => L<double, int,MidpointRounding,double> ((d,n,p) => p == MidpointRounding.ToEven ? Sql.RoundToEven(d,n)!.Value : Sql.Round(d,n)!.Value )) },

			{ M(() => Math.Sign  ((decimal)0)), N(() => L<decimal,int>(p => Sql.Sign(p)!.Value )) },
			{ M(() => Math.Sign  ((double) 0)), N(() => L<double, int>(p => Sql.Sign(p)!.Value )) },
			{ M(() => Math.Sign  ((short)  0)), N(() => L<short,  int>(p => Sql.Sign(p)!.Value )) },
			{ M(() => Math.Sign  ((int)    0)), N(() => L<int,    int>(p => Sql.Sign(p)!.Value )) },
			{ M(() => Math.Sign  ((long)   0)), N(() => L<long,   int>(p => Sql.Sign(p)!.Value )) },
			{ M(() => Math.Sign  ((sbyte)  0)), N(() => L<sbyte,  int>(p => Sql.Sign(p)!.Value )) },
			{ M(() => Math.Sign  ((float)  0)), N(() => L<float,  int>(p => Sql.Sign(p)!.Value )) },

			{ M(() => Math.Sin   (0)), N(() => L<double,double>((double p) => Sql.Sin (p)!.Value )) },
			{ M(() => Math.Sinh  (0)), N(() => L<double,double>((double p) => Sql.Sinh(p)!.Value )) },
			{ M(() => Math.Sqrt  (0)), N(() => L<double,double>((double p) => Sql.Sqrt(p)!.Value )) },
			{ M(() => Math.Tan   (0)), N(() => L<double,double>((double p) => Sql.Tan (p)!.Value )) },
			{ M(() => Math.Tanh  (0)), N(() => L<double,double>((double p) => Sql.Tanh(p)!.Value )) },

			{ M(() => Math.Truncate(0m)),  N(() => L<decimal,decimal>((decimal p) => Sql.Truncate(p)!.Value )) },
			{ M(() => Math.Truncate(0.0)), N(() => L<double,double>  ((double  p) => Sql.Truncate(p)!.Value )) },

			#endregion

			#region Visual Basic Compiler Services

//			{ M(() => Operators.CompareString("","",false)), L<S,S,B,I>((s1,s2,b) => b ? string.CompareOrdinal(s1.ToUpper(), s2.ToUpper()) : string.CompareOrdinal(s1, s2)) },

			#endregion

			#region SqlTypes

			{ M(() => new SqlBoolean().Value),   N(() => L<SqlBoolean,bool>((SqlBoolean obj) => (bool)obj))          },
			{ M(() => new SqlBoolean().IsFalse), N(() => L<SqlBoolean,bool>((SqlBoolean obj) => (bool)obj == false)) },
			{ M(() => new SqlBoolean().IsTrue),  N(() => L<SqlBoolean,bool>((SqlBoolean obj) => (bool)obj == true))  },
			{ M(() => SqlBoolean.True),          N(() => L<bool>           (()               => true))  },
			{ M(() => SqlBoolean.False),         N(() => L<bool>           (()               => false)) },

			#endregion
		};

		#endregion

		static Dictionary<string,Dictionary<MemberInfo,IExpressionInfo>> LoadMembers()
		{
			SetGenericInfoProvider(typeof(GenericInfoProvider<>));

			var members = new Dictionary<string,Dictionary<MemberInfo,IExpressionInfo>>
			{
				{ "", _commonMembers },

				#region SqlServer

				{ ProviderName.SqlServer, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.PadRight("",0,' ') ), N(() => L<string,int?,char,string>       ((string p0,int? p1,char p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ') ), N(() => L<string,int?,char,string>       ((string p0,int? p1,char p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },
					{ M(() => Sql.Trim    ("")       ), N(() => L<string,string?>                ((string p0)                   => Sql.TrimLeft(Sql.TrimRight(p0)))) },
					{ M(() => Sql.MakeDateTime(0,0,0)), N(() => L<int?,int?,int?,DateTime?>((int? y,int? m,int? d)  => DateAdd(Sql.DateParts.Month, (y!.Value - 1900) * 12 + m!.Value - 1, d!.Value - 1))) },
					{ M(() => Sql.Cosh(0)            ), N(() => L<double?,double?>               ( v    => (Sql.Exp(v) + Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Log(0m, 0)         ), N(() => L<decimal?,decimal?,decimal?>    ((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log(0.0,0)         ), N(() => L<double?,double?,double?>       ((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Sinh(0)            ), N(() => L<double?,double?>               ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)            ), N(() => L<double?,double?>               ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },
				}},

				#endregion

				#region SqlServer2000

				{ ProviderName.SqlServer2000, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.MakeDateTime(0, 0, 0, 0, 0, 0) ), N(() => L<int?,int?,int?,int?,int?,int?,DateTime?>((y,m,d,h,mm,s) => Sql.Convert(Sql.DateTime2,
						y.ToString() + "-" + m.ToString() + "-" + d.ToString() + " " +
						h.ToString() + ":" + mm.ToString() + ":" + s.ToString(), 120))) },
					{ M(() => DateTime.Parse("")),   N(() => L<string,DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) )) },
					{ M(() => Sql.RoundToEven(0m) ), N(() => L<decimal?,decimal?>((decimal? d) => d - Sql.Floor(d) == 0.5m && (long)Sql.Floor(d)! % 2 == 0? Sql.Floor(d) : Sql.Round(d))) },
					{ M(() => Sql.RoundToEven(0.0)), N(() => L<double?, double?> ((double?  d) => d - Sql.Floor(d) == 0.5  && (long)Sql.Floor(d)! % 2 == 0? Sql.Floor(d) : Sql.Round(d))) },
				}},

				#endregion

				#region SqlServer2005

				{ ProviderName.SqlServer2005, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.MakeDateTime(0, 0, 0, 0, 0, 0) ), N(() => L<int?,int?,int?,int?,int?,int?,DateTime?>((y,m,d,h,mm,s) => Sql.Convert(Sql.DateTime2,
						y.ToString() + "-" + m.ToString() + "-" + d.ToString() + " " +
						h.ToString() + ":" + mm.ToString() + ":" + s.ToString(), 120))) },
					{ M(() => DateTime.Parse("")), N(() => L<string,DateTime>(p0 => Sql.ConvertTo<DateTime>.From(p0) )) },
				}},

				#endregion

				#region SqlCe

				{ ProviderName.SqlCe, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Left    ("",0)    ), N(() => L<string?,int?,string?>    ((string? p0,int? p1)       => Sql.Substring(p0, 1, p1))) },
					{ M(() => Sql.Right   ("",0)    ), N(() => L<string?,int?,string?>    ((string? p0,int? p1)       => Sql.Substring(p0, p0!.Length - p1 + 1, p1))) },
					{ M(() => Sql.PadRight("",0,' ')), N(() => L<string,int?,char?,string>((string p0,int? p1,char? p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ')), N(() => L<string,int?,char?,string>((string p0,int? p1,char? p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },
					{ M(() => Sql.Trim    ("")      ), N(() => L<string?,string?>      ((string? p0)             => Sql.TrimLeft(Sql.TrimRight(p0)))) },

					{ M(() => Sql.Cosh(0)    ), N(() => L<double?,double?>   ( v    => (Sql.Exp(v) + Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Log (0m, 0)), N(() => L<decimal?,decimal?,decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log (0.0,0)), N(() => L<double?,double?,double?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Sinh(0)    ), N(() => L<double?,double?>   ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)    ), N(() => L<double?,double?>   ( v    => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },
				}},

				#endregion

				#region DB2

				{ ProviderName.DB2, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Space   (0)        ), N(() => L<int?,string>       ( p0           => Sql.Convert(Sql.VarChar(1000), Replicate(" ", p0)))) },
					{ M(() => Sql.Stuff   ("",0,0,"")), N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.PadRight("",0,' ') ), N(() => L<string,int?,char?,string>  ((p0,p1,p2)    => p0.Length > p1 ? p0 : p0 + VarChar(Replicate(p2, p1 - p0.Length)!, 1000))) },
					{ M(() => Sql.PadLeft ("",0,' ') ), N(() => L<string,int?,char?,string>  ((p0,p1,p2)    => p0.Length > p1 ? p0 : VarChar(Replicate(p2, p1 - p0.Length)!, 1000) + p0)) },

					{ M(() => Sql.ConvertTo<string>.From((decimal)0)), N(() => L<decimal,string?>((decimal p) => Sql.TrimLeft(Sql.Convert<string,decimal>(p), '0'))) },
					{ M(() => Sql.ConvertTo<string>.From(Guid.Empty)), N(() => L<Guid,   string?>((Guid    p) => Sql.Lower(
						Sql.Substring(Hex(p),  7,  2) + Sql.Substring(Hex(p),  5, 2) + Sql.Substring(Hex(p), 3, 2) + Sql.Substring(Hex(p), 1, 2) + "-" +
						Sql.Substring(Hex(p), 11,  2) + Sql.Substring(Hex(p),  9, 2) + "-" +
						Sql.Substring(Hex(p), 15,  2) + Sql.Substring(Hex(p), 13, 2) + "-" +
						Sql.Substring(Hex(p), 17,  4) + "-" +
						Sql.Substring(Hex(p), 21, 12)))) },

					{ M(() => Sql.Log(0m, 0)), N(() => L<decimal?,decimal?,decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log(0.0,0)), N(() => L<double?,double?,double?>   ((m,n) => Sql.Log(n) / Sql.Log(m))) },
				}},

				#endregion

				#region Informix

				{ ProviderName.Informix, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Left ("",0)     ), N(() => L<string?,int?,string?>     ((string? p0,int? p1)            => Sql.Substring(p0,  1, p1)))                  },
					{ M(() => Sql.Right("",0)     ), N(() => L<string?,int?,string?>     ((string? p0,int? p1)            => Sql.Substring(p0,  p0!.Length - p1 + 1, p1))) },
					{ M(() => Sql.Stuff("",0,0,"")), N(() => L<string?,int?,int?,string?,string?>((string? p0,int? p1,int? p2,string? p3) =>     AltStuff (p0,  p1, p2, p3)))             },
					{ M(() => Sql.Space(0)        ), N(() => L<int?,string?>       ((int? p0)                 => Sql.PadRight (" ", p0, ' ')))                },

					{ M(() => Sql.MakeDateTime(0,0,0)), N(() => L<int?,int?,int?,DateTime?>((y,m,d) => Mdy(m, d, y))) },

					{ M(() => Sql.Cot (0)         ), N(() => L<double?,double?>      ( v            => Sql.Cos(v) / Sql.Sin(v) ))        },
					{ M(() => Sql.Cosh(0)         ), N(() => L<double?,double?>      ( v            => (Sql.Exp(v) + Sql.Exp(-v)) / 2 )) },

					{ M(() => Sql.Degrees((decimal?)0)), N(() => L<decimal?,decimal?>( v => (decimal?)(v!.Value * (180 / (decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((double?) 0)), N(() => L<double?, double?> ( v => (double?) (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((short?)  0)), N(() => L<short?,  short?>  ( v => (short?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((int?)    0)), N(() => L<int?,    int?>    ( v => (int?)    (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((long?)   0)), N(() => L<long?,   long?>   ( v => (long?)   (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((sbyte?)  0)), N(() => L<sbyte?,  sbyte?>  ( v => (sbyte?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((float?)  0)), N(() => L<float?,  float?>  ( v => (float?)  (v!.Value * (180 / Math.PI)))) },

					{ M(() => Sql.Log(0m, 0)), N(() => L<decimal?,decimal?,decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log(0.0,0)), N(() => L<double?,double?,double?>((m,n) => Sql.Log(n) / Sql.Log(m))) },

					{ M(() => Sql.Sign((decimal?)0)), N(() => L<decimal?,int?>((decimal? p) => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((double?) 0)), N(() => L<double?, int?>((double?  p) => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((short?)  0)), N(() => L<short?,  int?>((short?   p) => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((int?)    0)), N(() => L<int?,    int?>((int?     p) => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((long?)   0)), N(() => L<long?,   int?>((long?    p) => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((sbyte?)  0)), N(() => L<sbyte?,  int?>((sbyte?   p) => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },
					{ M(() => Sql.Sign((float?)  0)), N(() => L<float?,  int?>((float?   p) => (int?)(p > 0 ? 1 : p < 0 ? -1 : 0) )) },

					{ M(() => Sql.Sinh(0)), N(() => L<double?,double?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)), N(() => L<double?,double?>( v => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) +Sql.Exp(-v)))) },
				}},

				#endregion

				#region Oracle

				{ ProviderName.Oracle, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Left ("",0)     ), N(() => L<string?,int?,string?>             ((string? p0,int? p1)            => Sql.Substring(p0, 1, p1))) },
					{ M(() => Sql.Right("",0)     ), N(() => L<string?,int?,string?>             ((string? p0,int? p1)            => Sql.Substring(p0, p0!.Length - p1 + 1, p1))) },
					{ M(() => Sql.Stuff("",0,0,"")), N(() => L<string?,int?,int?,string?,string?>((string? p0,int? p1,int? p2,string? p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.Space(0)        ), N(() => L<int?,string?>       ((int? p0)                 => Sql.PadRight(" ", p0, ' '))) },

					{ M(() => Sql.ConvertTo<string>.From(Guid.Empty)), N(() => L<Guid,string?>(p => Sql.Lower(
						Sql.Substring(Sql.Convert2(Sql.Char(36), p),  7,  2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p),  5, 2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p), 3, 2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p), 1, 2) + "-" +
						Sql.Substring(Sql.Convert2(Sql.Char(36), p), 11,  2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p),  9, 2) + "-" +
						Sql.Substring(Sql.Convert2(Sql.Char(36), p), 15,  2) + Sql.Substring(Sql.Convert2(Sql.Char(36), p), 13, 2) + "-" +
						Sql.Substring(Sql.Convert2(Sql.Char(36), p), 17,  4) + "-" +
						Sql.Substring(Sql.Convert2(Sql.Char(36), p), 21, 12)))) },

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

				{ ProviderName.Firebird, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M<string?>(_  => Sql.Space(0         )), N(() => L<int?,string?>       ( p0           => Sql.PadRight(" ", p0, ' '))) },
					{ M<string?>(s  => Sql.Stuff(s, 0, 0, s)), N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },

					{ M(() => Sql.Degrees((decimal?)0)), N(() => L<decimal?,decimal?>((decimal? v) => (decimal?)(v!.Value * 180 / DecimalPI()))) },
					{ M(() => Sql.Degrees((double?) 0)), N(() => L<double?, double?> ((double?  v) => (double?) (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((short?)  0)), N(() => L<short?,  short?>  ((short?   v) => (short?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((int?)    0)), N(() => L<int?,    int?>    ((int?     v) => (int?)    (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((long?)   0)), N(() => L<long?,   long?>   ((long?    v) => (long?)   (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((sbyte?)  0)), N(() => L<sbyte?,  sbyte?>  ((sbyte?   v) => (sbyte?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((float?)  0)), N(() => L<float?,  float?>  ((float?   v) => (float?)  (v!.Value * (180 / Math.PI)))) },

					{ M(() => Sql.RoundToEven(0.0)  ), N(() => L<double?,double?>     ((double? v)        => (double?)Sql.RoundToEven((decimal)v!)))    },
					{ M(() => Sql.RoundToEven(0.0,0)), N(() => L<double?,int?,double?>((double? v,int? p) => (double?)Sql.RoundToEven((decimal)v!, p))) },
				}},

				#endregion

				#region MySql

				{ ProviderName.MySql, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M<string>(s => Sql.Stuff(s, 0, 0, s)), N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },

					{ M(() => Sql.Cosh(0)), N(() => L<double?,double?>(v => (Sql.Exp(v) + Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Sinh(0)), N(() => L<double?,double?>(v => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)), N(() => L<double?,double?>(v => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },
				}},

				#endregion

				#region PostgreSQL

				{ ProviderName.PostgreSQL, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Left ("",0)     ), N(() => L<string?,int?,string?>             ((p0,p1)                                   => Sql.Substring(p0, 1, p1))) },
					{ M(() => Sql.Right("",0)     ), N(() => L<string?,int?,string?>             ((string? p0,int? p1)                     => Sql.Substring(p0, p0!.Length - p1 + 1, p1))) },
					{ M(() => Sql.Stuff("",0,0,"")), N(() => L<string?,int?,int?,string?,string?>((string? p0,int? p1,int? p2,string? p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.Space(0)        ), N(() => L<int?,string?>                     ((int? p0)                              => Replicate(" ", p0))) },

					{ M(() => Sql.Cosh(0)           ), N(() => L<double?,double?>     ((double? v)        => (Sql.Exp(v) + Sql.Exp(-v)) / 2 )) },
					{ M(() => Sql.Round      (0.0,0)), N(() => L<double?,int?,double?>((double? v,int? p) => (double?)Sql.Round      ((decimal)v!, p))) },
					{ M(() => Sql.RoundToEven(0.0)  ), N(() => L<double?,double?>     ((double? v)        => (double?)Sql.RoundToEven((decimal)v!)))    },
					{ M(() => Sql.RoundToEven(0.0,0)), N(() => L<double?,int?,double?>((double? v,int? p) => (double?)Sql.RoundToEven((decimal)v!, p))) },

					{ M(() => Sql.Log  ((double)0,0)), N(() => L<double?,double?,double?>((double? m,double? n)        => (double?)Sql.Log((decimal)m!,(decimal)n!)!.Value)) },
					{ M(() => Sql.Sinh (0)          ), N(() => L<double?,double?>        ((double? v)                  => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh (0)          ), N(() => L<double?,double?>        ((double? v)                  => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },

					{ M(() => Sql.Truncate(0.0)     ), N(() => L<double?,double?>    ((double? v)                  => (double?)Sql.Truncate((decimal)v!))) },
				}},

				#endregion

				#region SQLite

				{ ProviderName.SQLite, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Stuff   ("",0,0,"")), N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.PadRight("",0,' ') ), N(() => L<string,int?,char?,string>         ((p0,p1,p2)    => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ') ), N(() => L<string,int?,char?,string>         ((p0,p1,p2)    => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },

					{ M(() => Sql.MakeDateTime(0, 0, 0)), N(() => L<int?,int?,int?,DateTime?>((y,m,d) => Sql.Convert(Sql.Date,
						y.ToString() + "-" +
						(m.ToString()!.Length == 1 ? "0" + m.ToString() : m.ToString()) + "-" +
						(d.ToString()!.Length == 1 ? "0" + d.ToString() : d.ToString())))) },

					{ M(() => Sql.MakeDateTime(0, 0, 0, 0, 0, 0)), N(() => L<int?,int?,int?,int?,int?,int?,DateTime?>((y,m,d,h,i,s) => Sql.Convert(Sql.DateTime2,
						y.ToString() + "-" +
						(m.ToString()!.Length == 1 ? "0" + m.ToString() : m.ToString()) + "-" +
						(d.ToString()!.Length == 1 ? "0" + d.ToString() : d.ToString()) + " " +
						(h.ToString()!.Length == 1 ? "0" + h.ToString() : h.ToString()) + ":" +
						(i.ToString()!.Length == 1 ? "0" + i.ToString() : i.ToString()) + ":" +
						(s.ToString()!.Length == 1 ? "0" + s.ToString() : s.ToString())))) },

					{ M(() => Sql.ConvertTo<string>.From(Guid.Empty)), N(() => L<Guid,string?>((Guid p) => Sql.Lower(
						Sql.Substring(Hex(p),  7,  2) + Sql.Substring(Hex(p),  5, 2) + Sql.Substring(Hex(p), 3, 2) + Sql.Substring(Hex(p), 1, 2) + "-" +
						Sql.Substring(Hex(p), 11,  2) + Sql.Substring(Hex(p),  9, 2) + "-" +
						Sql.Substring(Hex(p), 15,  2) + Sql.Substring(Hex(p), 13, 2) + "-" +
						Sql.Substring(Hex(p), 17,  4) + "-" +
						Sql.Substring(Hex(p), 21, 12)))) },

					{ M(() => Sql.Log (0m, 0)), N(() => L<decimal?,decimal?,decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log (0.0,0)), N(() => L<double?,double?,double?>   ((m,n) => Sql.Log(n) / Sql.Log(m))) },

					{ M(() => Sql.Truncate(0m)),  N(() => L<decimal?,decimal?>((decimal? v) => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
					{ M(() => Sql.Truncate(0.0)), N(() => L<double?,double?>  ((double?  v) => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
				}},

				{ ProviderName.SQLiteMS, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Math.Floor((decimal)0)), N(() => L<decimal,decimal>((decimal x) => x > 0 ? (int)x : (int)(x-0.9999999999999999m) )) },
					{ M(() => Math.Floor ((double)0)), N(() => L<double,double>  ((double  x) => x > 0 ? (int)x : (int)(x-0.9999999999999999) )) },
				}},

				#endregion

				#region Sybase

				{ ProviderName.Sybase, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.PadRight("",0,' ')), N(() => L<string,int?,char?,string>((p0,p1,p2) => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ')), N(() => L<string,int?,char?,string>((p0,p1,p2) => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },
					{ M(() => Sql.Trim    ("")      ), N(() => L<string?,string?>         ( p0        => Sql.TrimLeft(Sql.TrimRight(p0)))) },

					{ M(() => Sql.Cosh(0)    ),          N(() => L<double?,double?>           ( v    => (Sql.Exp(v) + Sql.Exp(-v)) / 2))  },
					{ M(() => Sql.Log (0m, 0)),          N(() => L<decimal?,decimal?,decimal?>((m,n) => Sql.Log(n) / Sql.Log(m))) },
					{ M(() => Sql.Log (0.0,0)),          N(() => L<double?,double?,double?>   ((m,n) => Sql.Log(n) / Sql.Log(m)))    },

					{ M(() => Sql.Degrees((decimal?)0)), N(() => L<decimal?,decimal?>( v => (decimal?)(v!.Value * (180 / (decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((double?) 0)), N(() => L<double?, double?> ( v => (double?) (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((short?)  0)), N(() => L<short?,  short?>  ( v => (short?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((int?)    0)), N(() => L<int?,    int?>    ( v => (int?)    (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((long?)   0)), N(() => L<long?,   long?>   ( v => (long?)   (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((sbyte?)  0)), N(() => L<sbyte?,  sbyte?>  ( v => (sbyte?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((float?)  0)), N(() => L<float?,  float?>  ( v => (float?)  (v!.Value * (180 / Math.PI)))) },

					{ M(() => Sql.Sinh(0)), N(() => L<double?,double?>((double? v) => (Sql.Exp(v) - Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Tanh(0)), N(() => L<double?,double?>((double? v) => (Sql.Exp(v) - Sql.Exp(-v)) / (Sql.Exp(v) + Sql.Exp(-v)))) },

					{ M(() => Sql.Truncate(0m)),  N(() => L<decimal?,decimal?>((decimal? v) => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
					{ M(() => Sql.Truncate(0.0)), N(() => L<double?,double?>  ((double?  v) => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
				}},

				#endregion

				#region Access

				{ ProviderName.Access, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Stuff   ("",0,0,"")), N(() => L<string?,int?,int?,string?,string?>((p0,p1,p2,p3) => AltStuff(p0, p1, p2, p3))) },
					{ M(() => Sql.PadRight("",0,' ') ), N(() => L<string,int?,char?,string>         ((p0,p1,p2)    => p0.Length > p1 ? p0 : p0 + Replicate(p2, p1 - p0.Length))) },
					{ M(() => Sql.PadLeft ("",0,' ') ), N(() => L<string,int?,char?,string>         ((p0,p1,p2)    => p0.Length > p1 ? p0 : Replicate(p2, p1 - p0.Length) + p0)) },
					{ M(() => Sql.MakeDateTime(0,0,0)), N(() => L<int?,int?,int?,DateTime?>         ((y,m,d)       => MakeDateTime2(y, m, d)))                                   },

					{ M(() => Sql.ConvertTo<string>.From(Guid.Empty)), N(() => L<Guid,string?>(p => Sql.Lower(Sql.Substring(p.ToString(), 2, 36)))) },

					{ M(() => Sql.Ceiling((decimal)0)), N(() => L<decimal?,decimal?>(p => -Sql.Floor(-p) )) },
					{ M(() => Sql.Ceiling((double) 0)), N(() => L<double?, double?> (p => -Sql.Floor(-p) )) },

					{ M(() => Sql.Cot  (0)    ), N(() => L<double?,double?>           ((double? v)             => Sql.Cos(v) / Sql.Sin(v)       )) },
					{ M(() => Sql.Cosh (0)    ), N(() => L<double?,double?>           ((double? v)             => (Sql.Exp(v) + Sql.Exp(-v)) / 2)) },
					{ M(() => Sql.Log  (0m, 0)), N(() => L<decimal?,decimal?,decimal?>((decimal? m,decimal? n) => Sql.Log(n) / Sql.Log(m)       )) },
					{ M(() => Sql.Log  (0.0,0)), N(() => L<double?,double?,double?>   ((double? m,double? n)   => Sql.Log(n) / Sql.Log(m)       )) },
					{ M(() => Sql.Log10(0.0)  ), N(() => L<double?,double?>           ((double? n)             => Sql.Log(n) / Sql.Log(10.0)    )) },

					{ M(() => Sql.Degrees((decimal?)0)), N(() => L<decimal?,decimal?>((decimal? v) => (decimal?)         (          v!.Value  * (180 / (decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((double?) 0)), N(() => L<double?, double?> ((double?  v) => (double?)          (          v!.Value  * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((short?)  0)), N(() => L<short?,  short?>  ((short?   v) => (short?)  AccessInt(AccessInt(v!.Value) * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((int?)    0)), N(() => L<int?,    int?>    ((int?     v) => (int?)    AccessInt(AccessInt(v!.Value) * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((long?)   0)), N(() => L<long?,   long?>   ((long?    v) => (long?)   AccessInt(AccessInt(v!.Value) * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((sbyte?)  0)), N(() => L<sbyte?,  sbyte?>  ((sbyte?   v) => (sbyte?)  AccessInt(AccessInt(v!.Value) * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((float?)  0)), N(() => L<float?,  float?>  ((float?   v) => (float?)           (          v!.Value  * (180 / Math.PI)))) },

					{ M(() => Sql.Round      (0m)   ), N(() => L<decimal?,decimal?>  ((decimal? d) => d - Sql.Floor(d) == 0.5m && Sql.Floor(d) % 2 == 0? Sql.Ceiling(d) : AccessRound(d, 0))) },
					{ M(() => Sql.Round      (0.0)  ), N(() => L<double?, double?>   ((double?  d) => d - Sql.Floor(d) == 0.5  && Sql.Floor(d) % 2 == 0? Sql.Ceiling(d) : AccessRound(d, 0))) },
					{ M(() => Sql.Round      (0m, 0)), N(() => L<decimal?,int?,decimal?>((decimal? v,int? p)=> (decimal?)(
						p == 1 ? Sql.Round(v * 10) / 10 :
						p == 2 ? Sql.Round(v * 10) / 10 :
						p == 3 ? Sql.Round(v * 10) / 10 :
						p == 4 ? Sql.Round(v * 10) / 10 :
						p == 5 ? Sql.Round(v * 10) / 10 :
								 Sql.Round(v * 10) / 10))) },
					{ M(() => Sql.Round      (0.0,0)), N(() => L<double?,int?,double?>((double? v,int? p) => (double?)(
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

					{ M(() => Sql.Truncate(0m)),  N(() => L<decimal?,decimal?>((decimal? v) => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
					{ M(() => Sql.Truncate(0.0)), N(() => L<double?,double?>  ((double?  v) => v >= 0 ? Sql.Floor(v) : Sql.Ceiling(v))) },
				}},

				#endregion

				#region SapHana

				{ ProviderName.SapHana, new Dictionary<MemberInfo,IExpressionInfo> {
					{ M(() => Sql.Degrees((decimal?)0)), N(() => L<decimal?,decimal?>((decimal? v) => (decimal?) (v!.Value * (180 / (decimal)Math.PI)))) },
					{ M(() => Sql.Degrees((double?) 0)), N(() => L<double?, double?> ((double?  v) => (double?)  (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((short?)  0)), N(() => L<short?,  short?>  ((short?   v) => (short?)   (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((int?)    0)), N(() => L<int?,    int?>    ((int?     v) => (int?)     (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((long?)   0)), N(() => L<long?,   long?>   ((long?    v) => (long?)    (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((sbyte?)  0)), N(() => L<sbyte?,  sbyte?>  ((sbyte?   v) => (sbyte?)   (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.Degrees((float?)  0)), N(() => L<float?,  float?>  ((float?   v) => (float?)   (v!.Value * (180 / Math.PI)))) },
					{ M(() => Sql.RoundToEven(0.0)  ), N(() => L<double?,double?>       ((double? v)          => (double?)Sql.RoundToEven((decimal)v!)))    },
					{ M(() => Sql.RoundToEven(0.0,0)), N(() => L<double?,int?,double?>((double? v,int? p) => (double?)Sql.RoundToEven((decimal)v!, p))) },
					{ M(() => Sql.Stuff("",0,0,"")), N(() => L<string?,int?,int?,string?,string?>((string? p0,int? p1,int? p2,string? p3) => AltStuff (p0,  p1, p2, p3)))             },
				}},

				#endregion
			};

#if DEBUG

			foreach (var membersValue in members.Values)
			{
				foreach (var member in membersValue)
				{
					int pCount;

					if (member.Key is MethodInfo method)
					{
						pCount = method.GetParameters().Length;

						if (!method.IsStatic)
							pCount++;
					}
					else if (member.Key is ConstructorInfo ctor)
					{
						pCount = ctor.GetParameters().Length;
					}
					else if (member.Key is PropertyInfo prop)
					{
						pCount = prop.GetGetMethod()?.IsStatic == true ? 0 : 1;
					}
					else if (member.Key is FieldInfo field)
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

			public override bool Equals(object? obj)
			{
				return obj is TypeMember other
					&& Type == other.Type
					&& string.Equals(Member, other.Member);
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
			if (!_typeMembers.TryGetValue(providerName, out var dic))
				_typeMembers.Add(providerName, dic = new Dictionary<TypeMember,IExpressionInfo>());

			var expr = new LazyExpressionInfo();

			expr.SetExpression(expression);

			dic[new TypeMember(objectType, memberInfo.Name)] = expr;

			_checkUserNamespace = false;
		}

		public static void MapMember(string providerName, Type objectType, MemberInfo memberInfo, IExpressionInfo expressionInfo)
		{
			if (!_typeMembers.TryGetValue(providerName, out var dic))
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

				{ MT<bool   >(() => ((bool)   true).ToString()), N(() => L<bool,    string>((bool    p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<byte   >(() => ((byte)    0)  .ToString()), N(() => L<byte,    string>((byte    p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<char   >(() => ((char)   '0') .ToString()), N(() => L<char,    string>((char    p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<decimal>(() => ((decimal) 0)  .ToString()), N(() => L<decimal, string>((decimal p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<double >(() => ((double)  0)  .ToString()), N(() => L<double,  string>((double  p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<short  >(() => ((short)   0)  .ToString()), N(() => L<short,   string>((short   p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<int    >(() => ((int)     0)  .ToString()), N(() => L<int,     string>((int     p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<long   >(() => ((long)    0)  .ToString()), N(() => L<long,    string>((long    p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<sbyte  >(() => ((sbyte)   0)  .ToString()), N(() => L<sbyte,   string>((sbyte   p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<float  >(() => ((float)   0)  .ToString()), N(() => L<float,   string>((float   p0) => Sql.ConvertTo<string>.From(p0) )) },
//				{ MT<string >(() => ((string) "0") .ToString()), N(() => L<string,  string>((string  p0) => p0                             )) },
				{ MT<ushort >(() => ((ushort)  0)  .ToString()), N(() => L<ushort,  string>((ushort  p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<uint   >(() => ((uint)    0)  .ToString()), N(() => L<uint,    string>((uint    p0) => Sql.ConvertTo<string>.From(p0) )) },
				{ MT<ulong  >(() => ((ulong)   0)  .ToString()), N(() => L<ulong,   string>((ulong   p0) => Sql.ConvertTo<string>.From(p0) )) },

				#endregion

			}},
		};

		#region Sql specific

		[CLSCompliant(false)]
		[Sql.Function("RTrim", 0)]
		public static string? TrimRight(string? str, params char[] trimChars)
		{
			return str?.TrimEnd(trimChars);
		}

		[CLSCompliant(false)]
		[Sql.Function("LTrim", 0)]
		public static string? TrimLeft(string? str, params char[] trimChars)
		{
			return str?.TrimStart(trimChars);
		}

		#endregion

		#region Provider specific functions

		[Sql.Function]
		public static int? ConvertToCaseCompareTo(string? str, string? value)
		{
			return str == null || value == null ? (int?)null : str.CompareTo(value);
		}

		// Access, DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite
		//
		[Sql.Function]
		public static string? AltStuff(string? str, int? startLocation, int? length, string? value)
		{
			return Sql.Stuff(str, startLocation, length, value);
		}

		// DB2
		//
		[Sql.Function]
		public static string? VarChar(object obj, int? size)
		{
			return obj.ToString();
		}

		// DB2
		//
		[Sql.Function]
		public static string? Hex(Guid? guid)
		{
			return guid == null ? null : guid.ToString();
		}

		// DB2, PostgreSQL, Access, MS SQL, SqlCe
		//
		[CLSCompliant(false)]
		[Sql.Function]
		[Sql.Function(ProviderName.DB2,        "Repeat")]
		[Sql.Function(ProviderName.PostgreSQL, "Repeat")]
		[Sql.Function(ProviderName.Access,     "string", 1, 0)]
		public static string? Replicate(string? str, int? count)
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
		[Sql.Function(ProviderName.Access,     "string", 1, 0)]
		public static string? Replicate(char? ch, int? count)
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
		class DateAddBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var partStr = Sql.DatePartBuilder.DatePartToStr(part);
				var number  = builder.GetExpression("number");
				var days    = builder.GetExpression("days");

				builder.ResultExpression = new SqlQuery.SqlFunction(typeof(DateTime?), builder.Expression,
					new SqlQuery.SqlExpression(partStr, SqlQuery.Precedence.Primary), number, days);
			}
		}

		[Sql.Extension("DateAdd", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilder))]
		public static DateTime? DateAdd(Sql.DateParts part, int? number, int? days)
		{
			return days == null ? null : Sql.DateAdd(part, number, new DateTime(1900, 1, days.Value + 1));
		}

		// MSSQL
		//
		[Sql.Function]
		public static decimal? Round(decimal? value, int precision, int mode) => 0;

		[Sql.Function]
		public static double?  Round(double?  value, int precision, int mode) => 0;

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
		[Sql.Function("PI", ServerSideOnly = true, CanBeNull = false)]
		public static decimal DecimalPI() { return (decimal)Math.PI; }
		[Sql.Function("PI", ServerSideOnly = true, CanBeNull = false)]
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
