namespace LinqToDB.Internals.SqlQuery
{
	public interface IQueryExtension : IQueryElement
	{
		IQueryElement Accept(QueryElementVisitor visitor);
	}
}
