﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	class TableLikeQueryContext : SubQueryContext
	{
		public SqlTableLikeSource Source { get; }

		public TableLikeQueryContext(IBuildContext sourceContext)
			: base(sourceContext, new SelectQuery { ParentSelect = sourceContext.SelectQuery }, true)
		{
			Source = sourceContext is EnumerableContext enumerableSource
				? new SqlTableLikeSource { SourceEnumerable = enumerableSource.Table }
				: new SqlTableLikeSource { SourceQuery = sourceContext.SelectQuery };

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
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			expression = SequenceHelper.CorrectExpression(expression, this, SubQuery);

			return SubQuery
				.ConvertToIndex(expression, level, flags)
				.Select(info =>
				{
					var expr  = (info.Sql is SqlColumn column) ? column.Expression : info.Sql;
					var field = RegisterSourceField(expr, expr, info.Index, info.MemberChain.LastOrDefault());

					return new SqlInfo(info.MemberChain, field);
				})
				.ToArray();
		}

		SqlField RegisterSourceField(ISqlExpression baseExpression, ISqlExpression expression, int index, MemberInfo member)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			var sourceField = Source.RegisterSourceField(baseExpression, expression, index, () =>
			{
				var f = QueryHelper.GetUnderlyingField(baseExpression ?? expression);

				var newField = f == null
					? new SqlField(expression.SystemType!, member?.Name, expression.CanBeNull)
					: new SqlField(f) { Name = member?.Name ?? f.Name};

				newField.PhysicalName = newField.Name;
				newField.Table        = Source;
				return newField;
			});

			return sourceField;
		}

		public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			return base.IsExpression(expression, level, requestFlag);
		}
	}
}
