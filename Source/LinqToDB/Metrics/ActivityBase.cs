using System.Data.Common;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Data;

namespace LinqToDB.Metrics
{
	/// <summary>
	/// Provides a basic implementation of the <see cref="IActivity"/> interface.
	/// You do not have to use this class.
	/// However, it can help you to avoid incompatibility issues in the future if the <see cref="IActivity"/> interface is extended.
	/// </summary>
	[PublicAPI]
	public abstract class ActivityBase(ActivityID activityID) : IActivity
	{
		public ActivityID ActivityID { get; } = activityID;

		public abstract void Dispose();

		public virtual ValueTask DisposeAsync()
		{
			Dispose();
			return default;
		}

		public virtual IActivity AddTag(ActivityTagID key, object? value)
		{
			return this;
		}

		public virtual IActivity AddQueryInfo(DataConnection? context, DbConnection? connection, DbCommand? command)
		{
			if (context != null)
			{
				AddTag(ActivityTagID.ConfigurationString, context.ConfigurationString);
				AddTag(ActivityTagID.DataProviderName,    context.DataProvider.Name);
			}

			if (connection != null)
			{
				// those properties could throw for some providers when connection failed to initialize, e.g. due to bad connection string
				// Observed for MySqlConnector and Sap.Data.*
				try
				{
					AddTag(ActivityTagID.DataSourceName, connection.DataSource);
				}
				catch
				{
				}

				try
				{
					AddTag(ActivityTagID.DatabaseName,   connection.Database);
				}
				catch
				{
				}
			}

			if (command != null)
			{
				AddTag(ActivityTagID.CommandText, command.CommandText);
			}

			return this;
		}
	}
}
