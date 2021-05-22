namespace Tests
{
	public static class CustomizationSupport
	{
		//Replace this instance with a custom implementation to override default behaviour
		public static readonly CustomizationSupportInterceptor Interceptor = new CustomizationSupportInterceptor();
	}
}
