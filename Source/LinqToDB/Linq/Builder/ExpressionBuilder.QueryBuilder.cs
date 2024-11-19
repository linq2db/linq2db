using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Common;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;
	using Reflection;

	partial class ExpressionBuilder
	{
		#region BuildExpression

		class FinalizeExpressionVisitor : ExpressionVisitorBase
		{
			HashSet<Expression>?                                                           _visited;
			HashSet<Expression>?                                                           _duplicates;
			Dictionary<Expression, Expression>?                                            _constructed;
			Dictionary<Expression, (ParameterExpression variable, Expression assignment)>? _constructedAssignments;

			ExpressionGenerator _generator = default!;
			IBuildContext       _context   = default!;
			bool                _constructRun;

			internal override Expression VisitSqlReaderIsNullExpression(SqlReaderIsNullExpression node)
			{
				return node;
			}

			internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
			{
				return node;
			}

			public override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
			{
				if (!_constructRun)
				{
					_visited ??= new(ExpressionEqualityComparer.Instance);
					if (!_visited.Add(node))
					{
						_duplicates ??= new(ExpressionEqualityComparer.Instance);
						_duplicates.Add(node);
					}
					else
					{
						var local = ConstructObject(node);
						local = TranslateExpression(local);

						// collecting recursively
						var collect = Visit(local);
					}

					return node;
				}

				_constructed ??= new(ExpressionEqualityComparer.Instance);
				if (!_constructed.TryGetValue(node, out var constructed))
				{
					constructed = ConstructObject(node);
					constructed = TranslateExpression(constructed);
					constructed = Visit(constructed);

					_constructed.Add(node, constructed);
				}

				if (_duplicates != null && _duplicates.Contains(node))
				{
					_constructedAssignments ??= new(ExpressionEqualityComparer.Instance);
					if (!_constructedAssignments.TryGetValue(node, out var assignmentPair))
					{
						var variable = _generator.AssignToVariable(Expression.Default(node.Type));
						var assign   = Expression.Assign(variable, Expression.Coalesce(variable, constructed));
						assignmentPair = (variable, assign);
						_constructedAssignments.Add(node, assignmentPair);
					}

					return assignmentPair.assignment;
				}

				return constructed;
			}

			Expression TranslateExpression(Expression local)
			{
				return _context.Builder.BuildSqlExpression(_context, local, ProjectFlags.Expression, buildFlags: BuildFlags.ForceDefaultIfEmpty);
			}

			Expression ConstructObject(SqlGenericConstructorExpression node)
			{
				return _context.Builder.Construct(_context.Builder.MappingSchema, node, ProjectFlags.Expression);
			}

			public Expression Finalize(Expression expression, IBuildContext context, ExpressionGenerator generator)
			{
				_visited                = new HashSet<Expression>(ExpressionEqualityComparer.Instance);
				_duplicates             = default;
				_constructed            = default;
				_constructedAssignments = default;
				_generator              = generator;
				_context                = context;

				var result = expression;
				while (true)
				{
					_visited.Clear();

					_constructRun = false;
					Visit(result);

					_constructRun = true;
					var current = result;
					result = Visit(current);

					result = TranslateExpression(result);

					if (ReferenceEquals(current, result))
						break;
				}

				return result;
			}

			public override void Cleanup()
			{
				base.Cleanup();

				_visited   = default!;
				_generator = default!;
				_context   = default!;

				_duplicates             = default;
				_constructed            = default;
				_constructedAssignments = default;
			}
		}

		Expression FinalizeProjection<T>(
			Query<T>            query,
			IBuildContext       context,
			Expression          expression,
			ParameterExpression queryParameter,
			ref List<Preamble>? preambles,
			Expression[]        previousKeys)
		{
			// Quick shortcut for non-queries
			if (expression.NodeType == ExpressionType.Default)
				return expression;

			// convert all missed references
			
			var postProcessed = FinalizeConstructors(context, expression, true);

			// process eager loading queries
			var correctedEager = CompleteEagerLoadingExpressions(postProcessed, context, queryParameter, ref preambles, previousKeys);

			if (SequenceHelper.HasError(correctedEager))
				return correctedEager;

			if (!ExpressionEqualityComparer.Instance.Equals(correctedEager, postProcessed))
			{
				// convert all missed references
				postProcessed = FinalizeConstructors(context, correctedEager, false);
			}

			var withColumns = ToColumns(context, postProcessed);
			return withColumns;
		}

		static bool GetParentQuery(Dictionary<SelectQuery, SelectQuery> parentInfo, SelectQuery currentQuery, [MaybeNullWhen(false)] out SelectQuery? parentQuery)
		{
			return parentInfo.TryGetValue(currentQuery, out parentQuery);
		}

		public class ParentInfo
		{
			Dictionary<SelectQuery, SelectQuery>? _info;

			public bool GetParentQuery(SelectQuery rootQuery, SelectQuery currentQuery, [MaybeNullWhen(false)] out SelectQuery? parentQuery)
			{
				if (_info == null)
				{
					_info = new(Utils.ObjectReferenceEqualityComparer<SelectQuery>.Default);
					BuildParentsInfo(rootQuery, _info);
				}
				return _info.TryGetValue(currentQuery, out parentQuery);
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

			public void Cleanup()
			{
				_info = null;
			}
		}

		public TExpression UpdateNesting<TExpression>(IBuildContext upToContext, TExpression expression)
			where TExpression : Expression
		{
			var corrected = UpdateNesting(upToContext.SelectQuery, expression);
			
			return corrected;
		}

		public TExpression UpdateNesting<TExpression>(SelectQuery upToQuery, TExpression expression)
			where TExpression : Expression
		{
			using var parentInfo = ParentInfoPool.Allocate();

			var corrected = UpdateNestingInternal(upToQuery, expression, parentInfo.Value);

			return corrected;
		}

		TExpression UpdateNestingInternal<TExpression>(SelectQuery upToQuery, TExpression expression, ParentInfo parentInfo)
			where TExpression : Expression
		{
			// short path
			if (expression is SqlPlaceholderExpression currentPlaceholder && currentPlaceholder.SelectQuery == upToQuery)
				return expression;

			var withColumns =
				expression.Transform(
					(builder: this, upToQuery, parentInfo),
					static (context, expr) =>
					{
						if (expr is SqlPlaceholderExpression placeholder && !ReferenceEquals(context.upToQuery, placeholder.SelectQuery))
						{
							do
							{
								if (placeholder.SelectQuery == null)
									break;

								if (ReferenceEquals(context.upToQuery, placeholder.SelectQuery))
									break;

								if (!context.parentInfo.GetParentQuery(context.upToQuery, placeholder.SelectQuery, out var parentQuery))
									break;

								placeholder = context.builder.MakeColumn(parentQuery, placeholder);
							} while (true);

							return placeholder;
						}

						return expr;
					});

			return (TExpression)withColumns;
		}

		public Expression ToColumns(IBuildContext rootContext, Expression expression)
		{
			return ToColumns(rootContext.SelectQuery, expression);
		}

		public Expression ToColumns(SelectQuery rootQuery, Expression expression)
		{
			using var parentInfo = ParentInfoPool.Allocate();

			var withColumns =
				expression.Transform(
					(builder: this, parentInfo: parentInfo.Value, rootQuery),
					static (context, expr) =>
					{
						if (expr is SqlPlaceholderExpression { SelectQuery: { } } placeholder)
						{
							do
							{
								if (placeholder.SelectQuery == null)
									break;

								if (placeholder.Sql is SqlRowExpression)
								{
									throw new LinqToDBException("Sql.Row(...) cannot be top level expression.");
								}

								if (ReferenceEquals(placeholder.SelectQuery, context.rootQuery))
								{
									placeholder = context.builder.MakeColumn(null, placeholder);
									break;
								}

								if (!context.parentInfo.GetParentQuery(context.rootQuery, placeholder.SelectQuery, out var parentQuery))
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

		public bool TryConvertToSql(IBuildContext? context, Expression expression, ProjectFlags flags,
			ColumnDescriptor? columnDescriptor, [NotNullWhen(true)] out ISqlExpression? sqlExpression,
			[NotNullWhen(false)] out SqlErrorExpression? error)
		{
			flags = flags & ~ProjectFlags.Expression | ProjectFlags.SQL;

			sqlExpression = null;

			//Just test that we can convert
			var actual = ConvertToSqlExpr(context, expression, flags | ProjectFlags.Test, unwrap: false, columnDescriptor : columnDescriptor);
			if (actual is not SqlPlaceholderExpression placeholderTest)
			{
				error = SqlErrorExpression.EnsureError(context, expression);
				return false;
			};

			sqlExpression = placeholderTest.Sql;

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				sqlExpression = null;
				//Test conversion success, do it again
				var newActual = ConvertToSqlExpr(context, expression, flags, columnDescriptor : columnDescriptor);
				if (newActual is not SqlPlaceholderExpression placeholder)
				{
					error = SqlErrorExpression.EnsureError(context, expression);
					return false;
				}

				sqlExpression = placeholder.Sql;
			}

			error = null;
			return true;
		}

		public SqlPlaceholderExpression? TryConvertToSqlPlaceholder(IBuildContext context, Expression expression, ProjectFlags flags, ColumnDescriptor? columnDescriptor = null)
		{
			flags |= ProjectFlags.SQL;
			flags &= ~ProjectFlags.Expression;

			//Just test that we can convert
			var converted = ConvertToSqlExpr(context, expression, flags | ProjectFlags.Test, columnDescriptor : columnDescriptor);
			if (converted is not SqlPlaceholderExpression)
				return null;

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				//Test conversion success, do it again
				converted = ConvertToSqlExpr(context, expression, flags, columnDescriptor : columnDescriptor);
				if (converted is not SqlPlaceholderExpression)
					return null;
			}

			return (SqlPlaceholderExpression)converted;
		}

		public static SqlErrorExpression CreateSqlError(IBuildContext? context, Expression expression)
		{
			return new SqlErrorExpression(context, expression);
		}

		public static bool HasError(Expression expression)
		{
			return null != expression.Find(0, (_, e) => e is SqlErrorExpression);
		}

		public Expression ConvertExtension(Sql.ExpressionAttribute attr, IBuildContext context, Expression expr, ProjectFlags flags)
		{
			var rootContext     = context;
			var rootSelectQuery = context.SelectQuery;

			var root = GetRootContext(context.Parent, new ContextRefExpression(context.ElementType, context), true);
			if (root != null)
			{
				rootContext = root.BuildContext;
			}

			if (rootContext is GroupByBuilder.GroupByContext groupBy)
			{
				rootSelectQuery = groupBy.SubQuery.SelectQuery;
			}

			var transformed = attr.GetExpression((builder: this, context: rootContext, flags),
				DataContext,
				this,
				rootSelectQuery, expr,
				static (context, e, descriptor, inline) =>
					context.builder.ConvertToExtensionSql(context.context, context.flags, e, descriptor, inline));

			if (transformed is SqlPlaceholderExpression placeholder)
			{
				RegisterExtensionAccessors(expr);

				placeholder = placeholder.WithSql(PosProcessCustomExpression(expr, placeholder.Sql, NullabilityContext.GetContext(placeholder.SelectQuery)));

				return placeholder.WithPath(expr);
			}

			if (attr.ServerSideOnly)
			{
				if (transformed is SqlErrorExpression errorExpr)
					return SqlErrorExpression.EnsureError(errorExpr, expr.Type);
				return SqlErrorExpression.EnsureError(expr, expr.Type);
			}

			return expr;
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

				if (attr != null)
				{
					return ConvertExtension(attr, context, expr, flags);
				}
			}

			return expr;
		}

		public void RegisterExtensionAccessors(Expression expression)
		{
			void Register(Expression expr)
			{
				if (!expr.Type.IsScalar() && CanBeCompiled(expr, true))
					ParametersContext.ApplyAccessors(expr, true);

			}

			// Extensions may have instance reference. Try to register them as parameterized to disallow caching objects in Expression Tree
			//
			if (expression is MemberExpression { Expression: not null } me)
			{
				Register(me.Expression);
			}
			else if (expression is MethodCallExpression mc)
			{
				if (mc.Object != null)
				{
					Register(mc.Object);
				}

				var dependentParameters = SqlQueryDependentAttributeHelper.GetQueryDependentAttributes(mc.Method);
				for (var index = 0; index < mc.Arguments.Count; index++)
				{
					if (dependentParameters != null && dependentParameters[index] != null)
						continue;

					var arg = mc.Arguments[index];
					Register(arg);
				}
			}
		}

		public Expression FinalizeConstructors(IBuildContext context, Expression inputExpression, bool deduplicate)
		{
			using var finalizeVisitor = _finalizeVisitorPool.Allocate();
			var generator       = new ExpressionGenerator();

			// Runs SqlGenericConstructorExpression deduplication and generating actual initializers
			var expression = finalizeVisitor.Value.Finalize(inputExpression, context, generator);

			generator.AddExpression(expression);

			var result = generator.Build();
			return result;
		}

		sealed class SubQueryContextInfo
		{
			public Expression     SequenceExpression = null!;
			public string?        ErrorMessage;
			public IBuildContext? Context;
			public bool           IsSequence;
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
			else if (expr is ContextRefExpression { BuildContext: DefaultIfEmptyBuilder.DefaultIfEmptyContext di })
			{
				return CorrectRoot(di.Sequence, new ContextRefExpression(expr.Type, di.Sequence));
			}

			var newExpr = MakeExpression(currentContext, expr, ProjectFlags.Traverse);
			if (!ExpressionEqualityComparer.Instance.Equals(newExpr, expr))
			{
				newExpr = CorrectRoot(currentContext, newExpr);
			}

			return newExpr;
		}

		public ContextRefExpression? GetRootContext(IBuildContext? currentContext, Expression? expression, bool isAggregation)
		{
			if (expression == null)
				return null;

			if (expression is MemberExpression memberExpression)
			{
				expression = GetRootContext(currentContext, memberExpression.Expression, isAggregation);
			}
			if (expression is MethodCallExpression methodCallExpression && methodCallExpression.IsQueryable())
			{
				if (isAggregation)
					expression = GetRootContext(currentContext, methodCallExpression.Arguments[0], isAggregation);
			}
			else if (expression is ContextRefExpression)
			{
				var newExpression = MakeExpression(currentContext, expression, isAggregation ? ProjectFlags.AggregationRoot : ProjectFlags.Root);

				if (!ExpressionEqualityComparer.Instance.Equals(newExpression, expression))
					expression = GetRootContext(currentContext, newExpression, isAggregation);
			}

			return expression as ContextRefExpression;
		}

		class SubqueryCacheKey
		{
			public SubqueryCacheKey(SelectQuery selectQuery, Expression expression)
			{
				SelectQuery = selectQuery;
				Expression  = expression;
			}

			public SelectQuery SelectQuery { get; }
			public Expression Expression { get; }

			sealed class BuildContextExpressionEqualityComparer : IEqualityComparer<SubqueryCacheKey>
			{
				public bool Equals(SubqueryCacheKey? x, SubqueryCacheKey? y)
				{
					if (ReferenceEquals(x, y))
					{
						return true;
					}

					if (ReferenceEquals(x, null))
					{
						return false;
					}

					if (ReferenceEquals(y, null))
					{
						return false;
					}

					if (x.GetType() != y.GetType())
					{
						return false;
					}

					return x.SelectQuery.Equals(y.SelectQuery, SqlExpression.DefaultComparer) && ExpressionEqualityComparer.Instance.Equals(x.Expression, y.Expression);
				}

				public int GetHashCode(SubqueryCacheKey obj)
				{
					unchecked
					{
						var hashCode = obj.SelectQuery.SourceID.GetHashCode();
						hashCode = (hashCode * 397) ^ ExpressionEqualityComparer.Instance.GetHashCode(obj.Expression);
						return hashCode;
					}
				}
			}

			public static IEqualityComparer<SubqueryCacheKey> Comparer { get; } = new BuildContextExpressionEqualityComparer();
		}

		Dictionary<SubqueryCacheKey, SubQueryContextInfo>? _buildContextCache;
		Dictionary<SubqueryCacheKey, SubQueryContextInfo>? _testBuildContextCache;

		SubQueryContextInfo GetSubQueryContext(IBuildContext inContext, ref IBuildContext context, Expression expr, ProjectFlags flags)
		{
			context   = inContext;
			var testExpression = CorrectRoot(context, expr);
			var cacheKey       = new SubqueryCacheKey(context.SelectQuery, testExpression);

			var shouldCache = flags.IsSql() || flags.IsExpression() || flags.IsExtractProjection() || flags.IsRoot();

			if (shouldCache && _buildContextCache?.TryGetValue(cacheKey, out var item) == true)
				return item;

			if (flags.IsTest())
			{
				if (_testBuildContextCache?.TryGetValue(cacheKey, out var testItem) == true)
					return testItem;
			}

			var rootQuery = GetRootContext(context, testExpression, false);
			rootQuery ??= GetRootContext(context, expr, false);

			if (rootQuery != null)
			{
				context = rootQuery.BuildContext;
			}

			var correctedForBuild = testExpression;
			var ctx               = GetSubQuery(context, correctedForBuild, flags, out var isSequence, out var errorMessage);

			var info = new SubQueryContextInfo { SequenceExpression = testExpression, Context = ctx, IsSequence = isSequence, ErrorMessage = errorMessage};

			if (shouldCache)
			{
				if (flags.IsTest())
				{
					_testBuildContextCache           ??= new(SubqueryCacheKey.Comparer);
					_testBuildContextCache[cacheKey] =   info;
				}
				else
				{
					_buildContextCache           ??= new(SubqueryCacheKey.Comparer);
					_buildContextCache[cacheKey] =   info;
				}
			}

			return info;
		}

		public static bool IsSingleElementContext(IBuildContext context)
		{
			return context is FirstSingleBuilder.FirstSingleContext;
		}

		Expression TranslateDetails(IBuildContext context, Expression expr, ProjectFlags flags)
		{
			using var visitor = _buildVisitorPool.Allocate();
			var newExpr = visitor.Value.Build(context, expr, flags, BuildFlags.ForceAssignments | BuildFlags.IgnoreRoot);
			return newExpr;
		}

		static string [] _singleElementMethods =
		{
			nameof(Enumerable.FirstOrDefault),
			nameof(Enumerable.First),
			nameof(Enumerable.Single),
			nameof(Enumerable.SingleOrDefault),
		};

		public Expression PrepareSubqueryExpression(Expression expr)
		{
			var newExpr = expr;

			if (expr.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expr;
				if (mc.IsQueryable(_singleElementMethods))
				{
					if (mc.Arguments is [var a0, var a1])
					{
						Expression whereMethod;

						var typeArguments = mc.Method.GetGenericArguments();
						if (mc.Method.DeclaringType == typeof(Queryable))
						{
							var methodInfo = Methods.Queryable.Where.MakeGenericMethod(typeArguments);
							whereMethod = Expression.Call(methodInfo, a0, a1);
							var limitCall = Expression.Call(typeof(Queryable), mc.Method.Name, typeArguments, whereMethod);

							newExpr = limitCall;
						}
						else
						{
							var methodInfo = Methods.Enumerable.Where.MakeGenericMethod(typeArguments);
							whereMethod = Expression.Call(methodInfo, a0, a1);
							var limitCall = Expression.Call(typeof(Enumerable), mc.Method.Name, typeArguments, whereMethod);

							newExpr = limitCall;
						}
					}
				}
			}

			return newExpr;
		}

		public Expression? TryGetSubQueryExpression(IBuildContext context, Expression expr, string? alias, ProjectFlags flags, out bool isSequence, out Expression? corrected)
		{
			isSequence = false;
			corrected  = null;

			if (flags.IsTraverse())
				return null;

			var unwrapped = expr.Unwrap();

			if (unwrapped is SqlErrorExpression)
				return expr;

			if (unwrapped is BinaryExpression or ConditionalExpression or DefaultExpression or DefaultValueExpression or SqlDefaultIfEmptyExpression)
				return null;

			if (unwrapped is SqlGenericConstructorExpression or ConstantExpression or SqlEagerLoadExpression)
				return null;

			if (unwrapped is ContextRefExpression contextRef && contextRef.BuildContext.ElementType == expr.Type)
				return null;

			if (SequenceHelper.IsSpecialProperty(unwrapped, out _, out _))
				return null;

			if (!flags.IsSubquery())
			{
				if (CanBeCompiled(unwrapped, true))
					return null;

				if (unwrapped is MemberInitExpression or NewExpression or NewArrayExpression)
				{
					var withDetails = TranslateDetails(context, unwrapped, flags);
					if (CanBeCompiled(withDetails, true))
						return null;
				}
			}

			if (unwrapped is MemberExpression me)
			{
				var attr = me.Member.GetExpressionAttribute(MappingSchema);
				if (attr != null)
					return null;
			}

			var info = GetSubQueryContext(context, ref context, unwrapped, flags);
			isSequence = info.IsSequence;

			if (info.Context == null)
			{
				if (isSequence)
				{
					if (flags.IsExpression())
					{
						// Trying to relax eager for First[OrDefault](predicate)
						var prepared = PrepareSubqueryExpression(expr);
						if (!ReferenceEquals(prepared, expr))
						{
							corrected = prepared;
						}

						return null;
					}

					return new SqlErrorExpression(expr, info.ErrorMessage, expr.Type);
				}

				return null;
			}

			if (!IsSingleElementContext(info.Context) && expr.Type.IsEnumerableType(info.Context.ElementType) && !flags.IsExtractProjection())
			{
				var eager = (Expression)new SqlEagerLoadExpression(unwrapped);
				eager = SqlAdjustTypeExpression.AdjustType(eager, expr.Type, MappingSchema);

				return eager;
			}

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

		public Expression ToReadExpression(
			ExpressionGenerator expressionGenerator,
			NullabilityContext  nullability,
			Expression          expression)
		{
			Expression? rowCounter = null;

			var simplified = expression.Transform(e =>
			{
				if (e.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked && e.Type != typeof(object))
				{
					if (((UnaryExpression)e).Operand is SqlPlaceholderExpression convertPlaceholder)
					{
						return convertPlaceholder.WithType(e.Type);
					}
				}

				return e;
			});

			var toRead = simplified.Transform(e =>
			{
				if (e is SqlPlaceholderExpression placeholder)
				{
					if (placeholder.Sql == null)
						throw new InvalidOperationException();
					if (placeholder.Index == null)
						throw new InvalidOperationException();

					var columnDescriptor = QueryHelper.GetColumnDescriptor(placeholder.Sql);

					var valueType = columnDescriptor?.GetDbDataType(true).SystemType
					                ?? placeholder.Type;

					var canBeNull = nullability.CanBeNull(placeholder.Sql) || placeholder.Type.IsNullable();

					if (canBeNull && valueType != placeholder.Type && valueType.IsValueType && !valueType.IsNullable())
					{
						valueType = valueType.AsNullable();
					}

					if (placeholder.Type != valueType && valueType.IsNullable() && placeholder.Type == valueType.ToNullableUnderlying())
					{
						// let ConvertFromDataReaderExpression handle default value
						valueType = placeholder.Type;
					}

					var readerExpression = (Expression)new ConvertFromDataReaderExpression(valueType, placeholder.Index.Value,
						columnDescriptor?.ValueConverter, DataReaderParam, canBeNull);

					if (placeholder.Type != readerExpression.Type)
					{
						readerExpression = Expression.Convert(readerExpression, placeholder.Type);
					}

					return new TransformInfo(readerExpression);
				}

				if (e.NodeType == ExpressionType.Equal || e.NodeType == ExpressionType.NotEqual)
				{
					var binary = (BinaryExpression)e;
					if (binary.Left.IsNullValue() && binary.Right is SqlPlaceholderExpression placeholderRight)
					{
						return new TransformInfo(new SqlReaderIsNullExpression(placeholderRight, e.NodeType == ExpressionType.NotEqual), false, true);
					}
					if (binary.Right.IsNullValue() && binary.Left is SqlPlaceholderExpression placeholderLeft)
					{
						return new TransformInfo(new SqlReaderIsNullExpression(placeholderLeft, e.NodeType == ExpressionType.NotEqual), false, true);
					}
				}

				if (e is SqlReaderIsNullExpression isNullExpression)
				{
					if (isNullExpression.Placeholder.Index == null)
						throw new InvalidOperationException();

					Expression nullCheck = Expression.Call(
						DataReaderParam,
						ReflectionHelper.DataReader.IsDBNull,
						// ReSharper disable once CoVariantArrayConversion
						ExpressionInstances.Int32Array(isNullExpression.Placeholder.Index.Value));

					if (isNullExpression.IsNot)
						nullCheck = Expression.Not(nullCheck);

					return new TransformInfo(nullCheck);
				}

				if (e == RowCounterParam)
				{
					if (rowCounter == null)
					{
						rowCounter = e;
						expressionGenerator.AddVariable(RowCounterParam);
						expressionGenerator.AddExpression(Expression.Assign(RowCounterParam,
							Expression.Property(QueryRunnerParam, QueryRunner.RowsCountInfo)));
					}
				}

				return new TransformInfo(e);
			});

			return toRead;
		}

		public Expression<Func<IQueryRunner,IDataContext,DbDataReader,Expression,object?[]?,object?[]?,T>> BuildMapper<T>(SelectQuery query, Expression expr)
		{
			var type = typeof(T);

			if (expr.Type != type)
				expr = Expression.Convert(expr, type);

			var expressionGenerator = new ExpressionGenerator();

			// variable accessed dynamically
			_ = expressionGenerator.AssignToVariable(DataReaderParam, "ldr");

			var readExpr = ToReadExpression(expressionGenerator, new NullabilityContext(query), expr);
			expressionGenerator.AddExpression(readExpr);

			var mappingBody = expressionGenerator.Build();

			var mapper = Expression.Lambda<Func<IQueryRunner,IDataContext,DbDataReader,Expression,object?[]?,object?[]?,T>>(mappingBody,
				QueryRunnerParam,
				ExpressionConstants.DataContextParam,
				DataReaderParam,
				ExpressionParam,
				ParametersParam,
				PreambleParam);

			return mapper;
		}

		#endregion

		#region BuildMultipleQuery

		public Expression? AssociationRoot;
		public Stack<Tuple<AccessorMember, IBuildContext, List<LoadWithInfo[]>?>>? AssociationPath;

		#endregion

	}
}
