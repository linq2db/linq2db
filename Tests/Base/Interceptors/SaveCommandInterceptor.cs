using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LinqToDB.Interceptors;

namespace Tests
{
	/// <summary>
	/// Provides access to last command and parameters.
	/// </summary>
	public sealed class SaveCommandInterceptor : CommandInterceptor
	{
		public DbParameter[] Parameters { get; private set; } = Array.Empty<DbParameter>();

		[MaybeNull]
		public DbCommand     Command    { get; private set; }

		public override DbCommand CommandInitialized(CommandInitializedEventData eventData, DbCommand command)
		{
			Parameters = command.Parameters.Cast<DbParameter>().ToArray();
			Command    = command;

			return command;
		}
	}
}
