using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	sealed class TableLikeQueryContext : BuildContextBase
	{
		public ContextRefExpression  TargetContextRef         { get; }
		public ContextRefExpression  SourceContextRef         { get; }
		public IBuildContext         InnerQueryContext        { get; }
		public SubQueryContext       SubqueryContext          { get; }
		public SqlTableLikeSource    Source                   { get; }

		public Expression?           SourceKeySelector        { get; set; }
		public Expression?           TargetKeySelector        { get; set; }
		public LambdaExpression?     ConnectionLambda         { get; set; }
		public ContextRefExpression? TargetInSourceContextRef { get; set; }

		public Expression SourcePropAccess { get; }
		public Expression TargetPropAccess { get; }

		Expression ProjectionBody       { get; }
		Expression SelfTargetPropAccess { get; }

		public override bool AutomaticAssociations => true;
		public override bool IsSingleElement       => true;

		public bool? IsSourceOuter { get; set; }

		public TableLikeQueryContext(ContextRefExpression targetContextRef, ContextRefExpression sourceContextRef)
			: base(sourceContextRef.BuildContext.Builder, targetContextRef.ElementType, sourceContextRef.BuildContext.SelectQuery)
		{
			TargetContextRef  = targetContextRef;
			SourceContextRef  = sourceContextRef;
			InnerQueryContext = sourceContextRef.BuildContext;
			SubqueryContext   = new SubQueryContext(sourceContextRef.BuildContext);

			var projectionType = typeof(ProjectionHelper<,>).MakeGenericType(targetContextRef.Type, sourceContextRef.Type);

			ProjectionBody = Expression.MemberInit(Expression.New(projectionType),
				Expression.Bind(projectionType.GetProperty(nameof(ProjectionHelper<object, object>.target)) ?? throw new InvalidOperationException(),
					targetContextRef),
				Expression.Bind(projectionType.GetProperty(nameof(ProjectionHelper<object, object>.source)) ?? throw new InvalidOperationException(),
					sourceContextRef),
				Expression.Bind(projectionType.GetProperty(nameof(ProjectionHelper<object, object>.selft_target)) ?? throw new InvalidOperationException(),
					targetContextRef));

			var thisContextRef = new ContextRefExpression(projectionType, this);

			TargetPropAccess = Expression.Property(thisContextRef, nameof(ProjectionHelper<object, object>.target));
			SourcePropAccess = Expression.Property(thisContextRef, nameof(ProjectionHelper<object, object>.source));
			SelfTargetPropAccess = Expression.Property(thisContextRef, nameof(ProjectionHelper<object, object>.selft_target));

			Source = sourceContextRef.BuildContext is EnumerableContext enumerableSource
				? new SqlTableLikeSource { SourceEnumerable = enumerableSource.Table }
				: new SqlTableLikeSource { SourceQuery = sourceContextRef.BuildContext.SelectQuery };
		}

		public Expression PrepareSourceBody(LambdaExpression lambdaExpression)
		{
			if (lambdaExpression.Parameters.Count != 1)
				throw new InvalidOperationException();

			SourceContextRef.Alias = lambdaExpression.Parameters[0].Name;

			return lambdaExpression.GetBody(SourcePropAccess);
		}

		public Expression PrepareTargetSource(LambdaExpression lambdaExpression)
		{
			if (lambdaExpression.Parameters.Count != 2)
				throw new InvalidOperationException();

			TargetContextRef.Alias = lambdaExpression.Parameters[0].Name;
			SourceContextRef.Alias = lambdaExpression.Parameters[1].Name;

			return lambdaExpression.GetBody(TargetPropAccess, SourcePropAccess);
		}

		public Expression PrepareTargetLambda(LambdaExpression lambdaExpression)
		{
			if (lambdaExpression.Parameters.Count != 1)
				throw new InvalidOperationException();

			TargetContextRef.Alias = lambdaExpression.Parameters[0].Name;

			return lambdaExpression.GetBody(TargetPropAccess);
		}

		class CorrectingVisitor: ExpressionVisitorBase
		{
			public   ParameterExpression  TargetParam      { get; }
			public   ContextRefExpression TargetContextRef { get; }

			public CorrectingVisitor(ParameterExpression targetParam, ContextRefExpression targetContextRef)
			{
				TargetParam      = targetParam;
				TargetContextRef = targetContextRef;
			}

			bool HasTargetRoot(Expression node)
			{
				switch (node.NodeType)
				{
					case ExpressionType.MemberAccess:
					{
						var member = (MemberExpression)node;
						if (member.Expression == TargetParam)
						{
							return true;
						}

						if (member.Expression != null)
						{
							return HasTargetRoot(member.Expression);
						}

						break;
					}
					case ExpressionType.Call:
					{
						var method = (MethodCallExpression)node;
						if (method.Object == TargetParam)
						{
							return true;
						}

						if (method.Method.IsStatic && method.Arguments.Count > 0 && method.IsAssociation(TargetContextRef.BuildContext.MappingSchema))
						{
							return HasTargetRoot(method.Arguments[0]);
						}

						if (method.Object != null)
						{
							return HasTargetRoot(method.Object);
						}

						if (method.IsQueryable() && method.Arguments.Count > 0)
						{
							if (HasTargetRoot(method.Arguments[0]))
							{
								return true;
							}
						}

						break;
					}

					case ExpressionType.Convert:
					case ExpressionType.ConvertChecked:
					{
						var unary = (UnaryExpression)node;
						return HasTargetRoot(unary.Operand);
					}
				}

				return false;
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				if (HasTargetRoot(node))
				{
					return CreateContextFromNode(node);
				}

				return base.VisitMethodCall(node);
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				if (HasTargetRoot(node))
				{
					return CreateContextFromNode(node);
				}

				return base.VisitMember(node);
			}

			Expression CreateContextFromNode(Expression node)
			{
				var containerContext = new SelfTargetContainerContext(TargetParam, TargetContextRef, node, true);
				var context          = new ContextRefExpression(node.Type, containerContext);
				return context;
			}
		}


		public Expression PrepareSelfTargetLambda(LambdaExpression lambdaExpression)
		{
			if (lambdaExpression.Parameters.Count != 1)
				throw new InvalidOperationException();

			var visitor = new CorrectingVisitor(lambdaExpression.Parameters[0], TargetContextRef);
			var correctedExpression = visitor.Visit(lambdaExpression.Body);

			return correctedExpression;
		}

		static Expression EnsureType(Expression expression, Type type)
		{
			if (expression.Type == type)
				return expression;

			return Expression.Convert(expression, type);
		}

		public Expression GenerateCondition()
		{
			if (ConnectionLambda is null)
				throw new ArgumentNullException(nameof(ConnectionLambda));

			return ConnectionLambda.GetBody(EnsureType(TargetPropAccess, ConnectionLambda.Parameters[0].Type),
				EnsureType(SourcePropAccess, ConnectionLambda.Parameters[1].Type));
		}

		Dictionary<Expression, SqlPlaceholderExpression> _knownMap = new (ExpressionEqualityComparer.Instance);

		public bool IsTargetAssociation(Expression pathExpression)
		{
			var result = null != pathExpression.Find(this, static (ctx, e) =>
			{
				if (ctx.Builder.IsAssociation(e, out _))
				{
					if (e.NodeType == ExpressionType.MemberAccess)
					{
						var unwrappedObj = ((MemberExpression)e).Expression.UnwrapConvert();
						if (ExpressionEqualityComparer.Instance.Equals(unwrappedObj, ctx.TargetContextRef) ||
							ExpressionEqualityComparer.Instance.Equals(unwrappedObj, ctx.TargetPropAccess))
						{
							return true;
						}
					}
				}

				return false;
			});

			return result;
		}

		bool IsTargetExpression(Expression pathExpression)
		{
			if (SourceContextRef == TargetContextRef)
				return false;

			var result = null != pathExpression.Find(this, static (ctx, e) =>
			{
				if (ExpressionEqualityComparer.Instance.Equals(e, ctx.TargetContextRef))
				{
					return true;
				}

				return false;
			});

			return result;
		}

		//TODO: not sure
		public override MappingSchema MappingSchema => TargetContextRef.BuildContext.MappingSchema;

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (!flags.IsSql())
				return path;

			if (SequenceHelper.IsSameContext(path, this))
				return path;

			var projectedPath = Builder.Project(this, path, null, -1, flags, ProjectionBody, true);

			if (projectedPath is SqlErrorExpression)
				projectedPath = path;

			if (!ReferenceEquals(projectedPath, path))
			{
				if (flags.IsRoot())
					return path;
			}

			var subqueryPath        = projectedPath;
			var correctedPath       = subqueryPath;

			if (IsTargetAssociation(projectedPath))
			{
				// Redirecting to TargetInSourceContextRef for correct processing associations
				//

				if (TargetInSourceContextRef == null)
				{
					var cloningContext = new CloningContext();
					var targetCloned   = cloningContext.CloneContext(TargetContextRef.BuildContext);

					if (ConnectionLambda == null)
						throw new InvalidOperationException();

					var predicate = SequenceHelper.PrepareBody(ConnectionLambda, targetCloned, SourceContextRef.BuildContext);

					var join = new SqlJoinedTable(JoinType.Left, targetCloned.SelectQuery, null, true);
					SourceContextRef.BuildContext.SelectQuery.From.Tables[0].Joins.Add(join);

					Builder.BuildSearchCondition(SourceContextRef.BuildContext, predicate, join.Condition);

					TargetInSourceContextRef =
						new ContextRefExpression(TargetContextRef.Type, targetCloned, "target");
				}

				correctedPath = correctedPath.Replace(TargetContextRef, TargetInSourceContextRef);
			}
			else if (IsTargetExpression(projectedPath))
			{
				// let target context to MakeExpression
				//
				return projectedPath;
			}

			if (!ReferenceEquals(correctedPath, path))
			{
				var isOuter = IsSourceOuter == true;
				correctedPath = Builder.BuildSqlExpression(InnerQueryContext, correctedPath, isOuter ? BuildFlags.ForceOuter : BuildFlags.None);

				correctedPath = Builder.UpdateNesting(InnerQueryContext, correctedPath);

				// replace tracking path back
				var translated = SequenceHelper.CorrectTrackingPath(Builder, correctedPath, path);

				var placeholders = ExpressionBuilder.CollectPlaceholders(translated);

				var remapped = TableLikeHelpers.RemapToFields(SubqueryContext, Source, Source.SourceFields, _knownMap, null, translated, placeholders);

				return remapped;
			}

			return correctedPath;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			throw new NotImplementedException();
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			throw new NotImplementedException();
		}

		public override SqlStatement GetResultStatement()
		{
			return SubqueryContext.GetResultStatement();
		}

		class ProjectionHelper<TTarget, TSource>
		{
			public TTarget? target       { get; set; }
			public TSource? source       { get; set; }
			public TTarget? selft_target { get; set; }
		}

		class SelfTargetContainerContext : BuildContextBase
		{
			public SelfTargetContainerContext(ParameterExpression targetParam, ContextRefExpression targetContextRef, Expression substitutedExpression, bool needsCloning) : 
				base(targetContextRef.BuildContext.Builder, targetContextRef.BuildContext.ElementType, targetContextRef.BuildContext.SelectQuery)
			{
				TargetParam           = targetParam;
				TargetContextRef      = targetContextRef;
				SubstitutedExpression = substitutedExpression;
				NeedsCloning          = needsCloning;
			}

			public ParameterExpression  TargetParam           { get; }
			public ContextRefExpression TargetContextRef      { get; }
			public Expression           SubstitutedExpression { get; }
			public bool                 NeedsCloning          { get; }
			IBuildContext               TargetContext         => TargetContextRef.BuildContext;

			public override bool IsSingleElement       => true;

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext Clone(CloningContext context)
			{
				throw new NotImplementedException();
			}

			public override SqlStatement GetResultStatement()
			{
				throw new NotImplementedException();
			}

			public override MappingSchema MappingSchema => TargetContextRef.BuildContext.MappingSchema;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (!flags.IsSql())
					return path;

				if (!SequenceHelper.IsSameContext(path, this))
					return path;

				// in case when there is no access to the Source we are trying to generate subquery SQL
				//
				var cloningContext = new CloningContext();

				var targetContext       = TargetContext;
				var clonedTargetContext = NeedsCloning ? cloningContext.CloneContext(targetContext) : targetContext;

				var clonedRef     = new ContextRefExpression(TargetParam.Type, clonedTargetContext, TargetParam.Name);
				var correctedPath = SubstitutedExpression.Replace(TargetParam, clonedRef);

				if (!NeedsCloning)
				{
					var resultExpr = Builder.BuildSqlExpression(clonedTargetContext, correctedPath, BuildFlags.ForceOuter);
					return resultExpr;
				}

				var sqlExpr = Builder.BuildExpression(clonedTargetContext, correctedPath);

				if (!flags.IsSql())
					return sqlExpr;

				sqlExpr = Builder.UpdateNesting(clonedTargetContext, sqlExpr);

				SqlPlaceholderExpression? placeholder = null;
				if (sqlExpr is SqlPlaceholderExpression fieldPlaceholder)
					placeholder = fieldPlaceholder;
				else if (Builder.ParseGenericConstructor(SequenceHelper.UnwrapDefaultIfEmpty(sqlExpr), flags, null) is SqlGenericConstructorExpression generic)
				{
					if (generic.Assignments.Count != 1)
						throw new InvalidOperationException();

					if (generic.Assignments[0].Expression is SqlPlaceholderExpression assignmentPlaceholder)
						placeholder = assignmentPlaceholder;
				}

				if (placeholder == null)
				{
					return ExpressionBuilder.CreateSqlError(SubstitutedExpression);
				}

				// forcing making column
				_ = Builder.MakeColumn(null, placeholder);

				var query = clonedTargetContext.SelectQuery;

				var targetTable = MergeBuilder.GetTargetTable(targetContext);
				if (targetTable == null)
					throw new NotImplementedException("Currently, only CTEs are supported as the target of a merge. You can fix by calling .AsCte() before calling .Merge()");

				var clonedTargetTable = MergeBuilder.GetTargetTable(clonedTargetContext);

				if (clonedTargetTable == null)
					throw new InvalidOperationException();

				query = MergeBuilder.ReplaceSourceInQuery(query, clonedTargetTable, targetTable);

				ISqlExpression placeholderSqlExpr = query;

				// if there is no FROM clause and only one column in SELECT clause, it means that we just used expression from Target
				if (query.Select.From.Tables.Count == 0 && query.Select.Columns.Count == 1)
					placeholderSqlExpr = query.Select.Columns[0].Expression;

				// creating subquery placeholder
				var resultPlaceholder = ExpressionBuilder.CreatePlaceholder(TargetContextRef.BuildContext, placeholderSqlExpr, placeholder.Path);

				var result = sqlExpr.Replace(placeholder, resultPlaceholder, ExpressionEqualityComparer.Instance);

				return result;
			}
		}
	}
}
