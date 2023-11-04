using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlQuery.Visitors
{
	using Common;
	using SqlQuery;

	/// <summary>
	/// This base visitor implements:
	/// <list type="bullet">
	/// <item>tracking of replaced elemnents with <see cref="GetReplacement"/> API to access replacements;</item>
	/// <item>changes element's <see cref="VisitMode.Transform"/> to <see cref="VisitMode.Modify"/> for already replaced element;</item>
	/// <item>provides <see cref="ProcessElement"/> API to re-visit element;</item>
	/// <item>skips visit of replaced element.</item>
	/// </list>
	/// </summary>
	public abstract class SqlQueryVisitor : QueryElementVisitor
	{
		// contains replacement map of [old => new] elements
		// cannot store self-mappings
		Dictionary<IQueryElement, IQueryElement>? _replacements;
		// contains new replacement element
		HashSet<IQueryElement>?                   _replaced;

		/// <summary>
		/// Visitor replaces elements in visited tree with new elements from <see cref="_replacements"/> replacement map.
		/// Separate replace-only visitor used to avoid side-effects from parent <see cref="SqlQueryVisitor"/> implementor.
		/// </summary>
		sealed class Replacer : QueryElementVisitor
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

				// when element was already replaced with new instance, we don't need to replace it again and can modify it inplace
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

			// CteClause reference not visited by main dispatcher
			protected override IQueryElement VisitCteClauseReference(CteClause element)
			{
				if (_queryVisitor.GetReplacement(element, out var newElement))
					return newElement;

				return base.VisitCteClauseReference(element);
			}
		}

		protected SqlQueryVisitor(VisitMode visitMode) : base(visitMode)
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

			// when element was already replaced with new instance, we don't need to replace it again and can modify it inplace
			if (visitMode == VisitMode.Transform && _replaced?.Contains(element) == true)
				return VisitMode.Modify;

			return visitMode;
		}

		/// <summary>
		/// Visits <paramref name="element"/> and correct it, if it contains old replaced elements.
		/// </summary>
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
						throw new InvalidOperationException($"Visitor replaced already replaced element {newElement}");
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

		public override IQueryElement NotifyReplaced(IQueryElement newElement, IQueryElement oldElement)
		{
			AddReplacement(oldElement, newElement);

			return base.NotifyReplaced(newElement, oldElement);
		}

		/// <summary>
		/// Remembers element replacement.
		/// </summary>
		protected void AddReplacement(IQueryElement oldElement, IQueryElement newElement)
		{
			_replacements ??= new (Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

			_replaced ??= new (Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);
			_replaced.Add(newElement);

			// adding new replacement instance means incorrect visitor use
#if NETCOREAPP3_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
			_replacements.TryAdd(oldElement, newElement);
#else
			_replacements[oldElement] = newElement;
#endif
		}

		/// <summary>
		/// Adds explicit replacement map.
		/// </summary>
		protected void AddReplacements(IReadOnlyDictionary<IQueryElement, IQueryElement> replacements)
		{
			_replacements ??= new (Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

			_replaced ??= new (Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

			foreach (var pair in replacements)
			{
				if (ReferenceEquals(pair.Key, pair.Value))
					throw new ArgumentException($"{nameof(replacements)} contains entry with key == value");

				_replacements.Add(pair.Key, pair.Value);
				_replaced.Add(pair.Value);
			}
		}

		/// <summary>
		/// Returns replacement element for <paramref name="element"/> if it was registered as replaced.
		/// </summary>
		protected bool GetReplacement(IQueryElement element, [NotNullWhen(true)] out IQueryElement? replacement)
		{
			if (_replacements?.TryGetValue(element, out var current) == true)
			{
				if (_replacements.TryGetValue(current, out var currentReplacement))
				{
					throw new InvalidOperationException($"Visitor replaced already replaced element {current}");
				}

				replacement = current;
				return true;
			}

			replacement = null;
			return false;
		}

		/// <summary>
		/// Writes registered replacement pairs to <paramref name="objectTree"/> dictionary.
		/// </summary>
		public void GetReplacements(Dictionary<IQueryElement, IQueryElement> objectTree)
		{
			if (_replacements != null)
			{
				foreach (var pair in _replacements)
				{
					if (ReferenceEquals(pair.Key, pair.Value))
						throw new ArgumentException($"{nameof(objectTree)} contains entry with key == value");

					objectTree[pair.Key] = pair.Value;
				}
			}
		}
	}
}
