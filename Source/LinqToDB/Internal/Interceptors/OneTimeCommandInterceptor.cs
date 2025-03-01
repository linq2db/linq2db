using System;
using System.Data.Common;

using LinqToDB.Interceptors;

namespace LinqToDB.Internal.Interceptors
{
	internal sealed class OneTimeCommandInterceptor : CommandInterceptor
	{
		readonly Func<CommandEventData,DbCommand,DbCommand>? _onCommandInitialized;

		public OneTimeCommandInterceptor(Func<CommandEventData, DbCommand, DbCommand> onCommandInitialized)
		{
			_onCommandInitialized = onCommandInitialized;
		}

		public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
		{
			if (_onCommandInitialized != null)
			{
				command = _onCommandInitialized(eventData, command);
				eventData.DataConnection.RemoveInterceptor(this);
			}

			return command;
		}
	}
}
