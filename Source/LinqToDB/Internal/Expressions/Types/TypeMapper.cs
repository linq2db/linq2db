using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.Expressions.Types
{
	/// <summary>
	/// Implements typed mappings support for dynamically loaded types.
	/// </summary>
	public sealed class TypeMapper
	{
		private static readonly Type[] _wrapperConstructorParameters1 = [typeof(object)];
		private static readonly Type[] _wrapperConstructorParameters2 = [typeof(object), typeof(Delegate[])];

		// [type name] = originalType
		private readonly IDictionary<string, Type>              _types                    = new Dictionary<string, Type>();

		// [wrapperType] = originalType?
		readonly Dictionary<Type, Type?>                        _typeMappingCache         = new ();
		// [originalType] = wrapperType
		readonly Dictionary<Type, Type>                         _typeMappingReverseCache  = new ();
		readonly Dictionary<LambdaExpression, LambdaExpression> _lambdaMappingCache       = new ();
		readonly Dictionary<Type, Func<object, object>>         _wrapperFactoryCache      = new ();
		readonly Dictionary<Type, Func<Task, object?>>          _taskWrapperFactoryCache  = new ();
		// [originalType] = converter
		readonly Dictionary<Type, LambdaExpression>             _enumToWrapperCache       = new ();
		// [wrapperType] = converter
		readonly Dictionary<Type, LambdaExpression>             _enumFromWrapperCache     = new ();
		readonly Dictionary<Type, ICustomMapper>                _typeMapperInstancesCache = new ();

		private bool _finalized;

		public void RegisterTypeWrapper<TWrapper>(Type originalType)
		{
			RegisterTypeWrapper(typeof(TWrapper), originalType);
		}

		public void RegisterTypeWrapper(Type wrapperType, Type originalType)
		{
			if (_finalized)
				throw new LinqToDBException($"Wrappers registration is not allowed after {nameof(FinalizeMappings)}() call");

			var wrapperAttr = wrapperType.GetAttribute<WrapperAttribute>();

			if ((wrapperAttr?.TypeName ?? wrapperType.Name) != originalType.Name)
				throw new LinqToDBException($"Original and wraped types should have same type name. {wrapperType.Name} != {originalType.Name}");

			var typeName = originalType.FullName ?? originalType.Name;
			if (_types.ContainsKey(typeName))
				throw new LinqToDBException($"Type with name {typeName} already registered in mapper");

			_types                  .Add(typeName    , originalType);
			_typeMappingCache       .Add(wrapperType , originalType);
			_typeMappingReverseCache.Add(originalType, wrapperType);

			if (typeof(TypeWrapper).IsSameOrParentOf(wrapperType))
			{
			}
			else if (wrapperAttr != null)
			{
				// build enum converters
				if (wrapperType.IsEnum)
				{
					BuildEnumConverters(wrapperType, originalType);
				}
			}
			else
				throw new LinqToDBException($"Type {wrapperType} should inherit from {typeof(TypeWrapper)} or marked with {typeof(WrapperAttribute)} attribute");
		}

		private void BuildEnumConverters(Type wrapperType, Type originalType)
		{
			// we generate two types of coverters based on enum values:
			// 1. if at least one enum value with same name has different values in both enums - we use dictionary to map values
			// 2. if enum values with same name have same value in both enums
			//		- we use by-value cast from one enum to another
			// 3. if enums doesn't have common values by name, we throw exception as it is probably bad mapping
			// for [Flags] enums we will support only by-value conversion for now

			var baseType = Enum.GetUnderlyingType(wrapperType);

			if (baseType != Enum.GetUnderlyingType(originalType))
				throw new LinqToDBException($"Enums {wrapperType} and {originalType} have different base types: {baseType} vs {Enum.GetUnderlyingType(originalType)}");

			var wrapperValues  = Enum.GetValues(wrapperType) .OfType<object>().Distinct().ToDictionary(v => string.Format(CultureInfo.InvariantCulture, "{0}", v), _ => _);
			var originalValues = Enum.GetValues(originalType).OfType<object>().Distinct().ToDictionary(v => string.Format(CultureInfo.InvariantCulture, "{0}", v), _ => _);

			var hasCommonMembers   = false;
			var hasDifferentValues = false;
			foreach (var kvp in wrapperValues)
			{
				if (originalValues.TryGetValue(kvp.Key, out var origValue))
				{
					hasCommonMembers = true;
					if (Convert.ToInt64(kvp.Value, CultureInfo.InvariantCulture) != Convert.ToInt64(origValue, CultureInfo.InvariantCulture))
					{
						hasDifferentValues = true;
						break;
					}
				}
			}

			if (!hasCommonMembers)
				throw new LinqToDBException($"Enums {wrapperType} and {originalType} have no common values");

			// build by-value converters
			var pWrapper  = Expression.Parameter(wrapperType);
			var pOriginal = Expression.Parameter(originalType);
			LambdaExpression convertFromWrapper;
			LambdaExpression convertToWrapper;

			if (hasDifferentValues)
			{
				// this should never happen, but it we will have such situation it is better to fail
				if (wrapperType.HasAttribute<FlagsAttribute>()
					|| originalType.HasAttribute<FlagsAttribute>())
					throw new LinqToDBException($"Flags enums {wrapperType} and {originalType} are not compatible by values");

				// build dictionary-based converters

				// create typed dictionaries to avoid allocations on boxing
				var w2oType = typeof(Dictionary<,>).MakeGenericType(wrapperType, originalType);
				var o2wType = typeof(Dictionary<,>).MakeGenericType(originalType, wrapperType);

				var wrapperToOriginal = w2oType.GetConstructor([])!.InvokeExt<IDictionary>([]);
				var originalToWrapper = o2wType.GetConstructor([])!.InvokeExt<IDictionary>([]);

				var w2o = wrapperToOriginal;
				var o2w = originalToWrapper;

				foreach (var kvp in wrapperValues)
					if (originalValues.TryGetValue(kvp.Key, out var orig))
						// we must take into account that enums could contain multiple fields with same value
						if (!w2o.Contains(kvp.Value))
							w2o.Add(kvp.Value, orig);

				foreach (var kvp in originalValues)
					if (wrapperValues.TryGetValue(kvp.Key, out var wrapped))
						// we must take into account that enums could contain multiple fields with same value
						if (!o2w.Contains(kvp.Value))
							o2w.Add(kvp.Value, wrapped);

				var origValue = Expression.Parameter(originalType.MakeByRefType());

				convertToWrapper = Expression.Lambda(
					Expression.Call(
						typeof(TypeMapper),
						nameof(ConvertEnum),
						new Type[] { originalType, wrapperType },
						Expression.Constant(originalToWrapper, o2wType),
						pOriginal),
					pOriginal);

				var wrappedValue = Expression.Parameter(wrapperType.MakeByRefType());
				convertFromWrapper = Expression.Lambda(
					Expression.Call(
						typeof(TypeMapper),
						nameof(ConvertEnum),
						new Type[] { wrapperType, originalType },
						Expression.Constant(wrapperToOriginal, w2oType),
						pWrapper),
					pWrapper);
			}
			else
			{
				convertFromWrapper = Expression.Lambda(Expression.Convert(pWrapper, originalType), pWrapper);
				convertToWrapper   = Expression.Lambda(Expression.Convert(pOriginal, wrapperType), pOriginal);

			}

			_enumToWrapperCache  .Add(originalType, convertToWrapper  );
			_enumFromWrapperCache.Add(wrapperType , convertFromWrapper);
		}

		// helper to inline dictionary-based enum conversion into expression
		private static TR ConvertEnum<T, TR>(Dictionary<T, TR> dictionary, T value)
			where T : Enum
			where TR : Enum
		{
			// in theory we can get rid of boxing below, but don't think it matters
			// as we shouldn't reach that branch for most if not all cases
			// https://stackoverflow.com/questions/1189144
			return dictionary.TryGetValue(value, out var res) ? res : (TR)(object)value;
		}

		public void FinalizeMappings()
		{
			if (_finalized)
				throw new LinqToDBException($"{nameof(FinalizeMappings)}() cannot be called multiple times");

			foreach (var wrapperType in _typeMappingCache.Keys.Where(t => typeof(TypeWrapper).IsSameOrParentOf(t)).ToList())
			{
				// pre-build wrappers
				var delegates     = BuildWrapperMethods(wrapperType);
				var eventsHandler = BuildWrapperEvents (wrapperType);

				// pre-register factory, so we don't need to use concurrent dictionary to access factory later
				var types = delegates != null ? _wrapperConstructorParameters2 : _wrapperConstructorParameters1;
				var ctor = wrapperType.GetConstructor(types);

				if (ctor == null)
					throw new LinqToDBException($"Cannot find constructor ({string.Join(", ", types.Select(t => t.ToString()))}) in type {wrapperType}");

				var pInstance = Expression.Parameter(typeof(object));

				Expression factoryBody = delegates != null
					? Expression.New(ctor, pInstance, Expression.Constant(delegates))
					: Expression.New(ctor, pInstance);

				if (eventsHandler != null)
				{
					var instance = Expression.Parameter(wrapperType);
					factoryBody = Expression.Block(
						new[] { instance },
						Expression.Assign(instance, factoryBody),
						Expression.Invoke(Expression.Constant(eventsHandler), instance),
						instance);
				}

				var factory = Expression
					.Lambda<Func<object, object>>(factoryBody, pInstance)
					.CompileExpression();

				_wrapperFactoryCache.Add(wrapperType, factory);

				// resolved Task<T> unwrap
				var originalType = _typeMappingCache[wrapperType];
				if (originalType != null)
				{
					var pTask = Expression.Parameter(typeof(Task));
					var taskT = typeof(Task<>).MakeGenericType(originalType);

					Expression taskResult = Expression.Property(Expression.Convert(pTask, taskT), nameof(Task<object>.Result));

					// TODO: add generics support to avoid boxing of structs?
					if (originalType.IsValueType)
						taskResult = Expression.Convert(taskResult, typeof(object));

					Expression taskFactoryBody = delegates != null
						? Expression.New(ctor, taskResult, Expression.Constant(delegates))
						: Expression.New(ctor, taskResult);

					if (eventsHandler != null)
					{
						var instance = Expression.Parameter(wrapperType);
						taskFactoryBody = Expression.Block(
							new[] { instance },
							Expression.Assign(instance, taskFactoryBody),
							Expression.Invoke(Expression.Constant(eventsHandler), instance),
							instance);
					}

					var taskFactory = Expression
						.Lambda<Func<Task, object?>>(taskFactoryBody, pTask)
						.CompileExpression();

					_taskWrapperFactoryCache.Add(wrapperType, taskFactory);
				}
			}

			_finalized = true;
		}

		private Delegate? BuildWrapperEvents(Type wrapperType)
		{
			// TODO: for now we don't unsubscribe from wrapped instance events as it doesn't create issues for our
			// use-cases, but if we decide to do it we can implement it following way:
			// - generate unsubscribe method
			// - require wrapper to implement IDisposable and call unsubscribe from Dispose()
			var events = wrapperType.GetProperty("Events", BindingFlags.Static | BindingFlags.NonPublic);

			if (events != null)
			{
				if (!TryMapType(wrapperType, out var targetType))
					throw new InvalidOperationException();

				var subscribeGenerator = new ExpressionGenerator(this);
				var pWrapper           = Expression.Parameter(wrapperType);

				foreach (var eventName in (string[])events.GetValue(null)!)
				{
					var wrapperEvent = wrapperType.GetEvent(eventName)!;
					var delegateType = wrapperEvent.EventHandlerType!;
					var invokeMethod = delegateType.GetMethod("Invoke")!;
					var returnType   = invokeMethod.ReturnType;

					if (TryMapType(delegateType, out delegateType))
						invokeMethod = delegateType.GetMethod("Invoke")!;
					else
						delegateType = wrapperEvent.EventHandlerType!;

					var lambdaReturnType = invokeMethod.ReturnType;
					var parameterInfos   = invokeMethod.GetParameters();
					var parameters       = new ParameterExpression[parameterInfos.Length];
					var parameterValues  = new Expression[parameterInfos.Length];
					var parameterTypes   = new Type[parameterInfos.Length];

					for (var i = 0; i < parameterInfos.Length; i++)
					{
						if (!TryMapType(parameterInfos[i].ParameterType, out var parameterType))
							parameterType = parameterInfos[i].ParameterType;

						parameterValues[i] = parameters[i] = Expression.Parameter(parameterType);
						parameterTypes[i] = parameterType;

						if (_typeMappingReverseCache.TryGetValue(parameterType, out var parameterWrapperType))
						{
							parameterValues[i] = Expression.Convert(MapExpression((object? value) => Wrap(parameterWrapperType, value), parameterValues[i]), parameterWrapperType);
							parameterTypes[i]  = parameterWrapperType;
						}
					}

					var handlerGenerator = new ExpressionGenerator(this);
					var delegateVariable = handlerGenerator.DeclareVariable(wrapperEvent.EventHandlerType!, "handler");

					handlerGenerator.Assign(delegateVariable, ExpressionHelper.Field(pWrapper, "_" + eventName));

					// returning event is a bad idea
					if (returnType != typeof(void))
						handlerGenerator.Condition(
							MapExpression((Delegate? handler) => handler != null, delegateVariable),
							Expression.Convert(MapInvoke(wrapperEvent.EventHandlerType!, delegateVariable, returnType, parameterValues, parameterTypes), lambdaReturnType),
							Expression.Default(lambdaReturnType));
					else
						handlerGenerator.IfThen(
							MapExpression((Delegate? handler) => handler != null, delegateVariable),
							MapInvoke(wrapperEvent.EventHandlerType!, delegateVariable, null, parameterValues, parameterTypes));

					var ei = targetType.GetEvent(eventName)!;

					subscribeGenerator.AddExpression(
						Expression.Call(
							Expression.Convert(ExpressionHelper.Property(pWrapper, nameof(TypeWrapper.instance_)), targetType),
							ei.AddMethod!,
							Expression.Lambda(delegateType, handlerGenerator.ResultExpression, parameters)));
				}

				var subscribeBody = subscribeGenerator.Build();
				return Expression.Lambda(subscribeBody, pWrapper).CompileExpression();

				Expression MapInvoke(Type delegateType, Expression delegateValue, Type? returnType, Expression[] parameters, Type[] parameterTypes)
				{
					var paramExpressions = new ParameterExpression[parameters.Length + 1];
					var paramValues = new Expression[parameters.Length + 1];

					paramExpressions[0] = Expression.Parameter(delegateType);
					paramValues[0] = delegateValue;

					for (var i = 0; i < parameters.Length; i++)
					{
						paramExpressions[i + 1] = Expression.Parameter(parameterTypes[i]);
						paramValues[i + 1] = parameters[i];
					}

					Expression expr = Expression.Invoke(
						paramExpressions[0],
						paramExpressions.Skip(1));

					expr = MapExpressionInternal(Expression.Lambda(expr, paramExpressions), paramValues);

					if (returnType != null && typeof(TypeWrapper).IsSameOrParentOf(returnType))
						expr = ExpressionHelper.Property(Expression.Convert(expr, returnType), nameof(TypeWrapper.instance_));

					return expr;
				}
			}

			return null;
		}

		private Delegate[]? BuildWrapperMethods(Type wrapperType)
		{
			var wrappers = wrapperType.GetProperty("Wrappers", BindingFlags.Static | BindingFlags.NonPublic);

			if (wrappers != null)
				return ((IEnumerable<object>)wrappers.GetValue(null)!)
					.Select(e => e is Tuple<LambdaExpression, bool> tuple
						? new { expr = tuple.Item1, optional = tuple.Item2 }
						: new { expr = (LambdaExpression)e, optional = false })
					.Select(e => BuildWrapper(e.expr, e.optional)!).ToArray();

			return null;
		}

		private bool TryMapType(Type type, [NotNullWhen(true)] out Type? replacement)
		{
			if (_typeMappingCache.TryGetValue(type, out replacement))
				return replacement != null;

			if (type.IsGenericType) {
				var typeArguments = type.GetGenericArguments().Select(t => TryMapType(t, out var r) ? r : null).ToArray();
				if (typeArguments.All(t => t != null))
				{
					replacement = type.GetGenericTypeDefinition().MakeGenericType(typeArguments!);
					_typeMappingCache.Add(type, replacement);
					return true;
				}
			}

			_typeMappingCache.Add(type, null);
			return false;
		}

		private static readonly MethodInfo _wrapInstanceMethodInfo = MemberHelper.MethodOf<TypeMapper>(t => t.Wrap(null!, null));

		private Expression BuildValueMapper(ExpressionGenerator generator, Expression expression)
		{
			var valueType = expression.Type;
			if (!TryMapType(valueType, out var replacementType))
				return expression;

			if (!replacementType.IsEnum)
				throw new LinqToDBException("Only enums converted automatically.");

			return _enumFromWrapperCache[valueType].GetBody(expression);
		}

		private Expression BuildValueMapperToType<TTarget>(ExpressionGenerator generator, Expression expression)
		{
			var valueType = expression.Type;
			var toType    = typeof(TTarget);

			if (!toType.IsEnum)
				throw new LinqToDBException("Only enums converted automatically.");

			return _enumToWrapperCache[valueType].GetBody(expression);
		}

		private Type MakeReplacement(Type type)
		{
			return TryMapType(type, out var replacement) ? replacement : type;
		}

		private LambdaExpression? MapLambdaInternal(LambdaExpression lambda, bool mapConvert = false, bool convertResult = true, bool ignoreMissingMembers = false)
		{
			if (_lambdaMappingCache.TryGetValue(lambda, out var mappedLambda))
				return mappedLambda;

			var newParameters = lambda.Parameters
				.Select(p => TryMapType(p.Type, out var replacement) ? Expression.Parameter(replacement, p.Name) : p)
				.ToArray();

			var ctx = new ReplaceTypesContext(this, lambda, newParameters, mapConvert, ignoreMissingMembers);

			var convertedBody = ReplaceTypes(lambda.Body, ctx);
			if (convertedBody == null)
				return null;

			if (convertResult && _typeMappingReverseCache.TryGetValue(convertedBody.Type, out var wrapperType) && wrapperType.IsEnum)
				convertedBody = _enumToWrapperCache[convertedBody.Type].GetBody(convertedBody);

			mappedLambda = Expression.Lambda(convertedBody, newParameters);

			_lambdaMappingCache.Add(lambda, mappedLambda);
			return mappedLambda;
		}

		sealed class ReplaceTypesContext
		{
			public ReplaceTypesContext(TypeMapper mapper, LambdaExpression lambda, ParameterExpression[] newParameters, bool mapConvert, bool ignoreMissingMembers)
			{
				Mapper               = mapper;
				Lambda               = lambda;
				NewParameters        = newParameters;
				MapConvert           = mapConvert;
				IgnoreMissingMembers = ignoreMissingMembers;
			}

			public bool Aborted;

			public readonly TypeMapper            Mapper;
			public readonly LambdaExpression      Lambda;
			public readonly ParameterExpression[] NewParameters;
			public readonly bool                  MapConvert;
			public readonly bool                  IgnoreMissingMembers;
		}

		static MemberInfo ReplaceMember(MemberInfo memberInfo, Type targetType)
		{
			var newMembers = targetType.GetMember(memberInfo.Name);
			if (newMembers.Length == 0)
				throw new LinqToDBException($"There is no member '{memberInfo.Name}' in type '{targetType.FullName}'");
			if (newMembers.Length > 1)
				throw new LinqToDBException($"Ambiguous member '{memberInfo.Name}' in type '{targetType.FullName}'");
			return newMembers[0];
		}

		static Expression? ReplaceTypes(Expression expression, ReplaceTypesContext ctx)
		{
			var converted = expression.Transform(ctx, static (context, e) =>
			{
				if (context.Aborted)
					return e;

				switch (e.NodeType)
				{
					case ExpressionType.Convert        :
					case ExpressionType.ConvertChecked :
						{
							if (!context.MapConvert)
								break;

							var ue   = (UnaryExpression)e;
							var expr = ReplaceTypes(ue.Operand, context)!;
							var type = context.Mapper.TryMapType(ue.Type, out var newType) ? newType : ue.Type;

							if (ue.Method != null)
							{
								if (context.Mapper.TryMapType(ue.Method.DeclaringType!, out var replacement))
								{
									var types = ue.Method.GetParameters()
										.Select(p => context.Mapper.MakeReplacement(p.ParameterType))
										.ToArray();

									// op_Explicit overloads by return type...
									var method = replacement.GetMethodEx(context.Mapper.MakeReplacement(ue.Method.ReturnType), ue.Method.Name, types);

									if (method == null)
									{
										if (context.IgnoreMissingMembers)
										{
											context.Aborted = true;
											return e;
										}

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
							var left  = ReplaceTypes(be.Left, context)!;
							var right = be.Right;

							if (context.Mapper.TryMapType(right.Type, out var replacement))
							{
								if (right.NodeType == ExpressionType.Constant
									&& right.EvaluateExpression() is TypeWrapper wrapper)
								{
									right = Expression.Constant(wrapper.instance_);
								}
								else if (replacement.IsEnum)
								{
									right = context.Mapper._enumFromWrapperCache[right.Type].GetBody(right);
								}
							}

							return Expression.Assign(left, right);
						}

					case ExpressionType.Parameter:
						{
							var idx = context.Lambda.Parameters.IndexOf((ParameterExpression)e);
							if (idx >= 0)
								return context.NewParameters[idx];
							break;
						}
					case ExpressionType.MemberAccess:
						{
							var ma = (MemberExpression)e;

							if (ma.Expression == null)
								break;

							if (context.Mapper.TryMapType(ma.Expression.Type, out var replacement))
							{
								var expr = ReplaceTypes(ma.Expression, context)!;
								if (expr.Type != replacement)
									throw new LinqToDBException($"Invalid replacement of '{ma.Expression}' to type '{replacement.FullName}'.");

								var prop = replacement.GetProperty(ma.Member.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
								if (prop == null)
									prop = replacement.GetProperty(ma.Member.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
								if (prop == null)
								{
									if (context.IgnoreMissingMembers)
									{
										context.Aborted = true;
										return e;
									}

									throw new LinqToDBException($"Property not found in target type: {replacement.FullName}.{ma.Member.Name}");
								}

								return Expression.MakeMemberAccess(expr, prop);
							}

							if (context.Mapper.TryMapType(ma.Type, out replacement))
							{
								if (ma.Expression.NodeType == ExpressionType.Constant
									&& ma.EvaluateExpression() is TypeWrapper wrapper)
								{
									return Expression.Constant(wrapper.instance_);
								}
								else if (replacement.IsEnum)
									return context.Mapper._enumFromWrapperCache[ma.Type].GetBody(ma);
							}

							break;
						}
					case ExpressionType.New:
						{
							var ne = (NewExpression)e;
							if (context.Mapper.TryMapType(ne.Type, out var replacement))
							{
								var paramTypes = ne.Constructor!.GetParameters()
									.Select(p => context.Mapper.TryMapType(p.ParameterType, out var newType) ? newType : p.ParameterType)
									.ToArray();

								var ctor = replacement.GetConstructor(paramTypes);

								if (ctor == null)
								{
									if (context.IgnoreMissingMembers)
									{
										context.Aborted = true;
										return e;
									}

									var name = replacement.FullName + "." + ne.Constructor.Name + "(" +
									           string.Join(", ", paramTypes.Select(t => t.Name)) + ")";
									throw new LinqToDBException($"Constructor not found in target type: {name}");
								}

								var newArguments  = ne.Arguments.Select(a => ReplaceTypes(a, context)!);
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
							if (context.Mapper.TryMapType(mi.Type, out var replacement))
							{
								var newExpression = (NewExpression)ReplaceTypes(mi.NewExpression, context)!;
								var newBindings = mi.Bindings.Select(b =>
								{
									switch (b.BindingType)
									{
										case MemberBindingType.Assignment:
											{
												var mab = (MemberAssignment)b;
												return Expression.Bind(ReplaceMember(mab.Member, replacement),
													ReplaceTypes(mab.Expression, context)!);
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
											throw new InvalidOperationException($"Unexpected binding type: {b.BindingType}");
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

							var methodName         = mc.Method.GetAttribute<TypeWrapperNameAttribute>()?.Name ?? mc.Method.Name;
							var customReturnMapper = context.Mapper.CreateTypeMapper(mc.Method.ReturnParameter.GetAttribute<CustomMapperAttribute>()?.Mapper);

							if (context.Mapper.TryMapType(mc.Method.DeclaringType!, out var replacement))
							{
								var types = mc.Method.GetParameters()
									.Select(p => context.Mapper.MakeReplacement(p.ParameterType))
									.ToArray();

								if (mc.Method.IsGenericMethod)
								{
									// typeArgs replacements not implemented now, as we don't have usecases for it
									var typeArgs = mc.Method.GetGenericArguments();
									var method   = replacement.GetMethodEx(methodName, typeArgs.Length, types);

									if (method == null
										|| (customReturnMapper == null && !mc.Method.ReturnType.IsAssignableFrom(method.ReturnType) && (!context.Mapper.TryMapType(mc.Method.ReturnType, out var newReturnType) || method.ReturnType != newReturnType)))
									{
										if (context.IgnoreMissingMembers)
										{
											context.Aborted = true;
											return e;
										}

										var name = replacement.FullName + "." + methodName + "<" +
												   string.Join(", ", typeArgs.Select(t => t.Name))+ ">(" +
												   string.Join(", ", types.Select(t => t.Name)) + ")";
										throw new LinqToDBException($"Method not found in target type: {name}");
									}

									var newArguments  = mc.Arguments.Select(a => ReplaceTypes(a, context)!);
									var newMethodCall = Expression.Call(ReplaceTypes(mc.Object!, context)!, methodName, typeArgs, newArguments.ToArray());

									if (customReturnMapper != null)
									{
										if (!customReturnMapper.CanMap(newMethodCall))
										{
											if (context.IgnoreMissingMembers)
											{
												context.Aborted = true;
												return e;
											}

											throw new LinqToDBException($"Cannot map return type: {newMethodCall.Type} using {customReturnMapper.GetType()} mapper");
										}

										return customReturnMapper.Map(newMethodCall);
									}

									return newMethodCall;
								}
								else
								{
									var method = replacement.GetMethod(methodName, types);

									if (method == null
										|| (customReturnMapper == null && !mc.Method.ReturnType.IsAssignableFrom(method.ReturnType) && (!context.Mapper.TryMapType(mc.Method.ReturnType, out var newReturnType) || method.ReturnType != newReturnType)))
									{
										if (context.IgnoreMissingMembers)
										{
											context.Aborted = true;
											return e;
										}

										var name = replacement.FullName + "." + methodName + "(" +
												   string.Join(", ", types.Select(t => t.Name)) + ")";
										throw new LinqToDBException($"Method not found in target type: {name}");
									}

									var newArguments  = mc.Arguments.Select(a => ReplaceTypes(a, context)!);
									var newMethodCall = Expression.Call(ReplaceTypes(mc.Object!, context), method, newArguments);

									if (customReturnMapper != null)
									{
										if (!customReturnMapper.CanMap(newMethodCall))
										{
											if (context.IgnoreMissingMembers)
											{
												context.Aborted = true;
												return e;
											}

											throw new LinqToDBException($"Cannot map return type: {newMethodCall.Type} using {customReturnMapper.GetType()} mapper");
										}

										return customReturnMapper.Map(newMethodCall);
									}

									return newMethodCall;
								}
							}

							break;
						}
				}

				return e;
			});

			return ctx.Aborted ? null : converted;
		}

		[return: NotNullIfNotNull(nameof(mapperType))]
		private ICustomMapper? CreateTypeMapper(Type? mapperType)
		{
			if (mapperType == null)
				return null;

			if (!_typeMapperInstancesCache.TryGetValue(mapperType, out var mapper))
			{
				mapper = ActivatorExt.CreateInstance<ICustomMapper>(mapperType);

				_typeMapperInstancesCache[mapperType] = mapper;
			}

			return mapper;
		}

		private Expression MapExpressionInternal(LambdaExpression lambdaExpression, params Expression[] parameters)
		{
			if (lambdaExpression.Parameters.Count != parameters.Length)
				throw new LinqToDBException($"Parameters count is different: {lambdaExpression.Parameters.Count} != {parameters.Length}.");

			var lambda = MapLambdaInternal(lambdaExpression, true)!;
			var expr   = lambda.Body.Transform((lambdaParams: lambda.Parameters, parameters), static (context, e) =>
			{
				if (e.NodeType == ExpressionType.Parameter)
				{
					var idx = context.lambdaParams.IndexOf((ParameterExpression)e);
					if (idx >= 0)
						return context.parameters[idx];
				}

				return e;
			});

			return expr;
		}

		private LambdaExpression CorrectLambdaParameters(LambdaExpression lambda, Type? resultType, params Type[] paramTypes)
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

		#endregion

		#region MapAction

		public Expression MapAction(Expression<Action> action)
			=> MapExpressionInternal(action);

		public Expression MapAction<T>(Expression<Action<T>> action, Expression p)
			=> MapExpressionInternal(action, p);

		public Expression MapAction<T1, T2>(Expression<Action<T1, T2>> action, Expression p1, Expression p2)
			=> MapExpressionInternal(action, p1, p2);

		public Expression MapAction<T1, T2, T3>(Expression<Action<T1, T2, T3>> action, Expression p1, Expression p2, Expression p3)
			=> MapExpressionInternal(action, p1, p2, p3);

		public Expression MapAction<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> action, Expression p1, Expression p2, Expression p3, Expression p4)
			=> MapExpressionInternal(action, p1, p2, p3, p4);

		public Expression MapAction<T1, T2, T3, T4, T5>(Expression<Action<T1, T2, T3, T4, T5>> action, Expression p1, Expression p2, Expression p3, Expression p4, Expression p5)
			=> MapExpressionInternal(action, p1, p2, p3, p4, p5);
		#endregion

		#region MapLambda

		public LambdaExpression MapLambda<T, TR>(Expression<Func<T, TR>> func) => MapLambdaInternal(func, true)!;
		public LambdaExpression MapLambda<T1, T2, TR>(Expression<Func<T1, T2, TR>> func) => MapLambdaInternal(func, true)!;
		public LambdaExpression MapLambda<T1, T2, T3, TR>(Expression<Func<T1, T2, T3, TR>> func) => MapLambdaInternal(func, true)!;
		public LambdaExpression MapLambda<T1, T2, T3, T4, TR>(Expression<Func<T1, T2, T3, T4, TR>> func) => MapLambdaInternal(func, true)!;
		public LambdaExpression MapLambda<T1, T2, T3, T4, T5, TR>(Expression<Func<T1, T2, T3, T4, T5, TR>> func) => MapLambdaInternal(func, true)!;
		public LambdaExpression MapLambda<T1, T2, T3, T4, T5, T6, TR>(Expression<Func<T1, T2, T3, T4, T5, T6, TR>> func) => MapLambdaInternal(func, true)!;
		public LambdaExpression MapLambda<T1, T2, T3, T4, T5, T6, T7, TR>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, TR>> func) => MapLambdaInternal(func, true)!;

		#endregion

		#region MapActionLambda

		public LambdaExpression MapActionLambda(Expression<Action> action) => MapLambdaInternal(action, true)!;
		public LambdaExpression MapActionLambda<T>(Expression<Action<T>> action) => MapLambdaInternal(action, true)!;
		public LambdaExpression MapActionLambda<T1, T2>(Expression<Action<T1, T2>> action) => MapLambdaInternal(action, true)!;
		public LambdaExpression MapActionLambda<T1, T2, T3>(Expression<Action<T1, T2, T3>> action) => MapLambdaInternal(action, true)!;
		public LambdaExpression MapActionLambda<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> action) => MapLambdaInternal(action, true)!;
		public LambdaExpression MapActionLambda<T1, T2, T3, T4, T5>(Expression<Action<T1, T2, T3, T4, T5>> action) => MapLambdaInternal(action, true)!;

		#endregion

		#region BuildFunc

		public Func<TR> BuildFunc<TR>(LambdaExpression lambda) =>
			(Func<TR>)CorrectLambdaParameters(lambda, typeof(TR)).CompileExpression();

		public Func<T, TR> BuildFunc<T, TR>(LambdaExpression lambda) =>
			(Func<T, TR>)CorrectLambdaParameters(lambda, typeof(TR), typeof(T)).CompileExpression();

		public Func<T1, T2, TR> BuildFunc<T1, T2, TR>(LambdaExpression lambda) =>
			(Func<T1, T2, TR>)CorrectLambdaParameters(lambda, typeof(TR), typeof(T1), typeof(T2)).CompileExpression();

		public Func<T1, T2, T3, TR> BuildFunc<T1, T2, T3, TR>(LambdaExpression lambda) =>
			(Func<T1, T2, T3, TR>)CorrectLambdaParameters(lambda, typeof(TR), typeof(T1), typeof(T2), typeof(T3)).CompileExpression();

		public Func<T1, T2, T3, T4, TR> BuildFunc<T1, T2, T3, T4, TR>(LambdaExpression lambda) =>
			(Func<T1, T2, T3, T4, TR>)CorrectLambdaParameters(lambda, typeof(TR), typeof(T1), typeof(T2), typeof(T3), typeof(T4)).CompileExpression();

		public Func<T1, T2, T3, T4, T5, TR> BuildFunc<T1, T2, T3, T4, T5, TR>(LambdaExpression lambda) =>
			(Func<T1, T2, T3, T4, T5, TR>)CorrectLambdaParameters(lambda, typeof(TR), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)).CompileExpression();

		public Func<T1, T2, T3, T4, T5, T6, TR> BuildFunc<T1, T2, T3, T4, T5, T6, TR>(LambdaExpression lambda) =>
			(Func<T1, T2, T3, T4, T5, T6, TR>)CorrectLambdaParameters(lambda, typeof(TR), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)).CompileExpression();

		public Func<T1, T2, T3, T4, T5, T6, T7, TR> BuildFunc<T1, T2, T3, T4, T5, T6, T7, TR>(LambdaExpression lambda) =>
			(Func<T1, T2, T3, T4, T5, T6, T7, TR>)CorrectLambdaParameters(lambda, typeof(TR), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)).CompileExpression();
		#endregion

		#region BuildAction

		public Action BuildAction(LambdaExpression lambda) =>
			(Action)CorrectLambdaParameters(lambda, null).CompileExpression();

		public Action<T> BuildAction<T>(LambdaExpression lambda) =>
			(Action<T>)CorrectLambdaParameters(lambda, null, typeof(T)).CompileExpression();

		public Action<T1, T2> BuildAction<T1, T2>(LambdaExpression lambda) =>
			(Action<T1, T2>)CorrectLambdaParameters(lambda, null, typeof(T1), typeof(T2)).CompileExpression();

		public Action<T1, T2, T3> BuildAction<T1, T2, T3>(LambdaExpression lambda) =>
			(Action<T1, T2, T3>)CorrectLambdaParameters(lambda, null, typeof(T1), typeof(T2), typeof(T3)).CompileExpression();

		public Action<T1, T2, T3, T4> BuildAction<T1, T2, T3, T4>(LambdaExpression lambda) =>
			(Action<T1, T2, T3, T4>)CorrectLambdaParameters(lambda, null, typeof(T1), typeof(T2), typeof(T3), typeof(T4)).CompileExpression();

		public Action<T1, T2, T3, T4, T5> BuildAction<T1, T2, T3, T4, T5>(LambdaExpression lambda) =>
			(Action<T1, T2, T3, T4, T5>)CorrectLambdaParameters(lambda, null, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)).CompileExpression();

		#endregion

		#region Setters

		public class MemberBuilder<T, TV>
		{
			private readonly TypeMapper _mapper;
			private readonly Expression<Func<T, TV>> _memberExpression;

			internal MemberBuilder(TypeMapper mapper, Expression<Func<T, TV>> memberExpression)
			{
				_mapper = mapper;
				_memberExpression = memberExpression;
			}

			private Expression<Action<TBase, TV>> BuildSetterExpression<TBase>()
			{
				var generator = new ExpressionGenerator(_mapper);

				var propLambda    = _mapper.MapLambdaInternal(_memberExpression, false, false)!;

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

			private Expression<Func<TBase, TV>> BuildGetterExpression<TBase>()
			{
				var generator = new ExpressionGenerator(_mapper);

				var propLambda    = _mapper.MapLambdaInternal(_memberExpression)!;
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

				return resultLambda.CompileExpression();
			}

			public Func<TBase, TV> BuildGetter<TBase>()
			{
				var resultLambda = BuildGetterExpression<TBase>();

				return resultLambda.CompileExpression();
			}
		}

		public class TypeBuilder<T>
		{
			private readonly TypeMapper _mapper;

			internal TypeBuilder(TypeMapper mapper)
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

		#region Wrapper factories

		public Func<TR> BuildWrappedFactory<TR>(Expression<Func<TR>> newFunc)
			where TR : TypeWrapper
		{
			return (Func<TR>)BuildFactoryImpl<TR>(newFunc, true);
		}

		public Func<T, TR> BuildWrappedFactory<T, TR>(Expression<Func<T, TR>> newFunc)
			where TR : TypeWrapper
		{
			return (Func<T, TR>)BuildFactoryImpl<TR>(newFunc, true);
		}

		public Func<T1, T2, TR> BuildWrappedFactory<T1, T2, TR>(Expression<Func<T1, T2, TR>> newFunc)
			where TR : TypeWrapper
		{
			return (Func<T1, T2, TR>)BuildFactoryImpl<TR>(newFunc, true);
		}

		public Func<T1, T2, T3, TR> BuildWrappedFactory<T1, T2, T3, TR>(Expression<Func<T1, T2, T3, TR>> newFunc)
			where TR : TypeWrapper
		{
			return (Func<T1, T2, T3, TR>)BuildFactoryImpl<TR>(newFunc, true);
		}

		public Func<T1, T2, T3, T4, TR> BuildWrappedFactory<T1, T2, T3, T4, TR>(Expression<Func<T1, T2, T3, T4, TR>> newFunc)
			where TR : TypeWrapper
		{
			return (Func<T1, T2, T3, T4, TR>)BuildFactoryImpl<TR>(newFunc, true);
		}

		public Func<T1, T2, T3, T4, T5, TR> BuildWrappedFactory<T1, T2, T3, T4, T5, TR>(Expression<Func<T1, T2, T3, T4, T5, TR>> newFunc)
			where TR : TypeWrapper
		{
			return (Func<T1, T2, T3, T4, T5, TR>)BuildFactoryImpl<TR>(newFunc, true);
		}

		public Func<T1, T2, T3, T4, T5, T6, TR> BuildWrappedFactory<T1, T2, T3, T4, T5, T6, TR>(Expression<Func<T1, T2, T3, T4, T5, T6, TR>> newFunc)
			where TR : TypeWrapper
		{
			return (Func<T1, T2, T3, T4, T5, T6, TR>)BuildFactoryImpl<TR>(newFunc, true);
		}

		public Func<object> BuildFactory<TR>(Expression<Func<TR>> newFunc)
		{
			return (Func<object>)BuildFactoryImpl<TR>(newFunc, false);
		}

		public Func<T, object> BuildFactory<T, TR>(Expression<Func<T, TR>> newFunc)
		{
			return (Func<T, object>)BuildFactoryImpl<TR>(newFunc, false);
		}

		public Func<T1, T2, object> BuildFactory<T1, T2, TR>(Expression<Func<T1, T2, TR>> newFunc)
		{
			return (Func<T1, T2, object>)BuildFactoryImpl<TR>(newFunc, false);
		}

		public Func<T1, T2, T3, object> BuildFactory<T1, T2, T3, TR>(Expression<Func<T1, T2, T3, TR>> newFunc)
		{
			return (Func<T1, T2, T3, object>)BuildFactoryImpl<TR>(newFunc, false);
		}

		public Func<TRes> BuildTypedFactory<TR, TRes>(Expression<Func<TR>> newFunc)
		{
			return (Func<TRes>)BuildFactoryImpl<TR, TRes>(newFunc, false);
		}

		public Func<T, TRes> BuildTypedFactory<T, TR, TRes>(Expression<Func<T, TR>> newFunc)
		{
			return (Func<T, TRes>)BuildFactoryImpl<TR, TRes>(newFunc, false);
		}

		public Func<T1, T2, TRes> BuildTypedFactory<T1, T2, TR, TRes>(Expression<Func<T1, T2, TR>> newFunc)
		{
			return (Func<T1, T2, TRes>)BuildFactoryImpl<TR, TRes>(newFunc, false);
		}

		public Func<T1, T2, T3, TRes> BuildTypedFactory<T1, T2, T3, TR, TRes>(Expression<Func<T1, T2, T3, TR>> newFunc)
		{
			return (Func<T1, T2, T3, TRes>)BuildFactoryImpl<TR, TRes>(newFunc, false);
		}

		#endregion

		#region BuildWrapFunc

		private Delegate BuildFactoryImpl<T>(LambdaExpression lambda, bool wrapResult)
		{
			// TODO: here are two optimizations that could be done to make generated action a bit faster:
			// 1. require caller to pass unwrapped instance, so we don't need to generate instance_ property access
			// 2. generate wrapper constructor call instead of Wrap method call (will need null check of wrapped value)
			var wrapperType = typeof(T);
			if (!TryMapType(wrapperType, out var _))
				throw new LinqToDBException($"Wrapper type {wrapperType} is not registered");

			return BuildWrapperImpl(lambda, wrapResult, false)!;
		}

		private Delegate BuildFactoryImpl<T, TRes>(LambdaExpression lambda, bool wrapResult)
		{
			// TODO: here are two optimizations that could be done to make generated action a bit faster:
			// 1. require caller to pass unwrapped instance, so we don't need to generate instance_ property access
			// 2. generate wrapper constructor call instead of Wrap method call (will need null check of wrapped value)
			var wrapperType = typeof(T);
			if (!TryMapType(wrapperType, out var _))
				throw new LinqToDBException($"Wrapper type {wrapperType} is not registered");

			return BuildWrapperImpl(lambda, wrapResult, false, typeof(TRes))!;
		}

		private Delegate? BuildWrapper(LambdaExpression lambda, bool optional)
		{
			return BuildWrapperImpl(lambda, true, optional);
		}

		private Delegate? BuildWrapperImpl(LambdaExpression lambda, bool wrapResult, bool optional, Type? returnTypeOverride = null)
		{
			var mappedLambda = MapLambdaInternal(lambda, true, ignoreMissingMembers: optional);
			if (optional && mappedLambda == null)
				return null;

			var parametersMap = new Dictionary<Expression, Expression>();
			for (var i = 0; i < lambda.Parameters.Count; i++)
			{
				var oldParameter = lambda.Parameters[i];

				if (TryMapType(oldParameter.Type, out var mappedType))
				{
					if (typeof(TypeWrapper).IsSameOrParentOf(oldParameter.Type))
						parametersMap.Add(
							mappedLambda!.Parameters[i],
							Expression.Convert(ExpressionHelper.Property(oldParameter, nameof(TypeWrapper.instance_)), mappedType));
					else if (oldParameter.Type.IsEnum)
						parametersMap.Add(
							mappedLambda!.Parameters[i],
							_enumFromWrapperCache[oldParameter.Type].GetBody(oldParameter));
				}
			}

			var expr = mappedLambda!.Body.Transform(parametersMap, static (parametersMap, e) =>
			{
				if (e.NodeType == ExpressionType.Parameter && parametersMap.TryGetValue(e, out var replacement))
					return replacement;

				return e;
			});

			if (typeof(TypeWrapper).IsSameOrParentOf(lambda.ReturnType) && TryMapType(lambda.ReturnType, out var returnType))
			{
				if (wrapResult)
				{
					expr = expr.Transform((mapper: this, lambda, returnType), static (context, e) =>
					{
						if (e.Type == context.returnType)
							return Expression.Convert(Expression.Call(
								Expression.Constant(context.mapper),
								_wrapInstanceMethodInfo,
								Expression.Constant(context.lambda.ReturnType),
								e), context.lambda.ReturnType);

						return e;
					});
				}
				else
				{
					expr = expr.Transform((returnTypeOverride, returnType), static (ctx, e) =>
					{
						if (e.Type == ctx.returnType)
							return Expression.Convert(e, ctx.returnTypeOverride ?? typeof(object));

						return e;
					});
				}
			}

			return Expression.Lambda(expr, lambda.Parameters).CompileExpression();
		}

		#endregion

		[return: NotNullIfNotNull(nameof(instance))]
		public TR? Wrap<TR>(object? instance)
			where TR: TypeWrapper
		{
			if (instance == null)
				return null;

			return (TR)Wrap(typeof(TR), instance);
		}

		[return: NotNullIfNotNull(nameof(instance))]
		private object? Wrap(Type wrapperType, object? instance)
		{
			if (instance == null)
				return null;

			if (!_wrapperFactoryCache.TryGetValue(wrapperType, out var factory))
				throw new LinqToDBException($"Missing type wrapper factory registration for type {wrapperType}");

			return factory(instance);
		}

		public async Task<TR?> WrapTask<TR>(Task instanceTask, Type instanceType, CancellationToken cancellationToken)
			where TR : TypeWrapper
		{
			await instanceTask.ConfigureAwait(false);

			return (TR?)WrapTask(typeof(TR), instanceTask);
		}

		private object? WrapTask(Type wrapperType, Task instance)
		{
			if (!_taskWrapperFactoryCache.TryGetValue(wrapperType, out var factory))
				throw new LinqToDBException($"Missing type wrapper factory registration for type {wrapperType}");

			return factory(instance);
		}
	}
}
