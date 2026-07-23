using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;
using LinqToDB.Metadata;
using NHibernate;

namespace LinqToDB.NHibernate
{
	/// <summary>
	/// Interface for EF.Core - LINQ To DB integration bridge.
	/// </summary>
	public interface ILinqToDBForNHibernateTools
	{
		/// <summary>
		/// Clears internal caches
		/// </summary>
		void ClearCaches();

		/// <summary>
		/// Returns LINQ To DB provider, based on provider data from EF.Core.
		/// </summary>
		/// <param name="providerInfo">Provider information, extracted from EF.Core.</param>
		/// <param name="connectionInfo">Database connection information.</param>
		/// <returns>LINQ TO DB provider instance.</returns>
		IDataProvider? GetDataProvider(NHProviderInfo providerInfo, NHConnectionInfo connectionInfo);

		/// <summary>
		/// Creates metadata provider for specified EF.Core data model.
		/// </summary>
		/// <returns>LINQ To DB metadata provider for specified EF.Core model. Can return <see langword="null"/>.</returns>
		IMetadataReader? CreateMetadataReader(ISessionFactory? sessionFactory);

		/// <summary>
		/// Creates mapping schema using provided EF.Core data model and metadata provider.
		/// </summary>
		/// <param name="sessionFactory"></param>
		/// <param name="mappingSchemas"></param>
		/// <param name="metadataReader">Additional optional LINQ To DB database metadata provider.</param>
		/// <returns>Mapping schema for provided EF.Core model.</returns>
		MappingSchema CreateMappingSchema(ISessionFactory sessionFactory, MappingSchema[]? mappingSchemas, IMetadataReader metadataReader);

		/// <summary>
		/// Returns mapping schema using provided EF.Core data model and metadata provider.
		/// </summary>
		/// <param name="metadataReader">Additional optional LINQ To DB database metadata provider.</param>
		/// <returns>Mapping schema for provided EF.Core model.</returns>
		MappingSchema GetMappingSchema(ISessionFactory? sessionFactory, IMetadataReader? metadataReader);

		/// <summary>
		/// Returns EF.Core <see cref="ISessionFactory"/> for specific <see cref="ISession"/> instance.
		/// </summary>
		/// <param name="session"></param>
		/// <returns><see cref="ISessionFactory"/> instance.</returns>
		ISessionFactory? GetSessionOptions(ISession? session);

		/// <summary>
		/// Transforms EF.Core expression tree to LINQ To DB expression.
		/// </summary>
		/// <param name="expression">EF.Core expression tree.</param>
		/// <param name="dc">LINQ To DB <see cref="IDataContext"/> instance.</param>
		/// <returns>Transformed expression.</returns>
		Expression TransformExpression(Expression expression, IDataContext dc, ISession? session, ISessionFactory? sessionFactory);

		/// <summary>
		/// Extracts <see cref="ISession"/> instance from <see cref="IQueryable"/> object.
		/// </summary>
		/// <param name="query">EF.Core query.</param>
		/// <returns>Current <see cref="ISession"/> instance.</returns>
		ISession? GetCurrentContext(IQueryable query);

		/// <summary>
		/// Extracts EF.Core connection information object from <see cref="ISessionFactory"/>.
		/// </summary>
		/// <param name="sessionFactory"></param>
		/// <returns>EF.Core connection data.</returns>
		NHConnectionInfo ExtractConnectionInfo(ISessionFactory? sessionFactory);

		/*
		/// <summary>
		/// Extracts EF.Core data model instance from <see cref="IDbContextOptions"/>.
		/// </summary>
		/// <param name="options"><see cref="IDbContextOptions"/> instance.</param>
		/// <returns>EF.Core data model instance.</returns>
		IModel? ExtractModel(IDbContextOptions? options);

		/// <summary>
		/// Creates logger used for logging Linq To DB connection calls.
		/// </summary>
		/// <param name="options"><see cref="IDbContextOptions"/> instance.</param>
		/// <returns>Logger instance.</returns>
		ILogger? CreateLogger(IDbContextOptions? options);
		*/

		/*/// <summary>
		/// Logs DataConnection information.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="logger"></param>
		void LogConnectionTrace(TraceInfo info, ILogger logger);*/

		/// <summary>
		/// Enables attaching entities to change tracker.
		/// Entities will be attached only if AsNoTracking() is not used in query and DbContext is configured to track entities. 
		/// </summary>
		bool EnableChangeTracker { get; set; }

		void AddMappingSchema(ISessionFactory sessionFactory, MappingSchema mappingSchema);
	}
}
