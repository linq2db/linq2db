using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public class QueryInformation
	{
		public SelectQuery RootQuery { get; }

		private Dictionary<SelectQuery, HierarchyInfo>?     _parents;
		private Dictionary<SelectQuery, List<SelectQuery>>? _tree;

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public QueryInformation(SelectQuery rootQuery)
		{
			RootQuery = rootQuery ?? throw new ArgumentNullException(nameof(rootQuery));
		}

		/// <summary>
		/// Returns parent query if query is subquery for select
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <returns></returns>
		public SelectQuery? GetParentQuery(SelectQuery selectQuery)
		{
			var info = GetHierarchyInfo(selectQuery);
			return info?.HierarchyType == HierarchyType.From || info?.HierarchyType == HierarchyType.Join
				? info.MasterQuery
				: null;
		}

		/// <summary>
		/// Returns HirarchyInfo for specific selectQuery
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <returns></returns>
		public HierarchyInfo? GetHierarchyInfo(SelectQuery selectQuery)
		{
			CheckInitialized();
			_parents!.TryGetValue(selectQuery, out var result);
			return result;
		}

		private void CheckInitialized()
		{
			if (_parents == null)
			{
				_parents = new Dictionary<SelectQuery, HierarchyInfo>();
				_tree    = new Dictionary<SelectQuery, List<SelectQuery>>();
				BuildParentHierarchy(RootQuery);
			}
		}

		/// <summary>
		/// Resync tree info. Can be called also during enumeration.
		/// </summary>
		public void Resync()
		{
			_parents = null;
			_tree    = null;
		}

		public IEnumerable<SelectQuery> GetQueriesParentFirst()
		{
			return GetQueriesParentFirst(RootQuery);
		}

		public IEnumerable<SelectQuery> GetQueriesParentFirst(SelectQuery root)
		{
			yield return root;

			CheckInitialized();

			if (_tree!.TryGetValue(root, out var list))
			{
				// assuming that list at this stage is immutable
				foreach (var item in list)
				foreach (var subItem in GetQueriesParentFirst(item))
				{
					yield return subItem;
				}
			}
		}

		public IEnumerable<SelectQuery> GetQueriesChildFirst()
		{
			return GetQueriesChildFirst(RootQuery);
		}

		public IEnumerable<SelectQuery> GetQueriesChildFirst(SelectQuery root)
		{
			CheckInitialized();

			if (_tree!.TryGetValue(root, out var list))
			{
				foreach (var item in list)
				foreach (var subItem in GetQueriesChildFirst(item))
				{
					yield return subItem;
				}

				// assuming that list at this stage is immutable
				foreach (var item in list)
				{
					yield return item;
				}
			}

			yield return root;
		}

		void RegisterHierarchry(SelectQuery parent, SelectQuery child, HierarchyInfo info)
		{
			_parents![child] = info;

			if (!_tree!.TryGetValue(parent, out var list))
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
					RegisterHierarchry(selectQuery, s, new HierarchyInfo(selectQuery, HierarchyType.From, selectQuery));

					foreach (var setOperator in s.SetOperators)
					{
						RegisterHierarchry(selectQuery, setOperator.SelectQuery, new HierarchyInfo(selectQuery, HierarchyType.SetOperator, setOperator));
						BuildParentHierarchy(setOperator.SelectQuery);
					}

					BuildParentHierarchy(s);
				}

				foreach (var joinedTable in table.Joins)
				{
					if (joinedTable.Table.Source is SelectQuery joinQuery)
					{
						RegisterHierarchry(selectQuery, joinQuery,
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
			if (!selectQuery.Where.IsEmpty)
				items.Add(selectQuery.Where);

			var ctx = new BuildParentHierarchyContext(this, selectQuery);
			foreach (var item in items)
			{
				ctx.Parent = null;
				item.VisitParentFirst(ctx, static (context, e) =>
				{
					if (e is SelectQuery q)
					{
						context.Info.RegisterHierarchry(context.SelectQuery, q, new HierarchyInfo(context.SelectQuery, HierarchyType.InnerQuery, context.Parent));
						context.Info.BuildParentHierarchy(q);
						return false;
					}

					context.Parent = e;

					return true;
				});
			}
		}

		private sealed class BuildParentHierarchyContext
		{
			public BuildParentHierarchyContext(QueryInformation qi, SelectQuery selectQuery)
			{
				Info        = qi;
				SelectQuery = selectQuery;
			}

			public readonly QueryInformation Info;
			public readonly SelectQuery      SelectQuery;

			public IQueryElement? Parent;
		}

		public enum HierarchyType
		{
			From,
			Join,
			SetOperator,
			InnerQuery
		}

		public class HierarchyInfo
		{
			public HierarchyInfo(SelectQuery masterQuery, HierarchyType hierarchyType, IQueryElement? parentElement)
			{
				MasterQuery   = masterQuery;
				HierarchyType = hierarchyType;
				ParentElement = parentElement;
			}

			public SelectQuery    MasterQuery   { get; }
			public HierarchyType  HierarchyType { get; }
			public IQueryElement? ParentElement { get; }
		}
	}
}
