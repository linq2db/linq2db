using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;

namespace LinqToDB.Linq.Relinq.Clauses
{
    public sealed class ExtendedJoinClause : IBodyClause, IQuerySource
    {
	    public Expression Predicate { get; set; }
	    public SqlJoinType JoinType { get; set; }

	    public ExtendedJoinClause([NotNull] string itemName, [NotNull] Type itemType,
		    [NotNull] Expression innerSequence, [NotNull] Expression predicate, SqlJoinType joinType)
        {
	        Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
	        JoinType = joinType;

	        ItemName = itemName ?? throw new ArgumentNullException(nameof(itemName));
            ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
            InnerSequence = innerSequence ?? throw new ArgumentNullException(nameof(innerSequence));
        }

        public Expression InnerSequence { get; set; }

        public void Accept ([NotNull] IQueryModelVisitor visitor, [NotNull] QueryModel queryModel, int index)
        {
	        if (visitor == null) throw new ArgumentNullException(nameof(visitor));
	        if (queryModel == null) throw new ArgumentNullException(nameof(queryModel));

			if (visitor is IExtendedQueryVisitor v)
				v.VisitJoinClause(this, queryModel, index);
        }

        IBodyClause IBodyClause.Clone (CloneContext cloneContext)
        {
            return Clone (cloneContext);
        }

        /// <summary>
        ///     Transforms all the expressions in this clause and its child objects via the given
        ///     <paramref name="transformation" /> delegate.
        /// </summary>
        /// <param name="transformation">
        ///     The transformation object. This delegate is called for each <see cref="Expression" /> within this
        ///     clause, and those expressions will be replaced with what the delegate returns.
        /// </param>
        public void TransformExpressions ([NotNull] Func<Expression, Expression> transformation)
        {
	        if (transformation == null) throw new ArgumentNullException(nameof(transformation));
	        InnerSequence = transformation (InnerSequence);
            Predicate = transformation (Predicate);
        }

        public Type ItemType { get; set; }
        public string ItemName { get; set; }

        /// <summary>
        ///     Clones this clause, registering its clone with the <paramref name="cloneContext" />.
        /// </summary>
        /// <param name="cloneContext">The clones of all query source clauses are registered with this <see cref="CloneContext" />.</param>
        /// <returns>A clone of this clause.</returns>
        public ExtendedJoinClause Clone ([NotNull] CloneContext cloneContext)
        {
	        if (cloneContext == null) throw new ArgumentNullException(nameof(cloneContext));
	        var clone = new ExtendedJoinClause(ItemName, ItemType, InnerSequence, Predicate, JoinType);
            cloneContext.QuerySourceMapping.AddMapping (this, new QuerySourceReferenceExpression (clone));
            return clone;
        }

        public override string ToString ()
        {
	        return $"join {ItemType.Name} {ItemName} in {InnerSequence} on {Predicate}";
        }
    }
}
