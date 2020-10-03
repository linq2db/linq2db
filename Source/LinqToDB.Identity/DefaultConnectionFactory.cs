using LinqToDB.Data;

namespace LinqToDB.Identity
{
	/// <summary>
	///     Represents default <see cref="IConnectionFactory" />
	/// </summary>
	public class DefaultConnectionFactory : IConnectionFactory
	{
		/// <summary>
		///     Creates <see cref="DataConnection" /> with default parameters
		/// </summary>
		/// <returns>
		///     <see cref="DataConnection" />
		/// </returns>
		public DataConnection GetConnection()
		{
			return new DataConnection();
		}

		/// <summary>
		///     Creates <see cref="DataContext" /> with default parameters
		/// </summary>
		/// <returns>
		///     <see cref="DataContext" />
		/// </returns>
		public IDataContext GetContext()
		{
			return new DataContext();
		}
	}
}