namespace LinqToDB.Linq
{
	using SqlQuery;

	public interface IQueryContext
	{
		SqlStatement    Statement       { get; }
		object?         Context         { get; set; }
		bool            IsContinuousRun { get; set; }
		AliasesContext? Aliases         { get; set; }
		DataOptions?    DataOptions     { get; }
	}
}
