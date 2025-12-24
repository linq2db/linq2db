using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IProperty : IMemberBase
	{
		bool    IsAuto     { get; set; }
		string? InitValue  { get; set; }
		bool    IsVirtual  { get; set; }
		bool    IsOverride { get; set; }
		bool    IsAbstract { get; set; }
		bool    IsStatic   { get; set; }
		bool    HasGetter  { get; set; }
		bool    HasSetter  { get; set; }
		bool    IsNullable { get; set; }

		List<Func<IEnumerable<string>>> GetBodyBuilders { get; set; }
		List<Func<IEnumerable<string>>> SetBodyBuilders { get; set; }

		int GetterLen { get; set; }
		int SetterLen { get; set; }

		bool EnforceNotNullable { get; }

		IEnumerable<string> BuildGetBody();
		IEnumerable<string> BuildSetBody();
	}
}
