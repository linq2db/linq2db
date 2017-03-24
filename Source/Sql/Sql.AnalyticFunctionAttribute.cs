using System;

namespace LinqToDB
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;

	using JetBrains.Annotations;

	using Expressions;
	using Extensions;
	using SqlQuery;

	public static partial class Sql
	{
		public class AnalyticFunctionAttribute : ChainCollector
		{
			private class FunctionBuilder<T>: IReadyToFunction<T>, IAnalyticFunctionBuilder
			{
				public Func<Expression, ISqlExpression> Convert { get; private set; }
				public Expression[] Arguments { get; private set; }

				public FunctionBuilder(
					[NotNull] SqlAnalyticFunction function, 
					[NotNull] Func<Expression, ISqlExpression> convert,
					[NotNull] Expression[] arguments)
				{
					if (function == null) throw new ArgumentNullException("function");
					if (convert == null) throw new ArgumentNullException("convert");
					if (arguments == null) throw new ArgumentNullException("arguments");
					Function = function;
					Convert = convert;
					Arguments = arguments;
				}

				#region Implementation of IAnalyticFunctionBuilder
				public ISqlExpression ConvertExpression(Expression expr)
				{
					return Convert(expr);
				}

				public ISqlExpression GetArgument(int index)
				{
					return ConvertExpression(Arguments[index]);
				}

				public T1 GetValue<T1>(Expression expr)
				{
					// should return compiled value
					throw new NotImplementedException();
				}

				public SqlAnalyticFunction Function { get; private set; }
				#endregion
			}

			public class FunctionChain
			{
				public FunctionChain([NotNull] MemberInfo member)
				{
					if (member == null) throw new ArgumentNullException("member");
					Member = member;
				}

				public FunctionChain([NotNull] MethodInfo method, [NotNull] Expression[] argumends)
				{
					if (method    == null) throw new ArgumentNullException("method");
					if (argumends == null) throw new ArgumentNullException("argumends");
					Method = method;
					Argumends = argumends;
				}

				public string Name { get { return Method == null ? Member.Name : Method.Name; } }

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

				public MemberInfo Member { get; private set; }
				public MethodInfo Method { get; private set; }

				public Expression[] Argumends { get; private set; }
			}

			public AnalyticFunctionAttribute(string functionName)
			{
				FunctionName     = functionName;
				ServerSideOnly   = true;
				PreferServerSide = true;
				ExpectExpression = true;
				HasPureConvertor = true;
			}

			private static Expression ReplaceExpression(Expression root, Expression from, Expression to)
			{
				return root.Transform(expr =>
				{
					if (expr == from)
						return to;

					return expr;
				});
			}

			protected SqlAnalyticFunction.AnalyticClause BuildAnalyticClause(Expression expr, Func<Expression, ISqlExpression> converter, out Expression entityParam)
			{
				var analytic     = new SqlAnalyticFunction.AnalyticClause();
				var chains  = new List<FunctionChain>();
				var current = expr;

				while (current != null)
				{
					switch (current.NodeType)
					{
						case ExpressionType.MemberAccess :
							{
								var member = (MemberExpression)current;
								current = member.Expression;
								chains.Add(new FunctionChain(member.Member));

								break;
							}
						case ExpressionType.Call :
							{
								var call = (MethodCallExpression) current;
								current = call.Object;
								chains.Add(new FunctionChain(call.Method, call.Arguments.ToArray()));
								break;
							}
						default:
							throw new InvalidOperationException(string.Format("Invalid method chain for Analytic function ({0}) -> {1}", expr, current));
					}
				}

				Expression tableExpression = null;
				ISqlExpression table = null;

				for (var i = chains.Count - 1; i >= 0; i--)
				{
					var chain = chains[i];

					if (chain.Method != null && chain.Method.IsStatic && chain.Method.Name == "Over")
					{
						tableExpression = chain.Argumends.Single();
						table = converter(tableExpression);
						continue;
					}

					if (table == null)
					{
						throw new InvalidOperationException("Invalid method chain for Analytic function");
					}

					var parentType = chain.Method != null ? chain.Method.ReturnType : chain.Member.DeclaringType;

					if (parentType == null || tableExpression == null)
						throw new InvalidOperationException();

					if (typeof(IPartitionNotRanged<>).IsSameOrParentOf(parentType))
					{
						switch (chain.Name)
						{
							case "Rows" :
							case "Range" :
								{
									var member = chain.EnsureMember();
									analytic.Windowing = analytic.Windowing ?? new SqlAnalyticFunction.WindowingClause();
									analytic.Windowing.BasedOn = member.Name == "Range"
										? SqlAnalyticFunction.BasedOn.Range
										: SqlAnalyticFunction.BasedOn.Rows;
									continue;
								}
						}
					}

					if (typeof(IPartitionNotOrdered<>).IsSameOrParentOf(parentType))
					{
						switch (chain.Name)
						{
							case "PartitionBy" :
								{
									chain.EnsureMethod();
									analytic.QueryPartition =
										new SqlAnalyticFunction.QueryPartitionClause(
											ConvertBodies(chain.Argumends, tableExpression, converter).ToArray());
									continue;
								}
						}
					}

					if (typeof(IPartitionNotOrdered<>).IsSameOrParentOf(parentType) ||
						typeof(IPartitionOrdered<>).IsSameOrParentOf(parentType))
					{
						switch (chain.Name)
						{
							case "OrderBy" :
							case "OrderByDesc" :
							case "ThenBy" :
							case "ThenByDesc" :
								{
									chain.EnsureMethod();

									var fields = ConvertBodies(chain.Argumends, tableExpression, converter);
									var isDescending = chain.Name.Contains("Desc");

									analytic.OrderBy = analytic.OrderBy ?? new SqlAnalyticFunction.OrderByClause();
									analytic.OrderBy.Items.AddRange(fields.Select(f => new SqlAnalyticFunction.OrderByItem(f, isDescending)));
									continue;
								}
						}
					}

					if (typeof(IWindowFrameExtent<>).IsSameOrParentOf(parentType))
					{
						analytic.Windowing = analytic.Windowing ?? new SqlAnalyticFunction.WindowingClause();
						switch (chain.Name)
						{
							case "UnboundedPreceding" :
								{
									chain.EnsureMember();
									analytic.Windowing.Start = analytic.Windowing.Start ?? new SqlAnalyticFunction.PointExpression();
									analytic.Windowing.Start.Kind = SqlAnalyticFunction.LimitExpressionKind.UnboundedPreceding;
									continue;
								}
							case "Between" :
								{
									chain.EnsureMember();
									continue;
								}
						}
					}

					if (typeof(IWindowFrameBetween<>).IsSameOrParentOf(parentType))
					{
						analytic.Windowing = analytic.Windowing ?? new SqlAnalyticFunction.WindowingClause();
						switch (chain.Name)
						{
							case "UnboundedPreceding" :
								{
									chain.EnsureMember();
									analytic.Windowing.Start = analytic.Windowing.Start ?? new SqlAnalyticFunction.PointExpression();
									analytic.Windowing.Start.Kind = SqlAnalyticFunction.LimitExpressionKind.UnboundedPreceding;
									continue;
								}
							case "Value" :
								{
									chain.EnsureMethod();
									analytic.Windowing.Start = analytic.Windowing.Start ?? new SqlAnalyticFunction.PointExpression();
									analytic.Windowing.Start.Kind = SqlAnalyticFunction.LimitExpressionKind.ValueExprPreceding;
									analytic.Windowing.Start.ValueExpression = ConvertBodies(chain.Argumends, tableExpression, converter).First();
									continue;
								}
						}
					}

					if (typeof(IWindowFrameBetweenNext<>).IsSameOrParentOf(parentType))
					{
						analytic.Windowing = analytic.Windowing ?? new SqlAnalyticFunction.WindowingClause();
						switch (chain.Name)
						{
							case "And" :
								{
									chain.EnsureMember();
									continue;
								}
						}
					}

					if (typeof(IWindowFrameFollowing<>).IsSameOrParentOf(parentType))
					{
						analytic.Windowing = analytic.Windowing ?? new SqlAnalyticFunction.WindowingClause();
						switch (chain.Name)
						{
							case "UnboundedFollowing" :
								{
									chain.EnsureMember();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.PointExpression();
									analytic.Windowing.End.Kind = SqlAnalyticFunction.LimitExpressionKind.UnboundedFollowing;
									continue;
								}
							case "CurrentRow" :
								{
									chain.EnsureMember();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.PointExpression();
									analytic.Windowing.End.Kind = SqlAnalyticFunction.LimitExpressionKind.CurrentRow;
									continue;
								}
							case "Value" :
								{
									chain.EnsureMethod();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.PointExpression();
									analytic.Windowing.End.Kind = SqlAnalyticFunction.LimitExpressionKind.ValueExprFollowing;
									analytic.Windowing.End.ValueExpression = ConvertBodies(chain.Argumends, tableExpression, converter).First();
									continue;
								}
						}
					}

					if (typeof(IValueExprFirst<>).IsSameOrParentOf(parentType))
					{
						analytic.Windowing = analytic.Windowing ?? new SqlAnalyticFunction.WindowingClause();
						switch (chain.Name)
						{
							case "Preceding" :
								{
									chain.EnsureMember();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.PointExpression();
									analytic.Windowing.End.Kind = SqlAnalyticFunction.LimitExpressionKind.UnboundedPreceding;
									continue;
								}
							case "Following" :
								{
									chain.EnsureMember();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.PointExpression();
									analytic.Windowing.End.Kind = SqlAnalyticFunction.LimitExpressionKind.UnboundedFollowing;
									continue;
								}
							case "And" :
								{
									chain.EnsureMember();
									continue;
								}
						}
					}

					if (typeof(IValueExprSecond<>).IsSameOrParentOf(parentType))
					{
						analytic.Windowing = analytic.Windowing ?? new SqlAnalyticFunction.WindowingClause();
						switch (chain.Name)
						{
							case "Preceding" :
								{
									chain.EnsureMember();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.PointExpression();
									analytic.Windowing.End.Kind = SqlAnalyticFunction.LimitExpressionKind.UnboundedPreceding;
									continue;
								}
							case "Following" :
								{
									chain.EnsureMember();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.PointExpression();
									analytic.Windowing.End.Kind = SqlAnalyticFunction.LimitExpressionKind.UnboundedFollowing;
									continue;
								}
						}
					}

					throw new InvalidOperationException(string.Format("Invalid method chain for Analytic function: {0}", chain.Name));
				}

				entityParam = tableExpression;
				return analytic;
			}

			private static IEnumerable<ISqlExpression> ConvertBodies(IEnumerable<Expression> expressions, Expression tableExpression, Func<Expression, ISqlExpression> converter)
			{
				var bodies = expressions
					.Select(e => e.Unwrap())
					.OfType<LambdaExpression>()
					.Select(l => ReplaceExpression(l.Body, l.Parameters[0], tableExpression))
					.Select(converter);
				return bodies;
			}

			public override ISqlExpression GetExpression(MemberInfo member, Expression[] args, Func<Expression, ISqlExpression> converter)
			{
				var firstArg = args.First();
				Expression entityParam;
				var analytic = BuildAnalyticClause(firstArg, converter, out entityParam);

				var method = (MethodInfo) member;
				var func = new SqlAnalyticFunction(method.ReturnType, FunctionName, Precedence, analytic);

				if (args.Length > 1)
				{
					// run function in parsing mode

					Func<Expression, ISqlExpression> convertFunc = e =>
					{
						var lambda = e as LambdaExpression;
						if (lambda != null)
						{
							e = lambda.Body;
							foreach (var param in lambda.Parameters.Where(p => entityParam.Type == p.Type))
							{
								e = ReplaceExpression(e, param, entityParam);
							}
						}
						return converter(e);
					};

					var generic = typeof(FunctionBuilder<>).MakeGenericType(firstArg.Type.GenericTypeArguments.Single());
					var builder = Activator.CreateInstance(generic, func, convertFunc, args.Skip(1).ToArray());

					var paramValues = new List<object>();
					paramValues.Add(builder);
					for (int i = 1; i < args.Length; i++)
					{
						var arg = args[i];
						object argValue;
						if (typeof(Expression).IsSameOrParentOf(arg.Type))
						{
							argValue = arg.Unwrap();
						}
						else
						{
							var lambda = System.Linq.Expressions.Expression.Lambda(arg);
							var v = lambda.Compile();
							argValue = v.DynamicInvoke();
						}
						paramValues.Add(argValue);
					}

//					paramValues.AddRange(method.GetParameters()
//						.Skip(1)
//						.Select(p => p.ParameterType.GetDefaultValue()));

					method.Invoke(builder, paramValues.ToArray());
				}

				return func;
			}

			public string FunctionName { get; private set; }
		}
	}
}