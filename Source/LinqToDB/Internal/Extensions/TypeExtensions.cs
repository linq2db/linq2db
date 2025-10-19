using System;

namespace LinqToDB.Internal.Extensions
{
	internal static class TypeExtensions
	{
		extension(Type type)
		{
			/// <summary>
			///		Gets the underlying type code of the specified <see cref="Type"/>.
			/// </summary>
			public TypeCode TypeCode => Type.GetTypeCode(type);

			/// <summary>
			///		Returns <see langword="true" /> iff the type is <c><see langword="typeof" />(<see cref="string"/>)</c>.
			/// </summary>
			public bool IsStringType => type == typeof(string);

#if NET8_0_OR_GREATER
			/// <summary>
			///		Returns <see langword="true" /> iff the type is <c><see langword="typeof" />(<see cref="MemoryExtensions"/>)</c>.
			/// </summary>
			public bool IsMemoryExtensionsType => type == typeof(MemoryExtensions);
#endif
		}
	}
}
