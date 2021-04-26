using System;

namespace LinqToDB.SqlQuery
{
	public static class QueryVisitorExtensions
	{
		public static void Visit<TContext>(this IQueryElement element, TContext context, Action<TContext, IQueryElement> action)
		{
			new QueryVisitor<TContext>(context).Visit(element, action);
		}

		public static void Visit(this IQueryElement element, Action<object?, IQueryElement> action)
		{
			new QueryVisitor<object?>(null).Visit(element, action);
		}

		public static void VisitAll<TContext>(this IQueryElement element, TContext context, Action<TContext, IQueryElement> action)
		{
			new QueryVisitor<TContext>(context).VisitAll(element, action);
		}

		public static void VisitAll(this IQueryElement element, Action<object?, IQueryElement> action)
		{
			new QueryVisitor<object?>(null).VisitAll(element, action);
		}

		public static void VisitParentFirst<TContext>(this IQueryElement element, TContext context, Func<TContext, IQueryElement, bool> action)
		{
			new QueryVisitor<TContext>(context).VisitParentFirst(element, action);
		}

		public static void VisitParentFirst(this IQueryElement element, Func<object?, IQueryElement, bool> action)
		{
			new QueryVisitor<object?>(null).VisitParentFirst(element, action);
		}

		public static void VisitParentFirstAll<TContext>(this IQueryElement element, TContext context, Func<TContext, IQueryElement, bool> action)
		{
			new QueryVisitor<TContext>(context).VisitParentFirstAll(element, action);
		}

		public static void VisitParentFirstAll(this IQueryElement element, Func<object?, IQueryElement, bool> action)
		{
			new QueryVisitor<object?>(null).VisitParentFirstAll(element, action);
		}

		public static IQueryElement? Find<TContext>(this IQueryElement? element, TContext context, Func<TContext, IQueryElement, bool> find)
		{
			if (element == null)
				return null;

			return new QueryVisitor<TContext>(context).Find(element, find);
		}

		public static IQueryElement? Find(this IQueryElement? element, Func<object?, IQueryElement, bool> find)
		{
			if (element == null)
				return null;

			return new QueryVisitor<object?>(null).Find(element, find);
		}
	}
}
