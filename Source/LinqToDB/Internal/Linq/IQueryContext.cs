using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq
{
	interface IQueryContext
	{
		SqlStatement    Statement       { get; }
		object?         Context         { get; set; }
		bool            IsContinuousRun { get; set; }
		DataOptions?    DataOptions     { get; }
	}
}
