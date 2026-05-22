using System;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.DataProvider.Firebird
{
	public static class FirebirdExtensions
	{
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "ext is an extension point")]
		public static IFirebirdExtensions? Firebird(this IFirebirdExtensions? ext) => null;

		[Sql.Extension("UUID_TO_CHAR({guid})", PreferServerSide = true, IsNullable = Sql.IsNullableType.SameAsFirstParameter)]
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "ext is an extension point")]
		public static string? UuidToChar(this IFirebirdExtensions? ext, [ExprParameter] Guid? guid) =>
			guid?.ToString("D").ToUpperInvariant();
	}
}
