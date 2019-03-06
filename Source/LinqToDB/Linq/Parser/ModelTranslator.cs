using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Linq.Parser.Builders;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq.Parser.Clauses;
using LinqToDB.Mapping;

namespace LinqToDB.Linq.Parser
{
	public class ModelTranslator
	{
		private static readonly BaseBuilder[] _builders = 
		{
			new WhereMethodBuilder(),
			new AnyBuilder(), 
			new ArrayBuilder(), 
			new SelectBuilder(),
			new SelectManyBuilder(),
			new ConstantQueryBuilder(),
			new GetTableBuilder(),
			new UnionBuilder(),
			new JoinBuilder(), 
			new TakeBuilder(), 
			new SkipBuilder(), 
		};

		private static readonly Dictionary<MethodInfo, MethodCallBuilder[]> _methodCallBuilders;
		private static readonly BaseBuilder[] _otherBuilders;

		static ModelTranslator()
		{
			_methodCallBuilders = _builders
				.OfType<MethodCallBuilder>()
				.SelectMany(b => b.SupportedMethods(), (b, mi) => new { b = b, mi })
				.ToLookup(p => p.mi, p => p.b)
				.ToDictionary(l => l.Key, l => l.ToArray());

			_otherBuilders = _builders
				.Where(b => !(b is MethodCallBuilder))
				.ToArray();
		}

		public MappingSchema MappingSchema { get; }
		public ParameterExpression DataContextParam { get; }

		public ModelTranslator(
			[JetBrains.Annotations.NotNull] MappingSchema mappingSchema,
			[JetBrains.Annotations.NotNull] ParameterExpression dataContextParam)
		{
			MappingSchema = mappingSchema ?? throw new ArgumentNullException(nameof(mappingSchema));
			DataContextParam = dataContextParam ?? throw new ArgumentNullException(nameof(dataContextParam));
		}

		private Dictionary<ParameterExpression, QuerySourceReferenceExpression> _registeredSources = new Dictionary<ParameterExpression, QuerySourceReferenceExpression>();
		private readonly Dictionary<IQuerySource, QuerySourceReferenceExpression> _registeredSources2 = new Dictionary<IQuerySource, QuerySourceReferenceExpression>();


		private static BaseBuilder FindBuilder(Expression expression)
		{
			if (expression.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expression;

				if (_methodCallBuilders.TryGetValue(mc.Method.EnsureDefinition(), out var builders))
				{
					foreach (var builder in builders)
					{
						if (builder.CanBuild(expression))
							return builder;
					}

				}
			}
			else
			{
				foreach (var builder in _otherBuilders)
				{
					if (builder.CanBuild(expression))
						return builder;
				}
			}

			return null;
		}

		public Sequence BuildSequence(ParseBuildInfo parseBuildInfo, Expression expression)
		{
			var builder = FindBuilder(expression);
			if (builder != null)
				return builder.BuildSequence(this, parseBuildInfo, expression);

			throw new NotImplementedException();
		}

		LambdaExpression ConvertMethodExpression(Type type, MemberInfo mi)
		{
			var attr = MappingSchema.GetAttribute<ExpressionMethodAttribute>(type, mi, a => a.Configuration);

			if (attr != null)
			{
				if (attr.Expression != null)
					return attr.Expression;

				if (!string.IsNullOrEmpty(attr.MethodName))
				{
					Expression expr;

					if (mi is MethodInfo method && method.IsGenericMethod)
					{
						var args  = method.GetGenericArguments();
						var names = args.Select(t => (object)t.Name).ToArray();
						var name  = string.Format(attr.MethodName, names);

						expr = Expression.Call(
							mi.DeclaringType,
							name,
							name != attr.MethodName ? Array<Type>.Empty : args);
					}
					else
					{
						expr = Expression.Call(mi.DeclaringType, attr.MethodName, Array<Type>.Empty);
					}

					var call = Expression.Lambda<Func<LambdaExpression>>(Expression.Convert(expr,
						typeof(LambdaExpression)));

					return call.Compile()();
				}
			}

			return null;
		}

		#region ExpandExpression

