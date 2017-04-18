using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using LinqToDB.Mapping;

namespace LinqToDB
{
	using Expressions;

	using Extensions;

	using JetBrains.Annotations;

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

		public static Sql.SqlExtensionParam AddEpression(this Sql.ISqExtensionBuilder builder, string name, string expr)
		{
			return builder.AddParameter(name, new SqlExpression(expr, Precedence.Primary));
		}
	}

	public partial class Sql
	{
		public interface ISqlExtension
		{
			
		}

		public static ISqlExtension Ext
		{
			get
			{
				return null;
			}
		}

		public interface ISqExtensionBuilder
		{
			string         Configuration    { get; }
			MappingSchema  Mapping          { get; }
			SqlExtension   Extension        { get; }
			ISqlExpression ResultExpression { get; set; }
			string         Expression       { get; set; }

			T GetValue<T>(int index);
			T GetValue<T>(string argName);
			ISqlExpression GetExpression(int index);
			ISqlExpression GetExpression(string argName);

			SqlExtensionParam AddParameter(string name, ISqlExpression expr);
		}

		public class SqlExtension
		{
			public Dictionary<string, List<SqlExtensionParam>> NamedParameters
			{
				get { return _namedParameters; }
			}

			readonly Dictionary<string, List<SqlExtensionParam>> _namedParameters;
			public int ChainPrecedence { get; set; }

			public SqlExtension(Type systemType, string expr, int precedence, int chainPrecedence, params SqlExtensionParam[] parameters)
			{
				if (parameters == null) throw new ArgumentNullException("parameters");

				foreach (var value in parameters)
					if (value == null) throw new ArgumentNullException("parameters");

				SystemType       = systemType;
				Expr             = expr;
				Precedence       = precedence;
				ChainPrecedence  = chainPrecedence;
				_namedParameters = parameters.ToLookup(p => p.Name).ToDictionary(p => p.Key, p => p.ToList());
			}

			public SqlExtension(string expr, params SqlExtensionParam[] parameters)
				: this(null, expr, SqlQuery.Precedence.Unknown, 0, parameters)
			{
			}

			public Type    SystemType { get;         set; }
			public string  Expr       { get;         set; }
			public int     Precedence { get; private set; }

			public SqlExtensionParam AddParameter(string name, ISqlExpression sqlExpression)
			{
				return AddParameter(new SqlExtensionParam(name ?? string.Empty, sqlExpression));
			}

			public SqlExtensionParam AddParameter(SqlExtensionParam param)
			{
				List<SqlExtensionParam> list;
				if (!_namedParameters.TryGetValue(param.Name, out list))
				{
					list = new List<SqlExtensionParam>();
					_namedParameters.Add(param.Name, list);
				}
				list.Add(param);
				return param;
			}

			public IEnumerable<SqlExtensionParam> GetParametersByName(string name)
			{
				List<SqlExtensionParam> list;
				if (_namedParameters.TryGetValue(name, out list))
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

			public string Name { get; set; }
			public SqlExtension   Extension { get; set; }
			public ISqlExpression Expression { get; set; }
		}

		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
		public class ExtensionAttribute : ExpressionAttribute
		{
			public string TokenName { get; set; }

			protected class ExtensionBuilder: ISqExtensionBuilder
			{
				readonly ConvertHelper _convert;

				public ExtensionBuilder(
					string                     configuration,
					[NotNull]   MappingSchema  mapping,
					[NotNull]   SqlExtension   extension, 
					[NotNull]   ConvertHelper  convertHeper,
					[CanBeNull] MethodInfo     method,
					[NotNull]   Expression[]   arguments)
				{
					if (mapping      == null) throw new ArgumentNullException("mapping");
					if (extension    == null) throw new ArgumentNullException("extension");
					if (convertHeper == null) throw new ArgumentNullException("convertHeper");
					if (arguments    == null) throw new ArgumentNullException("arguments");

					Mapping       = mapping;
					Configuration = configuration;
					Extension     = extension;
					_convert      = convertHeper;
					Method        = method;
					Arguments     = arguments;
				}

				public ISqlExpression ConvertExpression(Expression expr)
				{
					return _convert.Convert(expr);
				}

				public MethodInfo     Method           { get; private set; }
				public Expression[]   Arguments        { get; private set; }

				#region ISqExtensionBuilder Members

				public string         Configuration    { get; private set; }
				public MappingSchema  Mapping          { get; private set; }
				public SqlExtension   Extension        { get; private set; }
				public ISqlExpression ResultExpression { get;         set; }

				public string Expression
				{
					get { return Extension.Expr;  }
					set { Extension.Expr = value; }
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

				public SqlExtensionParam AddParameter(string name, ISqlExpression expr)
				{
					return Extension.AddParameter(name, expr);;
				}

				#endregion

			}

			public string BuilderMethod   { get; set; }
			public Type   BuilderType     { get; set; }
			public int    ChainPrecedence { get; set; }

			public ExtensionAttribute(string expression): this(expression, string.Empty)
			{
			}

			public ExtensionAttribute(string expression, string names): this(string.Empty, expression, names)
			{
			}

			public ExtensionAttribute(string configuration, string expression, string tokenName): base(configuration, expression)
			{
				TokenName        = tokenName;

				ServerSideOnly   = true;
				PreferServerSide = true;
				ExpectExpression = true;
				ServerSideOnly   = true;
				PreferServerSide = true;
			}

			static T GetExpressionValue<T>(Expression expr)
			{
				var lambda = System.Linq.Expressions.Expression.Lambda<Func<T>>(expr);
				return lambda.Compile()();
			}

			protected List<SqlExtensionParam> BuildFunctionsChain(MappingSchema mapping, Expression expr, ConvertHelper convertHelper)
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
							throw new InvalidOperationException(string.Format("Invalid method chain for Extension ({0}) -> {1}", expr, current));

					}

					var attributes = mapping.GetAttributes<ExtensionAttribute>(memberInfo.ReflectedTypeEx(), memberInfo,
						a => a.Configuration);
					foreach (var attr in attributes)
					{
						var chainExtension = attr.BuildExtensions(mapping, memberInfo, arguments, convertHelper);
						chains.AddRange(chainExtension.Reverse());
					}

				}

				chains.Reverse();
				return chains;
			}

			public static string ResolveExpressionValues([NotNull] string expression, [NotNull] Func<string, string, string> valueProvider)
			{
				if (expression == null) throw new ArgumentNullException("expression");
				if (valueProvider == null) throw new ArgumentNullException("valueProvider");

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

			IEnumerable<SqlExtensionParam> BuildExtensions(MappingSchema mapping, MemberInfo member, Expression[] arguments, ConvertHelper convertHelper)
			{
				var extension = new SqlExtension(member.GetMemberType(), Expression, Precedence, ChainPrecedence);
				SqlExtensionParam result = null;

				var method = member as MethodInfo;
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
							var arrayInit = arg as NewArrayExpression;
							var sqlExpressions = arrayInit != null
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
						else
						{
							//TODO: support of aggreagate functions
//							if (typeof(IEnumerable<>).IsSameOrParentOf(param.ParameterType))
//							{
//								var zz = convertHelper.Convert(arg);
//								convertHelper.AddTypeMapping(param.ParameterType.GetGenericArguments().First(), arg);
//								var generic = param.ParameterType.GetGenericArguments().First();
//							}
						}
					}
				}

				var builderType = BuilderType ?? member.DeclaringType;
				if (!string.IsNullOrEmpty(BuilderMethod) && builderType != null)
				{
					var builderMethod = builderType.GetMethodEx(BuilderMethod, typeof(ISqExtensionBuilder));

					if (builderMethod == null || !builderMethod.IsStatic)
					{
						throw new InvalidOperationException(string.Format(
							"Builder method 'public static void {2}.{0}({1})' for class '{2}' not found", BuilderMethod,
							typeof(ISqExtensionBuilder).Name,
							builderType.Name));
					}

					var builder = new ExtensionBuilder(Configuration, mapping, extension, convertHelper, method, arguments);
					builderMethod.Invoke(null, new object[] {builder});

					result = builder.ResultExpression != null ? new SqlExtensionParam(TokenName, builder.ResultExpression) : new SqlExtensionParam(TokenName, builder.Extension);
				}

				result = result ?? new SqlExtensionParam(TokenName, extension);

				return new[] {result};
			}

			static IEnumerable<Expression> ExtractArray(Expression expression)
			{
				var array = (NewArrayExpression) expression;
				return array.Expressions;
			}

			protected class ConvertHelper
			{
				readonly Func<Expression, ISqlExpression> _converter;
				readonly Dictionary<Type, Expression>     _typeMapping = new Dictionary<Type, Expression>();

				public ConvertHelper([NotNull] Func<Expression, ISqlExpression> converter)
				{
					if (converter == null) throw new ArgumentNullException("converter");
					_converter = converter;
				}

				public ISqlExpression Convert(Expression exp)
				{
					var current = exp.Unwrap();
					if (current.NodeType == ExpressionType.Lambda)
					{
						var toReplace = new Dictionary<Expression, Expression>();
						current.Visit(e =>
							{
								var param = e as ParameterExpression;
								if (param != null)
								{
									Expression value;
									if (_typeMapping.TryGetValue(param.Type, out value))
									{
										toReplace[param] = value;
									}
								}
							}
						);

						if (toReplace.Count > 0)
						{
							current = ((LambdaExpression) current).Body.Transform(e =>
							{
								Expression value;
								if (toReplace.TryGetValue(e, out value))
									return value;
								return e;
							});
						}

						//TODO: Aggregate functions support
//						current = ((LambdaExpression) current).Body;
					}
					return _converter(current);
				}

				public void AddTypeMapping(Type type, Expression expr)
				{
					_typeMapping[type] = expr;	
				}
			}

			protected static SqlExpression BuildSqlExpression(SqlExtension root, Type systemType, int precedence)
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
						string paramValue;
						if (!resolvedParams.TryGetValue(p, out paramValue))
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
								sb.Length  = 0;
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

				var expr = ResolveExpressionValues(root.Expr, valueProvider);

				return new SqlExpression(systemType, expr, precedence, newParams.ToArray());
			}

			public override ISqlExpression GetExpression(MappingSchema mapping, Expression expression, Func<Expression, ISqlExpression> converter)
			{
				var helper = new ConvertHelper(converter);
				var chain  = BuildFunctionsChain(mapping, expression, helper);

				if (chain.Count == 0)
					throw new InvalidOperationException("No sequnce found");

				var main = chain.Where(c => c.Extension != null).OrderByDescending(c => c.Extension.ChainPrecedence).FirstOrDefault();
				if (main == null)
					throw new InvalidOperationException("Can not find root sequence");

				var mainExtension = main.Extension;

				mainExtension.SystemType = expression.Type;
				foreach (var c in chain.Where(c => c.Extension != mainExtension))
				{
					mainExtension.AddParameter(c);
				}

				//TODO: SytemType and precedence calculation
				var res = BuildSqlExpression(mainExtension, expression.Type, mainExtension.Precedence);

				return res;
			}
		}

	}
}