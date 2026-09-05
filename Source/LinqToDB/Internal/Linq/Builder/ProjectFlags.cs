using System;

namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Two kinds of bit in one enum. <b>Purpose</b> - <see cref="SQL"/>, <see cref="Expression"/>,
	/// <see cref="Root"/>, <see cref="ExtractProjection"/>, <see cref="AggregationRoot"/>,
	/// <see cref="AssociationRoot"/>, <see cref="Table"/>, <see cref="Traverse"/>, <see cref="Subquery"/>,
	/// <see cref="Expand"/> - is mutually exclusive: exactly one is set on any value reaching
	/// <see cref="IBuildContext.MakeExpression"/>. <b>Modifiers</b> - <see cref="Keys"/>,
	/// <see cref="MemberRoot"/>, <see cref="ForSetProjection"/> - are independent, except that
	/// <see cref="Keys"/> only ever accompanies <see cref="SQL"/>, <see cref="Expression"/> or
	/// <see cref="ExtractProjection"/>.
	/// <para>
	/// Neither split is expressible in the type system, so both are enforced by the <c>LINQ2DB0004</c> /
	/// <c>LINQ2DB0005</c> analyzer, which derives them from <c>ExpressionBuildVisitor.GetProjectFlags</c> -
	/// the sole producer - rather than from this comment. Adding a member here, or changing which purpose
	/// carries which modifier there, is a build error until that analyzer's reader agrees.
	/// </para>
	/// </summary>
	[Flags]
	enum ProjectFlags
	{
		None            = 0x00,

		SQL        = 1 << 0,
		Expression = 1 << 1,
		Root       = 1 << 2,
		/// <summary>
		/// Forces expanding associations and GroupJoin into query expression
		/// </summary>
		ExtractProjection = 1 << 3,

		AggregationRoot = 1 << 4,
		/// <summary>
		/// Specify that from whole context we need just key fields.
		/// </summary>
		Keys            = 1 << 5,
		AssociationRoot = 1 << 7,
		/// <summary>
		/// Specify that we are looking for a table
		/// </summary>
		Table = 1 << 8,
		/// <summary>
		/// Specify that we expect real expression under hidden by Selects chain
		/// </summary>
		Traverse = 1 << 9,

		Subquery = 1 << 10,

		Expand = 1 << 11,

		MemberRoot = 1 << 12,
		ForSetProjection = 1 << 13,
	}
}
