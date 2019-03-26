using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq.Parser;
using LinqToDB.Linq.Relinq.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using Remotion.Linq;
using Remotion.Linq.Parsing.ExpressionVisitors.Transformation;
using Remotion.Linq.Parsing.ExpressionVisitors.TreeEvaluation;
using Remotion.Linq.Parsing.Structure;

namespace LinqToDB.Linq.Relinq
{
	public class ParsingContext
	{
		/// <summary>
		/// Used to do not allow parameters evaluation
		/// </summary>
		class EvaluatableFilter : EvaluatableExpressionFilterBase, IEvaluatableExpressionFilter
		{
			public override bool IsEvaluatableMember(MemberExpression node)
			{
				if (node.Expression.NodeType == ExpressionType.Constant && !typeof(IQueryable<>).IsSameOrParentOf(node.Expression.Type))
					return false;
				return base.IsEvaluatableMember(node);
			}
		}

		public MappingSchema MappingSchema => DataContext.MappingSchema;
		public IDataContext DataContext { get; }

		readonly List<ParameterAccessor>   CurrentSqlParameters = new List<ParameterAccessor>();
		readonly Dictionary<Expression,Expression> _expressionAccessors = new Dictionary<Expression, Expression>();

		public ParsingContext([JetBrains.Annotations.NotNull] IDataContext dataContext)
		{
			DataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
		}

		public Expression PrepareExpression(Expression expression)
		{
			var transformed = ExposeExpression(expression, new HashSet<Expression>());
//			    transformed = DetectParameters(transformed);
			return transformed;
		}

		private Expression DetectParameters(Expression expression)
		{
			var transformed = expression.Transform(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.MemberAccess:
						{
							var ma = (MemberExpression)e;
							if (ma.Expression.NodeType == ExpressionType.Constant)
							{
								return new QueryParameterExpression(ma);
							}
							break;
						}
				}

				return e;
			});