		internal static Expression AggregateExpression(Expression expression)
		{
			return expression.Transform(expr =>
			{
				switch (expr.NodeType)
				{
					case ExpressionType.Or      :
					case ExpressionType.And     :
					case ExpressionType.OrElse  :
					case ExpressionType.AndAlso :
						{
							var stack  = new Stack<Expression>();
							var items  = new List<Expression>();
							var binary = (BinaryExpression) expr;

							stack.Push(binary.Right);
							stack.Push(binary.Left);
							while (stack.Count > 0)
							{
								var item = stack.Pop();
								if (item.NodeType == expr.NodeType)
								{
									binary  = (BinaryExpression) item;
									stack.Push(binary.Right);
									stack.Push(binary.Left);
								}
								else
									items.Add(item);
							}

							if (items.Count > 3)
							{
								// having N items will lead to NxM recursive calls in expression visitors and
								// will result in stack overflow on relatively small numbers (~1000 items).
								// To fix it we will rebalance condition tree here which will result in
								// LOG2(N)*M recursive calls, or 10*M calls for 1000 items.
								//
								// E.g. we have condition A OR B OR C OR D OR E
								// as an expression tree it represented as tree with depth 5
								//   OR
								// A    OR
								//    B    OR
								//       C    OR
								//          D    E
								// for rebalanced tree it will have depth 4
								//                  OR
								//        OR
								//   OR        OR        OR
								// A    B    C    D    E    F
								// Not much on small numbers, but huge improvement on bigger numbers
								while (items.Count != 1)
								{
									items = CompactTree(items, expr.NodeType);
								}

								return items[0];
							}
							break;
						}
				}

				return expr;
			});
		}

		private static List<Expression> CompactTree(List<Expression> items, ExpressionType nodeType)
		{
			var result = new List<Expression>();

			// traverse list from left to right to preserve calculation order
			for (var i = 0; i < items.Count; i += 2)
			{
				if (i + 1 == items.Count)
				{
					// last non-paired item
					result.Add(items[i]);
				}
				else
				{
					result.Add(Expression.MakeBinary(nodeType, items[i], items[i + 1]));
				}
			}

			return result;
		}

		internal static Expression ExpandExpression(Expression expression)
		{
			if (Common.Configuration.Linq.UseBinaryAggregateExpression)
				expression = AggregateExpression(expression);

			return expression.Transform(expr =>
			{
				switch (expr.NodeType)
				{
					case ExpressionType.Call:
						{
							var mc = (MethodCallExpression)expr;

							List<Expression> newArgs = null;
							for (var index = 0; index < mc.Arguments.Count; index++)
							{
								var arg = mc.Arguments[index];
								Expression newArg = null;
								if (typeof(LambdaExpression).IsSameOrParentOf(arg.Type))
								{
									var argUnwrapped = arg.Unwrap();
									if (argUnwrapped.NodeType == ExpressionType.MemberAccess ||
									    argUnwrapped.NodeType == ExpressionType.Call)
									{
										if (argUnwrapped.EvaluateExpression() is LambdaExpression lambda)
											newArg = ExpandExpression(lambda);
									}
								}

								if (newArg == null)
									newArgs?.Add(arg);
								else
								{
									if (newArgs == null)
										newArgs = new List<Expression>(mc.Arguments.Take(index));
									newArgs.Add(newArg);
								}
							}

							if (newArgs != null)
							{
								mc = mc.Update(mc.Object, newArgs);
							}


							if (mc.Method.Name == "Compile" && typeof(LambdaExpression).IsSameOrParentOf(mc.Method.DeclaringType))
							{
								if (mc.Object.EvaluateExpression() is LambdaExpression lambda)
								{
									return ExpandExpression(lambda);
								}
							}

							return mc;
						}

					case ExpressionType.Invoke:
						{
							var invocation = (InvocationExpression)expr;
							if (invocation.Expression.NodeType == ExpressionType.Call)
							{
								var mc = (MethodCallExpression)invocation.Expression;
								if (mc.Method.Name == "Compile" &&
								    typeof(LambdaExpression).IsSameOrParentOf(mc.Method.DeclaringType))
								{
									if (mc.Object.EvaluateExpression() is LambdaExpression lambda)
									{
										var map = new Dictionary<Expression, Expression>();
										for (int i = 0; i < invocation.Arguments.Count; i++)
										{
											map.Add(lambda.Parameters[i], invocation.Arguments[i]);
										}

										var newBody = lambda.Body.Transform(se =>
										{
											if (se.NodeType == ExpressionType.Parameter &&
											    map.TryGetValue(se, out var newExpr))
												return newExpr;
											return se;
										});

										return ExpandExpression(newBody);
									}
								}
							}
							break;
						}
				}

				return expr;
			});
		}

		#endregion

		#region ExposeExpression

