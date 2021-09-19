using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Base class for collection of code nodes of specific type.
	/// </summary>
	/// <typeparam name="TElement">Type of nodes in collection.</typeparam>
	public abstract class CodeElementList<TElement>
		where TElement : ICodeElement
	{
		protected CodeElementList(List<TElement>? items)
		{
			Items = items ?? new ();
		}

		public List<TElement> Items { get; }

		public void Add(TElement element)
		{
			Items.Add(element);
		}

		public void InsertAt(TElement element, int index)
		{
			Items.Insert(index, element);
		}
	}
}
