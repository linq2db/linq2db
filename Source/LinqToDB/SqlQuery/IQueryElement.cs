namespace LinqToDB.SqlQuery
{
	public interface IQueryElement
	{
#if DEBUG
		public string DebugText { get; }
#endif
		QueryElementType       ElementType { get; }
		QueryElementTextWriter ToString(QueryElementTextWriter writer);
	}
}
