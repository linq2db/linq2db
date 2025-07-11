namespace LinqToDB.Internal.SqlQuery
{
	public interface IQueryExtension : IQueryElement
	{
		IQueryElement Accept(QueryElementVisitor visitor);
	}
}
