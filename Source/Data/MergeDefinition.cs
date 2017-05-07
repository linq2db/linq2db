using LinqToDB.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Data
{
	internal class MergeDefinition<TTarget, TSource> : IMerge<TTarget, TSource>, IMerge<TTarget>
	{
		private readonly Table<TTarget> _target;
		private readonly Expression<Func<TTarget, TSource, bool>> _matchPredicate;
		private readonly IEnumerable<TSource> _enumerableSource;
		private readonly IQueryable<TSource> _queryableSource;

		private readonly Operation[] _operations;

		public MergeDefinition(
			ITable<TTarget> target,
			IEnumerable<TSource> source,
			Expression<Func<TTarget, TSource, bool>> matchPredicate)
		{
			_target = (Table<TTarget>)target;
			_enumerableSource = source;
			_matchPredicate = matchPredicate;

			_operations = new Operation[0];
		}

		public MergeDefinition(
			ITable<TTarget> target,
			IQueryable<TSource> source,
			Expression<Func<TTarget, TSource, bool>> matchPredicate)
		{
			_target = (Table<TTarget>)target;
			_queryableSource = source;
			_matchPredicate = matchPredicate;

			_operations = new Operation[0];
		}

		private MergeDefinition(
			Table<TTarget> target,
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

		public MergeDefinition<TTarget, TSource> AddOperation(Operation operation)
		{
			return new MergeDefinition<TTarget, TSource>(
				_target,
				_enumerableSource,
				_queryableSource,
				_matchPredicate,
				_operations.Concat(new[] { operation }).ToArray());
		}

		private enum OperationType
		{
			Insert,
			Update,
			Delete,
			UpdateBySource,
			DeleteBySource
		}

		internal class Operation
		{
			private readonly OperationType _type;

			private readonly Expression<Func<TSource, bool>> _notMatchedPredicate;
			private readonly Expression<Func<TTarget, TSource, bool>> _matchedPredicate;
			private readonly Expression<Func<TTarget, bool>> _bySourcePredicate;

			private readonly Expression<Func<TSource, TTarget>> _create;
			private readonly Expression<Func<TTarget, TSource, TTarget>> _update;
			private readonly Expression<Func<TTarget, TTarget>> _updateBySource;

			private Operation(
				OperationType type,
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

			public static Operation Insert(
				Expression<Func<TSource, bool>> predicate,
				Expression<Func<TSource, TTarget>> create)
			{
				return new Operation(OperationType.Insert, predicate, null, null, create, null, null);
			}

			public static Operation Update(
				Expression<Func<TTarget, TSource, bool>> predicate,
				Expression<Func<TTarget, TSource, TTarget>> udpate)
			{
				return new Operation(OperationType.Update, null, predicate, null, null, udpate, null);
			}

			public static Operation Delete(
				Expression<Func<TTarget, TSource, bool>> predicate)
			{
				return new Operation(OperationType.Delete, null, predicate, null, null, null, null);
			}

			public static Operation UpdateBySource(
				Expression<Func<TTarget, bool>> predicate,
				Expression<Func<TTarget, TTarget>> udpate)
			{
				return new Operation(OperationType.UpdateBySource, null, null, predicate, null, null, udpate);
			}

			public static Operation DeleteBySource(
				Expression<Func<TTarget, bool>> predicate)
			{
				return new Operation(OperationType.DeleteBySource, null, null, predicate, null, null, null);
			}
		}
	}
}
