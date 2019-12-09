using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq.Builder;

namespace Tests.Playground
{
	public class EagerLoadingProbes
	{
		private static readonly MethodInfo[] _tupleConstructors = 
		{
			MemberHelper.MethodOf(() => Tuple.Create(0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0, 0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0, 0, 0, 0, 0)).GetGenericMethodDefinition(),
		};

		private static readonly MethodInfo _queryWithDetailsInternalMethodInfo = MemberHelper.MethodOf(() =>
			QueryWithDetailsInternal<int, int, int>((IQueryable<int>)null, null, null, null)).GetGenericMethodDefinition();

		class EagerLoadingContext<T, TKey>
		{
			private Dictionary<TKey, List<T>> _items;

			public void Add(TKey key, T item)
			{
				List<T> list;
				if (_items == null)
				{
					_items = new Dictionary<TKey, List<T>>();
					list = new List<T>();
					_items.Add(key, list);
				}
				else if (!_items.TryGetValue(key, out list))
				{
					list = new List<T>();
					_items.Add(key, list);
				}
				list.Add(item);
			}

			public List<T> GetList(TKey key)
			{
				if (_items == null || !_items.TryGetValue(key, out var list))
					return new List<T>();
				return list;
			}
		}

		private static Expression GenerateKeyExpression(Expression[] members, int startIndex)
		{
			var count = members.Length - startIndex;
			if (count == 0)
				throw new ArgumentException();

			if (count == 1)
				return members[startIndex];

			Expression[] arguments;

			if (count > 8)
			{
				count = 8;
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count - 1);
				arguments[count - 1] = GenerateKeyExpression(members, startIndex + count);
			}
			else
			{
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count);
			}

			var constructor = _tupleConstructors[count - 2];

			var typedConstructorPlain = constructor.MakeGenericMethod(arguments.Select(a => a.Type).ToArray());

			return Expression.Call(typedConstructorPlain, arguments);
		}

		public static List<T> QueryWithDetails<T, TD>(
			IDataContext dc,
			IQueryable<T> mainQuery,
			Expression<Func<T, IEnumerable<TD>>> detailQueryExpression,
			Action<T, List<TD>> detailSetter)
		{
			var ed = dc.MappingSchema.GetEntityDescriptor(typeof(T));
			var keys = ed.Columns.Where(c => c.IsPrimaryKey).ToArray();
			if (keys.Length == 0) 
				keys = ed.Columns.Where(c => dc.MappingSchema.IsScalarType(c.MemberType)).ToArray();

			if (keys.Length == 0)
				throw new LinqToDBException($"Can not retrieve key fro type '{typeof(T).Name}'");

			var objParam = Expression.Parameter(typeof(T), "obj");
			var properties = keys.Select(k => (Expression)Expression.MakeMemberAccess(objParam, k.MemberInfo))
				.ToArray();
			var getKeyExpr = Expression.Lambda(GenerateKeyExpression(properties, 0), objParam);

			var method = _queryWithDetailsInternalMethodInfo.MakeGenericMethod(new[] { typeof(T), typeof(TD), getKeyExpr.Body.Type });

			var result = method.Invoke(null, new object[] { mainQuery, detailQueryExpression, getKeyExpr, detailSetter });
			return (List<T>)result;
		}

		private static List<T> QueryWithDetailsInternal<T, TD, TKey>(
			IQueryable<T> mainQuery, 
			Expression<Func<T, IEnumerable<TD>>> detailQueryExpression,
			Expression<Func<T, TKey>> getKeyExpression,
			Action<T, List<TD>> detailSetter)
		{
			var detailQuery = mainQuery.SelectMany(detailQueryExpression, (m, d) => new { MasterKey = getKeyExpression.Compile().Invoke(m), Detail = d});
			var detailsWithKey = detailQuery.ToList();
			var detailDictionary = new Dictionary<TKey, List<TD>>();

			foreach (var d in detailsWithKey)
			{
				if (!detailDictionary.TryGetValue(d.MasterKey, out var list))
				{
					list = new List<TD>();
					detailDictionary.Add(d.MasterKey, list);
				}
				list.Add(d.Detail);
			}

			var mainEntities = mainQuery.ToList();
			var getKeyFunc = getKeyExpression.Compile();

			foreach (var entity in mainEntities)
			{
				var key = getKeyFunc(entity);
				if (!detailDictionary.TryGetValue(key, out var ds))
					ds = new List<TD>();
				detailSetter(entity, ds);
			}

			return mainEntities;
		}

