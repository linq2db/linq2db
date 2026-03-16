using System.ComponentModel;

using LinqToDB.Data;
using LinqToDB.Mapping;

namespace LinqToDB
{
	/// <summary>
	/// High-level architectural principles and mental model of LinqToDB.
	/// This type exists for documentation purposes only.
	/// Not intended for application code.
	/// </summary>
	/// <remarks>
	/// <para><b>Core Identity</b></para>
	/// <para>LinqToDB is a deterministic LINQ-to-SQL translator.</para>
	/// <para>
	/// The same LINQ expression under the same configuration produces the same SQL intent (semantically equivalent SQL).
	/// </para>
	/// <para><b>How to think about LinqToDB:</b></para>
	/// <para>
	/// When writing a LINQ query, think in terms of SQL intent and shape.
	/// Every LINQ construct should correspond to a SQL construct.
	/// If a LINQ expression has no clear SQL representation,
	/// it must be rewritten or explicitly mapped.
	/// </para>
	/// <list type="bullet">
	///   <item>
	///     <description>
	///       LINQ queries are captured as Expression Trees and translated to SQL.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///       Generated SQL is executed by a selected database provider.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///       Query behavior is explicit and configuration-driven.
	///     </description>
	///   </item>
	/// </list>
	///
	/// <para><b>Translation Pipeline</b></para>
	/// <para>
	/// Query processing follows a deterministic translation pipeline:
	/// </para>
	/// <para>
	/// Expression Tree → SQL AST → SQL text → Execution.
	/// Each stage is deterministic under the same configuration.
	/// </para>
	/// <para>
	/// SQL generation is provider-defined.
	/// Different database providers may emit different SQL text while preserving equivalent SQL semantics.
	/// </para>
	///
	/// <para><b>What LinqToDB does not provide implicitly:</b></para>
	///
	/// <list type="bullet">
	///   <item>
	///     <description>No change tracking or identity map.</description>
	///   </item>
	///   <item>
	///     <description>No hidden runtime entity services.</description>
	///   </item>
	///   <item>
	///     <description>No automatic server/client query splitting or client-side execution of non-translatable query logic.</description>
	///   </item>
	///   <item>
	///     <description>No implicit navigation loading.</description>
	///   </item>
	/// </list>
	///
	/// <para>
	/// If an expression cannot be translated to SQL, it must be rewritten,
	/// mapped explicitly, or executed after materialization.
	/// </para>
	///
	/// <para><b>Machine-Readable Documentation</b></para>
	/// <para>
	/// Some XML documentation comments include compact machine-readable metadata in the form:
	/// </para>
	/// <para>
	/// AI-Tags: Key=Value; ...
	/// </para>
	/// <para>
	/// These tags describe execution semantics, composability, logical API grouping,
	/// and provider-related behavior.
	/// They are intended for tooling and AI agents and do not affect runtime behavior.
	/// </para>
	///
	/// <para>
	/// Primary entry points: <see cref="DataConnection"/>,
	/// <see cref="DataOptions"/>,
	/// <see cref="ITable{T}"/>,
	/// <see cref="MappingSchema"/>,
	/// <see cref="Sql"/>.
	/// </para>
	///
	/// <para><b>Related documentation</b></para>
	/// <para>
	/// The following files are included in the NuGet package:
	/// </para>
	/// <list type="bullet">
	///   <item>
	///     <description>docs/architecture.md — architectural model and translation pipeline.</description>
	///   </item>
	///   <item>
	///     <description>docs/ai-tags.md — specification of AI-Tags format and semantics.</description>
	///   </item>
	/// </list>
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static class LinqToDBArchitecture
	{
		/// <summary>
		/// Execution and query translation model.
		/// </summary>
		/// <remarks>
		/// <para><b>Conceptual pipeline:</b></para>
		///
		/// <list type="number">
		///   <item>
		///     <description>
		///       A LINQ query is constructed over <see cref="ITable{T}"/> or other queryable sources.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       The LINQ <c>Expression Tree</c> is captured and analyzed.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Sub-expressions that can be evaluated on the client without referencing query data
		///       may be computed and supplied as SQL parameters.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       The remaining expression is translated into an internal SQL Abstract Syntax Tree (SQL AST).
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Provider-specific rules transform the SQL AST according to dialect and capabilities.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Final SQL text and parameters are generated from the SQL AST and executed.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Results are materialized into objects without state tracking.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para>
		///   Query execution occurs only when the query is enumerated or explicitly materialized.
		/// </para>
		///
		/// <para><b>Translation rules:</b></para>
		///
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       Only SQL-representable constructs are translated.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       .NET methods are translatable only if built-in or explicitly mapped.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Expressions that do not depend on query data may be evaluated locally
		///       and passed to SQL as parameters.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Expressions that depend on query data (e.g., table columns)
		///       must be translatable to SQL.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Translation behavior depends on the selected provider.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para><b>Examples:</b></para>
		/// <example>
		/// <code>
		/// var x = 1;
		///
		/// // Fully evaluable on client before translation:
		/// // RunClientMethod(x * 2) is computed locally and passed as a SQL parameter.
		/// // Resulting SQL shape:
		/// //   WHERE t.Field1 = @p
		/// where t.Field1 == RunClientMethod(x * 2)
		///
		/// // Requires SQL mapping (depends on column value)
		/// where t.Field1 == RunClientMethod(t.Field2)
		/// </code>
		/// </example>
		///
		/// <para><b>Remote execution:</b></para>
		///
		/// <para>
		/// LinqToDB supports remote query execution (e.g., WCF, gRPC, SignalR, HTTP).
		/// In remote mode, the query is translated locally into SQL AST first,
		/// then the SQL AST (and parameters) is transferred to the remote side
		/// where provider-specific SQL text generation and execution occur.
		/// </para>
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static class TranslationModel
		{
		}

		/// <summary>
		/// Configuration model: connections, providers, and options.
		/// </summary>
		/// <remarks>
		/// <para><b>Core idea:</b> configuration is explicit and is provided via
		/// <see cref="DataOptions"/> / <see cref="DataConnection"/> (base: <see cref="DataContext"/>).</para>
		///
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       <see cref="DataConnection"/> represents a configured execution context:
		///       connection + provider + mapping + options.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       <see cref="DataOptions"/> is a composable configuration object used to
		///       construct a <see cref="DataConnection"/>.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Options can be applied globally (application defaults) or per connection,
		///       depending on how <see cref="DataOptions"/> is constructed and reused.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para><b>What configuration affects:</b></para>
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       Provider and SQL dialect (what SQL is generated and how).
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Mapping behavior (type conversions, column mapping, associations).
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Query translation and execution behavior (provider capabilities and option flags).
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para><b>Query-level configuration (Options):</b></para>
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       Some behaviors can be configured per query (for example, translation/execution switches).
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Temporary option overrides can be applied within a logical scope using
		///       <see cref="DataContext.UseOptions"/>, and are automatically restored
		///       when the scope ends.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Query options are part of the translation/execution contract: they influence generated SQL
		///       and/or execution strategy, not object materialization semantics.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para>
		/// Recommended approach: keep a small number of well-defined <see cref="DataOptions"/> presets
		/// (e.g., per provider/environment) and create <see cref="DataConnection"/> instances from them.
		/// </para>
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static class ConfigurationModel
		{
		}

		/// <summary>
		/// Provider model and provider-aware translation.
		/// </summary>
		/// <remarks>
		/// <para>
		/// LinqToDB generates SQL according to the configured database provider.
		/// Each provider defines its SQL dialect, capabilities, and translation rules.
		/// </para>
		///
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       The same LINQ query may produce different SQL depending on the selected provider.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       The translator attempts to generate SQL that is idiomatic and efficient
		///       for the configured provider.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       SQL portability is not guaranteed; behavior is provider-aware by design.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para><b>Controlling provider-specific behavior:</b></para>
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       Provider-specific query APIs (e.g., <c>AsSqlServer()</c>, <c>AsOracle()</c>)
		///       allow explicitly introducing provider-specific constructs within a LINQ query.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Provider-specific fragments may coexist in a single LINQ expression.
		///       During translation, only fragments relevant to the configured provider are applied.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Method and property mappings can be defined per provider (for example,
		///       via attributes such as <c>[Function]</c> or <c>[Property]</c>),
		///       allowing the same .NET member to translate differently depending on the provider.
		///       These provider-specific mappings are part of the same provider-aware translation mechanism.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para>
		/// Provider-aware behavior is explicit and configuration-driven; translation uses the configured provider.
		/// </para>
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static class ProviderModel
		{
		}

		/// <summary>
		/// Mapping model and associations.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Mapping in LinqToDB defines how .NET types and members correspond
		/// to database objects and SQL constructs.
		/// </para>
		///
		/// <para><b>Mapping model:</b></para>
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       Mapping can be defined via attributes or via <see cref="MappingSchema"/>.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Mapping can also be configured dynamically using Fluent Mapping
		///       via <see cref="MappingSchema"/> and related builder APIs.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Mapping determines table names, column names, keys,
		///       type conversions, and value transformations.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Mapping affects SQL translation, not runtime object behavior.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para>
		/// Fluent mapping provides an explicit, runtime-configurable alternative
		/// to attribute-based mapping. It affects SQL translation behavior,
		/// not runtime entity state management.
		/// </para>
		///
		/// <para>
		/// Type conversions and custom mappings are part of the translation contract:
		/// they define how expressions are represented in SQL and how values
		/// are materialized.
		/// </para>
		///
		/// <para><b>Associations:</b></para>
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       Associations describe relational links between mapped entities.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Associations participate in query translation and are expressed
		///       as SQL joins or related SQL constructs.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Associations do not introduce automatic state tracking,
		///       identity management, or hidden loading behavior.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para>
		/// Query shape determines how associations are translated.
		/// Explicit query composition defines the resulting SQL.
		/// </para>
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static class MappingModel
		{
		}

		/// <summary>
		/// Method translation and extension model.
		/// </summary>
		/// <remarks>
		/// <para>
		/// LinqToDB translates supported .NET methods and properties into SQL.
		/// Translation is explicit and provider-aware.
		/// </para>
		///
		/// <para><b>Built-in translation:</b></para>
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       A large set of .NET methods and properties (including common
		///       BCL members) is supported and translated to SQL constructs.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Translation behavior may differ per provider depending on
		///       SQL dialect and feature availability.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para><b>Custom method mapping:</b></para>
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       Application code may define custom method and property mappings
		///       that translate to SQL.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       The same .NET member may translate differently for different providers.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Unmapped methods that depend on query data cannot be translated.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para>
		/// Method translation is part of the query translation contract.
		/// It defines how expression tree nodes are represented in SQL,
		/// not how .NET code is executed at runtime.
		/// </para>
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static class ExtensionModel
		{
		}

		/// <summary>
		/// Interceptors and cross-cutting behavior.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Interceptors provide explicit extension points for modifying
		/// or observing query translation and execution behavior.
		/// </para>
		///
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       Interceptors can participate in query processing,
		///       command generation, or execution stages.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Typical use cases include logging, metrics collection,
		///       auditing, command rewriting, filtering, or policy enforcement.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Interceptors operate on the translation/execution pipeline,
		///       not on a built-in runtime entity management model.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para>
		/// The core LinqToDB library does not provide automatic state tracking,
		/// identity management, or implicit persistence services.
		/// Such behaviors can be implemented explicitly using interceptors
		/// or external extensions.
		/// </para>
		///
		/// <para>
		/// Lightweight identity and entity management utilities are available
		/// in separate packages (e.g., LinqToDB.Tools), and are not part of the core
		/// translation engine.
		/// </para>
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static class Interceptors
		{
		}

		/// <summary>
		/// Data modification API (CRUD operations).
		/// </summary>
		/// <remarks>
		/// <para>
		/// In addition to standard LINQ query operators (primarily oriented
		/// around SELECT), LinqToDB provides a full set of explicit
		/// data modification APIs.
		/// </para>
		///
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       <b>Insert</b> operations for adding records.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       <b>Update</b> operations for modifying records,
		///       including set-based updates.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       <b>Delete</b> operations for removing records.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Extended operations (e.g., output-returning variants)
		///       are available where supported by the provider.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para>
		/// Data modification operations translate directly to SQL statements
		/// and execute immediately.
		/// </para>
		///
		/// <para>
		/// LinqToDB does not implement a unit-of-work or deferred persistence model.
		/// There is no <c>SaveChanges()</c> method. Application code explicitly
		/// invokes modification operations when changes are required.
		/// </para>
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static class DataModificationAPI
		{
		}

		/// <summary>
		/// Advanced SQL features.
		/// </summary>
		/// <remarks>
		/// <para>
		/// LinqToDB provides access to advanced SQL constructs beyond basic
		/// CRUD-style queries. These features operate at the SQL level and
		/// participate directly in query translation.
		/// </para>
		///
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       <b>Merge API</b> enables set-based INSERT/UPDATE/DELETE operations
		///       using database-native MERGE semantics (where supported).
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       <b>Temporary tables</b> allow explicit creation and usage of
		///       provider-supported temporary storage within a session.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       <b>BulkCopy</b> provides efficient bulk data insertion
		///       using provider-specific optimized mechanisms.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       <b>LoadWith</b> modifies query shape to include associated data
		///       during SQL generation. It affects generated joins and projections,
		///       but does not introduce automatic state tracking or lazy loading.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       <b>Window functions</b> expose SQL windowing capabilities
		///       (e.g., OVER, PARTITION BY, ORDER BY) through LINQ APIs (LINQ-shaped constructs),
		///       enabling advanced analytical queries.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para>
		/// Availability and SQL shape of these features depend on provider capabilities.
		/// All advanced constructs are explicit and translation-driven.
		/// </para>
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static class AdvancedSqlFeatures
		{
		}

		/// <summary>
		/// Common incorrect assumptions and anti-patterns.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The following assumptions do not apply to LinqToDB:
		/// </para>
		///
		/// <list type="bullet">
		///   <item>
		///     <description>
		///       There is no implicit change tracking or identity map in the core library.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       There is no implicit in-memory graph synchronization.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       There is no deferred persistence model and no <c>SaveChanges()</c>.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Queries do not automatically split between server and client execution.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Navigation properties do not trigger automatic loading.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       SQL generated for one provider is not guaranteed to be portable
		///       to another provider without explicit provider-aware constructs.
		///     </description>
		///   </item>
		///   <item>
		///     <description>
		///       Unmapped .NET methods that depend on query data cannot be translated.
		///     </description>
		///   </item>
		/// </list>
		///
		/// <para>
		/// LinqToDB favors explicit, deterministic, and set-based data access.
		/// Application code defines SQL intent directly through LINQ expressions.
		/// </para>
		/// <para>
		/// LinqToDB is a translation engine, not an object state management framework.
		/// </para>
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static class AntiPatterns
		{
		}
	}
}
