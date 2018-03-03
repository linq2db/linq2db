﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using JetBrains.Annotations;

using LinqToDB.Mapping;

using NotNull = JetBrains.Annotations.NotNullAttribute;

namespace LinqToDB
{
	using Extensions;
	using SqlQuery;

	[AttributeUsage(AttributeTargets.Parameter)]
	[MeansImplicitUse]
	public class ExprParameterAttribute : Attribute
	{
		public string Name { get; set; }

		public ExprParameterAttribute(string name)
		{
			Name = name;
		}

		public ExprParameterAttribute()
		{
		}
	}

	public static class ExtensionlBuilderExtensions
	{
		public static Sql.SqlExtensionParam AddParameter(this Sql.ISqExtensionBuilder builder, string name, string value)
		{
			return builder.AddParameter(name, new SqlValue(value));
		}

		public static Sql.SqlExtensionParam AddExpression(this Sql.ISqExtensionBuilder builder, string name, string expr)
		{
			return builder.AddParameter(name, new SqlExpression(expr, Precedence.Primary));
		}

		public static ISqlExpression Add(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right, Type type)
		{
			return new SqlBinaryExpression(type, left, "+", right, Precedence.Additive);
		}
		public static ISqlExpression Add<T>(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right)
		{
			return builder.Add(left, right, typeof(T));
		}

		public static ISqlExpression Add(this Sql.ISqExtensionBuilder builder, ISqlExpression left, int value)
		{
			return builder.Add<int>(left, new SqlValue(value));
		}

		public static ISqlExpression Inc(this Sql.ISqExtensionBuilder builder, ISqlExpression expr)
		{
			return builder.Add(expr, 1);
		}

		public static ISqlExpression Sub(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right, Type type)
		{
			return new SqlBinaryExpression(type, left, "-", right, Precedence.Subtraction);
		}

		public static ISqlExpression Sub<T>(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right)
		{
			return builder.Sub(left, right, typeof(T));
		}

		public static ISqlExpression Sub(this Sql.ISqExtensionBuilder builder, ISqlExpression left, int value)
		{
			return builder.Sub<int>(left, new SqlValue(value));
		}

		public static ISqlExpression Dec(this Sql.ISqExtensionBuilder builder, ISqlExpression expr)
		{
			return builder.Sub(expr, 1);
		}

		public static ISqlExpression Mul(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right, Type type)
		{
			return new SqlBinaryExpression(type, left, "*", right, Precedence.Multiplicative);
		}

		public static ISqlExpression Mul<T>(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right)
		{
			return builder.Mul(left, right, typeof(T));
		}

		public static ISqlExpression Mul(this Sql.ISqExtensionBuilder builder, ISqlExpression expr1, int value)
		{
			return builder.Mul<int>(expr1, new SqlValue(value));
		}

		public static ISqlExpression Div(this Sql.ISqExtensionBuilder builder, ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return new SqlBinaryExpression(type, expr1, "/", expr2, Precedence.Multiplicative);
		}

		public static ISqlExpression Div<T>(this Sql.ISqExtensionBuilder builder, ISqlExpression expr1, ISqlExpression expr2)
		{
			return builder.Div(expr1, expr2, typeof(T));
		}

		public static ISqlExpression Div(this Sql.ISqExtensionBuilder builder, ISqlExpression expr1, int value)
		{
			return builder.Div<int>(expr1, new SqlValue(value));
		}
	}

	public partial class Sql
	{
		public interface ISqlExtension
		{
		}

		public static ISqlExtension Ext => null;

		public interface IExtensionCallBuilder
		{
			void Build(ISqExtensionBuilder builder);
		}

		public interface ISqExtensionBuilder
		{
			string         Configuration    { get; }
			MappingSchema  Mapping          { get; }
			SelectQuery    Query            { get; }
			MemberInfo     Member           { get; }
			SqlExtension   Extension        { get; }
			ISqlExpression ResultExpression { get; set; }
			string         Expression       { get; set; }
			Expression[]   Arguments        { get; }

			T GetValue<T>(int index);
			T GetValue<T>(string argName);

