using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IMethod : IMemberBase
	{
		bool                            IsAbstract        { get; set; }
		bool                            IsVirtual         { get; set; }
		bool                            IsOverride        { get; set; }
		bool                            IsStatic          { get; set; }
		List<string>                    GenericArguments  { get; set; }
		List<string>                    AfterSignature    { get; set; }
		List<Func<string>>              ParameterBuilders { get; set; }
		List<Func<IEnumerable<string>>> BodyBuilders      { get; set; }

		IEnumerable<string> BuildBody();
	}
}
