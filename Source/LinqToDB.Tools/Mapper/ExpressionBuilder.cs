using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

using static System.Linq.Expressions.Expression;

// ReSharper disable TailRecursiveCall

namespace LinqToDB.Tools.Mapper
{
	using Common.Internal;
	using Expressions;
	using Extensions;
	using Reflection;

	sealed class ExpressionBuilder
	{
		sealed class BuilderData
		{
			public BuilderData(Tuple<MemberInfo[],LambdaExpression>[]? memberMappers) => MemberMappers = memberMappers;

			public readonly Tuple<MemberInfo[],LambdaExpression>[]?          MemberMappers;
			public readonly Dictionary<Tuple<Type,Type>,ParameterExpression> Mappers     = new ();
			public readonly HashSet<Tuple<Type,Type>>                        MapperTypes = new ();
			public readonly List<ParameterExpression>                        Locals      = new ();
			public readonly List<Expression>                                 Expressions = new ();

			public ParameterExpression? LocalDic;

			public int  NameCounter;
			public int  RestartCounter;

			public bool IsRestart => RestartCounter > 10;
		}

		public ExpressionBuilder(IMapperBuilder mapperBuilder, Tuple<MemberInfo[],LambdaExpression>[]? memberMappers)
			: this(mapperBuilder, new BuilderData(memberMappers)) =>
			_processCrossReferences = mapperBuilder.ProcessCrossReferences == true;

		ExpressionBuilder(IMapperBuilder mapperBuilder, BuilderData data)
		{
			_mapperBuilder          = mapperBuilder;
			_data                   = data;
			_fromType               = mapperBuilder.FromType;
			_toType                 = mapperBuilder.ToType;
			_processCrossReferences = true;
		}

		readonly IMapperBuilder _mapperBuilder;
		readonly Type           _fromType;
		readonly Type           _toType;
		readonly BuilderData    _data;
		readonly bool           _processCrossReferences;

		#region GetExpression

		public LambdaExpression? GetExpression()
		{
			if (_mapperBuilder.MappingSchema.IsScalarType(_fromType) || _mapperBuilder.MappingSchema.IsScalarType(_toType))
				return _mapperBuilder.MappingSchema.GetConvertExpression(_fromType, _toType);

			var pFrom = Parameter(_fromType, "from");

			Expression? expr;

			if (_mapperBuilder.MappingSchema.IsScalarType(_fromType) || _mapperBuilder.MappingSchema.IsScalarType(_toType))
			{
				expr = GetExpressionImpl(pFrom, _toType);
			}
			else if (_processCrossReferences)
			{
				_data.LocalDic = Parameter(typeof(IDictionary<object,object>), FormattableString.Invariant($"ldic{++_data.NameCounter}"));
				_data.Locals.Add(_data.LocalDic);
				_data.Expressions.Add(Assign(_data.LocalDic, New(MemberHelper.ConstructorOf(() => new Dictionary<object,object>()))));

				expr = new MappingImpl(this, pFrom, Constant(_mapperBuilder.MappingSchema.GetDefaultValue(_toType), _toType)).GetExpression();
			}
			else
			{
				expr = GetExpressionImpl(pFrom, _toType);
			}

			if (_data.IsRestart)
			{
				_mapperBuilder.ProcessCrossReferences = true;
				return new ExpressionBuilder(_mapperBuilder, new BuilderData(_data.MemberMappers)).GetExpression();
			}

			var l = Lambda(
				_data.Locals.Count > 0 || _data.Expressions.Count > 0 ?
					Block(_data.Locals, _data.Expressions.Concat(new [] { expr! })) :
					expr!,
				pFrom);

			return l;
		}

		LambdaExpression? GetExpressionInner()
		{
			if (_mapperBuilder.MappingSchema.IsScalarType(_fromType) || _mapperBuilder.MappingSchema.IsScalarType(_toType))
				return _mapperBuilder.MappingSchema.GetConvertExpression(_fromType, _toType);

			var pFrom = Parameter(_fromType, "from");
			var expr  = GetExpressionImpl(pFrom, _toType);

			if (_data.IsRestart)
				return null;

			var l = Lambda(expr!, pFrom);

			return l;
		}

