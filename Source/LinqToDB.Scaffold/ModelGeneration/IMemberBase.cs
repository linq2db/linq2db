using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IMemberBase : IClassMember
	{
		string?          ID                   { get; set; }
		AccessModifier   AccessModifier       { get; set; }
		string?          Name                 { get; set; }
		Func<string?>?   TypeBuilder          { get; set; }
		List<string>     Comment              { get; set; }
		string?          EndLineComment       { get; set; }
		List<IAttribute> Attributes           { get; set; }
		bool             InsertBlankLineAfter { get; set; }
		string?          Conditional          { get; set; }

		int AccessModifierLen { get; set; }
		int ModifierLen       { get; set; }
		int TypeLen           { get; set; }
		int NameLen           { get; set; }
		int ParamLen          { get; set; }
		int BodyLen           { get; set; }

		public string? Type { get; set; }

		string? BuildType();
	}
}
