using System.Data.Common;

using LinqToDB.Interceptors;

namespace Tests
{
	// used for native oracle provider to avoid :NEW trigger token recognized as parameter
	public sealed class BindByNameOracleCommandInterceptor : CommandInterceptor
	{
		public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
		{
			((dynamic)command).BindByName = false;

			return command;
		}
	}
}
