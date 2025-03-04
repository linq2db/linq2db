namespace LinqToDB.Internal.Options
{
	interface IApplicable<in T>
	{
		void Apply(T obj);
	}
}
