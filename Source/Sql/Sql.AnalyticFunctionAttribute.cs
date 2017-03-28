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
		public class AnalyticFunctionAttribute : ExpressionAttribute
		{
			class FunctionBuilder: IReadyToFunction, IAnalyticFunctionBuilder
			{
				public FunctionBuilder(
					string                                     configuration,
					[NotNull] SqlAnalyticFunction              function, 
					[NotNull] Func<Expression, ISqlExpression> convert,
					[NotNull] Expression[]                     arguments)
				{
					if (function  == null) throw new ArgumentNullException("function");
					if (convert   == null) throw new ArgumentNullException("convert");
					if (arguments == null) throw new ArgumentNullException("arguments");

					Function  = function;
					Convert   = convert;
					Arguments = arguments;
				}

				public Func<Expression, ISqlExpression> Convert   { get; private set; }
				public Expression[]                     Arguments { get; private set; }

				#region IAnalyticFunctionBuilder Members

				public ISqlExpression ConvertExpression(Expression expr)
				{
					return Convert(expr);
				}

				public string Configuration { get; private set; }

				public ISqlExpression GetArgument(int index)
				{
					return ConvertExpression(Arguments[index]);
				}

				public ISqlExpression[] GetArrayArgument(int index)
				{
					var array = (NewArrayExpression) Arguments[index];
					return array.Expressions.Select(ConvertExpression).ToArray();
				}

				public T GetValue<T>(int index)
				{
					var lambda = System.Linq.Expressions.Expression.Lambda<Func<T>>(Arguments[index]);
					return lambda.Compile()();
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

			public AnalyticFunctionAttribute(string expression): this(string.Empty, expression)
			{
			}

			public AnalyticFunctionAttribute(string configuration, string expression): base(configuration, expression)
			{
				ServerSideOnly   = true;
				PreferServerSide = true;
				ExpectExpression = true;
				HasPureConvertor = true;
				ServerSideOnly   = true;
				PreferServerSide = true;
			}

			static T GetExpressionValue<T>(Expression expr)
			{
				var lambda = System.Linq.Expressions.Expression.Lambda<Func<T>>(expr);
				return lambda.Compile()();
			}

			protected SqlAnalyticFunction.AnalyticClause BuildAnalyticClause(Expression expr, Func<Expression, ISqlExpression> converter)
			{
				var analytic = new SqlAnalyticFunction.AnalyticClause();
				var chains   = new List<FunctionChain>();
				var current  = expr;

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

				for (var i = chains.Count - 1; i >= 0; i--)
				{
					var chain = chains[i];

					if (chain.Name == "Over")
					{
						continue;
					}

					var parentType = chain.Method != null ? chain.Method.ReturnType : chain.Member.DeclaringType;

					if (parentType == null)
						throw new InvalidOperationException();

					if (typeof(IPartitionNotRanged).IsSameOrParentOf(parentType))
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

					if (typeof(IPartitionNotOrdered).IsSameOrParentOf(parentType))
					{
						switch (chain.Name)
						{
							case "PartitionBy" :
								{
									chain.EnsureMethod();
									analytic.QueryPartition =
										new SqlAnalyticFunction.QueryPartitionClause(
											ConvertBodies(ExtractArray(chain.Arguments[0]), converter).ToArray());
									continue;
								}
						}
					}

					if (typeof(IPartitionNotOrdered).IsSameOrParentOf(parentType) ||
						typeof(IPartitionOrdered).IsSameOrParentOf(parentType))
					{
						switch (chain.Name)
						{
							case "OrderBy"     :
							case "OrderByDesc" :
							case "ThenBy"      :
							case "ThenByDesc"  :
								{
									chain.EnsureMethod();

									var fields = ConvertBodies(chain.Arguments, converter);
									var isDescending = chain.Name.Contains("Desc");

									var nulls = NullsPosition.None;

									if (chain.Arguments.Length > 1 && chain.Arguments[1].Type == typeof(NullsPosition))
									{
										nulls = GetExpressionValue<NullsPosition>(chain.Arguments[1]);
									}

									analytic.OrderBy = analytic.OrderBy ?? new SqlAnalyticFunction.OrderByClause();
									analytic.OrderBy.Items.AddRange(fields.Select(f => new SqlAnalyticFunction.OrderByItem(f, isDescending, nulls)));


									continue;
								}
						}
					}

					if (typeof(IWindowFrameExtent).IsSameOrParentOf(parentType))
					{
						analytic.Windowing = analytic.Windowing ?? new SqlAnalyticFunction.WindowingClause();
						switch (chain.Name)
						{
							case "UnboundedPreceding" :
								{
									chain.EnsureMember();
									analytic.Windowing.Start = analytic.Windowing.Start ?? new SqlAnalyticFunction.WindowFrameBound();
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

					if (typeof(IWindowFrameBetween).IsSameOrParentOf(parentType))
					{
						analytic.Windowing = analytic.Windowing ?? new SqlAnalyticFunction.WindowingClause();
						switch (chain.Name)
						{
							case "UnboundedPreceding" :
								{
									chain.EnsureMember();
									analytic.Windowing.Start = analytic.Windowing.Start ?? new SqlAnalyticFunction.WindowFrameBound();
									analytic.Windowing.Start.Kind = SqlAnalyticFunction.LimitExpressionKind.UnboundedPreceding;
									continue;
								}
							case "Value" :
								{
									chain.EnsureMethod();
									analytic.Windowing.Start = analytic.Windowing.Start ?? new SqlAnalyticFunction.WindowFrameBound();
									analytic.Windowing.Start.Kind = SqlAnalyticFunction.LimitExpressionKind.ValueExprPreceding;
									analytic.Windowing.Start.ValueExpression = ConvertBodies(chain.Arguments, converter).First();
									continue;
								}
						}
					}

					if (typeof(IWindowFrameBetweenNext).IsSameOrParentOf(parentType))
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

					if (typeof(IWindowFrameFollowing).IsSameOrParentOf(parentType))
					{
						analytic.Windowing = analytic.Windowing ?? new SqlAnalyticFunction.WindowingClause();
						switch (chain.Name)
						{
							case "UnboundedFollowing" :
								{
									chain.EnsureMember();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.WindowFrameBound();
									analytic.Windowing.End.Kind = SqlAnalyticFunction.LimitExpressionKind.UnboundedFollowing;
									continue;
								}
							case "CurrentRow" :
								{
									chain.EnsureMember();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.WindowFrameBound();
									analytic.Windowing.End.Kind = SqlAnalyticFunction.LimitExpressionKind.CurrentRow;
									continue;
								}
							case "Value" :
								{
									chain.EnsureMethod();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.WindowFrameBound();
									analytic.Windowing.End.Kind = SqlAnalyticFunction.LimitExpressionKind.ValueExprFollowing;
									analytic.Windowing.End.ValueExpression = ConvertBodies(chain.Arguments, converter).First();
									continue;
								}
						}
					}

					if (typeof(IValueExprFirst).IsSameOrParentOf(parentType))
					{
						analytic.Windowing = analytic.Windowing ?? new SqlAnalyticFunction.WindowingClause();
						switch (chain.Name)
						{
							case "Preceding" :
								{
									chain.EnsureMember();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.WindowFrameBound();
									analytic.Windowing.End.Kind = SqlAnalyticFunction.LimitExpressionKind.UnboundedPreceding;
									continue;
								}
							case "Following" :
								{
									chain.EnsureMember();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.WindowFrameBound();
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

					if (typeof(IValueExprSecond).IsSameOrParentOf(parentType))
					{
						analytic.Windowing = analytic.Windowing ?? new SqlAnalyticFunction.WindowingClause();
						switch (chain.Name)
						{
							case "Preceding" :
								{
									chain.EnsureMember();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.WindowFrameBound();
									analytic.Windowing.End.Kind = SqlAnalyticFunction.LimitExpressionKind.UnboundedPreceding;
									continue;
								}
							case "Following" :
								{
									chain.EnsureMember();
									analytic.Windowing.End = analytic.Windowing.End ?? new SqlAnalyticFunction.WindowFrameBound();
									analytic.Windowing.End.Kind = SqlAnalyticFunction.LimitExpressionKind.UnboundedFollowing;
									continue;
								}
						}
					}

					throw new InvalidOperationException(string.Format("Invalid method chain for Analytic function: {0}", chain.Name));
				}

				return analytic;
			}

			private static IEnumerable<Expression> ExtractArray(Expression expression)
			{
				var array = (NewArrayExpression) expression;
				return array.Expressions;
			}

			private static IEnumerable<ISqlExpression> ConvertBodies(IEnumerable<Expression> expressions, Func<Expression, ISqlExpression> converter)
			{
				var bodies = expressions
					.Select(e => e.Unwrap())
					.Select(converter);
				return bodies;
			}

			public override ISqlExpression GetExpression(MemberInfo member, Expression[] args, Func<Expression, ISqlExpression> converter)
			{
				var firstArg = args.First();
				var analytic = BuildAnalyticClause(firstArg, converter);

				var method = (MethodInfo) member;
				var func = new SqlAnalyticFunction(method.ReturnType, Expression, Precedence, analytic);

				if (args.Length > 1)
				{
					// run function in init mode

					var builder = new FunctionBuilder(Configuration, func, converter, args.Skip(1).ToArray());

					var paramValues = new List<object>();
					paramValues.Add(builder);

					paramValues.AddRange(method.GetParameters()
						.Skip(1)
						.Select(p => p.ParameterType.GetDefaultValue()));

					method.Invoke(builder, paramValues.ToArray());
				}

				return func;
			}
		}
	}
}