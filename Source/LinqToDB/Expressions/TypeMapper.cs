using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	using System.ComponentModel;
	using System.Diagnostics.CodeAnalysis;
	using Common;
	using LinqToDB.Extensions;

	public sealed class TypeMapper
	{
		private static readonly Type[] _wrapperContructorParameters2 = new[] { typeof(object), typeof(TypeMapper) };
		private static readonly Type[] _wrapperContructorParameters3 = new[] { typeof(object), typeof(TypeMapper), typeof(Delegate[]) };

		// [type name] = originalType
		private readonly IDictionary<string, Type>              _types                   = new Dictionary<string, Type>();

		// [wrapperType] = originalType?
		readonly Dictionary<Type, Type?>                        _typeMappingCache        = new Dictionary<Type, Type?>();
		// [originalType] = wrapperType
		readonly Dictionary<Type, Type>                         _typeMappingReverseCache = new Dictionary<Type, Type>();
		readonly Dictionary<LambdaExpression, LambdaExpression> _lambdaMappingCache      = new Dictionary<LambdaExpression, LambdaExpression>();
		readonly Dictionary<Type, Func<object, object>>         _wrapperFactoryCache     = new Dictionary<Type, Func<object, object>>();

		private bool _finalized;

		public void RegisterTypeWrapper<TWrapper>(Type originalType)
		{
			RegisterTypeWrapper(typeof(TWrapper), originalType);
		}

		public void RegisterTypeWrapper(Type wrapperType, Type originalType)
		{
			if (_finalized)
				throw new LinqToDBException($"Wrappers registration is not allowed after {nameof(FinalizeMappings)}() call");

			if (wrapperType.Name != originalType.Name)
				throw new LinqToDBException($"Original and wraped types should have same type name. {wrapperType.Name} != {originalType.Name}");
			if (_types.ContainsKey(originalType.Name))
				throw new LinqToDBException($"Type with name {originalType.Name} already registered in mapper");

			_types                  .Add(originalType.Name, originalType);
			_typeMappingCache       .Add(wrapperType      , originalType);
			_typeMappingReverseCache.Add(originalType     , wrapperType);

			if (typeof(TypeWrapper).IsSameOrParentOf(wrapperType))
			{
			}
			else if (wrapperType.GetCustomAttributes(typeof(WrapperAttribute), true).Any())
			{
			}
			else
				throw new LinqToDBException($"Type {wrapperType} should inherit from {typeof(TypeWrapper)} or marked with {typeof(WrapperAttribute)} attribute");
		}

		public void FinalizeMappings()
		{
			if (_finalized)
				throw new LinqToDBException($"{nameof(FinalizeMappings)}() cannot be called multiple times");

			foreach (var wrapperType in _typeMappingCache.Keys.Where(t => typeof(TypeWrapper).IsSameOrParentOf(t)).ToList())
			{
				// pre-build delegates
				var wrappers = wrapperType.GetProperty("Wrappers", BindingFlags.Static | BindingFlags.NonPublic);
				Delegate[]? delegates = null;
				if (wrappers != null)
					delegates = ((LambdaExpression[])wrappers.GetValue(null)).Select(expr =>
					{
						try
						{
							return BuildWrapper(expr);
						}
						catch
						{
							throw;
							// right now it is valid case (see npgsql BinaryImporter)
							// probably we should make it more explicit by providing canFail flag for lambda
							//return null!;
						}
					}).ToArray();

				// pre-register factory, so we don't need to use concurrent dictionary to access factory later
				var types = wrappers != null ? _wrapperContructorParameters3 : _wrapperContructorParameters2;
				var ctor = wrapperType.GetConstructor(types);

				if (ctor == null)
					throw new LinqToDBException($"Cannot find contructor ({string.Join(", ", types.Select(t => t.ToString()))}) in type {wrapperType}");

				var pInstance = Expression.Parameter(typeof(object));

				var factory = Expression
					.Lambda<Func<object, object>>(
						wrappers != null
							? Expression.New(ctor, pInstance, Expression.Constant(this), Expression.Constant(delegates))
							: Expression.New(ctor, pInstance, Expression.Constant(this)),
						pInstance)
					.Compile();

				_wrapperFactoryCache.Add(wrapperType, factory);
			}

			// TODO: add enum mappers generation

			_finalized = true;
		}

		private bool TryMapType(Type type, [NotNullWhen(true)] out Type? replacement)
		{
			if (_typeMappingCache.TryGetValue(type, out replacement))
				return replacement != null;

			_typeMappingCache.Add(type, null);
			return false;
		}

		private bool TryMapValue(object? value, [NotNullWhen(true)] out object? replacement)
		{
			replacement = value;
			if (value == null)
				return false;

			var valueType = value.GetType();
			if (TryMapType(valueType, out var replacementType))
			{
				if (replacementType.IsEnum)
				{
					replacement = Enum.Parse(replacementType, value.ToString(), true);
					return true;
				}

				throw new LinqToDBException($"Only enums convert automatically");
			}

			return false;
		}

		private static MethodInfo _getNameMethodInfo      = MemberHelper.MethodOf(() => Enum.GetName(null, null));
		private static MethodInfo _wrapInstanceMethodInfo = MemberHelper.MethodOf<TypeMapper>(t => t.Wrap(null!, null));

		private Expression BuildValueMapper(ExpressionGenerator generator, Expression expression)
		{
			var valueType = expression.Type;
			if (!TryMapType(valueType, out var replacementType))
				return expression;

			if (!replacementType.IsEnum)
				throw new LinqToDBException("Only enums converted automatically.");

			var result = generator.MapExpression((object val) => Enum.Parse(replacementType, val.ToString()), expression);

			return result;
		}

		private Expression BuildValueMapperToType<TTarget>(ExpressionGenerator generator, Expression expression)
		{
			var valueType = expression.Type;
			var toType    = typeof(TTarget);

			if (!toType.IsEnum)
				throw new LinqToDBException("Only enums converted automatically.");

			var result = generator.MapExpression((object val) => (TTarget)Enum.Parse(toType, val.ToString()), false, expression);

			return result;
		}

		private Type MakeReplacement(Type type)
		{
			return TryMapType(type, out var replacement) ? replacement : type;
		}

		LambdaExpression MapLambdaInternal(LambdaExpression lambda, bool mapConvert = false, bool convertResult = true)
		{
			if (_lambdaMappingCache.TryGetValue(lambda, out var mappedLambda))
				return mappedLambda;

			var newParameters = lambda.Parameters
				.Select(p => TryMapType(p.Type, out var replacement) ? Expression.Parameter(replacement, p.Name) : p)
				.ToArray();

			MemberInfo ReplaceMember(MemberInfo memberInfo, Type targetType)
			{
				var newMembers = targetType.GetMember(memberInfo.Name);
				if (newMembers.Length == 0)
					throw new LinqToDBException($"There is no member '{memberInfo.Name}' in type '{targetType.FullName}'");
				if (newMembers.Length > 1)
					throw new LinqToDBException($"Ambiguous member '{memberInfo.Name}' in type '{targetType.FullName}'");
				return newMembers[0];
			}

			Expression? ReplaceTypes(Expression? expression)
			{
				if (expression == null)
					return null;

				var converted = expression.Transform(e =>
				{
					switch (e.NodeType)
					{
						case ExpressionType.Convert  :
							{
								if (!mapConvert)
									break;

								var ue   = (UnaryExpression)e;
								var expr = ReplaceTypes(ue.Operand)!;
								var type = TryMapType(ue.Type, out var newType) ? newType : ue.Type;

								if (ue.Method != null)
								{
									if (TryMapType(ue.Method.DeclaringType, out var replacement))
									{
										var types = ue.Method.GetParameters()
											.Select(p => MakeReplacement(p.ParameterType))
											.ToArray();

										// op_Explicit overloads by return type...
										var method = replacement.GetMethodEx(MakeReplacement(ue.Method.ReturnType), ue.Method.Name, types);

										if (method == null)
										{
											var name = replacement.FullName + "." + ue.Method.Name + "(" +
													   string.Join(", ", types.Select(t => t.Name)) + ")";
											throw new LinqToDBException($"Method not found in target type: {name}");
										}

										return Expression.Convert(expr, type, method);
									}

									return ue;
								}

								if (expr.Type == type)
									return expr;

								if (ue.Type != type)
									return Expression.Convert(expr, type);

								break;
							}

						case ExpressionType.Assign:
							{
								var be    = (BinaryExpression)e;
								var left  = ReplaceTypes(be.Left)!;
								var right = be.Right;

								if (TryMapType(right.Type, out var replacement))
								{
									if (right.NodeType == ExpressionType.Constant
										&& right.EvaluateExpression() is TypeWrapper wrapper)
									{
										right = Expression.Constant(wrapper.instance_);
									}
									else if (replacement.IsEnum)
									{
										right = Expression.Convert(
											Expression.Call(
												typeof(Enum),
												nameof(Enum.Parse),
												Array<Type>.Empty,
												Expression.Constant(replacement),
												Expression.Call(right, nameof(Enum.ToString), Array<Type>.Empty)),
											replacement);
									}
								}

								return Expression.Assign(left, right);
							}

						case ExpressionType.Parameter:
							{
								var idx = lambda.Parameters.IndexOf((ParameterExpression)e);
								if (idx >= 0)
									return newParameters[idx];
								break;
							}
						case ExpressionType.MemberAccess:
							{
								var ma = (MemberExpression)e;
								if (TryMapType(ma.Expression.Type, out var replacement))
								{
									var expr = ReplaceTypes(ma.Expression)!;
									if (expr.Type != replacement)
										throw new LinqToDBException($"Invalid replacement of '{ma.Expression}' to type '{replacement.FullName}'.");

									var prop = replacement.GetProperty(ma.Member.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
									if (prop == null)
										throw new LinqToDBException($"Property not found in target type: {replacement.FullName}.{ma.Member.Name}");
									return Expression.MakeMemberAccess(expr, prop);
								}

								if (TryMapType(ma.Type, out replacement))
								{
									if (ma.Expression.NodeType == ExpressionType.Constant
										&& ma.EvaluateExpression() is TypeWrapper wrapper)
									{
										return Expression.Constant(wrapper.instance_);
									}
									else if (replacement.IsEnum)
									{
										return Expression.Convert(
											Expression.Call(
												typeof(Enum),
												nameof(Enum.Parse),
												Array<Type>.Empty,
												Expression.Constant(replacement),
												Expression.Call(ma, nameof(Enum.ToString), Array<Type>.Empty)),
											replacement);
									}
								}

								break;
							}
						case ExpressionType.New:
							{
								var ne = (NewExpression)e;
								if (TryMapType(ne.Type, out var replacement))
								{
									var paramTypes = ne.Constructor.GetParameters()
										.Select(p => TryMapType(p.ParameterType, out var newType) ? newType : p.ParameterType)
										.ToArray();

									var ctor = replacement.GetConstructor(paramTypes);

									if (ctor == null)
									{
										var name = replacement.FullName + "." + ne.Constructor.Name + "(" +
										           string.Join(", ", paramTypes.Select(t => t.Name)) + ")";
										throw new LinqToDBException($"Constructor not found in target type: {name}");
									}

									var newArguments  = ne.Arguments.Select(ReplaceTypes);
									if (ne.Members != null)
									{
										var newMembers = ne.Members.Select(m => ReplaceMember(m, replacement));
										var newExpression = Expression.New(ctor, newArguments, newMembers);
										return newExpression;
									}
									else
									{
										var newExpression = Expression.New(ctor, newArguments);
										return newExpression;
									}
								}
								break;
							}
						case ExpressionType.MemberInit:
							{
								var mi = (MemberInitExpression)e;
								if (TryMapType(mi.Type, out var replacement))
								{
									var newExpression = (NewExpression)ReplaceTypes(mi.NewExpression)!;
									var newBindings = mi.Bindings.Select(b =>
									{
										switch (b.BindingType)
										{
											case MemberBindingType.Assignment:
												{
													var mab = (MemberAssignment)b;
													return Expression.Bind(ReplaceMember(mab.Member, replacement),
														ReplaceTypes(mab.Expression));
												}
											case MemberBindingType.MemberBinding:
												{
													throw new NotImplementedException();
												}
											case MemberBindingType.ListBinding:
												{
													throw new NotImplementedException();
												}
											default:
												throw new ArgumentOutOfRangeException();
										}
									});

									var newMemberInit = Expression.MemberInit(newExpression, newBindings);
									return newMemberInit;
								}
								break;
							}
						case ExpressionType.Call:
							{
								var mc = (MethodCallExpression)e;

								if (TryMapType(mc.Method.DeclaringType, out var replacement))
								{
									var types = mc.Method.GetParameters()
										.Select(p => MakeReplacement(p.ParameterType))
										.ToArray();

									if (mc.Method.IsGenericMethod)
									{
										// typeArgs replacements not implemented now, as we don't have usecases for it
										var typeArgs = mc.Method.GetGenericArguments();
										var method   = replacement.GetMethodEx(mc.Method.Name, typeArgs.Length, types);

										if (method == null)
										{
											var name = replacement.FullName + "." + mc.Method.Name + "<" +
													   string.Join(", ", typeArgs.Select(t => t.Name))+ ">(" +
													   string.Join(", ", types.Select(t => t.Name)) + ")";
											throw new LinqToDBException($"Method not found in target type: {name}");
										}

										var newArguments  = mc.Arguments.Select(ReplaceTypes);
										var newMethodCall = Expression.Call(ReplaceTypes(mc.Object), mc.Method.Name, typeArgs, newArguments.ToArray());
										return newMethodCall;
									}
									else
									{
										var method = replacement.GetMethod(mc.Method.Name, types);

										if (method == null)
										{
											var name = replacement.FullName + "." + mc.Method.Name + "(" +
													   string.Join(", ", types.Select(t => t.Name)) + ")";
											throw new LinqToDBException($"Method not found in target type: {name}");
										}

										var newArguments = mc.Arguments.Select(ReplaceTypes);
										var newMethodCall = Expression.Call(ReplaceTypes(mc.Object), method, newArguments);
										return newMethodCall;
									}
								}

								break;
							}
					}

					return e;
				});
				return converted;
			}

			var convertedBody = ReplaceTypes(lambda.Body)!;

			if (convertResult && _typeMappingReverseCache.TryGetValue(convertedBody.Type, out var wrapperType) && wrapperType.IsEnum)
				convertedBody = Expression.Convert(
					Expression.Call(
						typeof(Enum),
						nameof(Enum.Parse),
						Array<Type>.Empty,
						Expression.Constant(wrapperType),
						Expression.Call(convertedBody, nameof(Enum.ToString), Array<Type>.Empty)),
					wrapperType);

			mappedLambda = Expression.Lambda(convertedBody, newParameters);

			_lambdaMappingCache.Add(lambda, mappedLambda);
			return mappedLambda;
		}

		private Expression MapExpressionInternal(LambdaExpression lambdaExpression, params Expression[] parameters)
		{
			return MapExpressionInternal(lambdaExpression, true, parameters);
		}

		private Expression MapExpressionInternal(LambdaExpression lambdaExpression, bool mapConvert, params Expression[] parameters)
		{
			if (lambdaExpression.Parameters.Count != parameters.Length)
				throw new LinqToDBException($"Parameters count is different: {lambdaExpression.Parameters.Count} != {parameters.Length}.");

			var lambda = MapLambdaInternal(lambdaExpression, mapConvert);
			var expr   = lambda.Body.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Parameter)
				{
					var idx = lambda.Parameters.IndexOf((ParameterExpression)e);
					if (idx >= 0)
						return parameters[idx];
				}

				return e;
			});

			return expr;
		}

		LambdaExpression CorrectLambdaParameters(LambdaExpression lambda, Type? resultType, params Type[] paramTypes)
		{
			if (lambda.Parameters.Count != paramTypes.Length)
				throw new LinqToDBException("Invalid count of types.");

			var parameters = new ParameterExpression[paramTypes.Length];
			var generator  = new ExpressionGenerator(this);

			for (int i = 0; i < paramTypes.Length; i++)
			{
				var parameter = lambda.Parameters[i];
				if (paramTypes[i] != parameter.Type)
				{
					var variable  = generator.AddVariable(parameter);
					parameters[i] = Expression.Parameter(paramTypes[i], parameter.Name);
					generator.Assign(variable, parameters[i]);
				}
				else
				{
					parameters[i] = parameter;
				}
			}

			var body = lambda.Body;
			if (resultType != null && body.Type != resultType)
			{
				body = Expression.Convert(body, resultType);
			}

			generator.AddExpression(body);
			var newBody = generator.Build();

			return Expression.Lambda(newBody, parameters);
		}

		#region MapExpression

		public Expression MapExpression<TR>(Expression<Func<TR>> func)
			=> MapExpressionInternal(func);

		public Expression MapExpression<T, TR>(Expression<Func<T, TR>> func, Expression p)
			=> MapExpressionInternal(func, p);

		public Expression MapExpression<T1, T2, TR>(Expression<Func<T1, T2, TR>> func, Expression p1, Expression p2) 
			=> MapExpressionInternal(func, p1, p2);

		public Expression MapExpression<T1, T2, T3, TR>(Expression<Func<T1, T2, T3, TR>> func, Expression p1, Expression p2, Expression p3) 
			=> MapExpressionInternal(func, p1, p2, p3);

		public Expression MapExpression<T1, T2, T3, T4, TR>(Expression<Func<T1, T2, T3, T4, TR>> func, Expression p1, Expression p2, Expression p3, Expression p4) 
			=> MapExpressionInternal(func, p1, p2, p3, p4);

		public Expression MapExpression<T1, T2, T3, T4, T5, TR>(Expression<Func<T1, T2, T3, T4, T5, TR>> func, Expression p1, Expression p2, Expression p3, Expression p4, Expression p5) 
			=> MapExpressionInternal(func, p1, p2, p3, p4, p5);

		public Expression MapExpression<TR>(Expression<Func<TR>> func, bool mapConvert)
			=> MapExpressionInternal(func, mapConvert);

		public Expression MapExpression<T, TR>(Expression<Func<T, TR>> func, bool mapConvert, Expression p)
			=> MapExpressionInternal(func, mapConvert, p);

		public Expression MapExpression<T1, T2, TR>(Expression<Func<T1, T2, TR>> func, bool mapConvert, Expression p1, Expression p2)
			=> MapExpressionInternal(func, mapConvert, p1, p2);

		public Expression MapExpression<T1, T2, T3, TR>(Expression<Func<T1, T2, T3, TR>> func, bool mapConvert, Expression p1, Expression p2, Expression p3)
			=> MapExpressionInternal(func, mapConvert, p1, p2, p3);

		public Expression MapExpression<T1, T2, T3, T4, TR>(Expression<Func<T1, T2, T3, T4, TR>> func, bool mapConvert, Expression p1, Expression p2, Expression p3, Expression p4)
			=> MapExpressionInternal(func, mapConvert, p1, p2, p3, p4);

		public Expression MapExpression<T1, T2, T3, T4, T5, TR>(Expression<Func<T1, T2, T3, T4, T5, TR>> func, bool mapConvert, Expression p1, Expression p2, Expression p3, Expression p4, Expression p5)
			=> MapExpressionInternal(func, mapConvert, p1, p2, p3, p4, p5);
		#endregion

		#region MapAction

		public Expression MapAction(Expression<Action> action, bool mapConvert)
			=> MapExpressionInternal(action, mapConvert);

		public Expression MapAction<T>(Expression<Action<T>> action, bool mapConvert, Expression p)
			=> MapExpressionInternal(action, mapConvert, p);

		public Expression MapAction<T1, T2>(Expression<Action<T1, T2>> action, bool mapConvert, Expression p1, Expression p2)
			=> MapExpressionInternal(action, mapConvert, p1, p2);

		public Expression MapAction<T1, T2, T3>(Expression<Action<T1, T2, T3>> action, bool mapConvert, Expression p1, Expression p2, Expression p3)
			=> MapExpressionInternal(action, mapConvert, p1, p2, p3);

		public Expression MapAction<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> action, bool mapConvert, Expression p1, Expression p2, Expression p3, Expression p4)
			=> MapExpressionInternal(action, mapConvert, p1, p2, p3, p4);

		public Expression MapAction<T1, T2, T3, T4, T5>(Expression<Action<T1, T2, T3, T4, T5>> action, bool mapConvert, Expression p1, Expression p2, Expression p3, Expression p4, Expression p5)
			=> MapExpressionInternal(action, mapConvert, p1, p2, p3, p4, p5);
		#endregion

		#region MapLambda

		public LambdaExpression MapLambda<T, TR>(Expression<Func<T, TR>> func) => MapLambdaInternal(func, true);
		public LambdaExpression MapLambda<T1, T2, TR>(Expression<Func<T1, T2, TR>> func) => MapLambdaInternal(func, true);
		public LambdaExpression MapLambda<T1, T2, T3, TR>(Expression<Func<T1, T2, T3, TR>> func) => MapLambdaInternal(func, true);
		public LambdaExpression MapLambda<T1, T2, T3, T4, TR>(Expression<Func<T1, T2, T3, T4, TR>> func) => MapLambdaInternal(func, true);
		public LambdaExpression MapLambda<T1, T2, T3, T4, T5, TR>(Expression<Func<T1, T2, T3, T4, T5, TR>> func) => MapLambdaInternal(func, true);

		#endregion

		#region MapActionLambda

		public LambdaExpression MapActionLambda(Expression<Action> action) => MapLambdaInternal(action);
		public LambdaExpression MapActionLambda<T>(Expression<Action<T>> action) => MapLambdaInternal(action);
		public LambdaExpression MapActionLambda<T1, T2>(Expression<Action<T1, T2>> action) => MapLambdaInternal(action);
		public LambdaExpression MapActionLambda<T1, T2, T3>(Expression<Action<T1, T2, T3>> action) => MapLambdaInternal(action);
		public LambdaExpression MapActionLambda<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> action) => MapLambdaInternal(action);
		public LambdaExpression MapActionLambda<T1, T2, T3, T4, T5>(Expression<Action<T1, T2, T3, T4, T5>> action) => MapLambdaInternal(action);

		#endregion

		#region BuildFunc

		public Func<TR> BuildFunc<TR>(LambdaExpression lambda) => 
			(Func<TR>)CorrectLambdaParameters(lambda, typeof(TR)).Compile();

		public Func<T, TR> BuildFunc<T, TR>(LambdaExpression lambda) =>
			(Func<T, TR>)CorrectLambdaParameters(lambda, typeof(TR), typeof(T)).Compile();

		public Func<T1, T2, TR> BuildFunc<T1, T2, TR>(LambdaExpression lambda) => 
			(Func<T1, T2, TR>)CorrectLambdaParameters(lambda, typeof(TR), typeof(T1), typeof(T2)).Compile();

		public Func<T1, T2, T3, TR> BuildFunc<T1, T2, T3, TR>(LambdaExpression lambda) => 
			(Func<T1, T2, T3, TR>)CorrectLambdaParameters(lambda, typeof(TR), typeof(T1), typeof(T2), typeof(T3)).Compile();

		public Func<T1, T2, T3, T4, TR> BuildFunc<T1, T2, T3, T4, TR>(LambdaExpression lambda) => 
			(Func<T1, T2, T3, T4, TR>)CorrectLambdaParameters(lambda, typeof(TR), typeof(T1), typeof(T2), typeof(T3), typeof(T4)).Compile();

		public Func<T1, T2, T3, T4, T5, TR> BuildFunc<T1, T2, T3, T4, T5, TR>(LambdaExpression lambda) => 
			(Func<T1, T2, T3, T4, T5, TR>)CorrectLambdaParameters(lambda, typeof(TR), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)).Compile();

		#endregion

		#region BuildAction

		public Action BuildAction(LambdaExpression lambda) => 
			(Action)CorrectLambdaParameters(lambda, null).Compile();

		public Action<T> BuildAction<T>(LambdaExpression lambda) =>
			(Action<T>)CorrectLambdaParameters(lambda, null, typeof(T)).Compile();

		public Action<T1, T2> BuildAction<T1, T2>(LambdaExpression lambda) => 
			(Action<T1, T2>)CorrectLambdaParameters(lambda, null, typeof(T1), typeof(T2)).Compile();

		public Action<T1, T2, T3> BuildAction<T1, T2, T3>(LambdaExpression lambda) => 
			(Action<T1, T2, T3>)CorrectLambdaParameters(lambda, null, typeof(T1), typeof(T2), typeof(T3)).Compile();

		public Action<T1, T2, T3, T4> BuildAction<T1, T2, T3, T4>(LambdaExpression lambda) => 
			(Action<T1, T2, T3, T4>)CorrectLambdaParameters(lambda, null, typeof(T1), typeof(T2), typeof(T3), typeof(T4)).Compile();

		public Action<T1, T2, T3, T4, T5> BuildAction<T1, T2, T3, T4, T5>(LambdaExpression lambda) => 
			(Action<T1, T2, T3, T4, T5>)CorrectLambdaParameters(lambda, null, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)).Compile();

		#endregion

		#region MemberAccess

		public MemberExpression MemberAccess<T>(Expression<Func<T, object?>> memberExpression, Expression obj)
		{
			var expr = MapExpression(memberExpression, obj).Unwrap();
			return (MemberExpression)expr;
		}

		#endregion

		#region Setters

		public LambdaExpression MapSetter<T>(Expression<Func<T, object?>> propExpression,
			Expression<Func<object?>> valueExpression)
		{
			var propLambda  = MapLambdaInternal(propExpression);

			var valueLambda = MapLambdaInternal(valueExpression);

			var left  = propLambda.Body.Unwrap();
			var right = valueLambda.Body.Unwrap();

			if (right.Type != left.Type)
				right = Expression.Convert(right, left.Type);

			var assign = Expression.Assign(left, right);
			return Expression.Lambda(assign, propLambda.Parameters);
		}

		public LambdaExpression MapSetterValue<T, TV>(Expression<Func<T, TV>> propExpression, TV value)
		{
			var left = propExpression.Body.Unwrap();
			var right = (Expression)Expression.Constant(value);

			if (right.Type != left.Type)
				right = Expression.Convert(right, left.Type);

			var assign = Expression.Assign(left, right);
			return MapLambdaInternal(Expression.Lambda(assign, propExpression.Parameters));
		}

		public void SetValue<T>(object? instance, Expression<Func<T, object?>> propExpression, object? value)
		{
			var setterExpression = MapSetterValue(propExpression, value);
			setterExpression.Compile().DynamicInvoke(instance);
		}

		public void SetValue<T, TV>(object? instance, Expression<Func<T, TV>> propExpression, TV value)
		{
			var setterExpression = MapSetterValue(propExpression, value);
			setterExpression.Compile().DynamicInvoke(instance);
		}

		public class MemberBuilder<T, TV>
		{
			private readonly TypeMapper _mapper;
			private readonly Expression<Func<T, TV>> _memberExpression;

			public MemberBuilder(TypeMapper mapper, Expression<Func<T, TV>> memberExpression)
			{
				_mapper = mapper;
				_memberExpression = memberExpression;
			}

			public Expression<Action<TBase>> BuildSetterExpression<TBase>(TV value)
			{
				var setterExpression = _mapper.MapSetterValue(_memberExpression, value);

				var generator = new ExpressionGenerator(_mapper);

				var convertedType = setterExpression.Parameters[0].Type;

				var newParameter     = Expression.Parameter(typeof(TBase), setterExpression.Parameters[0].Name);
				var requiredVariable = generator.DeclareVariable(convertedType, "v");

				var replacedBody = setterExpression.GetBody(requiredVariable).Unwrap();

				generator.Assign(requiredVariable, newParameter);
				generator.AddExpression(replacedBody);

				var block = generator.Build();

				var resultExpression = Expression.Lambda<Action<TBase>>(block, newParameter);
				return resultExpression;
			}

			public Action<TBase> BuildSetter<TBase>(TV value)
			{
				var setterExpression = BuildSetterExpression<TBase>(value);

				return setterExpression.Compile();
			}

			public Expression<Action<TBase, TV>> BuildSetterExpression<TBase>()
			{
				var generator = new ExpressionGenerator(_mapper);

				var propLambda    = _mapper.MapLambdaInternal(_memberExpression, false, false);

				if (!_mapper.TryMapType(propLambda.Parameters[0].Type, out var convertedType))
					convertedType = propLambda.Parameters[0].Type;

				var newParameter     = Expression.Parameter(typeof(TBase), propLambda.Parameters[0].Name);
				var valueParameter   = Expression.Parameter(typeof(TV), "value");
				var requiredVariable = generator.DeclareVariable(convertedType, "v");

				generator.Assign(requiredVariable, newParameter);

				var left  = propLambda.GetBody(requiredVariable).Unwrap();
				var right = _mapper.BuildValueMapper(generator, valueParameter);

				generator.Assign(left, right);

				var generated = generator.Build();

				var resultLambda = Expression.Lambda<Action<TBase, TV>>(generated, newParameter, valueParameter);

				return resultLambda;
			}

			public Expression<Func<TBase, TV>> BuildGetterExpression<TBase>()
			{
				var generator = new ExpressionGenerator(_mapper);

				var propLambda    = _mapper.MapLambdaInternal(_memberExpression);
				var convertedType = propLambda.Parameters[0].Type;

				var newParameter     = Expression.Parameter(typeof(TBase), propLambda.Parameters[0].Name);
				var requiredVariable = generator.DeclareVariable(convertedType, "v");

				generator.Assign(requiredVariable, newParameter);

				var left  = propLambda.GetBody(requiredVariable);

				if (left.Type != typeof(TV))
					left = _mapper.BuildValueMapperToType<TV>(generator, left);

				generator.AddExpression(left);

				var generated = generator.Build();

				var resultLambda = Expression.Lambda<Func<TBase, TV>>(generated, newParameter);

				return resultLambda;
			}

			public Action<TBase, TV> BuildSetter<TBase>()
			{
				var resultLambda = BuildSetterExpression<TBase>();

				return resultLambda.Compile();
			}

			public Func<TBase, TV> BuildGetter<TBase>()
			{
				var resultLambda = BuildGetterExpression<TBase>();

				return resultLambda.Compile();
			}

			public Expression GetAccess(Expression instance)
			{
				var propLambda  = _mapper.MapLambdaInternal(_memberExpression);
				return propLambda.GetBody(instance);
			}
		}

		public class TypeBuilder<T>
		{
			private readonly TypeMapper _mapper;

			public TypeBuilder(TypeMapper mapper)
			{
				_mapper = mapper;
			}

			public MemberBuilder<T, TV> Member<TV>(Expression<Func<T, TV>> memberExpression)
			{
				return new MemberBuilder<T, TV>(_mapper, memberExpression);
			}
		}

		public TypeBuilder<T> Type<T>()
		{
			return new TypeBuilder<T>(this);
		}


		#endregion

		#region Throw

		private UnaryExpression MapThrowExpression(LambdaExpression lambdaExpression, params Expression[] parameters)
		{
			var exp = MapExpressionInternal(lambdaExpression, parameters);

			return Expression.Throw(exp);
		}

		public Expression Throw<TR>(Expression<Func<TR>> newExpr) => MapThrowExpression(newExpr);
		public Expression Throw<T1, TR>(Expression<Func<T1, TR>> newExpr, Expression p) => MapThrowExpression(newExpr, p);
		public Expression Throw<T1, T2, TR>(Expression<Func<T1, T2, TR>> newExpr, Expression p1, Expression p2) => MapThrowExpression(newExpr, p1, p2);
		public Expression Throw<T1, T2, T3, TR>(Expression<Func<T1, T2, T3, TR>> newExpr, Expression p1, Expression p2, Expression p3) => MapThrowExpression(newExpr, p1, p2, p3);

		#endregion

		#region BuildWrapFunc

		private static readonly object _wraperLock = new object();

		public void BuildWrapFunc<TI, TR>(ref Func<TI, TR>? func, Expression<Func<TI, TR>> wrapper)
			where TI : TypeWrapper
		{
			if (func == null)
				lock (_wraperLock)
					if (func == null)
						func = (Func<TI, TR>)BuildWrapper(wrapper);
		}

		public void BuildWrapFunc<TI, T1, TR>(ref Func<TI, T1, TR>? func, Expression<Func<TI, T1, TR>> wrapper)
			where TI : TypeWrapper
		{
			if (func == null)
				lock (_wraperLock)
					if (func == null)
						func = (Func<TI, T1, TR>)BuildWrapper(wrapper);
		}

		private Delegate BuildWrapper(LambdaExpression lambda)
		{
			// TODO: here are two optimizations that could be done to make generated action a bit faster:
			// 1. require caller to pass unwrapped instance, so we don't need to generate instance_ property access
			// 2. generate wrapper constructor call instead of Wrap method call (will need null check of wrapped value)
			if (!TryMapType(lambda.Parameters[0].Type, out var targetType))
				throw new LinqToDBException($"Wrapper type {lambda.Parameters[0].Type} is not registered");

			var pInstance = Expression.Parameter(typeof(object));
			var instance = Expression.Convert(pInstance, targetType);

			var mappedLambda = MapLambdaInternal(lambda, true);
			
			var parametersMap = new Dictionary<Expression, Expression>();
			for (var i = 0; i < lambda.Parameters.Count; i++)
			{
				var oldParameter = lambda.Parameters[i];

				if (TryMapType(oldParameter.Type, out var mappedType))
				{
					if (typeof(TypeWrapper).IsSameOrParentOf(oldParameter.Type))
						parametersMap.Add(mappedLambda.Parameters[i], Expression.Convert(Expression.Property(oldParameter, nameof(TypeWrapper.instance_)), mappedType));
					else if (oldParameter.Type.IsEnum)
						parametersMap.Add(mappedLambda.Parameters[i], Expression.Convert(
							Expression.Call(
								typeof(Enum),
								nameof(Enum.Parse),
								Array<Type>.Empty,
								Expression.Constant(mappedType),
								Expression.Call(oldParameter, nameof(Enum.ToString), Array<Type>.Empty)),
							mappedType));
				}
			}

			var expr = mappedLambda.Body.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Parameter && parametersMap.TryGetValue(e, out var replacement))
					return replacement;

				return e;
			});

			if (typeof(TypeWrapper).IsSameOrParentOf(lambda.ReturnType) && TryMapType(lambda.ReturnType, out var returnType))
			{
				expr = expr.Transform(e =>
				{
					if (e.Type == returnType)
						return Expression.Convert(Expression.Call(
							Expression.Constant(this),
							_wrapInstanceMethodInfo,
							Expression.Constant(lambda.ReturnType),
							e), lambda.ReturnType);

					return e;
				});
			}

			return Expression.Lambda(expr, lambda.Parameters).Compile();
		}

		#endregion

		[return: MaybeNull]
		public TR Wrap<T, TR>(T instance, Expression<Func<T, TR>> func)
			where T: TypeWrapper
		{
			var expr = MapExpressionInternal(func, Expression.Constant(instance.instance_));

			var result = expr.EvaluateExpression();

			if (result == null)
				return default!;

			if (typeof(TypeWrapper).IsSameOrParentOf(typeof(TR)))
			{
				return (TR)Wrap(typeof(TR), result);
			}

			return (TR)result;
		}

		public void WrapAction<T>(T instance, Expression<Action<T>> action)
			where T: TypeWrapper
		{
			var expr = MapExpressionInternal(action, Expression.Constant(instance.instance_));

			expr.EvaluateExpression();
		}

		public void WrapAction<T, T1>(T instance, Expression<Action<T, T1>> action)
			where T: TypeWrapper
		{
			var expr = MapExpressionInternal(action, Expression.Constant(instance.instance_));

			expr.EvaluateExpression();
		}

		public void WrapAction<T, T1, T2>(T instance, Expression<Action<T, T1, T2>> action)
			where T: TypeWrapper
		{
			var expr = MapExpressionInternal(action, Expression.Constant(instance.instance_));

			expr.EvaluateExpression();
		}

		[return: MaybeNull]
		public TR CreateAndWrap<TR>(Expression<Func<TR>> newFunc)
			where TR: TypeWrapper
		{
			if (newFunc == null) throw new ArgumentNullException(nameof(newFunc));

			var expr     = MapExpressionInternal(newFunc, true);
			var instance = expr.EvaluateExpression();

			return Wrap<TR>(instance);
		}

		[return: NotNullIfNotNull("instance")]
		public TR? Wrap<TR>(object? instance)
			where TR: TypeWrapper
		{
			if (instance == null)
				return null;

			return (TR)Wrap(typeof(TR), instance);
		}

		[return: NotNullIfNotNull("instance")]
		private object? Wrap(Type wrapperType, object? instance)
		{
			if (instance == null)
				return null;

			if (!_wrapperFactoryCache.TryGetValue(wrapperType, out var factory))
				throw new LinqToDBException($"Missing type wrapper factory registration for type {wrapperType}");

			return factory(instance);
		}

		public object? Evaluate<T>(object? instance, Expression<Func<T, object?>> func)
		{
			var expr = MapExpressionInternal(func, Expression.Constant(instance));
			return expr.EvaluateExpression();
		}

		public object? Evaluate<T>(T instance, Expression<Func<T, object?>> func)
			where T: TypeWrapper
		{
			var expr = MapExpressionInternal(func, Expression.Constant(instance.instance_));
			return expr.EvaluateExpression();
		}

		#region events
		public void MapEvent<TWrapper, TDelegate>(EventHandlerList events, object? instance, string eventName)
			where TWrapper : TypeWrapper
			where TDelegate : Delegate
		{
			if (!TryMapType(typeof(TWrapper), out var targetType))
				throw new InvalidOperationException();

			Type? delegateType = typeof(TDelegate);
			var invoke         = delegateType.GetMethod("Invoke");
			var returnType     = invoke.ReturnType;

			if (TryMapType(typeof(TDelegate), out delegateType))
				invoke = delegateType.GetMethod("Invoke");
			else
				delegateType = typeof(TDelegate);

			var lambdaReturnType = invoke.ReturnType;
			var parameterInfos   = invoke.GetParameters();
			var parameters       = new ParameterExpression[parameterInfos.Length];
			var parameterValues  = new Expression[parameterInfos.Length];

			for (var i = 0; i < parameterInfos.Length; i++)
			{
				if (!TryMapType(parameterInfos[i].ParameterType, out var parameterType))
					parameterType = parameterInfos[i].ParameterType;

				parameterValues[i] = parameters[i] = Expression.Parameter(parameterType);

				if (_typeMappingReverseCache.TryGetValue(parameterType, out var wrapperType))
					parameterValues[i] = MapExpression((object? value) => Wrap(wrapperType, value), parameterValues[i]);
			}

			var ei = targetType.GetEvent(eventName);

			var generator        = new ExpressionGenerator(this);
			var delegateVariable = generator.DeclareVariable(typeof(Delegate), "handler");

			generator.Assign(delegateVariable, MapExpression(() => events[eventName]));

			if (returnType != typeof(void))
				generator.Condition(
					MapExpression((Delegate? handler) => handler != null, delegateVariable),
					Expression.Convert(MapDynamicInvoke(delegateVariable, returnType, parameterValues), lambdaReturnType),
					Expression.Default(lambdaReturnType));
			else
				generator.IfThen(
					MapExpression((Delegate? handler) => handler != null, delegateVariable),
					MapDynamicInvoke(delegateVariable, null, parameterValues));

			var generated    = generator.Build();
			var resultLambda = Expression.Lambda(delegateType, generated, parameters);

			ei.AddEventHandler(instance, resultLambda.Compile());

			Expression MapDynamicInvoke(Expression delegateValue, Type? returnType, Expression[] parameters)
			{
				var paramExpressions = new ParameterExpression[parameters.Length + 1];
				var paramValues      = new Expression         [parameters.Length + 1];

				paramExpressions[0]  = Expression.Parameter(typeof(Delegate));
				paramValues     [0]  = delegateValue;

				for (var i = 0; i < parameters.Length; i++)
				{
					paramExpressions[i + 1] = Expression.Parameter(typeof(object));
					paramValues     [i + 1] = parameters[i];
				}

				Expression expr = Expression.Call(
					paramExpressions[0],
					typeof(Delegate).GetMethod(nameof(Delegate.DynamicInvoke)),
					Expression.NewArrayInit(typeof(object), paramExpressions.Skip(1)));

				expr = MapExpressionInternal(Expression.Lambda(expr, paramExpressions), paramValues);

				if (returnType != null && typeof(TypeWrapper).IsSameOrParentOf(returnType))
					expr = Expression.Property(Expression.Convert(expr, returnType), nameof(TypeWrapper.instance_));

				return expr;
			}
		}

		#endregion
	}
}
