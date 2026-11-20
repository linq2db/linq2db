using System;
using System.Linq.Expressions;

namespace LinqToDB.Internal.DataProvider.Translation
{
	/// <summary>
	/// A translation context that types operands as a comparison between them types its own.
	/// </summary>
	interface IComparedValueTranslationContext
	{
		/// <summary>
		/// Types what is translated within as a comparison between <paramref name="left"/> and <paramref name="right"/>
		/// types its operands.
		/// </summary>
		IDisposable UsingComparisonDescriptor(Expression left, Expression right);
	}
}
