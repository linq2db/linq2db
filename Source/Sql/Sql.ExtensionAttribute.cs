using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB
{
	using Expressions;

	using Extensions;

	using JetBrains.Annotations;

	using SqlQuery;

	[AttributeUsage(AttributeTargets.Parameter)]
	//[MeansImplicitUse]
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
		public static SqlExtension.ExtensionParam AddParameter(this Sql.ISqExtensionBuilder builder, string name, string value)
		{
			return builder.AddParameter(name, new SqlValue(value));
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
			string		   Configuration    { get; }
			SqlExtension   Extension        { get; }
			ISqlExpression ResultExpression { get; set; }
			string         Expression       { get; set; }

			T GetValue<T>(int index);
			T GetValue<T>(string argName);

			SqlExtension.ExtensionParam AddParameter(string name, ISqlExpression expr);
		}

		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
		public class ExtensionAttribute : ExpressionAttribute
		{
			/// <summary>
			/// Names separated by comma
			/// </summary>
			public string Names { get; set; }

			protected class ExtensionBuilder: ISqlExtension, ISqExtensionBuilder
			{
				readonly ConvertHelper _convert;

				public ExtensionBuilder(
					string                                       configuration,
					[NotNull]   SqlExtension                     extension, 
					[NotNull]   ConvertHelper                    convertHeper,
					[CanBeNull] MethodInfo                       method,
					[NotNull]   Expression[]                     arguments)
				{
					if (extension == null) throw new ArgumentNullException("extension");
					if (convertHeper   == null) throw new ArgumentNullException("convertHeper");
					if (arguments == null) throw new ArgumentNullException("arguments");

					Extension = extension;
					_convert   = convertHeper;
					Method    = method;
					Arguments = arguments;
				}

				public SqlExtension Extension { get; private set; }
				ISqlExpression ISqExtensionBuilder.ResultExpression
				{
					get { return ResultExpression; }
					set { ResultExpression = value; }
				}

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

				public SqlExtension.ExtensionParam AddParameter(string name, ISqlExpression expr)
				{
					return Extension.AddParameter(name, expr);;
				}

				public MethodInfo                       Method    { get; private set; }
				public Expression[]                     Arguments { get; private set; }

				#region IAnalyticFunctionBuilder Members

				public ISqlExpression ConvertExpression(Expression expr)
				{
					return _convert.Convert(expr);
				}

				public string Configuration { get; private set; }
				public ISqlExpression ResultExpression { get; private set; }

				#endregion
			}

			public class FunctionChain
			{
				public FunctionChain([NotNull] MemberInfo member)
				{
					if (member == null) throw new ArgumentNullException("member");
					Member = member;
				}

				public FunctionChain([NotNull] MethodInfo method, [NotNull] Expression[] arguments)
				{
					if (method    == null) throw new ArgumentNullException("method");
					if (arguments == null) throw new ArgumentNullException("arguments");

					Method    = method;
					Arguments = arguments;
				}

				public string       Name      { get { return Method == null ? Member.Name : Method.Name; } }
				public Expression[] Arguments { get; private set; }
				public MemberInfo   Member    { get; private set; }
				public MethodInfo   Method    { get; private set; }

				public MethodInfo EnsureMethod()
				{
					if (Method == null)
						throw new InvalidOperationException(string.Format("'{0}' must be a mathod", Name));
					return Method;
				}

				public MemberInfo EnsureMember()
				{
					if (Member == null)
						throw new InvalidOperationException(string.Format("'{0}' must be a member", Name));
					return Member;
				}

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

			public ExtensionAttribute(string configuration, string expression, string names): base(configuration, expression)
			{
				Names = names;

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

			protected List<SqlExtension.ExtensionParam> BuildFunctionsChain(Expression expr, ConvertHelper convertHelper)
			{
				var chains   = new List<SqlExtension.ExtensionParam>();
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

					var attributes = memberInfo.GetCustomAttributes(true).OfType<ExtensionAttribute>();
					foreach (var attr in attributes)
					{
						var chainExtension = attr.BuildExtensions(memberInfo, arguments, convertHelper);
						if (expr != null)
						{
							//TODO: check order
							chains.AddRange(chainExtension);
						}
					}

				}

				chains.Reverse();
				return chains;
			}

			IEnumerable<SqlExtension.ExtensionParam> BuildExtensions(MemberInfo member, Expression[] arguments, ConvertHelper convertHelper)
			{
				var extension = new SqlExtension(member.GetMemberType(), Expression, Precedence, ChainPrecedence);
				ISqlExpression result = null;

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
					var builderMethod = builderType.GetMethod(BuilderMethod,
						BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

					if (builderMethod == null ||
					    !builderMethod.IsStatic ||
					    builderMethod.GetParameters().Length != 1 ||
					    builderMethod.GetParameters()[0].ParameterType != typeof(ISqExtensionBuilder))
					{
						throw new InvalidOperationException(string.Format(
							"Builder method 'static void {0}({1})' for class '{2}' not found", BuilderMethod,
							typeof(ISqExtensionBuilder).Name,
							builderType.Name));
					}

					var builder = new ExtensionBuilder(Configuration, extension, convertHelper, method, arguments);
					builderMethod.Invoke(null, new object[] {builder});

					result = builder.Extension ?? builder.ResultExpression;
				}

				if (!(result is SqlExtension) && Names == null)
					throw new InvalidOperationException("Relpaced expressions should have name");

				result = result ?? extension;

				var extNames = Names == null ? new string[] {""} : Names.Split(',');

				return extNames.Select(n => new SqlExtension.ExtensionParam(n, result));
			}

			static IEnumerable<Expression> ExtractArray(Expression expression)
			{
				var array = (NewArrayExpression) expression;
				return array.Expressions;
			}

			protected class ConvertHelper
			{
				readonly Func<Expression, ISqlExpression> _converter;
				readonly Dictionary<Type, Expression> _typeMapping = new Dictionary<Type, Expression>();

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

//						current = ((LambdaExpression) current).Body;
					}
					return _converter(current);
				}

				public void AddTypeMapping(Type type, Expression expr)
				{
					_typeMapping[type] = expr;	
				}
			}

			public override ISqlExpression GetExpression(Expression expression, Func<Expression, ISqlExpression> converter)
			{
				var helper = new ConvertHelper(converter);
				var chain = BuildFunctionsChain(expression, helper);

				if (chain.Count == 0)
					throw new InvalidOperationException("No sequnce found");

				var ordered = chain.OrderByDescending(c => c.Expression is SqlExtension ? ((SqlExtension) c.Expression).ChainPrecedence : -1).ToList();
				var main = ordered.FirstOrDefault(c => c.Expression is SqlExtension);
				if (main == null)
					throw new InvalidOperationException("Can not find root sequence");

				var mainExtension = (SqlExtension) main.Expression;
				mainExtension.SystemType = expression.Type;
				foreach (var c in chain.Where(c => c.Expression as SqlExtension != mainExtension))
				{
					mainExtension.AddParameter(c);
				}

				return mainExtension;
			}
		}

	}
}