using System;

namespace LinqToDB.Internal.Linq.Builder
{
	[Flags]
	enum BuildFlags
	{
		None                = 0,
		ForceParameter      = 1 << 0,
		ForceDefaultIfEmpty = 1 << 1,
		ForSetProjection    = 1 << 2,
		ForKeys             = 1 << 3,
		ForceOuter          = 1 << 4,
		ForExtension        = 1 << 5,
		ForMemberRoot       = 1 << 6,
		FormatAsExpression  = 1 << 7,
		// forces clearing flags
		ResetPrevious       = 1 << 8,
		// Set while a member translator is translating: it re-enters the builder through
		// ITranslationContext.Translate to translate its own arguments, and those must not be pulled client-side
		// by PreferClientCalculation - the translator would then receive a non-placeholder and decline.
		// Survives ResetPrevious (see CombineFlags).
		InsideTranslation   = 1 << 9,
	}
}
