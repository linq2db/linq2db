using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Data;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using Infrastructure;

	sealed class ParametersContext
	{
		readonly IUniqueIdGenerator<ParameterAccessor> _accessorIdGenerator = new UniqueIdGenerator<ParameterAccessor>();

		public ParametersContext(Expression parametersExpression, ExpressionTreeOptimizationContext optimizationContext, IDataContext dataContext)
		{
			ParametersExpression = parametersExpression;
			OptimizationContext  = optimizationContext;
			DataContext          = dataContext;

			_expressionAccessors = parametersExpression.GetExpressionAccessors(ExpressionBuilder.ExpressionParam);
		}

		public Expression                        ParametersExpression { get; }
		public ExpressionTreeOptimizationContext OptimizationContext  { get; }
		public IDataContext                      DataContext          { get; }
		public MappingSchema                     MappingSchema        => DataContext.MappingSchema;

		static readonly ParameterExpression ItemParameter = Expression.Parameter(typeof(object));

		static readonly ParameterExpression[] AccessorParameters =
		{
			ExpressionBuilder.ExpressionParam,
			ExpressionConstants.DataContextParam,
			ExpressionBuilder.ParametersParam
		};


		public readonly List<ParameterAccessor>           CurrentSqlParameters = new();
		readonly        Dictionary<Expression,Expression> _expressionAccessors;

		#region Build Parameter

		internal List<(Expression Expression, ColumnDescriptor? Column, ParameterAccessor Accessor)>? _parameters;

		internal List<(Func<Expression, IDataContext?, object?[]?, object?> main, Func<Expression, IDataContext?, object?[]?, object?> substituted)>? _parametersDuplicateCheck;

		internal Dictionary<Expression, (Expression used, MappingSchema mappingSchema, Func<IDataContext, MappingSchema, Expression> accessorFunc)>? _dynamicAccessors;

		internal void RegisterDuplicateParameter(Expression expression, Func<Expression, IDataContext?, object?[]?, object?> mainAccessor, Func<Expression, IDataContext?, object?[]?, object?> substitutedAccessor)
		{
			_parametersDuplicateCheck ??= new();

			_parametersDuplicateCheck.Add((mainAccessor, substitutedAccessor));
		}

		/// <summary>
		/// Used for comparing query in cache to resolve whether generated expressions are equal.
		/// </summary>
		/// <param name="forExpression">Expression which is used as key to do not generate duplicate comparers.</param>
		/// <param name="dataContext">DataContext which is used to execute <paramref name="accessorFunc"/>.</param>
		/// <param name="accessorFunc">Function, which will used for retrieving current expression during cache comparison.</param>
		/// <returns>Result of execution of accessorFunc</returns>
		public Expression RegisterDynamicExpressionAccessor(Expression forExpression, IDataContext dataContext, MappingSchema mappingSchema, Func<IDataContext, MappingSchema, Expression> accessorFunc)
		{
			var result = accessorFunc(dataContext, mappingSchema);

			_dynamicAccessors ??= new(ExpressionEqualityComparer.Instance);

			if (!_dynamicAccessors.ContainsKey(forExpression))
				_dynamicAccessors.Add(forExpression, (result, mappingSchema, accessorFunc));

			return result;
		}

		internal void AddCurrentSqlParameter(ParameterAccessor parameterAccessor)
		{
			CurrentSqlParameters.Add(parameterAccessor);
		}

		internal enum BuildParameterType
		{
			Default,
			Bool,
			InPredicate
		}

		public ParameterAccessor? BuildParameter(
			IBuildContext?     context,
			Expression         expr,
			ColumnDescriptor?  columnDescriptor,
			bool               forceConstant           = false,
			bool               doNotCheckCompatibility = false,
			bool               forceNew                = false,
			string?            alias                   = null,
			BuildParameterType buildParameterType      = BuildParameterType.Default)
		{
			string? name = alias;

			var newExpr = ReplaceParameter(context?.MappingSchema ?? MappingSchema, expr, columnDescriptor, forceConstant, nm => name = nm);

			var newAccessor = PrepareConvertersAndCreateParameter(newExpr, expr, name, columnDescriptor, doNotCheckCompatibility, buildParameterType);
			if (newAccessor == null)
				return null;

			// do replacing again for registering parameterized constants
			ApplyAccessors(expr, true);

			if (!forceNew && !ReferenceEquals(newExpr.ValueExpression, expr))
			{
				// check that expression is not just compilable expression
				var hasAccessors = HasAccessors(expr);

				// we can find duplicates in this case
				if (hasAccessors)
				{
					var found = newAccessor;

					if (_parameters != null)
					{
						foreach (var (paramExpr, column, accessor) in _parameters)
						{
							// build
							if (!accessor.SqlParameter.Type.Equals(newAccessor.SqlParameter.Type))
								continue;

							// we cannot merge parameters if they have defined ValueConverter
							if (column != null && columnDescriptor != null)
							{
								if (!ReferenceEquals(column, columnDescriptor))
								{
									if (column.ValueConverter != null || columnDescriptor.ValueConverter != null)
										continue;
								}
							}
							else if (!ReferenceEquals(column, columnDescriptor))
								continue;

							if (ReferenceEquals(paramExpr, expr))
							{
								// Its is the same already created parameter
								return accessor;
							}

							if (paramExpr.EqualsTo(expr, OptimizationContext.GetSimpleEqualsToContext(true)))
							{
								found = accessor;
								break;
							}
						}
					}

					// We already have registered parameter for the same expression
					//
					if (!ReferenceEquals(found, newAccessor))
					{
						// registers duplicate parameter check for expression cache
						RegisterDuplicateParameter(expr, found.ClientValueAccessor, newAccessor.ClientValueAccessor);
						return found;
					}
				}
			}

			(_parameters ??= new()).Add((expr, columnDescriptor, newAccessor));
			AddCurrentSqlParameter(newAccessor);

			return newAccessor;
		}

		static bool HasDbMapping(MappingSchema mappingSchema, Type testedType, out LambdaExpression? convertExpr)
		{
			if (mappingSchema.IsScalarType(testedType))
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

		ParameterAccessor? PrepareConvertersAndCreateParameter(ValueTypeExpression newExpr, Expression valueExpression, string? name, ColumnDescriptor? columnDescriptor, bool doNotCheckCompatibility, BuildParameterType buildParameterType)
		{
			if (valueExpression.Type == typeof(void))
				return null;

			Type? elementType     = null;
			var   isParameterList = buildParameterType == BuildParameterType.InPredicate;

			if (isParameterList)
			{
				elementType = newExpr.ValueExpression.Type.GetItemType()
					?? newExpr.ValueExpression.UnwrapConvert().Type.GetItemType()
					?? columnDescriptor?.MemberType
					?? typeof(object);
			}

			var originalAccessor = newExpr.ValueExpression;
			var valueType        = elementType ?? newExpr.ValueExpression.Type;

			var        objParam            = ItemParameter;
			Expression providerValueGetter = Expression.Convert(objParam, valueType);

			if (!newExpr.IsDataParameter)
			{
				if (columnDescriptor != null && originalAccessor is not BinaryExpression)
				{
					newExpr.DataType = columnDescriptor
						.GetDbDataType(true)
						.WithSystemType(valueType);

					if (valueType != columnDescriptor.MemberType)
					{
						var memberType = columnDescriptor.MemberType;
						var noConvert  = providerValueGetter;

						if (noConvert.Type != typeof(object))
							providerValueGetter = noConvert;
						else if (!isParameterList && providerValueGetter.Type != valueExpression.Type)
							providerValueGetter = Expression.Convert(noConvert, valueExpression.Type);
						else if (providerValueGetter.Type == typeof(object))
							providerValueGetter = Expression.Convert(noConvert, elementType != null && elementType != typeof(object) ? elementType : memberType);

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
								var convertLambda = MappingSchema.GenerateSafeConvert(providerValueGetter.Type, memberType);
								providerValueGetter = InternalExtensions.ApplyLambdaToExpression(convertLambda, providerValueGetter);
							}

							if (providerValueGetter.Type != memberType)
							{
								providerValueGetter = Expression.Convert(providerValueGetter, memberType);
							}
						}
					}
					else if (valueType != providerValueGetter.Type)
					{
						providerValueGetter = Expression.Convert(providerValueGetter, valueType);
					}

					providerValueGetter = columnDescriptor.ApplyConversions(providerValueGetter, newExpr.DataType, true);

					if (name == null)
					{
						if (columnDescriptor.MemberName.Contains("."))
							name = columnDescriptor.ColumnName;
						else
							name = columnDescriptor.MemberName;
					}
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
								: ColumnDescriptor.ApplyConversions(MappingSchema, providerValueGetter, newExpr.DataType, null, true);
						}
						else
						{
							providerValueGetter = ColumnDescriptor.ApplyConversions(MappingSchema, providerValueGetter, newExpr.DataType, null, true);
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
					newExpr.DataType = dbDataType;

					dbDataTypeExpression = Expression.Call(Expression.Constant(dbDataType),
						DbDataType.WithSetValuesMethodInfo, dbDataTypeExpression);
				}

				providerValueGetter = Expression.Property(providerValueGetter, Methods.LinqToDB.DataParameter.Value);
			}

			name ??= columnDescriptor?.MemberName;

			Func<object?, object?>? providerValueFunc = null;

			if (providerValueGetter.UnwrapConvert() != objParam)
			{
				providerValueGetter = CorrectAccessorExpression(providerValueGetter, DataContext);
				if (providerValueGetter.Type != typeof(object))
					providerValueGetter = Expression.Convert(providerValueGetter, typeof(object));
				var providerValueLambda = Expression.Lambda<Func<object?, object?>>(providerValueGetter, objParam);
				providerValueFunc = providerValueLambda.CompileExpression();
			}

			Func<object?, DbDataType>? dbDataTypeFunc      = null;

			if (dbDataTypeExpression != null)
			{
				dbDataTypeExpression = CorrectAccessorExpression(dbDataTypeExpression, DataContext);
				var dbDataTypeAccessor = Expression.Lambda<Func<object?, DbDataType>>(dbDataTypeExpression, objParam);
				dbDataTypeFunc = dbDataTypeAccessor.CompileExpression();
			}

			var p = CreateParameterAccessor(
				_accessorIdGenerator,
				DataContext,
				newExpr.ValueExpression,
				isParameterList ? null : providerValueFunc,
				isParameterList ? providerValueFunc : null,
				newExpr.DataType,
				dbDataTypeFunc,
				valueExpression,
				ParametersExpression,
				name
			);

			return p;
		}

		sealed class ValueTypeExpression
		{
			public Expression ValueExpression      = null!;

			public bool       IsDataParameter;
			public DbDataType DataType;
		}

		public Expression ApplyAccessors(Expression expression, bool register)
		{
			var result = expression.Transform(
				(1, paramContext : this, register),
				static (context, expr) =>
				{
					// TODO: !!! Code should be synched with ReplaceParameter !!!
					if (expr.NodeType == ExpressionType.ArrayIndex && ((BinaryExpression)expr).Left == ExpressionBuilder.ParametersParam)
					{
						return new TransformInfo(expr, true);
					}

					if (expr.NodeType == ExpressionType.Constant && context.paramContext.GetAccessorExpression(expr, out var accessor, context.register))
					{
						if (accessor.Type != expr.Type)
							accessor = Expression.Convert(accessor, expr.Type);

						return new TransformInfo(accessor);
					}

					return new TransformInfo(expr);
				});

			return result;
		}

		public bool HasAccessors(Expression expression)
		{
			var result = false;
			expression.Transform(
				(1, paramContext : this),
				(context, expr) =>
				{
					// TODO: !!! Code should be synched with ReplaceParameter !!!
					if (expr.NodeType == ExpressionType.ArrayIndex && ((BinaryExpression)expr).Left == ExpressionBuilder.ParametersParam)
					{
						return new TransformInfo(expr, true);
					}

					if (expr.NodeType == ExpressionType.Constant && context.paramContext.GetAccessorExpression(expr, out var _, false))
					{
						result = true;
					}

					return new TransformInfo(expr, result);
				});

			return result;
		}

		ValueTypeExpression ReplaceParameter(MappingSchema mappingSchema, Expression expression, ColumnDescriptor? columnDescriptor, bool forceConstant, Action<string>? setName)
		{
			var dbDataType = columnDescriptor?.GetDbDataType(true) ?? mappingSchema.GetDbDataType(expression.Type);

			var result = new ValueTypeExpression
			{
				DataType             = dbDataType,
			};

			var unwrapped = expression.Unwrap();
			if (unwrapped.NodeType == ExpressionType.MemberAccess)
			{
				var ma = (MemberExpression)unwrapped;
				setName?.Invoke(ma.Member.Name);
			}

			result.ValueExpression = expression.Transform(
				(forceConstant, columnDescriptor, (expression as MemberExpression)?.Member, result, setName, paramContext: this),
				static (context, expr) =>
				{
					if (expr.NodeType == ExpressionType.ArrayIndex && ((BinaryExpression)expr).Left == ExpressionBuilder.ParametersParam)
					{
						return new TransformInfo(expr, true);
					}

					if (expr.NodeType == ExpressionType.Constant)
					{
						var exprType = expr.Type;
						if (context.paramContext.GetAccessorExpression(expr, out var val, false))
						{
							var constantValue = ((ConstantExpression)expr).Value;

							expr = Expression.Convert(val, exprType);

							if (constantValue is DataParameter dataParameter)
							{
								context.result.IsDataParameter = true;
								context.result.DataType        = dataParameter.DbDataType;
							}
							else if (context.Member != null)
							{
								if (context.columnDescriptor == null)
								{
									var mt = ExpressionBuilder.GetMemberDataType(context.paramContext.MappingSchema, context.Member);

									if (mt.DataType != DataType.Undefined)
									{
										context.result.DataType = context.result.DataType.WithDataType(mt.DataType);
									}

									if (mt.DbType != null)
									{
										context.result.DataType = context.result.DataType.WithDbType(mt.DbType);
									}

									if (mt.Length != null)
									{
										context.result.DataType = context.result.DataType.WithLength(mt.Length);
									}
								}

								context.setName?.Invoke(context.Member.Name);
							}
						}
					}

					return new TransformInfo(expr);
				});

			return result;
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

			var clientValueMapper = Expression.Lambda<Func<Expression,IDataContext?,object?[]?,object?>>(
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

		List<Expression>? _parameterized;

		public List<Expression>?                   GetParameterized()  => _parameterized;

		public List<(Func<Expression, IDataContext?, object?[]?, object?> main, Func<Expression, IDataContext?, object?[]?, object?> substituted)>? GetParameterDuplicates()
			=> _parametersDuplicateCheck;

		public List<(Expression used, MappingSchema mappingSchema, Func<IDataContext, MappingSchema, Expression> accessorFunc)>? GetDynamicAccessors() => _dynamicAccessors?.Values.ToList();

		public bool CanBeConstant(Expression expr)
		{
			if (_parameterized != null && _parameterized.Contains(expr))
				return false;
			return true;
		}

		public void AddAsParameterized(Expression expression)
		{
			_parameterized ??= new ();
			if (!_parameterized.Contains(expression))
				_parameterized.Add(expression);
		}

		public bool GetAccessorExpression(Expression expression, [NotNullWhen(true)] out Expression? accessor, bool register)
		{
			if (_expressionAccessors.TryGetValue(expression, out accessor))
			{
				if (register)
					AddAsParameterized(expression);

				return true;
			}

			accessor = null;
			return false;
		}
	}
}
