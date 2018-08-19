using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB
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
		: IMergeableUsing<TTarget>,
			IMergeableOn<TTarget, TSource>,
			IMergeable<TTarget, TSource>
	{
		public MergeDefinition(ITable<TTarget> target)
		{
			Target = target;
		}

		public MergeDefinition(ITable<TTarget> target, string hint)
		{
			Target = target;
			Hint   = hint;
		}

		public MergeDefinition(ITable<TTarget> target, IQueryable<TSource> source)
		{
			Target          = target;
			QueryableSource = source;
		}

		public MergeDefinition(ITable<TTarget> target, IQueryable<TSource> source, string hint)
		{
			Target          = target;
			QueryableSource = source;
			Hint            = hint;
		}

		private MergeDefinition(
			ITable<TTarget>                          target,
			string                                   hint,
			IEnumerable<TSource>                     enumerableSource,
			IQueryable<TSource>                      queryableSource,
			Expression<Func<TTarget, TSource, bool>> matchPredicate,
			Expression                               targetKey,
			Expression                               sourceKey,
			Type                                     keyType,
			Operation[]                              operations)
		{
			Target           = target;
			Hint             = hint;
			EnumerableSource = enumerableSource;
			QueryableSource  = queryableSource;
			MatchPredicate   = matchPredicate;
			TargetKey        = targetKey;
			SourceKey        = sourceKey;
			KeyType          = keyType;

			Operations       = operations ?? new Operation[0];
		}

		public IEnumerable<TSource>                     EnumerableSource { get; }
		public Expression<Func<TTarget, TSource, bool>> MatchPredicate   { get; }
		public Operation[]                              Operations       { get; }
		public IQueryable<TSource>                      QueryableSource  { get; }
		public string                                   Hint             { get; }
		public ITable<TTarget>                          Target           { get; }
		public Expression                               TargetKey        { get; }
		public Expression                               SourceKey        { get; }
		public Type                                     KeyType          { get; }

		public MergeDefinition<TTarget, TNewSource> AddSource<TNewSource>(IQueryable<TNewSource> source)
			where TNewSource : class
		{
			return new MergeDefinition<TTarget, TNewSource>(Target, Hint, null, source, null, null, null, null, null);
		}

		public MergeDefinition<TTarget, TNewSource> AddSource<TNewSource>(IEnumerable<TNewSource> source)
			where TNewSource : class
		{
			return new MergeDefinition<TTarget, TNewSource>(Target, Hint, source, null, null, null, null, null, null);
		}

		public MergeDefinition<TTarget, TSource> AddOperation(Operation operation)
		{
			return new MergeDefinition<TTarget, TSource>(
				Target,
				Hint,
				EnumerableSource,
				QueryableSource,
				MatchPredicate,
				TargetKey,
				SourceKey,
				KeyType,
				(Operations ?? new Operation[0]).Concat(new[] { operation }).ToArray());
		}

		public MergeDefinition<TTarget, TSource> AddOnPredicate(Expression<Func<TTarget, TSource, bool>> matchPredicate)
		{
			return new MergeDefinition<TTarget, TSource>(
				Target,
				Hint,
				EnumerableSource,
				QueryableSource,
				matchPredicate,
				null,
				null,
				null,
				Operations);
		}

		public MergeDefinition<TTarget, TSource> AddOnKey<TKey>(
			Expression<Func<TTarget, TKey>> targetKey,
			Expression<Func<TSource, TKey>> sourceKey)
		{
			return new MergeDefinition<TTarget, TSource>(
				Target,
				Hint,
				EnumerableSource,
				QueryableSource,
				null,
				targetKey,
				sourceKey,
				typeof(TKey),
				Operations);
		}

		public class Operation
		{
			private Operation(
				MergeOperationType                          type,
				Expression<Func<TSource, bool>>             notMatchedPredicate,
				Expression<Func<TTarget, TSource, bool>>    matchedPredicate1,
				Expression<Func<TTarget, TSource, bool>>    matchedPredicate2,
				Expression<Func<TTarget, bool>>             bySourcePredicate,
				Expression<Func<TSource, TTarget>>          create,
				Expression<Func<TTarget, TSource, TTarget>> update,
				Expression<Func<TTarget, TTarget>>          updateBySource)
			{
				Type                     = type;

				NotMatchedPredicate      = notMatchedPredicate;
				MatchedPredicate         = matchedPredicate1;
				MatchedPredicate2        = matchedPredicate2;
				BySourcePredicate        = bySourcePredicate;

				CreateExpression         = create;
				UpdateExpression         = update;
				UpdateBySourceExpression = updateBySource;
			}


			public bool HasCondition
			{
				get
				{
					switch (Type)
					{
						case MergeOperationType.Delete:
						case MergeOperationType.Update:
							return MatchedPredicate != null;
						case MergeOperationType.UpdateWithDelete:
							return MatchedPredicate != null || MatchedPredicate2 != null;
						case MergeOperationType.Insert:
							return NotMatchedPredicate != null;
						case MergeOperationType.DeleteBySource:
						case MergeOperationType.UpdateBySource:
							return BySourcePredicate != null;
					}

					throw new InvalidOperationException();
				}
			}

			public Expression<Func<TTarget, bool>>             BySourcePredicate        { get; }
			public Expression<Func<TSource, TTarget>>          CreateExpression         { get; }
			public Expression<Func<TTarget, TSource, bool>>    MatchedPredicate         { get; }
			public Expression<Func<TTarget, TSource, bool>>    MatchedPredicate2        { get; }
			public Expression<Func<TSource, bool>>             NotMatchedPredicate      { get; }
			public MergeOperationType                          Type                     { get; }
			public Expression<Func<TTarget, TTarget>>          UpdateBySourceExpression { get; }
			public Expression<Func<TTarget, TSource, TTarget>> UpdateExpression         { get; private set; }

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
				Expression<Func<TSource, bool>>    predicate,
				Expression<Func<TSource, TTarget>> create)
			{
				return new Operation(MergeOperationType.Insert, predicate, null, null, null, create, null, null);
			}

			public static Operation Update(
				Expression<Func<TTarget, TSource, bool>>    predicate,
				Expression<Func<TTarget, TSource, TTarget>> udpate)
			{
				return new Operation(MergeOperationType.Update, null, predicate, null, null, null, udpate, null);
			}

			public static Operation UpdateWithDelete(
				Expression<Func<TTarget, TSource, bool>>    updatePredicate,
				Expression<Func<TTarget, TSource, TTarget>> udpate,
				Expression<Func<TTarget, TSource, bool>>    deletePredicate)
			{
				return new Operation(MergeOperationType.UpdateWithDelete, null, updatePredicate, deletePredicate, null, null, udpate, null);
			}

			public static Operation UpdateBySource(
				Expression<Func<TTarget, bool>>    predicate,
				Expression<Func<TTarget, TTarget>> udpate)
			{
				return new Operation(MergeOperationType.UpdateBySource, null, null, null, predicate, null, null, udpate);
			}
		}
	}
}
