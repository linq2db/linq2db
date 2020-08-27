﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using Common;
	using Reflection;
	using SqlQuery;

	partial class ExpressionBuilder
	{
		#region BuildExpression

		readonly HashSet<Expression>                    _skippedExpressions   = new HashSet<Expression>();
		readonly Dictionary<Expression,UnaryExpression> _convertedExpressions = new Dictionary<Expression,UnaryExpression>();

		public void UpdateConvertedExpression(Expression oldExpression, Expression newExpression)
		{
			if (_convertedExpressions.TryGetValue(oldExpression, out var conversion)
				&& !_convertedExpressions.ContainsKey(newExpression))
			{
				UnaryExpression newConversion;
				if (conversion.NodeType == ExpressionType.Convert)
				{
					newConversion = Expression.Convert(newExpression, conversion.Type);
				}
				else
				{
					newConversion = Expression.ConvertChecked(newExpression, conversion.Type);
				}

				_convertedExpressions.Add(newExpression, newConversion);
			}
		}

		public void RemoveConvertedExpression(Expression ex)
		{
			_convertedExpressions.Remove(ex);
		}

		public Expression BuildExpression(IBuildContext context, Expression expression, bool enforceServerSide, string? alias = null)
		{
			var newExpr = expression.Transform(expr => TransformExpression(context, expr, enforceServerSide, alias));
			return newExpr;
		}

		Expression ConvertAssignmentArgument(IBuildContext context, Expression expr, MemberInfo? memberInfo, bool enforceServerSide,
			string? alias)
		{
			var resultExpr = expr;
			resultExpr = CorrectConditional(context, resultExpr, enforceServerSide, alias);

			// Update nullability
			resultExpr = resultExpr.Transform(UpdateNullabilityFromExtension);

			if (resultExpr.NodeType == ExpressionType.Convert || resultExpr.NodeType == ExpressionType.ConvertChecked)
			{
				var conv = (UnaryExpression)resultExpr;
				if (memberInfo?.GetMemberType().IsNullable() == true
					&& conv.Operand is ConvertFromDataReaderExpression readerExpression
					&& !readerExpression.Type.IsNullable())
				{
					resultExpr = readerExpression.MakeNullable();
				}
			}
			else if (resultExpr.NodeType == ExpressionType.Extension &&
					 resultExpr is ConvertFromDataReaderExpression readerExpression)
			{
				if (memberInfo?.GetMemberType().IsNullable() == true &&
					!readerExpression.Type.IsNullable())
				{
					resultExpr = readerExpression.MakeNullable();
				}
				else if (memberInfo?.GetMemberType().IsNullable() == false &&
					readerExpression.Type.IsNullable())
				{
					resultExpr = readerExpression.MakeNotNullable();
				}
			}

			return resultExpr;
		}

		private Expression UpdateNullabilityFromExtension(Expression resultExpr)
		{
			if (resultExpr.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)resultExpr;
				var attr = MappingSchema.GetAttribute<Sql.ExpressionAttribute>(mc.Method.ReflectedType!, mc.Method);

				if (attr != null
					&& attr.IsNullable == Sql.IsNullableType.IfAnyParameterNullable
					&& mc.Arguments.Count == 1
					&& attr.Expression == "{0}"
					&& mc.Method.ReturnParameter?.ParameterType.IsNullable() == true
				)
				{
					var parameter = mc.Method.GetParameters()[0];
					if (mc.Method.ReturnParameter?.ParameterType != parameter.ParameterType
						&& parameter.ParameterType.IsValueType
						&& mc.Arguments[0] is ConvertFromDataReaderExpression readerExpression)
					{
						resultExpr = readerExpression.MakeNullable();
					}
				}
			}

			return resultExpr;
		}

		TransformInfo TransformExpression(IBuildContext context, Expression expr, bool enforceServerSide, string? alias)
		{
			if (_skippedExpressions.Contains(expr))
				return new TransformInfo(expr, true);

			alias ??= _optimizationContext.GetExpressionAlias(expr);

			switch (expr.NodeType)
			{
				case ExpressionType.Convert       :
				case ExpressionType.ConvertChecked:
					{
						if (expr.Type == typeof(object))
							break;

						var cex = (UnaryExpression)expr;

						_convertedExpressions.Add(cex.Operand, cex);

						var newOperand = BuildExpression(context, cex.Operand, enforceServerSide);

						if (newOperand.Type != cex.Type)
						{
							if (cex.Type.IsNullable() && newOperand is ConvertFromDataReaderExpression readerExpression)
							{
								newOperand = readerExpression.MakeNullable();
							}

							newOperand = cex.Update(newOperand);
						}
						var ret = new TransformInfo(newOperand, true);

						RemoveConvertedExpression(cex.Operand);

						return ret;
					}

				case ExpressionType.MemberAccess:
					{
						var ma = (MemberExpression)expr;

						if (IsServerSideOnly(ma) || PreferServerSide(ma, enforceServerSide) && !HasNoneSqlMember(ma))
						{
							return new TransformInfo(BuildSql(context, expr, alias));
						}

						var l  = Expressions.ConvertMember(MappingSchema, ma.Expression?.Type, ma.Member);
						if (l != null)
						{
							// In Grouping KeyContext we have to perform calculation on server side
							if (Contexts.Any(c => c is GroupByBuilder.KeyContext))
								return new TransformInfo(BuildSql(context, expr, alias));
							break;
						}

						if (ma.Member.IsNullableValueMember())
							break;

						var ctx = GetContext(context, ma);

						if (ctx != null)
						{
							var prevCount  = ctx.SelectQuery.Select.Columns.Count;
							var expression = ctx.BuildExpression(ma, 0, enforceServerSide);

							if (expression.NodeType == ExpressionType.Extension && expression is DefaultValueExpression 
							                                                    && ma.Expression?.NodeType == ExpressionType.Parameter)
							{
								var objExpression = BuildExpression(ctx, ma.Expression, enforceServerSide, alias);
								var varTempVar    = objExpression.NodeType == ExpressionType.Parameter
									? objExpression
									: BuildVariable(objExpression, ((ParameterExpression)ma.Expression).Name);

								var condition = Expression.Condition(
									Expression.Equal(varTempVar,
										new DefaultValueExpression(MappingSchema, ma.Expression.Type)), expression,
									Expression.MakeMemberAccess(varTempVar, ma.Member));
								expression = condition;
							}
							else if (!alias.IsNullOrEmpty() && (ctx.SelectQuery.Select.Columns.Count - prevCount) == 1)
							{
								ctx.SelectQuery.Select.Columns[ctx.SelectQuery.Select.Columns.Count - 1].Alias = alias;
							}
							return new TransformInfo(expression);
						}

						var ex = ma.Expression;

						while (ex is MemberExpression memberExpression)
							ex = memberExpression.Expression;

						if (ex is MethodCallExpression ce)
						{
							if (IsSubQuery(context, ce))
							{
								if (!IsMultipleQuery(ce, context.Builder.MappingSchema))
								{
									var info = GetSubQueryContext(context, ce);
									if (alias != null)
										info.Context.SetAlias(alias);
									var par  = Expression.Parameter(ex.Type);
									var bex  = info.Context.BuildExpression(ma.Transform(e => e == ex ? par : e), 0, enforceServerSide);

									if (bex != null)
										return new TransformInfo(bex);
								}
							}
						}

						ex = ma.Expression;

						if (ex != null && ex.NodeType == ExpressionType.Constant)
						{
							// field = localVariable
							//
							if (!_expressionAccessors.TryGetValue(ex, out var c))
								return new TransformInfo(ma);
							return new TransformInfo(Expression.MakeMemberAccess(Expression.Convert(c, ex.Type), ma.Member));
						}

						break;
					}

				case ExpressionType.Parameter:
					{
						if (expr == ParametersParam || expr == PreambleParam)
							break;

						var ctx = GetContext(context, expr);

						if (ctx != null)
						{
							var buildExpr = ctx.BuildExpression(expr, 0, enforceServerSide);
							if (buildExpr.Type != expr.Type)
							{
								buildExpr = Expression.Convert(buildExpr, expr.Type);
							}
							return new TransformInfo(buildExpr);
						}

						break;
					}

				case ExpressionType.Constant:
					{
						if (expr.Type.IsConstantable(true))
							break;

						if ((_buildMultipleQueryExpressions == null || !_buildMultipleQueryExpressions.Contains(expr)) && IsSequence(new BuildInfo(context, expr, new SelectQuery())))
						{
							return new TransformInfo(BuildMultipleQuery(context, expr, enforceServerSide));
						}

						if (_expressionAccessors.TryGetValue(expr, out var accessor))
							return new TransformInfo(Expression.Convert(accessor, expr.Type));

						break;
					}

				case ExpressionType.Coalesce:

					if (expr.Type == typeof(string) && MappingSchema.GetDefaultValue(typeof(string)) != null)
						return new TransformInfo(BuildSql(context, expr, alias));

					if (CanBeTranslatedToSql(context, ConvertExpression(expr), true))
						return new TransformInfo(BuildSql(context, expr, alias));

					break;

				case ExpressionType.Call:
					{
						var ce = (MethodCallExpression)expr;

						if (IsGroupJoinSource(context, ce))
						{
							foreach (var arg in ce.Arguments.Skip(1))
								if (!_skippedExpressions.Contains(arg))
									_skippedExpressions.Add(arg);

							if (IsSubQuery(context, ce))
							{
								if (ce.IsQueryable())
								//if (!typeof(IEnumerable).IsSameOrParentOf(expr.Type) || expr.Type == typeof(string) || expr.Type.IsArray)
								{
									var ctx = GetContext(context, expr);

									if (ctx != null)
										return new TransformInfo(ctx.BuildExpression(expr, 0, enforceServerSide));
								}
							}

							break;
						}

						if (ce.IsAssociation(MappingSchema))
						{
							var ctx = GetContext(context, ce);
							if (ctx == null)
								throw new InvalidOperationException();

							return new TransformInfo(ctx.BuildExpression(ce, 0, enforceServerSide));
						}

						if ((_buildMultipleQueryExpressions == null || !_buildMultipleQueryExpressions.Contains(ce)) && IsSubQuery(context, ce))
						{
							if (IsMultipleQuery(ce, MappingSchema))
								return new TransformInfo(BuildMultipleQuery(context, ce, enforceServerSide));

							return new TransformInfo(GetSubQueryExpression(context, ce, enforceServerSide, alias));
						}

						if (IsServerSideOnly(expr) || PreferServerSide(expr, enforceServerSide) || ce.Method.IsSqlPropertyMethodEx())
							return new TransformInfo(BuildSql(context, expr, alias));
					}

					break;

				case ExpressionType.New:
					{
						var ne = (NewExpression)expr;

						List<Expression>? arguments = null;
						for (var i = 0; i < ne.Arguments.Count; i++)
						{
							var argument    = ne.Arguments[i];
							var memberAlias = ne.Members?[i].Name;

							var newArgument = ConvertAssignmentArgument(context, argument, ne.Members?[i], enforceServerSide, memberAlias);
							if (newArgument != argument)
							{
								if (arguments == null)
									arguments = ne.Arguments.Take(i).ToList();
							}
							arguments?.Add(newArgument);
						}

						if (arguments != null)
						{
							ne = ne.Update(arguments);
						}

						return new TransformInfo(ne, true);
					}

				case ExpressionType.MemberInit:
					{
						var mi      = (MemberInitExpression)expr;
						var newPart = (NewExpression)BuildExpression(context, mi.NewExpression, enforceServerSide);
						List<MemberBinding>? bindings = null;
						for (var i = 0; i < mi.Bindings.Count; i++)
						{
							var binding    = mi.Bindings[i];
							var newBinding = binding;
							if (binding is MemberAssignment assignment)
							{
								var argument = ConvertAssignmentArgument(context, assignment.Expression,
									assignment.Member, enforceServerSide, assignment.Member.Name);
								if (argument != assignment.Expression)
								{
									newBinding = Expression.Bind(assignment.Member, argument);
								}
							}

							if (newBinding != binding)
							{
								if (bindings == null)
									bindings = mi.Bindings.Take(i).ToList();
							}

							bindings?.Add(newBinding);
						}

						if (mi.NewExpression != newPart || bindings != null)
						{
							mi = mi.Update(newPart, bindings ?? mi.Bindings.AsEnumerable());
						}

						return new TransformInfo(mi, true);
					}

			}

			if (enforceServerSide || EnforceServerSide(context))
			{
				switch (expr.NodeType)
				{
					case ExpressionType.MemberInit :
					case ExpressionType.Convert    :
						break;

					default                        :
						if (!enforceServerSide && CanBeCompiled(expr))
							break;
						return new TransformInfo(BuildSql(context, expr, alias));
				}
			}

			return new TransformInfo(expr);
		}

		Expression CorrectConditional(IBuildContext context, Expression expr, bool enforceServerSide, string? alias)
		{
			if (expr.NodeType != ExpressionType.Conditional)
				return BuildExpression(context, expr, enforceServerSide, alias);

			var cond = (ConditionalExpression)expr;

					if (cond.Test.NodeType == ExpressionType.Equal || cond.Test.NodeType == ExpressionType.NotEqual)
					{
						var b = (BinaryExpression)cond.Test;

				Expression? cnt = null;
				Expression? obj = null;

				if (IsNullConstant(b.Left))
				{
					cnt = b.Left;
					obj = b.Right;
				}
				else if (IsNullConstant(b.Right))
				{
					cnt = b.Right;
					obj = b.Left;
				}

				if (cnt != null)
				{
					var objContext = GetContext(context, obj);
					if (objContext != null && objContext.IsExpression(obj, 0, RequestFor.Object).Result)
					{
						var sql = objContext.ConvertToSql(obj, 0, ConvertFlags.Key);
						if (sql.Length == 0)
							sql = objContext.ConvertToSql(obj, 0, ConvertFlags.All);

						if (sql.Length > 0)
						{
							Expression? predicate = null;
							foreach (var f in sql)
							{
								if (f.Sql is SqlField field && field.Table!.All == field)
											continue;

								var valueType = f.Sql.SystemType!;

								if (!valueType.IsNullable() && valueType.IsValueType)
									valueType = typeof(Nullable<>).MakeGenericType(valueType);

								var reader     = BuildSql(context, f.Sql, valueType, null);
								var comparison = Expression.MakeBinary(cond.Test.NodeType,
									Expression.Default(valueType), reader);

								predicate = predicate == null
									? comparison
									: Expression.MakeBinary(
										cond.Test.NodeType == ExpressionType.Equal
											? ExpressionType.AndAlso
											: ExpressionType.OrElse, predicate, comparison);
							}

							if (predicate != null)
								cond = cond.Update(predicate,
									CorrectConditional(context, cond.IfTrue,  enforceServerSide, alias),
									CorrectConditional(context, cond.IfFalse, enforceServerSide, alias));
						}
					}
				}
			}

			if (cond == expr)
				expr = BuildExpression(context, expr, enforceServerSide, alias);
			else
				expr = cond;

			return expr;
		}

		static bool IsMultipleQuery(MethodCallExpression ce, MappingSchema mappingSchema)
		{
			//TODO: Multiply query check should be smarter, possibly not needed if we create fallback mechanism
			return !ce.IsQueryable(FirstSingleBuilder.MethodNames) 
			       && typeof(IEnumerable).IsSameOrParentOf(ce.Type) 
			       && ce.Type != typeof(string) 
			       && !ce.Type.IsArray 
			       && !ce.IsAggregate(mappingSchema);
		}

		class SubQueryContextInfo
		{
			public MethodCallExpression Method  = null!;
			public IBuildContext        Context = null!;
			public Expression?          Expression;
		}

		readonly Dictionary<IBuildContext,List<SubQueryContextInfo>> _buildContextCache = new Dictionary<IBuildContext,List<SubQueryContextInfo>>();

		SubQueryContextInfo GetSubQueryContext(IBuildContext context, MethodCallExpression expr)
		{
			if (!_buildContextCache.TryGetValue(context, out var sbi))
				_buildContextCache[context] = sbi = new List<SubQueryContextInfo>();

			foreach (var item in sbi)
			{
				if (expr.EqualsTo(item.Method, DataContext, new Dictionary<Expression,QueryableAccessor>(), null, null))
					return item;
			}

			var ctx = GetSubQuery(context, expr);

			var info = new SubQueryContextInfo { Method = expr, Context = ctx };

			sbi.Add(info);

			return info;
		}

		public Expression GetSubQueryExpression(IBuildContext context, MethodCallExpression expr, bool enforceServerSide, string? alias)
		{
			var info = GetSubQueryContext(context, expr);
			if (info.Expression == null)
				info.Expression = info.Context.BuildExpression(null, 0, enforceServerSide);

			if (!alias.IsNullOrEmpty())
				info.Context.SetAlias(alias);
			return info.Expression;
		}

		static bool EnforceServerSide(IBuildContext context)
		{
			return context.SelectQuery.Select.IsDistinct;
		}

		#endregion

		#region BuildSql

		Expression BuildSql(IBuildContext context, Expression expression, string? alias)
		{
			//TODO: Check that we can pass column descriptor here
			var sqlex = ConvertToSqlExpression(context, expression, null, false);
			var idx   = context.SelectQuery.Select.Add(sqlex);

			if (alias != null)
				context.SelectQuery.Select.Columns[idx].RawAlias = alias;

			idx = context.ConvertToParentIndex(idx, context);

			var field = BuildSql(expression, idx, sqlex);

			return field;
		}

		Expression BuildSql(IBuildContext context, ISqlExpression sqlExpression, Type overrideType, string? alias)
		{
			var idx   = context.SelectQuery.Select.Add(sqlExpression);

			if (alias != null)
				context.SelectQuery.Select.Columns[idx].RawAlias = alias;

			idx = context.ConvertToParentIndex(idx, context);

			var field = BuildSql(overrideType ?? sqlExpression.SystemType!, idx, sqlExpression);

			return field;
		}

		public Expression BuildSql(Expression expression, int idx, ISqlExpression sqlExpression)
		{
			var type = expression.Type;

			if (_convertedExpressions.TryGetValue(expression, out var cex))
			{
				if (cex.Type.IsNullable() && !type.IsNullable() && type.IsSameOrParentOf(cex.Type.ToNullableUnderlying()))
					type = cex.Type;
			}

			return BuildSql(type, idx, sqlExpression);
		}

		public Expression BuildSql(Type type, int idx, IValueConverter? converter)
		{
			return new ConvertFromDataReaderExpression(type, idx, converter, DataReaderLocal);
		}

		public Expression BuildSql(Type type, int idx, ISqlExpression? sourceExpression)
		{
			return BuildSql(type, idx, QueryHelper.GetValueConverter(sourceExpression));
		}
		
		#endregion

		#region IsNonSqlMember

		bool HasNoneSqlMember(Expression expr)
		{
			var inCte = false;

			var found = expr.Find(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.MemberAccess:
						{
							var me = (MemberExpression)e;

							var om = (
								from c in Contexts.OfType<TableBuilder.TableContext>()
								where c.ObjectType == me.Member.DeclaringType
								select c.EntityDescriptor
							).FirstOrDefault();

							return om != null && om.Associations.All(a => !a.MemberInfo.EqualsTo(me.Member)) &&
							       om[me.Member.Name] == null;
						}
					case ExpressionType.Call:
						{
							var mc = (MethodCallExpression)e;
							if (mc.IsCte(MappingSchema))
								inCte = true;
							break;
						}
				}

				return inCte;
			});

			return found != null && !inCte;
		}

		#endregion

		#region PreferServerSide

		bool PreferServerSide(Expression expr, bool enforceServerSide)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var pi = (MemberExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, pi.Expression?.Type, pi.Member);

						if (l != null)
						{
							var info = l.Body.Unwrap();

							if (l.Parameters.Count == 1 && pi.Expression != null)
								info = info.Transform(wpi => wpi == l.Parameters[0] ? pi.Expression : wpi);

							return info.Find(e => PreferServerSide(e, enforceServerSide)) != null;
						}

						var attr = GetExpressionAttribute(pi.Member);
						return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeCompiled(expr);
					}

				case ExpressionType.Call:
					{
						var pi = (MethodCallExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

						if (l != null)
							return l.Body.Unwrap().Find(e => PreferServerSide(e, enforceServerSide)) != null;

						var attr = GetExpressionAttribute(pi.Method);
						return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeCompiled(expr);
					}
				default:
					{
						if (expr is BinaryExpression binary)
						{
							var l = Expressions.ConvertBinary(MappingSchema, binary);
							if (l != null)
							{
								var body = l.Body.Unwrap();
								var newExpr = body.Transform(wpi =>
								{
									if (wpi.NodeType == ExpressionType.Parameter)
									{
										if (l.Parameters[0] == wpi)
											return binary.Left;
										if (l.Parameters[1] == wpi)
											return binary.Right;
									}

									return wpi;
								});

								return PreferServerSide(newExpr, enforceServerSide);
							}
						}
						break;
					}
			}

			return false;
		}

		#endregion

		#region Build Mapper

		public Expression BuildBlock(Expression expression)
		{
			if (IsBlockDisable || BlockExpressions.Count == 0)
				return expression;

			BlockExpressions.Add(expression);

			var blockExpression = Expression.Block(BlockVariables, BlockExpressions);

			while (BlockVariables.  Count > 1) BlockVariables.  RemoveAt(BlockVariables.  Count - 1);
			while (BlockExpressions.Count > 1) BlockExpressions.RemoveAt(BlockExpressions.Count - 1);

			return blockExpression;
		}

		public ParameterExpression BuildVariable(Expression expr, string? name = null)
		{
			if (name == null)
				name = expr.Type.Name + Interlocked.Increment(ref VarIndex);

			var variable = Expression.Variable(
				expr.Type,
				name.IndexOf('<') >= 0 ? null : name);

			BlockVariables.  Add(variable);
			BlockExpressions.Add(Expression.Assign(variable, expr));

			return variable;
		}

		public Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object?[]?,object?[]?,T>> BuildMapper<T>(Expression expr)
		{
			var type = typeof(T);

			if (expr.Type != type)
				expr = Expression.Convert(expr, type);

			var mapper = Expression.Lambda<Func<IQueryRunner,IDataContext,IDataReader,Expression,object?[]?,object?[]?,T>>(
				BuildBlock(expr), new[]
				{
					QueryRunnerParam,
					DataContextParam,
					DataReaderParam,
					ExpressionParam,
					ParametersParam,
					PreambleParam,
				});

			return mapper;
		}

		#endregion

		#region BuildMultipleQuery

		interface IMultipleQueryHelper
		{
			Expression GetSubquery(
				ExpressionBuilder       builder,
				Expression              expression,
				ParameterExpression     paramArray,
				IEnumerable<Expression> parameters);
		}

		class MultipleQueryHelper<TRet> : IMultipleQueryHelper
		{
			public Expression GetSubquery(
				ExpressionBuilder       builder,
				Expression              expression,
				ParameterExpression     paramArray,
				IEnumerable<Expression> parameters)
			{
				var lambda      = Expression.Lambda<Func<IDataContext,object?[],TRet>>(
					expression,
					Expression.Parameter(typeof(IDataContext), "ctx"),
					paramArray);
				var queryReader = CompiledQuery.Compile(lambda);

				return Expression.Call(
					null,
					MemberHelper.MethodOf(() => ExecuteSubQuery(null!, null!, null!)),
						DataContextParam,
						Expression.NewArrayInit(typeof(object), parameters),
						Expression.Constant(queryReader)
					);
			}

			static TRet ExecuteSubQuery(
				IDataContext                      dataContext,
				object?[]                         parameters,
				Func<IDataContext,object?[],TRet> queryReader)
			{
				var db = dataContext.Clone(true);

				db.CloseAfterUse = true;

				return queryReader(db, parameters);
			}
		}

		static Expression GetMultipleQueryExpression(IBuildContext context, MappingSchema mappingSchema,
			Expression expression, HashSet<ParameterExpression> parameters, out bool isLazy)
		{
			if (!Common.Configuration.Linq.AllowMultipleQuery)
				throw new LinqException("Multiple queries are not allowed. Set the 'LinqToDB.Common.Configuration.Linq.AllowMultipleQuery' flag to 'true' to allow multiple queries.");

			var valueExpression = EagerLoading.GenerateDetailsExpression(context, mappingSchema, expression);

			if (valueExpression == null)
			{
				isLazy = true;
				return GetMultipleQueryExpressionLazy(context, mappingSchema, expression, parameters);
			}

			valueExpression = EagerLoading.AdjustType(valueExpression, expression.Type, mappingSchema);

			isLazy = false;
			return valueExpression;
		}

		static Expression GetMultipleQueryExpressionLazy(IBuildContext context, MappingSchema mappingSchema, Expression expression, HashSet<ParameterExpression> parameters)
		{
			expression.Visit(e =>
			{
				if (e.NodeType == ExpressionType.Lambda)
					foreach (var p in ((LambdaExpression)e).Parameters)
						parameters.Add(p);
			});

			// Convert associations.
			//
			return expression.Transform(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.MemberAccess :
						{
							var root = context.Builder.GetRootObject(e);

							if (root != null &&
								root.NodeType == ExpressionType.Parameter &&
								!parameters.Contains((ParameterExpression)root))
							{
								var res = context.IsExpression(e, 0, RequestFor.Association);

								if (res.Result)
								{
									var associationContext = (AssociationContext)res.Context!;

									if (associationContext.Descriptor.IsList)
									{
										var me = (MemberExpression)e;

										var parentType = me.Expression.Type;
										var childType  = me.Type;

										var queryMethod = AssociationHelper.CreateAssociationQueryLambda(context.Builder,
											new AccessorMember(me), associationContext.Descriptor, parentType, parentType, childType, false,
											false, null, out _);

										var dcConst = Expression.Constant(context.Builder.DataContext.Clone(true));

										var expr = queryMethod.GetBody(me.Expression, dcConst);

										if (e == expression)
										{
											expr = Expression.Call(
												Methods.Enumerable.ToList.MakeGenericMethod(childType),
												expr);
										}

										return expr;
									}
								}
							}

							break;
						}
				}

				return e;
			});
		}

		public Expression? AssociationRoot;
		public Stack<Tuple<AccessorMember, IBuildContext, List<LoadWithInfo[]>?>>? AssociationPath;

		HashSet<Expression>? _buildMultipleQueryExpressions;

		public Expression BuildMultipleQuery(IBuildContext context, Expression expression, bool enforceServerSide)
		{
			var parameters = new HashSet<ParameterExpression>();

			expression = GetMultipleQueryExpression(context, MappingSchema, expression, parameters, out var isLazy);

			if (!isLazy)
				return expression;

			var paramex = Expression.Parameter(typeof(object[]), "ps");
			var parms   = new List<Expression>();

			// Convert parameters.
			//
			expression = expression.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Lambda)
				{
					foreach (var param in ((LambdaExpression)e).Parameters)
					{
						parameters.Add(param);
					}
				}

				var root = context.Builder.GetRootObject(e);

				if (root != null &&
					root.NodeType == ExpressionType.Parameter &&
					!parameters.Contains((ParameterExpression)root))
				{
					if (_buildMultipleQueryExpressions == null)
						_buildMultipleQueryExpressions = new HashSet<Expression>();

					_buildMultipleQueryExpressions.Add(e);

					var ex = Expression.Convert(BuildExpression(context, e, enforceServerSide), typeof(object));

					_buildMultipleQueryExpressions.Remove(e);

					parms.Add(ex);

					return Expression.Convert(
						Expression.ArrayIndex(paramex, Expression.Constant(parms.Count - 1)),
						e.Type);
				}

				return e;
			});

			var sqtype = typeof(MultipleQueryHelper<>).MakeGenericType(expression.Type);
			var helper = (IMultipleQueryHelper)Activator.CreateInstance(sqtype)!;

			return helper.GetSubquery(this, expression, paramex, parms);
		}

		#endregion

	}
}
