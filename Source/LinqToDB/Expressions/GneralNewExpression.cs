using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Expressions
{
	public abstract class UnifiedNewExpression : BaseCustomExpression
	{
		private Tuple<MemberInfo, Expression>[] _members;

		public Expression ObjectExpression { get; }

		public UnifiedNewExpression([JetBrains.Annotations.NotNull] NewExpression newExpression)
		{
			NewExpression = newExpression ?? throw new ArgumentNullException(nameof(newExpression));
			Type = newExpression.Type;

			_members = NewExpression.Members
				.Select((m, i) => Tuple.Create(m, NewExpression.Arguments[i]))
				.ToArray();
		}

		public UnifiedNewExpression([JetBrains.Annotations.NotNull] MemberInitExpression memberInitExpression)
		{
			MemberInitExpression = memberInitExpression ?? throw new ArgumentNullException(nameof(memberInitExpression));
			Type = MemberInitExpression.Type;

			_members = MemberInitExpression.NewExpression.Members
				.Select((m, i) => Tuple.Create(m, NewExpression.Arguments[i]))
				.ToArray();
		}

		public UnifiedNewExpression([JetBrains.Annotations.NotNull] Expression objectExpression, [JetBrains.Annotations.NotNull] MappingSchema mappingSchema)
		{
			if (objectExpression == null) throw new ArgumentNullException(nameof(objectExpression));
			if (mappingSchema == null) throw new ArgumentNullException(nameof(mappingSchema));
			if (!objectExpression.Type.IsClassEx())
				throw new ArgumentNullException(nameof(objectExpression), "Expression should be class type");

			ObjectExpression = objectExpression;
			Type = objectExpression.Type;

			var entityDescriptor = mappingSchema.GetEntityDescriptor(objectExpression.Type);

			_members = entityDescriptor.Columns
				.Select(c => Tuple.Create(c.MemberInfo, (Expression)MakeMemberAccess(objectExpression, c.MemberInfo)))
				.ToArray();
		}

		public override Expression Reduce()
		{
			if (NewExpression != null)
				return NewExpression;
			if (MemberInitExpression != null)
				return MemberInitExpression;

			if (ObjectExpression != null)
			{
				//TODO: complex!

				var ctor = ObjectExpression.Type.GetConstructors().Single();
				var newExpr = Expression.New(ctor, _members.Select(t => t.Item2), _members.Select(t => t.Item1));
				return newExpr;
			}

			throw new InvalidOperationException();
		}

		public NewExpression NewExpression { get; }
		public MemberInitExpression MemberInitExpression { get; }

		public override Type Type { get; }
		public override ExpressionType NodeType => ExpressionType.Extension;
		public override bool CanReduce => true;

		public override void CustomVisit(Action<Expression> func)
		{
			foreach (var member in _members)
				func(member.Item2);
		}

		public override bool CustomVisit(Func<Expression, bool> func)
		{
			throw new NotImplementedException();
		}

		public override Expression CustomFind(Func<Expression, bool> func)
		{
			throw new NotImplementedException();
		}

		public override Expression CustomTransform(Func<Expression, Expression> func)
		{
			throw new NotImplementedException();
		}
	}
}
