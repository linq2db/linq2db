using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Runtime.CompilerServices;

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

		Expression ConvertAssignmentArgument(Dictionary<Expression, Expression> translated, IBuildContext context, Expression pathExpr, Expression expr, 
			MemberInfo? memberInfo, 
			Type memberType, 
			ProjectFlags flags, string? alias)
		{
			var resultExpr = TryConvertToSqlExpr(context, expr, flags);

			if (resultExpr is SqlPlaceholderExpression tryPlaceholder)
			{
				if (tryPlaceholder.Sql.ElementType == QueryElementType.SqlValue)
				{
					resultExpr = null;
				}
			}

			if (resultExpr == null)
			{
				resultExpr = BuildSqlExpression(translated, context, expr, flags, alias);
			}

			if (resultExpr is SqlPlaceholderExpression placeholder)
			{
				if (memberInfo != null)
				{
					var trackingPath = Expression.MakeMemberAccess(pathExpr, memberInfo);

					// this Path used later in ConvertCompare
					//
					placeholder = placeholder.WithTrackingPath(trackingPath);
					resultExpr  = placeholder;
				}

				if (!string.IsNullOrEmpty(alias))
				{
					placeholder.Alias = alias;
					if (placeholder.Sql is SqlColumn column)
						column.RawAlias = alias;
				}

				if (!memberType.IsNullable() && placeholder.Type.IsNullable())
				{
					resultExpr = placeholder.MakeNotNullable();
				}
			}

			// Update nullability
			resultExpr =
				(_updateNullabilityFromExtensionTransformer ??=
					TransformVisitor<ExpressionBuilder>.Create(this,
						static (ctx, e) => ctx.UpdateNullabilityFromExtension(e))).Transform(resultExpr);

			if (resultExpr.NodeType == ExpressionType.Convert || resultExpr.NodeType == ExpressionType.ConvertChecked)
			{
				var conv = (UnaryExpression)resultExpr;
				if (memberType.IsNullable() == true
				    && conv.Operand is SqlPlaceholderExpression readerExpression
				    && !readerExpression.Type.IsNullable())
				{
					resultExpr = readerExpression.MakeNullable();
				}
			}
			else if (resultExpr.NodeType == ExpressionType.Extension &&
					 resultExpr is SqlPlaceholderExpression readerExpression)
			{
				if (memberType.IsNullable() &&
					!readerExpression.Type.IsNullable())
				{
					resultExpr = readerExpression.MakeNullable();
				}
				else if (!memberType.IsNullable() && readerExpression.Type.IsNullable())
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
				var attr = mc.Method.GetExpressionAttribute(MappingSchema);

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

		Expression Deduplicate(Expression expression)
		{
			var visited    = new HashSet<Expression>();
			var duplicates = new Dictionary<Expression, Expression?>();

			expression.Visit(
				(builder: this, duplicates, visited),
				static (ctx, e) =>
				{
					if (e is SqlGenericConstructorExpression || e is SqlPlaceholderExpression)
					{
						if (!ctx.visited.Add(e))
						{
							ctx.duplicates.Add(e, null);
						}
					}
				});

			if (duplicates.Count == 0)
				return expression;

			var globalGenerator = new ExpressionGenerator();

			foreach (var d in duplicates.Keys.ToList())
			{
				var variable = globalGenerator.AssignToVariable(d);
				duplicates[d] = variable;
			}

			var corrected = expression.Transform(
				(builder: this, duplicates),
				static (ctx, e) =>
				{
					if (e.NodeType == ExpressionType.Extension && ctx.duplicates.TryGetValue(e, out var replacement))
					{
						return replacement;
					}

					return e;
				});

			globalGenerator.AddExpression(corrected);

			var result = globalGenerator.Build();
			return result;
		}

		Expression FinalizeProjection<T>(
			Query<T>            query, 
			IBuildContext       context, 
			Expression          expression, 
			ParameterExpression queryParameter, 
			ref List<Preamble>? preambles,
			Expression[]        previousKeys)
		{
			// going to parent

			while (context.Parent != null)
			{
				context = context.Parent;
			}

			// postprocessing constructors

			var globalGenerator = new ExpressionGenerator();
			var processedMap    = new Dictionary<Expression, Expression>();

			// convert all missed references
			var postProcessed = BuildSqlExpression(new Dictionary<Expression, Expression>(), context, expression, ProjectFlags.Expression);

			// deduplicate objects instantiation
			postProcessed = Deduplicate(postProcessed);

			// process eager loading queries
			var correctedEager = CompleteEagerLoadingExpressions(postProcessed, context, queryParameter, ref preambles, previousKeys);
			if (!ReferenceEquals(correctedEager, postProcessed))
			{
				// convert all missed references
				postProcessed = BuildSqlExpression(new Dictionary<Expression, Expression>(), context, correctedEager, ProjectFlags.Expression);
			}

			// Deduplication
			//
			postProcessed = postProcessed.Transform(
				(builder: this, map: processedMap, translatedMap: new Dictionary<Expression, Expression>(), generator: globalGenerator, context),
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
			return withColumns;}

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
								var parent = context.info.GetParentQuery(placeholder.SelectQuery!);

								placeholder = context.builder.MakeColumn(parent, placeholder);

								if (parent == null)
									break;

							} while (true);

							context.map[expr] = placeholder;
							return placeholder;
						}

						return expr;
					});

			return withColumns;
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
			// short path
			if (expression is SqlPlaceholderExpression currentPlaceholder && currentPlaceholder.SelectQuery == upToContext.SelectQuery)
				return expression;

			var info = new QueryInformation(upToContext.SelectQuery);

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
			flags = flags & ~ProjectFlags.Expression | ProjectFlags.SQL;

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
				var newActual = ConvertToSqlExpr(context, expression, flags, columnDescriptor: columnDescriptor);
				if (newActual is not SqlPlaceholderExpression placeholder)
					return false;

				sqlExpression = placeholder.Sql;
			}

			return true;
		}

		public Expression? TryConvertToSqlExpr(IBuildContext context, Expression expression, ProjectFlags flags)
		{
			flags |= ProjectFlags.SQL;
			flags &= ~ProjectFlags.Expression;

			//Just test that we can convert
			var converted = ConvertToSqlExpr(context, expression, flags | ProjectFlags.Test);
			if (converted is not SqlPlaceholderExpression)
				return null;

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				//Test conversion success, do it again
				converted = ConvertToSqlExpr(context, expression, flags);
				if (converted is not SqlPlaceholderExpression)
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
					// Shortcut: if expression can be compiled we can live it as is but inject accessors 
					//
					if (context.flags.HasFlag(ProjectFlags.Expression) && context.builder.CanBeCompiled(expr))
					{
						// correct expression based on accessors

						var valueAccessor = context.builder.ParametersContext.ReplaceParameter(
							context.builder.ParametersContext._expressionAccessors, expr, false, s => { });

						var valueExpr = valueAccessor.ValueExpression;

						if (valueExpr.Type != expr.Type)
						{
							valueExpr = Expression.Convert(valueExpr.UnwrapConvert(), expr.Type);
						}

						return new TransformInfo(valueExpr, true);
					}

					if (context.translated.TryGetValue(expr, out var replaced))
						return new TransformInfo(replaced, true);

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

								break;
							}

						case ExpressionType.Call:
							{
								var newExpr = context.builder.MakeExpression(expr, context.flags);

								if (!ReferenceEquals(newExpr, expr))
								{
									return new TransformInfo(newExpr, false, true);
								}

								var ce = (MethodCallExpression)expr;

								if (ce.IsSameGenericMethod(Methods.LinqToDB.SqlExt.Alias))
								{
									var withAlias = context.builder.BuildSqlExpression(context.translated, context.context,
										ce.Arguments[0],
										context.flags, context.alias ?? ce.Arguments[1].EvaluateExpression<string>());
									return new TransformInfo(withAlias);
								}

								var info = new BuildInfo(context.context, ce, new SelectQuery {ParentSelect = context.context.SelectQuery});

								if (context.builder.IsSequence(info))
								{
									return new TransformInfo(
										context.builder.GetSubQueryExpression(context.context, ce, false,
											context.alias, context.flags.HasFlag(ProjectFlags.Test)), false, true);
								}

								break;
							}

						case ExpressionType.New:
							{
								var ne = (NewExpression)expr;

								List<Expression>? arguments = null;

								var parameters = ne.Constructor.GetParameters();

								for (var i = 0; i < ne.Arguments.Count; i++)
								{
									var argument    = ne.Arguments[i];
									var memberInfo  = ne.Members?[i];
									var memberAlias = memberInfo?.Name            ?? parameters[i].Name;
									var memberType  = memberInfo?.GetMemberType() ?? parameters[i].ParameterType;

									var newArgument = context.builder.ConvertAssignmentArgument(context.translated, context.context, ne, argument, memberInfo, memberType, context.flags, memberAlias);
									if (newArgument != argument)
										arguments ??= ne.Arguments.Take(i).ToList();

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
										var argument = context.builder.ConvertAssignmentArgument(context.translated, context.context, mi, assignment.Expression,
											assignment.Member, assignment.Member.GetMemberType(), context.flags, assignment.Member.Name);
										if (argument != assignment.Expression)
										{
											newBinding = Expression.Bind(assignment.Member, argument);
										}
									}

									if (newBinding != binding)
									{
										bindings ??= mi.Bindings.Take(i).ToList();
									}

									bindings?.Add(newBinding);
								}

								if (mi.NewExpression != newPart || bindings != null)
								{
									mi = mi.Update(newPart, bindings ?? mi.Bindings.AsEnumerable());
								}

								return new TransformInfo(mi, true);
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
								else
								{
									//TODO: maybe remove
									var info = new BuildInfo(context.context, contextRef, new SelectQuery {ParentSelect = context.context.SelectQuery});

									if (context.builder.IsSequence(info))
									{
										buildExpr = context.builder.GetSubQueryExpression(context.context, contextRef,
											false,
											context.alias, context.flags.HasFlag(ProjectFlags.Test));
									}
								}

								context.translated[expr] = buildExpr;

								return new TransformInfo(buildExpr, false, true);
							}

							if (expr is SqlGenericConstructorExpression constructorExpression)
							{
								if (context.flags.HasFlag(ProjectFlags.Expression))
								{
									var constructed = context.builder.TryConstruct(constructorExpression, context.context, context.flags);
									if (!ReferenceEquals(constructed, constructorExpression))
									{
										constructed = context.builder.BuildSqlExpression(context.translated,
											context.context, constructed, context.flags, context.alias);
									}
									context.translated[expr] = constructed;
									return new TransformInfo(constructed, false, true);
								}
							}

							if (expr is SqlGenericParamAccessExpression paramAccessExpression)
							{
								return new TransformInfo(context.builder.MakeExpression(paramAccessExpression, context.flags), false, true);
							}

							return new TransformInfo(expr);
						}


						/*
						case ExpressionType.Conditional:
						case ExpressionType.Throw:
						case ExpressionType.Block:

						case ExpressionType.Lambda:
						case ExpressionType.Parameter:
						case ExpressionType.NewArrayInit:
					{
							return new TransformInfo(expr);
						}
					*/
					}

					/*
					var asSQL = context.builder.TryConvertToSqlExpr(context.context, expr, context.flags);
					if (asSQL != null)
						return new TransformInfo(asSQL);
						*/

					return new TransformInfo(expr);
				});

			return result;
				}

		class SubQueryContextInfo
				{
			public Expression    SequenceExpression  = null!;
			public IBuildContext Context = null!;
			public Expression?   Expression;
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

		public ContextRefExpression? GetRootContext(Expression? expression, bool isAggregation)
		{
			if (expression == null)
				return null;

			expression = MakeExpression(expression, isAggregation ? ProjectFlags.AggregationRoot : ProjectFlags.Root);

			if (expression is MemberExpression memberExpression)
			{
				expression = GetRootContext(memberExpression.Expression, isAggregation);
			}

			if (expression is MethodCallExpression mc && mc.IsQueryable())
			{
				expression = GetRootContext(mc.Arguments[0], isAggregation);
			}

			return expression as ContextRefExpression;
		}

		List<SubQueryContextInfo>? _buildContextCache;

		SubQueryContextInfo GetSubQueryContext(IBuildContext context, Expression expr, bool isTest)
		{
			var testExpression = CorrectRoot(expr);

			_buildContextCache ??= new List<SubQueryContextInfo>();

			foreach (var item in _buildContextCache)
			{
				if (testExpression.EqualsTo(item.SequenceExpression, OptimizationContext.GetSimpleEqualsToContext(false)))
					return item;
			}

			var rootQuery = GetRootContext(testExpression, false);

			if (rootQuery != null)
			{
				context = rootQuery.BuildContext;
			}
			else
			{
				var contextRef = new ContextRefExpression(typeof(object), context);
				rootQuery = GetRootContext(contextRef, false);
				if (rootQuery != null)
				{
					context = rootQuery.BuildContext;
				}
			}

			var ctx = GetSubQuery(context, testExpression, isTest);

			var info = new SubQueryContextInfo { SequenceExpression = testExpression, Context = ctx };
			
			if (!isTest)
			{
				_buildContextCache.Add(info);
			}

			return info;
		}

		Expression GetSubQueryExpression(IBuildContext context, Expression expr, bool enforceServerSide, string? alias, bool isTest)
		{
			var info = GetSubQueryContext(context, expr, isTest);
			if (info.Expression == null)
				info.Expression = MakeExpression(new ContextRefExpression(expr.Type, info.Context), ProjectFlags.Expression);

			if (!string.IsNullOrEmpty(alias))
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
			if (expr.Type == typeof(Sql.SqlID))
				return true;

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

						var attr = pi.Member.GetExpressionAttribute(MappingSchema);
						return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeCompiled(expr);
					}

				case ExpressionType.Call:
					{
						var pi = (MethodCallExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

						if (l != null)
							return GetVisitor(enforceServerSide).Find(l.Body.Unwrap()) != null;

						var attr = pi.Method.GetExpressionAttribute(MappingSchema);
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
			name ??= expr.Type.Name + Interlocked.Increment(ref VarIndex);

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

		public Expression<Func<IQueryRunner,IDataContext,DbDataReader,Expression,object?[]?,object?[]?,T>> BuildMapper<T>(Expression expr)
		{
			var type = typeof(T);

			if (expr.Type != type)
				expr = Expression.Convert(expr, type);

			expr = ToReadExpression(expr);

			var mapper = Expression.Lambda<Func<IQueryRunner,IDataContext,DbDataReader,Expression,object?[]?,object?[]?,T>>(
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

			if (valueExpression.Type != expression.Type)
				valueExpression = new SqlAdjustTypeExpression(valueExpression, expression.Type, mappingSchema);

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

										var parentType = me.Expression!.Type;
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
					context.builder._buildMultipleQueryExpressions ??= new HashSet<Expression>();

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
