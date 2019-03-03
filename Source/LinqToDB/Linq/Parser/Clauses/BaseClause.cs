using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Linq.Parser.Clauses
{
	public abstract class BaseClause
	{

		protected bool VisitListParentFirst<T>(IList<T> list, Func<BaseClause, bool> func)
			where T: BaseClause
		{
			foreach (var item in list)
			{
				if (!item.VisitParentFirst(func))
					return false;
			}

			return true;
		}

		protected List<T> VisitList<T>(List<T> list, Func<BaseClause, BaseClause> func)
			where T: BaseClause
		{
			List<T> current = null;
			for (int i = 0; i < list.Count; i++)
			{
				var item = list[i];
				if (item != null)
				{
					var newItem = (T)item.Visit(func);
					if (newItem != item)
					{
						if (current == null)
							current = new List<T>(list.Take(i));
						item = newItem;
					}
				}

				current?.Add(item);
			}

			return current ?? list;
		}

		public abstract BaseClause Visit(Func<BaseClause, BaseClause> func);
		public abstract bool VisitParentFirst(Func<BaseClause, bool> func);
	}
}
