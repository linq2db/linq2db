using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class TableLikeQueryContext : IBuildContext
	{
#if DEBUG
		public string SqlQueryText => SelectQuery == null ? "" : SelectQuery.SqlText;
		public string Path         => this.GetPath();
		public int    ContextId    { get; }
#endif
		public SelectQuery? SelectQuery
		{
			get => InnerQueryContext?.SelectQuery;
			set { }
		}

		public SqlStatement?  Statement { get; set; }
		public IBuildContext? Parent    { get; set; }

		public ExpressionBuilder  Builder           { get; }
		public Expression?        Expression        { get; }

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

		public TableLikeQueryContext(ContextRefExpression targetContextRef, ContextRefExpression sourceContextRef)
		{
			TargetContextRef  = targetContextRef;
			SourceContextRef  = sourceContextRef;
			Builder           = targetContextRef.BuildContext.Builder;
			InnerQueryContext = sourceContextRef.BuildContext;
			SubqueryContext   = new SubQueryContext(sourceContextRef.BuildContext);

			var projectionType = typeof(ProjectionHelper<,>).MakeGenericType(targetContextRef.Type, sourceContextRef.Type);

			ProjectionBody = Expression.MemberInit(Expression.New(projectionType),
				Expression.Bind(projectionType.GetProperty(nameof(ProjectionHelper<object, object>.target)),
					targetContextRef),
				Expression.Bind(projectionType.GetProperty(nameof(ProjectionHelper<object, object>.source)),
					sourceContextRef),
				Expression.Bind(projectionType.GetProperty(nameof(ProjectionHelper<object, object>.selft_target)),
					targetContextRef));

			var thisContextRef = new ContextRefExpression(projectionType, this);

			TargetPropAccess = Expression.Property(thisContextRef, nameof(ProjectionHelper<object, object>.target));
			SourcePropAccess = Expression.Property(thisContextRef, nameof(ProjectionHelper<object, object>.source));
			SelfTargetPropAccess = Expression.Property(thisContextRef, nameof(ProjectionHelper<object, object>.selft_target));

			Source = sourceContextRef.BuildContext is EnumerableContext enumerableSource
				? new SqlTableLikeSource { SourceEnumerable = enumerableSource.Table }
				: new SqlTableLikeSource { SourceQuery = sourceContextRef.BuildContext.SelectQuery };
		}

		public Expression PrepareSourceLambda(LambdaExpression lambdaExpression)
		{
			if (lambdaExpression.Parameters.Count == 1)
				return lambdaExpression.GetBody(SourcePropAccess);

			throw new InvalidOperationException();
		}

		public Expression PrepareTargetSourceLambda(LambdaExpression lambdaExpression)
		{
			if (lambdaExpression.Parameters.Count == 2)
				return lambdaExpression.GetBody(TargetPropAccess, SourcePropAccess);

			throw new InvalidOperationException();
		}

		public Expression PrepareTargetLambda(LambdaExpression lambdaExpression)
		{
			if (lambdaExpression.Parameters.Count == 1)
				return lambdaExpression.GetBody(TargetPropAccess);

			throw new InvalidOperationException();
		}

		public Expression PrepareSelfTargetLambda(LambdaExpression lambdaExpression)
		{
			if (lambdaExpression.Parameters.Count == 1)
			{
				return lambdaExpression.GetBody(EnsureType(SelfTargetPropAccess, lambdaExpression.Parameters[0].Type));
			}

			throw new InvalidOperationException();
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

		Dictionary<SqlPlaceholderExpression, SqlPlaceholderExpression> _knownMap = new (ExpressionEqualityComparer.Instance);

		public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			throw new NotImplementedException();
		}

		public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
		{
			throw new NotImplementedException();
		}

		public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		public bool IsTargetAssociation(Expression pathExpression)
		{
			var result = null != pathExpression.Find(this, static (ctx, e) =>
			{
				if (ctx.Builder.IsAssociation(e))
				{
					if (e.NodeType == ExpressionType.MemberAccess)
					{
						var unwrappedObj = ((MemberExpression)e).Expression.UnwrapConvert();
						if (ExpressionEqualityComparer.Instance.Equals(unwrappedObj, ctx.TargetContextRef) || ExpressionEqualityComparer.Instance.Equals(unwrappedObj, ctx.TargetPropAccess))
						{
							return true;
						}
					}
				}

				return false;
			});

			return result;
		}

		bool IsSelfTargetExpression(Expression pathExpression)
		{
			var result = null != pathExpression.Find(this, static (ctx, e) =>
			{
				if (ExpressionEqualityComparer.Instance.Equals(e, ctx.SelfTargetPropAccess))
				{
					return true;
				}

				return false;
			});

			return result;
		}

		bool IsTargetExpression(Expression pathExpression)
		{
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

		public Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.HasFlag(ProjectFlags.AssociationRoot) || flags.HasFlag(ProjectFlags.Expand))
				return path;

			if (SequenceHelper.IsSameContext(path, this))
				return path;

			var projectedPath = Builder.Project(this, path, null, -1, flags, ProjectionBody, true);

			if (!ReferenceEquals(projectedPath, path))
			{
				if (flags.HasFlag(ProjectFlags.Root))
					return path;
			}

			var subqueryPath        = projectedPath;
			var correctedPath       = subqueryPath;
			var isTargetAssociation = false;
			
			if (!flags.HasFlag(ProjectFlags.Test))
			{
				if (IsTargetAssociation(projectedPath))
				{
					if (IsSelfTargetExpression(path))
					{
						// in case when there is no access to the Source we are trying to generate subquery SQL
						//
						var cloningContext = new CloningContext();

						var targetContext = TargetContextRef.BuildContext;
						var clonedTargetContext = cloningContext.CloneContext(targetContext);
						var clonedContextRef = new ContextRefExpression(TargetContextRef.Type, clonedTargetContext, "self_target");

						correctedPath = correctedPath.Replace(TargetContextRef, clonedContextRef);
						var sqlExpr = Builder.ConvertToSqlExpr(clonedTargetContext, correctedPath, flags);

						SqlPlaceholderExpression? placeholder = null;
						if (sqlExpr is SqlPlaceholderExpression fieldPlaceholder)
							placeholder = fieldPlaceholder;
						else if (sqlExpr is SqlGenericConstructorExpression generic)
						{
							if (generic.Assignments.Count != 1)
								throw new InvalidOperationException();

							if (generic.Assignments[0].Expression is SqlPlaceholderExpression assignmentPlaceholder)
								placeholder = assignmentPlaceholder;
						}

						if (placeholder == null)
							throw new InvalidOperationException();

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
						
						// creating subquery placeholder
						var resultPlaceholder = ExpressionBuilder.CreatePlaceholder(TargetContextRef.BuildContext, query, placeholder.Path);

						var result = sqlExpr.Replace(placeholder, resultPlaceholder, ExpressionEqualityComparer.Instance);

						return result;
					}

					// Redirecting to TargetInSourceContextRef for correct processing associations
					//

					isTargetAssociation = true;
 
					if (TargetInSourceContextRef == null)
					{
						var cloningContext = new CloningContext();
						var targetCloned   = cloningContext.CloneContext(TargetContextRef.BuildContext);

						if (ConnectionLambda == null)
							throw new InvalidOperationException();

						var predicate = SequenceHelper.PrepareBody(ConnectionLambda, targetCloned, SourceContextRef.BuildContext);

						var join = new SqlJoinedTable(JoinType.Left, targetCloned.SelectQuery, null, true);
						SourceContextRef.BuildContext.SelectQuery.From.Tables[0].Joins.Add(join);

						Builder.BuildSearchCondition(SourceContextRef.BuildContext, predicate, ProjectFlags.SQL, join.Condition.Conditions);

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
			}

			if (!ReferenceEquals(correctedPath, path))
			{
				correctedPath = Builder.ConvertToSqlExpr(InnerQueryContext, correctedPath, flags);

				if (!flags.HasFlag(ProjectFlags.Test))
				{
					correctedPath = SequenceHelper.CorrectTrackingPath(correctedPath, SubqueryContext, this);

					var memberPath = TableLikeHelpers.GetMemberPath(isTargetAssociation ? path : subqueryPath);
					correctedPath = Builder.UpdateNesting(SubqueryContext, correctedPath);
					var placeholders = ExpressionBuilder.CollectPlaceholders2(correctedPath, memberPath).ToList();

					var remapped = TableLikeHelpers.RemapToFields(SubqueryContext, Source, Source.SourceFields, _knownMap, correctedPath, placeholders);

					return remapped;
				}
			}

			return correctedPath;
		}

		public IBuildContext Clone(CloningContext context)
		{
			throw new NotImplementedException();
		}

		public void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			throw new NotImplementedException();
		}

		public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			throw new NotImplementedException();
		}

		public IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			return null;
		}

		public int ConvertToParentIndex(int index, IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public void SetAlias(string? alias)
		{
		}

		public ISqlExpression? GetSubQuery(IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public SqlStatement GetResultStatement()
		{
			return SubqueryContext.GetResultStatement();
		}

		public void CompleteColumns()
		{
		}

		class ProjectionHelper<TTarget, TSource>
		{
			public TTarget? target       { get; set; }
			public TSource? source       { get; set; }
			public TTarget? selft_target { get; set; }
		}
	}
}
