using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public abstract class CodeElementList<T>
		where T : ICodeElement
	{
		public List<T> Items { get; } = new();

		public void Add(T element)
		{
			Items.Add(element);
		}

		public void InsertAt(T element, int index)
		{
			Items.Insert(index, element);
		}
	}
}