		Expression? GetExpressionImpl(Expression fromExpression, Type toType)
		{
			var fromAccessor = TypeAccessor.GetAccessor(fromExpression.Type);
			var toAccessor   = TypeAccessor.GetAccessor(toType);
			var binds        = new List<MemberAssignment>();
			var key          = new Tuple<Type,Type>(fromExpression.Type, toType);

			if (!_data.MapperTypes.Add(key))
			{
				_data.RestartCounter++;

				if (_data.IsRestart)
					return null;
			}

			var initExpression = BuildCollectionMapper(fromExpression, toType);

			if (initExpression != null)
				return initExpression;

			foreach (var toMember in toAccessor.Members.Where(_mapperBuilder.ToMemberFilter))
			{
				if (_data.IsRestart)
					return null;

				if (!toMember.HasSetter)
					continue;

				if (_data.MemberMappers != null)
				{
					var processed = false;

					foreach (var item in _data.MemberMappers)
					{
						if (item.Item1.Length == 1 && item.Item1[0] == toMember.MemberInfo)
						{
							binds.Add(BuildAssignment(item.Item2.GetBody, fromExpression, item.Item2.Type, toMember));
							processed = true;
							break;
						}
					}

					if (processed)
						continue;
				}

				if (_mapperBuilder.ToMappingDictionary == null ||
					!_mapperBuilder.ToMappingDictionary.TryGetValue(toType, out var mapDic) ||
					!mapDic.TryGetValue(toMember.Name, out var toName))
					toName = toMember.Name;

				var fromMember = fromAccessor.Members.FirstOrDefault(mi =>
				{
					if (_mapperBuilder.FromMappingDictionary == null ||
						!_mapperBuilder.FromMappingDictionary.TryGetValue(fromExpression.Type, out mapDic) ||
						!mapDic.TryGetValue(mi.Name, out var fromName))
						fromName = mi.Name;
					return fromName == toName;
				});

				if (fromMember == null || !fromMember.HasGetter)
					continue;

				if (_mapperBuilder.MappingSchema.IsScalarType(fromMember.Type) || _mapperBuilder.MappingSchema.IsScalarType(toMember.Type))
				{
					binds.Add(BuildAssignment(fromMember.GetGetterExpression, fromExpression, fromMember.Type, toMember));
				}
				else if (fromMember.Type == toMember.Type && _mapperBuilder.DeepCopy == false)
				{
					binds.Add(Bind(toMember.MemberInfo, fromMember.GetGetterExpression(fromExpression)));
				}
				else
				{
					var getValue = fromMember.GetGetterExpression(fromExpression);
					var exExpr   = GetExpressionImpl(getValue, toMember.Type);

					if (_data.IsRestart)
						return null;

					var expr     = Condition(
						Equal(getValue, Constant(_mapperBuilder.MappingSchema.GetDefaultValue(getValue.Type), getValue.Type)),
						Constant(_mapperBuilder.MappingSchema.GetDefaultValue(toMember.Type), toMember.Type),
						exExpr!);

					binds.Add(Bind(toMember.MemberInfo, expr));
				}
			}

			if (_data.IsRestart)
				return null;

			var newExpression = GetNewExpression(toType);

			initExpression =
				Convert(
					binds.Count > 0
						? MemberInit(newExpression, binds)
						: newExpression,
					toType);

			return initExpression;
		}

		static NewExpression GetNewExpression(Type originalType)
		{
			var type = originalType;

			if (type.IsInterface && type.IsGenericType)
			{
				var definition = type.GetGenericTypeDefinition();

				if (definition == typeof(IList<>) || definition == typeof(IEnumerable<>))
				{
					type = typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]);
				}
			}

