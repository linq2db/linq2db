using System.Data.Common;

namespace LinqToDB.Interceptors
{
	public abstract class CommandInterceptor : ICommandInterceptor
	{
		public virtual DbCommand CommandInitialized(CommandInitializedEventData eventData, DbCommand command) => command;
	}
}
