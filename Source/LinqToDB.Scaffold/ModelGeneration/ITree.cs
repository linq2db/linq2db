using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface ITree
	{
		ITree?             Parent { get; set; }
		IEnumerable<ITree> GetNodes();
		void               SetTree();
	}
}