		Expression ExposeExpression(Expression expression, HashSet<Expression> visited)
		{
			return expression.Transform(expr =>
			{
				switch (expr.NodeType)
				{
					case ExpressionType.MemberAccess:
						{
							var me = (MemberExpression)expr;
							var l  = ConvertMethodExpression(me.Expression?.Type ?? me.Member.ReflectedTypeEx(), me.Member);

							if (l != null)
							{
								var body  = l.Body.Unwrap();
								var parms = l.Parameters.ToDictionary(p => p);
								var ex    = body.Transform(wpi =>
								{
									if (wpi.NodeType == ExpressionType.Parameter && parms.ContainsKey((ParameterExpression)wpi))
									{
										if (wpi.Type.IsSameOrParentOf(me.Expression.Type))
										{
											return me.Expression;
										}

										if (DataContextParam.Type.IsSameOrParentOf(wpi.Type))
										{
											if (DataContextParam.Type != wpi.Type)
												return Expression.Convert(DataContextParam, wpi.Type);
											return DataContextParam;
										}

										throw new LinqToDBException($"Can't convert {wpi} to expression.");
									}

									return wpi;
								});

								if (ex.Type != expr.Type)
									ex = new ChangeTypeExpression(ex, expr.Type);

								return ExposeExpression(ex, visited);
							}

							break;
						}

					case ExpressionType.Constant :
						{
							var c = (ConstantExpression)expr;

							// Fix Mono behaviour.
							//
							//if (c.Value is IExpressionQuery)
							//	return ((IQueryable)c.Value).Expression;

							if (c.Value is IQueryable queryable && !(queryable is ITable))
							{
								var e = queryable.Expression;

								if (!visited.Contains(e))
								{
									visited.Add(e);
									return ExposeExpression(e, visited);
								}
							}

							break;
						}

					case ExpressionType.Invoke:
						{
							var invocation = (InvocationExpression)expr;
							if (invocation.Expression.NodeType == ExpressionType.Call)
							{
								var mc = (MethodCallExpression)invocation.Expression;
								if (mc.Method.Name == "Compile" &&
								    typeof(LambdaExpression).IsSameOrParentOf(mc.Method.DeclaringType))
								{
									if (mc.Object.EvaluateExpression() is LambdaExpression lambds)
									{
										var map = new Dictionary<Expression, Expression>();
										for (int i = 0; i < invocation.Arguments.Count; i++)
										{
											map.Add(lambds.Parameters[i], invocation.Arguments[i]);
										}

										var newBody = lambds.Body.Transform(se =>
										{
											if (se.NodeType == ExpressionType.Parameter &&
											    map.TryGetValue(se, out var newExpr))
												return newExpr;
											return se;
										});

										return ExposeExpression(newBody, visited);
									}
								}
							}
							break;
						}

				}

				return expr;
			});
		}

		#endregion

		public Expression ConvertExpression(Expression expression)
		{
			var result = expression.Transform(e =>
			{
				if (IsSequence(e))
				{
					var subquery = new SubQueryExpression(ParseModel(e), e.Type);
					return subquery;
				}

//				switch (e.NodeType)
//				{
//					case ExpressionType.Equal:
//						{
//							var binary = (BinaryExpression)e;
//							if (binary.Left.)
//						}
//				}

//				switch (e.NodeType)
//				{
//					case ExpressionType.New:
//						return new UnifiedNewExpression((NewExpression)e);
//					case ExpressionType.MemberInit:
//						return new UnifiedNewExpression((MemberInitExpression)e);
//				}

				return e;
			});
			return result;
		}

		public Expression PrepareExpressionForTranslation(Expression expression)
		{
			expression = ExpandExpression(expression);
			expression = ExposeExpression(expression, new HashSet<Expression>());
			return expression;
		}

		public QuerySourceReferenceExpression GetSourceReference(Sequence current)
		{
			var qs = current.GetQuerySource();
			if (qs == null)
				throw new Exception("Sequence does not contain source.");
			return GetSourceReference(qs);
		}

		public QuerySourceReferenceExpression GetSourceReference(IQuerySource querySource)
		{
			if (!_registeredSources2.TryGetValue(querySource, out var value))
				value = RegisterSource(querySource);
			return value;
		}

		public QuerySourceReferenceExpression RegisterSource(IQuerySource source, ParameterExpression parameter)
		{
			var referenceExpression = new QuerySourceReferenceExpression(source);
			_registeredSources.Add(parameter, referenceExpression);
			_registeredSources2.Add(source, referenceExpression);
			return referenceExpression;
		}

		public QuerySourceReferenceExpression RegisterSource(IQuerySource source)
		{
			var referenceExpression = new QuerySourceReferenceExpression(source);
			_registeredSources2.Add(source, referenceExpression);
			return referenceExpression;
		}

		public static Sequence CompactSequence(Sequence sequence)
		{
			for (var index = 0; index < sequence.Clauses.Count; index++)
			{
				var clause = sequence.Clauses[index];
				if (clause is Sequence sq) 
					sequence.Clauses[index] = CompactSequence(sq);
			}

			if (sequence.Clauses.Count == 1 && sequence.Clauses[0] is Sequence s)
				return s;
			return sequence;
		}

		public Sequence ParseModel(Expression expression)
		{
			var sequence = BuildSequence(new ParseBuildInfo(), expression);
			sequence = CompactSequence(sequence);

			return sequence;
		}

		public static bool IsSequence(Expression expression)
		{
			return FindBuilder(expression) != null;
		}

	}
}
