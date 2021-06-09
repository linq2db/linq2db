using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlQuery
{
	public static class QueryVisitorExtensions
	{
		#region Visit
		public static void Visit<TContext>(this IQueryElement element, TContext context, Action<TContext, IQueryElement> action)
		{
			new QueryVisitor<TContext>(context, false, action).Visit(element);
		}

		public static void Visit(this IQueryElement element, Action<IQueryElement> action)
		{
			new QueryVisitor<object?>(false, action).Visit(element);
		}

		public static void VisitAll<TContext>(this IQueryElement element, TContext context, Action<TContext, IQueryElement> action)
		{
			new QueryVisitor<TContext>(context, true, action).Visit(element);
		}

		public static void VisitAll(this IQueryElement element, Action<IQueryElement> action)
		{
			new QueryVisitor<object?>(true, action).Visit(element);
		}

		#endregion

		#region VisitParent
		public static void VisitParentFirst<TContext>(this IQueryElement element, TContext context, Func<TContext, IQueryElement, bool> action)
		{
			new QueryParentVisitor<TContext>(context, false, action).Visit(element);
		}

		public static void VisitParentFirst(this IQueryElement element, Func<IQueryElement, bool> action)
		{
			new QueryParentVisitor<object?>(false, action).Visit(element);
		}

		public static void VisitParentFirstAll<TContext>(this IQueryElement element, TContext context, Func<TContext, IQueryElement, bool> action)
		{
			new QueryParentVisitor<TContext>(context, true, action).Visit(element);
		}

		public static void VisitParentFirstAll(this IQueryElement element, Func<IQueryElement, bool> action)
		{
			new QueryParentVisitor<object?>(true, action).Visit(element);
		}
		#endregion

		#region Find
		public static IQueryElement? Find<TContext>(this IQueryElement? element, TContext context, Func<TContext, IQueryElement, bool> find)
		{
			if (element == null)
				return null;

			return new QueryFindVisitor<TContext>(context, find).Find(element);
		}

		public static IQueryElement? Find(this IQueryElement? element, Func<IQueryElement, bool> find)
		{
			if (element == null)
				return null;

			return new QueryFindVisitor<object?>(find).Find(element);
		}

		public static IQueryElement? Find(this IQueryElement? element, QueryElementType type)
		{
			if (element == null)
				return null;

			return new QueryFindVisitor<QueryElementType>(type, static (type, e) => e.ElementType == type).Find(element);
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
