using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.SqlProvider
{
	/// <summary>
	/// The narrow render-time context the SQL builder needs from the query pipeline: the <see cref="EvaluationContext"/>
	/// for evaluating expressions against this run's parameter values, and <see cref="AddParameter"/> to register the
	/// parameters the rendered command references. The builder depends only on this — not on the full
	/// <see cref="OptimizationContext"/>, which also drives the optimize/convert pipeline the builder no longer touches.
	/// </summary>
	public interface ISqlBuilderRenderContext
	{
		/// <summary>Evaluation context carrying this run's parameter values, used for render-time expression evaluation.</summary>
		EvaluationContext EvaluationContext { get; }

		/// <summary>
		/// Registers <paramref name="parameter"/> with the command's parameter set and returns the parameter to render
		/// (possibly a deduplicated/renamed instance).
		/// </summary>
		SqlParameter AddParameter(SqlParameter parameter);

		/// <summary>
		/// Reserves <paramref name="name"/> in this run's parameter-name bucket and returns the resulting collision-free
		/// name. Used by a builder to pre-register names (e.g. CTE variable names) so generated parameter names can't
		/// collide with them.
		/// </summary>
		string? NormalizeParameterName(string? name);
	}
}
