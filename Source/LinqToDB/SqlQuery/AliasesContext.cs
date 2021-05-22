using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	using Common;

	public class AliasesContext
	{
		HashSet<IQueryElement> _aliasesSet = new HashSet<IQueryElement>(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

		public void RegisterAliased(IQueryElement element)
		{
			_aliasesSet.Add(element);
		}

		public void RegisterAliased(IReadOnlyCollection<IQueryElement> elements)
		{
			_aliasesSet.AddRange(elements);
		}

		public bool IsAliased(IQueryElement element)
		{
			return _aliasesSet.Contains(element);
		}

#if NET45
		public ICollection<IQueryElement> GetAliased()
#else
		public IReadOnlyCollection<IQueryElement> GetAliased()
#endif
		{
			return _aliasesSet;
		}

		public HashSet<string> GetUsedTableAliases()
		{
			return new(_aliasesSet.Where(e => e.ElementType == QueryElementType.TableSource)
					.Select(e => ((SqlTableSource)e).Alias!),
				StringComparer.OrdinalIgnoreCase);

		}

		public SqlParameter[] GetParameters()
		{
			return _aliasesSet.Where(e => e.ElementType == QueryElementType.SqlParameter)
				.Select(e => (SqlParameter)e).ToArray();
		}
	}
}
