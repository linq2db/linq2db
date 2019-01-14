using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Linq;
using LinqToDB.Linq.Builder;

namespace Tests.Playground
{
	public class DynamicConnection : DataConnection, IExpressionPreprocessor
	{
		private static MethodInfo _getItemInfo = MemberHelper.MethodOf<BaseEntity>(be => be[""]);
		private static MethodInfo _getItemInfoPropValue = MemberHelper.MethodOf<PropertyValue>(be => be[""]);
		private static MethodInfo _sqlProperty = typeof(Sql).GetMethod("Property").GetGenericMethodDefinition();

		public DynamicConnection(string configurationString) : base(configurationString)
		{
		}


		public Expression UpCastQuery(Expression expression)
		{
			var result = expression.Transform(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.Call:
						{
							var mc = (MethodCallExpression)e;

							if (mc.IsQueryable())
							{
								var a = UpCastQuery(ReplaceDynamicProps(mc.Arguments[0]));

								var methodGenericArgument = mc.Method.GetGenericArguments()[0];
								var constantGenericArgument = a.Type.GetGenericArguments()[0];
								if (methodGenericArgument != constantGenericArgument)
								{
									var toReplace = mc.Method.GetGenericMethodDefinition()
										.MakeGenericMethod(constantGenericArgument);
									var arguments = new[] {a}.Concat(mc.Arguments.Skip(1).Select(UpCastQuery)).ToList();
									for (int i = 1; i < arguments.Count; i++)
									{
										var arg = arguments[i].Unwrap();
										if (arg.NodeType == ExpressionType.Lambda)
										{
											var dictionary = new Dictionary<Expression, Expression>();
											var lambda = (LambdaExpression)arg;
											foreach (var parameter in lambda.Parameters)
											{
												var newParameter = parameter;
												if (parameter.Type == methodGenericArgument)
												{
													newParameter = Expression.Parameter(constantGenericArgument,
														parameter.Name);
												}

												dictionary.Add(parameter, newParameter);
											}

											var body = lambda.Body.Transform(le =>
												dictionary.TryGetValue(le, out var ne) ? ne : le);
											arg = Expression.Lambda(body,
												lambda.Parameters.Select(
													p => (ParameterExpression)dictionary[p]));
											arguments[i] = arg;
										}
									}

									mc = Expression.Call(null, toReplace, arguments);
								}
							}

							e = mc;
							break;
						}
				}

				return e;
			});