			ISqlExpression GetExpression(int index);
			ISqlExpression GetExpression(string argName);
			ISqlExpression ConvertToSqlExpression();
			ISqlExpression ConvertToSqlExpression(int precedence);
			ISqlExpression ConvertExpressionToSql(Expression expression);

			SqlExtensionParam AddParameter(string name, ISqlExpression expr);
		}

		public class SqlExtension
		{
			public Dictionary<string,List<SqlExtensionParam>> NamedParameters => _namedParameters;

			readonly Dictionary<string,List<SqlExtensionParam>> _namedParameters;
			public int ChainPrecedence { get; set; }

			public SqlExtension(
				Type systemType, string expr, int precedence, int chainPrecedence, bool isAggregate, params SqlExtensionParam[] parameters)
			{
				if (parameters == null) throw new ArgumentNullException(nameof(parameters));

				foreach (var value in parameters)
					if (value == null) throw new ArgumentNullException(nameof(parameters));

				SystemType       = systemType;
				Expr             = expr;
				Precedence       = precedence;
				ChainPrecedence  = chainPrecedence;
				IsAggregate      = isAggregate;
				_namedParameters = parameters.ToLookup(p => p.Name).ToDictionary(p => p.Key, p => p.ToList());
			}

			public SqlExtension(string expr, params SqlExtensionParam[] parameters)
				: this(null, expr, SqlQuery.Precedence.Unknown, 0, false, parameters)
			{
			}

			public Type   SystemType  { get; set; }
			public string Expr        { get; set; }
			public int    Precedence  { get; set; }
			public bool   IsAggregate { get; set; }

			public SqlExtensionParam AddParameter(string name, ISqlExpression sqlExpression)
			{
				return AddParameter(new SqlExtensionParam(name ?? string.Empty, sqlExpression));
			}

			public SqlExtensionParam AddParameter(SqlExtensionParam param)
			{
				var key = param.Name ?? string.Empty;

				if (!_namedParameters.TryGetValue(key, out var list))
				{
					list = new List<SqlExtensionParam>();
					_namedParameters.Add(key, list);
				}

				list.Add(param);
				return param;
			}

			public IEnumerable<SqlExtensionParam> GetParametersByName(string name)
			{
				if (_namedParameters.TryGetValue(name, out var list))
					return list;
				return Enumerable.Empty<SqlExtensionParam>();
			}

			public SqlExtensionParam[] GetParameters()
			{
				return _namedParameters.Values.SelectMany(_ => _).ToArray();
			}
		}

		public class SqlExtensionParam
		{
			public SqlExtensionParam(string name, ISqlExpression expression)
			{
				Name       = name;
				Expression = expression;
			}

			public SqlExtensionParam(string name, SqlExtension extension)
			{
				Name      = name;
				Extension = extension;
			}

			public string         Name       { get; set; }
			public SqlExtension   Extension  { get; set; }
			public ISqlExpression Expression { get; set; }
		}

		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
		public class ExtensionAttribute : ExpressionAttribute
		{
			private static ConcurrentDictionary<Type, IExtensionCallBuilder> _builders = new ConcurrentDictionary<Type, IExtensionCallBuilder>();

			public string TokenName { get; set; }

			protected class ExtensionBuilder: ISqExtensionBuilder
			{
				readonly ConvertHelper _convert;

				public ExtensionBuilder(
					string                     configuration,
					[NotNull]   MappingSchema  mapping,
					[NotNull]	SelectQuery    query,
					[NotNull]   SqlExtension   extension,
					[NotNull]   ConvertHelper  convertHeper,
					[NotNull]   MemberInfo     member,
					[NotNull]   Expression[]   arguments)
				{
					Mapping       = mapping      ?? throw new ArgumentNullException(nameof(mapping));
					Query         = query        ?? throw new ArgumentNullException(nameof(query));
					Configuration = configuration;
					Extension     = extension    ?? throw new ArgumentNullException(nameof(extension));
					_convert      = convertHeper ?? throw new ArgumentNullException(nameof(convertHeper));
					Member        = member;
					Method        = member as MethodInfo;
					Arguments     = arguments    ?? throw new ArgumentNullException(nameof(arguments));
				}

				public MethodInfo   Method { get; }

				public ISqlExpression ConvertExpression(Expression expr)
				{
					return _convert.Convert(expr);
				}

