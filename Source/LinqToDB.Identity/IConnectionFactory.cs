using LinqToDB.Data;

namespace LinqToDB.Identity
{
	/// <summary>
	///     Represents connection factory
	/// </summary>
	public interface IConnectionFactory
	{
		/// <summary>
		///     Gets new instance of <see cref="IDataContext" />
		/// </summary>
		/// <returns>
		///     <see cref="IDataContext" />
		/// </returns>
		IDataContext GetContext();

		/// <summary>
		///     Gets new instance of <see cref="DataConnection" />
		/// </summary>
		/// <returns>
		///     <see cref="DataConnection" />
		/// </returns>
		DataConnection GetConnection();
	}
}