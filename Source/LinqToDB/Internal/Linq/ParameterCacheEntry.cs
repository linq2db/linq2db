using System.Linq.Expressions;

namespace LinqToDB.Internal.Linq
{
	sealed class ParameterCacheEntry
	{
		public ParameterCacheEntry(
			int           parameterId,
			string        parameterName,
			in DbDataType dbDataType,
			Expression    clientValueGetter,
			Expression?   clientToProviderConverter,
			Expression?   itemAccessor,
			Expression?   dbDataTypeAccessor)
		{
			ParameterId               = parameterId;
			DbDataTypeAccessor        = dbDataTypeAccessor;
			ParameterName             = parameterName;
			DbDataType                = dbDataType;
			ClientToProviderConverter = clientToProviderConverter;
			ClientValueGetter         = clientValueGetter;
			ItemAccessor              = itemAccessor;
		}

		public readonly int        ParameterId;
		public readonly string     ParameterName;
		public readonly DbDataType DbDataType;

		/// <summary>
		/// Body of Expression&lt;Func&lt;IQueryExpressions, IDataContext?, object?[]?, object?&gt;&gt;
		/// </summary>
		public readonly Expression ClientValueGetter;
		/// <summary>
		/// Body of Expression&lt;Func&lt;object?, object?&gt;&gt;
		/// </summary>
		public readonly Expression? ClientToProviderConverter;
		/// <summary>
		/// Body of Expression&lt;Func&lt;object?, object?&gt;&gt;
		/// </summary>
		public readonly Expression? ItemAccessor;
		/// <summary>
		/// Body of Expression&lt;Func&lt;object?, DbDataType&gt;&gt;
		/// </summary>
		public readonly Expression? DbDataTypeAccessor;

		public bool    IsEvaluated    { get; private set; }
		public object? EvaluatedValue { get; private set; }

		public void SetEvaluatedValue(object? value)
		{
			EvaluatedValue = value;
			IsEvaluated = true;
		}
	}
}