				#region ISqExtensionBuilder Members

				public string         Configuration    { get; }
				public MappingSchema  Mapping          { get; }
				public SelectQuery    Query            { get; }
				public MemberInfo     Member           { get; }
				public SqlExtension   Extension        { get; }
				public ISqlExpression ResultExpression { get; set; }
				public Expression[]   Arguments        { get; }

				public string Expression
				{
					get => Extension.Expr;
					set => Extension.Expr = value;
				}

				public T GetValue<T>(int index)
				{
					var lambda = System.Linq.Expressions.Expression.Lambda<Func<T>>(Arguments[index]);
					return lambda.Compile()();
				}

				public T GetValue<T>(string argName)
				{
					if (Method != null)
					{
						var parameters = Method.GetParameters();
						for (int i = 0; i < parameters.Length; i++)
						{
							if (parameters[i].Name == argName)
							{
								return GetValue<T>(i);
							}
						}
					}

					throw new InvalidOperationException(string.Format("Argument '{0}' bot found", argName));
				}

				public ISqlExpression GetExpression(int index)
				{
					return ConvertExpression(Arguments[index]);
				}

				public ISqlExpression GetExpression(string argName)
				{
					if (Method != null)
					{
						var parameters = Method.GetParameters();
						for (int i = 0; i < parameters.Length; i++)
						{
							if (parameters[i].Name == argName)
							{
								return GetExpression(i);
							}
						}
					}

					throw new InvalidOperationException(string.Format("Argument '{0}' bot found", argName));
				}

				public ISqlExpression ConvertToSqlExpression()
				{
					return ConvertToSqlExpression(Extension.Precedence);
				}

				public ISqlExpression ConvertToSqlExpression(int precedence)
				{
					return BuildSqlExpression(Extension, Extension.SystemType, precedence, Extension.IsAggregate);
				}

				public ISqlExpression ConvertExpressionToSql(Expression expression)
				{
					return ConvertExpression(expression);
				}

				public SqlExtensionParam AddParameter(string name, ISqlExpression expr)
				{
					return Extension.AddParameter(name, expr);
				}

				#endregion

			}

			public Type      BuilderType     { get; set; }
			public int       ChainPrecedence { get; set; }

			public ExtensionAttribute(string expression): this(string.Empty, expression)
			{
			}

			public ExtensionAttribute(string configuration, string expression) : base(configuration, expression)
			{
				ExpectExpression = true;
				ServerSideOnly   = true;
				PreferServerSide = true;
			}

			public ExtensionAttribute(Type builderType): this(string.Empty, string.Empty)
			{
				BuilderType = builderType;
			}

			static T GetExpressionValue<T>(Expression expr)
			{
				var lambda = System.Linq.Expressions.Expression.Lambda<Func<T>>(expr);
				return lambda.Compile()();
			}

			protected List<SqlExtensionParam> BuildFunctionsChain(MappingSchema mapping, SelectQuery query, Expression expr, ConvertHelper convertHelper)
			{
				var chains   = new List<SqlExtensionParam>();
				var current  = expr;

				while (current != null)
				{
					MemberInfo   memberInfo;
					Expression[] arguments;
					switch (current.NodeType)
					{
						case ExpressionType.MemberAccess :
							{
								var memberExpr = (MemberExpression)current;

								memberInfo = memberExpr.Member;
								arguments  = new Expression[0];
								current    = memberExpr.Expression;

								break;
							}

						case ExpressionType.Call :
							{
								var call = (MethodCallExpression) current;

								memberInfo = call.Method;
								arguments  = call.Arguments.ToArray();

								if (call.Method.IsStatic)
									current = call.Arguments.First();
								else
									current = call.Object;

								break;
							}
						default:
						{
							current = null;
							continue;
						}
					}

					var attributes = mapping.GetAttributes<ExtensionAttribute>(memberInfo.ReflectedTypeEx(), memberInfo,
						a => string.IsNullOrEmpty(a.Configuration) ? "___" : a.Configuration);
					if (attributes.Length == 0)
						attributes =
							mapping.GetAttributes<ExtensionAttribute>(memberInfo.ReflectedTypeEx(), memberInfo, a => a.Configuration);

					foreach (var attr in attributes)
					{
						var param = attr.BuildExtensionParam(mapping, query, memberInfo, arguments, convertHelper);
						chains.Add(param);
					}

				}

				return chains;
			}

