using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Common.Internal;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	partial class ExpressionBuilder
	{
		#region BuildExpression

		class DeduplicateVisitor : ExpressionVisitorBase
		{
			List<(ParameterExpression variable, Expression assingment)> _variables = default!;
			(Expression? node, ParameterExpression? variable)           _replacement;
			HashSet<Expression>                                         _visited    = default!;
			List<Expression>                                            _duplicates = default!;

			public Expression Deduplicate(Expression expression, List<(ParameterExpression variable, Expression assingment)> variables)
			{
				_variables = variables;

				_duplicates ??= new();
				_duplicates.Clear();

				var result = expression;
				do
				{
					_replacement = default;
					_visited     = new HashSet<Expression>(ExpressionEqualityComparer.Instance);

					result = Visit(result);

					if (_replacement.node == null)
						break;

					// perform full replacement
					result = Visit(result);
				} while (true);

				return result;
			}

			[return: NotNullIfNotNull(nameof(node))]
			public override Expression? Visit(Expression? node)
			{
				if (node == null) 
					return null;

				if (node.NodeType == ExpressionType.Parameter || node.NodeType == ExpressionType.Call || node.NodeType == ExpressionType.MemberAccess ||
				    node.NodeType == ExpressionType.Assign || node.NodeType == ExpressionType.Constant || node.NodeType == ExpressionType.Default)
				{
					return node;
				}

				if (_replacement.node == null)
				{
					if (null == node.Find(1, (_, e) => e is SqlPlaceholderExpression))
						return node;

					if (!_visited.Add(node))
					{
						var variable = Expression.Variable(node.Type, "v" + _variables.Count);

						_replacement = (node, variable);

						_variables.Add((variable, node));
						return variable;
					}
				}
				else
				{
					if ( ExpressionEqualityComparer.Instance.Equals(node, _replacement.node))
						return _replacement.variable!;
				}

				return base.Visit(node);
			}

			internal override Expression VisitSqlReaderIsNullExpression(SqlReaderIsNullExpression node)
			{
				return node;
			}

			internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
			{
				return node;
			}
		}

		Expression Deduplicate(List<(ParameterExpression variable, Expression assingment)> variables,
			Expression expression)
		{
			var visitor = new DeduplicateVisitor();

			var deduplicated = visitor.Deduplicate(expression, variables);
			return deduplicated;
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
			var postProcessed = FinalizeConstructors(context, expression, true);

			// process eager loading queries
			var correctedEager = CompleteEagerLoadingExpressions(postProcessed, context, queryParameter, ref preambles, previousKeys);
			if (!ExpressionEqualityComparer.Instance.Equals((correctedEager, postProcessed)))
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

		public Expression UpdateNesting(IBuildContext upToContext, Expression expression)
		{
			return UpdateNesting(upToContext.SelectQuery, expression);
		}

		public class ParentInfo
		{
			Dictionary<SelectQuery, SelectQuery>? _info;

			public bool GetParentQuery(SelectQuery rootQuery, SelectQuery currentQuery, [MaybeNullWhen(false)] out SelectQuery? parentQuery)
			{
				if (_info == null)
				{
					_info = new();
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

		public Expression UpdateNesting(SelectQuery upToQuery, Expression expression)
		{
			// short path
			if (expression is SqlPlaceholderExpression currentPlaceholder && currentPlaceholder.SelectQuery == upToQuery)
				return expression;

			using var parentInfo = ParentInfoPool.Allocate();

			var withColumns =
				expression.Transform(
					(builder: this, upToQuery, parentInfo: parentInfo.Value),
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

			return withColumns;
		}

		public Expression ToColumns(IBuildContext rootContext, Expression expression)
		{
			using var parentInfo = ParentInfoPool.Allocate();

			var withColumns =
				expression.Transform(
					(builder: this, parentInfo: parentInfo.Value, rootQuery: rootContext.SelectQuery),
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

		public bool TryConvertToSql(IBuildContext? context, ProjectFlags flags, Expression expression, ColumnDescriptor? columnDescriptor, [NotNullWhen(true)] out ISqlExpression? sqlExpression, [NotNullWhen(false)] out SqlErrorExpression? error)
		{
			flags = flags & ~ProjectFlags.Expression | ProjectFlags.SQL;

			sqlExpression = null;

			//Just test that we can convert
			var actual = ConvertToSqlExpr(context, expression, flags | ProjectFlags.Test, columnDescriptor : columnDescriptor);
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

		public Expression BuildFinalProjection(IBuildContext context, Expression expression, ProjectFlags flags, string? alias = null)
		{
			var result = BuildSqlExpression(context, expression, flags, alias);

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

				if (attr != null && (!flags.HasFlag(ProjectFlags.Expression) || attr.ServerSideOnly || attr.Expression == "{0}"))
				{
					var transformed = attr.GetExpression((builder: this, context, flags),
						DataContext,
						this,
						context.SelectQuery, expr,
						static (context, e, descriptor) =>
							context.builder.ConvertToExtensionSql(context.context, context.flags, e, descriptor));

					if (transformed is SqlPlaceholderExpression placeholder)
						return placeholder.WithPath(expr);


					if (attr.ServerSideOnly)
					{
						if (transformed is SqlErrorExpression errorExpr)
							return SqlErrorExpression.EnsureError(errorExpr, expr.Type);
						return SqlErrorExpression.EnsureError(expr, expr.Type);
					}
				}
			}

			return expr;
		}

		public Expression FinalizeConstructors(IBuildContext context, Expression inputExpression, bool deduplicate)
		{
			List<(ParameterExpression variable, Expression assignment)>? variables = null;

			if (deduplicate)
				variables = new();

			var expression = FinalizeConstructorInternal(context, inputExpression, variables);

			if (variables?.Count > 0)
			{
				var expressionGenerator = new ExpressionGenerator();

				for (var index = 0; index < variables.Count; index++)
				{
					var (variable, assignment) = variables[index];
					expressionGenerator.AddVariable(variable);
					expressionGenerator.Assign(variable, assignment);
				}

				expressionGenerator.AddExpression(expression);
				var built = expressionGenerator.Build();
				expression = built;
			}

			return expression;
		}

		Expression FinalizeConstructorInternal(IBuildContext context, Expression inputExpression, List<(ParameterExpression variable, Expression assignment)>? variables)
		{
			var expression = BuildFinalProjection(context, inputExpression, ProjectFlags.Expression);

			expression = OptimizationContext.OptimizeExpressionTree(expression, true);

			if (variables != null)
				expression = Deduplicate(variables, expression);

			var reconstructed = expression.Transform((builder : this, context), (ctx, e) =>
			{
				if (e is SqlGenericConstructorExpression generic)
				{
					return new TransformInfo(ctx.builder.Construct(ctx.builder.MappingSchema, generic, ctx.context,
						ProjectFlags.Expression), false, true);
				}

				return new TransformInfo(e);
			});

			return reconstructed;
		}

		sealed class SubQueryContextInfo
		{
			public Expression     SequenceExpression = null!;
			public IBuildContext? Context;
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

		Dictionary<Expression, SubQueryContextInfo>? _buildContextCache;
		Dictionary<Expression, SubQueryContextInfo>? _testbuildContextCache;

		SubQueryContextInfo GetSubQueryContext(IBuildContext inContext, ref IBuildContext context, Expression expr, ProjectFlags flags)
		{
			context = inContext;
			var testExpression = CorrectRoot(context, expr);

			var shouldCache = flags.IsSql() || flags.IsExpression() || flags.IsExtractProjection() || flags.IsRoot();
			
			if (shouldCache && _buildContextCache?.TryGetValue(testExpression, out var item) == true)
				return item;

			if (flags.IsTest())
			{
				if (_testbuildContextCache?.TryGetValue(testExpression, out var testItem) == true)
					return testItem;
			}
			
			var rootQuery = GetRootContext(context, testExpression, false);
			rootQuery ??= GetRootContext(context, expr, false);

			if (rootQuery != null)
			{
				context = rootQuery.BuildContext;
			}

			var ctx = GetSubQuery(context, testExpression, flags);

			var info = new SubQueryContextInfo { SequenceExpression = testExpression, Context = ctx };

			if (shouldCache)
			{
				if (flags.IsTest())
				{
					_testbuildContextCache ??= new(ExpressionEqualityComparer.Instance);
					_testbuildContextCache[testExpression] = info;
				}
				else
				{
					_buildContextCache ??= new(ExpressionEqualityComparer.Instance);
					_buildContextCache[testExpression] = info;
				}
			}

			return info;
		}

		public static bool IsSingleElementContext(IBuildContext context)
		{
			return context is FirstSingleBuilder.FirstSingleContext;
		}

		public Expression? TryGetSubQueryExpression(IBuildContext context, Expression expr, string? alias, ProjectFlags flags)
		{
			if (flags.IsTraverse())
				return null;

			var unwrapped = expr.Unwrap();

			if (unwrapped is BinaryExpression or ConditionalExpression or DefaultExpression or DefaultValueExpression)
				return null;

			if (unwrapped is SqlGenericConstructorExpression or ConstantExpression or SqlEagerLoadExpression)
				return null;

			if (unwrapped is ContextRefExpression contextRef && contextRef.BuildContext.ElementType == expr.Type)
				return null;

			if (SequenceHelper.IsSpecialProperty(unwrapped, out _, out _))
				return null;

			if (!flags.IsSubquery() && CanBeCompiled(expr, flags.IsExpression()))
				return null;

			if (unwrapped is MemberExpression me)
			{
				var attr = me.Member.GetExpressionAttribute(MappingSchema);
				if (attr != null)
					return null;
			}

			var info = GetSubQueryContext(context, ref context, unwrapped, flags);

			if (info.Context == null)
				return null;

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
				if (e.NodeType == ExpressionType.Convert && e.Type != typeof(object))
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
