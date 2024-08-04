namespace LinqToDB.EntityFrameworkCore.BaseTests.Interceptors
{
	public abstract class TestInterceptor
	{
		public bool HasInterceptorBeenInvoked { get; protected set; }

		public void ResetInvocations()
		{
			HasInterceptorBeenInvoked = false;
		}
	}
}
