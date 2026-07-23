using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.SqlProvider
{
	public sealed class OptimizationContext : ISqlBuilderRenderContext
	{
		private IQueryParametersNormalizer?                      _parametersNormalizer;
		private Dictionary<SqlParameter, SqlParameter>?          _parametersMap;
		private List<SqlParameter>?                              _actualParameters;
		private Dictionary<(DbDataType, object?), SqlParameter>? _dynamicParameters;

		public DataOptions                   DataOptions      { get; }
		public SqlProviderFlags              SqlProviderFlags { get; }
		public MappingSchema                 MappingSchema    { get; }
		public SqlExpressionConvertVisitor   ConvertVisitor   { get; }
		public SqlExpressionOptimizerVisitor OptimizerVisitor { get; }

		readonly Func<IQueryParametersNormalizer>           _parametersNormalizerFactory;

		public SqlQueryVisitor.IVisitorTransformationInfo TransformationInfo =>
			field ??= new SqlQueryVisitor.VisitorTransformationInfo();

		public SqlQueryVisitor.IVisitorTransformationInfo TransformationInfoConvert =>
			field ??= new SqlQueryVisitor.VisitorTransformationInfo();

		public OptimizationContext(
			EvaluationContext                evaluationContext,
			DataOptions                      dataOptions,
			SqlProviderFlags                 sqlProviderFlags,
			MappingSchema                    mappingSchema,
			SqlExpressionOptimizerVisitor    optimizerVisitor,
			SqlExpressionConvertVisitor      convertVisitor,
			ISqlExpressionFactory            factory,
			bool                             isParameterOrderDepended,
			Func<IQueryParametersNormalizer> parametersNormalizerFactory)
		{
			EvaluationContext              = evaluationContext;
			DataOptions                    = dataOptions;
			SqlProviderFlags               = sqlProviderFlags;
			MappingSchema                  = mappingSchema;
			OptimizerVisitor               = optimizerVisitor;
			ConvertVisitor                 = convertVisitor;
			Factory                        = factory;
			IsParameterOrderDependent      = isParameterOrderDepended;
			_parametersNormalizerFactory   = parametersNormalizerFactory;
		}

		public EvaluationContext     EvaluationContext              { get; }
		public bool                  IsParameterOrderDependent      { get; }
		public ISqlExpressionFactory Factory                        { get; }

		public bool HasParameters() => _actualParameters?.Count > 0;

		public IReadOnlyList<SqlParameter> GetParameters() => _actualParameters ?? (IReadOnlyList<SqlParameter>)[];

		/// <summary>
		/// When set (combined multi-statement command render), a later statement reuses an earlier statement's parameter
		/// when they share a value accessor (<see cref="SqlParameter.AccessorId"/>), so the merged command references one
		/// @p (matching the remote separate-command path) instead of minting @p_1. Cross-statement only: enable on the
		/// render context and call <see cref="PromoteParametersForSharing"/> between statements.
		/// </summary>
		public bool ShareParametersByAccessor { get; set; }

		private Dictionary<int, SqlParameter>? _sharedByAccessor;
		private Dictionary<(string?, DbDataType, object?), SqlParameter>? _sharedByValue;

		public SqlParameter AddParameter(SqlParameter parameter)
		{
			var returnValue = parameter;

			if (!IsParameterOrderDependent && _parametersMap?.TryGetValue(parameter, out var newParameter) == true)
			{
				returnValue = newParameter;
			}
			else if (ShareParametersByAccessor && !IsParameterOrderDependent && TryGetSharedParameter(parameter, out var shared))
			{
				// A prior statement in this combined command already emitted an equivalent parameter (same value accessor, or
				// same name+value when it has no accessor - e.g. a convert-derived LIKE pattern). Reuse it so the merged command
				// references one @p (matching remote's separate-command path) instead of minting @p_1. Cross-statement only:
				// within-statement duplicates still uniquify (see PromoteParametersForSharing).
				returnValue = shared;
				(_parametersMap ??= new()).Add(parameter, returnValue);
			}
			else
			{
				var newName = NormalizeParameterName(parameter.Name);

				if (IsParameterOrderDependent || !string.Equals(newName, parameter.Name, StringComparison.Ordinal))
				{
					returnValue = new SqlParameter(parameter.Type, newName, parameter.Value)
					{
						AccessorId     = parameter.AccessorId,
						ValueConverter = parameter.ValueConverter,
						NeedsCast      = parameter.NeedsCast,
					};
				}

				if (!IsParameterOrderDependent)
					(_parametersMap ??= new()).Add(parameter, returnValue);

				(_actualParameters ??= new()).Add(returnValue);
			}

			return returnValue;
		}

		// Finds a parameter emitted by an earlier statement of the current combined command equivalent to <paramref
		// name="parameter"/>: by value accessor when it has one, otherwise by (name, type, resolved value) - the latter
		// covers convert-derived constants (e.g. LIKE patterns) whose AccessorId is null. Requires bound values for the
		// value path, so it is a no-op there at compile time (EvaluationContext.ParameterValues null).
		bool TryGetSharedParameter(SqlParameter parameter, [MaybeNullWhen(false)] out SqlParameter shared)
		{
			if (parameter.AccessorId is {} accessorId)
			{
				if (_sharedByAccessor != null && _sharedByAccessor.TryGetValue(accessorId, out shared))
					return true;
			}
			else if (_sharedByValue != null && EvaluationContext.ParameterValues != null)
			{
				var value = parameter.GetParameterValue(EvaluationContext.ParameterValues);

				if (_sharedByValue.TryGetValue((parameter.Name, value.DbDataType, value.ProviderValue), out shared))
					return true;
			}

			shared = null;
			return false;
		}

		/// <summary>
		/// Promotes the parameters emitted so far (through the just-rendered statement) into the cross-statement sharing
		/// map, so a later statement in the same combined command that references the same value accessor reuses them.
		/// No-op unless <see cref="ShareParametersByAccessor"/> is set. Call between statements of a combined command.
		/// </summary>
		public void PromoteParametersForSharing()
		{
			if (!ShareParametersByAccessor || _actualParameters == null)
				return;

			foreach (var parameter in _actualParameters)
			{
				if (parameter.AccessorId is {} accessorId)
				{
					_sharedByAccessor ??= new();
					_sharedByAccessor[accessorId] = parameter;
				}
				else if (EvaluationContext.ParameterValues != null)
				{
					var value = parameter.GetParameterValue(EvaluationContext.ParameterValues);
					var key   = (parameter.Name, value.DbDataType, value.ProviderValue);

					_sharedByValue ??= new();
					_sharedByValue[key] = parameter;
				}
			}
		}

		/// <summary>
		/// Normalizes <paramref name="name"/> through the query parameters name normalizer, registers it
		/// as used, and returns the resulting collision-free name.
		/// </summary>
		public string? NormalizeParameterName(string? name)
		{
			return (_parametersNormalizer ??= _parametersNormalizerFactory()).Normalize(name);
		}

		public SqlParameter SuggestDynamicParameter(DbDataType dbDataType, object? value)
		{
			var key = (dbDataType, value);

			if (_dynamicParameters == null || !_dynamicParameters.TryGetValue(key, out var param))
			{
				// converting to SQL Parameter
				// real name (in case of conflicts) will be generated on later stage in AddParameter method
				param = new SqlParameter(dbDataType, "value", value);

				_dynamicParameters ??= new();
				_dynamicParameters.Add(key, param);
			}

			return param;
		}

		public void ClearParameters()
		{
			// must discard instance instead of Clean as it is returned by GetParameters
			_actualParameters     = null;
			_parametersNormalizer = null;
			// Each rendered group is a separate DbCommand carrying only its own parameters, so the dedup map
			// must reset at the group boundary too — otherwise a later group's parameters are collapsed onto
			// the previous group's identities and never emitted. Breaks gated multi-group scenarios such as the
			// InsertOrReplace/Upsert UPDATE→INSERT emulation, where both groups carry value parameters.
			_parametersMap        = null;
			_sharedByAccessor     = null;
			_sharedByValue        = null;
		}

		/// <summary>
		/// The whole render-time pipeline, in one traversal. <see cref="SqlExpressionConvertVisitor"/> derives from
		/// <see cref="SqlExpressionOptimizerVisitor"/>, so a single <see cref="SqlExpressionConvertVisitor.Convert"/> pass
		/// optimizes, lowers, and runs the CompareNulls null-guard reduce on each node as it passes over it — collapsing the
		/// old five passes (optimize, convert, reduce, simplify, convert) to one. Optimization over the <b>un-lowered</b> AST
		/// (the rules that only match abstract nodes) already happens during query build in <c>SelectQueryOptimizerVisitor</c>,
		/// so re-running it here was redundant.
		/// </summary>
		public T OptimizeAndConvertAll<T>(T element, NullabilityContext nullabilityContext)
			where T : class, IQueryElement
		{
#if BUGCHECK
			// A Transform pass must leave what it is handed untouched. That element is the query's CACHED statement: it is
			// re-rendered on every execution and by the remote path, so one write reaching it corrupts every later render —
			// and the render that breaks is the NEXT one, which makes the failure look unrelated to the rule that caused it.
			// Only the Phase-S prepare runs a Modify pass, and it owns its statement (Monitor.Enter), so it is exempt.
			var before = ConvertVisitor.VisitMode == VisitMode.Transform ? element.ToDebugString() : null;
#endif

			var result = (T)ConvertVisitor.Convert(this, nullabilityContext, element);

#if BUGCHECK
			if (before != null)
			{
				var after = element.ToDebugString();

				if (after != before)
					throw new InvalidOperationException($"Transform-mode convert mutated the element it was given.{Environment.NewLine}BEFORE:{Environment.NewLine}{before}{Environment.NewLine}AFTER:{Environment.NewLine}{after}");
			}
#endif

			return result;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public T? Optimize<T>(T? element, NullabilityContext nullabilityContext)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			var newElement = OptimizerVisitor.Optimize(EvaluationContext, nullabilityContext, DataOptions, MappingSchema, element, false);

			return (T)newElement;
		}
	}
}
