using System;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class SubQueryClause : BaseClause, IQuerySource
	{
		private readonly IQuerySource _querySource;
		public Sequence Sequence { get; }

		public SubQueryClause([NotNull] Sequence sequence)
		{
			_querySource = sequence.GetQuerySource();
			Sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
			QuerySourceId = QuerySourceHelper.GetNexSourceId();
		}

		public int QuerySourceId { get; }
		public Type ItemType => _querySource.ItemType;
		public string ItemName => _querySource.ItemName;

		public override BaseClause Visit(Func<BaseClause, BaseClause> func)
		{
			var sequence = (Sequence)Sequence.Visit(func);
			var current = this;
			if (sequence != Sequence)
				current = new SubQueryClause(sequence);
			return func(current);
		}

		public override bool VisitParentFirst(Func<BaseClause, bool> func)
		{
			return func(this) && Sequence.VisitParentFirst(func);
		}

		public bool DoesContainMember(MemberInfo memberInfo)
		{
			throw new NotImplementedException();
		}

		public ISqlExpression ConvertToSql(ISqlTableSource tableSource, Expression ma)
		{
			throw new NotImplementedException();
		}
	}
}
