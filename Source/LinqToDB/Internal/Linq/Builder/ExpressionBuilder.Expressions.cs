using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Mapping;
using LinqToDB.Model;

namespace LinqToDB.Internal.Linq.Builder
{
	partial class ExpressionBuilder
	{
		static ObjectPool<FinalizeExpressionVisitor> _finalizeVisitorPool = new(() => new FinalizeExpressionVisitor(), v => v.Cleanup(), 100);

		public ColumnDescriptor? CurrentDescriptor => _buildVisitor.CurrentDescriptor;

		public Expression BuildSqlExpression(IBuildContext? context, Expression expression, BuildPurpose buildPurpose, BuildFlags buildFlags, string? alias = null)
		{
			var result = _buildVisitor.BuildExpression(context, expression, buildPurpose, buildFlags, alias);
			return context != null ? UpdateNesting(context, result) : result;
		}

		public Expression BuildSqlExpression(IBuildContext? context, Expression expression, BuildFlags buildFlags, string? alias = null)
		{
			var result = _buildVisitor.BuildExpression(context, expression, BuildPurpose.Sql, buildFlags, alias);
			return context != null ? UpdateNesting(context, result) : result;
		}

		public Expression BuildSqlExpression(IBuildContext? context, Expression expression)
		{
			var result = _buildVisitor.BuildExpression(context, expression, BuildPurpose.Sql);
			return context != null ? UpdateNesting(context, result) : result;
		}

		public Expression BuildExpression(IBuildContext? context, Expression expression, BuildFlags buildFlags, string? alias = null)
		{
			return _buildVisitor.BuildExpression(context, expression, buildFlags, alias);
		}

		public Expression BuildExpression(IBuildContext context, Expression expression)
		{
			return _buildVisitor.BuildExpression(context, expression);
		}

		public Expression BuildRootExpression(Expression expression)
		{
			return _buildVisitor.BuildExpression(expression, BuildPurpose.Root);
		}

		public Expression BuildExtractExpression(IBuildContext context, Expression expression)
		{
			return _buildVisitor.BuildExpression(context, expression, BuildPurpose.Extract);
		}

		public Expression BuildExpandExpression(Expression expression)
		{
			return _buildVisitor.BuildExpression(expression, BuildPurpose.Expand, BuildFlags.ForExpanding);
		}

		public Expression BuildExpandExpression(IBuildContext context, Expression expression)
		{
			return _buildVisitor.BuildExpression(context, expression, BuildPurpose.Expand, BuildFlags.ForExpanding);
		}

		public Expression BuildSubqueryExpression(Expression expression)
		{
			return _buildVisitor.BuildExpression(expression, BuildPurpose.SubQuery);
		}

		public Expression BuildTableExpression(Expression expression)
		{
			return _buildVisitor.BuildExpression(expression, BuildPurpose.Table);
		}

		public Expression BuildTraverseExpression(Expression expression)
		{
			return _buildVisitor.BuildExpression(expression, BuildPurpose.Traverse);
		}

		public Expression BuildAggregationRootExpression(Expression expression)
		{
			return _buildVisitor.BuildExpression(expression, BuildPurpose.AggregationRoot);
		}

		public Expression ConvertToExtensionSql(IBuildContext context, Expression expression, ColumnDescriptor? columnDescriptor, bool? inlineParameters)
		{
			return _buildVisitor.ConvertToExtensionSql(context, expression, columnDescriptor, inlineParameters);
		}

		bool _handlingAlias;

		Expression CheckForAlias(IBuildContext context, MemberExpression memberExpression, EntityDescriptor entityDescriptor, string alias, ProjectFlags flags)
		{
			if (_handlingAlias)
				return memberExpression;

			var otherProp = entityDescriptor.TypeAccessor.GetMemberByName(alias);

			if (otherProp == null)
				return memberExpression;

			var newPath     = Expression.MakeMemberAccess(memberExpression.Expression, otherProp.MemberInfo);

			_handlingAlias = true;
			var aliasResult = BuildExpression(context, newPath);
			_handlingAlias = false;

			if (aliasResult is not SqlErrorExpression && aliasResult is not DefaultValueExpression)
			{
				return aliasResult;
			}

			return memberExpression;
		}

		public bool HandleAlias(IBuildContext context, Expression expression, ProjectFlags flags, [NotNullWhen(true)] out Expression? result)
		{
			result = null;

			if (expression is not MemberExpression memberExpression)
				return false;

			var ed = MappingSchema.GetEntityDescriptor(memberExpression.Expression!.Type);

			if (ed.Aliases == null)
				return false;

			var testedColumn = ed.Columns.FirstOrDefault(c =>
				MemberInfoComparer.Instance.Equals(c.MemberInfo, memberExpression.Member));

			if (testedColumn != null)
			{
				var otherColumns = ed.Aliases.Where(a =>
					a.Value == testedColumn.MemberName);

				foreach (var other in otherColumns)
				{
					var newResult = CheckForAlias(context, memberExpression, ed, other.Key, flags);
					if (!ReferenceEquals(newResult, memberExpression))
					{
						result = newResult;
						return true;
					}
				}
			}
			else
			{
				if (ed.Aliases.TryGetValue(memberExpression.Member.Name, out var alias))
				{
					var newResult = CheckForAlias(context, memberExpression, ed, alias, flags);
					if (!ReferenceEquals(newResult, memberExpression))
					{
						result = newResult;
						return true;
					}
				}
			}

			return false;
		}
	}
}
