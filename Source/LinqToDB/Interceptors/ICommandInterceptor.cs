using System.Data.Common;

namespace LinqToDB.Interceptors
{
	public interface ICommandInterceptor : IInterceptor
	{
		/// <summary>
		/// Event, triggered after command prepared for execution with both command text and parameters set.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="command">Initialized command instance.</param>
		/// <returns>Returns command instance for execution.</returns>
		DbCommand CommandInitialized(CommandInitializedEventData eventData, DbCommand command);
	}
}
