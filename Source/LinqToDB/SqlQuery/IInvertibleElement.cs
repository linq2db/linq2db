namespace LinqToDB.SqlQuery
{
	public interface IInvertibleElement
	{
		bool CanInvert { get; }
		ISqlPredicate Invert();
	}
}