			return New(type);
		}

		[return: NotNullIfNotNull(nameof(expr))]
		static Expression? Convert(Expression? expr, Type toType) =>
			expr == null ? null : expr.Type == toType ? expr : Expression.Convert(expr, toType);

		Expression? BuildCollectionMapper(Expression fromExpression, Type toType)
		{
			var fromType = fromExpression.Type;

			if (toType.IsSubClassOf(typeof(IEnumerable<>)) && fromType.IsSubClassOf(typeof(IEnumerable<>)))
				return Convert(ConvertCollection(fromExpression, toType), toType);

			return null;
		}

		Expression? ConvertCollection(Expression fromExpression, Type toType)
		{
			var fromType     = fromExpression.Type;
			var fromItemType = fromType.GetItemType()!;
			var toItemType   = toType.  GetItemType()!;

			if (toType.IsGenericType && !toType.IsGenericTypeDefinition)
			{
				var toDefinition = toType.GetGenericTypeDefinition();

				if (toDefinition == typeof(List<>) || typeof(List<>).IsSubClassOf(toDefinition))
					return Convert(
						ToList(this, fromExpression, fromItemType, toItemType), toType);

				if (toDefinition == typeof(HashSet<>) || typeof(HashSet<>).IsSubClassOf(toDefinition))
					return Convert(
						ToHashSet(this, fromExpression, fromItemType, toItemType), toType);
			}

			if (toType.IsArray)
				return Convert(ToArray(this, fromExpression, fromItemType, toItemType), toType);

			throw new NotImplementedException();
		}

		MemberAssignment BuildAssignment(
			Func<Expression, Expression> getter,
			Expression                   fromExpression,
			Type                         fromMemberType,
			MemberAccessor               toMember)
		{
			var getValue = getter(fromExpression);
			var expr     = _mapperBuilder.MappingSchema.GetConvertExpression(fromMemberType, toMember.Type)!;
			var convert  = expr.GetBody(getValue);

			return Bind(toMember.MemberInfo, convert);
		}

		#endregion

		#region GetExpressionEx

		public LambdaExpression GetExpressionEx()
		{
			var pFrom = Parameter(_fromType, "from");
			var pTo   = Parameter(_toType,   "to");
			var pDic  = Parameter(typeof(IDictionary<object,object>), FormattableString.Invariant($"dic{++_data.NameCounter}"));

			if (_mapperBuilder.MappingSchema.IsScalarType(_fromType) || _mapperBuilder.MappingSchema.IsScalarType(_toType))
			{
				return Lambda(
					_mapperBuilder.MappingSchema.GetConvertExpression(_fromType, _toType)!.GetBody(pFrom),
					pFrom,
					pTo,
					pDic);
			}

			_data.LocalDic = Parameter(typeof(IDictionary<object,object>), FormattableString.Invariant($"ldic{++_data.NameCounter}"));
			_data.Locals.     Add(_data.LocalDic);
			_data.Expressions.Add(Assign(_data.LocalDic, pDic));

			var expr = new MappingImpl(this, pFrom, pTo).GetExpression();

			var l = Lambda(Block(_data.Locals, _data.Expressions.Concat(new[] { expr })), pFrom, pTo, pDic);

			return l;
		}

		LambdaExpression GetExpressionExInner()
		{
			var pFrom = Parameter(_fromType, "from");
			var pTo   = Parameter(_toType,   "to");

			if (_mapperBuilder.MappingSchema.IsScalarType(_fromType) || _mapperBuilder.MappingSchema.IsScalarType(_toType))
			{
				return Lambda(
					_mapperBuilder.MappingSchema.GetConvertExpression(_fromType, _toType)!.GetBody(pFrom),
					pFrom,
					pTo);
			}

			var expr = new MappingImpl(this, pFrom, pTo).GetExpression();

			var l = Lambda(expr, pFrom, pTo);

			return l;
		}

		sealed class MappingImpl
		{
			public MappingImpl(
				ExpressionBuilder builder,
				Expression        fromExpression,
				Expression        toExpression)
			{
				_builder        = builder;
				_fromExpression = fromExpression;
				_toExpression   = toExpression;
				_localObject    = Parameter(_toExpression.Type, FormattableString.Invariant($"obj{++_builder._data.NameCounter}"));
				_fromAccessor   = TypeAccessor.GetAccessor(_fromExpression.Type);
				_toAccessor     = TypeAccessor.GetAccessor(_toExpression.  Type);
				_cacheMapper    = _builder._mapperBuilder.ProcessCrossReferences != false;
			}

			readonly ExpressionBuilder         _builder;
			readonly Expression                _fromExpression;
			readonly Expression                _toExpression;
			readonly ParameterExpression       _localObject;
			readonly TypeAccessor              _fromAccessor;
			readonly TypeAccessor              _toAccessor;
			readonly List<Expression>          _expressions = new ();
			readonly List<ParameterExpression> _locals      = new ();
			readonly bool                      _cacheMapper;

			public Expression GetExpression()
			{
				_locals.Add(_localObject);

				if (!BuildArrayMapper())
				{
					var newLocalObjectExpr = GetNewExpression(_toExpression.Type);

					//_actualLocalObjectType = newLocalObjectExpr.Type;

					_expressions.Add(Assign(
						_localObject,
						Condition(
							Equal(
								_toExpression,
								Constant(
									_builder._mapperBuilder.MappingSchema.GetDefaultValue(_toExpression.Type),
									_toExpression.Type)),
							Convert(newLocalObjectExpr, _toExpression.Type),
							_toExpression)));

					if (_cacheMapper)
					{
						_expressions.Add(
							Call(
								MemberHelper.MethodOf(() => Add(null!, null!, null!)),
								_builder._data.LocalDic!,
								_fromExpression,
								_localObject));
					}

					if (!BuildListMapper())
						GetObjectExpression();
				}

				_expressions.Add(_localObject);

				var expr = Block(_locals, _expressions) as Expression;

				if (_cacheMapper)
					expr = Expression.Convert(
						Coalesce(
							Call(
								MemberHelper.MethodOf<IDictionary<object,object>>(_ => GetValue(null!, null!)),
								_builder._data.LocalDic!,
								_fromExpression),
							expr),
						_toExpression.Type);

				return expr;
			}

			void GetObjectExpression()
			{
				foreach (var toMember in _toAccessor.Members.Where(_builder._mapperBuilder.ToMemberFilter))
				{
					if (!toMember.HasSetter)
						continue;

					if (_builder._data.MemberMappers != null)
					{
						var processed = false;

						foreach (var item in _builder._data.MemberMappers)
						{
							if (item.Item1.Length == 1 && item.Item1[0] == toMember.MemberInfo)
							{
								_expressions.Add(BuildAssignment(item.Item2.GetBody, toMember.GetSetterExpression, item.Item2.Type, _localObject, toMember));
								processed = true;
								break;
							}
						}

						if (processed)
							continue;
					}

					if (_builder._mapperBuilder.ToMappingDictionary == null ||
						!_builder._mapperBuilder.ToMappingDictionary.TryGetValue(_toExpression.Type, out var mapDic) ||
						!mapDic.TryGetValue(toMember.Name, out var toName))
						toName = toMember.Name;

					var fromMember = _fromAccessor.Members.FirstOrDefault(mi =>
					{
						if (_builder._mapperBuilder.FromMappingDictionary == null ||
							!_builder._mapperBuilder.FromMappingDictionary.TryGetValue(_fromExpression.Type, out mapDic) ||
							!mapDic.TryGetValue(mi.Name, out var fromName))
							fromName = mi.Name;
						return fromName == toName;
					});

					if (fromMember == null || !fromMember.HasGetter)
						continue;

					if (_builder._mapperBuilder.MappingSchema.IsScalarType(fromMember.Type) ||
						_builder._mapperBuilder.MappingSchema.IsScalarType(toMember.Type))
					{
						_expressions.Add(BuildAssignment(fromMember.GetGetterExpression, toMember.GetSetterExpression, fromMember.Type, _localObject, toMember));
					}
					else if (fromMember.Type == toMember.Type && _builder._mapperBuilder.DeepCopy == false)
					{
						_expressions.Add(toMember.GetSetterExpression(_localObject, fromMember.GetGetterExpression(_fromExpression)));
					}
					else
					{
						var getValue = fromMember.GetGetterExpression(_fromExpression);
						var expr     = IfThenElse(
							// if (from == null)
							Equal(getValue, Constant(_builder._mapperBuilder.MappingSchema.GetDefaultValue(getValue.Type), getValue.Type)),
							//   localObject = null;
							toMember.GetSetterExpression(
								_localObject,
								Constant(_builder._mapperBuilder.MappingSchema.GetDefaultValue(toMember.Type), toMember.Type)),
							// else
							toMember.HasGetter ?
								toMember.GetSetterExpression(_localObject, BuildClassMapper(getValue, toMember)) :
								toMember.GetSetterExpression(_localObject, _builder.GetExpressionImpl(getValue, toMember.Type)!));

						_expressions.Add(expr);
					}
				}
			}

			Expression BuildClassMapper(Expression getValue, MemberAccessor toMember)
			{
				var key   = Tuple.Create(_fromExpression.Type, toMember.Type);
				var pFrom = Parameter(getValue.Type, "pFrom");
				var pTo   = Parameter(toMember.Type, "pTo");
				var toObj = toMember.GetGetterExpression(_localObject);

				ParameterExpression? nullPrm = null;

				if (_cacheMapper)
				{
					if (_builder._data.Mappers.TryGetValue(key, out nullPrm))
						return Invoke(nullPrm, getValue, toObj);

					nullPrm = Parameter(
						Lambda(
							Constant(
								_builder._mapperBuilder.MappingSchema.GetDefaultValue(toMember.Type),
								toMember.Type),
							pFrom,
							pTo).Type);

					_builder._data.Mappers.Add(key, nullPrm);
					_builder._data.Locals. Add(nullPrm);
				}

				var expr = new MappingImpl(_builder, _cacheMapper ? pFrom : getValue, _cacheMapper ? pTo : toObj).GetExpression();

				if (_cacheMapper)
				{
					var lex = Lambda(expr, pFrom, pTo);

					_builder._data.Expressions.Add(Assign(nullPrm!, lex));

					expr = Invoke(nullPrm!, getValue, toObj);
				}

				return expr;
			}

			bool BuildArrayMapper()
			{
				var fromType = _fromExpression.Type;
				var toType   = _localObject.Type;

				if (toType.IsArray && fromType.IsSubClassOf(typeof(IEnumerable<>)))
				{
					var fromItemType = fromType.GetItemType()!;
					var toItemType   = toType.  GetItemType()!;

					var expr = ToArray(_builder, _fromExpression, fromItemType, toItemType);

					_expressions.Add(Assign(_localObject, expr!));

					return true;
				}

				return false;
			}

			bool BuildListMapper()
			{
				var fromListType = _fromExpression.Type;
				var toListType   = _localObject.Type;

				if (!toListType.IsSubClassOf(typeof(IEnumerable<>)) || !fromListType.IsSubClassOf(typeof(IEnumerable<>)))
					return false;

				var clearMethodInfo = toListType.GetMethod("Clear");

				if (clearMethodInfo != null)
					_expressions.Add(Call(_localObject, clearMethodInfo));

				var fromItemType = fromListType.GetItemType()!;
				var toItemType   = toListType.  GetItemType()!;

				var addRangeMethodInfo = toListType.GetMethod("AddRange");

				if (addRangeMethodInfo != null)
				{
					var selectExpr = Select(_builder, _fromExpression, fromItemType, toItemType)!;
					_expressions.Add(Call(_localObject, addRangeMethodInfo, selectExpr));
				}
				else if (toListType.IsGenericType && !toListType.IsGenericTypeDefinition)
				{
					if (toListType.IsSubClassOf(typeof(ICollection<>)))
					{
						var selectExpr = Select(
							_builder,
							_fromExpression, fromItemType,
							toItemType)!;

						_expressions.Add(
							Call(
								MemberHelper.MethodOf(() => AddRange<int>(null!, null!))
									.GetGenericMethodDefinition()
									.MakeGenericMethod(toItemType),
								_localObject,
								selectExpr));
					}
					else
					{
						_expressions.Add(
							Assign(
								_localObject,
								_builder.ConvertCollection(_fromExpression, toListType)!));
					}
				}
				else
				{
					throw new NotImplementedException();
				}

				return true;
			}

			Expression BuildAssignment(
				Func<Expression, Expression>             getter,
				Func<Expression, Expression, Expression> setter,
				Type                                     fromMemberType,
				Expression                               toExpression,
				MemberAccessor                           toMember)
			{
				var getValue = getter(_fromExpression);
				var expr     = _builder._mapperBuilder.MappingSchema.GetConvertExpression(fromMemberType, toMember.Type)!;
				var convert  = expr.GetBody(getValue);

				return setter(toExpression, convert);
			}
		}

		#endregion

		static object? GetValue(IDictionary<object,object>? dic, object key) =>
			dic != null && dic.TryGetValue(key, out var result) ? result : null;

		static void Add(IDictionary<object,object>? dic, object? key, object value)
		{
			if (key != null && dic != null)
				dic[key] = value;
		}

		static HashSet<T> ToHashSet<T>([InstantHandle] IEnumerable<T> source) => new (source);

		static void AddRange<T>(ICollection<T> source, [InstantHandle] IEnumerable<T> items)
		{
			foreach (var item in items)
				source.Add(item);
		}

		static IMapperBuilder GetBuilder<TFrom,TTo>(IMapperBuilder builder) =>
			new MapperBuilder<TFrom,TTo>
			{
				MappingSchema          = builder.MappingSchema,
				MemberMappers          = builder.MemberMappers,
				FromMappingDictionary  = builder.FromMappingDictionary,
				ToMappingDictionary    = builder.ToMappingDictionary,
				ToMemberFilter         = builder.ToMemberFilter,
				ProcessCrossReferences = builder.ProcessCrossReferences,
				DeepCopy               = builder.DeepCopy
			};

		static Expression? ToList(
			ExpressionBuilder builder,
			Expression        fromExpression,
			Type              fromItemType,
			Type              toItemType)
		{
			var toListInfo = MemberHelper.MethodOf(() => Enumerable.ToList<int>(null!)).GetGenericMethodDefinition();
			var expr       = Select(builder, fromExpression, fromItemType, toItemType);

			if (builder._data.IsRestart)
				return null;

			return Call(toListInfo.MakeGenericMethod(toItemType), expr!);
		}

		static Expression? ToHashSet(
			ExpressionBuilder builder,
			Expression        fromExpression,
			Type              fromItemType,
			Type              toItemType)
		{
			var toListInfo = MemberHelper.MethodOf(() => ToHashSet<int>(null!)).GetGenericMethodDefinition();
			var expr       = Select(builder, fromExpression, fromItemType, toItemType);

			if (builder._data.IsRestart)
				return null;

			return Call(toListInfo.MakeGenericMethod(toItemType), expr!);
		}

		static Expression? ToArray(
			ExpressionBuilder builder,
			Expression        fromExpression,
			Type              fromItemType,
			Type              toItemType)
		{
			var toListInfo = MemberHelper.MethodOf(() => Enumerable.ToArray<int>(null!)).GetGenericMethodDefinition();
			var expr       = Select(builder, fromExpression, fromItemType, toItemType);

			if (builder._data.IsRestart)
				return null;

			return Call(toListInfo.MakeGenericMethod(toItemType), expr!);
		}

		static Expression? Select(
			ExpressionBuilder builder,
			Expression        getValue,
			Type              fromItemType,
			Type              toItemType)
		{
			var getBuilderInfo = MemberHelper.MethodOf(() => GetBuilder<int,int>(null!)).               GetGenericMethodDefinition();
			var selectInfo     = MemberHelper.MethodOf(() => Enumerable.Select<int,int>(null!, _ => _)).GetGenericMethodDefinition();
			var itemBuilder    =
				getBuilderInfo
					.MakeGenericMethod(fromItemType, toItemType)
					.InvokeExt<IMapperBuilder>(null, new object[] { builder._mapperBuilder });

			var expr = getValue;

			if (builder._mapperBuilder.DeepCopy != false || fromItemType != toItemType)
			{
				Expression? selector;

				var selectorBuilder = new ExpressionBuilder(itemBuilder, builder._data);

				if (builder._data.LocalDic == null)
				{
					selector = selectorBuilder.GetExpressionInner();
				}
				else
				{
					var p  = Parameter(fromItemType);
					var ex = selectorBuilder.GetExpressionExInner();

					selector = Lambda(
						Invoke(ex, p, Constant(builder._mapperBuilder.MappingSchema.GetDefaultValue(toItemType), toItemType)),
						p);
				}

				if (builder._data.IsRestart)
					return null;

				expr = Call(selectInfo.MakeGenericMethod(fromItemType, toItemType), getValue, selector!);
			}

			return expr;
		}
	}
}
