using System;
using System.Text.RegularExpressions;
using LinqToDB.SqlQuery;

namespace LinqToDB
{
	public static partial class Sql
	{
		/// <summary>
		/// Apply collation to a string expression.
		/// </summary>
		/// <param name="expr">Expression to apply collation to.</param>
		/// <param name="collation">Collation to apply.</param>
		/// <returns>Expression with specified collation.</returns>
		/// <remarks>Server-side only, does not perform validation on collation name beyond simple valid character checks.</remarks>
		[Sql.Extension("{expr} COLLATE {collation}", ServerSideOnly = true, BuilderType = typeof(NamedCollationBuilder))]
		public static string Collate(this string expr, string collation)
			=> throw new InvalidOperationException("Collation is server-side only.");

		internal class NamedCollationBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var expr = builder.GetExpression("expr");
				var collation = builder.GetValue<string>("collation");

				if (!ValidateCollation(collation))
					throw new InvalidOperationException($"Invalid collation: {collation}");

				builder.ResultExpression = new SqlExpression($"{{0}} COLLATE {collation}", Precedence.Primary, expr);
			}

			/// <summary>
			/// Simple check for invalid collation names.
			/// </summary>
			/// <param name="collation">Collation name to check.</param>
			/// <returns>False if invalid characters found, else true.</returns>
			protected virtual bool ValidateCollation(string collation)
			{
				return !string.IsNullOrWhiteSpace(collation) 
					&& Regex.IsMatch(collation, @"^[a-zA-Z0-9_\.-@]+$");
			}
		}
	}
}
