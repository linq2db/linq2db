using System;
using System.Data.Common;

namespace LinqToDB.Interceptors
{
	internal sealed class OneTimeCommandInterceptor : CommandInterceptor
	{
		private readonly Func<CommandInitializedEventData, DbCommand, DbCommand>? _onCommandInitialized;
		public OneTimeCommandInterceptor(Func<CommandInitializedEventData, DbCommand, DbCommand> onCommandInitialized)
		{
			_onCommandInitialized = onCommandInitialized;
		}

		public override DbCommand CommandInitialized(CommandInitializedEventData eventData, DbCommand command)
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
