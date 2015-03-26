using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	class QueryExpression : Expression
	{
		public QueryExpression(ClauseBuilderBase clauseBuilder)
		{
			AddClause(clauseBuilder);
		}

		readonly List<ClauseBuilderBase> _builders = new List<ClauseBuilderBase>();

		Type _type;

		public override Type           Type      { get { return _type; } }
		public override bool           CanReduce { get { return true;  } }
		public override ExpressionType NodeType  { get { return ExpressionType.Extension; } }

		public override Expression Reduce()
		{
			return _builders[_builders.Count - 1].Expression;
		}

		public QueryExpression AddClause(ClauseBuilderBase clauseBuilder)
		{
			_builders.Add(clauseBuilder);

			_type = clauseBuilder.Type;

			clauseBuilder.Query = this;

			return this;
		}

		public Func<IDataContext,Expression,IEnumerable<T>> BuildEnumerable<T>()
		{
			return null;
		}

		public Func<IDataContext,Expression,T> BuildElement<T>()
		{
			return null;
		}
	}
}
