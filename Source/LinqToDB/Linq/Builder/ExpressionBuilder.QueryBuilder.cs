using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
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
	using System.Runtime.CompilerServices;

	partial class ExpressionBuilder
	{
		#region BuildExpression

		readonly HashSet<Expression>                    _skippedExpressions   = new ();
		readonly Dictionary<Expression,UnaryExpression> _convertedExpressions = new ();

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

		Expression ConvertAssignmentArgument(Dictionary<Expression, Expression> translated, IBuildContext context, Expression expr, MemberInfo? memberInfo, ProjectFlags flags,
			string? alias)
		{
			var resultExpr = BuildSqlExpression(translated, context, expr, flags, alias);

			if (!string.IsNullOrEmpty(alias) && resultExpr is SqlPlaceholderExpression placeholder)
			{
				placeholder.Alias = alias;
				if (placeholder.Sql is SqlColumn column)
					column.RawAlias = alias;
			}

			// Update nullability
			resultExpr = (_updateNullabilityFromExtensionTransformer ??= TransformVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.UpdateNullabilityFromExtension(e))).Transform(resultExpr);

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

		private TransformVisitor<ExpressionBuilder>? _updateNullabilityFromExtensionTransformer;
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
						&& mc.Arguments[0] is SqlPlaceholderExpression placeholder)
					{
						resultExpr = placeholder.MakeNullable();
					}
				}
			}

			return resultExpr;
		}

		public Expression FinalizeProjection(IBuildContext context, Expression expression)
		{
			// going to parent

			while (context.Parent != null)
			{
				context = context.Parent;
			}

			// postprocess constuctors

			var globalGenerator = new ExpressionGenerator();
			var processedMap    = new Dictionary<Expression, Expression>();

			// convert all missed references
			var translatedMap   = new Dictionary<Expression, Expression>();
			var postProcessed = BuildSqlExpression(translatedMap, context, expression, ProjectFlags.Expression);

			// Deduplication
			//
			postProcessed = postProcessed.Transform(
				(builder: this, map: processedMap, translatedMap, generator: globalGenerator),
				static (ctx, e) =>
				{
					if (e is SqlErrorExpression error)
						throw error.CreateError();

					if (e is ContextConstructionExpression construction)
					{
						var innerExpression = ctx.builder.BuildSqlExpression(ctx.translatedMap,
							construction.BuildContext, construction.InnerExpression, ProjectFlags.Expression);

						if (ctx.map.TryGetValue(e, out var processed))
							return processed;

						var variable = ctx.generator.AssignToVariable(innerExpression);

						if (construction.PostProcess?.Count > 0)
						{
							foreach (var lambda in construction.PostProcess)
							{
								var body = lambda.GetBody(variable);
								ctx.generator.AddExpression(body);
							}
						}

						ctx.map[e] = variable;
						return variable;
					}

					//TODO: palcehorders also can be simplified
					//if (e is SqlPlaceholderExpression) ...

					return e;
				});

			globalGenerator.AddExpression(postProcessed);

			postProcessed = globalGenerator.Build();

			var withColumns = ToColumns(context, postProcessed);
			return withColumns;
		}

		public Expression ToColumns(IBuildContext rootContext, Expression expression)
		{
			var info         = new QueryInformation(rootContext.SelectQuery);
			var processedMap = new Dictionary<Expression, Expression>();

			var withColumns =
				expression.Transform(
					(builder: this, map: processedMap, info),
					static (context, expr) =>
					{
						if (context.map.TryGetValue(expr, out var mapped))
							return mapped;

						if (expr is SqlPlaceholderExpression placeholder && placeholder.SelectQuery != null)
						{
							do
							{
								var parent = context.info.GetParentQuery(placeholder.SelectQuery);
								if (parent == null)
								{
									placeholder = context.builder.MakeColumn(null, placeholder);
									break;
								}

								placeholder = context.builder.MakeColumn(parent, placeholder);

							} while (true);

							context.map[expr] = placeholder;
							return placeholder;
						}

						return expr;
					});

			return withColumns;
		}

		static bool IsSameParentTree(IBuildContext upToContext, IBuildContext? tested)
		{
			var current = tested;
			while (current != null)
			{
				if (current == upToContext || current.SelectQuery == upToContext.SelectQuery)
					return true;

				current = current.Parent;
			}

			return false;
		}

		static bool IsSameParentTree(QueryInformation info, SelectQuery testedQuery)
		{
			var parent = info.GetParentQuery(testedQuery);

			while (parent != null)
			{
				if (ReferenceEquals(parent, info.RootQuery))
					return true;

				parent = info.GetParentQuery(parent);
			}

			return false;
		}

		public Expression UpdateNesting(IBuildContext upToContext, Expression expression)
		{
			var info  = new QueryInformation(upToContext.SelectQuery);

			var withColumns =
				expression.Transform(
					(builder: this, upToContext, info),
					static (context, expr) =>
					{
						if (expr is SqlErrorExpression error)
							throw error.CreateError();

						if (expr is SqlPlaceholderExpression placeholder && !ReferenceEquals(context.upToContext.SelectQuery, placeholder.SelectQuery))
						{
							if (IsSameParentTree(context.info, placeholder.SelectQuery))
							{
								do
								{
									var parentQuery = context.info.GetParentQuery(placeholder.SelectQuery);

									if (parentQuery == null)
										break;

									placeholder = context.builder.MakeColumn(parentQuery, placeholder);

									if (ReferenceEquals(context.upToContext.SelectQuery, parentQuery))
										break;

								} while (true);
							}

							return placeholder;
						}

						return expr;
					});

			return withColumns;
		}

		public bool TryConvertToSql(IBuildContext context, ProjectFlags flags, Expression expression, ColumnDescriptor? columnDescriptor, [NotNullWhen(true)] out ISqlExpression? sqlExpression, out Expression actual)
		{
			sqlExpression = null;

			//Just test that we can convert
			actual = ConvertToSqlExpr(context, expression, flags | ProjectFlags.Test, columnDescriptor: columnDescriptor);
			if (actual is not SqlPlaceholderExpression placeholderTest)
				return false;

			sqlExpression = placeholderTest.Sql;

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				sqlExpression = null;
				//Test conversion success, do it again
				actual = ConvertToSqlExpr(context, expression, flags, columnDescriptor: columnDescriptor);
				if (actual is not SqlPlaceholderExpression placeholder)
					return false;

				sqlExpression = placeholder.Sql;
			}

			return true;
		}

		public Expression? TryConvertToSqlExpr(IBuildContext context, Expression expression, ProjectFlags flags)
		{
			//Just test that we can convert
			var converted = ConvertToSqlExpr(context, expression, flags | ProjectFlags.Test);
			if (converted is not SqlPlaceholderExpression)
				return null;

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				//Test conversion success, do it again
				converted = ConvertToSqlExpr(context, expression, flags);
				if (converted is not SqlPlaceholderExpression placeholder)
					return null;
			}

			return converted;
		}

		public SqlErrorExpression CreateSqlError(IBuildContext? context, Expression expression)
		{
			return new SqlErrorExpression(context, expression);
		}

		public Expression BuildSqlExpression(Dictionary<Expression, Expression> translated, IBuildContext context, Expression expression, ProjectFlags flags, string? alias = null)
		{
			var result = expression.Transform(
				(builder: this, context, flags, alias, translated),
				static (context, expr) =>
				{
					if (context.builder.CanBeCompiled(expr) && context.flags.HasFlag(ProjectFlags.Expression))
					{
						if (context.builder.ParametersContext._expressionAccessors.TryGetValue(expr, out var accessor))
						{
							// get data from parameter accessor

							var valueAccessor = context.builder.ParametersContext.ReplaceParameter(
								context.builder.ParametersContext._expressionAccessors, expr, false, s => { });

							var valueExpr = valueAccessor.ValueExpression;

							if (valueExpr.Type != expr.Type)
							{
								valueExpr = Expression.Convert(valueExpr.UnwrapConvert(), expr.Type);
							}

							return new TransformInfo(valueExpr, true);
						}
					}

					if (context.translated.TryGetValue(expr, out var replaced))
						return new TransformInfo(replaced, true);

					if (context.builder._skippedExpressions.Contains(expr))
						return new TransformInfo(expr, true);

					switch (expr.NodeType)
					{
						case ExpressionType.Convert       :
						case ExpressionType.ConvertChecked:
							{
								if (expr.Type == typeof(object))
									break;

								var cex = (UnaryExpression)expr;

								context.builder._convertedExpressions.Add(cex.Operand, cex);

								var saveBlockDisable = context.builder.IsBlockDisable;
								context.builder.IsBlockDisable = true;
								var newOperand = context.builder.BuildSqlExpression(context.translated, context.context, cex.Operand, context.flags);
								context.builder.IsBlockDisable = saveBlockDisable;

								if (newOperand.Type != cex.Type)
								{
									if (cex.Type.IsNullable() && newOperand is SqlPlaceholderExpression sqlPlaceholder)
									{
										newOperand = sqlPlaceholder.MakeNullable();
									}

									newOperand = cex.Update(newOperand);
								}
								var ret = new TransformInfo(newOperand, true);

								context.builder.RemoveConvertedExpression(cex.Operand);

								return ret;
							}

						case ExpressionType.MemberAccess:
							{
								var ma = (MemberExpression)expr;

								/*if (ma.Expression is not ContextRefExpression && context.builder.IsAssociation(ma.Expression))
								{
									var projected = context.builder.Project(context.context, ma, null, 0, context.flags, ma);
								}*/

								if (context.builder.IsServerSideOnly(ma) || context.builder.PreferServerSide(ma, false) && !context.builder.HasNoneSqlMember(ma))
								{
									return new TransformInfo(context.builder.BuildSql(context.context, expr, context.alias));
								}

								var newExpr = context.builder.ExposeExpression(ma);

								if (!ReferenceEquals(newExpr, ma))
									return new TransformInfo(newExpr, false, true);

								if (ma.Member.IsNullableValueMember())
									break;

								newExpr = context.builder.MakeExpression(ma, context.flags);

								if (!ReferenceEquals(newExpr, ma))
									return new TransformInfo(newExpr, false, true);

								/*
								if (ctx != null)
								{
									var prevCount  = context.context.SelectQuery.Select.Columns.Count;
									var expression = context.builder.MakeExpression(ctx, ma, context.flags);

									if (expression.NodeType == ExpressionType.Extension && expression is DefaultValueExpression 
																						&& ma.Expression?.NodeType == ExpressionType.Parameter)
									{
										var objExpression = context.builder.BuildSqlExpression(ctx, ma.Expression, context.flags, context.alias);
										var varTempVar    = objExpression.NodeType == ExpressionType.Parameter
											? objExpression
											: context.builder.BuildVariable(objExpression, ((ParameterExpression)ma.Expression).Name);

										var condition = Expression.Condition(
											Expression.Equal(varTempVar,
												new DefaultValueExpression(context.builder.MappingSchema, ma.Expression.Type)), expression,
											Expression.MakeMemberAccess(varTempVar, ma.Member));
										expression = condition;
									}
									else if (!context.alias.IsNullOrEmpty() && (ctx.SelectQuery.Select.Columns.Count - prevCount) == 1)
									{
										ctx.SelectQuery.Select.Columns[ctx.SelectQuery.Select.Columns.Count - 1].Alias = context.alias;
									}

									return new TransformInfo(expression, false, true);
								}
								*/


								var ex = ma.Expression;

								while (ex is MemberExpression memberExpression)
									ex = memberExpression.Expression;

								if (ex is MethodCallExpression ce)
								{
									var buildInfo = new BuildInfo((IBuildContext?)null, ce, new SelectQuery());
									if (context.builder.IsSequence(buildInfo))
									{
										var subqueryCtx = context.builder.GetSubQueryContext(context.context, ce);
										var subqueryExpr =
											subqueryCtx.Context.MakeExpression(
												new ContextRefExpression(ce.Type, subqueryCtx.Context), ProjectFlags.Root);

										subqueryExpr = ma.Replace(ex, subqueryExpr);

										return new TransformInfo(subqueryExpr, false, true);
									}


									if (!context.builder.IsEnumerableSource(ce) && context.builder.IsSubQuery(context.context, ce))
									{
										if (!context.context.Builder.IsMultipleQuery(ce, context.context.Builder.MappingSchema))
										{
											var info = context.builder.GetSubQueryContext(context.context, ce);
											if (context.alias != null)
												info.Context.SetAlias(context.alias);

											var par  = Expression.Parameter(ex.Type);
											var bex  = context.builder.MakeExpression(ma.Replace(ex, par), context.flags);

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
									if (!context.builder.ParametersContext._expressionAccessors.TryGetValue(ex, out var c))
										return new TransformInfo(ma);
									return new TransformInfo(Expression.MakeMemberAccess(Expression.Convert(c, ex.Type), ma.Member));
								}

								break;
							}

						case ExpressionType.Constant:
							{
								if (expr.Type.IsConstantable(true))
									break;

								if ((context.builder._buildMultipleQueryExpressions == null || !context.builder._buildMultipleQueryExpressions.Contains(expr)) && context.builder.IsSequence(new BuildInfo(context.context, expr, new SelectQuery())))
								{
									return new TransformInfo(context.builder.BuildMultipleQuery(context.context, expr, context.flags));
								}

								if (context.builder.ParametersContext._expressionAccessors.TryGetValue(expr, out var accessor))
									return new TransformInfo(Expression.Convert(accessor, expr.Type));

								break;
							}

						case ExpressionType.Coalesce:
						{
							//if (context.flags.HasFlag(ProjectFlags.Expression))
							{
								var sql = context.builder.TryConvertToSqlExpr(context.context, expr, context.flags);
								if (sql != null)
									return new TransformInfo(sql);
							}

							break;
						}
						case ExpressionType.Call:
							{
								var ce = (MethodCallExpression)expr;

								/*if (context.builder.IsEnumerableSource(ce))
									break;

								if (context.builder.IsGroupJoinSource(context.context, ce))
								{
									foreach (var arg in ce.Arguments.Skip(1))
										if (!context.builder._skippedExpressions.Contains(arg))
										context.builder._skippedExpressions.Add(arg);

									if (context.builder.IsSubQuery(context.context, ce))
									{
										if (ce.IsQueryable())
										//if (!typeof(IEnumerable).IsSameOrParentOf(expr.Type) || expr.Type == typeof(string) || expr.Type.IsArray)
										{
											var ctx = context.builder.GetContext(context.context, expr);

											if (ctx != null)
												return new TransformInfo(ctx.BuildExpression(expr, 0, context.enforceServerSide));
										}
									}

							break;
						}*/

								if (ce.IsAssociation(context.builder.MappingSchema))
								{
									var ctx = context.builder.GetContext(context.context, ce);
									if (ctx == null)
										throw new InvalidOperationException();

									return new TransformInfo(context.builder.MakeExpression(ce, context.flags));
								}

								/*if ((context.builder._buildMultipleQueryExpressions == null || !context.builder._buildMultipleQueryExpressions.Contains(ce)) && context.builder.IsSubQuery(context.context, ce))
								{
									if (context.builder.IsMultipleQuery(ce, context.builder.MappingSchema))
										return new TransformInfo(context.builder.BuildMultipleQuery(context.context, ce, context.flags));

									return new TransformInfo(context.builder.GetSubQueryExpression(context.context, ce, false, context.alias));
								}*/

								if (ce.IsSameGenericMethod(Methods.LinqToDB.SqlExt.Alias))
								{
									return new TransformInfo(context.builder.BuildSql(context.context, ce.Arguments[0], context.alias ?? ce.Arguments[1].EvaluateExpression<string>()));
								}

								var info = new BuildInfo(context.context, ce, new SelectQuery {ParentSelect = context.context.SelectQuery});

								if (context.builder.IsSequence(info))
								{
									return new TransformInfo(
										context.builder.GetSubQueryExpression(context.context, ce, false,
											context.alias), false, true);
								}

								//TODO: Don't like group by check, we have to validate this case
								if (context.flags.HasFlag(ProjectFlags.SQL)      ||
								    !context.context.SelectQuery.GroupBy.IsEmpty ||
								    context.builder.IsServerSideOnly(expr)
								   )
								{
									var newExpr = context.builder.TryConvertToSqlExpr(context.context, expr, context.flags);
									if (newExpr != null)
									{
										return new TransformInfo(newExpr, false, true);
									}
								}

								return new TransformInfo(expr);
							}

						case ExpressionType.New:
							{
								var ne = (NewExpression)expr;

								List<Expression>? arguments = null;
								for (var i = 0; i < ne.Arguments.Count; i++)
								{
									var argument    = ne.Arguments[i];
									var memberAlias = ne.Members?[i].Name;

									var newArgument = context.builder.ConvertAssignmentArgument(context.translated, context.context, argument, ne.Members?[i], context.flags, memberAlias);
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
								var newPart = (NewExpression)context.builder.BuildSqlExpression(context.translated, context.context, mi.NewExpression, context.flags);
								List<MemberBinding>? bindings = null;
								for (var i = 0; i < mi.Bindings.Count; i++)
								{
									var binding    = mi.Bindings[i];
									var newBinding = binding;
									if (binding is MemberAssignment assignment)
									{
										var argument = context.builder.ConvertAssignmentArgument(context.translated, context.context, assignment.Expression,
											assignment.Member, context.flags, assignment.Member.Name);
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

						case ExpressionType.Conditional:
						{
							var cond    = (ConditionalExpression)expr;
							var condSql = context.builder.TryConvertToSqlExpr(context.context, cond, context.flags);
							if (condSql != null)
								return new TransformInfo(condSql);

							/*var testSQl = context.builder.TryConvertToSqlExpr(context.context, cond.Test);
							if (testSQl != null)
								return new TransformInfo(cond.Update(testSQl, cond.IfTrue, cond.IfFalse), false, true);
							*/
							break;
						}

						case ExpressionType.Extension:
						{
							if (expr is ContextRefExpression contextRef)
							{
								var buildExpr = context.builder.MakeExpression(contextRef, context.flags);
								if (buildExpr.Type != expr.Type)
								{
									buildExpr = Expression.Convert(buildExpr, expr.Type);
								}

								if (!ReferenceEquals(buildExpr, contextRef))
								{
									buildExpr = context.builder.BuildSqlExpression(context.translated, context.context,
										buildExpr,
										context.flags, context.alias);
								}

								context.translated[expr] = buildExpr;

								return new TransformInfo(buildExpr);
							}

							if (expr is SqlPlaceholderExpression)
								return new TransformInfo(expr);

							break;
						}
					}

					//TODO: remove
					/*if (false || EnforceServerSide(context.context))
					{
						switch (expr.NodeType)
						{
							case ExpressionType.MemberInit :
							case ExpressionType.Convert    :
								break;
							default                        :
							{
								if (!false && context.builder.CanBeCompiled(expr))
									break;
								return new TransformInfo(context.builder.BuildSql(context.context, expr,
									context.alias));
							}

						}
					}*/

					return new TransformInfo(expr);
				});

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				result = UpdateNesting(context, result);
			}

			return result;
		}

		Expression CorrectConditional(Dictionary<Expression, Expression> translated, IBuildContext context, Expression expr, ProjectFlags flags, string? alias)
		{
			return BuildSqlExpression(translated, context, expr, flags, alias);
			
			if (expr.NodeType != ExpressionType.Conditional)
				return BuildSqlExpression(translated, context, expr, flags, alias);

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
						//var sql = objContext.MakeSql(obj)?.Sql;
						throw new NotImplementedException();
						/*if (sql.Length > 0)
						{
							Expression? predicate = null;
							foreach (var f in sql)
							{
								if (f.Sql is SqlField field && field.Table!.All == field)
											continue;

								var valueType = f.Sql.SystemType!;

								if (!valueType.IsNullableType())
									valueType = valueType.AsNullable();

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
						}*/
					}
				}
			}

			if (cond == expr)
				expr = BuildSqlExpression(translated, context, expr, flags, alias);
			else
				expr = cond;

			return expr;
		}

		bool IsEnumerableSource(Expression expr)
		{
			if (!CanBeCompiled(expr))
			{
				// Special case, contains has it's own translation
				if (!(expr is MethodCallExpression mce && mce.IsQueryable("Contains")))
					return false;
			}

			var selectQuery = new SelectQuery();
			while (expr != null)
			{
				var buildInfo = new BuildInfo((IBuildContext?)null, expr, selectQuery);
				if (GetBuilder(buildInfo, false) is EnumerableBuilder)
				{
					return true;
				}

				switch (expr)
				{
					case MemberExpression me:
						expr = me.Expression;
						continue;
					case MethodCallExpression mc when mc.IsQueryable():
						expr = mc.Arguments[0];
						continue;
				}

				break;
			}

			return false;
		}

		bool IsMultipleQuery(MethodCallExpression ce, MappingSchema mappingSchema)
		{
			//TODO: Multiply query check should be smarter, possibly not needed if we create fallback mechanism
			var result = !ce.IsQueryable(FirstSingleBuilder.MethodNames)
			       && typeof(IEnumerable).IsSameOrParentOf(ce.Type)
			       && ce.Type != typeof(string) 
			       && !ce.Type.IsArray 
			       && !ce.IsAggregate(mappingSchema);

			return result;
		}

		class SubQueryContextInfo
		{
			public MethodCallExpression Method  = null!;
			public IBuildContext        Context = null!;
			public Expression?          Expression;
		}


		public Expression CorrectRoot(Expression expr)
		{
			if (expr is MethodCallExpression mc && mc.IsQueryable())
			{
				var firstArg = CorrectRoot(mc.Arguments[0]);
				if (!ReferenceEquals(firstArg, mc.Arguments[0]))
				{
					var args = mc.Arguments.ToArray();
					args[0] = firstArg;
					return mc.Update(null, args);
				}

			}
			else
				expr = MakeExpression(expr, ProjectFlags.Root);

			return expr;
		}

		public ContextRefExpression? GetRootContext(Expression? expression)
		{
			if (expression == null)
				return null;

			expression = MakeExpression(expression, ProjectFlags.Root);

			if (expression is MemberExpression memberExpression)
			{
				expression = GetRootContext(memberExpression.Expression);
			}

			if (expression is MethodCallExpression mc && mc.IsQueryable())
			{
				expression = GetRootContext(mc.Arguments[0]);
			}

			return expression as ContextRefExpression;
		}

		List<SubQueryContextInfo>? _buildContextCache;

		SubQueryContextInfo GetSubQueryContext(IBuildContext context, MethodCallExpression expr)
		{
			var testExpression = (MethodCallExpression)CorrectRoot(expr);

			_buildContextCache ??= new List<SubQueryContextInfo>();

			foreach (var item in _buildContextCache)
			{
				if (testExpression.EqualsTo(item.Method, OptimizationContext.GetSimpleEqualsToContext(false)))
					return item;
			}

			var rootQuery = GetRootContext(testExpression);

			if (rootQuery != null)
			{
				context = rootQuery.BuildContext;
			}

			var ctx = GetSubQuery(context, testExpression);

			var info = new SubQueryContextInfo { Method = testExpression, Context = ctx };

			_buildContextCache.Add(info);

			return info;
		}

		public Expression GetSubQueryExpression(IBuildContext context, MethodCallExpression expr, bool enforceServerSide, string? alias)
		{
			var info = GetSubQueryContext(context, expr);
			if (info.Expression == null)
				info.Expression = MakeExpression(new ContextRefExpression(expr.Type, info.Context), ProjectFlags.Expression);

			if (!alias.IsNullOrEmpty())
				info.Context.SetAlias(alias);

			return UpdateNesting(context, info.Expression);
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
			var ctx = new WritableContext<bool, ExpressionBuilder>(this);

			var found = expr.Find(ctx, static (ctx, e) => ctx.StaticValue.HasNoneSqlMemberFind(ctx, e));

			return found != null && !ctx.WriteableValue;
		}

		private bool HasNoneSqlMemberFind(WritableContext<bool, ExpressionBuilder> context, Expression e)
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

					if (om != null && om[me.Member.Name] == null)
					{
						foreach (var a in om.Associations)
							if (a.MemberInfo.EqualsTo(me.Member))
								return false;

						return true;
					}

					return false;
				}
				case ExpressionType.Call:
				{
					var mc = (MethodCallExpression)e;
					if (mc.IsCte(MappingSchema))
						context.WriteableValue = true;
					break;
				}
			}

			return context.WriteableValue;
		}

		#endregion

		#region PreferServerSide

		private FindVisitor<ExpressionBuilder>? _enforceServerSideVisitorTrue;
		private FindVisitor<ExpressionBuilder>? _enforceServerSideVisitorFalse;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private FindVisitor<ExpressionBuilder> GetVisitor(bool enforceServerSide)
		{
			if (enforceServerSide)
				return _enforceServerSideVisitorTrue ??= FindVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.PreferServerSide(e, true));
			else
				return _enforceServerSideVisitorFalse ??= FindVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.PreferServerSide(e, false));
		}

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
								info = info.Replace(l.Parameters[0], pi.Expression);

							return GetVisitor(enforceServerSide).Find(info) != null;
						}

						var attr = GetExpressionAttribute(pi.Member);
						return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeCompiled(expr);
					}

				case ExpressionType.Call:
					{
						var pi = (MethodCallExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

						if (l != null)
							return GetVisitor(enforceServerSide).Find(l.Body.Unwrap()) != null;

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
								var newExpr = body.Transform((l, binary), static (context, wpi) =>
								{
									if (wpi.NodeType == ExpressionType.Parameter)
									{
										if (context.l.Parameters[0] == wpi)
											return context.binary.Left;
										if (context.l.Parameters[1] == wpi)
											return context.binary.Right;
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

		public Expression ToReadExpression(Expression expression)
		{
			var toRead = expression.Transform(e =>
			{
				if (e is SqlPlaceholderExpression placeholder)
				{
					if (placeholder.Sql == null)
						throw new InvalidOperationException();
					if (placeholder.Index == null)
						throw new InvalidOperationException();

					var valueType = placeholder.Type;

					// read from DataReader as Nullable
					/*if (placeholder.IsNullable && valueType.IsValueType && !valueType.IsNullable())
						valueType = valueType.AsNullable();*/

					return new ConvertFromDataReaderExpression(valueType, placeholder.Index.Value,
						QueryHelper.GetColumnDescriptor(placeholder.Sql)?.ValueConverter, DataReaderParam);
				}

				if (e is SqlReaderIsNullExpression isNullExpression)
				{
					if (isNullExpression.Placeholder.Index == null)
						throw new InvalidOperationException();

					var nullCheck = Expression.Call(
						DataReaderParam,
						ReflectionHelper.DataReader.IsDBNull,
						ExpressionInstances.Int32Array(isNullExpression.Placeholder.Index.Value));

					return nullCheck;
				}

				return e;
			});

			return toRead;
		}

		public Expression<Func<IQueryRunner,IDataContext,IDataReader,Expression,object?[]?,object?[]?,T>> BuildMapper<T>(Expression expr)
		{
			var type = typeof(T);

			if (expr.Type != type)
				expr = Expression.Convert(expr, type);

			expr = ToReadExpression(expr);

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
			expression.Visit(parameters, static (parameters, e) =>
			{
				if (e.NodeType == ExpressionType.Lambda)
					foreach (var p in ((LambdaExpression)e).Parameters)
						parameters.Add(p);
			});

			// Convert associations.
			//
			return expression.Transform((context, expression, parameters), static (context, e) =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.MemberAccess :
						{
							var root = context.context.Builder.GetRootObject(e);

							if (root != null &&
								root.NodeType == ExpressionType.Parameter &&
								!context.parameters.Contains((ParameterExpression)root)
								|| root is ContextRefExpression)
							{
								var res = context.context.IsExpression(e, 0, RequestFor.Association);

								if (res.Result)
								{
									var associationContext = (AssociationContext)res.Context!;

									if (associationContext.Descriptor.IsList)
									{
										var me = (MemberExpression)e;

										var parentType = me.Expression.Type;
										var childType  = me.Type;

										var queryMethod = AssociationHelper.CreateAssociationQueryLambda(context.context.Builder,
											new AccessorMember(me), associationContext.Descriptor, parentType, parentType, childType, false,
											false, null, out _);

										var dcConst = Expression.Constant(context.context.Builder.DataContext.Clone(true));

										var expr = queryMethod.GetBody(me.Expression, dcConst);

										if (e == context.expression)
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

		public Expression BuildMultipleQuery(IBuildContext context, Expression expression, ProjectFlags flags)
		{
			var parameters = new HashSet<ParameterExpression>();

			expression = GetMultipleQueryExpression(context, MappingSchema, expression, parameters, out var isLazy);

			if (!isLazy)
				return expression;

			var paramex = Expression.Parameter(typeof(object[]), "ps");
			var parms   = new List<Expression>();

			var translated = new Dictionary<Expression, Expression>();

			// Convert parameters.
			//
			expression = expression.Transform((parameters, buildContext: context, builder: this, parms, paramex, flags, translated), static (context, e) =>
			{
				if (e.NodeType == ExpressionType.Lambda)
				{
					foreach (var param in ((LambdaExpression)e).Parameters)
					{
						context.parameters.Add(param);
					}
				}

				var root = context.buildContext.Builder.GetRootObject(e);

				if (root != null &&
				    (root.NodeType == ExpressionType.Parameter &&
				     !context.parameters.Contains((ParameterExpression)root) 
				     || root is ContextRefExpression))
				{
					if (context.builder._buildMultipleQueryExpressions == null)
						context.builder._buildMultipleQueryExpressions = new HashSet<Expression>();

					context.builder._buildMultipleQueryExpressions.Add(e);

					var ex = Expression.Convert(context.builder.BuildSqlExpression(context.translated, context.buildContext, e, context.flags), typeof(object));

					context.builder._buildMultipleQueryExpressions.Remove(e);

					context.parms.Add(ex);

					return Expression.Convert(
						Expression.ArrayIndex(context.paramex, ExpressionInstances.Int32(context.parms.Count - 1)),
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
