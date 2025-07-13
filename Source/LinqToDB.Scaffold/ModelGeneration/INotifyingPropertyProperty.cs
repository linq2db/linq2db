using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface INotifyingPropertyProperty : IProperty
	{
		bool         IsNotifying { get; set; }
		List<string> Dependents  { get; set; }
	}
}
