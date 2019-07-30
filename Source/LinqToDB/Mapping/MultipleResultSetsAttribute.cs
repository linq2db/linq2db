using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToDB.Mapping
{   /// <summary>
	/// You can apply this to a class to allow the processing of multiple set of results in response to a batch of SQL being run.
	/// This is useful in the case that multiple SQL statements are executed as part of a command or stored procedure.
	/// Within a denoted class, ResultSetIndexAttribute should be set on enumerable properties in the order they are returned in the script.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class MultipleResultSetsAttribute : Attribute
	{
	}
}
