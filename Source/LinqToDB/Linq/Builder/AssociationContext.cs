﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using System.Collections.Generic;
	using Mapping;
	using SqlQuery;

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}, T: {BuildContextDebuggingHelper.GetContextInfo(TableContext)}")]
	sealed class AssociationContext : IBuildContext
	{
#if DEBUG
		string? IBuildContext.SqlQueryText => TableContext.SqlQueryText;
		public string Path => this.GetPath();
#endif
		public ExpressionBuilder Builder { get; }
		public Expression?       Expression { get; }

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

		public IBuildContext         TableContext    { get; }
		public AssociationDescriptor Descriptor      { get; }
		public List<LoadWithInfo[]>? CurrentLoadWith { get; }
		public IBuildContext         SubqueryContext { get; }
		public SqlJoinedTable        Join            { get; }

		public AssociationContext(ExpressionBuilder builder, AssociationDescriptor descriptor, List<LoadWithInfo[]>? currentLoadWith, IBuildContext tableContext, IBuildContext subqueryContext, SqlJoinedTable join)
		{
			Builder                = builder;
			Descriptor             = descriptor;
			CurrentLoadWith        = currentLoadWith;
			TableContext           = tableContext;
			SubqueryContext        = subqueryContext;
			Join                   = join;
			SubqueryContext.Parent = this;
			Parent                 = tableContext;
		}

		public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			SubqueryContext.BuildQuery(query, queryParameter);
		}

		public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, SubqueryContext);
			return SubqueryContext.BuildExpression(expression, level, enforceServerSide);
		}

		public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			expression  = SequenceHelper.CorrectExpression(expression, this, SubqueryContext);
			var indexes = ConvertToIndex(expression, level, flags);

			for (var i = 0; i < indexes.Length; i++)
				indexes[i] = indexes[i].WithQuery(SelectQuery);

			return indexes;
		}

		public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			expression  = SequenceHelper.CorrectExpression(expression, this, SubqueryContext);
			var indexes = SubqueryContext.ConvertToIndex(expression, level, flags);
			var isOuter = SubqueryContext is DefaultIfEmptyBuilder.DefaultIfEmptyContext defaultIfEmpty && !defaultIfEmpty.Disabled && !Builder.DisableDefaultIfEmpty;

			for (var i = 0; i < indexes.Length; i++)
			{
				var index  = indexes[i];
				indexes[i] = index = index.WithSql(SubqueryContext.SelectQuery.Select.Columns[index.Index]);

				// force nullability
				if (isOuter && !index.Sql.CanBeNull)
					indexes[i] = index.WithSql(new SqlExpression(index.Sql.SystemType, "{0}", index.Sql.Precedence, index.Sql) { CanBeNull = true });
			}

			return indexes;
		}

		public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, SubqueryContext);
			return SubqueryContext.IsExpression(expression, level, requestFlag);
		}

		public IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			//???
			expression = SequenceHelper.CorrectExpression(expression, this, SubqueryContext);
			return SubqueryContext.GetContext(expression, level, buildInfo);
		}

		public int ConvertToParentIndex(int index, IBuildContext context)
		{
			if (context != null)
			{
				if (context.SelectQuery != SelectQuery)
					index = SelectQuery.Select.Add(context.SelectQuery.Select.Columns[index]);
			}
			return TableContext.ConvertToParentIndex(index, this);
		}

		public void SetAlias(string? alias)
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

		public void CompleteColumns()
		{
		}

		public bool IsCompatibleLoadWith()
		{
			if (TableContext is not TableBuilder.TableContext tc)
				return false;
			if (!ReferenceEquals(CurrentLoadWith, tc.LoadWith))
				return false;

			return true;
		}
	}
}
