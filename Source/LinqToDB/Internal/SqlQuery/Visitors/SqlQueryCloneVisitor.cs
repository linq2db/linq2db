using System;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public class SqlQueryCloneVisitor : SqlQueryCloneVisitorBase
	{
		Func<IQueryElement, bool>? _cloneFunc;

		public IQueryElement Clone(IQueryElement element, Func<IQueryElement, bool>? cloneFunc)
		{
			_cloneFunc = cloneFunc;

			return PerformClone(element);
		}

		public override void Cleanup()
		{
			base.Cleanup();
			_cloneFunc = null;
		}

		protected override bool ShouldReplace(IQueryElement element)
		{
			return base.ShouldReplace(element) || (_cloneFunc == null || _cloneFunc(element));
		}

	}
}
