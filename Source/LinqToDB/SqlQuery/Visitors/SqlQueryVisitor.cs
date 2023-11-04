﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlQuery.Visitors
{
	using Common;
	using SqlQuery;

	public class SqlQueryVisitor : QueryElementVisitor
	{
		Dictionary<IQueryElement, IQueryElement>? _replacements;
		HashSet<IQueryElement>?                   _replaced;

		class Replacer : QueryElementVisitor
		{
			readonly SqlQueryVisitor _queryVisitor;

			public Replacer(SqlQueryVisitor queryVisitor) : base(queryVisitor.VisitMode)
			{
				_queryVisitor = queryVisitor;
			}

			public override IQueryElement NotifyReplaced(IQueryElement newElement, IQueryElement oldElement)
			{
				return _queryVisitor.NotifyReplaced(newElement, oldElement);
			}

			public override VisitMode GetVisitMode(IQueryElement element)
			{
				var visitMode = VisitMode;
				if (visitMode == VisitMode.ReadOnly)
					return VisitMode.ReadOnly;

				// We can just update elements which are replaced by new instances
				//
				if (visitMode == VisitMode.Transform && _queryVisitor._replaced?.Contains(element) == true)
					return VisitMode.Modify;

				return visitMode;
			}

			[return: NotNullIfNotNull(nameof(element))]
			public override IQueryElement? Visit(IQueryElement? element)
			{
				if (element != null && _queryVisitor.GetReplacement(element, out var newElement))
					return newElement;

				return base.Visit(element);
			}

			protected override IQueryElement VisitCteClauseReference(CteClause element)
			{
				if (_queryVisitor.GetReplacement(element, out var newElement))
					return newElement;

				return base.VisitCteClauseReference(element);
			}

		}

		public SqlQueryVisitor(VisitMode visitMode) : base(visitMode)
		{
		}

		public virtual void Cleanup()
		{
			_replacements = null;
			_replaced     = null;
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			if (element == null) return null;

			if (GetReplacement(element, out var newElement))
				return newElement;

			return base.Visit(element);
		}

		public override VisitMode GetVisitMode(IQueryElement element)
		{
			var visitMode = VisitMode;
			if (visitMode == VisitMode.ReadOnly)
				return VisitMode.ReadOnly;

			// We can just update elements which are replaced by new instances
			//
			if (visitMode == VisitMode.Transform && _replaced?.Contains(element) == true)
				return VisitMode.Modify;

			return visitMode;
		}

		public virtual IQueryElement ProcessElement(IQueryElement element)
		{
			var newElement = Visit(element);
			if (!ReferenceEquals(newElement, element))
			{
				if (VisitMode == VisitMode.ReadOnly)
					throw new InvalidOperationException("VisitMode is readonly but element changed.");

				if (_replacements != null)
				{
					// go through tree and correct references

					var replacer  = new Replacer(this);
					var finalized = replacer.Visit(newElement);
					if (!ReferenceEquals(newElement, finalized))
					{
						// do we need to run again?
					}

					return finalized;
				}
			}

			return newElement;
		}

		protected override IQueryElement VisitCteClauseReference(CteClause element)
		{
			if (GetReplacement(element, out var newElement))
				return newElement;

			return base.VisitCteClauseReference(element);
		}

		protected override IQueryElement VisitCteClause(CteClause element)
		{
			if (GetReplacement(element, out var newElement))
				return newElement;

			return base.VisitCteClause(element);
		}

		protected override IQueryElement VisitSqlColumnReference(SqlColumn element)
		{
			if (GetReplacement(element, out var replacement))
				return replacement;

			return base.VisitSqlColumnReference(element);
		}

		protected override IQueryElement VisitSqlFieldReference(SqlField element)
		{
			if (GetReplacement(element, out var replacement))
				return replacement;

			return base.VisitSqlFieldReference(element);
		}

		public override IQueryElement NotifyReplaced(IQueryElement newElement, IQueryElement oldElement)
		{
			AddReplacement(oldElement, newElement);

			return base.NotifyReplaced(newElement, oldElement);
		}

		/*
		protected void AddReplacements(IEnumerable<SqlColumn> columns, IEnumerable<ISqlExpression> replacements)
		{
			_columnReplacements ??= new Dictionary<SqlColumn, ISqlExpression>(Utils.ObjectReferenceEqualityComparer<SqlColumn>.Default);

			using var ce = columns.GetEnumerator();
			using var re = replacements.GetEnumerator();

			while (ce.MoveNext() && re.MoveNext())
			{
				_columnReplacements[ce.Current] = re.Current;
			}
		}

		*/

		protected void AddReplacements(IEnumerable<IQueryElement> oldElements, IEnumerable<IQueryElement> newElements)
		{
			_replacements ??= new Dictionary<IQueryElement, IQueryElement>(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

			using var oe = oldElements.GetEnumerator();
			using var ne = newElements.GetEnumerator();

			while (oe.MoveNext() && ne.MoveNext())
			{
				_replacements[oe.Current] = ne.Current;
			}
		}

		protected void AddReplacement(IQueryElement oldElement, IQueryElement newElement)
		{
			_replacements ??= new Dictionary<IQueryElement, IQueryElement>(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

			_replaced ??= new HashSet<IQueryElement>(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);
			_replaced.Add(newElement);

			_replacements[oldElement] = newElement;
		}

		protected void AddReplacements(IReadOnlyDictionary<IQueryElement, IQueryElement> replacements)
		{
			_replacements ??= new Dictionary<IQueryElement, IQueryElement>(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

			_replaced ??= new HashSet<IQueryElement>(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

			foreach (var pair in replacements)
			{
				if (ReferenceEquals(pair.Key, pair.Value))
					continue;
				_replacements[pair.Key] = pair.Value;
				_replaced.Add(pair.Value);
			}
		}

		protected bool GetReplacement(IQueryElement element, [NotNullWhen(true)] out IQueryElement? replacement)
		{
			if (_replacements != null && _replacements.TryGetValue(element, out var current))
			{
				while (_replacements.TryGetValue(current, out var currentReplacement))
				{
					if (ReferenceEquals(element, currentReplacement))
						break;
					current = currentReplacement;
				}

				replacement = current;
				return true;
			}

			replacement = null;
			return false;
		}

		public void GetReplacements(Dictionary<IQueryElement, IQueryElement> objectTree)
		{
			if (_replacements != null)
			{
				foreach (var pair in _replacements)
				{
					if (!ReferenceEquals(pair.Key, pair.Value))
						objectTree[pair.Key] = pair.Value;
				}
			}
		}

	}
}
