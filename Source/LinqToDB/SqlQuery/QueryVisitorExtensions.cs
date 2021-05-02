using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlQuery
{
	public static class QueryVisitorExtensions
	{
		#region QueryVisitor
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
		#endregion

		#region Clone
		[return: NotNullIfNotNull("element")]
		public static T? Clone<T>(this T? element, Dictionary<IQueryElement, IQueryElement> objectTree)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			return new CloneVisitor<object?>(objectTree, null).Clone(element);
		}

		[return: NotNullIfNotNull("elements")]
		public static T[]? Clone<T>(this T[]? elements, Dictionary<IQueryElement, IQueryElement> objectTree)
			where T : class, IQueryElement
		{
			if (elements == null)
				return null;

			return new CloneVisitor<object?>(objectTree, null).Clone(elements);
		}

		[return: NotNullIfNotNull("element")]
		public static T? Clone<T, TContext>(this T? element, TContext context, Dictionary<IQueryElement, IQueryElement> objectTree, Func<TContext, IQueryElement, bool> doClone)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			return new CloneVisitor<TContext>(objectTree, context, doClone).Clone(element);
		}

		[return: NotNullIfNotNull("element")]
		public static T? Clone<T, TContext>(this T? element, TContext context, Func<TContext, IQueryElement, bool> doClone)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			return new CloneVisitor<TContext>(null, context, doClone).Clone(element);
		}

		[return: NotNullIfNotNull("element")]
		public static T? Clone<T>(this T? element, Func<IQueryElement, bool> doClone)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			return new CloneVisitor<object?>(null, doClone).Clone(element);
		}

		[return: NotNullIfNotNull("element")]
		public static T? Clone<T>(this T? element)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			return new CloneVisitor<object?>(null, null).Clone(element);
		}
		#endregion

		#region Convert
		public static T Convert<TContext, T>(this T element, TContext context, Func<ConvertVisitor<TContext>, IQueryElement, IQueryElement> convertAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<TContext>(context, convertAction, false, false).ConvertInternal(element) ?? element;
		}

		public static T Convert<T>(this T element, Func<ConvertVisitor<object?>, IQueryElement, IQueryElement> convertAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<object?>(null, convertAction, false, false).ConvertInternal(element) ?? element;
		}

		public static T Convert<TContext, T>(this T element, TContext context, bool allowMutation, Func<ConvertVisitor<TContext>, IQueryElement, IQueryElement> convertAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<TContext>(context, convertAction, false, allowMutation).ConvertInternal(element) ?? element;
		}

		public static T ConvertAll<TContext, T>(this T element, TContext context, Func<ConvertVisitor<TContext>, IQueryElement, IQueryElement> convertAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<TContext>(context, convertAction, true, false).ConvertInternal(element) ?? element;
		}

		public static T ConvertAll<TContext, T>(this T element, TContext context, Func<ConvertVisitor<TContext>, IQueryElement, IQueryElement> convertAction, Func<VisitArgs<TContext>, bool> parentAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<TContext>(context, convertAction, true, false, parentAction).ConvertInternal(element) ?? element;
		}

		public static T ConvertAll<TContext, T>(this T element, TContext context, bool allowMutation, Func<ConvertVisitor<TContext>, IQueryElement, IQueryElement> convertAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<TContext>(context, convertAction, true, allowMutation).ConvertInternal(element) ?? element;
		}

		public static T ConvertAll<T>(this T element, bool allowMutation, Func<ConvertVisitor<object?>, IQueryElement, IQueryElement> convertAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<object?>(null, convertAction, true, allowMutation).ConvertInternal(element) ?? element;
		}
		#endregion
	}
}
