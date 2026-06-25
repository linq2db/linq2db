using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq
{
	interface IQueryContext
	{
		SqlStatement    Statement       { get; }
		object?         Context         { get; set; }
		bool            IsContinuousRun { get; set; }

		/// <summary>
		/// Finalized alias context from the most recent <c>PrepareQueryAndAliases</c> pass, re-fed as the
		/// previous-run seed on the next execution of a parameter-dependent / continuous query so stable
		/// aliases are reused rather than re-derived. <see langword="null"/> once SQL is fully built and cached.
		/// Written without the query lock on continuous runs; the race is benign - each candidate is a
		/// complete, valid name set over the immutable cached statement, so last-writer-wins only picks
		/// which valid seed the next run reuses.
		/// </summary>
		AliasesContext? Aliases         { get; set; }
		DataOptions?    DataOptions     { get; }
	}
}
