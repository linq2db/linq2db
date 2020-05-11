using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Data;
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using SqlQuery;

	public class ExpressionTreeOptimizationContext
	{
		readonly HashSet<Expression> _visitedExpressions = new HashSet<Expression>();

		public MappingSchema MappingSchema { get; }

		public ExpressionTreeOptimizationContext(MappingSchema mappingSchema)
		{
			MappingSchema = mappingSchema;
		}

		public void ClearVisitedCache()
		{
			_visitedExpressions.Clear();
		}

		public static Expression AggregateExpression(Expression expression)
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

		static List<Expression> CompactTree(List<Expression> items, ExpressionType nodeType)
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

		Sql.ExpressionAttribute? GetExpressionAttribute(MemberInfo member)
		{
			return MappingSchema.GetAttribute<Sql.ExpressionAttribute>(member.ReflectedType, member, a => a.Configuration);
		}

		public Expression ExpandQueryableMethods(Expression expression)
		{
			var result = expression.Transform(expr =>
			{
				if (expr.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression)expr;
					if (typeof(IQueryable<>).IsSameOrParentOf(mc.Type)
					    && !mc.IsQueryable(false)
					    && CanBeCompiled(mc))
					{
						var queryable = (IQueryable)mc.EvaluateExpression()!;

						if (!queryable.Expression.EqualsTo(mc,
							new Dictionary<Expression, QueryableAccessor>(), null, null,
							compareConstantValues: true))
						{
							return new TransformInfo(queryable.Expression, false, true);
						}
					}
				}

				return new TransformInfo(expr);
			});

			return result;
		}

		public static Expression ExpandExpression(Expression expression)
		{
			if (Common.Configuration.Linq.UseBinaryAggregateExpression)
				expression = AggregateExpression(expression);

			var result = expression.Transform(expr =>
			{
				switch (expr.NodeType)
				{
					case ExpressionType.Call:
						{
							var mc = (MethodCallExpression)expr;

							List<Expression>? newArgs = null;
							for (var index = 0; index < mc.Arguments.Count; index++)
							{
								var arg = mc.Arguments[index];
								Expression? newArg = null;
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

			return result;
		}

		#region IsServerSideOnly

		Expression? _lastIserverSideOnlyExpr;
		bool        _lastIserverSideOnlyResult;

		public bool IsServerSideOnly(Expression expr)
		{
			if (_lastIserverSideOnlyExpr == expr)
				return _lastIserverSideOnlyResult;

			var result = false;

			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var ex = (MemberExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, ex.Expression?.Type, ex.Member);

						if (l != null)
						{
							result = IsServerSideOnly(l.Body.Unwrap());
						}
						else
						{
							var attr = GetExpressionAttribute(ex.Member);
							result = attr != null && attr.ServerSideOnly;
						}

						break;
					}

				case ExpressionType.Call:
					{
						var e = (MethodCallExpression)expr;

						if (e.Method.DeclaringType == typeof(Enumerable))
						{
							if (CountBuilder.MethodNames.Contains(e.Method.Name) || e.IsAggregate(MappingSchema))
								result = IsQueryMember(e.Arguments[0]);
						}
						else if (e.IsAggregate(MappingSchema) || e.IsAssociation(MappingSchema))
						{
							result = true;
						}
						else if (e.Method.DeclaringType == typeof(Queryable))
						{
							switch (e.Method.Name)
							{
								case "Any"      :
								case "All"      :
								case "Contains" : result = true; break;
							}
						}
						else
						{
							var l = Expressions.ConvertMember(MappingSchema, e.Object?.Type, e.Method);

							if (l != null)
							{
								result = l.Body.Unwrap().Find(IsServerSideOnly) != null;
							}
							else
							{
								var attr = GetExpressionAttribute(e.Method);
								result = attr != null && attr.ServerSideOnly;
							}
						}

						break;
					}
			}

			_lastIserverSideOnlyExpr = expr;
			return _lastIserverSideOnlyResult = result;
		}

		static bool IsQueryMember(Expression expr)
		{
			expr = expr.Unwrap();
			if (expr != null) switch (expr.NodeType)
			{
				case ExpressionType.Parameter    : return true;
				case ExpressionType.MemberAccess : return IsQueryMember(((MemberExpression)expr).Expression);
				case ExpressionType.Call         :
					{
						var call = (MethodCallExpression)expr;

						if (call.Method.DeclaringType == typeof(Queryable))
							return true;

						if (call.Method.DeclaringType == typeof(Enumerable) && call.Arguments.Count > 0)
							return IsQueryMember(call.Arguments[0]);

						return IsQueryMember(call.Object);
					}
			}

			return false;
		}

		#endregion

		#region CanBeCompiled

		Expression? _lastExpr2;
		bool        _lastResult2;

		public bool CanBeCompiled(Expression expr)
		{
			if (_lastExpr2 == expr)
				return _lastResult2;

			var allowedParams = new HashSet<Expression> { ExpressionBuilder.ParametersParam };

			var result = null == expr.Find(ex =>
			{
				if (IsServerSideOnly(ex))
					return true;

				switch (ex.NodeType)
				{
					case ExpressionType.Parameter:
						return !allowedParams.Contains(ex);

					case ExpressionType.Call     :
						{
							var mc = (MethodCallExpression)ex;
							foreach (var arg in mc.Arguments)
							{
								if (arg.NodeType == ExpressionType.Lambda)
								{
									var lambda = (LambdaExpression)arg;
									foreach (var prm in lambda.Parameters)
										allowedParams.Add(prm);
								}
							}
							break;
						}
					case ExpressionType.Constant :
						{
							var cnt = (ConstantExpression)ex;
							if (cnt.Value is ISqlExpression)
								return true;
							break;
						}
					case ExpressionType.Extension:
						{
							if (ex is ContextRefExpression)
								return true;
							return !ex.CanReduce;
						}
				}

				return false;
			});

			_lastExpr2 = expr;
			return _lastResult2 = result;
		}

		#endregion

		#region CanBeConstant

		Expression? _lastExpr1;
		bool        _lastResult1;

		public bool CanBeConstant(Expression expr)
		{
			if (_lastExpr1 == expr)
				return _lastResult1;

			var result = null == expr.Find(ex =>
			{
				if (ex is BinaryExpression || ex is UnaryExpression /*|| ex.NodeType == ExpressionType.Convert*/)
					return false;

				if (MappingSchema.GetConvertExpression(ex.Type, typeof(DataParameter), false, false) != null)
					return true;

				switch (ex.NodeType)
				{
					case ExpressionType.Constant     :
						{
							var c = (ConstantExpression)ex;

							if (c.Value == null || ex.Type.IsConstantable())
								return false;

							break;
						}

					case ExpressionType.MemberAccess :
						{
							var ma = (MemberExpression)ex;

							var l = Expressions.ConvertMember(MappingSchema, ma.Expression?.Type, ma.Member);

							if (l != null)
								return l.Body.Unwrap().Find(CanBeConstant) == null;

							if (ma.Member.DeclaringType.IsConstantable() || ma.Member.IsNullableValueMember())
								return false;

							break;
						}

					case ExpressionType.Call         :
						{
							var mc = (MethodCallExpression)ex;

							if (mc.Method.DeclaringType.IsConstantable() || mc.Method.DeclaringType == typeof(object))
								return false;

							var attr = GetExpressionAttribute(mc.Method);

							if (attr != null && !attr.ServerSideOnly)
								return false;

							break;
						}
				}

				return true;
			});


			_lastExpr1 = expr;
			return _lastResult1 = result;
		}

		#endregion


		public Expression ExposeExpression(Expression expression)
		{
			var result = expression;

			result = result.Transform(expr =>
			{
				switch (expr.NodeType)
				{
					case ExpressionType.MemberAccess:
						{
							var me = (MemberExpression)expr;

							if (me.Member.IsNullableHasValueMember())
							{
								var obj = ExposeExpression(me.Expression);
								return Expression.NotEqual(obj, Expression.Constant(null, obj.Type));
							}

							var l  = ConvertMethodExpression(me.Expression?.Type ?? me.Member.ReflectedType, me.Member, out var alias);

							if (l != null)
							{
								var body  = l.Body.Unwrap();
								var parms = l.Parameters.ToDictionary(p => p);
								var ex    = body.Transform(wpi =>
								{
									if (wpi.NodeType == ExpressionType.Parameter && parms.ContainsKey((ParameterExpression)wpi))
									{
										if (wpi.Type.IsSameOrParentOf(me.Expression!.Type))
										{
											return me.Expression;
										}

										if (ExpressionBuilder.DataContextParam.Type.IsSameOrParentOf(wpi.Type))
										{
											if (ExpressionBuilder.DataContextParam.Type != wpi.Type)
												return Expression.Convert(ExpressionBuilder.DataContextParam, wpi.Type);
											return ExpressionBuilder.DataContextParam;
										}

										throw new LinqToDBException($"Can't convert {wpi} to expression.");
									}

									return wpi;
								});

								if (ex.Type != expr.Type)
									ex = new ChangeTypeExpression(ex, expr.Type);
								ex = ExposeExpression(ex);
								RegisterAlias(ex, alias!);
								return ex;
							}

							break;
						}

					case ExpressionType.Convert:
						{
							var ex = (UnaryExpression)expr;
							if (ex.Method != null)
							{
								var l = ConvertMethodExpression(ex.Method.DeclaringType, ex.Method, out var alias);
								if (l != null)
								{
									var exposed = l.GetBody(ex.Operand);
									exposed = ExposeExpression(exposed);
									RegisterAlias(exposed, alias!);
									return exposed;
								}
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

								if (!_visitedExpressions!.Contains(e))
								{
									_visitedExpressions!.Add(e);
									return ExposeExpression(e);
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

										return ExposeExpression(newBody);
									}
								}
							}
							break;
						}

				}

				return expr;
			});

			RelocateAlias(expression, result);
			return result;
		}

		public LambdaExpression? ConvertMethodExpression(Type type, MemberInfo mi, out string? alias)
		{
			var attr = MappingSchema.GetAttribute<ExpressionMethodAttribute>(type, mi, a => a.Configuration);

			if (attr != null)
			{
				alias = attr.Alias ?? mi.Name;
				if (attr.Expression != null)
					return attr.Expression;

				if (!String.IsNullOrEmpty(attr.MethodName))
				{
					Expression expr;

					if (mi is MethodInfo method && method.IsGenericMethod)
					{
						var args  = method.GetGenericArguments();
						var names = args.Select(t => (object)t.Name).ToArray();
						var name  = String.Format(attr.MethodName, names);

						expr = Expression.Call(
							mi.DeclaringType,
							name,
							name != attr.MethodName ? Array<Type>.Empty : args);
					}
					else
					{
						expr = Expression.Call(mi.DeclaringType, attr.MethodName, Array<Type>.Empty);
					}

					var evaluated = (LambdaExpression?)expr.EvaluateExpression();
					return evaluated;
				}
			}

			alias = null;
			return null;
		}


		#region Aliases

		private readonly Dictionary<Expression, string> _expressionAliases = new Dictionary<Expression, string>();

		public void RegisterAlias(Expression expression, string alias, bool force = false)
		{
			if (_expressionAliases.ContainsKey(expression))
			{
				if (!force)
					return;
				_expressionAliases.Remove(expression);
			}
			_expressionAliases.Add(expression, alias);
		}

		public void RelocateAlias(Expression oldExpression, Expression newExpression)
		{
			if (ReferenceEquals(oldExpression, newExpression))
				return;

			if (_expressionAliases.TryGetValue(oldExpression, out var alias))
				RegisterAlias(newExpression, alias);
		}

		public string GetExpressionAlias(Expression expression)
		{
			_expressionAliases.TryGetValue(expression, out var value);
			return value;
		}

		#endregion
	}
}
