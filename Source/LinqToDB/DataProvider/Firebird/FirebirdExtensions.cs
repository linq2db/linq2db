using System;

namespace LinqToDB.DataProvider.Firebird
{
	public static class FirebirdExtensions
	{
		public static IFirebirdExtensions? Firebird(
#pragma warning disable IDE0060 // Remove unused parameter
			this IFirebirdExtensions? ext
#pragma warning restore IDE0060 // Remove unused parameter
			) => null;

		[Sql.Extension("UUID_TO_CHAR({guid})", PreferServerSide = true, IsNullable = Sql.IsNullableType.SameAsFirstParameter)]
		public static string? UuidToChar(
#pragma warning disable IDE0060 // Remove unused parameter
			this IFirebirdExtensions? ext,
#pragma warning restore IDE0060 // Remove unused parameter
			[ExprParameter] Guid? guid) => guid?.ToString("D").ToUpperInvariant();
	}
}
