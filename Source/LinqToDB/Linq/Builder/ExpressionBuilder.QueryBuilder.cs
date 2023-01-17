using System.Diagnostics.CodeAnalysis;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using LinqToDB.Expressions;

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

		Expression Deduplicate(Expression expression, bool onlyConstructRef)
		{
			var visited    = new HashSet<Expression>(ExpressionEqualityComparer.Instance);
			var duplicates = new Dictionary<Expression, Expression?>(ExpressionEqualityComparer.Instance);

			expression.Visit(
				(builder: this, duplicates, visited, onlyConstructRef),
				static (ctx, e) =>
				{

					bool checkForDuplicate;

					if (ctx.onlyConstructRef)
					{
						checkForDuplicate = e is ContextConstructionExpression;
					}
					else
					{
						checkForDuplicate = e is SqlGenericConstructorExpression || e is SqlAdjustTypeExpression ||
						                    e is SqlReaderIsNullExpression       || e is ContextConstructionExpression;
					}
					/*else
					{
						if (e is MemberExpression me)
						{
							var current = me;
							do
							{
								if (current.Expression is ContextRefExpression)
								{
									checkForDuplicate = true;
									break;
								}

								if (current.Expression is MemberExpression me2)
									current = me2;
								else
									break;
							} while (true);
						}
					}*/

					if (checkForDuplicate)
					{
						if (!ctx.visited.Add(e))
						{
							ctx.duplicates[e] = null;
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
					if ((e.NodeType == ExpressionType.Extension || e.NodeType == ExpressionType.MemberAccess) && ctx.duplicates.TryGetValue(e, out var replacement))
					{
						return replacement!;
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

			// convert all missed references
			var postProcessed = FinalizeConstructors(context, expression);

			// process eager loading queries
			var correctedEager = CompleteEagerLoadingExpressions(postProcessed, context, queryParameter, ref preambles, previousKeys);
			if (!ReferenceEquals(correctedEager, postProcessed))
			{
				// convert all missed references
				postProcessed = FinalizeConstructors(context, correctedEager);
			}

			var withColumns = ToColumns(context, postProcessed);
			return withColumns;
		}

		static void BuildParentsInfo(SelectQuery selectQuery, Dictionary<SelectQuery, SelectQuery> parentInfo)
		{
			foreach (var ts in selectQuery.From.Tables)
			{
				if (ts.Source is SelectQuery sc)
				{
					parentInfo[sc] = selectQuery;
					BuildParentsInfo(sc, parentInfo);
				}

				foreach (var join in ts.Joins)
				{
					if (join.Table.Source is SelectQuery jc)
					{
						parentInfo[jc] = selectQuery;
						BuildParentsInfo(jc, parentInfo);
					}
				}
			}
		}

		static bool GetParentQuery(Dictionary<SelectQuery, SelectQuery> parentInfo, SelectQuery currentQuery, [MaybeNullWhen(false)] out SelectQuery? parentQuery)
		{
			return parentInfo.TryGetValue(currentQuery, out parentQuery);
		}

		public Expression UpdateNesting(IBuildContext upToContext, Expression expression)
		{
			// short path
			if (expression is SqlPlaceholderExpression currentPlaceholder && currentPlaceholder.SelectQuery == upToContext.SelectQuery)
				return expression;

			var parentInfo = new Dictionary<SelectQuery, SelectQuery>();
			BuildParentsInfo(upToContext.SelectQuery, parentInfo);

			var withColumns =
				expression.Transform(
					(builder: this, upToContext, parentInfo),
					static (context, expr) =>
					{
						if (expr is SqlPlaceholderExpression placeholder && !ReferenceEquals(context.upToContext.SelectQuery, placeholder.SelectQuery))
						{
							do
							{
								if (placeholder.SelectQuery == null)
									break;

								if (ReferenceEquals(context.upToContext.SelectQuery, placeholder.SelectQuery))
									break;

								if (!GetParentQuery(context.parentInfo, placeholder.SelectQuery, out var parentQuery))
									break;

								placeholder = context.builder.MakeColumn(parentQuery, placeholder);


							} while (true);

							return placeholder;
						}

						return expr;
					});

			return withColumns;
		}

		public Expression ToColumns(IBuildContext rootContext, Expression expression)
		{
			var parentInfo = new Dictionary<SelectQuery, SelectQuery>();
			BuildParentsInfo(rootContext.SelectQuery, parentInfo);

			var withColumns =
				expression.Transform(
					(builder: this, parentInfo, rootQuery: rootContext.SelectQuery),
					static (context, expr) =>
					{
						if (expr is SqlPlaceholderExpression { SelectQuery: { } } placeholder)
						{
							do
							{
								if (placeholder.SelectQuery == null)
									break;

								if (ReferenceEquals(placeholder.SelectQuery, context.rootQuery))
								{
									placeholder = context.builder.MakeColumn(null, placeholder);
									break;
								}

								if (!GetParentQuery(context.parentInfo, placeholder.SelectQuery, out var parentQuery))
								{
									// Handling OUTPUT cases
									//
									placeholder = context.builder.MakeColumn(null, placeholder);
									break;
								}

								placeholder = context.builder.MakeColumn(parentQuery, placeholder);

							} while (true);

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

		public SqlPlaceholderExpression? TryConvertToSqlPlaceholder(IBuildContext context, Expression expression, ProjectFlags flags, ColumnDescriptor? columnDescriptor = null)
		{
			flags |= ProjectFlags.SQL;
			flags &= ~ProjectFlags.Expression;

			//Just test that we can convert
			var converted = ConvertToSqlExpr(context, expression, flags | ProjectFlags.Test, columnDescriptor: columnDescriptor);
			if (converted is not SqlPlaceholderExpression)
				return null;

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				//Test conversion success, do it again
				converted = ConvertToSqlExpr(context, expression, flags, columnDescriptor: columnDescriptor);
				if (converted is not SqlPlaceholderExpression)
					return null;
			}

			return (SqlPlaceholderExpression)converted;
		}

		public static SqlErrorExpression CreateSqlError(IBuildContext? context, Expression expression)
		{
			return new SqlErrorExpression(context, expression);
		}

		public Expression BuildSqlExpression(IBuildContext context, Expression expression, ProjectFlags flags, string? alias = null)
		{
			var result = expression.Transform(
				(builder: this, context, flags, alias),
				static (context, expr) =>
				{
					// Shortcut: if expression can be compiled we can live it as is but inject accessors 
					//
					if (context.flags.IsExpression()             &&
					    expr.NodeType != ExpressionType.New      &&
					    expr.NodeType != ExpressionType.Constant &&
					    expr.NodeType != ExpressionType.Default  &&
					    expr is not DefaultValueExpression       &&
					    context.builder.CanBeCompiled(expr, false))
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

					switch (expr.NodeType)
					{
						case ExpressionType.Convert:
						case ExpressionType.ConvertChecked:
						{
							if (expr.Type == typeof(object))
								break;

							var cex = (UnaryExpression)expr;

							context.builder._convertedExpressions.Add(cex.Operand, cex);

							var saveBlockDisable = context.builder.IsBlockDisable;
							context.builder.IsBlockDisable = true;
							var newOperand = context.builder.BuildSqlExpression(context.context,
								cex.Operand, context.flags);
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

							var newExpr = context.builder.ExposeExpression(ma);

							if (!ReferenceEquals(newExpr, ma))
								return new TransformInfo(newExpr, false, true);

							if (ma.Member.IsNullableValueMember())
								break;

							newExpr = context.builder.MakeExpression(context.context, ma, context.flags);

							if (!ReferenceEquals(newExpr, ma))
								return new TransformInfo(newExpr, false, true);

							break;
						}

						case ExpressionType.Call:
						{
							var newExpr = context.builder.MakeExpression(context.context, expr, context.flags);

							if (!ReferenceEquals(newExpr, expr))
							{
								return new TransformInfo(newExpr, false, true);
							}

							var ce = (MethodCallExpression)expr;

							if (ce.IsSameGenericMethod(Methods.LinqToDB.SqlExt.Alias))
							{
								var withAlias = context.builder.BuildSqlExpression(context.context,
									ce.Arguments[0],
									context.flags, context.alias ?? ce.Arguments[1].EvaluateExpression<string>());
								return new TransformInfo(withAlias);
							}

							break;
						}

						case ExpressionType.Extension:
						{
							if (expr is ContextRefExpression contextRef)
							{
								var buildExpr = context.builder.MakeExpression(contextRef.BuildContext, contextRef,
									context.flags);

								return new TransformInfo(buildExpr, false, true);
							}

							if (expr is SqlGenericParamAccessExpression paramAccessExpression)
							{
								return new TransformInfo(
									context.builder.MakeExpression(context.context, paramAccessExpression,
										context.flags), false, true);
							}

							/*if (expr is SqlGenericConstructorExpression genericConstructor)
							{
								// trying to convert all properties to the SQL
								if (context.flags.IsExpression())
								{
									if (genericConstructor.Assignments.Count > 0)
									{
										var ed = context.builder.MappingSchema.GetEntityDescriptor(genericConstructor.Type);
										
										List<SqlGenericConstructorExpression.Assignment>? newAssignments = null;

										for (var index = 0; index < genericConstructor.Assignments.Count; index++)
										{
											var assignment = genericConstructor.Assignments[index];

											if (assignment.Expression is not SqlPlaceholderExpression 
											    && context.builder.MappingSchema.IsScalarType(assignment.MemberInfo.GetMemberType()))
											{
												var columnDescriptor = ed.Columns.FirstOrDefault(c =>
													MemberInfoComparer.Instance.Equals(c.MemberInfo,
														assignment.MemberInfo));
												if (columnDescriptor != null)
												{
													var translated =
														context.builder.ConvertToSqlExpr(context.context,
															assignment.Expression,
															context.flags, columnDescriptor: columnDescriptor,
															alias: assignment.MemberInfo.Name);

													if (translated is SqlPlaceholderExpression)
													{
														if (newAssignments == null)
														{
															newAssignments = new List<SqlGenericConstructorExpression.Assignment>(genericConstructor.Assignments.Take(index));
														}

														newAssignments.Add(assignment.WithExpression(translated));
														continue;
													}
												}
											}

											newAssignments?.Add(assignment);
										}

										if (newAssignments != null)
										{
											var newConstructor = genericConstructor.ReplaceAssignments(newAssignments);
											return new TransformInfo(newConstructor, false, true);
										}
									}
									/*
									var newParameters = new List<SqlGenericConstructorExpression.Assignment>();

									newConstructor = newConstructor.ReplaceParameters(genericConstructor.Parameters.Select(p =>
										p.WithExpression(ConvertToSqlExpr(context, p.Expression, flags, unwrap, columnDescriptor,
											isPureExpression, p.MemberInfo?.Name ?? alias))).ToList());
											#1#
								}

							}*/

							return new TransformInfo(expr);
						}

						case ExpressionType.TypeIs:
						{
							if (context.flags.IsExpression())
							{
								var test = context.builder.MakeExpression(context.context, expr,
									context.flags);

								if (!HasError(test))
								{
									return new TransformInfo(test, false, true);
								}
							}

							break;
						}

						case ExpressionType.Conditional:
						{
							break;
						}


						/*
						case ExpressionType.Conditional:
						{
							if (context.flags.IsExpression())
							{
								// Try to convert condition to the SQL
								var asSQL = context.builder.ConvertToSqlExpr(context.context, expr,
									context.flags);

								if (asSQL is SqlPlaceholderExpression)
								{
									return new TransformInfo(asSQL);
								}
							}

							break;
						}
						*/

						case ExpressionType.Parameter:
						{
							return new TransformInfo(expr);
						}

					}

					expr = context.builder.HandleExtension(context.context, expr, context.flags);
					return new TransformInfo(expr);
				});

			return result;
		}

		public static bool HasError(Expression expression)
		{
			return null != expression.Find(0, (_, e) => e is SqlErrorExpression);
		}

		public Expression HandleExtension(IBuildContext context, Expression expr, ProjectFlags flags)
		{

			// Handling ExpressionAttribute
			//
			if (expr.NodeType == ExpressionType.Call || expr.NodeType == ExpressionType.MemberAccess)
			{
				MemberInfo memberInfo;
				if (expr.NodeType == ExpressionType.Call)
				{
					memberInfo = ((MethodCallExpression)expr).Method;
				}
				else
				{
					memberInfo = ((MemberExpression)expr).Member;
				}

				var attr = memberInfo.GetExpressionAttribute(MappingSchema);

				if (attr != null && (flags.HasFlag(ProjectFlags.Expression) || attr.ServerSideOnly))
				{
					var converted = attr.GetExpression((builder: this, context),
						DataContext,
						context.SelectQuery, expr,
						static (context, e, descriptor) =>
							context.builder.ConvertToExtensionSql(context.context, e, descriptor));

					if (converted != null)
					{
						var newExpr = CreatePlaceholder(context.SelectQuery, converted, expr);
						return newExpr;
					}
				}
			}

			return expr;
		}

		public Expression FinalizeConstructors(IBuildContext context, Expression expression)
		{
			do
			{
				expression = BuildSqlExpression(context, expression, ProjectFlags.Expression);

				expression = OptimizationContext.OptimizeExpressionTree(expression, true);

				var deduplicated = Deduplicate(expression, true);
				deduplicated = Deduplicate(deduplicated, false);

				var reconstructed = deduplicated.Transform((builder: this, context), (ctx, e) =>
				{
					if (e is SqlGenericConstructorExpression generic)
					{
						return ctx.builder.TryConstruct(ctx.builder.MappingSchema, generic, ctx.context,
							ProjectFlags.Expression);
					}

					return e;
				});

				if (ReferenceEquals(reconstructed, deduplicated))
					return reconstructed;

				expression = reconstructed;

			} while (true);
		}

		sealed class SubQueryContextInfo
		{
			public Expression     SequenceExpression = null!;
			public IBuildContext? Context;
			public Expression?    Expression;
		}

		public Expression CorrectRoot(IBuildContext? currentContext, Expression expr)
		{
			if (expr is MethodCallExpression mc && mc.IsQueryable())
			{
				var firstArg = CorrectRoot(currentContext, mc.Arguments[0]);
				if (!ReferenceEquals(firstArg, mc.Arguments[0]))
				{
					var args = mc.Arguments.ToArray();
					args[0] = firstArg;
					return mc.Update(null, args);
				}
			}
			else if (expr is ContextRefExpression { BuildContext: ScopeContext sc })
			{
				return CorrectRoot(sc.Context, new ContextRefExpression(expr.Type, sc.Context));
			}
			else if (expr is ContextRefExpression { BuildContext: DefaultIfEmptyBuilder.DefaultIfEmptyContext di })
			{
				return CorrectRoot(di.Sequence, new ContextRefExpression(expr.Type, di.Sequence));
			}
			else
				expr = MakeExpression(currentContext, expr, ProjectFlags.Root);

			return expr;
		}

		public ContextRefExpression? GetRootContext(IBuildContext? currentContext, Expression? expression, bool isAggregation)
		{
			if (expression == null)
				return null;

			expression = MakeExpression(currentContext, expression, isAggregation ? ProjectFlags.AggregationRoot : ProjectFlags.Root);

			if (expression is MemberExpression memberExpression)
			{
				expression = GetRootContext(currentContext, memberExpression.Expression, isAggregation);
			}

			if (expression is MethodCallExpression mc && mc.IsQueryable())
			{
				expression = GetRootContext(currentContext, mc.Arguments[0], isAggregation);
			}

			return expression as ContextRefExpression;
		}

		List<SubQueryContextInfo>? _buildContextCache;

		SubQueryContextInfo GetSubQueryContext(IBuildContext context, Expression expr, bool isTest)
		{
			var testExpression = CorrectRoot(context, expr);

			_buildContextCache ??= new List<SubQueryContextInfo>();

			foreach (var item in _buildContextCache)
			{
				if (testExpression.EqualsTo(item.SequenceExpression, OptimizationContext.GetSimpleEqualsToContext(false)))
					return item;
			}

			var rootQuery = GetRootContext(context, testExpression, false);

			if (rootQuery != null)
			{
				context = rootQuery.BuildContext;
			}
			else
			{
				var contextRef = new ContextRefExpression(typeof(object), context);
				rootQuery = GetRootContext(context, contextRef, false);
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

		public Expression? TryGetSubQueryExpression(IBuildContext context, Expression expr, string? alias, bool isTest)
		{
			var unwrapped = expr.Unwrap();
			var info = GetSubQueryContext(context, unwrapped, isTest);

			if (info.Context == null)
				return null;

			var resultExpr = (Expression)new ContextRefExpression(unwrapped.Type, info.Context);

			return resultExpr;
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
						return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeCompiled(expr, false);
					}

				case ExpressionType.Call:
					{
						var pi = (MethodCallExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

						if (l != null)
							return GetVisitor(enforceServerSide).Find(l.Body.Unwrap()) != null;

						var attr = pi.Method.GetExpressionAttribute(MappingSchema);
						return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeCompiled(expr, false);
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

		public Expression ToReadExpression(NullabilityContext nullability, Expression expression)
		{
			var toRead = expression.Transform(e =>
			{
				if (e is SqlPlaceholderExpression placeholder)
				{
					if (placeholder.Sql == null)
						throw new InvalidOperationException();
					if (placeholder.Index == null)
						throw new InvalidOperationException();

					var columnDescriptor = QueryHelper.GetColumnDescriptor(placeholder.Sql);

					var valueType = columnDescriptor?.GetDbDataType(true).SystemType 
					                ?? placeholder.Sql.SystemType 
					                ?? placeholder.Type;

					var canBeNull = nullability.CanBeNull(placeholder.Sql);

					if (canBeNull && valueType != placeholder.Type && valueType.IsValueType && !valueType.IsNullable())
					{
						valueType = valueType.AsNullable();
					}

					var readerExpression = (Expression)new ConvertFromDataReaderExpression(valueType, placeholder.Index.Value,
						columnDescriptor?.ValueConverter, DataReaderParam, canBeNull);

					if (placeholder.Type != readerExpression.Type)
					{
						readerExpression = Expression.Convert(readerExpression, placeholder.Type);
					}

					return readerExpression;
				}

				if (e is SqlReaderIsNullExpression isNullExpression)
				{
					if (isNullExpression.Placeholder.Index == null)
						throw new InvalidOperationException();

					Expression nullCheck = Expression.Call(
						DataReaderParam,
						ReflectionHelper.DataReader.IsDBNull,
						ExpressionInstances.Int32Array(isNullExpression.Placeholder.Index.Value));

					if (isNullExpression.IsNot)
						nullCheck = Expression.Not(nullCheck);

					return nullCheck;
				}

				return e;
			});

			return toRead;
		}

		public Expression<Func<IQueryRunner,IDataContext,DbDataReader,Expression,object?[]?,object?[]?,T>> BuildMapper<T>(SelectQuery query, Expression expr)
		{
			var type = typeof(T);

			if (expr.Type != type)
				expr = Expression.Convert(expr, type);

			var readExpr = ToReadExpression(new NullabilityContext(query), expr);

			var mapper = Expression.Lambda<Func<IQueryRunner,IDataContext,DbDataReader,Expression,object?[]?,object?[]?,T>>(
				BuildBlock(readExpr), new[]
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

		public Expression? AssociationRoot;
		public Stack<Tuple<AccessorMember, IBuildContext, List<LoadWithInfo[]>?>>? AssociationPath;

		#endregion

	}
}
