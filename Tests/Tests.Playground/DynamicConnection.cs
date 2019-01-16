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
using LinqToDB.Extensions;

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

		private static void CalcTypesMap(Type genericType, Type realType, Dictionary<string, Type> typesMap)
		{
			if (genericType.IsGenericType)
			{
				var ge = genericType.GetGenericArguments();
				var re = realType.GetGenericArguments();
//				if (re.Length == 0)
//				{
//					realType = typeof(IEnumerable<>).MakeGenericType(realType);
//					re = realType.GetGenericArguments();
//				}

				CalcTypesMap(ge, re, typesMap);
			}
			else
			{
				if (!typesMap.ContainsKey(genericType.Name))
					typesMap.Add(genericType.Name, realType);
			}
		}

		private static void CalcTypesMap(Type[] genericTypes, Type[] realTypes, Dictionary<string, Type> typesMap)
		{
			for (int i = 0; i < genericTypes.Length; i++)
			{
				var gt = genericTypes[i];
				var rt = realTypes[i];
				CalcTypesMap(gt, rt, typesMap);
			}
		}

		//TODO: move to parameter of ReplaceDynamicPropsl
		private Dictionary<MemberInfo, Expression> knownReplacements;

		public Expression ReplaceDynamicProps(Type propType, Expression expression, bool throwIfNotExists)
		{
			var result = expression.Transform(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.Quote:
						e = ReplaceDynamicProps(null, e.Unwrap(), throwIfNotExists);
						break;
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

							var left  = ReplaceDynamicProps(propType, be.Left, throwIfNotExists);
							var right = ReplaceDynamicProps(propType, be.Right, throwIfNotExists);

							if (ReferenceEquals(left, be.Left) && ReferenceEquals(right, be.Right))
								break;

							switch (e.NodeType)
							{
								case ExpressionType.LessThan:
									e = Expression.LessThan(left, right);
									break;
								case ExpressionType.LessThanOrEqual:
									e = Expression.LessThanOrEqual(left, right);
									break;
								case ExpressionType.GreaterThan:
									e = Expression.GreaterThan(left, right);
									break;
								case ExpressionType.GreaterThanOrEqual:
									e = Expression.GreaterThanOrEqual(left, right);
									break;
								case ExpressionType.Equal:
									e = ExpressionBuilder.Equal(MappingSchema, left, right);
									break;
								default:
									throw new ArgumentException();
							}

							break;
						}
					case ExpressionType.Lambda:
						{
							var lambda = (LambdaExpression)e;
							var body = ReplaceDynamicProps(propType, lambda.Body, throwIfNotExists);
							if (body != lambda.Body)
							{
								var newLambda = Expression.Lambda(body, lambda.Parameters);
								e = newLambda;
							}
							break;
						}

					case ExpressionType.New:
						{
							var newExpr = (NewExpression)e;
							for (var i = 0; i < newExpr.Members.Count; i++)
							{
								var member = newExpr.Members[i];
								if (member.GetMemberType() != newExpr.Arguments[i].Type)
								{
									knownReplacements.Remove(member);
									knownReplacements.Add(member, newExpr.Arguments[i]);
								}
							}

							break;
						}

					case ExpressionType.Call:
						{
							e = TransformProp(propType, e, false);

							if (e.NodeType != ExpressionType.Call)
								return e;
							
							var mc = (MethodCallExpression)e;

							var arguments = mc.Arguments.Select(arg => ReplaceDynamicProps(propType, arg, false)).ToList();
							if (!arguments.SequenceEqual(mc.Arguments))
								mc = mc.Update(mc.Object, arguments);

							var newMc = UpcastMethodExpression(mc);
							if (!ReferenceEquals(mc, newMc))
								newMc = (MethodCallExpression)ReplaceDynamicProps(propType, newMc, true);

							e = newMc;

							break;
						}
					case ExpressionType.Convert:
						{
							var unary = (UnaryExpression)e;
							var operand = ReplaceDynamicProps(unary.Type, unary.Operand, throwIfNotExists);
							if (!ReferenceEquals(operand, unary.Operand))
								e = Expression.Convert(operand, expression.Type);
							break;
						}
				}

				return e;
			});

			return result;
		}

		private static Type CalcNewType(Type generic, Dictionary<string, Type> typesMap)
		{
			if (generic.IsGenericType)
			{
				var newArguments = generic.GetGenericArguments()
					.Select(ga => CalcNewType(ga, typesMap))
					.ToArray();
				return generic.GetGenericTypeDefinition().MakeGenericType(newArguments);
			}

			if (!typesMap.TryGetValue(generic.Name, out var result))
				throw new Exception("Invalid type mapping");
			return result;
		}

		private static LambdaExpression UpcastLambda(LambdaExpression lambda, Type genericType,
			Dictionary<string, Type> typesMap)
		{
			var genericBodyType = typeof(Expression).IsSameOrParentOf(genericType) ? genericType.GetGenericArguments()[0] : genericType;
			var genericBodyTypeArguments = genericBodyType.GetGenericArguments();

			var dictionary = new Dictionary<Expression, Expression>();
			for (var i = 0; i < lambda.Parameters.Count; i++)
			{
				var parameter = lambda.Parameters[i];
				var newParameter = parameter;

				var genericParamType = genericBodyTypeArguments[i];
				var newParamType = CalcNewType(genericParamType, typesMap);

				if (newParamType != parameter.Type)
				{
					newParameter = Expression.Parameter(newParamType, parameter.Name);
				}

				dictionary.Add(parameter, newParameter);
			}

			var body = lambda.Body.Transform(le =>
				dictionary.TryGetValue(le, out var ne) ? ne : le);

			var genericReturnType = genericBodyTypeArguments[genericBodyTypeArguments.Length - 1];
			var resultType = CalcNewType(genericReturnType, typesMap);
			if (body.Type != resultType)
			{
				body = Expression.Convert(body, resultType);
			}

			var newLabda = Expression.Lambda(body, lambda.Parameters.Select(
				p => (ParameterExpression)dictionary[p]));

			return newLabda;
		}

		private static MethodCallExpression UpcastMethodExpression(MethodCallExpression mc)
		{
			if (!mc.Method.IsGenericMethod || mc.Method.GetGenericMethodDefinition() == _sqlProperty)
				return mc;

			var genericDef = mc.Method.GetGenericMethodDefinition();
			var genericArgs = genericDef.GetGenericArguments();

			var genericParameters = genericDef.GetParameters();
			var currentParameters = mc.Method.GetParameters();

			var genericTypesMap = new Dictionary<string, Type>();
			for (int i = 0; i < genericParameters.Length; i++)
			{
				var gp = genericParameters[i].ParameterType;
				var cp = currentParameters[i].ParameterType;
				var at = mc.Arguments[i].Type;

				if (cp.IsGenericType && at.IsGenericType)
					cp = at;
				CalcTypesMap(gp, cp, genericTypesMap);
			}


			var newGenericArguments = genericArgs.Select(p => genericTypesMap[p.Name]).ToArray();
			var methodGenericArguments = mc.Method.GetGenericArguments();

			if (!newGenericArguments.SequenceEqual(methodGenericArguments))
			{
				var toReplace = genericDef
					.MakeGenericMethod(newGenericArguments);

				var arguments = mc.Arguments.ToList();
				for (int i = 0; i < mc.Arguments.Count; i++)
				{
					var gp = genericParameters[i].ParameterType;
					var arg = arguments[i].Unwrap();

					if (arg.NodeType == ExpressionType.Lambda)
					{
						arguments[i] = UpcastLambda((LambdaExpression)arg, gp, genericTypesMap);
					}
				}

				mc = Expression.Call(null, toReplace, arguments);
			}

			return mc;
		}

		private Expression FixArgument(Expression expr)
		{
			return expr;
		}

		public Expression ProcessExpression(Expression expression)
		{
			knownReplacements = new Dictionary<MemberInfo, Expression>();
			var result = ReplaceDynamicProps(null, expression, true);
			return result;
		}

		private Expression TransformProp(Type propType, Expression expr, bool throwIfNotExists)
		{
			if (expr.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expr;
				if (mc.Method == _getItemInfo)
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
							{
							
								if (propType != null && objectExpr is MemberExpression memberExpression)
								{
									var sqlProp = _sqlProperty.MakeGenericMethod(propType);
									objectExpr = Expression.Call(null, sqlProp, objectExpr, Expression.Constant(name));
								}
								else
								{
									if (throwIfNotExists)
										throw new Exception($"Property {objectExpr.Type}.{name} not found.");
									return expr;
								}
							}
							else
							{
								objectExpr = Expression.MakeMemberAccess(objectExpr, prop);
							}
						}

						expr = ReplaceDynamicProps(propType, objectExpr, throwIfNotExists);
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
						{
							if (throwIfNotExists)
								throw new Exception($"Property {objectExpr.Type}.{name} not found.");
							return expr;
						}

						objectExpr = Expression.MakeMemberAccess(objectExpr, prop);
					}

					expr = ReplaceDynamicProps(propType, objectExpr, throwIfNotExists);
						
				}
			}

			return expr;
		}

		private Expression TransformPropOld(Type propType, Expression expr)
		{
			if (expr.NodeType == ExpressionType.Call)
			{
				if (propType != null)
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

						var sqlPropMethod = _sqlProperty.MakeGenericMethod(propType);
						objectExpr = Expression.Call(null, sqlPropMethod, objectExpr, Expression.Constant(path[path.Length - 1]));

						expr = ReplaceDynamicProps(propType, objectExpr, false);
					}
				}
			}
			else
				expr = ReplaceDynamicProps(propType, expr, false);

			return expr;
		}
	}
}
