using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlQuery
{
	using Common.Internal;
	using Visitors;

	public static class QueryVisitorExtensions
	{
		internal static readonly ObjectPool<SqlQueryFindVisitor>   FindVisitorPool        = new(() => new SqlQueryFindVisitor(),        v => v.Cleanup(), 100);
		internal static readonly ObjectPool<SqlQueryActionVisitor> ActionVisitorPool      = new(() => new SqlQueryActionVisitor(),      v => v.Cleanup(), 100);
		internal static readonly ObjectPool<SqlQueryParentFirstVisitor> ParentVisitorPool = new(() => new SqlQueryParentFirstVisitor(), v => v.Cleanup(), 100);
		internal static readonly ObjectPool<SqlQueryCloneVisitor>  CloneVisitorPool       = new(() => new SqlQueryCloneVisitor(),       v => v.Cleanup(), 100);

		class PoolHolder<TContext>
		{
			public static readonly ObjectPool<SqlQueryFindVisitor<TContext>>   FindPool             = new(() => new SqlQueryFindVisitor<TContext>(),        v => v.Cleanup(), 100);
			public static readonly ObjectPool<SqlQueryActionVisitor<TContext>> ActionPool           = new(() => new SqlQueryActionVisitor<TContext>(),      v => v.Cleanup(), 100);
			public static readonly ObjectPool<SqlQueryParentFirstVisitor<TContext>> ParentFirstPool = new(() => new SqlQueryParentFirstVisitor<TContext>(), v => v.Cleanup(), 100);
			public static readonly ObjectPool<SqlQueryCloneVisitor<TContext>>  ClonePool            = new(() => new SqlQueryCloneVisitor<TContext>(),       v => v.Cleanup(), 100);
		}

		#region Visit
		public static void Visit<TContext>(this IQueryElement element, TContext context, Action<TContext, IQueryElement> action)
		{
			using var actionVisitor = PoolHolder<TContext>.ActionPool.Allocate();
			actionVisitor.Value.Visit(context, element, false, action);
		}

		public static void Visit(this IQueryElement element, Action<IQueryElement> action)
		{
			using var actionVisitor = ActionVisitorPool.Allocate();
			actionVisitor.Value.Visit(element, false, action);
		}

		public static void VisitAll<TContext>(this IQueryElement element, TContext context, Action<TContext, IQueryElement> action)
		{
			using var actionVisitor = PoolHolder<TContext>.ActionPool.Allocate();
			actionVisitor.Value.Visit(context, element, true, action);
		}

		public static void VisitAll(this IQueryElement element, Action<IQueryElement> action)
		{
			using var actionVisitor = ActionVisitorPool.Allocate();
			actionVisitor.Value.Visit(element, true, action);
		}

		#endregion

		#region VisitParent
		public static void VisitParentFirst<TContext>(this IQueryElement element, TContext context, Func<TContext, IQueryElement, bool> action)
		{
			using var actionVisitor = PoolHolder<TContext>.ParentFirstPool.Allocate();
			actionVisitor.Value.Visit(context, element, false, action);
		}

		public static void VisitParentFirst(this IQueryElement element, Func<IQueryElement, bool> action)
		{
			using var actionVisitor = ParentVisitorPool.Allocate();
			actionVisitor.Value.Visit(element, false, action);
		}

		public static void VisitParentFirstAll<TContext>(this IQueryElement element, TContext context, Func<TContext, IQueryElement, bool> action)
		{
			using var actionVisitor = PoolHolder<TContext>.ParentFirstPool.Allocate();
			actionVisitor.Value.Visit(context, element, true, action);
		}

		public static void VisitParentFirstAll(this IQueryElement element, Func<IQueryElement, bool> action)
		{
			using var actionVisitor = ParentVisitorPool.Allocate();
			actionVisitor.Value.Visit(element, true, action);
		}

		#endregion

		#region Find
		public static IQueryElement? Find<TContext>(this IQueryElement? element, TContext context, Func<TContext, IQueryElement, bool> find)
		{
			if (element == null)
				return null;

			using var findVisitor = PoolHolder<TContext>.FindPool.Allocate();
			return findVisitor.Value.Find(context, element, find);
		}

		public static IQueryElement? Find(this IQueryElement? element, Func<IQueryElement, bool> find)
		{
			if (element == null)
				return null;

			using var findVisitor = FindVisitorPool.Allocate();
			return findVisitor.Value.Find(element, find);
		}

		public static IQueryElement? Find(this IQueryElement? element, QueryElementType type)
		{
			if (element == null)
				return null;

			using var findVisitor = PoolHolder<QueryElementType>.FindPool.Allocate();
			return findVisitor.Value.Find(type, element, static (type, e) => e.ElementType == type);
		}
		#endregion

		#region Clone
		[return: NotNullIfNotNull(nameof(element))]
		public static T? Clone<T>(this T? element, Dictionary<IQueryElement, IQueryElement> objectTree)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			using var cloneVisitor = CloneVisitorPool.Allocate();
			cloneVisitor.Value.RegisterReplacements(objectTree);

			var clone = (T)cloneVisitor.Value.PerformClone(element);

			cloneVisitor.Value.GetReplacements(objectTree);

			return clone;
		}

		public static void CloneInto<T>(this IReadOnlyList<T> source, IList<T> target, Dictionary<IQueryElement, IQueryElement> objectTree)
			where T : class, IQueryElement
		{
			new CloneVisitor<object?>(objectTree, null).CloneInto(target, source);
		}

        [return: NotNullIfNotNull(nameof(elements))]
		public static T[]? Clone<T>(this T[]? elements, Dictionary<IQueryElement, IQueryElement> objectTree)
			where T : class, IQueryElement
		{
			if (elements == null)
				return null;

			return new CloneVisitor<object?>(objectTree, null).Clone(elements);
		}

		[return: NotNullIfNotNull(nameof(element))]
		public static T? Clone<T, TContext>(this T? element, TContext context, Dictionary<IQueryElement, IQueryElement> objectTree, Func<TContext, IQueryElement, bool> doClone)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			using var cloneVisitor = PoolHolder<TContext>.ClonePool.Allocate();
			cloneVisitor.Value.RegisterReplacements(objectTree);

			var clone = (T)cloneVisitor.Value.Clone(element, context, doClone);

			cloneVisitor.Value.GetReplacements(objectTree);

			return clone;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public static T? Clone<T, TContext>(this T? element, TContext context, Func<TContext, IQueryElement, bool> doClone)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			using var cloneVisitor = PoolHolder<TContext>.ClonePool.Allocate();

			return (T)cloneVisitor.Value.Clone(element, context, doClone);
		}

		[return: NotNullIfNotNull(nameof(element))]
		public static T? Clone<T>(this T? element, Func<IQueryElement, bool> doClone)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			using var cloneVisitor = CloneVisitorPool.Allocate();

			return (T)cloneVisitor.Value.Clone(element, doClone);
		}

		[return: NotNullIfNotNull(nameof(element))]
		public static T? Clone<T>(this T? element)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			using var cloneVisitor = CloneVisitorPool.Allocate();

			return (T)cloneVisitor.Value.Clone(element, null);
		}
		#endregion

		#region Convert
		public static T Convert<TContext, T>(this T element, TContext context, Func<ConvertVisitor<TContext>, IQueryElement, IQueryElement> convertAction, bool withStack)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<TContext>(context, convertAction, false, false, withStack).ConvertInternal(element) ?? element;
		}

		public static T Convert<TContext, T>(this T element, TContext context, Func<ConvertVisitor<TContext>, IQueryElement, IQueryElement> convertAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<TContext>(context, convertAction, false, false, false).ConvertInternal(element) ?? element;
		}

		public static T Convert<T>(this T element, Func<ConvertVisitor<object?>, IQueryElement, IQueryElement> convertAction, bool withStack)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<object?>(null, convertAction, false, false, withStack).ConvertInternal(element) ?? element;
		}

		public static T Convert<T>(this T element, Func<ConvertVisitor<object?>, IQueryElement, IQueryElement> convertAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<object?>(null, convertAction, false, false, false).ConvertInternal(element) ?? element;
		}

		public static T Convert<TContext, T>(this T element, TContext context, bool allowMutation, Func<ConvertVisitor<TContext>, IQueryElement, IQueryElement> convertAction, bool withStack)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<TContext>(context, convertAction, false, allowMutation, withStack).ConvertInternal(element) ?? element;
		}

		public static T Convert<TContext, T>(this T element, TContext context, bool allowMutation, Func<ConvertVisitor<TContext>, IQueryElement, IQueryElement> convertAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<TContext>(context, convertAction, false, allowMutation, false).ConvertInternal(element) ?? element;
		}

		public static T ConvertAll<TContext, T>(this T element, TContext context, Func<ConvertVisitor<TContext>, IQueryElement, IQueryElement> convertAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<TContext>(context, convertAction, true, false, false).ConvertInternal(element) ?? element;
		}

		public static T ConvertAll<TContext, T>(this T element, TContext context, Func<ConvertVisitor<TContext>, IQueryElement, IQueryElement> convertAction, Func<ConvertVisitor<TContext>, bool> parentAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<TContext>(context, convertAction, true, false, false, parentAction).ConvertInternal(element) ?? element;
		}

		public static T ConvertAll<TContext, T>(this T element, TContext context, bool allowMutation, Func<ConvertVisitor<TContext>, IQueryElement, IQueryElement> convertAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<TContext>(context, convertAction, true, allowMutation, false).ConvertInternal(element) ?? element;
		}

		public static T ConvertAll<T>(this T element, bool allowMutation, Func<ConvertVisitor<object?>, IQueryElement, IQueryElement> convertAction)
			where T : class, IQueryElement
		{
			return (T?)new ConvertVisitor<object?>(null, convertAction, true, allowMutation, false).ConvertInternal(element) ?? element;
		}
		#endregion
	}
}
