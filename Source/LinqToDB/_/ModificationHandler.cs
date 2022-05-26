using LinqToDB.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB
{
	public class ModificationHandler<T> : IModificationHandler<T> where T : class
	{
		public ModificationHandler(IDataContext dataContext, Expression<Func<T, bool>> predicate) : this(dataContext.GetTable<T>(), predicate) { }

		public ModificationHandler(ITable<T> table, Expression<Func<T, bool>> predicate)
		{
			Table = table;
			Predicate = predicate;
		}

		public Type EntityType { get; } = typeof(T);
		public IDictionary<LambdaExpression, object> ExpressionValues { get; } = new Dictionary<LambdaExpression, object>();
		public Expression<Func<T, bool>> Predicate { get; set; }
		public ITable<T> Table { get; }

	}
}
