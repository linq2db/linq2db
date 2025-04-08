using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface ITypeBase : IClassMember
	{
		AccessModifier   AccessModifier { get; set; }
		string?          Name           { get; set; }
		bool             IsPartial      { get; set; }
		List<string>     Comment        { get; set; }
		List<IAttribute> Attributes     { get; set; }
		string?          Conditional    { get; set; }
		string           ClassKeyword   { get; set; }

		void Render(ModelGenerator tt);
	}
}
