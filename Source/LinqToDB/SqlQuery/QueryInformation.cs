using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace LinqToDB.SqlQuery
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public class QueryInformation
	{
		private readonly SelectQuery                       _rootQuery;
		private Dictionary<SelectQuery, HierarchyInfo>     _parents;
		private Dictionary<SelectQuery, List<SelectQuery>> _tree;

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public QueryInformation([NotNull] SelectQuery rootQuery)
		{
			_rootQuery = rootQuery ?? throw new ArgumentNullException(nameof(rootQuery));
		}

		/// <summary>
		/// Returns parent query if query is subquery for select
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <returns></returns>
		public SelectQuery GetParentQuery(SelectQuery selectQuery)
		{
			var info = GetHierarchyInfo(selectQuery);
			return info?.HierarchyType == HierarchyType.From ? info.MasterQuery : null;
		}

		/// <summary>
		/// Returns HirarchyInfo for specific selectQuery
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <returns></returns>
		public HierarchyInfo GetHierarchyInfo(SelectQuery selectQuery)
		{
			CheckInitialized();
			_parents.TryGetValue(selectQuery, out var result);
			return result;
		}

		private void CheckInitialized()
		{
			if (_parents == null)
			{
				_parents = new Dictionary<SelectQuery, HierarchyInfo>();
				_tree    = new Dictionary<SelectQuery, List<SelectQuery>>();
				BuildParentHierarchy(_rootQuery);
			}
		}

		/// <summary>
		/// Resync tree info. Can be called also during enumeration.
		/// </summary>
		public void Resync()
		{
			_parents = null;
			_tree = null;
		}

		public IEnumerable<SelectQuery> GetQueriesParentFirst()
		{
			return GetQueriesParentFirst(_rootQuery);
		}

		public IEnumerable<SelectQuery> GetQueriesParentFirst(SelectQuery root)
		{
			yield return root;

			CheckInitialized();

			if (_tree.TryGetValue(root, out var list))
			{
				// assuming that list at this stage is immutable
				foreach (var item in list)
				{
					yield return item;
				}

				foreach (var item in list)
				foreach (var subItem in GetQueriesParentFirst(item))
				{
					yield return subItem;
				}
			}
		}

		public bool? GetUnionInvolving(SelectQuery selectQuery)
		{
			var info = GetHierarchyInfo(selectQuery);
			if (info?.HierarchyType != HierarchyType.Union)
				return null;
			return ((SqlUnion)info.ParentElement).IsAll;
		}

		void RegisterHierachry(SelectQuery parent, SelectQuery child, HierarchyInfo info)
		{
			_parents[child] = info;

			if (!_tree.TryGetValue(parent, out var list))
			{
				list = new List<SelectQuery>();
				_tree.Add(parent, list);
			}
			list.Add(child);
		}

		void BuildParentHierarchy(SelectQuery selectQuery)
		{
			foreach (var table in selectQuery.From.Tables)
			{
				if (table.Source is SelectQuery s)
				{
					RegisterHierachry(selectQuery, s, new HierarchyInfo(selectQuery, HierarchyType.From, selectQuery));

					foreach (var union in s.Unions)
					{
						RegisterHierachry(selectQuery, union.SelectQuery, new HierarchyInfo(selectQuery, HierarchyType.Union, union));
						BuildParentHierarchy(union.SelectQuery);
					}

					BuildParentHierarchy(s);
				}

				foreach (var joinedTable in table.Joins)
				{
					if (joinedTable.Table.Source is SelectQuery joinQuery)
					{
						RegisterHierachry(selectQuery, joinQuery,
							new HierarchyInfo(selectQuery, HierarchyType.Join, joinedTable));
						BuildParentHierarchy(joinQuery);
					}
				}

			}

			var items = new List<IQueryElement>
			{
				selectQuery.GroupBy,
				selectQuery.Having,
				selectQuery.Where,
				selectQuery.OrderBy
			};

			items.AddRange(selectQuery.Select.Columns);

			foreach (var item in items)
			{
				new QueryVisitor().VisitParentFirst(item, e =>
				{
					if (e is SelectQuery q)
					{
						RegisterHierachry(selectQuery, q, new HierarchyInfo(selectQuery, HierarchyType.InnerQuery, null));
						BuildParentHierarchy(q);
						return false;
					}

					return true;
				});
			}
		}

		public enum HierarchyType
		{
			From,
			Join,
			Union,
			InnerQuery
		}

		public class HierarchyInfo
		{
			public HierarchyInfo(SelectQuery masterQuery, HierarchyType hierarchyType, IQueryElement parentElement)
			{
				MasterQuery = masterQuery;
				HierarchyType = hierarchyType;
				ParentElement = parentElement;
			}

			public SelectQuery   MasterQuery   { get; }
			public HierarchyType HierarchyType { get; }
			public IQueryElement ParentElement { get; }
		}
	}
}

