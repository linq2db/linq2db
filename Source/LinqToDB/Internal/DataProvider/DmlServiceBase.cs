using System;

using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Base class for provider-specific <see cref="IDmlService"/> implementations. By default
	/// <see cref="BuildCommandScenario"/> returns <see langword="null"/> (the runner falls back to the legacy
	/// <see cref="ISqlBuilder.CommandCount"/> / <c>BuildCommand</c> splitting); providers override it for identity,
	/// truncate-reset, etc. <see cref="PlanScenario"/> defaults to all-singleton (sequential) groups.
	/// </summary>
	public abstract class DmlServiceBase : IDmlService
	{
		/// <summary>
		/// Returns <see langword="null"/> by default — the runner uses the legacy command-splitting path
		/// (<see cref="ISqlBuilder.CommandCount"/> / <c>BuildCommand</c>). Providers override to build an explicit
		/// scenario (identity retrieval, per-field truncate reset, etc.), using <paramref name="factory"/> to
		/// construct any synthetic statements (e.g. an identity <c>SELECT</c>).
		/// </summary>
		public virtual SqlCommandScenario? BuildCommandScenario(SqlStatement statement, SqlProviderFlags flags, ISqlExpressionFactory factory)
		{
			return null;
		}

		/// <summary>
		/// Default plan: every step is its own physical command group (sequential). Providers override to combine
		/// contiguous non-gated steps into one round-trip.
		/// </summary>
		public virtual SqlCommandGroupPlan PlanScenario(SqlCommandScenario scenario)
		{
			var groups = new SqlCommandGroup[scenario.Steps.Count];

			for (var i = 0; i < scenario.Steps.Count; i++)
				groups[i] = new SqlCommandGroup { StepIndexes = [i] };

			return new SqlCommandGroupPlan { Groups = groups };
		}

		public bool IsTableNotFoundException(Exception exception)
		{
			ArgumentNullException.ThrowIfNull(exception);

			for (var current = exception; current != null; current = current.InnerException)
			{
				if (IsTableNotFoundExceptionCore(current))
					return true;

				if (current is AggregateException agg)
				{
					foreach (var inner in agg.Flatten().InnerExceptions)
						if (IsTableNotFoundException(inner))
							return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Detects a provider-specific "table not found" exception.
		/// Called for every link of the inner-exception chain.
		/// </summary>
		protected abstract bool IsTableNotFoundExceptionCore(Exception exception);

		/// <summary>
		/// Matches <paramref name="marker"/> against the exception's type name or its message.
		/// The message check is needed for remote (gRPC / HTTP) data contexts where the original
		/// provider exception is wrapped — the type name survives only as text inside the
		/// wrapping exception's <see cref="Exception.Message"/> (populated via <see cref="Exception.ToString"/>).
		/// </summary>
		protected static bool TypeOrMessageContains(Exception exception, string marker)
		{
			var typeName = exception.GetType().FullName;

			return (typeName != null && typeName.Contains(marker, StringComparison.Ordinal))
				|| exception.Message.Contains(marker, StringComparison.Ordinal);
		}

		/// <summary>
		/// True if <paramref name="exception"/>'s <see cref="Exception.HResult"/> matches
		/// <paramref name="hResult"/>, or the remote-transport message wrapper contains the
		/// canonical hex form ("0x1234ABCD").
		/// </summary>
		protected static bool HResultMatches(Exception exception, int hResult)
		{
			if (exception.HResult == hResult)
				return true;

			var hex = "0x" + hResult.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
			return exception.Message.Contains(hex, StringComparison.OrdinalIgnoreCase);
		}
	}
}
