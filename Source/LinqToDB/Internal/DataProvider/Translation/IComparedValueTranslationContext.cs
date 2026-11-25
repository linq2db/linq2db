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
		/// types its operands. When <paramref name="left"/> is also compared with <paramref name="otherRight"/> and only that
		/// pair finds a column that stores values in a form of its own, both pairs are written in that form.
		/// </summary>
		IDisposable UsingComparisonDescriptor(Expression left, Expression right, Expression? otherRight);
	}
}
