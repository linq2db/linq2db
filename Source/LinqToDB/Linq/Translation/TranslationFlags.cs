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
		Traverse   = 1 << 3,

		/// <summary>
		/// The caller prefers client-side calculation (<see cref="LinqToDB.LinqOptions.PreferClientCalculation"/>).
		/// A translation registered as optional declines, so the expression is evaluated on the client instead of
		/// becoming an SQL column. Translations that are not registered as optional are unaffected.
		/// </summary>
		/// <remarks>
		/// Every <see cref="IMemberTranslator"/> receives this flag, including user-supplied ones and the remote
		/// pass-through. A direct implementation may return <see langword="null"/> when it is set to leave the
		/// expression to the client. Registrations made by a translator deriving from <c>MemberTranslatorBase</c>
		/// are mandatory unless made inside <c>TranslationRegistration.OptionalScope()</c>, so overriding a member
		/// the built-in translators declared optional silently keeps it server-side under the option.
		/// </remarks>
		SkipOptional = 1 << 4,
	}
}

