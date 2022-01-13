using System;
using System.Data.Common;

namespace LinqToDB.Interceptors
{
	internal sealed class OneTimeCommandInterceptor : CommandInterceptor
	{
		private readonly Func<CommandEventData, DbCommand, DbCommand>? _onCommandInitialized;
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
