namespace LinqToDB.EntityFrameworkCore.Tests.Interceptors
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
