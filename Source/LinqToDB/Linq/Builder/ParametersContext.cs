﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Data;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;

	sealed class ParametersContext
	{
		readonly object?[]? _parameterValues;

		public ParametersContext(Expression parametersExpression, object?[]? parameterValues, ExpressionTreeOptimizationContext optimizationContext, IDataContext dataContext)
		{
			_parameterValues     = parameterValues;
			ParametersExpression = parametersExpression;
			OptimizationContext  = optimizationContext;
			DataContext          = dataContext;

			_expressionAccessors = parametersExpression.GetExpressionAccessors(ExpressionBuilder.ExpressionParam);
		}

		public Expression                        ParametersExpression { get; }
		public ExpressionTreeOptimizationContext OptimizationContext  { get; }
		public IDataContext                      DataContext          { get; }
		public MappingSchema                     MappingSchema        => DataContext.MappingSchema;

		static ParameterExpression[] AccessorParameters =
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
			var idx = CurrentSqlParameters.Count;
			CurrentSqlParameters.Add(parameterAccessor);
			parameterAccessor.SqlParameter.AccessorId = idx;
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

			// do replacing again for registering parametrized constants
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

							if (paramExpr.EqualsTo(expr, OptimizationContext.GetSimpleEqualsToContext(true, null)))
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
						RegisterDuplicateParameter(expr, found.ValueAccessor, newAccessor.ValueAccessor);
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
			var originalAccessor = newExpr.ValueExpression;
			if (buildParameterType != BuildParameterType.InPredicate)
			{
				if (!newExpr.IsDataParameter)
				{
					if (columnDescriptor != null && originalAccessor is not BinaryExpression)
					{
						newExpr.DataType = columnDescriptor.GetDbDataType(true)
							.WithSystemType(newExpr.ValueExpression.Type);

						if (newExpr.ValueExpression.Type != columnDescriptor.MemberType)
						{
							var memberType = columnDescriptor.MemberType;
							var noConvert  = newExpr.ValueExpression.UnwrapConvert();
							if (noConvert.Type != typeof(object))
								newExpr.ValueExpression = noConvert;
							else if (newExpr.ValueExpression.Type != valueExpression.Type)
								newExpr.ValueExpression = Expression.Convert(noConvert, valueExpression.Type);
							else if (newExpr.ValueExpression.Type == typeof(object))
								newExpr.ValueExpression = Expression.Convert(noConvert, memberType);

							if (newExpr.ValueExpression.Type != memberType)
							{
								if (memberType.IsValueType ||
								    !memberType.IsSameOrParentOf(newExpr.ValueExpression.Type))
								{
									var convertLambda = MappingSchema.GetConvertExpression(newExpr.ValueExpression.Type, memberType, checkNull: true, createDefault: false);
									if (convertLambda != null)
									{
										newExpr.ValueExpression = InternalExtensions.ApplyLambdaToExpression(convertLambda, newExpr.ValueExpression);
									}
								}

								if (newExpr.ValueExpression.Type.IsNullable() && newExpr.ValueExpression.Type.ToNullableUnderlying() != memberType)
								{
									var convertLambda = MappingSchema.GenerateSafeConvert(newExpr.ValueExpression.Type, memberType);
									newExpr.ValueExpression = InternalExtensions.ApplyLambdaToExpression(convertLambda, newExpr.ValueExpression);
								}

								if (newExpr.ValueExpression.Type != memberType)
								{
									newExpr.ValueExpression = Expression.Convert(newExpr.ValueExpression, memberType);
								}
							}
						}

						newExpr.ValueExpression = columnDescriptor.ApplyConversions(newExpr.ValueExpression, newExpr.DataType, true);

						newExpr.DbDataTypeExpression = Expression.Constant(newExpr.DataType);

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
							if (newExpr.ValueExpression.Type != typeof(DataParameter))
							{
								LambdaExpression? convertExpr = null;
								if (buildParameterType == BuildParameterType.Default
									&& !HasDbMapping(MappingSchema, newExpr.ValueExpression.Type, out convertExpr))
								{
									if (!doNotCheckCompatibility)
										return null;
								}

								newExpr.ValueExpression = convertExpr != null
									? InternalExtensions.ApplyLambdaToExpression(convertExpr, newExpr.ValueExpression)
									: ColumnDescriptor.ApplyConversions(MappingSchema, newExpr.ValueExpression, newExpr.DataType, null, true);
							}
							else
							{
								newExpr.ValueExpression = ColumnDescriptor.ApplyConversions(MappingSchema, newExpr.ValueExpression, newExpr.DataType, null, true);
							}
						}
					}
				}

				if (typeof(DataParameter).IsSameOrParentOf(newExpr.ValueExpression.Type))
				{
					newExpr.DbDataTypeExpression = Expression.Property(newExpr.ValueExpression, Methods.LinqToDB.DataParameter.DbDataType);

					if (columnDescriptor != null)
					{
						var dbDataType = columnDescriptor.GetDbDataType(false);
						newExpr.DbDataTypeExpression = Expression.Call(Expression.Constant(dbDataType),
							DbDataType.WithSetValuesMethodInfo, newExpr.DbDataTypeExpression);
					}

					newExpr.ValueExpression = Expression.Property(newExpr.ValueExpression, Methods.LinqToDB.DataParameter.Value);
				}
			}

			name ??= columnDescriptor?.MemberName;

			var p = CreateParameterAccessor(
				DataContext, newExpr.ValueExpression, originalAccessor, newExpr.DbDataTypeExpression, valueExpression, ParametersExpression, name);

			return p;
		}

		public sealed class ValueTypeExpression
		{
			public Expression ValueExpression      = null!;
			public Expression DbDataTypeExpression = null!;

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

		public ValueTypeExpression ReplaceParameter(MappingSchema mappingSchema, Expression expression, ColumnDescriptor? columnDescriptor, bool forceConstant, Action<string>? setName)
		{
			var result = new ValueTypeExpression
			{
				DataType             = columnDescriptor?.GetDbDataType(true) ?? new DbDataType(expression.Type),
				DbDataTypeExpression = Expression.Constant(mappingSchema.GetDbDataType(expression.UnwrapConvertToObject().Type), typeof(DbDataType)),
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
								var dataParamExpr = Expression.Convert(expr, typeof(DataParameter));
								context.result.DbDataTypeExpression = Expression.Property(dataParamExpr, nameof(DataParameter.DbDataType));

								expr = Expression.Property(dataParamExpr, nameof(DataParameter.Value));
							}
							else if (context.Member != null)
							{
								if (context.columnDescriptor == null)
								{
									var mt = ExpressionBuilder.GetMemberDataType(context.paramContext.MappingSchema, context.Member);

									if (mt.DataType != DataType.Undefined)
									{
										context.result.DataType             = context.result.DataType.WithDataType(mt.DataType);
										context.result.DbDataTypeExpression = Expression.Constant(mt);
									}

									if (mt.DbType != null)
									{
										context.result.DataType             = context.result.DataType.WithDbType(mt.DbType);
										context.result.DbDataTypeExpression = Expression.Constant(mt);
									}

									if (mt.Length != null)
									{
										context.result.DataType             = context.result.DataType.WithLength(mt.Length);
										context.result.DbDataTypeExpression = Expression.Constant(mt);
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
			IDataContext        dataContext,
			Expression          accessorExpression,
			Expression          originalAccessorExpression,
			Expression          dbDataTypeAccessorExpression,
			Expression          expression,
			Expression?         parametersExpression,
			string?             name)
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
			accessorExpression = CorrectAccessorExpression(accessorExpression, dataContext);
			originalAccessorExpression   = CorrectAccessorExpression(originalAccessorExpression, dataContext);
			dbDataTypeAccessorExpression = CorrectAccessorExpression(dbDataTypeAccessorExpression, dataContext);

			var mapper = Expression.Lambda<Func<Expression,IDataContext?,object?[]?,object?>>(
				Expression.Convert(accessorExpression, typeof(object)),
				AccessorParameters);

			var original = Expression.Lambda<Func<Expression,IDataContext?,object?[]?,object?>>(
				Expression.Convert(originalAccessorExpression, typeof(object)),
				AccessorParameters);

			var dbDataTypeAccessor = Expression.Lambda<Func<Expression,IDataContext?,object?[]?,DbDataType>>(
				Expression.Convert(dbDataTypeAccessorExpression, typeof(DbDataType)),
				AccessorParameters);

			var dataTypeAccessor = dbDataTypeAccessor.CompileExpression();

			var parameterType = parametersExpression == null
				? new DbDataType(accessorExpression.Type)
				: dataTypeAccessor(parametersExpression, dataContext, null);

			return new ParameterAccessor(
					mapper.CompileExpression(),
					original.CompileExpression(),
					dataTypeAccessor,
					new SqlParameter(parameterType, name, null)
					{
						IsQueryParameter = !dataContext.InlineParameters
					}
				)
#if DEBUG
				{
					AccessorExpr = mapper
				}
#endif
				;
		}

		static Expression CorrectAccessorExpression(Expression accessorExpression, IDataContext dataContext)
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
					case ExpressionType.Convert:
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

		List<Expression>? _parametrized;

		public List<Expression>?                   GetParametrized()  => _parametrized;

		public List<(Func<Expression, IDataContext?, object?[]?, object?> main, Func<Expression, IDataContext?, object?[]?, object?> substituted)>? GetParameterDuplicates() 
			=> _parametersDuplicateCheck;

		public List<(Expression used, MappingSchema mappingSchema, Func<IDataContext, MappingSchema, Expression> accessorFunc)>? GetDynamicAccessors() => _dynamicAccessors?.Values.ToList();

		public bool CanBeConstant(Expression expr)
		{
			if (_parametrized != null && _parametrized.Contains(expr))
				return false;
			return true;
		}

		public void AddAsParametrized(Expression expression)
		{
			_parametrized ??= new ();
			if (!_parametrized.Contains(expression))
				_parametrized.Add(expression);
		}

		public bool GetAccessorExpression(Expression expression, [NotNullWhen(true)] out Expression? accessor, bool register)
		{
			if (_expressionAccessors.TryGetValue(expression, out accessor))
			{
				if (register)
					AddAsParametrized(expression);

				return true;
			}

			accessor = null;
			return false;
		}
	}
}
