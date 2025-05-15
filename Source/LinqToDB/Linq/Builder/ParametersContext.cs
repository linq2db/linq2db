using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Expressions.Internal;
using LinqToDB.Extensions;
using LinqToDB.Infrastructure;
using LinqToDB.Mapping;
using LinqToDB.Reflection;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	sealed class ParametersContext
	{
		readonly        IUniqueIdGenerator<ParameterAccessor> _accessorIdGenerator = new UniqueIdGenerator<ParameterAccessor>();
		public readonly ExpressionCacheManager                CacheManager;

		public ParametersContext(IQueryExpressions parametersExpression, ExpressionTreeOptimizationContext optimizationContext, IDataContext dataContext)
		{
			ParametersExpression = parametersExpression;
			OptimizationContext  = optimizationContext;
			DataContext          = dataContext;
			CacheManager         = new ExpressionCacheManager(parametersExpression.MainExpression, new UniqueIdGenerator<ExpressionCacheManager>());
		}

		public IQueryExpressions                 ParametersExpression { get; }
		public ExpressionTreeOptimizationContext OptimizationContext  { get; }
		public IDataContext                      DataContext          { get; }
		public MappingSchema                     MappingSchema        => DataContext.MappingSchema;

		public static readonly ParameterExpression ItemParameter = Expression.Parameter(typeof(object));

		static readonly ParameterExpression[] AccessorParameters =
		{
			ExpressionBuilder.QueryExpressionContainerParam,
			ExpressionConstants.DataContextParam,
			ExpressionBuilder.ParametersParam
		};

		public readonly List<ParameterAccessor>           CurrentSqlParameters = new();
		//readonly        Dictionary<Expression,Expression> _expressionAccessors;

		#region Build Parameter

		internal List<(Expression Expression, ColumnDescriptor? Column, ParameterAccessor Accessor)>? _parameters;

		internal Dictionary<int, SqlParameter>? _parametersById;

		internal List<(Func<Expression, IDataContext?, object?[]?, object?> main, Func<Expression, IDataContext?, object?[]?, object?> substituted)>? _parametersDuplicateCheck;

		internal Dictionary<Expression, (Expression used, MappingSchema mappingSchema, Func<IDataContext, MappingSchema, Expression> accessorFunc)>? _dynamicAccessors;

		/// <summary>
		/// Used for comparing query in cache to resolve whether generated expressions are equal.
		/// </summary>
		/// <param name="forExpression">Expression which is used as key to do not generate duplicate comparers.</param>
		/// <param name="dataContext">DataContext which is used to execute <paramref name="accessorFunc"/>.</param>
		/// <param name="accessorFunc">Function, which will used for retrieving current expression during cache comparison.</param>
		/// <returns>Result of execution of accessorFunc</returns>
		public Expression RegisterDynamicExpressionAccessor(Expression forExpression, IDataContext dataContext, MappingSchema mappingSchema, QueryCacheCompareInfo.ExpressionAccessorFunc accessorFunc)
		{
			var result = CacheManager.RegisterDynamicExpressionAccessor(forExpression, dataContext, mappingSchema, accessorFunc);
			return result;
		}

		internal void RegisterNonQueryParameter(SqlParameter parameter)
		{
			CacheManager.RegisterNonQueryParameter(parameter);
		}

		internal enum BuildParameterType
		{
			Default,
			Bool,
			InPredicate
		}

		public Expression SimplifyConversion(Expression expression)
		{
			if (expression.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
			{
				var unary = (UnaryExpression)expression;
				if (unary.Operand.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				{
					var unaryOperand = (UnaryExpression)unary.Operand;
					if (unary.Type == unaryOperand.Operand.Type)
						return unaryOperand.Operand;
				}
				else if (unary.Operand is MemberExpression memberExpression && memberExpression.Member.IsNullableValueMember() && memberExpression.Expression?.Type == expression.Type)
				{
					return memberExpression.Expression!;
				}
			}

			var unwrapped = SequenceHelper.UnwrapConstantAndParameter(expression);
			if (!ReferenceEquals(unwrapped, expression))
				return SimplifyConversion(unwrapped);

			return expression;
		}

		public SqlParameter? BuildParameter(
			IBuildContext?     context,
			Expression         expr,
			ColumnDescriptor?  columnDescriptor,
			bool               doNotCheckCompatibility = false,
			string?            alias                   = null,
			BuildParameterType buildParameterType      = BuildParameterType.Default)
		{
			if (columnDescriptor is null && expr is ConstantExpression { Value: null })
				return null;

			if (expr.Type == typeof(void))
				return null;

			expr = SimplifyConversion(expr);

			var suggested     = ExpressionCacheManager.SuggestParameterDisplayName(expr);
			var parameterName = suggested ?? alias;

			if (parameterName == null && columnDescriptor != null)
			{
				if (columnDescriptor.MemberName.Contains("."))
					parameterName = columnDescriptor.ColumnName;
				else
					parameterName = columnDescriptor.MemberName;
			}

			parameterName ??= "p";

			var mappingSchema = context?.MappingSchema ?? MappingSchema;
			var entry         = PrepareParameterCacheEntry(mappingSchema, expr, parameterName, columnDescriptor, doNotCheckCompatibility, buildParameterType);

			if (entry is null)
				return null;

			var finalParameterId = entry.ParameterId;

			var unwrapped = expr.UnwrapConvert();
			var forceNew  = false;

			if (unwrapped.NodeType == ExpressionType.Constant)
				forceNew = CanBeConstant(unwrapped);

			if (forceNew)
				CacheManager.RegisterParameterEntry(expr, entry);
			else
			{
				if (context?.Builder != null && mappingSchema.IsScalarType(expr.Type))
					CacheManager.RegisterParameterEntry(expr, entry, context.Builder.EvaluateExpression, out finalParameterId);
				else
					CacheManager.RegisterParameterEntry(expr, entry, null, out finalParameterId);
			}

			_parametersById ??= new();

			if (_parametersById.TryGetValue(finalParameterId, out var sqlParameter))
				return sqlParameter;

			sqlParameter = new SqlParameter(entry.DbDataType, entry.ParameterName, null)
			{
				AccessorId       = finalParameterId,
				IsQueryParameter = !(context != null ? context.Builder.GetTranslationModifier().InlineParameters : DataContext.InlineParameters)
			};

			_parametersById[finalParameterId] = sqlParameter;

			return sqlParameter;
		}

		ParameterCacheEntry? PrepareParameterCacheEntry(MappingSchema mappingSchema, Expression paramExpression, string parameterName, ColumnDescriptor? columnDescriptor, bool doNotCheckCompatibility, BuildParameterType buildParameterType)
		{
			Type? elementType     = null;
			var   isParameterList = buildParameterType == BuildParameterType.InPredicate;

			if (isParameterList)
			{
				elementType = paramExpression.Type.GetItemType()
				              ?? paramExpression.UnwrapConvert().Type.GetItemType()
				              ?? columnDescriptor?.MemberType;

				if (elementType == null)
					return null;
			}

			var originalAccessor = paramExpression;
			var valueType        = elementType ?? paramExpression.Type;

			var paramDataType = columnDescriptor?.GetDbDataType(true) ?? mappingSchema.GetDbDataType(valueType);

			var        objParam                   = ItemParameter;
			Expression defaultProviderValueGetter = Expression.Convert(objParam, valueType);
			var        providerValueGetter        = defaultProviderValueGetter;

			if (!typeof(DataParameter).IsSameOrParentOf(paramExpression.Type))
			{
				if (columnDescriptor != null && originalAccessor is not BinaryExpression)
				{
					paramDataType = columnDescriptor
						.GetDbDataType(true)
						.WithSystemType(valueType);

					if (valueType != columnDescriptor.MemberType)
					{
						var memberType = columnDescriptor.MemberType;
						var noConvert  = providerValueGetter.UnwrapConvert();

						if (noConvert.Type != typeof(object))
						{
							providerValueGetter = noConvert;
						}
						else if (!isParameterList && providerValueGetter.Type != paramExpression.Type)
						{
							providerValueGetter = Expression.Convert(noConvert, paramExpression.Type);
						}
						else if (providerValueGetter.Type == typeof(object))
						{
							var convertLambda = GenerateConvertFromObject(mappingSchema, memberType);
							if (convertLambda == null)
								return null;

							providerValueGetter = InternalExtensions.ApplyLambdaToExpression(convertLambda, noConvert);
						}

						if (providerValueGetter.Type != memberType
							&& !(providerValueGetter.Type.IsNullable() && providerValueGetter.Type.ToNullableUnderlying() == memberType.ToNullableUnderlying()))
						{
							if (memberType.IsValueType ||
								!memberType.IsSameOrParentOf(providerValueGetter.Type))
							{
								var convertLambda = MappingSchema.GetConvertExpression(providerValueGetter.Type, memberType, checkNull: true, createDefault: false);
								if (convertLambda != null)
								{
									providerValueGetter = InternalExtensions.ApplyLambdaToExpression(convertLambda, providerValueGetter);
								}
							}

							if (providerValueGetter.Type.IsNullable() && providerValueGetter.Type.ToNullableUnderlying() != memberType)
							{
								var toType = providerValueGetter.Type.IsNullableValueType() && memberType.IsValueType && !memberType.IsNullableValueType()
									? memberType.MakeNullable()
									: memberType;

								var convertLambda   = MappingSchema.GenerateSafeConvert(providerValueGetter.Type, toType);
								providerValueGetter = InternalExtensions.ApplyLambdaToExpression(convertLambda, providerValueGetter);
							}

							if (providerValueGetter.Type != memberType && memberType.IsNullableType() && providerValueGetter.Type.UnwrapNullableType() == memberType.UnwrapNullableType())
							{
								providerValueGetter = Expression.Convert(providerValueGetter, memberType);
							}
						}
					}
					else if (valueType != providerValueGetter.Type)
					{
						providerValueGetter = Expression.Convert(providerValueGetter, valueType);
					}

					providerValueGetter = columnDescriptor.ApplyConversions(providerValueGetter, paramDataType, true);
				}
				else
				{
					if (buildParameterType == BuildParameterType.Bool)
					{
						// right now, do nothing
					}
					else
					{
						// Try GetConvertExpression<.., DataParameter>() first.
						//
						if (providerValueGetter.Type != typeof(DataParameter))
						{
							LambdaExpression? convertExpr = null;
							if (buildParameterType == BuildParameterType.Default
								&& !HasDbMapping(MappingSchema, providerValueGetter.Type, out convertExpr))
							{
								if (!doNotCheckCompatibility)
									return null;
							}

							providerValueGetter = convertExpr != null
								? InternalExtensions.ApplyLambdaToExpression(convertExpr, providerValueGetter)
								: ColumnDescriptor.ApplyConversions(MappingSchema, providerValueGetter, paramDataType, null, true);
						}
						else
						{
							providerValueGetter = ColumnDescriptor.ApplyConversions(MappingSchema, providerValueGetter, paramDataType, null, true);
						}
					}
				}
			}

			Expression? dbDataTypeExpression = null;

			if (typeof(DataParameter).IsSameOrParentOf(providerValueGetter.Type))
			{
				dbDataTypeExpression = Expression.Property(providerValueGetter, Methods.LinqToDB.DataParameter.DbDataType);

				if (columnDescriptor != null)
				{
					var dbDataType = columnDescriptor.GetDbDataType(false);
					paramDataType = dbDataType.WithSystemType(valueType);

					dbDataTypeExpression = Expression.Call(Expression.Constant(dbDataType),
						DbDataType.WithSetValuesMethodInfo, dbDataTypeExpression);

					dbDataTypeExpression = Expression.Call(dbDataTypeExpression, DbDataType.WithSystemTypeMethodInfo, Expression.Constant(valueType));
				}

				providerValueGetter = Expression.Property(providerValueGetter, Methods.LinqToDB.DataParameter.Value);
			}

			if (ReferenceEquals(providerValueGetter, defaultProviderValueGetter))
			{
				providerValueGetter = null;
			}
			else {
				//providerValueGetter = CorrectAccessorExpression(providerValueGetter, DataContext);
				if (providerValueGetter.Type != typeof(object))
					providerValueGetter = Expression.Convert(providerValueGetter, typeof(object));
			}

			var parameterCacheEntry = new ParameterCacheEntry(
				_accessorIdGenerator.GetNext(),
				parameterName,
				paramDataType,
				paramExpression, 
				isParameterList ? null : providerValueGetter,
				isParameterList ? providerValueGetter : null,
				dbDataTypeExpression);

			return parameterCacheEntry;
		}

		static LambdaExpression? GenerateConvertFromObject(MappingSchema mappingSchema, Type toType)
		{
			var param = Expression.Parameter(typeof(object), "p");
			var continuation = Expression.Parameter(toType, "cont");

			Expression convertBody = Expression.Condition(Expression.Equal(param, Expression.Constant(null)),
				Expression.Default(toType),
				continuation
			);

			var underlying = toType.ToNullableUnderlying();

			if (underlying != toType)
			{
				convertBody = Inject(convertBody, Expression.Condition(Expression.TypeIs(param, underlying),
					Expression.Convert(param, toType),
					continuation));
			}

			if (underlying.IsEnum)
			{
				var intConverter = mappingSchema.GetConvertExpression(new DbDataType(typeof(int)), new DbDataType(toType), checkNull : false, createDefault : true);

				if (intConverter != null)
				{
					convertBody = Inject(convertBody, Expression.Condition(
						Expression.TypeIs(param, typeof(int)),
						Expression.Invoke(intConverter, Expression.Convert(param, typeof(int))), 
						continuation));
				}

				var stringConverter = mappingSchema.GetConvertExpression(new DbDataType(typeof(string)), new DbDataType(toType), checkNull : false, createDefault : true);

				if (stringConverter != null)
				{
					convertBody = Inject(convertBody, Expression.Condition(
						Expression.TypeIs(param, typeof(string)),
						Expression.Invoke(stringConverter, Expression.Convert(param, typeof(string))),
						continuation));
				}
			}

			var defaultConverter = mappingSchema.GetConvertExpression(new DbDataType(typeof(object)), new DbDataType(toType), checkNull : false, createDefault : true);

			convertBody = Inject(convertBody, Expression.Invoke(defaultConverter!, param));

			return Expression.Lambda(convertBody, param);

			// --- helper method ---
			Expression Inject(Expression expr, Expression addition)
			{
				return expr.Replace(continuation, addition);
			}
		}

		static bool HasDbMapping(MappingSchema mappingSchema, Type testedType, out LambdaExpression? convertExpr)
		{
			if (mappingSchema.IsScalarType(testedType) && testedType != typeof(object))
			{
				convertExpr = null;
				return true;
			}

			convertExpr = mappingSchema.GetConvertExpression(testedType, typeof(DataParameter), false, false);

			if (convertExpr != null)
				return true;

			if (testedType == typeof(object))
				return false;

			var dataType = mappingSchema.GetDataType(testedType);
			if (dataType.Type.DataType != DataType.Undefined)
				return true;

			var notNullable = testedType.ToNullableUnderlying();

			if (notNullable != testedType)
				return HasDbMapping(mappingSchema, notNullable, out convertExpr);

			// TODO: Workaround, wee need good TypeMapping approach
			if (testedType.IsArray)
			{
				convertExpr = null;
				return HasDbMapping(mappingSchema, testedType.GetElementType()!, out _);
			}

			if (!testedType.IsEnum)
				return false;

			var defaultMapping = Converter.GetDefaultMappingFromEnumType(mappingSchema, testedType);
			if (defaultMapping != null && defaultMapping != testedType)
				return HasDbMapping(mappingSchema, defaultMapping, out convertExpr);

			var enumDefault = mappingSchema.GetDefaultFromEnumType(testedType);
			if (enumDefault != null && enumDefault != testedType)
				return HasDbMapping(mappingSchema, enumDefault, out convertExpr);

			return false;
		}

		public Expression ApplyAccessors(Expression expression)
		{
			return CacheManager.ApplyAccessors(expression);
		}

		#endregion

		internal static ParameterAccessor CreateParameterAccessor(
			IUniqueIdGenerator         accessorIdGenerator,
			IDataContext               dataContext,
			Expression                 clientAccessorExpression,
			Func<object?, object?>?    providerConvertFunc,
			Func<object?, object?>?    itemProviderConvertFunc,
			DbDataType                 dbDataType,
			Func<object?, DbDataType>? dbDataTypeAccessorFunc,
			Expression                 expression,
			Expression?                parametersExpression,
			string?                    name)
		{
			// Extracting name for parameter
			//
			if (name == null && expression.Type == typeof(DataParameter))
			{
				var dp = expression.EvaluateExpression<DataParameter>();
				if (dp != null && !string.IsNullOrEmpty(dp.Name))
					name = dp.Name;
			}

			name ??= "p";

			// see #820
			clientAccessorExpression = CorrectAccessorExpression(clientAccessorExpression, dataContext);

			var clientValueMapper = Expression.Lambda<Func<IQueryExpressions,IDataContext?,object?[]?,object?>>(
				Expression.Convert(clientAccessorExpression, typeof(object)), AccessorParameters);

			var clientValueFunc = clientValueMapper.CompileExpression();

			var accessorId = accessorIdGenerator.GetNext();
			return new ParameterAccessor(
					accessorId,
					clientValueFunc,
					providerConvertFunc,
					itemProviderConvertFunc,
					dbDataTypeAccessorFunc,
					new SqlParameter(dbDataType, name, null)
					{
						AccessorId = accessorId,
						IsQueryParameter = !dataContext.InlineParameters
					}
				)
#if DEBUG
				{
					AccessorExpr     = clientValueMapper,
				}
#endif
				;
		}

		public static Expression CorrectAccessorExpression(Expression accessorExpression, IDataContext dataContext)
		{
			// see #820
			accessorExpression = accessorExpression.Transform(dataContext, static (context, e) =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.MemberAccess:
					{
						var ma = (MemberExpression) e;

						if (ma.Member.IsNullableValueMember())
						{
							return Expression.Condition(
								Expression.Equal(ma.Expression!, Expression.Constant(null, ma.Expression!.Type)),
								Expression.Default(e.Type),
								e);
						}

						return e;
					}
					case ExpressionType.Convert       :
					case ExpressionType.ConvertChecked:
					{
						var ce = (UnaryExpression) e;
						if (ce.Operand.Type.IsNullable() && !ce.Type.IsNullable())
						{
							return Expression.Condition(
								Expression.Equal(ce.Operand, Expression.Constant(null, ce.Operand.Type)),
								Expression.Default(e.Type),
								e);
						}

						return e;
					}

					case ExpressionType.Extension:
					{
						if (e is SqlQueryRootExpression root)
						{
							var newExpr = (Expression)ExpressionConstants.DataContextParam;
							if (newExpr.Type != e.Type)
								newExpr = Expression.Convert(newExpr, e.Type);
							return newExpr;
						}

						return e;
					}
					default:
						return e;
				}
			})!;

			return accessorExpression;
		}

		public bool CanBeConstant(Expression expr)
		{
			if (CacheManager.TryFindParameterEntry(expr, out _))
				return false;
			return true;
		}

		public void MarkAsValue(Expression expression, object? value)
		{
			CacheManager.MarkAsValue(expression, value);
		}

		public void RegisterSqlValue(Expression constantExpr, SqlValue value)
		{
			CacheManager.RegisterSqlValue(constantExpr, value);
		}

	}
}
