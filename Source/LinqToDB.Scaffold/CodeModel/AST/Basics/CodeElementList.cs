using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Base class for collection of code nodes of specific type.
	/// </summary>
	/// <typeparam name="TElement">Type of nodes in collection.</typeparam>
	public abstract class CodeElementList<TElement>
		where TElement : ICodeElement
	{
		private readonly List<TElement> _items;

		protected CodeElementList(IEnumerable<TElement>? items)
		{
			_items = [.. items ?? []];
		}

		public IReadOnlyList<TElement> Items => _items;

		public void Add(TElement element)
		{
			_items.Add(element);
		}

		public void InsertAt(TElement element, int index)
		{
			_items.Insert(index, element);
		}
	}
}