			return transformed;
		}

		QueryParser CreateParser()
		{
			return new QueryParser(new ExpressionTreeParser(
				ExpressionTreeParser.CreateDefaultNodeTypeProvider(),
				ExpressionTreeParser.CreateDefaultProcessor(
					ExpressionTransformerRegistry.CreateDefault(),
					new EvaluatableFilter())));
		}

		public QueryModel ParseModel(Expression queryExpression)
		{
			var transformed = PrepareExpression(queryExpression);
			var parser      = CreateParser();
			var queryModel  = parser.GetParsedQuery(transformed);
			return queryModel;
		}

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

		Expression ExposeExpression(Expression expression, HashSet<Expression> visitedExpressions)
		{
			var transformed = ExpandExpression(expression);

			transformed = transformed.Transform(expr =>
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

										if (TranslationContext.DataContextParam.Type.IsSameOrParentOf(wpi.Type))
										{
											if (TranslationContext.DataContextParam.Type != wpi.Type)
												return Expression.Convert(TranslationContext.DataContextParam, wpi.Type);
											return TranslationContext.DataContextParam;
										}

										throw new LinqToDBException($"Can't convert {wpi} to expression.");
									}

									return wpi;
								});

								if (ex.Type != expr.Type)
									ex = new ChangeTypeExpression(ex, expr.Type);

								return ExposeExpression(ex, visitedExpressions);
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

								if (visitedExpressions.Add(e))
								{
									return ExposeExpression(e, visitedExpressions);
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

										return ExposeExpression(newBody, visitedExpressions);
									}
								}
							}
							break;
						}

				}

				return expr;
			});

			return transformed;
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

		#region Build Parameter

		readonly Dictionary<Expression,ParameterAccessor> _parameters = new Dictionary<Expression,ParameterAccessor>();

		public readonly HashSet<Expression> AsParameters = new HashSet<Expression>();

		internal enum BuildParameterType
		{
			Default,
			InPredicate
		}

			internal static ParameterAccessor CreateParameterAccessor(
				IDataContext        dataContext,
				Expression          accessorExpression,
				Expression          dataTypeAccessorExpression,
				Expression          dbTypeAccessorExpression,
				Expression          expression,
				ParameterExpression expressionParam,
				ParameterExpression parametersParam,
				string              name,
				BuildParameterType  buildParameterType = BuildParameterType.Default,
				LambdaExpression    expr = null)
			{
				var type = accessorExpression.Type;

				if (buildParameterType != BuildParameterType.InPredicate)
					expr = expr ?? dataContext.MappingSchema.GetConvertExpression(type, typeof(DataParameter), createDefault: false);
				else
					expr = null;

				if (expr != null)
				{
					if (accessorExpression == null || dataTypeAccessorExpression == null || dbTypeAccessorExpression == null)
					{
						var body = expr.GetBody(accessorExpression);

						accessorExpression         = Expression.PropertyOrField(body, "Value");
						dataTypeAccessorExpression = Expression.PropertyOrField(body, "DataType");
						dbTypeAccessorExpression   = Expression.PropertyOrField(body, "DbType");
					}
				}
				else
				{
					if (type == typeof(DataParameter))
					{
						var dp = expression.EvaluateExpression() as DataParameter;
						if (dp?.Name?.IsNullOrEmpty() == false)
							name = dp.Name;

						dataTypeAccessorExpression = Expression.PropertyOrField(accessorExpression, "DataType");
						dbTypeAccessorExpression   = Expression.PropertyOrField(accessorExpression, "DbType");
						accessorExpression         = Expression.PropertyOrField(accessorExpression, "Value");
					}
					else
					{
						var defaultType = Converter.GetDefaultMappingFromEnumType(dataContext.MappingSchema, type);

						if (defaultType != null)
						{
							var enumMapExpr = dataContext.MappingSchema.GetConvertExpression(type, defaultType);
							accessorExpression = enumMapExpr.GetBody(accessorExpression);
						}
					}
				}

				// see #820
				accessorExpression = accessorExpression.Transform(e =>
				{
					switch (e.NodeType)
					{
						case ExpressionType.MemberAccess:
							var ma = (MemberExpression) e;

							if (ma.Member.IsNullableValueMember())
							{
								return Expression.Condition(
									Expression.Equal(ma.Expression, Expression.Constant(null, ma.Expression.Type)),
									Expression.Default(e.Type),
									e);
							}

							return e;
						case ExpressionType.Convert:
							var ce = (UnaryExpression) e;
							if (ce.Operand.Type.IsNullable() && !ce.Type.IsNullable())
							{
								return Expression.Condition(
									Expression.Equal(ce.Operand, Expression.Constant(null, ce.Operand.Type)),
									Expression.Default(e.Type),
									e);
							}
							return e;
						default:
							return e;
					}
				});

				var mapper = Expression.Lambda<Func<Expression,object[],object>>(
					Expression.Convert(accessorExpression, typeof(object)),
					new [] { expressionParam, parametersParam });

				var dataTypeAccessor = Expression.Lambda<Func<Expression,object[],DataType>>(
					Expression.Convert(dataTypeAccessorExpression, typeof(DataType)),
					new [] { expressionParam, parametersParam });

				var dbTypeAccessor = Expression.Lambda<Func<Expression,object[],string>>(
					Expression.Convert(dbTypeAccessorExpression, typeof(string)),
					new [] { expressionParam, parametersParam });

				return new ParameterAccessor
				(
					expression,
					mapper.Compile(),
					dataTypeAccessor.Compile(),
					dbTypeAccessor.Compile(),
					new SqlParameter(accessorExpression.Type, name, null) { IsQueryParameter = !(dataContext.InlineParameters && accessorExpression.Type.IsScalar(false)) }
				);
			}

		class ValueTypeExpression
		{
			public Expression ValueExpression;
			public Expression DataTypeExpression;
			public Expression DbTypeExpression;

			public DbDataType DataType;
		}

		ValueTypeExpression ReplaceParameter(IDictionary<Expression,Expression> expressionAccessors, Expression expression, Action<string> setName)
		{
			var result = new ValueTypeExpression
			{
				DataType           = new DbDataType(expression.Type),
				DataTypeExpression = Expression.Constant(DataType.Undefined),
				DbTypeExpression   = Expression.Constant(null, typeof(string))
			};

			var unwrapped = expression.Unwrap();
			if (unwrapped.NodeType == ExpressionType.MemberAccess)
			{
				var ma = (MemberExpression)unwrapped;
				setName(ma.Member.Name);
			}

			result.ValueExpression = expression.Transform(expr =>
			{
				if (expr.NodeType == ExpressionType.Constant)
				{
					var c = (ConstantExpression)expr;

					if (!expr.Type.IsConstantable() || AsParameters.Contains(c))
					{
						if (expressionAccessors.TryGetValue(expr, out var val))
						{
							expr = Expression.Convert(val, expr.Type);

							if (expression.NodeType == ExpressionType.MemberAccess)
							{
								var ma = (MemberExpression)expression;

								var mt = GetMemberDataType(ma.Member);

								if (mt.DataType != DataType.Undefined)
								{
									result.DataType.WithDataType(mt.DataType);
									result.DataTypeExpression = Expression.Constant(mt.DataType);
								}

								if (mt.DbType != null)
								{
									result.DataType.WithDbType(mt.DbType);
									result.DbTypeExpression = Expression.Constant(mt.DbType);
								}

								setName(ma.Member.Name);
							}
						}
					}
				}

				return expr;
			});

			return result;
		}

		#endregion

		DbDataType GetMemberDataType(MemberInfo member)
		{
			var typeResult = new DbDataType(member.GetMemberType());

			var dta      = MappingSchema.GetAttribute<DataTypeAttribute>(member.ReflectedTypeEx(), member);
			var ca       = MappingSchema.GetAttribute<ColumnAttribute>  (member.ReflectedTypeEx(), member);

			var dataType = ca?.DataType ?? dta?.DataType;

			if (dataType != null)
				typeResult = typeResult.WithDataType(dataType.Value);

			var dbType = ca?.DbType ?? dta?.DbType;
			if (dbType != null)
				typeResult = typeResult.WithDbType(dbType);

			return typeResult;
		}

		static DbDataType GetDataType(ISqlExpression expr, DbDataType baseType)
		{
			var systemType = baseType.SystemType;
			var dataType   = baseType.DataType;
			string dbType  = baseType.DbType;

			QueryVisitor.Find(expr, e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlField:
						dataType   = ((SqlField)e).DataType;
						dbType     = ((SqlField)e).DbType;
						//systemType = ((SqlField)e).SystemType;
						return true;
					case QueryElementType.SqlParameter:
						dataType   = ((SqlParameter)e).DataType;
						dbType     = ((SqlParameter)e).DbType;
						//systemType = ((SqlParameter)e).SystemType;
						return true;
					case QueryElementType.SqlDataType:
						dataType   = ((SqlDataType)e).DataType;
						dbType     = ((SqlDataType)e).DbType;
						//systemType = ((SqlDataType)e).SystemType;
						return true;
					case QueryElementType.SqlValue:
						dataType   = ((SqlValue)e).ValueType.DataType;
						dbType     = ((SqlValue)e).ValueType.DbType;
						//systemType = ((SqlValue)e).ValueType.SystemType;
						return true;
					default:
						return false;
				}
			});

			return new DbDataType(
				systemType ?? baseType.SystemType,
				dataType == DataType.Undefined ? baseType.DataType : dataType,
				string.IsNullOrEmpty(dbType) ? baseType.DbType : dbType
			);
		}


	}
}
