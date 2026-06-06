#if !EF31
using System.Collections.Generic;

using LinqToDB.Mapping;

namespace LinqToDB.EntityFrameworkCore.Internal
{
	/// <summary>
	/// Internal marker entity that represents the hidden join table of an EF Core
	/// many-to-many (skip navigation) relationship as a queryable LINQ To DB table.
	/// <para>
	/// Closing the generic over the related entity types plus the join entity type yields a
	/// distinct CLR type per relationship. This is required because EF Core's implicit join
	/// entity uses a single shared CLR type (<see cref="Dictionary{TKey, TValue}"/>) for every
	/// implicit many-to-many relationship and therefore cannot be addressed by CLR type alone.
	/// </para>
	/// <para>
	/// Foreign key columns are supplied as dynamic columns by <see cref="EFCoreMetadataReader"/>;
	/// the type itself declares no mapped members other than the dynamic-columns store.
	/// </para>
	/// </summary>
	/// <typeparam name="TThis">Entity type on the declaring side of the navigation.</typeparam>
	/// <typeparam name="TOther">Entity type on the target side of the navigation.</typeparam>
	/// <typeparam name="TJoin">
	/// CLR type of the EF join entity (<see cref="Dictionary{TKey, TValue}"/> for an implicit join,
	/// or the join class for an explicit <c>UsingEntity&lt;TJoin&gt;</c>). Acts as a discriminator so
	/// that multiple distinct relationships between the same <typeparamref name="TThis"/>/<typeparamref name="TOther"/>
	/// pair (e.g. via different explicit join entities) map to distinct marker types.
	/// </typeparam>
	internal sealed class EfJoinTable<TThis, TOther, TJoin>
		where TThis  : class
		where TOther : class
		where TJoin  : class
	{
		[DynamicColumnsStore]
		public IDictionary<string, object> Values { get; set; } = null!;
	}
}
#endif
