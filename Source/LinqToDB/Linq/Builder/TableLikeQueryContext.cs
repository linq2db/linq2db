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

		public Expression PrepareSelfTargetLambda(LambdaExpression lambdaExpression)
		{
			if (lambdaExpression.Parameters.Count != 1)
				throw new InvalidOperationException();

			return lambdaExpression.GetBody(EnsureType(SelfTargetPropAccess, lambdaExpression.Parameters[0].Type));
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
			if (flags.HasFlag(ProjectFlags.AssociationRoot))
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

			if (!flags.IsTest())
			{
				if (IsTargetAssociation(projectedPath))
				{
					if (IsSelfTargetExpression(path))
					{
						var selfTargetContext = new SelfTargetContext(TargetContextRef);
						correctedPath = correctedPath.Replace(TargetContextRef, TargetContextRef.WithContext(selfTargetContext));
						return correctedPath;
					}

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

						Builder.BuildSearchCondition(SourceContextRef.BuildContext, predicate, ProjectFlags.SQL, join.Condition);

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
				// remove forcing, if association is created in source. Maybe we can find better way...
				if (!HasAssociation(Builder, path))
					flags &= ~ProjectFlags.ForceOuterAssociation;

				correctedPath = Builder.ConvertToSqlExpr(InnerQueryContext, correctedPath, flags);

				if (!flags.IsTest())
				{
					// replace tracking path back
					var translated = SequenceHelper.CorrectTrackingPath(Builder, correctedPath, path);

					var placeholders = ExpressionBuilder.CollectPlaceholders(translated);

					var remapped = TableLikeHelpers.RemapToFields(SubqueryContext, Source, Source.SourceFields, _knownMap, null, translated, placeholders);

					return remapped;
				}
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

		public static bool HasAssociation(ExpressionBuilder builder, Expression expression)
		{
			var result = null != expression.Find(builder, static (builder, expr) =>
			{
				if (builder.IsAssociation(expr, out _))
					return true;
				return false;
			});

			return result;
		}

		class ProjectionHelper<TTarget, TSource>
		{
			public TTarget? target       { get; set; }
			public TSource? source       { get; set; }
			public TTarget? selft_target { get; set; }
		}

		class SelfTargetContext : PassThroughContext
		{
			public SelfTargetContext(ContextRefExpression targetContextRef) : base(targetContextRef.BuildContext)
			{
				TargetContextRef = targetContextRef;
			}

			public ContextRefExpression TargetContextRef { get; }
			IBuildContext               TargetContext    => Context;

			public override IBuildContext Clone(CloningContext context)
			{
				throw new NotImplementedException();
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsTest() || !HasAssociation(Builder, path))
					return base.MakeExpression(path, flags);

				// in case when there is no access to the Source we are trying to generate subquery SQL
				//
				var cloningContext = new CloningContext();

				var targetContext = TargetContext;
				var clonedTargetContext = cloningContext.CloneContext(targetContext);

				var correctedPath = SequenceHelper.ReplaceContext(path, this, clonedTargetContext);

				var sqlExpr = Builder.ConvertToSqlExpr(clonedTargetContext, correctedPath, flags);

				SqlPlaceholderExpression? placeholder = null;
				if (sqlExpr is SqlPlaceholderExpression fieldPlaceholder)
					placeholder = fieldPlaceholder;
				else if (SequenceHelper.UnwrapDefaultIfEmpty(sqlExpr) is SqlGenericConstructorExpression generic)
				{
					if (generic.Assignments.Count != 1)
						throw new InvalidOperationException();

					if (generic.Assignments[0].Expression is SqlPlaceholderExpression assignmentPlaceholder)
						placeholder = assignmentPlaceholder;
				}

				if (placeholder == null)
				{
					return ExpressionBuilder.CreateSqlError(this, path);
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

				// creating subquery placeholder
				var resultPlaceholder = ExpressionBuilder.CreatePlaceholder(TargetContextRef.BuildContext, query, placeholder.Path);

				var result = sqlExpr.Replace(placeholder, resultPlaceholder, ExpressionEqualityComparer.Instance);

				return result;
			}
		}
	}
}
