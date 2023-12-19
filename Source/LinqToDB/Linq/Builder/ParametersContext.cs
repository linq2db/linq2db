using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

		internal void RegisterDuplicateParameter(Expression expression, Func<Expression, IDataContext?, object?[]?, object?> mainAccessor, Func<Expression, IDataContext?, object?[]?, object?> substitutedAccessor)
		{
			_parametersDuplicateCheck ??= new();

			_parametersDuplicateCheck.Add((mainAccessor, substitutedAccessor));
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
			InPredicate
		}

		public ParameterAccessor? BuildParameter(
			Expression         expr,
			ColumnDescriptor?  columnDescriptor,
			bool               forceConstant           = false,
			bool               doNotCheckCompatibility = false,
			bool               forceNew                = false,
			string?            alias                   = null,
			BuildParameterType buildParameterType      = BuildParameterType.Default)
		{
			string? name = alias;

			var newExpr = ReplaceParameter(expr, columnDescriptor, forceConstant, nm => name = nm);

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
								found = accessor;
								break;
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

		public ParameterAccessor BuildParameterFromArgument(MethodCallExpression methodCall, int argumentIndex, ColumnDescriptor? columnDescriptor,
			BuildParameterType buildParameterType = BuildParameterType.Default)
		{
			var valueAccessor = GenerateArgumentAccessor(methodCall, argumentIndex, columnDescriptor);

			var dataType = new DbDataType(valueAccessor.Type);
			var newExpr  = new ValueTypeExpression
			{
				DataType             = dataType,
				DbDataTypeExpression = Expression.Constant(dataType),
				ValueExpression      = valueAccessor
			};

			var p = PrepareConvertersAndCreateParameter(newExpr, valueAccessor, null, columnDescriptor, false,
				buildParameterType);
#pragma warning disable CS8604 // TODO:WAITFIX
			AddCurrentSqlParameter(p);
#pragma warning restore CS8604

			return p;
		}

		Expression? GetActualMethodAccessor(MethodCallExpression methodCall)
		{
			Expression? current = methodCall;
			if (_expressionAccessors.TryGetValue(current, out var methodAccessor))
				return methodAccessor;

			// Looking in known accessors for method with only changed first parameter.
			// Typical case when we have transformed Queryable method chain.
			foreach (var accessorPair in _expressionAccessors)
			{
				if (accessorPair.Key.NodeType != ExpressionType.Call)
					continue;

				var mc = (MethodCallExpression)accessorPair.Key;
				if (mc.Method != methodCall.Method)
					continue;

				var isEqual = true;
				for (int i = 1; i < mc.Arguments.Count; i++)
				{
					isEqual = (mc.Arguments[i].Equals(methodCall.Arguments[i]));
					if (!isEqual)
						break;
				}
				if (isEqual)
				{
					return accessorPair.Value;
				}
			}

			return null;
		}

		Expression GenerateArgumentAccessor(MethodCallExpression methodCall, int argumentIndex, ColumnDescriptor? columnDescriptor)
		{
			var arg = methodCall.Arguments[argumentIndex];
			var methodAccessor = GetActualMethodAccessor(methodCall);
			if (methodAccessor == null)
			{
				// compiled query case
				//
				if (null == arg.Find(ExpressionBuilder.ParametersParam))
					throw new InvalidOperationException($"Method '{methodCall}' does not have accessor.");
				return arg;
			}

			var prop = Expression.Property(methodAccessor, ReflectionHelper.MethodCall.Arguments);
			var valueAccessorExpr = Expression.Call(prop, ReflectionHelper.IndexExpressor<Expression>.Item,
				ExpressionInstances.Int32Array(argumentIndex));

			var expectedType = columnDescriptor?.MemberType ?? arg.Type;

			var evaluatedExpr = Expression.Call(null,
				Methods.LinqToDB.EvaluateExpression,
				valueAccessorExpr);

			var valueAccessor = (Expression)evaluatedExpr;
			valueAccessor = Expression.Convert(valueAccessor, expectedType);

			return valueAccessor;
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
						// Try GetConvertExpression<.., DataParameter>() first.
						//
						if (newExpr.ValueExpression.Type != typeof(DataParameter))
						{
							LambdaExpression? convertExpr = null;
							if (buildParameterType == BuildParameterType.Default && !HasDbMapping(MappingSchema, newExpr.ValueExpression.Type, out convertExpr))
							{
								if (!doNotCheckCompatibility)
									return null;
							}

							newExpr.ValueExpression = convertExpr != null ?
								InternalExtensions.ApplyLambdaToExpression(convertExpr, newExpr.ValueExpression) :
								ColumnDescriptor.ApplyConversions(MappingSchema, newExpr.ValueExpression, newExpr.DataType, null, true);
						}
						else
						{
							newExpr.ValueExpression = ColumnDescriptor.ApplyConversions(MappingSchema, newExpr.ValueExpression, newExpr.DataType, null, true);
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
					// !!! Code should be synched with ReplaceParameter !!!
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
					// !!! Code should be synched with ReplaceParameter !!!
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

		public ValueTypeExpression ReplaceParameter(Expression expression, ColumnDescriptor? columnDescriptor, bool forceConstant, Action<string>? setName)
		{
			var result = new ValueTypeExpression
			{
				DataType             = columnDescriptor?.GetDbDataType(true) ?? new DbDataType(expression.Type),
				DbDataTypeExpression = Expression.Constant(new DbDataType(expression.Type), typeof(DbDataType)),
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

			// see #820
			accessorExpression         = CorrectAccessorExpression(accessorExpression, dataContext);
			originalAccessorExpression = CorrectAccessorExpression(originalAccessorExpression, dataContext);

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

			return new ParameterAccessor
				(
					expression,
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

		internal ISqlExpression GetParameter(Expression ex, MemberInfo member, ColumnDescriptor? columnDescriptor)
		{
			if (member is MethodInfo mi)
				member = mi.GetPropertyInfo()!; // ??

			var vte  = ReplaceParameter(ex, columnDescriptor, forceConstant: false, null);
			var par  = vte.ValueExpression;
			var expr = Expression.MakeMemberAccess(par.Type == typeof(object) ? Expression.Convert(par, member.DeclaringType ?? typeof(object)) : par, member);

			vte.ValueExpression = expr;

			if (columnDescriptor != null)
			{
				var dbDataType = columnDescriptor.GetDbDataType(true);
				vte.DataType             = dbDataType;
				vte.DbDataTypeExpression = Expression.Constant(dbDataType);
			}

			if (!expr.Type.IsSameOrParentOf(vte.DataType.SystemType))
			{
				var dbDataType = new DbDataType(expr.Type);
				vte.DataType             = dbDataType;
				vte.DbDataTypeExpression = Expression.Constant(dbDataType);
			}

			var p = PrepareConvertersAndCreateParameter(vte, expr, member?.Name, columnDescriptor, false, BuildParameterType.Default);

#pragma warning disable CS8620 // TODO:WAITFIX
			(_parameters ??= new()).Add((expr, columnDescriptor, p));
#pragma warning restore CS8620
#pragma warning disable CS8604 // TODO:WAITFIX
			AddCurrentSqlParameter(p);
#pragma warning restore CS8604

			return p.SqlParameter;
		}

		List<Expression>? _parametrized;

		public List<Expression>?                   GetParametrized()  => _parametrized;

		public List<(Func<Expression, IDataContext?, object?[]?, object?> main, Func<Expression, IDataContext?, object?[]?, object?> substituted)>? GetParameterDuplicates() 
			=> _parametersDuplicateCheck;

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
