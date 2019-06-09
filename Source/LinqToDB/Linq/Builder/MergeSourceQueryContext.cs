using JetBrains.Annotations;
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

		public MergeSourceQueryContext(
			ExpressionBuilder builder,
			BuildInfo buildInfo,
			SqlMergeStatement merge,
			IBuildContext sourceContext,
			Type sourceType)
			:base(sourceContext, new SelectQuery { ParentSelect = sourceContext.SelectQuery }, true)
		{
			_merge = merge;

			_merge.Source = new SqlMergeSourceTable()
			{
				SourceQuery = sourceContext.SelectQuery
			};

			if (SubQuery is SelectContext select)
				select.AllowAddDefault = false;
		}

		public MergeSourceQueryContext(
			ExpressionBuilder builder,
			BuildInfo buildInfo,
			SqlMergeStatement merge,
			EnumerableContext sourceContext,
			Type sourceType,
			bool _)
			: base(sourceContext, new SelectQuery { ParentSelect = sourceContext.SelectQuery }, true)
		{
			_merge = merge;
			//_merge.Source = new SqlMergeSourceTable(builder.MappingSchema, _merge, sourceType)
			_merge.Source = new SqlMergeSourceTable()
			{
				SourceEnumerable = sourceContext.Table
			};

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

		public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
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

		SqlField RegisterSourceField(ISqlExpression baseExpression, [NotNull] ISqlExpression expression, int index, MemberInfo member)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			var sourceField = _merge.Source.RegisterSourceField(baseExpression, expression, index, () =>
			{
				var f = QueryHelper.GetUnderlyingField(baseExpression ?? expression);

				var newField = f == null
					? new SqlField()  { SystemType = expression.SystemType, CanBeNull = expression.CanBeNull, Name = member?.Name }
					: new SqlField(f) { Name = member?.Name ?? f.Name};

				newField.PhysicalName = newField.Name;
				newField.Table        = _merge.Source;
				return newField;
			});

			return sourceField;
		}
	}
}
