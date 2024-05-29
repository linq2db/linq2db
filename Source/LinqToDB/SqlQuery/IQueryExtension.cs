namespace LinqToDB.SqlQuery
{
	public interface IQueryExtension : IQueryElement
	{
		IQueryElement Accept(QueryElementVisitor visitor);
	}
}
