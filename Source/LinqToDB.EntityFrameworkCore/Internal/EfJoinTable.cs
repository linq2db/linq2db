#if !EF31
using System.Collections.Generic;

using LinqToDB.Mapping;

namespace LinqToDB.EntityFrameworkCore.Internal
{
	/// <summary>
	/// Internal marker entity that represents the hidden join table of an EF Core
	/// many-to-many (skip navigation) relationship as a queryable LINQ To DB table.
	/// <para>
	/// Closing the generic over the two related entity types yields a distinct CLR type
	/// per relationship. This is required because EF Core's implicit join entity uses a
	/// single shared CLR type (<see cref="Dictionary{TKey, TValue}"/>) for every
	/// many-to-many relationship and therefore cannot be addressed by CLR type.
	/// </para>
	/// <para>
	/// Foreign key columns are supplied as dynamic columns by <see cref="EFCoreMetadataReader"/>;
	/// the type itself declares no mapped members other than the dynamic-columns store.
	/// </para>
	/// </summary>
	/// <typeparam name="TThis">Entity type on the declaring side of the navigation.</typeparam>
	/// <typeparam name="TOther">Entity type on the target side of the navigation.</typeparam>
	internal sealed class EfJoinTable<TThis, TOther>
		where TThis  : class
		where TOther : class
	{
		[DynamicColumnsStore]
		public IDictionary<string, object> Values { get; set; } = null!;
	}
}
#endif
