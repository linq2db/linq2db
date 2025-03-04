using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IAttribute
	{
		string?      Name        { get; }
		List<string> Parameters  { get; }
		string?      Conditional { get; }
		bool         IsSeparated { get; }

		void Render(ModelGenerator tt);
	}
}
