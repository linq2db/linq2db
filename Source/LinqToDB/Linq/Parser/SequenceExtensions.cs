using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser
{
	public static class SequenceExtensions
	{
		public static int EnsureIndex(this Sequence sequence, BaseClause clause)
		{
			var idx = sequence.Clauses.IndexOf(clause);
			if (idx < 0)
				throw new InvalidOperationException("Clause do not belong to Sequence");
			return idx;
		}

		public static T LastClause<T>(this Sequence sequence)
		{
			for (int i = sequence.Clauses.Count - 1; i >= 0; i--)
			{
				if (sequence.Clauses[i] is T c)
					return c;
			}
			return default;
		}

		public static IEnumerable<T> PriorContinuous<T>(this Sequence sequence, BaseClause clause)
		{
			var idx = sequence.EnsureIndex(clause);

			if (idx == 0)
				yield break;

			for (int i = idx - 1; i >= 0; i--)
			{
				if (sequence.Clauses[i] is T c)
					yield return c;
				else
					break;
			}
		}

		public static T[] RemoveContinuous<T>(this Sequence sequence, BaseClause clause)
		{
			var result = sequence.PriorContinuous<T>(clause).ToArray();
			if (result.Length > 0)
			{
				var idx = sequence.Clauses.IndexOf(sequence);
				for (int i = 0; i < result.Length; i++)
				{
					sequence.Clauses.RemoveAt(idx - 1 - i);
				}
			}

			return result;
		}

		public static T RemovePrevious<T>(this Sequence sequence, BaseClause clause)
		{
			var idx = sequence.EnsureIndex(clause);

			if (idx > 0 && sequence.Clauses[idx - 1] is T c)
			{
				sequence.Clauses.RemoveAt(idx - 1);
				return c;
			}

			return default;
		}

		public static int InserBefore(this Sequence sequence, BaseClause clause, BaseClause newClause)
		{
			var idx = sequence.EnsureIndex(clause);
			sequence.Clauses.Insert(idx, newClause);
			return idx;
		}

		public static void ReplaceClause(this Sequence sequence, BaseClause clause, BaseClause newClause)
		{
			var idx = sequence.EnsureIndex(clause);
			sequence.Clauses[idx] = newClause;
		}

		public static Sequence TransformSequence(this Sequence sequence, Func<Sequence, Sequence> func)
		{
			for (int i = 0; i < sequence.Clauses.Count; i++)
			{
				if (sequence.Clauses[i] is Sequence seq)
					sequence.Clauses[i] = seq.TransformSequence(func);
				else
					sequence.Clauses[i].TransformExpression(e =>
					{
						if (e is SubQueryExpression2 subQuery)
						{
							var newSequence = subQuery.Sequence.TransformSequence(func);
							if (newSequence != subQuery.Sequence)
								return new SubQueryExpression2(sequence, subQuery.ItemType);
						}

						return e;
					});
					
			}
			return func(sequence);
		}
	}
}
