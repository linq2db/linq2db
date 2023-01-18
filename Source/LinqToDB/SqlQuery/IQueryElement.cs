namespace LinqToDB.SqlQuery
{
	public interface IQueryElement
	{
		QueryElementType       ElementType { get; }
		QueryElementTextWriter ToString(QueryElementTextWriter writer);
	}
}
