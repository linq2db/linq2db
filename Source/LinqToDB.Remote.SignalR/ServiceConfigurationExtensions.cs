using System;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Remote.SignalR
{
	[PublicAPI]
	public static class ServiceConfigurationExtensions
	{
		public static async Task InitSignalRAsync(this IDataContext dataContext)
		{
			if (dataContext is SignalRDataContext signalRDataContext)
			{
				await signalRDataContext.ConfigureAsync(default).ConfigureAwait(false);
			}
		}
	}
}
