namespace LinqToDB.Configuration
{
	interface IApplicable<in T>
	{
		void Apply(T obj);
	}
}
