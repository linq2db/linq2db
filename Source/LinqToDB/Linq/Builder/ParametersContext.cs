﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	class ParametersContext
	{
		public ParametersContext(Expression expression, ExpressionTreeOptimizationContext optimizationContext, IDataContext dataContext)
		{
			OptimizationContext = optimizationContext;
			DataContext         = dataContext;

			_expressionAccessors = expression.GetExpressionAccessors(ExpressionBuilder.ExpressionParam);
		}

		public ExpressionTreeOptimizationContext OptimizationContext { get; }
		public IDataContext                      DataContext         { get; }
		public MappingSchema                     MappingSchema       => DataContext.MappingSchema;

		private static ParameterExpression[] AccessorParameters = { ExpressionBuilder.ExpressionParam, ExpressionBuilder.DataContextParam, ExpressionBuilder.ParametersParam };

		public readonly   List<ParameterAccessor>           CurrentSqlParameters = new ();
		internal readonly Dictionary<Expression,Expression> _expressionAccessors;

		public ParameterAccessor? RegisterParameter(Expression expression)
		{
			if (typeof(IToSqlConverter).IsSameOrParentOf(expression.Type))
			{
				//TODO: Check this
				var sql = ExpressionBuilder.ConvertToSqlConvertible(expression);
				if (sql != null)
					return null;
			}

			if (!OptimizationContext.PreferServerSide(expression, false))
			{
				if (OptimizationContext.CanBeConstant(expression))
					return null;

				if (OptimizationContext.CanBeCompiled(expression))
				{
					return BuildParameter(expression, null);
				}
			}

			return null;
		}

		#region Build Parameter

		internal readonly Dictionary<Expression,ParameterAccessor> _parameters = new ();

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

		public ParameterAccessor BuildParameter(Expression expr, ColumnDescriptor? columnDescriptor, bool forceConstant = false,
			BuildParameterType buildParameterType = BuildParameterType.Default)
		{
			if (_parameters.TryGetValue(expr, out var p))
				return p;

			string? name = null;

			ValueTypeExpression newExpr;
			newExpr = ReplaceParameter(_expressionAccessors, expr, forceConstant, nm => name = nm);

			p = PrepareConvertersAndCreateParameter(newExpr, expr, name, columnDescriptor, buildParameterType);

			var found = p;
			foreach (var accessor in _parameters)
			{
				if (accessor.Value.SqlParameter.Type.Equals(p.SqlParameter.Type))
					continue;

				if (accessor.Key.EqualsTo(expr, OptimizationContext.GetSimpleEqualsToContext(true)))
				{
					found = accessor.Value;
					break;
				}
			}

			// We already have registered parameter for the same expression
			//
			if (!ReferenceEquals(found, p))
				return found;

			_parameters.Add(expr, p);
			AddCurrentSqlParameter(p);

			return p;
		}

		public ParameterAccessor BuildParameterFromArgumentProperty(MethodCallExpression methodCall, int argumentIndex, ColumnDescriptor columnDescriptor,
			BuildParameterType buildParameterType = BuildParameterType.Default)
		{
			var valueAccessor = GenerateArgumentAccessor(methodCall, argumentIndex, null);

			valueAccessor = Expression.MakeMemberAccess(valueAccessor, columnDescriptor.MemberInfo);

			var dataType = columnDescriptor.GetDbDataType(true);
			var newExpr  = new ValueTypeExpression
			{
				DataType             = dataType,
				DbDataTypeExpression = Expression.Constant(dataType),
				ValueExpression      = valueAccessor
			};

			var p = PrepareConvertersAndCreateParameter(newExpr, valueAccessor, null, columnDescriptor,
				buildParameterType);
			AddCurrentSqlParameter(p);

			return p;
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

			var p = PrepareConvertersAndCreateParameter(newExpr, valueAccessor, null, columnDescriptor,
				buildParameterType);
			AddCurrentSqlParameter(p);

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
				Expression.Constant(argumentIndex));

			var expectedType = columnDescriptor?.MemberType ?? arg.Type;

			var evaluatedExpr = Expression.Call(null,
				MemberHelper.MethodOf(() => InternalExtensions.EvaluateExpression(null)),
				valueAccessorExpr);

			var valueAccessor = (Expression)evaluatedExpr;
			valueAccessor = Expression.Convert(valueAccessor, expectedType);

			return valueAccessor;
		}

		ParameterAccessor PrepareConvertersAndCreateParameter(ValueTypeExpression newExpr, Expression valueExpression, string? name, ColumnDescriptor? columnDescriptor, BuildParameterType buildParameterType)
		{
			var originalAccessor = newExpr.ValueExpression;
			if (buildParameterType != BuildParameterType.InPredicate)
			{
				if (!typeof(DataParameter).IsSameOrParentOf(newExpr.ValueExpression.Type))
				{
					if (columnDescriptor != null && !(originalAccessor is BinaryExpression))
					{
						newExpr.DataType = columnDescriptor.GetDbDataType(true);
						if (newExpr.ValueExpression.Type != columnDescriptor.MemberType)
						{
							newExpr.ValueExpression = newExpr.ValueExpression.UnwrapConvert()!;
							var memberType = columnDescriptor.MemberType;
							if (newExpr.ValueExpression.Type != memberType)
							{
								if (!newExpr.ValueExpression.Type.IsNullable() || newExpr.ValueExpression.Type.ToNullableUnderlying() != memberType)
								{
									var convertLambda = MappingSchema.GenerateSafeConvert(newExpr.ValueExpression.Type,
										memberType);
									newExpr.ValueExpression =
										InternalExtensions.ApplyLambdaToExpression(convertLambda,
											newExpr.ValueExpression);
								}
							}
						}

						newExpr.ValueExpression = columnDescriptor.ApplyConversions(newExpr.ValueExpression, newExpr.DataType, true);

						if (name == null)
						{
							if (columnDescriptor.MemberName.Contains('.'))
								name = columnDescriptor.ColumnName;
							else
								name = columnDescriptor.MemberName;

						}

						newExpr.DbDataTypeExpression = Expression.Constant(newExpr.DataType);
					}
					else
					{
						newExpr.ValueExpression = ColumnDescriptor.ApplyConversions(MappingSchema, newExpr.ValueExpression, newExpr.DataType, null, true);
					}
				}

				if (typeof(DataParameter).IsSameOrParentOf(newExpr.ValueExpression.Type))
				{
					newExpr.DbDataTypeExpression = Expression.PropertyOrField(newExpr.ValueExpression, nameof(DataParameter.DbDataType));

					if (columnDescriptor != null)
					{
						var dbDataType = columnDescriptor.GetDbDataType(false);
						newExpr.DbDataTypeExpression = Expression.Call(Expression.Constant(dbDataType),
							DbDataType.WithSetValuesMethodInfo, newExpr.DbDataTypeExpression);
					}

					newExpr.ValueExpression = Expression.PropertyOrField(newExpr.ValueExpression, nameof(DataParameter.Value));
				}
			}

			name ??= columnDescriptor?.MemberName;

			var p = CreateParameterAccessor(
				DataContext, newExpr.ValueExpression, originalAccessor, newExpr.DbDataTypeExpression, valueExpression, name);

			return p;
		}

		public class ValueTypeExpression
		{
			public Expression ValueExpression      = null!;
			public Expression DbDataTypeExpression = null!;

			public DbDataType DataType;
		}

		public ValueTypeExpression ReplaceParameter(IDictionary<Expression, Expression> expressionAccessors, Expression expression, bool forceConstant, Action<string>? setName)
		{
			var result = new ValueTypeExpression
			{
				DataType             = new DbDataType(expression.Type),
				DbDataTypeExpression = Expression.Constant(new DbDataType(expression.Type), typeof(DbDataType)),
			};

			var unwrapped = expression.Unwrap();
			if (unwrapped.NodeType == ExpressionType.MemberAccess)
			{
				var ma = (MemberExpression)unwrapped;
				setName?.Invoke(ma.Member.Name);
			}

			result.ValueExpression = expression.Transform(
				(forceConstant, expression, expressionAccessors, result, setName, paramContext: this),
				static (context, expr) =>
				{
					if (expr.NodeType == ExpressionType.Constant)
					{
						var c = (ConstantExpression)expr;

						if (context.forceConstant || !expr.Type.IsConstantable(false))
						{
							if (context.expressionAccessors.TryGetValue(expr, out var val))
							{
								expr = Expression.Convert(val, expr.Type);

								if (context.expression.NodeType == ExpressionType.MemberAccess)
								{
									var ma = (MemberExpression)context.expression;

									var mt = ExpressionBuilder.GetMemberDataType(context.paramContext.MappingSchema, ma.Member);;

									if (mt.DataType != DataType.Undefined)
									{
										context.result.DataType = context.result.DataType.WithDataType(mt.DataType);
										context.result.DbDataTypeExpression = Expression.Constant(mt);
									}

									if (mt.DbType != null)
									{
										context.result.DataType = context.result.DataType.WithDbType(mt.DbType);
										context.result.DbDataTypeExpression = Expression.Constant(mt);
									}

									if (mt.Length != null)
									{
										context.result.DataType = context.result.DataType.WithLength(mt.Length);
										context.result.DbDataTypeExpression = Expression.Constant(mt);
									}

									context.setName?.Invoke(ma.Member.Name);
								}
							}
						}
					}

					return expr;
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
			string?             name)
		{
			// Extracting name for parameter
			//
			if (name == null && expression.Type == typeof(DataParameter))
			{
				var dp = expression.EvaluateExpression<DataParameter>();
				if (dp?.Name?.IsNullOrEmpty() == false)
					name = dp.Name;
			}

			// see #820
			accessorExpression         = CorrectAccessorExpression(accessorExpression, dataContext, ExpressionBuilder.DataContextParam);
			originalAccessorExpression = CorrectAccessorExpression(originalAccessorExpression, dataContext, ExpressionBuilder.DataContextParam);

			var mapper = Expression.Lambda<Func<Expression,IDataContext?,object?[]?,object?>>(
				Expression.Convert(accessorExpression, typeof(object)),
				AccessorParameters);

			var original = Expression.Lambda<Func<Expression,IDataContext?,object?[]?,object?>>(
				Expression.Convert(originalAccessorExpression, typeof(object)),
				AccessorParameters);

			var dbDataTypeAccessor = Expression.Lambda<Func<Expression,IDataContext?,object?[]?,DbDataType>>(
				Expression.Convert(dbDataTypeAccessorExpression, typeof(DbDataType)),
				AccessorParameters);

			return new ParameterAccessor
				(
					expression,
					mapper.CompileExpression(),
					original.CompileExpression(),
					dbDataTypeAccessor.CompileExpression(),
					new SqlParameter(new DbDataType(accessorExpression.Type), name, null)
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

	
		static Expression CorrectAccessorExpression(Expression accessorExpression, IDataContext dataContext, ParameterExpression dataContextParam)
		{
			// see #820
			accessorExpression = accessorExpression.Transform((dataContext, dataContextParam), static (context, e) =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.Parameter:
					{
						// DataContext creates DataConnection which is not compatible with QueryRunner and parameter evaluation.
						// It can be fixed by adding additional parameter to execution path, but it's may slowdown performance.
						// So for now decided to throw exception.
						if (e == context.dataContextParam && !typeof(DataConnection).IsSameOrParentOf(context.dataContext.GetType()))
							throw new LinqException("Only DataConnection descendants can be used as source of parameters.");
						return e;
					}
					case ExpressionType.MemberAccess:
					{
						var ma = (MemberExpression) e;

						if (ma.Member.IsNullableValueMember())
						{
							return Expression.Condition(
								Expression.Equal(ma.Expression, Expression.Constant(null, ma.Expression.Type)),
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
					default:
						return e;
				}
			})!;

			return accessorExpression;
		}

		internal ISqlExpression GetParameter(Expression ex, MemberInfo? member, ColumnDescriptor? columnDescriptor)
		{
			if (member is MethodInfo mi)
				member = mi.GetPropertyInfo();

			var vte  = ReplaceParameter(_expressionAccessors, ex, forceConstant: false, null);
			var par  = vte.ValueExpression;
			var expr = Expression.MakeMemberAccess(par.Type == typeof(object) ? Expression.Convert(par, member?.DeclaringType ?? typeof(object)) : par, member);

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

			var p = PrepareConvertersAndCreateParameter(vte, expr, member?.Name, columnDescriptor, BuildParameterType.Default);

			_parameters.Add(expr, p);
			AddCurrentSqlParameter(p);

			return p.SqlParameter;
		}

	}
}