			public static string ResolveExpressionValues([NotNull] string expression, [NotNull] Func<string, string, string> valueProvider)
			{
				if (expression    == null) throw new ArgumentNullException(nameof(expression));
				if (valueProvider == null) throw new ArgumentNullException(nameof(valueProvider));

				const string pattern = @"{([0-9a-z_A-Z?]*)(,\s'(.*)')?}";

				int  prevMatch         = -1;
				int  prevNotEmptyMatch = -1;
				bool spaceNeeded       = false;

				var str = Regex.Replace(expression, pattern, match =>
				{
					var paramName = match.Groups[1].Value;
					var canBeOptional = paramName.EndsWith("?");
					if (canBeOptional)
						paramName = paramName.TrimEnd('?');

					if (paramName == "_")
					{
						spaceNeeded = true;
						prevMatch = match.Index + match.Length;
						return string.Empty;
					}

					var delimiter  = match.Groups[3].Success ? match.Groups[3].Value : null;
					var calculated = valueProvider(paramName, delimiter);

					if (string.IsNullOrEmpty(calculated) && !canBeOptional)
						throw new InvalidOperationException(string.Format("Non optional parameter '{0}' not found", paramName));

					var res = calculated;
					if (spaceNeeded)
					{
						if (!string.IsNullOrEmpty(calculated))
						{
							var e = expression;
							if (prevMatch == match.Index && prevNotEmptyMatch == match.Index - 3 || (prevNotEmptyMatch >= 0 && e[prevNotEmptyMatch] != ' '))
								res = " " + calculated;
						}
						spaceNeeded = false;
					}

					if (!string.IsNullOrEmpty(calculated))
					{
						prevNotEmptyMatch = match.Index + match.Length;
					}

					return res;
				});

				return str;
			}

			SqlExtensionParam BuildExtensionParam(MappingSchema mapping, SelectQuery query, MemberInfo member, Expression[] arguments, ConvertHelper convertHelper)
			{
				var method = member as MethodInfo;
				var type   = member.GetMemberType();
				if (method != null)
					type = method.ReturnType ?? type;
				else if (member is PropertyInfo)
					type = ((PropertyInfo)member).PropertyType;

				var extension = new SqlExtension(type, Expression, Precedence, ChainPrecedence, IsAggregate);

				SqlExtensionParam result = null;

				if (method != null)
				{
					var parameters = method.GetParameters();

					for (var i = 0; i < parameters.Length; i++)
					{
						var arg   = arguments[i];
						var param = parameters[i];
						var names = param.GetCustomAttributes(true).OfType<ExprParameterAttribute>()
							.Select(a => a.Name ?? param.Name)
							.Distinct()
							.ToArray();

						if (names.Length > 0)
						{
							var sqlExpressions = arg is NewArrayExpression arrayInit
								? arrayInit.Expressions.Select(convertHelper.Convert).ToArray()
								: new[] {convertHelper.Convert(arg)};

							foreach (var name in names)
							{
								foreach (var sqlExpr in sqlExpressions)
								{
									extension.AddParameter(name, sqlExpr);
								}
							}
						}
					}
				}

				if (BuilderType != null)
				{
					var callBuilder = _builders.GetOrAdd(BuilderType, t =>
						{
							if (Activator.CreateInstance(BuilderType) is IExtensionCallBuilder res)
								return res;

							throw new ArgumentException(
								$"Type '{BuilderType}' does not implement {nameof(IExtensionCallBuilder)} interface.");
						}
					);

					var builder = new ExtensionBuilder(Configuration, mapping, query, extension, convertHelper, member, arguments);
					callBuilder.Build(builder);

					result = builder.ResultExpression != null ? new SqlExtensionParam(TokenName, builder.ResultExpression) : new SqlExtensionParam(TokenName, builder.Extension);
				}

				result = result ?? new SqlExtensionParam(TokenName, extension);

				return result;
			}

