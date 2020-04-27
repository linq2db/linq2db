using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}, T: {BuildContextDebuggingHelper.GetContextInfo(TableContext)}")]
	class AssociationContext : IBuildContext
	{
#if DEBUG
		string? IBuildContext._sqlQueryText => TableContext._sqlQueryText;
		public string Path => this.GetPath();
#endif
		public ExpressionBuilder Builder { get; }
		public Expression? Expression { get; }

		public SelectQuery SelectQuery
		{
			// get => TableContext.SelectQuery;
			get => SubqueryContext.SelectQuery;
			set => throw new NotImplementedException();
		}

		public SqlStatement? Statement 
		{ 
			get => SubqueryContext.Statement;
			set => SubqueryContext.Statement = value;
		}

		public IBuildContext? Parent { get; set; }

		public IBuildContext TableContext { get; }
		public IBuildContext SubqueryContext { get; }
		public SqlJoinedTable Join { get; }

		public AssociationContext(ExpressionBuilder builder, IBuildContext tableContext, IBuildContext subqueryContext, SqlJoinedTable join)
		{
			Builder = builder;
			TableContext = tableContext;
			SubqueryContext = subqueryContext;
			Join = join;
			SubqueryContext.Parent = this;
			Parent = tableContext;
		}

		public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			SubqueryContext.BuildQuery(query, queryParameter);
		}

		static Expression? CorrectExpression(Expression? expression, IBuildContext current, IBuildContext underlying)
		{
			if (expression != null)
			{
				var root = expression.GetRootObject(current.Builder.MappingSchema);
				if (root is ContextRefExpression refExpression)
				{
					if (refExpression.BuildContext == current)
					{
						expression = expression.Replace(root, new ContextRefExpression(root.Type, underlying));
					};
				}
			}

			return expression;
		}

		public void MarkNotWeak()
		{
			// Join.IsWeak = false;
		}

		public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
		{
			MarkNotWeak();
			expression = CorrectExpression(expression, this, SubqueryContext);
			return SubqueryContext.BuildExpression(expression, level, enforceServerSide);
		}

		public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			expression  = CorrectExpression(expression, this, SubqueryContext);
			var indexes = ConvertToIndex(expression, level, flags);
			foreach (var sqlInfo in indexes)
			{
				sqlInfo.Query = SelectQuery;
			}

			return indexes;
		}

		public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			MarkNotWeak();

			expression = CorrectExpression(expression, this, SubqueryContext);

			var indexes = SubqueryContext
				.ConvertToIndex(expression, level, flags)
				.ToArray();

			// foreach (var sqlInfo in indexes)
			// {
			// 	sqlInfo.Index = SelectQuery.Select.Add(sqlInfo.Sql);
			// 	sqlInfo.Sql   = SelectQuery.Select.Columns[sqlInfo.Index];
			// 	sqlInfo.Query = SelectQuery;
			//
			// }

			return indexes;
		}

		public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			expression = CorrectExpression(expression, this, SubqueryContext);
			return SubqueryContext.IsExpression(expression, level, requestFlag);
		}

		public IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			//???
			expression = CorrectExpression(expression, this, SubqueryContext);
			return SubqueryContext.GetContext(expression, level, buildInfo);
		}

		readonly Dictionary<ISqlExpression,int> _columnIndexes = new Dictionary<ISqlExpression,int>();

		int GetIndex(SqlColumn column)
		{
			if (!_columnIndexes.TryGetValue(column, out var idx))
			{
				idx = SelectQuery.Select.Add(column);
				_columnIndexes.Add(column, idx);
			}

			return idx;
		}


		public int ConvertToParentIndex(int index, IBuildContext context)
		{
			MarkNotWeak();

			if (context != null)
			{
				if (context.SelectQuery != SelectQuery)
					index = SelectQuery.Select.Add(context.SelectQuery.Select.Columns[index]);
			}
			return TableContext.ConvertToParentIndex(index, this);
		}

		public void SetAlias(string alias)
		{
			//TODO
		}

		public ISqlExpression? GetSubQuery(IBuildContext context)
		{
			return null;
		}

		public SqlStatement GetResultStatement()
		{
			return SubqueryContext.GetResultStatement();
		}
	}
}
