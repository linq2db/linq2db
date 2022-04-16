using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	using Common;

	public class AliasesContext
	{
		readonly HashSet<IQueryElement> _aliasesSet = new (Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

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

		public ISet<string> GetUsedTableAliases()
		{
			return new HashSet<string>(_aliasesSet.Where(e => e.ElementType == QueryElementType.TableSource)
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
