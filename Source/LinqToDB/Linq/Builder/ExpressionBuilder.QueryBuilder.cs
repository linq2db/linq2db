using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using LinqToDB.Common;
	using LinqToDB.Expressions;
	using LinqToDB.Expressions.ExpressionVisitors;
using LinqToDB.Extensions;
using LinqToDB.Reflection;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	internal partial class ExpressionBuilder
	{
		#region BuildExpression

		sealed class FinalizeExpressionVisitor : ExpressionVisitorBase
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

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				var newNode = base.VisitMethodCall(node);
				if (!ReferenceEquals(newNode, node))
					return Visit(newNode);

				if (_constructRun)
				{
					// remove translation helpers
					if (node.IsSameGenericMethod(Methods.LinqToDB.ApplyModifierInternal)
						|| node.IsSameGenericMethod(Methods.LinqToDB.DisableFilterInternal)
						|| node.IsSameGenericMethod(Methods.LinqToDB.LoadWithInternal))
					{
						return node.Arguments[0];
					}
				}

				return node;
			}

			Expression TranslateExpression(Expression local)
			{
				var translated = _context.Builder.BuildSqlExpression(_context, local, buildPurpose : BuildPurpose.Expression, buildFlags : BuildFlags.ForceDefaultIfEmpty);

				var lambdaResolver = new LambdaResolveVisitor(_context, BuildPurpose.Expression, false);
				translated = lambdaResolver.Visit(translated);

				return translated;
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

		public sealed class ParentInfo
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

		public ExpressionBuildVisitor.CacheSnapshot CreateSnapshot()
		{
			return _buildVisitor.CreateSnapshot();
		}

		public static SqlErrorExpression CreateSqlError(Expression expression)
		{
			return new SqlErrorExpression(expression);
		}

		public void RegisterExtensionAccessors(Expression expression)
		{
			void Register(Expression expr)
			{
				if (!MappingSchema.IsScalarType(expr.Type) && CanBeEvaluatedOnClient(expr))
				{
					var value = EvaluateExpression(expr);
					ParametersContext.MarkAsValue(expr, value);
				}
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
			var optimizerVisitor = new ExpressionTreeOptimizerVisitor();
			var optimized        = optimizerVisitor.Visit(inputExpression);

			using var finalizeVisitor = _finalizeVisitorPool.Allocate();
			var       generator       = new ExpressionGenerator();

			// Runs SqlGenericConstructorExpression deduplication and generating actual initializers
			var expression = finalizeVisitor.Value.Finalize(optimized, context, generator);

			generator.AddExpression(expression);

			var result = generator.Build();
			return result;
		}

		public ContextRefExpression? GetRootContext(Expression? expression, bool isAggregation)
		{
			if (expression == null)
				return null;

			var result = isAggregation
				? _buildVisitor.BuildAggregationRoot(expression)
				: _buildVisitor.BuildAssociationRoot(expression);

			return result as ContextRefExpression;
		}

		public ContextRefExpression? GetRootContext(IBuildContext context, bool isAggregation)
		{
			return GetRootContext(new ContextRefExpression(context.ElementType, context), isAggregation);
		}

		#endregion

		#region PreferServerSide

		FindVisitor<ExpressionBuilder>? _enforceServerSideVisitorTrue;
		FindVisitor<ExpressionBuilder>? _enforceServerSideVisitorFalse;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		FindVisitor<ExpressionBuilder> GetVisitor(bool enforceServerSide)
		{
			if (enforceServerSide)
				return _enforceServerSideVisitorTrue ??= FindVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.PreferServerSide(e, true));
			else
				return _enforceServerSideVisitorFalse ??= FindVisitor<ExpressionBuilder>.Create(this, static (ctx, e) => ctx.PreferServerSide(e, false));
		}

		internal bool PreferServerSide(Expression expr, bool enforceServerSide)
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
					return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeEvaluatedOnClient(expr);
				}

				case ExpressionType.Call:
				{
					var pi = (MethodCallExpression)expr;
					var l  = Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

					if (l != null)
						return GetVisitor(enforceServerSide).Find(l.Body.Unwrap()) != null;

					var attr = pi.Method.GetExpressionAttribute(MappingSchema);
					return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeEvaluatedOnClient(expr);
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
						columnDescriptor?.ValueConverter, Expression.Property(QueryRunnerParam, QueryRunner.DataContextInfo), DataReaderParam, canBeNull);

					if (placeholder.Type != readerExpression.Type)
					{
						readerExpression = Expression.Convert(readerExpression, placeholder.Type);
					}

					if (!canBeNull && readerExpression.Type == typeof(string) && DataContext.SqlProviderFlags.DoesProviderTreatsEmptyStringAsNull)
					{
						readerExpression = Expression.Coalesce(readerExpression, ExpressionInstances.EmptyString);
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

		public Expression<Func<IQueryRunner,IDataContext,DbDataReader,IQueryExpressions, object?[]?,object?[]?,T>> BuildMapper<T>(SelectQuery query, Expression expr)
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

			var mapper = Expression.Lambda<Func<IQueryRunner,IDataContext,DbDataReader,IQueryExpressions,object?[]?,object?[]?,T>>(mappingBody,
				QueryRunnerParam,
				ExpressionConstants.DataContextParam,
				DataReaderParam,
				QueryExpressionContainerParam,
				ParametersParam,
				PreambleParam);

			return mapper;
		}

		#endregion
	}
}
