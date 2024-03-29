using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	public interface ITree
	{
		ITree?             Parent { get; set; }
		IEnumerable<ITree> GetNodes();
		void               SetTree();
	}
}
