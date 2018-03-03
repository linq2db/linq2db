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
		UpdateWithDelete,
		UpdateBySource,
		DeleteBySource
	}

	public class MergeDefinition<TTarget, TSource>
		:	IMergeableUsing<TTarget>,
			IMergeableOn<TTarget, TSource>,
			IMergeable<TTarget, TSource>
	{
		private readonly IEnumerable<TSource> _enumerableSource;

		private readonly Expression<Func<TTarget, TSource, bool>> _matchPredicate;

		private readonly Expression _targetKey;
		private readonly Expression _sourceKey;
		private readonly Type _keyType;

		private readonly Operation[] _operations;

		private readonly IQueryable<TSource> _queryableSource;

		private readonly ITable<TTarget> _target;

		public MergeDefinition(ITable<TTarget> target)
		{
			_target = target;
		}

		public MergeDefinition(ITable<TTarget> target, IQueryable<TSource> source)
		{
			_target = target;
			_queryableSource = source;
		}

		private MergeDefinition(
			ITable<TTarget> target,
			IEnumerable<TSource> enumerableSource,
			IQueryable<TSource> queryableSource,
			Expression<Func<TTarget, TSource, bool>> matchPredicate,
			Expression targetKey,
			Expression sourceKey,
			Type keyType,
			Operation[] operations)
		{
			_target = target;
			_enumerableSource = enumerableSource;
			_queryableSource = queryableSource;
			_matchPredicate = matchPredicate;
			_targetKey = targetKey;
			_sourceKey = sourceKey;
			_keyType = keyType;

			_operations = operations ?? new Operation[0];
		}

		public IEnumerable<TSource> EnumerableSource
		{
			get
			{
				return _enumerableSource;
			}
		}

		public Expression<Func<TTarget, TSource, bool>> MatchPredicate
		{
			get
			{
				return _matchPredicate;
			}
		}

		public Operation[] Operations
		{
			get
			{
				return _operations;
			}
		}

		public IQueryable<TSource> QueryableSource
		{
			get
			{
				return _queryableSource;
			}
		}

		public ITable<TTarget> Target
		{
			get
			{
				return _target;
			}
		}

		public Expression TargetKey
		{
			get
			{
				return _targetKey;
			}
		}

		public Expression SourceKey
		{
			get
			{
				return _sourceKey;
			}
		}

		public Type KeyType
		{
			get
			{
				return _keyType;
			}
		}

		public MergeDefinition<TTarget, TNewSource> AddSource<TNewSource>(IQueryable<TNewSource> source)
			where TNewSource : class
		{
			return new MergeDefinition<TTarget, TNewSource>(_target, null, source, null, null, null, null, null);
		}

		public MergeDefinition<TTarget, TNewSource> AddSource<TNewSource>(IEnumerable<TNewSource> source)
			where TNewSource : class
		{
			return new MergeDefinition<TTarget, TNewSource>(_target, source, null, null, null, null, null, null);
		}

		public MergeDefinition<TTarget, TSource> AddOperation(Operation operation)
		{
			return new MergeDefinition<TTarget, TSource>(
				_target,
				_enumerableSource,
				_queryableSource,
				_matchPredicate,
				TargetKey,
				SourceKey,
				KeyType,
				(_operations ?? new Operation[0]).Concat(new[] { operation }).ToArray());
		}

		public MergeDefinition<TTarget, TSource> AddOnPredicate(Expression<Func<TTarget, TSource, bool>> matchPredicate)
		{
			return new MergeDefinition<TTarget, TSource>(
				_target,
				_enumerableSource,
				_queryableSource,
				matchPredicate,
				null,
				null,
				null,
				_operations);
		}

		public MergeDefinition<TTarget, TSource> AddOnKey<TKey>(
			Expression<Func<TTarget, TKey>> targetKey,
			Expression<Func<TSource, TKey>> sourceKey)
		{
			return new MergeDefinition<TTarget, TSource>(
				_target,
				_enumerableSource,
				_queryableSource,
				null,
				targetKey,
				sourceKey,
				typeof(TKey),
				_operations);
		}

		public class Operation
		{
			private readonly Expression<Func<TTarget, bool>> _bySourcePredicate;

			private readonly Expression<Func<TSource, TTarget>> _create;

			private readonly Expression<Func<TTarget, TSource, bool>> _matchedPredicate;

			private readonly Expression<Func<TTarget, TSource, bool>> _matchedPredicate2;

			private readonly Expression<Func<TSource, bool>> _notMatchedPredicate;

			private readonly MergeOperationType _type;

			private readonly Expression<Func<TTarget, TSource, TTarget>> _update;

			private readonly Expression<Func<TTarget, TTarget>> _updateBySource;

			private Operation(
				MergeOperationType type,
				Expression<Func<TSource, bool>> notMatchedPredicate,
				Expression<Func<TTarget, TSource, bool>> matchedPredicate1,
				Expression<Func<TTarget, TSource, bool>> matchedPredicate2,
				Expression<Func<TTarget, bool>> bySourcePredicate,
				Expression<Func<TSource, TTarget>> create,
				Expression<Func<TTarget, TSource, TTarget>> update,
				Expression<Func<TTarget, TTarget>> updateBySource)
			{
				_type = type;

				_notMatchedPredicate = notMatchedPredicate;
				_matchedPredicate = matchedPredicate1;
				_matchedPredicate2 = matchedPredicate2;
				_bySourcePredicate = bySourcePredicate;

				_create = create;
				_update = update;
				_updateBySource = updateBySource;
			}

			public Expression<Func<TTarget, bool>> BySourcePredicate
			{
				get
				{
					return _bySourcePredicate;
				}
			}

			public Expression<Func<TSource, TTarget>> CreateExpression
			{
				get
				{
					return _create;
				}
			}

			public bool HasCondition
			{
				get
				{
					switch (_type)
					{
						case MergeOperationType.Delete:
						case MergeOperationType.Update:
							return _matchedPredicate != null;
						case MergeOperationType.UpdateWithDelete:
							return _matchedPredicate != null || _matchedPredicate2 != null;
						case MergeOperationType.Insert:
							return _notMatchedPredicate != null;
						case MergeOperationType.DeleteBySource:
						case MergeOperationType.UpdateBySource:
							return _bySourcePredicate != null;
					}

					throw new InvalidOperationException();
				}
			}

			public Expression<Func<TTarget, TSource, bool>> MatchedPredicate
			{
				get
				{
					return _matchedPredicate;
				}
			}

			public Expression<Func<TTarget, TSource, bool>> MatchedPredicate2
			{
				get
				{
					return _matchedPredicate2;
				}
			}

			public Expression<Func<TSource, bool>> NotMatchedPredicate
			{
				get
				{
					return _notMatchedPredicate;
				}
			}

			public MergeOperationType Type
			{
				get
				{
					return _type;
				}
			}

			public Expression<Func<TTarget, TTarget>> UpdateBySourceExpression
			{
				get
				{
					return _updateBySource;
				}
			}

			public Expression<Func<TTarget, TSource, TTarget>> UpdateExpression
			{
				get
				{
					return _update;
				}
			}

			public static Operation Delete(
				Expression<Func<TTarget, TSource, bool>> predicate)
			{
				return new Operation(MergeOperationType.Delete, null, predicate, null, null, null, null, null);
			}

			public static Operation DeleteBySource(
				Expression<Func<TTarget, bool>> predicate)
			{
				return new Operation(MergeOperationType.DeleteBySource, null, null, null, predicate, null, null, null);
			}

			public static Operation Insert(
				Expression<Func<TSource, bool>> predicate,
				Expression<Func<TSource, TTarget>> create)
			{
				return new Operation(MergeOperationType.Insert, predicate, null, null, null, create, null, null);
			}

			public static Operation Update(
				Expression<Func<TTarget, TSource, bool>> predicate,
				Expression<Func<TTarget, TSource, TTarget>> udpate)
			{
				return new Operation(MergeOperationType.Update, null, predicate, null, null, null, udpate, null);
			}

			public static Operation UpdateWithDelete(
				Expression<Func<TTarget, TSource, bool>> updatePredicate,
				Expression<Func<TTarget, TSource, TTarget>> udpate,
				Expression<Func<TTarget, TSource, bool>> deletePredicate)
			{
				return new Operation(MergeOperationType.UpdateWithDelete, null, updatePredicate, deletePredicate, null, null, udpate, null);
			}

			public static Operation UpdateBySource(
				Expression<Func<TTarget, bool>> predicate,
				Expression<Func<TTarget, TTarget>> udpate)
			{
				return new Operation(MergeOperationType.UpdateBySource, null, null, null, predicate, null, null, udpate);
			}
		}
	}
}
