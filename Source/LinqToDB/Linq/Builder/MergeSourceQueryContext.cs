using LinqToDB.SqlQuery;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	class MergeSourceQueryContext : SubQueryContext
	{
		private readonly SqlMergeStatement _merge;

		public MergeSourceQueryContext(SqlMergeStatement merge, IBuildContext sourceContext)
			: base(sourceContext, new SelectQuery { ParentSelect = sourceContext.SelectQuery }, true)
		{
			_merge = merge;

			_merge.Source = sourceContext is EnumerableContext enumerableSource
				? new SqlMergeSourceTable() { SourceEnumerable = enumerableSource.Table }
				: new SqlMergeSourceTable() { SourceQuery = sourceContext.SelectQuery };

			if (SubQuery is SelectContext select)
				select.AllowAddDefault = false;
		}

		public void MatchBuilt()
		{
			// for table source, we should build all associations, used in operations as left joins to not affect
			// number of records, returned by source, if association had inner join configured
			// associations, used in match, should use their original join type
			if (SubQuery is TableBuilder.TableContext table)
				table.ForceLeftJoinAssociations = true;
		}

		public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			return SubQuery
				.ConvertToIndex(expression, level, flags)
				.Select(info =>
				{
					var expr  = (info.Sql is SqlColumn column) ? column.Expression : info.Sql;
					var field = RegisterSourceField(expr, expr, info.Index, info.MemberChain.LastOrDefault());

					return new SqlInfo(info.MemberChain)
					{
						Sql = field
					};
				})
				.ToArray();
		}

		SqlField RegisterSourceField(ISqlExpression baseExpression, ISqlExpression expression, int index, MemberInfo member)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			var sourceField = _merge.Source.RegisterSourceField(baseExpression, expression, index, () =>
			{
				var f = QueryHelper.GetUnderlyingField(baseExpression ?? expression);

				var newField = f == null
					? new SqlField(expression.SystemType!, member?.Name, expression.CanBeNull)
					: new SqlField(f) { Name = member?.Name ?? f.Name};

				newField.PhysicalName = newField.Name;
				newField.Table        = _merge.Source;
				return newField;
			});

			return sourceField;
		}

		public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor testFlag)
		{
			return base.IsExpression(expression, level, testFlag);
		}
	}
}
