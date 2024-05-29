using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	class SubQueryContext : BuildContextBase
	{
		public SubQueryContext(IBuildContext subQuery, SelectQuery selectQuery, bool addToSql)
			: base(subQuery.Builder, subQuery.ElementType, selectQuery)
		{
			if (selectQuery == subQuery.SelectQuery)
				throw new ArgumentException("Wrong subQuery argument.", nameof(subQuery));

			SubQuery        = subQuery;
			SubQuery.Parent = this;

			if (addToSql)
				selectQuery.From.Table(SubQuery.SelectQuery);
		}

		public SubQueryContext(IBuildContext subQuery, bool addToSql = true)
			: this(subQuery, new SelectQuery(), addToSql)
		{
		}

		public          IBuildContext SubQuery      { get; }
		public override MappingSchema MappingSchema => SubQuery.MappingSchema;

		protected virtual bool OptimizeColumns => true;

		protected virtual int GetIndex(int index, ISqlExpression column)
		{
			throw new NotImplementedException();
		}

		public override void SetAlias(string? alias)
		{
			if (alias == null)
				return;

			SubQuery.SetAlias(alias);

			if (alias.Contains('<'))
				return;

			var table = SelectQuery.From.Tables.FirstOrDefault();

			if (table is { Alias: null })
				table.Alias = alias;
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, SubQuery);
			return SubQuery.GetContext(expression, buildInfo);
		}

		public override SqlStatement GetResultStatement()
		{
			return new SqlSelectStatement(SelectQuery);
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			SubQuery.SetRunQuery(query, expr);
		}

		public override IBuildContext Clone(CloningContext context)
		{
			var selectQuery = context.CloneElement(SelectQuery);
			return new SubQueryContext(context.CloneContext(SubQuery), selectQuery, false);
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.IsRoot() && SequenceHelper.IsSameContext(path, this))
				return path;

			var corrected = SequenceHelper.CorrectExpression(path, this, SubQuery);

			if (flags.IsExtractProjection() || flags.IsTraverse() || flags.IsAggregationRoot() || flags.IsSubquery() || flags.IsTable() || flags.IsAssociationRoot() || flags.IsRoot())
			{
				var processed = Builder.MakeExpression(SubQuery, corrected, flags);
				return processed;
			}

			var buildFlags = ExpressionBuilder.BuildFlags.None;
			if (flags.IsSql())
				buildFlags = ExpressionBuilder.BuildFlags.ForceAssignments;

			var result = Builder.BuildSqlExpression(SubQuery, corrected, flags, buildFlags: buildFlags);

			if (ExpressionEqualityComparer.Instance.Equals(corrected, result))
				return path;

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				result = SequenceHelper.CorrectTrackingPath(Builder, result, path);

				// correct all placeholders, they should target to appropriate SubQuery.SelectQuery
				//
				result = Builder.UpdateNesting(this, result);
				result = SequenceHelper.CorrectSelectQuery(result, SelectQuery);

				if (!flags.HasFlag(ProjectFlags.AssociationRoot))
				{
					// remap back, especially for Recursive CTE
					result = SequenceHelper.ReplaceContext(result, SubQuery, this);
				}
			}

			return result;
		}

		public virtual  bool NeedsSubqueryForComparison => false;
		public override bool IsOptional                 => SubQuery.IsOptional;
	}
}
