using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGenerator
{
	public interface ITree
	{
		ITree?             Parent { get; set; }
		IEnumerable<ITree> GetNodes();
		void               SetTree();
	}
}
