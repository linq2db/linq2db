using System;

namespace LinqToDB.Linq.Translation
{
	[Flags]
	public enum TranslationFlags
	{
		None       = 0,
		Expression = 1,
		Sql        = 1 << 1,
		Expand     = 1 << 2,
		Traverse   = 1 << 3
	}
}

