using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser
{
	public class OptimizationFlags
	{
		public bool CountFilterSupported { get; set; }
	}

	public class ModelOptimizer
	{
		public OptimizationFlags Flags { get; }

		public ModelOptimizer([NotNull] OptimizationFlags flags)
		{
			Flags = flags ?? throw new ArgumentNullException(nameof(flags));
		}

		public Sequence OptimizeModel(Sequence sequence)
		{
			sequence = CompactSequence(sequence);
			var result = sequence.TransformSequence(InternalOptimizeSequence);
			return result;
		}

		private Sequence InternalOptimizeSequence(Sequence sequence)
		{
			sequence = OptimizeResults(sequence);
			return sequence;
		}

		private static Sequence CompactSequence(Sequence sequence)
		{
			for (var index = 0; index < sequence.Clauses.Count; index++)
			{
				var clause = sequence.Clauses[index];
				if (clause is Sequence sq)
				{
					sq = CompactSequence(sq);
					if (sq.Clauses.Count == 1)
						sequence.Clauses[index] = sq.Clauses[0];
					else
						sequence.Clauses[index] = sq;
				}
			}

			return sequence;
		}


		private static Sequence CombineWhereClauses(Sequence sequence)
		{
			WhereClause currentWhere = null;
			for (int i = 0; i < sequence.Clauses.Count; i++)
			{
				var clause = sequence.Clauses[i];
				if (!(clause is WhereClause where))
					currentWhere = null;
				else
				{
					if (currentWhere == null)
						currentWhere = where;
					else
					{
						currentWhere.SearchExpression =
							Expression.AndAlso(currentWhere.SearchExpression, where.SearchExpression);
						sequence.Clauses.RemoveAt(i);
						--i;
					}
				}
			}

			return sequence;
		}

		private Sequence OptimizeResults(Sequence sequence)
		{
			sequence = CombineWhereClauses(sequence);

			var lastOne = sequence.LastClause<IResultClause>();
			switch (lastOne)
			{
				case CountClause count:
					{
						OptimizeCount(sequence, count);
						break;
					}
			}

			return sequence;
		}

		private void OptimizeCount(Sequence sequence, CountClause count)
		{
			if (Flags.CountFilterSupported)
			{
				var whereClause = sequence.RemovePrevious<WhereClause>(count);
				if (whereClause != null)
				{
					sequence.ReplaceClause(count,
						new CountClause(count.FilterExpression == null
							? whereClause.SearchExpression
							: Expression.AndAlso(whereClause.SearchExpression, count.FilterExpression), count.ResultType));
				}
			}
			else
			{
				if (count.FilterExpression != null)
				{
					// move filter to Where
					sequence.InserBefore(count, new WhereClause(count.FilterExpression));
					sequence.ReplaceClause(count, new CountClause(null, count.ResultType));

					CombineWhereClauses(sequence);
				}
			}
		}
	}
}
