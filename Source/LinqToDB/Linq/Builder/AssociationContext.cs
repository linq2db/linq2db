using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Mapping;
	using SqlQuery;

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}, T: {BuildContextDebuggingHelper.GetContextInfo(TableContext)}")]
	class AssociationContext : IBuildContext
	{
#if DEBUG
		string? IBuildContext._sqlQueryText => TableContext._sqlQueryText;
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
		public IBuildContext         SubqueryContext { get; }
		public SqlJoinedTable        Join            { get; }

		public AssociationContext(ExpressionBuilder builder, AssociationDescriptor descriptor, IBuildContext tableContext, IBuildContext subqueryContext, SqlJoinedTable join)
		{
			Builder                = builder;
			Descriptor             = descriptor;
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

			indexes = indexes.Select(idx => idx.WithQuery(SelectQuery))
				.ToArray();

			return indexes;
		}

		public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, SubqueryContext);

			var indexes = SubqueryContext
				.ConvertToIndex(expression, level, flags)
				.ToArray();

			var corrected = indexes.Select(s => s.WithSql(SubqueryContext.SelectQuery.Select.Columns[s.Index]))
				.ToArray();

			return corrected;
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

		public void CompleteColumns()
		{
		}
	}
}
