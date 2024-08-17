using System.Linq;

using LinqToDB.Interceptors;

using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LinqToDB.EntityFrameworkCore.Tests.Interceptors.Extensions
{
	public static class LinqToDBContextOptionsBuilderExtensions
	{
		public static void UseEfCoreRegisteredInterceptorsIfPossible(this LinqToDBContextOptionsBuilder builder)
		{
			var coreEfExtension = builder.DbContextOptions.FindExtension<CoreOptionsExtension>();
			if (coreEfExtension?.Interceptors != null)
			{
				foreach (var comboInterceptor in coreEfExtension.Interceptors.OfType<IInterceptor>())
				{
					builder.AddInterceptor(comboInterceptor);
				}
			}
		}
	}
}
