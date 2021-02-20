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
		[Extension("", ServerSideOnly = true, BuilderType = typeof(DB2LUWCollationBuilder)    , Configuration = ProviderName.DB2LUW)]
		[Extension("", ServerSideOnly = true, BuilderType = typeof(PostgreSQLCollationBuilder), Configuration = ProviderName.PostgreSQL)]
		[Extension("", ServerSideOnly = true, BuilderType = typeof(NamedCollationBuilder))]
		public static string Collate(this string expr, string collation)
			=> throw new InvalidOperationException($"{nameof(Sql)}.{nameof(Sql.Collate)} is server-side only API.");

		internal class NamedCollationBuilder : IExtensionCallBuilder
		{
			private static readonly Regex _collationValidator = new Regex(@"^[a-zA-Z0-9_\.-@]+$", RegexOptions.Compiled);

			public void Build(ISqExtensionBuilder builder)
			{
				var expr = builder.GetExpression("expr");
				var collation = builder.GetValue<string>("collation");

				if (!ValidateCollation(collation))
					throw new InvalidOperationException($"Invalid collation: {collation}");

				builder.ResultExpression = new SqlExpression(typeof(string), $"{{0}} COLLATE {collation}", Precedence.Primary, expr);
			}

			/// <summary>
			/// Simple check for invalid collation names.
			/// </summary>
			/// <param name="collation">Collation name to check.</param>
			/// <returns>False if invalid characters found, else true.</returns>
			private bool ValidateCollation(string collation)
			{
				return !string.IsNullOrWhiteSpace(collation) && _collationValidator.IsMatch(collation);
			}
		}

		internal class PostgreSQLCollationBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var expr      = builder.GetExpression("expr");
				var collation = builder.GetValue<string>("collation").Replace("\"", "\"\"");

				builder.ResultExpression = new SqlExpression(typeof(string), $"{{0}} COLLATE \"{collation}\"", Precedence.Primary, expr);
			}
		}

		internal class DB2LUWCollationBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var expr      = builder.GetExpression("expr");
				var collation = builder.GetValue<string>("collation");

				// collation cannot be parameter
				builder.ResultExpression = new SqlExpression(typeof(string), $"COLLATION_KEY_BIT({{0}}, {{1}})", Precedence.Primary, expr, new SqlValue(typeof(string), collation));
			}
		}
	}
}
