using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq
{
	abstract class QueryableMemberAccessor
	{
		public Expression Expression { get; protected set; } = null!;
		public abstract Expression Execute(MemberInfo mi, IDataContext ctx);
	}

	class QueryableMemberAccessor<TContext> : QueryableMemberAccessor
	{
		private readonly TContext                                             _context;
		private readonly Func<TContext, MemberInfo, IDataContext, Expression> _accessor;

		public QueryableMemberAccessor(TContext context, Expression expression, Func<TContext, MemberInfo, IDataContext, Expression> accessor)
		{
			_context    = context;
			Expression = expression;
			_accessor   = accessor;
		}

		public override Expression Execute(MemberInfo mi, IDataContext ctx) => _accessor(_context, mi, ctx);
	}
}
