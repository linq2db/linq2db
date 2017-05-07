using LinqToDB.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider
{
	/// <summary>
	/// Basic merge builder's validation options set to validate merge operation on SQL:2008 level without specific
	/// database limitations or extensions.
	/// </summary>
	public class BasicMergeBuilder
	{
		private static MergeOperationType[] _matchedTypes = new[]
		{
			MergeOperationType.Delete,
			MergeOperationType.Update
		};

		private static MergeOperationType[] _notMatchedBySourceTypes = new[]
{
			MergeOperationType.DeleteBySource,
			MergeOperationType.UpdateBySource
		};

		private static MergeOperationType[] _notMatchedTypes = new[]
		{
			MergeOperationType.Insert
		};

		private readonly StringBuilder _command = new StringBuilder();

		private readonly IList<DataParameter> _parameters = new List<DataParameter>();

		public DataParameter[] Parameters => _parameters.ToArray();

		protected virtual bool BySourceOperationsSupported => false;

		protected virtual bool DeleteOperationSupported => true;

		protected virtual int MaxOperationsCount => 0;

		protected virtual bool OperationPerdicateSupported => true;

		protected virtual bool SameTypeOperationsAllowed => true;

		public virtual string BuildCommand<TTarget, TSource>(MergeDefinition<TTarget, TSource> merge)
			where TTarget : class
			where TSource : class
		{
			// TODO
			throw new NotImplementedException();

			//return _command.ToString();
		}

		public virtual void Validate<TTarget, TSource>(MergeDefinition<TTarget, TSource> merge, string providerName)
			where TTarget : class
			where TSource : class
		{
			// validate operations limit
			if (MaxOperationsCount > 0 && merge.Operations.Length > MaxOperationsCount)
				throw new LinqToDBException($"Merge cannot contain more than {MaxOperationsCount} operations for {providerName} provider.");

			// - validate that specified operations supported by provider
			// - validate that operations don't have conditions if provider doesn't support them
			foreach (var operation in merge.Operations)
			{
				switch (operation.Type)
				{
					case MergeOperationType.Delete:
						if (!DeleteOperationSupported)
							throw new LinqToDBException($"Merge Delete operation is not supported by {providerName} provider.");
						if (!OperationPerdicateSupported && operation.MatchedPredicate != null)
							throw new LinqToDBException($"Merge operation conditions are not supported by {providerName} provider.");
						break;
					case MergeOperationType.Insert:
						if (!OperationPerdicateSupported && operation.NotMatchedPredicate != null)
							throw new LinqToDBException($"Merge operation conditions are not supported by {providerName} provider.");
						break;
					case MergeOperationType.Update:
						if (!OperationPerdicateSupported && operation.MatchedPredicate != null)
							throw new LinqToDBException($"Merge operation conditions are not supported by {providerName} provider.");
						break;
					case MergeOperationType.DeleteBySource:
						if (!BySourceOperationsSupported)
							throw new LinqToDBException($"Merge Delete By Source operation is not supported by {providerName} provider.");
						if (!OperationPerdicateSupported && operation.BySourcePredicate != null)
							throw new LinqToDBException($"Merge operation conditions are not supported by {providerName} provider.");
						break;
					case MergeOperationType.UpdateBySource:
						if (!BySourceOperationsSupported)
							throw new LinqToDBException($"Merge Update By Source operation is not supported by {providerName} provider.");
						if (!OperationPerdicateSupported && operation.BySourcePredicate != null)
							throw new LinqToDBException($"Merge operation conditions are not supported by {providerName} provider.");
						break;
				}
			}

			// - operations without conditions not placed before operations with conditions in each match group
			// - there is no multiple operations without condition in each match group
			ValidateGroupConditions(merge, _matchedTypes);
			ValidateGroupConditions(merge, _notMatchedTypes);
			ValidateGroupConditions(merge, _notMatchedBySourceTypes);

			// validate that there is no duplicate operations (by type) if provider doesn't support them
			if (!SameTypeOperationsAllowed && merge.Operations.GroupBy(_ => _.Type).Any(_ => _.Count() > 1))
				throw new LinqToDBException($"Multiple operations of the same type are not supported by {providerName} provider.");
		}

		private static void ValidateGroupConditions<TTarget, TSource>(MergeDefinition<TTarget, TSource> merge, MergeOperationType[] groupTypes)
			where TTarget : class
			where TSource : class
		{
			var hasUnconditional = false;
			foreach (var operation in merge.Operations.Where(_ => groupTypes.Contains(_.Type)))
			{
				if (hasUnconditional && operation.HasCondition)
					throw new LinqToDBException("Unconditional Merge operation cannot be followed by operation with condition within the same match group.");

				if (hasUnconditional && !operation.HasCondition)
					throw new LinqToDBException("Multiple unconditional Merge operations not allowed within the same match group.");

				if (!hasUnconditional && !operation.HasCondition)
					hasUnconditional = true;
			}
		}
	}
}
