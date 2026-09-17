using System;
using System.Linq.Expressions;

using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	/// <summary>
	/// The part of a translation context that reads a translated value the way .NET reads it. It belongs on
	/// <see cref="ITranslationContext"/> and is kept beside it instead, because adding a member to a shipped interface
	/// breaks whoever implements it, which is a major release's business. Moving it onto the interface in 7.0 leaves the
	/// call sites alone: they go through <see cref="TranslationContextExtensions.ReadAsValue"/>, and an interface member
	/// wins over an extension method of the same shape.
	/// </summary>
	public interface ITranslationValueReader
	{
		/// <inheritdoc cref="TranslationContextExtensions.ReadAsValue"/>
		Expression ReadAsValue(Expression translated, Type valueType);
	}
}
