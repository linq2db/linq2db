using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Used to mark the index of a result set when multiple result sets need to be processed for a command.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class ResultSetIndexAttribute : Attribute
	{
		public int Index { get; }

		public ResultSetIndexAttribute(int index)
		{
			Index = index;
		}
	}
}
