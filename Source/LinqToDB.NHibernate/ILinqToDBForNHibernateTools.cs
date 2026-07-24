using System.Linq;
using System.Linq.Expressions;

using LinqToDB.DataProvider;
using LinqToDB.Mapping;
using LinqToDB.Metadata;

using NHibernate;

namespace LinqToDB.NHibernate
{
	/// <summary>
	/// Bridge between an NHibernate <see cref="ISessionFactory"/> / <see cref="ISession"/> and linq2db.
	/// </summary>
	public interface ILinqToDBForNHibernateTools
	{
		/// <summary>
		/// Clears internal caches.
		/// </summary>
		void ClearCaches();

		/// <summary>
		/// Returns the linq2db provider for the given NHibernate provider and connection information.
		/// </summary>
		/// <param name="providerInfo">NHibernate provider information.</param>
		/// <param name="connectionInfo">Database connection information.</param>
		/// <returns>linq2db provider instance.</returns>
		IDataProvider? GetDataProvider(NHProviderInfo providerInfo, NHConnectionInfo connectionInfo);

		/// <summary>
		/// Creates a linq2db metadata reader over the given NHibernate <see cref="ISessionFactory"/>.
		/// </summary>
		/// <param name="sessionFactory">NHibernate session factory.</param>
		/// <returns>linq2db metadata reader, or <see langword="null"/>.</returns>
		IMetadataReader? CreateMetadataReader(ISessionFactory? sessionFactory);

		/// <summary>
		/// Creates a mapping schema for the given NHibernate <see cref="ISessionFactory"/> and metadata reader.
		/// </summary>
		/// <param name="sessionFactory">NHibernate session factory.</param>
		/// <param name="mappingSchemas">Additional mapping schemas.</param>
		/// <param name="metadataReader">Additional optional linq2db metadata reader.</param>
		/// <returns>Mapping schema.</returns>
		MappingSchema CreateMappingSchema(ISessionFactory sessionFactory, MappingSchema[]? mappingSchemas, IMetadataReader metadataReader);

		/// <summary>
		/// Returns the mapping schema for the given NHibernate <see cref="ISessionFactory"/> and metadata reader.
		/// </summary>
		/// <param name="sessionFactory">NHibernate session factory.</param>
		/// <param name="metadataReader">Additional optional linq2db metadata reader.</param>
		/// <returns>Mapping schema.</returns>
		MappingSchema GetMappingSchema(ISessionFactory? sessionFactory, IMetadataReader? metadataReader);

		/// <summary>
		/// Returns the <see cref="ISessionFactory"/> for the given <see cref="ISession"/>.
		/// </summary>
		/// <param name="session">NHibernate session.</param>
		/// <returns><see cref="ISessionFactory"/> instance.</returns>
		ISessionFactory? GetSessionOptions(ISession? session);

		/// <summary>
		/// Transforms a native NHibernate query expression tree into a linq2db expression.
		/// </summary>
		/// <param name="expression">Expression tree.</param>
		/// <param name="dc">linq2db <see cref="IDataContext"/> instance.</param>
		/// <param name="session">NHibernate session.</param>
		/// <param name="sessionFactory">NHibernate session factory.</param>
		/// <returns>Transformed expression.</returns>
		Expression TransformExpression(Expression expression, IDataContext dc, ISession? session, ISessionFactory? sessionFactory);

		/// <summary>
		/// Recovers the <see cref="ISession"/> that a native NHibernate <see cref="IQueryable"/> was created from.
		/// </summary>
		/// <param name="query">Native NHibernate query.</param>
		/// <returns>The <see cref="ISession"/> instance, or <see langword="null"/>.</returns>
		ISession? GetCurrentContext(IQueryable query);

		/// <summary>
		/// When enabled (the default), an entity materialised by a linq2db query over an attached session is
		/// locked into that session so it becomes change-tracked.
		/// </summary>
		bool EnableChangeTracker { get; set; }

		/// <summary>
		/// Registers an additional mapping schema for the given NHibernate <see cref="ISessionFactory"/>.
		/// </summary>
		/// <param name="sessionFactory">NHibernate session factory.</param>
		/// <param name="mappingSchema">Mapping schema to add.</param>
		void AddMappingSchema(ISessionFactory sessionFactory, MappingSchema mappingSchema);
	}
}