//
//		this IQueryable<TSource> source, Expression<Func<TSource, IEnumerable<TCollection>>> collectionSelector, Expression<Func<TSource, TCollection, TResult>> resultSelector);

//		this IQueryable<TSource> source, Expression<Func<TSource, IEnumerable<TCollection2>>> collectionSelector, Expression<Func<TSource, TCollection2, TResult>> resultSelector);

		private static MethodInfo _keyDataHolderMethodInfo =
			MemberHelper.MethodOf(() => KDH.Create(0, 0)).GetGenericMethodDefinition();

		private static MethodInfo _selectMethodInfo =
			MemberHelper.MethodOf((IQueryable<int> q) => q.Select(p => p)).GetGenericMethodDefinition();

		public class RemappingInfo
		{
			public RemappingInfo(Dictionary<MemberInfo, MemberInfo[]> memberMapping, Expression resultExpression)
			{
				MemberMapping = memberMapping;
				ResultExpression = resultExpression;
			}

			public Dictionary<MemberInfo, MemberInfo[]> MemberMapping { get; }
			public Expression ResultExpression { get; }
		}

		public class ReplaceInfo
		{
			public LambdaExpression TargetLambda { get; set; }
			public List<Expression> Keys { get; } = new List<Expression>();
			public Dictionary<MemberInfo, MemberInfo[]> MemberMapping { get; } = new Dictionary<MemberInfo, MemberInfo[]>();
		}

		// public static LambdaExpression CorrectLambdaType(LambdaExpression before, LambdaExpression after)
		// {
		// 	if (before.Body.Type.IsGenericType && typeof(IQueryable<>).IsSameOrParentOf(after.Body.Type))
		// 	{
		// 		var typesMapping = new Dictionary<Type, Type>();
		// 		var genericArgs = before.Type.GetGenericArguments();
		// 		RegisterTypeRemapping(before.Type, after.Type, genericArgs, typesMapping);
		//
		//
		// 		var resultGeneric = genericArgs[genericArgs.Length - 1];
		//
		// 		var newGenerciArgs = after.Type.GetGenericArguments();
		// 		var newResultGeneric = newGenerciArgs[newGenerciArgs.Length - 1];
		//
		// 		var expectedType = ConstructType(resultGeneric, newResultGeneric, typesMapping);
		// 		if (expectedType != newResultGeneric)
		// 		{
		// 			after = Expression.Lambda(Expression.Convert(after.Body, expectedType),
		// 				after.Parameters);
		// 		}
		// 	}
		//
		// 	return after;
		// }

		public static LambdaExpression CorrectLambdaType(LambdaExpression before, LambdaExpression after)
		{
			if (typeof(IQueryable<>).IsSameOrParentOf(after.ReturnType))
			{
				if (!typeof(IQueryable<>).IsSameOrParentOf(before.ReturnType))
				{
					var convertType = typeof(IEnumerable<>).MakeGenericType(after.Body.Type.GetGenericArguments()[0]);

					after = Expression.Lambda(Expression.Convert(after.Body, convertType), after.Parameters);
				}
			}

			return after;
		}



		public static Expression ApplyReMapping(Expression expr, ReplaceInfo replaceInfo)
		{
			var newExpr = expr;
			switch (expr.NodeType)
			{
				case ExpressionType.Constant:
					{
						if (typeof(IQueryable<>).IsSameOrParentOf(expr.Type))
						{
							var cnt = (ConstantExpression)expr;
							var value = ((IQueryable)cnt.Value).Expression;
							if (value.NodeType != ExpressionType.Constant)
							{
								var newValue = ApplyReMapping(value, replaceInfo);
								if (newValue.Type != value.Type)
								{
									newExpr = newValue;
								}
							}
						}
						break;
					}
				case ExpressionType.Quote:
					{
						var unary = (UnaryExpression)expr;
						var newOperand = ApplyReMapping(unary.Operand, replaceInfo);
						newExpr = unary.Update(newOperand);
						break;
					}
				case ExpressionType.Lambda:
					{
						if (expr == replaceInfo.TargetLambda)
						{
							var lambdaExpression = (LambdaExpression)expr;

							if (typeof(IQueryable<>).IsSameOrParentOf(lambdaExpression.Body.Type))
							{
								// replacing body
								var neededKey     = GenerateKeyExpression(replaceInfo.Keys.ToArray(), 0);
								var itemType      = EagerLoading.GetEnumerableElementType(lambdaExpression.Body.Type);
								var parameter     = Expression.Parameter(itemType, "t");
								var genericMethod = _keyDataHolderMethodInfo.MakeGenericMethod(neededKey.Type, itemType);

								var selectMethod = _selectMethodInfo.MakeGenericMethod(itemType, genericMethod.ReturnType);
								var selectBody   = Expression.Call(genericMethod, neededKey, parameter);
								var newBody      = Expression.Call(selectMethod, lambdaExpression.Body, Expression.Lambda(selectBody, parameter));
								newExpr          = Expression.Lambda(newBody, lambdaExpression.Parameters);
							}
							else
							{
								// replacing body
								var neededKey     = GenerateKeyExpression(replaceInfo.Keys.ToArray(), 0);
								var genericMethod = _keyDataHolderMethodInfo.MakeGenericMethod(neededKey.Type, lambdaExpression.Body.Type);

								var newBody   = Expression.Call(genericMethod, neededKey, lambdaExpression.Body);
								newExpr       = Expression.Lambda(newBody, lambdaExpression.Parameters);
							}

							newExpr = CorrectLambdaType(lambdaExpression, (LambdaExpression)newExpr);

							// if (expr.Type.IsGenericType)
							// {
							// 	var typesMapping = new Dictionary<Type, Type>();
							// 	var genericArgs = expr.Type.GetGenericArguments();
							// 	RegisterTypeRemapping(expr.Type, newExpr.Type, genericArgs, typesMapping);
							//
							//
							// 	var resultGeneric = genericArgs[genericArgs.Length - 1];
							//
							// 	var newGenerciArgs = newExpr.Type.GetGenericArguments();
							// 	var newResultGeneric = newGenerciArgs[newGenerciArgs.Length - 1];
							//
							// 	var expectedType = ConstructType(resultGeneric, newResultGeneric, typesMapping);
							// 	if (expectedType != newResultGeneric)
							// 	{
							// 		var newLambda = (LambdaExpression)newExpr;
							// 		newExpr = Expression.Lambda(Expression.Convert(newLambda.Body, expectedType),
							// 			newLambda.Parameters);
							// 	}
							// }

						}
						break;
					}
				case ExpressionType.Call:
					{
						var mc = (MethodCallExpression)expr;
						if (false && mc.IsQueryable("SelectMany"))
						{
							// public static IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(
							//        this IQueryable<TSource> source,
							//        Expression<Func<TSource, IEnumerable<TCollection>>> collectionSelector,
							//        Expression<Func<TSource, TCollection, TResult>> resultSelector
							// );

						}
						else 
						{
							if (mc.IsQueryable())
							{
								var methodGenericArgumets   = mc.Method.GetGenericArguments();
								var methodGenericDefinition = mc.Method.GetGenericMethodDefinition();
								var genericArguments        = methodGenericDefinition.GetGenericArguments();
								var genericParameters       = methodGenericDefinition.GetParameters();
								var typesMapping            = new Dictionary<Type, Type>();
								var newArguments            = mc.Arguments.ToArray();
								var methodNeedsUpdate       = false;
								for (int i = 0; i < mc.Arguments.Count; i++)
								{
									var arg = mc.Arguments[i];
									var newArg = ApplyReMapping(arg, replaceInfo);
									if (arg.Type != newArg.Type)
									{
										methodNeedsUpdate = true;
										RegisterTypeRemapping(genericParameters[i].ParameterType, newArg.Type,
											genericArguments, typesMapping);

										newArguments[i] = newArg;
									}
									else
									{
										arg = arg.Unwrap();
										if (arg.NodeType == ExpressionType.Lambda && typesMapping.Count > 0)
										{
											var currentLambdaTemplateParams = genericParameters[i];
											var templateLambdaType          = currentLambdaTemplateParams.ParameterType;
											var ga                          = templateLambdaType.GetGenericArguments()[0].GetGenericArguments();
											var argLambda                   = (LambdaExpression)arg;
											var newParameters               = argLambda.Parameters.ToArray();
											var newBody                     = argLambda.Body;
											var needsUpdate                 = false;
											ParameterExpression transientParam = null;
											for (int j = 0; j < argLambda.Parameters.Count; j++)
											{
												var prm = argLambda.Parameters[j];
												var genericType = ga[j];
												if (typesMapping.TryGetValue(genericType, out var replacedType))
												{
													if (replacedType != prm.Type)
													{
														needsUpdate = true;

														if (typeof(KDH<,>).IsSameOrParentOf(replacedType))
														{
															var newParam = Expression.Parameter(replacedType, prm.Name);
															transientParam = newParam;
															newParameters[j] = newParam;

															var accessExpr =
																Expression.PropertyOrField(newParam, "Data");
															newBody = newBody.Transform(e => e == prm ? accessExpr : e);
														}
														else if (typeof(IGrouping<,>).IsSameOrParentOf(replacedType))
														{
															//TODO: Support Grouping???
															return expr;
															var newParam = Expression.Parameter(replacedType, prm.Name);
															transientParam = newParam;
															newParameters[j] = newParam;

															var accessExpr =
																Expression.PropertyOrField(Expression.PropertyOrField(newParam, "Key"), "Data");
															newBody = newBody.Transform(e =>
															{
																if (e == prm)
																{
																	return newParam;
																}

																return e;
															});															
														}
													}
												}
											}

											if (needsUpdate)
											{
												methodNeedsUpdate = true;
												var resultTemplateParam = ga[ga.Length - 1];

												if (typesMapping.TryGetValue(resultTemplateParam, out var replacedType))
												{
													throw new NotImplementedException();
												}
												else
												{
													if (transientParam != null)
													{
														// replacing body
														if (typeof(IEnumerable<>).IsSameOrParentOf(resultTemplateParam))
														{
															var neededKey     = Expression.PropertyOrField(transientParam, "Key");
															var itemType      = EagerLoading.GetEnumerableElementType(newBody.Type);
															var parameter     = Expression.Parameter(itemType, "t");
															var genericMethod = _keyDataHolderMethodInfo.MakeGenericMethod(neededKey.Type, itemType);

															var selectMethod = _selectMethodInfo.MakeGenericMethod(itemType, genericMethod.ReturnType);
															var selectBody   = Expression.Call(genericMethod, neededKey, parameter);
															newBody          = Expression.Call(selectMethod, newBody, Expression.Lambda(selectBody, parameter));
														}
														else
														{
															var neededKey     = Expression.PropertyOrField(transientParam, "Key");
															var genericMethod = _keyDataHolderMethodInfo.MakeGenericMethod(neededKey.Type, newBody.Type);
															newBody   = Expression.Call(genericMethod, neededKey, newBody);
														}

													}
												}

												var newArgLambda = Expression.Lambda(newBody, newParameters);

												newArgLambda = CorrectLambdaType(argLambda, newArgLambda);
												
												RegisterTypeRemapping(templateLambdaType.GetGenericArguments()[0], newArgLambda.Type, genericArguments, typesMapping);

												newArguments[i] = newArgLambda;
											}
										}
									}
								}
									
								if (methodNeedsUpdate)
								{
									var newGenericArgumets = genericArguments.Select((t, i) =>
									{
										if (typesMapping.TryGetValue(t, out var replaced))
											return replaced;
										return methodGenericArgumets[i];
									}).ToArray();

									var newMethodInfo = methodGenericDefinition.MakeGenericMethod(newGenericArgumets);
									newExpr = Expression.Call(newMethodInfo, newArguments);
								}
							}
						}

						break;
					}
			}

			return newExpr;
		}

		static void RegisterTypeRemapping(Type templateType, Type replaced, Type[] templateArguments, Dictionary<Type, Type> typeMappings)
		{
			if (templateType.IsGenericType)
			{
				var currentTemplateArguments = templateType.GetGenericArguments();

				var replacedArguments = replaced.GetGenericArguments();
				for (int i = 0; i < currentTemplateArguments.Length; i++)
				{
					RegisterTypeRemapping(currentTemplateArguments[i], replacedArguments[i], templateArguments, typeMappings);
				}
			}
			else
			{
				var idx = Array.IndexOf(templateArguments, templateType);
				if (idx >= 0)
				{
					if (!typeMappings.TryGetValue(templateType, out var value))
					{
						typeMappings.Add(templateType, replaced);
					}
					else
					{
						if (value != replaced)
							throw new InvalidOperationException();
					}
				}
			}
		}

		static Type ConstructType(Type templateType, Type currentType, Dictionary<Type, Type> typeMappings)
		{
			if (templateType.IsGenericType)
			{
				var templateArguments = templateType.GetGenericArguments();
				var currentArguments  = currentType.GetGenericArguments();
				var newArgumets       = new Type[templateArguments.Length];
				for (int i = 0; i < templateArguments.Length; i++)
				{
					newArgumets[i] = ConstructType(templateArguments[i], currentArguments[i], typeMappings);
				}

				return templateType.GetGenericTypeDefinition().MakeGenericType(newArgumets);
			}

			if (!typeMappings.TryGetValue(templateType, out var replaced))
				replaced = currentType;
			return replaced;
		}

	}
}