			static IEnumerable<Expression> ExtractArray(Expression expression)
			{
				var array = (NewArrayExpression) expression;
				return array.Expressions;
			}

			protected class ConvertHelper
			{
				readonly Func<Expression, ISqlExpression> _converter;

				public ConvertHelper([NotNull] Func<Expression, ISqlExpression> converter)
				{
					_converter = converter ?? throw new ArgumentNullException(nameof(converter));
				}

				public ISqlExpression Convert(Expression exp)
				{
					return _converter(exp);
				}
			}

			public static SqlExpression BuildSqlExpression(SqlExtension root, Type systemType, int precedence, bool isAggregate)
			{
				var sb             = new StringBuilder();
				var resolvedParams = new Dictionary<SqlExtensionParam, string>();
				var resolving      = new HashSet<SqlExtensionParam>();
				var newParams      = new List<ISqlExpression>();

				Func<string, string, string> valueProvider = null;
				Stack<SqlExtension> current                = new Stack<SqlExtension>();

				valueProvider = (name, delimiter) =>
				{
					var found = root.GetParametersByName(name);
					if (current.Count != 0)
						found = current.Peek().GetParametersByName(name).Concat(found);
					string result = null;
					foreach (var p in found)
					{
						if (resolvedParams.TryGetValue(p, out var paramValue))
						{
							result = paramValue;
						}
						else
						{
							if (resolving.Contains(p))
								throw new InvalidOperationException("Circular reference");

							resolving.Add(p);
							var ext = p.Extension;
							if (ext != null)
							{
								current.Push(ext);
								paramValue = ResolveExpressionValues(ext.Expr, valueProvider);
								current.Pop();
							}
							else
							{
								sb.Length = 0;
								if (p.Expression != null)
								{
									paramValue = string.Format("{{{0}}}", newParams.Count);
									newParams.Add(p.Expression);
								}
							}

							resolvedParams.Add(p, paramValue);

							if (string.IsNullOrEmpty(paramValue))
								continue;

							if (!string.IsNullOrEmpty(result))
								result = result + delimiter;
							result = result + paramValue;
						}

						if (delimiter == null && !string.IsNullOrEmpty(result))
							break;
					}

					return result;
				};

				var expr          = ResolveExpressionValues(root.Expr, valueProvider);
				var sqlExpression = new SqlExpression(systemType, expr, precedence, isAggregate, newParams.ToArray());

				return sqlExpression;
			}

			public override ISqlExpression GetExpression(MappingSchema mapping, SelectQuery query, Expression expression, Func<Expression, ISqlExpression> converter)
			{
				var helper = new ConvertHelper(converter);

				// chain starts from the tail
				var chain  = BuildFunctionsChain(mapping, query, expression, helper);

				if (chain.Count == 0)
					throw new InvalidOperationException("No sequnce found");

				var ordered = chain.Where(c => c.Extension != null).OrderByDescending(c => c.Extension.ChainPrecedence).ToArray();
				var main    = ordered.FirstOrDefault();
				if (main == null)
				{
					var replaced = chain.Where(c => c.Expression != null).ToArray();
					if (replaced.Length != 1)
						throw new InvalidOperationException("Can not find root sequence");

					return replaced[0].Expression;
				}

				var mainExtension = main.Extension;

				// suggesting type
				var type = chain
					.Where(c => c.Extension != null)
					.Select(c => c.Extension.SystemType)
					.FirstOrDefault(t => t != null && mapping.IsScalarType(t));

				if (type == null)
					type = chain
						.Where(c => c.Expression != null)
						.Select(c => c.Expression.SystemType)
						.FirstOrDefault(t => t != null && mapping.IsScalarType(t));

				mainExtension.SystemType = type ?? expression.Type;

				// calculating that extension is aggregate
				var isAggregate = ordered.Any(c => c.Extension.IsAggregate);

				// add parameters in reverse order
				for (int i = chain.Count - 1; i >= 0; i--)
				{
					var c = chain[i];
					if (c.Extension == mainExtension)
						continue;
					mainExtension.AddParameter(c);
				}

				//TODO: Precedence calculation
				var res = BuildSqlExpression(mainExtension, mainExtension.SystemType, mainExtension.Precedence, isAggregate);

				return res;
			}
		}

	}
}