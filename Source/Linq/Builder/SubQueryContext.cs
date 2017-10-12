using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class SubQueryContext : PassThroughContext
	{
		public SubQueryContext(IBuildContext subQuery, SelectQuery selectQuery, bool addToSql)
			: base(subQuery)
		{
			if (selectQuery == subQuery.SelectQuery)
				throw new ArgumentException("Wrong subQuery argument.", "subQuery");

			SubQuery        = subQuery;
			SubQuery.Parent = this;
			SelectQuery     = selectQuery;

			if (addToSql)
				selectQuery.From.Table(SubQuery.SelectQuery);
		}

		public SubQueryContext(IBuildContext subQuery, bool addToSql = true)
			: this(subQuery, new SelectQuery { ParentSelect = subQuery.SelectQuery.ParentSelect }, addToSql)
		{
		}

		public          IBuildContext SubQuery    { get; private set; }
		public override SelectQuery   SelectQuery { get; set; }
		public override IBuildContext Parent      { get; set; }

		public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			if (Expression.NodeType == ExpressionType.Lambda)
			{
				var le = (LambdaExpression)Expression;

				if (le.Parameters.Count == 2 ||
					le.Parameters.Count == 1 && null != Expression.Find(
						e => e.NodeType == ExpressionType.Call && ((MethodCallExpression)e).IsQueryable()))
				{
					if (le.Body.NodeType == ExpressionType.New)
					{
						var ne = (NewExpression)le.Body;
						var p  = Expression.Parameter(ne.Type, "p");

						var seq = new SelectContext(
							Parent,
							Expression.Lambda(
								Expression.New(
									ne.Constructor,
									(IEnumerable<Expression>)ne.Members.Select(m => Expression.MakeMemberAccess(p, m)),
									ne.Members),
								p),
							this);

						seq.BuildQuery(query, queryParameter);

						return;
					}

					if (le.Body.NodeType == ExpressionType.MemberInit)
					{
						var mi = (MemberInitExpression)le.Body;

						if (mi.NewExpression.Arguments.Count == 0 && mi.Bindings.All(b => b is MemberAssignment))
						{
							var p = Expression.Parameter(mi.Type, "p");

							var seq = new SelectContext(
								Parent,
								Expression.Lambda(
								Expression.MemberInit(
									mi.NewExpression,
									(IEnumerable<MemberBinding>)mi.Bindings
										.OfType<MemberAssignment>()
										.Select(ma => Expression.Bind(ma.Member, Expression.MakeMemberAccess(p, ma.Member)))),
									p),
								this);

							seq.BuildQuery(query, queryParameter);

							return;
						}
					}
				}
			}

			base.BuildQuery(query, queryParameter);
		}

		public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
		{
			return SubQuery
				.ConvertToIndex(expression, level, flags)
				.Select(idx => new SqlInfo(idx.Members) { Sql = SubQuery.SelectQuery.Select.Columns[idx.Index] })
				.ToArray();
		}

		// JoinContext has similar logic. Consider to review it.
		//
		public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
		{
			return ConvertToSql(expression, level, flags)
				.Select(idx =>
				{
					idx.Query = SelectQuery;
					idx.Index = GetIndex((SelectQuery.Column)idx.Sql);

					return idx;
				})
				.ToArray();
		}

		public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor testFlag)
		{
			switch (testFlag)
			{
				case RequestFor.SubQuery : return IsExpressionResult.True;
			}

			return base.IsExpression(expression, level, testFlag);
		}

		protected internal readonly Dictionary<ISqlExpression,int> ColumnIndexes = new Dictionary<ISqlExpression,int>();

		protected virtual int GetIndex(SelectQuery.Column column)
		{
			int idx;

			if (!ColumnIndexes.TryGetValue(column, out idx))
			{
				idx = SelectQuery.Select.Add(column);
				ColumnIndexes.Add(column, idx);
			}

			return idx;
		}

		public override int ConvertToParentIndex(int index, IBuildContext context)
		{
			var idx = GetIndex(context.SelectQuery.Select.Columns[index]);
			return Parent == null ? idx : Parent.ConvertToParentIndex(idx, this);
		}

		public override void SetAlias(string alias)
		{
			if (alias == null)
				return;

#if NETFX_CORE
			if (alias.Contains("<"))
#else
			if (alias.Contains('<'))
#endif
				return;

			if (SelectQuery.From.Tables[0].Alias == null)
				SelectQuery.From.Tables[0].Alias = alias;
		}

		public override ISqlExpression GetSubQuery(IBuildContext context)
		{
			return null;
		}
	}
}
