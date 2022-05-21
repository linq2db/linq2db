namespace LinqToDB.SqlQuery
{
	public interface IInvertibleElement
	{
		bool CanInvert();
		ISqlPredicate Invert();
	}
}
