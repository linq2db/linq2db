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
	/// the sole producer - rather than from this comment. Adding a member here is a build error until
	/// <c>GetProjectFlags</c> produces it; changing which purpose carries which modifier there is not, because
	/// the reader re-derives the split from that method on every build.
	/// </para>
	/// <para>
	/// That analyzer applies the model to every <c>ProjectFlags</c> local or parameter in the assembly, not only
	/// to the values that reach <see cref="IBuildContext.MakeExpression"/>. A value composed by hand out of two
	/// purpose bits therefore lies outside the model - see <c>ProjectFlagsAnalyzer</c>'s remarks for the two such
	/// values in the tree and for the <c>#pragma warning disable</c> escape hatch - so check there before writing
	/// a flag conjunction over a value that did not come from <c>GetProjectFlags</c>.
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
