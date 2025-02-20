using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Interceptors;

namespace Tests
{
	/// <summary>
	/// Provides access to last command and parameters for command wrapped into miniprofiler wrapper.
	/// </summary>
	public sealed class SaveWrappedCommandInterceptor : CommandInterceptor
	{
		public DbParameter[] Parameters { get; private set; } = Array.Empty<DbParameter>();

		[MaybeNull]
		public DbCommand     Command    { get; private set; }

		public event Action<DbCommand>? OnCommandSet;

		private readonly bool _unwrap;

		public SaveWrappedCommandInterceptor(bool unwrap)
		{
			_unwrap = unwrap;
		}

		public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
		{
			Parameters = command.Parameters.Cast<DbParameter>().ToArray();
			Command    = _unwrap ? (DbCommand)((dynamic)command).WrappedCommand : command;

			OnCommandSet?.Invoke(command);

			return command;
		}
	}
}
