using System;

using LinqToDB.Mapping;

namespace Cli.NewCliFeatures.FluentSQLite
{
	static class CustomFluentExtensions
	{
		internal static EntityMappingBuilder<T> SpecificTypeHelper<T>(this EntityMappingBuilder<T> builder, Person? _) where T : Person =>
			throw new InvalidOperationException();
		internal static EntityMappingBuilder<T> SpecificTypeHelper<T>(this EntityMappingBuilder<T> builder, Child? _) where T : Child =>
			throw new InvalidOperationException();
		// fallback for other types
		internal static EntityMappingBuilder<T> SpecificTypeHelper<T>(this EntityMappingBuilder<T> builder, T? _) where T : class =>
			throw new InvalidOperationException();

		internal static EntityMappingBuilder<T> AllTypesHelper<T>(this EntityMappingBuilder<T> builder, T? _) where T : class =>
			throw new InvalidOperationException();
	}
}
