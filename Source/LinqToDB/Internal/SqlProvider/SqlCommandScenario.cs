using System.Collections.Generic;
using System.Data;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.SqlProvider
{
	/// <summary>
	/// How a <see cref="SqlCommandStep"/> is executed against the database.
	/// </summary>
	public enum SqlStepKind
	{
		/// <summary>Executed as a non-query; its result is the rows-affected count.</summary>
		NonQuery,
		/// <summary>Executed as a scalar query; its result is the scalar value.</summary>
		Scalar,
		/// <summary>Executed as a reader; its result is a recordset consumed by the caller.</summary>
		Reader,
	}

	/// <summary>
	/// Error policy for a <see cref="SqlCommandStep"/>.
	/// </summary>
	public enum SqlStepOnError
	{
		/// <summary>Propagate any error (default).</summary>
		Throw,
		/// <summary>Swallow errors from this step and continue with the next step.</summary>
		Ignore,
	}

	/// <summary>
	/// Predicate applied to an earlier step's unified result value to gate a <see cref="SqlCommandStep"/>.
	/// </summary>
	public enum SqlStepConditionKind
	{
		/// <summary>Run when the referenced step's result is <see langword="null"/>.</summary>
		ResultIsNull,
		/// <summary>Run when the referenced step's result is not <see langword="null"/>.</summary>
		ResultIsNotNull,
		/// <summary>Run when the referenced step's result equals zero.</summary>
		ResultIsZero,
		/// <summary>Run when the referenced step's result is a non-zero number.</summary>
		ResultIsNonZero,
	}

	/// <summary>
	/// Identifies a synthetic command fragment rendered for a step instead of its full statement.
	/// </summary>
	public enum SqlCommandFragment
	{
		/// <summary>No fragment; the step renders its full <see cref="SqlCommandStep.Statement"/>.</summary>
		None,
		/// <summary>Provider-specific identity-reset DDL fragment (rendered per identity field).</summary>
		IdentityReseed,
	}

	/// <summary>
	/// A gate: the owning step runs only if the condition holds against an earlier step's result (explicit reference).
	/// </summary>
	public sealed record SqlStepCondition
	{
		/// <summary>The predicate kind.</summary>
		public required SqlStepConditionKind Kind { get; init; }

		/// <summary>Index (into <see cref="SqlCommandScenario.Steps"/>) of the step whose result is tested.</summary>
		public required int SourceStepIndex { get; init; }
	}

	/// <summary>
	/// Forwarding: a value produced by an earlier step becomes a parameter of the owning step (explicit reference).
	/// </summary>
	public sealed record SqlStepParameterBinding
	{
		/// <summary>Index (into <see cref="SqlCommandScenario.Steps"/>) of the step whose result supplies the value.</summary>
		public required int SourceStepIndex { get; init; }

		/// <summary>The parameter in the owning step's statement that receives the forwarded value (used read-only, by identity).</summary>
		public required SqlParameter Target { get; init; }
	}

	/// <summary>
	/// A single executable step within a <see cref="SqlCommandScenario"/>. Pure execution data — the step declares only
	/// how it runs; its output lands in the <see cref="SqlCommandExecutionContext"/>. Behavior lives in the interpreter.
	/// </summary>
	public sealed record SqlCommandStep
	{
		/// <summary>The statement rendered for this step (via the SQL builder).</summary>
		public required SqlStatement Statement { get; init; }

		/// <summary>Synthetic fragment to render instead of the full statement, or <see cref="SqlCommandFragment.None"/>.</summary>
		public SqlCommandFragment Fragment { get; init; } = SqlCommandFragment.None;

		/// <summary>Identity-field index for <see cref="SqlCommandFragment.IdentityReseed"/> fragments; otherwise <c>-1</c>.</summary>
		public int FragmentFieldIndex { get; init; } = -1;

		/// <summary>How the step is executed.</summary>
		public required SqlStepKind Kind { get; init; }

		/// <summary>Error policy for the step.</summary>
		public SqlStepOnError OnError { get; init; } = SqlStepOnError.Throw;

		/// <summary>Optional gate; when set, the step runs only if the condition holds against an earlier step's result.</summary>
		public SqlStepCondition? RunIf { get; init; }

		/// <summary>Name of the output parameter used when the step produces its value via a database output parameter (Oracle/Firebird identity).</summary>
		public string? OutParameterName { get; init; }

		/// <summary>Database type of the output parameter, when <see cref="OutParameterName"/> is set.</summary>
		public DbType OutParameterDbType { get; init; } = DbType.Decimal;

		/// <summary>Parameters of this step whose values are forwarded from earlier steps' results.</summary>
		public IReadOnlyList<SqlStepParameterBinding> ParameterBindings { get; init; } = [];
	}

	/// <summary>
	/// An ordered list of steps produced for one logical database operation, plus which step(s) yield the outcome.
	/// The scenario is a logical plan; how its steps map to physical commands is decided by <see cref="SqlCommandGroupPlan"/>.
	/// </summary>
	public sealed record SqlCommandScenario
	{
		/// <summary>The ordered steps.</summary>
		public required IReadOnlyList<SqlCommandStep> Steps { get; init; }

		/// <summary>
		/// Candidate result step indices; the scenario outcome is the value of the last <em>executed</em> candidate.
		/// A gate-only step (e.g. an existence check) is not listed here.
		/// </summary>
		public required IReadOnlyList<int> OutcomeSteps { get; init; }
	}

	/// <summary>
	/// A group of steps executed as one physical database command (a single round-trip). A group with more than one
	/// step is a combined (multi-statement) command whose results are read across result sets.
	/// </summary>
	public sealed record SqlCommandGroup
	{
		/// <summary>Indices (into <see cref="SqlCommandScenario.Steps"/>) of the steps in this group, in execution order.</summary>
		public required IReadOnlyList<int> StepIndexes { get; init; }
	}

	/// <summary>
	/// The physical grouping of a <see cref="SqlCommandScenario"/> into command groups, produced by the DML service.
	/// </summary>
	public sealed record SqlCommandGroupPlan
	{
		/// <summary>The ordered command groups.</summary>
		public required IReadOnlyList<SqlCommandGroup> Groups { get; init; }
	}

	/// <summary>
	/// Threaded through the scenario interpreter; the single carrier of inter-step data. Holds one unified result value
	/// per step (rows-affected / scalar / output-parameter value / recordset marker). Gates, the outcome, and parameter
	/// forwarding read from it by explicit step index — nothing is pushed or declared by the producing step.
	/// </summary>
	public sealed class SqlCommandExecutionContext
	{
		readonly object?[] _results;
		readonly bool[]    _executed;

		/// <summary>Creates a context sized for <paramref name="stepCount"/> steps.</summary>
		/// <param name="stepCount">Number of steps in the scenario.</param>
		public SqlCommandExecutionContext(int stepCount)
		{
			_results  = new object?[stepCount];
			_executed = new bool[stepCount];
		}

		/// <summary>Records the result value of a step and marks it executed.</summary>
		public void SetResult(int step, object? value)
		{
			_results [step] = value;
			_executed[step] = true;
		}

		/// <summary>Gets the recorded result value of a step (<see langword="null"/> if not executed).</summary>
		public object? GetResult(int step) => _results[step];

		/// <summary>Whether the step has executed.</summary>
		public bool WasExecuted(int step) => _executed[step];
	}
}
