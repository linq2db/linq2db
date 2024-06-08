namespace LinqToDB.SqlQuery
{
	public interface IInvertibleElement
	{
		bool CanInvert();
		IQueryElement Invert();
	}
}
