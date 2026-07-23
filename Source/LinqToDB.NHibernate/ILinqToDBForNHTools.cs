using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;
using LinqToDB.Metadata;
using NHibernate;

namespace LinqToDB.NHibernateExtension
{
	/// <summary>
	/// Interface for EF.Core - LINQ To DB integration bridge.
	/// </summary>
	public interface ILinqToDBForNHTools
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
		/// <param name="model">EF.Core data model.</param>
		/// <param name="dependencies"></param>
		/// <param name="mappingSource"></param>
		/// <param name="logger"></param>
		/// <returns>LINQ To DB metadata provider for specified EF.Core model. Can return <c>null</c>.</returns>
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
		/// <param name="model">EF.Core data model.</param>
		/// <param name="metadataReader">Additional optional LINQ To DB database metadata provider.</param>
		/// <param name="convertorSelector">EF Core registry for type conversion.</param>
		/// <returns>Mapping schema for provided EF.Core model.</returns>
		MappingSchema GetMappingSchema(ISessionFactory sessionFactory, IMetadataReader? metadataReader);

		/// <summary>
		/// Returns EF.Core <see cref="IDbContextOptions"/> for specific <see cref="DbContext"/> instance.
		/// </summary>
		/// <param name="session"></param>
		/// <param name="context">EF.Core <see cref="DbContext"/> instance.</param>
		/// <returns><see cref="IDbContextOptions"/> instance.</returns>
		ISessionFactory? GetSessionOptions(ISession? session);

		/// <summary>
		/// Transforms EF.Core expression tree to LINQ To DB expression.
		/// </summary>
		/// <param name="expression">EF.Core expression tree.</param>
		/// <param name="dc">LINQ To DB <see cref="IDataContext"/> instance.</param>
		/// <param name="ctx">Optional DbContext instance.</param>
		/// <param name="model">EF.Core data model instance.</param>
		/// <returns>Transformed expression.</returns>
		Expression TransformExpression(Expression expression, IDataContext dc, ISession? session, ISessionFactory? sessionFactory);

		/// <summary>
		/// Extracts <see cref="DbContext"/> instance from <see cref="IQueryable"/> object.
		/// </summary>
		/// <param name="query">EF.Core query.</param>
		/// <returns>Current <see cref="DbContext"/> instance.</returns>
		ISession? GetCurrentContext(IQueryable query);

		/// <summary>
		/// Extracts EF.Core connection information object from <see cref="IDbContextOptions"/>.
		/// </summary>
		/// <param name="sessionFactory"></param>
		/// <param name="options"><see cref="IDbContextOptions"/> instance.</param>
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
