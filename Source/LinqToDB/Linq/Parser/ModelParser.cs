using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using LinqToDB.Linq.Parser.Builders;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Parser
{
	public abstract class BaseClause
	{

		protected bool VisitListParentFirst<T>(IList<T> list, Func<BaseClause, bool> func)
			where T: BaseClause
		{
			foreach (var item in list)
			{
				if (!item.VisitParentFirst(func))
					return false;
			}

			return true;
		}

		protected List<T> VisitList<T>(List<T> list, Func<BaseClause, BaseClause> func)
			where T: BaseClause
		{
			List<T> current = null;
			for (int i = 0; i < list.Count; i++)
			{
				var item = list[i];
				if (item != null)
				{
					var newItem = (T)item.Visit(func);
					if (newItem != item)
					{
						if (current == null)
							current = new List<T>(list.Take(i));
						item = newItem;
					}
				}

				current?.Add(item);
			}

			return current ?? list;
		}

		public abstract BaseClause Visit(Func<BaseClause, BaseClause> func);
		public abstract bool VisitParentFirst(Func<BaseClause, bool> func);
	}

	public class SelectClause : BaseClause
	{
		public Expression Selector { get; set; }

		public SelectClause(Expression selector)
		{
			Selector = selector;
		}

		public override BaseClause Visit(Func<BaseClause, BaseClause> func)
		{
			return func(this);
		}

		public override bool VisitParentFirst(Func<BaseClause, bool> func)
		{
			return func(this);
		}
	}

	public class Sequence : BaseClause
	{
		public List<BaseClause> Clauses { get; set; }

		public Sequence()
		{
			Clauses = new List<BaseClause>();
		}

		private Sequence([NotNull] List<BaseClause> clauses)
		{
			Clauses = clauses ?? throw new ArgumentNullException(nameof(clauses));
		}

		public void AddClause(BaseClause clause)
		{
			Clauses.Add(clause);
		}

		public IQuerySource GetQuerySource()
		{
			if (this is IQuerySource querySource)
				return querySource;

			for (var i = Clauses.Count - 1; i >= 0; i--)
			{
				var clause = Clauses[i];
				if (clause is IQuerySource qs)
					return qs;
			}

			return null;
		}

		public override BaseClause Visit(Func<BaseClause, BaseClause> func)
		{
			var clauses = VisitList(Clauses, func);
			if (clauses != Clauses) 
				return new Sequence(clauses);
			return func(this);
		}

		public override bool VisitParentFirst(Func<BaseClause, bool> func)
		{
			return func(this) &&
			       VisitListParentFirst(Clauses, func);
		}
	}

	public class ModelParser
	{
		private static readonly BaseBuilder[] _builders = 
		{
			new WhereMethodBuilder(),
			new ArrayBuilder(), 
			new SelectBuilder(),
			new SelectManyBuilder(),
			new ConstantQueryBuilder(),
			new UnionBuilder(),
			new JoinBuilder(), 
			new TakeBuilder(), 
			new SkipBuilder(), 
		};

		private static readonly Dictionary<MethodInfo, MethodCallBuilder[]> _methodCallBuilders;
		private static readonly BaseBuilder[] _otherBuilders;

		static ModelParser()
		{
			_methodCallBuilders = _builders
				.OfType<MethodCallBuilder>()
				.SelectMany(b => b.SupportedMethods(), (b, mi) => new { b = b, mi })
				.ToLookup(p => p.mi, p => p.b)
				.ToDictionary(l => l.Key, l => l.ToArray());

			_otherBuilders = _builders
				.Where(b => !(b is MethodCallBuilder))
				.ToArray();
		}

		private Dictionary<ParameterExpression, QuerySourceReferenceExpression> _registeredSources = new Dictionary<ParameterExpression, QuerySourceReferenceExpression>();
		private readonly Dictionary<IQuerySource, QuerySourceReferenceExpression> _registeredSources2 = new Dictionary<IQuerySource, QuerySourceReferenceExpression>();

		public Sequence BuildSequence(ParseBuildInfo parseBuildInfo, Expression expression)
		{
			if (expression.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expression;

				if (_methodCallBuilders.TryGetValue(mc.Method.EnsureDefinition(), out var builders))
				{
					foreach (var builder in builders)
					{
						if (builder.CanBuild(expression))
						{
							return builder.BuildSequence(this, parseBuildInfo, expression);
						}
					}

				}
			}
			else
			{
				foreach (var builder in _otherBuilders)
				{
					if (builder.CanBuild(expression))
					{
						return builder.BuildSequence(this, parseBuildInfo, expression);
					}
				}

			}

			throw new NotImplementedException();
		}

		public Expression ConvertExpression(Expression expression)
		{
			expression = expression.Unwrap();
			//TODO
			return expression;
		}

		public QuerySourceReferenceExpression GetSourceReference(Sequence current)
		{
			var qs = current.GetQuerySource();
			if (qs == null)
				throw new Exception("Sequence does not contain source.");
			return GetSourceReference(qs);
		}

		public QuerySourceReferenceExpression GetSourceReference(IQuerySource querySource)
		{
			if (!_registeredSources2.TryGetValue(querySource, out var value))
				value = RegisterSource(querySource);
			return value;
		}

		public QuerySourceReferenceExpression RegisterSource(IQuerySource source, ParameterExpression parameter)
		{
			var referenceExpression = new QuerySourceReferenceExpression(source);
			_registeredSources.Add(parameter, referenceExpression);
			_registeredSources2.Add(source, referenceExpression);
			return referenceExpression;
		}

		public QuerySourceReferenceExpression RegisterSource(IQuerySource source)
		{
			var referenceExpression = new QuerySourceReferenceExpression(source);
			_registeredSources2.Add(source, referenceExpression);
			return referenceExpression;
		}

		public static void CompactSequence(Sequence sequence)
		{
			for (var index = 0; index < sequence.Clauses.Count; index++)
			{
				var clause = sequence.Clauses[index];
				if (clause is Sequence sq)
				{
					CompactSequence(sq);
					if (sq.Clauses.Count == 1)
						sequence.Clauses[index] = sq.Clauses[0];
				}
			}
		}

		public Sequence ParseModel(Expression expression)
		{
			var sequence = BuildSequence(new ParseBuildInfo(), expression);
			CompactSequence(sequence);

			return sequence;
		}

		public static bool IsSequence(Expression expression)
		{
			if (expression.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expression;

				if (_methodCallBuilders.TryGetValue(mc.Method.EnsureDefinition(), out var builders))
				{
					foreach (var builder in builders)
					{
						if (builder.CanBuild(expression))
						{
							return true;
						}
					}

				}
			}
			else
			{
				foreach (var builder in _otherBuilders)
				{
					if (builder.CanBuild(expression))
					{
						return true;
					}
				}
			}

			return false;
		}

	}
}
