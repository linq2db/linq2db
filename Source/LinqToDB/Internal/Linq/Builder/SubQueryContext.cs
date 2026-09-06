using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	class SubQueryContext : BuildContextBase
	{
		public SubQueryContext(IBuildContext subQuery, SelectQuery selectQuery, bool addToSql)
			: base(subQuery.TranslationModifier, subQuery.Builder, subQuery.ElementType, selectQuery)
		{
			if (selectQuery == subQuery.SelectQuery)
				throw new ArgumentException("Wrong subQuery argument.", nameof(subQuery));

			SubQuery        = subQuery;
			SubQuery.Parent = this;

			if (addToSql)
				selectQuery.From.Table(SubQuery.SelectQuery);
		}

#pragma warning disable RS0060 // API with optional parameter(s) should have the most parameters amongst its public overloads
		public SubQueryContext(IBuildContext subQuery, bool addToSql = true)
#pragma warning restore RS0060 // API with optional parameter(s) should have the most parameters amongst its public overloads
			: this(subQuery, new SelectQuery(), addToSql)
		{
		}

		public          IBuildContext SubQuery      { get; }
		public override MappingSchema MappingSchema => SubQuery.MappingSchema;

		public bool IsSelectWrapper { get; set; }

		public override void SetAlias(string? alias)
		{
			if (alias == null)
				return;

			SubQuery.SetAlias(alias);

			if (alias.Contains('<', StringComparison.Ordinal))
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

			return new SubQueryContext(context.CloneContext(SubQuery), selectQuery, false) { IsSelectWrapper = IsSelectWrapper };
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.IsRoot() && SequenceHelper.IsSameContext(path, this))
				return path;

			var corrected = SequenceHelper.CorrectExpression(path, this, SubQuery);

			// Expand hands a member's defining expression to the caller, and a value bound to this subquery's row set
			// is then evaluated at the caller's level - a window function referenced from the parent lands inside
			// whatever the parent computes. Materialize it as a column instead. What has no SQL value of its own, a
			// window definition among them, keeps expanding below (#5867).
			if (flags.IsExpand()
				&& Builder.BuildSqlExpression(SubQuery, corrected) is SqlPlaceholderExpression sqlValue)
			{
				return Builder.UpdateNesting(this, sqlValue);
			}

			var result = Builder.BuildExpression(SubQuery, corrected);

			if (flags.IsTraverse() || flags.IsAggregationRoot() || flags.IsTable() || flags.IsAssociationRoot() || flags.IsRoot() || flags.IsSubquery())
			{
				return result;
			}

			if (ExpressionEqualityComparer.Instance.Equals(corrected, result))
			{
				return path;
			}

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

			return result;
		}

		public virtual  bool NeedsSubqueryForComparison => false;
		public override bool IsOptional                 => SubQuery.IsOptional;
	}
}
