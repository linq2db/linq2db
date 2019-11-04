using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace LinqToDB.Expressions
{
	using System.Diagnostics.CodeAnalysis;
	using Common;
	using LinqToDB.Extensions;

	public sealed class TypeMapper
	{
		public Type[] Types { get; }

		readonly Dictionary<Type, Type?>                        _typeMappingCache   = new Dictionary<Type, Type?>();
		readonly Dictionary<LambdaExpression, LambdaExpression> _lambdaMappingCache = new Dictionary<LambdaExpression, LambdaExpression>();

		public TypeMapper([NotNull] params Type[] types)
		{
			Types = types ?? throw new ArgumentNullException(nameof(types));
		}

		public Type FindReplacement(Type type)
		{
			return Types.FirstOrDefault(t => t.Name == type.Name);
		}

		private bool TryMapType(Type type, [NotNullWhen(true)] out Type? replacement)
		{
			if (_typeMappingCache.TryGetValue(type, out replacement))
				return replacement != null;

			if (typeof(TypeWrapper).IsSameOrParentOf(type) || type.GetCustomAttributes(typeof(WrapperAttribute), true).Any())
			{
				replacement = FindReplacement(type);
				if (replacement == null)
					throw new LinqToDBException($"Not registered replacement for type {type.Name}");
			}

			_typeMappingCache.Add(type, replacement);
			
			return replacement != null;
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
					var enumName = Enum.GetName(valueType, value);
					if (enumName.IsNullOrEmpty())
						throw new LinqToDBException($"Enum value '{value}' does not have name.");
					replacement = Enum.Parse(replacementType, enumName, true);
					return true;
				}

				throw new LinqToDBException($"Only enums convert automatically");
			}

			return false;
		}

		private static MethodInfo _getNameMethodInfo = MemberHelper.MethodOf(() => Enum.GetName(null, null));

		private Expression BuildValueMapper(ExpressionGenerator generator, Expression expression)
		{
			var valueType = expression.Type;
			if (!TryMapType(valueType, out var replacementType))
				return expression;

			if (!replacementType.IsEnum)
				throw new LinqToDBException("Only enums converted automatically.");

			var nameExpr = Expression.Call(_getNameMethodInfo, Expression.Constant(valueType),
				Expression.Convert(expression, typeof(object)));
			var nameVar  = generator.DeclareVariable(typeof(string), "enumName");

			generator.Assign(nameVar, nameExpr);
			generator.IfThen(MapExpression((string s) => s.IsNullOrEmpty(), nameVar),
				Throw(() => new LinqToDBException("Can not convert Enum value.")));

			var result = generator.MapExpression((string n) => Enum.Parse(replacementType, n), nameVar);

			return result;
		}

		private Expression BuildValueMapperToType<TTarget>(ExpressionGenerator generator, Expression expression)
		{
			var valueType = expression.Type;
			var toType    = typeof(TTarget);

			if (!toType.IsEnum)
				throw new LinqToDBException("Only enums converted automatically.");

			var nameExpr = Expression.Call(_getNameMethodInfo, Expression.Constant(valueType),
				Expression.Convert(expression, typeof(object)));
			var nameVar = generator.DeclareVariable(typeof(string), "enumName");

			generator.Assign(nameVar, nameExpr);
			generator.IfThen(MapExpression((string s) => s.IsNullOrEmpty(), nameVar),
				Throw(() => new LinqToDBException("Can not convert Enum value.")));

			var result = generator.MapExpression((string n) => (TTarget)Enum.Parse(toType, n), nameVar);

			return result;
		}

		private Type MakeReplacement(Type type)
		{
			return TryMapType(type, out var replacement) ? replacement : type;
		}

		LambdaExpression MapLambdaInternal(LambdaExpression lambda)
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

									var prop = replacement.GetProperty(ma.Member.Name);
									if (prop == null)
										throw new LinqToDBException($"Property not found in target type: {replacement.FullName}.{ma.Member.Name}");
									return Expression.MakeMemberAccess(expr, prop);
								}
								break;
							}
						case ExpressionType.New:
							{
								var ne = (NewExpression)e;
								if (TryMapType(ne.Type, out var replacement))
								{
									var paramTypes = ne.Constructor.GetParameters()
										.Select(p => p.ParameterType)
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

									var method = replacement.GetMethod(mc.Method.Name, types);

									if (method == null)
									{
										var name = replacement.FullName + "." + mc.Method.Name + "(" +
										           string.Join(", ", types.Select(t => t.Name)) + ")";
										throw new LinqToDBException($"Method not found in target type: {name}");
									}

									var newArguments  = mc.Arguments.Select(ReplaceTypes);
									var newMethodCall = Expression.Call(ReplaceTypes(mc.Object), method, newArguments);
									return newMethodCall;
								}

								break;
							}
					}

					return e;
				});
				return converted;
			}

			var convertedBody = ReplaceTypes(lambda.Body);
			mappedLambda = Expression.Lambda(convertedBody, newParameters);

			_lambdaMappingCache.Add(lambda, mappedLambda);
			return mappedLambda;
		}

		private Expression MapExpressionInternal(LambdaExpression lambdaExpression, params Expression[] parameters)
		{
			if (lambdaExpression.Parameters.Count != parameters.Length)
				throw new LinqToDBException($"Parameters count is different: {lambdaExpression.Parameters.Count} != {parameters.Length}.");

			var lambda = MapLambdaInternal(lambdaExpression);
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

		#endregion

		#region MapLambda

		public LambdaExpression MapLambda<T, TR>(Expression<Func<T, TR>> func) => MapLambdaInternal(func);
		public LambdaExpression MapLambda<T1, T2, TR>(Expression<Func<T1, T2, TR>> func) => MapLambdaInternal(func);
		public LambdaExpression MapLambda<T1, T2, T3, TR>(Expression<Func<T1, T2, T3, TR>> func) => MapLambdaInternal(func);
		public LambdaExpression MapLambda<T1, T2, T3, T4, TR>(Expression<Func<T1, T2, T3, T4, TR>> func) => MapLambdaInternal(func);
		public LambdaExpression MapLambda<T1, T2, T3, T4, T5, TR>(Expression<Func<T1, T2, T3, T4, T5, TR>> func) => MapLambdaInternal(func);

		#endregion

		#region MapActionLambda

		public LambdaExpression MapActionLambda(Expression<Action> action) => MapLambdaInternal(action);
		public LambdaExpression MapActionLambda<T>(Expression<Action<T>> action) => MapLambdaInternal(action);
		public LambdaExpression MapActionLambda<T1, T2>(Expression<Action<T1, T2>> action) => MapLambdaInternal(action);
		public LambdaExpression MapActionLambda<T1, T2, T3>(Expression<Action<T1, T2, T3>> action) => MapLambdaInternal(action);
		public LambdaExpression MapActionLambda<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> action) => MapLambdaInternal(action);
		public LambdaExpression MapActionLambda<T1, T2, T3, T4, T5>(Expression<Action<T1, T2, T3, T4, T5>> action) => MapLambdaInternal(action);

		#endregion

		#region CompileFunc

		LambdaExpression MapFuncInternal(LambdaExpression lambda, params Type[] types)
		{
			if (lambda.Parameters.Count != types.Length - 1)
				throw new LinqToDBException("Invalid count of types.");

			var parameters    = new ParameterExpression[types.Length - 1];
			var generator = new ExpressionGenerator(this);

			for (int i = 0; i < types.Length - 1; i++)
			{
				var parameter = lambda.Parameters[i];
				if (types[i] != parameter.Type)
				{
					var variable  = generator.AddVariable(parameter);
					parameters[i] = Expression.Parameter(types[i], parameter.Name);
					generator.Assign(variable, parameters[i]);
				}
				else
				{
					parameters[i] = parameter;
				}
			}

			var body = lambda.Body;
			var resultType = types[types.Length - 1];
			if (body.Type != resultType)
			{
				body = Expression.Convert(body, resultType);
			}

			generator.AddExpression(body);
			var newBody = generator.Build();

			return Expression.Lambda(newBody, parameters);
		}

		public Func<TR> CompileFunc<TR>(LambdaExpression lambda) => 
			(Func<TR>)MapFuncInternal(lambda, typeof(TR)).Compile();

		public Func<T, TR> CompileFunc<T, TR>(LambdaExpression lambda) =>
			(Func<T, TR>)MapFuncInternal(lambda, typeof(T), typeof(TR)).Compile();

		public Func<T1, T2, TR> CompileFunc<T1, T2, TR>(LambdaExpression lambda) => 
			(Func<T1, T2, TR>)MapFuncInternal(lambda, typeof(T1), typeof(T2), typeof(TR)).Compile();

		public Func<T1, T2, T3, TR> CompileFunc<T1, T2, T3, TR>(LambdaExpression lambda) => 
			(Func<T1, T2, T3, TR>)MapFuncInternal(lambda, typeof(T1), typeof(T2), typeof(T3), typeof(TR)).Compile();

		public Func<T1, T2, T3, T4, TR> CompileFunc<T1, T2, T3, T4, TR>(LambdaExpression lambda) => 
			(Func<T1, T2, T3, T4, TR>)MapFuncInternal(lambda, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(TR)).Compile();

		public Func<T1, T2, T3, T4, T5, TR> CompileFunc<T1, T2, T3, T4, T5, TR>(LambdaExpression lambda) => 
			(Func<T1, T2, T3, T4, T5, TR>)MapFuncInternal(lambda, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(TR)).Compile();

		#endregion
		#region CompileAction

		LambdaExpression MapActionInternal(LambdaExpression lambda, params Type[] types)
		{
			if (lambda.Parameters.Count != types.Length)
				throw new LinqToDBException("Invalid count of types.");

			var parameters = new ParameterExpression[types.Length];
			var generator = new ExpressionGenerator(this);

			for (int i = 0; i < types.Length; i++)
			{
				var parameter = lambda.Parameters[i];
				if (types[i] != parameter.Type)
				{
					var variable = generator.AddVariable(parameter);
					parameters[i] = Expression.Parameter(types[i], parameter.Name);
					generator.Assign(variable, parameters[i]);
				}
				else
				{
					parameters[i] = parameter;
				}
			}

			var body = lambda.Body;

			generator.AddExpression(body);
			var newBody = generator.Build();

			return Expression.Lambda(newBody, parameters);
		}

		public Action CompileAction(LambdaExpression lambda) =>
			(Action)MapActionInternal(lambda).Compile();

		public Action<T> CompileAction<T>(LambdaExpression lambda) =>
			(Action<T>)MapActionInternal(lambda, typeof(T)).Compile();

		public Action<T1, T2> CompileAction<T1, T2>(LambdaExpression lambda) => 
			(Action<T1, T2>)MapActionInternal(lambda, typeof(T1), typeof(T2)).Compile();

		public Action<T1, T2, T3> CompileAction<T1, T2, T3>(LambdaExpression lambda) => 
			(Action<T1, T2, T3>)MapActionInternal(lambda, typeof(T1), typeof(T2), typeof(T3)).Compile();

		public Action<T1, T2, T3, T4> CompileAction<T1, T2, T3, T4>(LambdaExpression lambda) => 
			(Action<T1, T2, T3, T4>)MapActionInternal(lambda, typeof(T1), typeof(T2), typeof(T3), typeof(T4)).Compile();

		public Action<T1, T2, T3, T4, T5> CompileAction<T1, T2, T3, T4, T5>(LambdaExpression lambda) => 
			(Action<T1, T2, T3, T4, T5>)MapActionInternal(lambda, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)).Compile();

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
			var propLambda  = MapLambdaInternal(propExpression);

			TryMapValue(value, out var replacedValue);

			var left  = propLambda.Body.Unwrap();
			var right = (Expression)Expression.Constant(replacedValue);

			if (right.Type != left.Type)
				right = Expression.Convert(right, left.Type);

			var assign = Expression.Assign(left, right);
			return Expression.Lambda(assign, propLambda.Parameters);
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

				var newParameter = Expression.Parameter(typeof(TBase), setterExpression.Parameters[0].Name);
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

				var propLambda    = _mapper.MapLambdaInternal(_memberExpression);
				var convertedType = propLambda.Parameters[0].Type;

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

				var left  = propLambda.GetBody(requiredVariable).Unwrap();

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
				var wrapper = (TR)Activator.CreateInstance(typeof(TR), result, instance.mapper_);
				return wrapper;
			}
			return (TR)result;
		}

		[return: MaybeNull]
		public TR Wrap<TR>(object? instance)
			where TR: TypeWrapper
		{
			if (instance == null)
				return null!;

			var wrapper = (TR)Activator.CreateInstance(typeof(TR), instance, this);
			return wrapper;
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

	}
}
