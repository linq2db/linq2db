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
		/// The whole render-time pipeline. <see cref="SqlExpressionConvertVisitor"/> derives from
		/// <see cref="SqlExpressionOptimizerVisitor"/>, so a single <see cref="SqlExpressionConvertVisitor.Convert"/> traversal
		/// optimizes, lowers, and runs the CompareNulls null-guard reduce on each node as it passes over it — collapsing what
		/// used to be four of the old five passes (optimize, convert, reduce, convert). Optimization over the
		/// <b>un-lowered</b> AST — the rules that only match abstract nodes — already happens during query build in
		/// <c>SelectQueryOptimizerVisitor</c>, so re-running it here was redundant.
		/// <para>
		/// A single optimize sweep follows the convert <b>only when it is needed</b>. The null-guard reduce (or a
		/// <c>col = col</c> fold) can turn a predicate into a constant <c>TRUE</c> at a leaf <i>after</i> the enclosing AND/OR
		/// search condition has already run its drop-constant pass — a bottom-up traversal cannot retroactively drop it — so a
		/// redundant <c>AND 1 = 1</c> can survive the convert. The convert raises <see cref="SqlExpressionOptimizerVisitor.
		/// FoldedPredicateToConstant"/> when it folds any predicate to a constant; only then does the collapse sweep run. A
		/// query that folds nothing stays strictly one-pass. The sweep carries no lowering and no reduce, so it is idempotent.
		/// </para>
		/// </summary>
		public T OptimizeAndConvertAll<T>(T element, NullabilityContext nullabilityContext)
			where T : class, IQueryElement
		{
			var result = (T)ConvertVisitor.Convert(this, nullabilityContext, element, visitQueries : true);

			if (ConvertVisitor.FoldedPredicateToConstant)
				result = (T)OptimizerVisitor.Optimize(EvaluationContext, nullabilityContext, null, DataOptions, MappingSchema, result, visitQueries : true);

			return result;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public T? Optimize<T>(T? element, NullabilityContext nullabilityContext, bool reducePredicates)
			where T : class, IQueryElement
		{
			if (element == null)
				return null;

			var newElement = OptimizerVisitor.Optimize(EvaluationContext, nullabilityContext, null, DataOptions, MappingSchema, element, false, reducePredicates);

			return (T)newElement;
		}
	}
}
