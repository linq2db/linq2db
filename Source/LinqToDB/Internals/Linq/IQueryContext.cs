using LinqToDB.Internals.SqlQuery;

namespace LinqToDB.Internals.Linq
{
	public interface IQueryContext
	{
		SqlStatement    Statement       { get; }
		object?         Context         { get; set; }
		bool            IsContinuousRun { get; set; }
		AliasesContext? Aliases         { get; set; }
		DataOptions?    DataOptions     { get; }
	}
}
