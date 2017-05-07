using LinqToDB.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Data
{
	public enum MergeOperationType
	{
		Insert,
		Update,
		Delete,
		UpdateBySource,
		DeleteBySource
	}

	public class MergeDefinition<TTarget, TSource> : IMerge<TTarget, TSource>, IMerge<TTarget>
	{
		private readonly IEnumerable<TSource> _enumerableSource;

		private readonly Expression<Func<TTarget, TSource, bool>> _matchPredicate;

		private readonly Operation[] _operations;

		private readonly IQueryable<TSource> _queryableSource;

		private readonly ITable<TTarget> _target;

		public MergeDefinition(
			ITable<TTarget> target,
			IEnumerable<TSource> source,
			Expression<Func<TTarget, TSource, bool>> matchPredicate)
		{
			_target = target;
			_enumerableSource = source;
			_matchPredicate = matchPredicate;

			_operations = new Operation[0];
		}

		public MergeDefinition(
			ITable<TTarget> target,
			IQueryable<TSource> source,
			Expression<Func<TTarget, TSource, bool>> matchPredicate)
		{
			_target = target;
			_queryableSource = source;
			_matchPredicate = matchPredicate;

			_operations = new Operation[0];
		}

		private MergeDefinition(
			ITable<TTarget> target,
			IEnumerable<TSource> enumerableSource,
			IQueryable<TSource> queryableSource,
			Expression<Func<TTarget, TSource, bool>> matchPredicate,
			Operation[] operations)
		{
			_target = target;
			_enumerableSource = enumerableSource;
			_queryableSource = queryableSource;
			_matchPredicate = matchPredicate;

			_operations = operations;
		}

		public Operation[] Operations => _operations;

		public ITable<TTarget> Target => _target;

		public MergeDefinition<TTarget, TSource> AddOperation(Operation operation)
		{
			return new MergeDefinition<TTarget, TSource>(
				_target,
				_enumerableSource,
				_queryableSource,
				_matchPredicate,
				_operations.Concat(new[] { operation }).ToArray());
		}

		public class Operation
		{
			private readonly Expression<Func<TTarget, bool>> _bySourcePredicate;

			private readonly Expression<Func<TSource, TTarget>> _create;

			private readonly Expression<Func<TTarget, TSource, bool>> _matchedPredicate;

			private readonly Expression<Func<TSource, bool>> _notMatchedPredicate;

			private readonly MergeOperationType _type;

			private readonly Expression<Func<TTarget, TSource, TTarget>> _update;

			private readonly Expression<Func<TTarget, TTarget>> _updateBySource;

			private Operation(
				MergeOperationType type,
				Expression<Func<TSource, bool>> notMatchedPredicate,
				Expression<Func<TTarget, TSource, bool>> matchedPredicate,
				Expression<Func<TTarget, bool>> bySourcePredicate,
				Expression<Func<TSource, TTarget>> create,
				Expression<Func<TTarget, TSource, TTarget>> update,
				Expression<Func<TTarget, TTarget>> updateBySource)
			{
				_type = type;

				_notMatchedPredicate = notMatchedPredicate;
				_matchedPredicate = matchedPredicate;
				_bySourcePredicate = bySourcePredicate;

				_create = create;
				_update = update;
				_updateBySource = updateBySource;
			}

			public Expression<Func<TTarget, bool>> BySourcePredicate => _bySourcePredicate;

			public Expression<Func<TSource, TTarget>> CreateExpression => _create;

			public bool HasCondition
			{
				get
				{
					switch (_type)
					{
						case MergeOperationType.Delete:
						case MergeOperationType.Update:
							return _matchedPredicate != null;
						case MergeOperationType.Insert:
							return _notMatchedPredicate != null;
						case MergeOperationType.DeleteBySource:
						case MergeOperationType.UpdateBySource:
							return _bySourcePredicate != null;
					}

					throw new InvalidOperationException();
				}
			}

			public Expression<Func<TTarget, TSource, bool>> MatchedPredicate => _matchedPredicate;

			public Expression<Func<TSource, bool>> NotMatchedPredicate => _notMatchedPredicate;

			public MergeOperationType Type => _type;

			public Expression<Func<TTarget, TTarget>> UpdateBySourceExpression => _updateBySource;

			public Expression<Func<TTarget, TSource, TTarget>> UpdateExpression => _update;

			public static Operation Delete(
				Expression<Func<TTarget, TSource, bool>> predicate)
			{
				return new Operation(MergeOperationType.Delete, null, predicate, null, null, null, null);
			}

			public static Operation DeleteBySource(
				Expression<Func<TTarget, bool>> predicate)
			{
				return new Operation(MergeOperationType.DeleteBySource, null, null, predicate, null, null, null);
			}

			public static Operation Insert(
				Expression<Func<TSource, bool>> predicate,
				Expression<Func<TSource, TTarget>> create)
			{
				return new Operation(MergeOperationType.Insert, predicate, null, null, create, null, null);
			}

			public static Operation Update(
				Expression<Func<TTarget, TSource, bool>> predicate,
				Expression<Func<TTarget, TSource, TTarget>> udpate)
			{
				return new Operation(MergeOperationType.Update, null, predicate, null, null, udpate, null);
			}

			public static Operation UpdateBySource(
				Expression<Func<TTarget, bool>> predicate,
				Expression<Func<TTarget, TTarget>> udpate)
			{
				return new Operation(MergeOperationType.UpdateBySource, null, null, predicate, null, null, udpate);
			}
		}
	}
}
