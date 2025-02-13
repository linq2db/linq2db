using System;

namespace LinqToDB.Remote
{
	/// <summary>
	/// A factory for creating <see cref="IDataContext"/> instances.
	/// </summary>
	/// <typeparam name="TContext"></typeparam>
	public interface IDataContextFactory<TContext>
	where TContext : IDataContext
	{
		TContext GetDataContext(string? configuration = null);
	}
}
