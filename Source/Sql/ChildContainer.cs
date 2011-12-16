using System;
using System.Collections.Generic;

namespace LinqToDB.Sql
{
	public class ChildContainer<TP,TC> : Dictionary<string,TC>, IDictionary<string,TC>
		where TC : IChild<TP>
		where TP : class
	{
		internal ChildContainer()
		{
		}

		internal ChildContainer(TP parent)
		{
			_parent = parent;
		}

		readonly TP _parent;
		public   TP  Parent { get { return _parent; } }

		public void Add(TC item)
		{
			Add(item.Name, item);
		}

		public new void Add(string key, TC value)
		{
			if (value.Parent != null) throw new InvalidOperationException("Invalid parent.");
			value.Parent = _parent;

			base.Add(key, value);
		}

		public void AddRange(IEnumerable<TC> collection)
		{
			foreach (var item in collection)
				Add(item);
		}
	}
}
