using System;

namespace LinqToDB.CodeGen.CodeModel
{
	[Flags]
	public enum MemberAttributes
	{
		None = 0,

		Partial = 1,

		Static = 2,

		Public = 4,
		Private = 8,

		Extension = 16,

		ReadOnly = 32,

		//AccessMask = Public,
		//ClassMask = Partial | Static | Public,
	}
}