			return result;
		}

		public Expression ReplaceDynamicProps(Expression expression)
		{
			Type propType = null;

			expression = UpCastQuery(expression);

			var result = expression.Transform(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.LessThan:
					case ExpressionType.LessThanOrEqual:
					case ExpressionType.GreaterThan:
					case ExpressionType.GreaterThanOrEqual:
					case ExpressionType.Equal:
						{
							var be = (BinaryExpression)e;
							if (be.Left.Type != typeof(PropertyValue))
							{
								propType = be.Left.Type;
							}
							else if (be.Right.Type != typeof(PropertyValue))
							{
								propType = be.Right.Type;
							}

							if (be.Method?.DeclaringType == typeof(PropertyValue))
							{
								propType = be.Method.GetParameters()[1].ParameterType;
							}

							switch (e.NodeType)
							{
								case ExpressionType.LessThan:
									e = Expression.LessThan(TransformProp(propType, be.Left), TransformProp(propType, be.Right));
									break;
								case ExpressionType.LessThanOrEqual:
									e = Expression.LessThanOrEqual(TransformProp(propType, be.Left), TransformProp(propType, be.Right));
									break;
								case ExpressionType.GreaterThan:
									e = Expression.GreaterThan(TransformProp(propType, be.Left), TransformProp(propType, be.Right));
									break;
								case ExpressionType.GreaterThanOrEqual:
									e = Expression.GreaterThanOrEqual(TransformProp(propType, be.Left), TransformProp(propType, be.Right));
									break;
								case ExpressionType.Equal:
									e = ExpressionBuilder.Equal(MappingSchema, TransformProp(propType, be.Left), TransformProp(propType, be.Right));
									break;
								default:
									throw new ArgumentException();
							}

							break;
						}

					case ExpressionType.ConvertChecked:
					case ExpressionType.Convert:
						{
							var unary = (UnaryExpression)e;
							if (unary.Method?.DeclaringType == typeof(PropertyValue))
							{
								if (e.NodeType == ExpressionType.Convert)
									e = Expression.Convert(TransformProp(unary.Type, unary.Operand), unary.Type);
								else
									e = Expression.ConvertChecked(TransformProp(unary.Type, unary.Operand), unary.Type);
							}
							break;
						}
					case ExpressionType.Call:
						{
							e = TransformProp(propType, e);
							break;
						}
				}

				return e;
			});

			return result;
		}

		private Expression FixArgument(Expression expr)
		{
			return expr;
		}

		public Expression ProcessExpression(Expression expression)
		{
			var result = ReplaceDynamicProps(expression);
			return result;
		}


		private Expression TransformProp(Type type, Expression expr)
		{
			if (expr.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expr;
				if (type != null && mc.Method == _getItemInfo)
				{
					if (mc.Method == _getItemInfo)
					{
						var propName = (string)mc.Arguments[0].EvaluateExpression();
						var path = propName.Split('.');
						var objectExpr = mc.Object;
						for (int i = 0; i < path.Length; i++)
						{
							var name = path[i];

							var prop = objectExpr.Type.GetProperty(name);
							if (prop == null)
								throw new Exception($"Property {objectExpr.Type}.{name} not found.");

							objectExpr = Expression.MakeMemberAccess(objectExpr, prop);
						}

						expr = ReplaceDynamicProps(objectExpr);
					}
				}
				else if (mc.Method == _getItemInfoPropValue)
				{
					var propName = (string)mc.Arguments[0].EvaluateExpression();
					var propPath = new List<string>();
					propPath.Add(propName);
					var objectProp = mc.Object;
					do
					{
						if (objectProp.NodeType == ExpressionType.Call)
						{
							var smc = (MethodCallExpression)objectProp;
							if (smc.Method == _getItemInfo || smc.Method == _getItemInfoPropValue)
							{
								propName = (string)smc.Arguments[0].EvaluateExpression();
								propPath.AddRange(propName.Split('.').Reverse());
								objectProp = smc.Object;
								continue;
							}
						}
						break;
					} while (true);

					var objectExpr = objectProp;

					for (int i = propPath.Count - 1; i >= 0; i--)
					{
						var name = propPath[i];

						var prop = objectExpr.Type.GetProperty(name);
						if (prop == null)
							throw new Exception($"Property {objectExpr.Type}.{name} not found.");

						objectExpr = Expression.MakeMemberAccess(objectExpr, prop);
					}

					expr = ReplaceDynamicProps(objectExpr);
						
				}
			}
			else
				expr = ReplaceDynamicProps(expr);

			return expr;
		}

		private Expression TransformPropOld(Type type, Expression expr)
		{
			if (expr.NodeType == ExpressionType.Call)
			{
				if (type != null)
				{
					var mc = (MethodCallExpression)expr;
					if (mc.Method == _getItemInfo)
					{
						var propName = (string)mc.Arguments[0].EvaluateExpression();
						var path = propName.Split('.');
						var objectExpr = mc.Object;
						for (int i = 0; i < path.Length - 1; i++)
						{
							var name = path[i];
								
							var sqlProp = _sqlProperty.MakeGenericMethod(typeof(BaseEntity));
							objectExpr = Expression.Call(null, sqlProp, objectExpr, Expression.Constant(name));
						}

						var sqlPropMethod = _sqlProperty.MakeGenericMethod(type);
						objectExpr = Expression.Call(null, sqlPropMethod, objectExpr, Expression.Constant(path[path.Length - 1]));

						expr = ReplaceDynamicProps(objectExpr);

					}
				}
			}
			else
				expr = ReplaceDynamicProps(expr);

			return expr;
		}
	}
}
