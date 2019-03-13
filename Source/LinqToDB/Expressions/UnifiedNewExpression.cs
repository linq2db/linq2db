using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Expressions
{
	public class UnifiedNewExpression : BaseCustomExpression
	{
		public ConstructorInfo Constructor { get; }

		private List<Tuple<MemberInfo, Expression>> _members;
		private bool _modified;
		private Type _type;

		public Expression ObjectExpression { get; set; }

		public UnifiedNewExpression([JetBrains.Annotations.NotNull] NewExpression newExpression)
		{
			Init(newExpression);
		}

		private void Init(NewExpression newExpression)
		{
			NewExpression = newExpression ?? throw new ArgumentNullException(nameof(newExpression));
			_type = newExpression.Type;

			_members = NewExpression.Members
				.Select((m, i) => Tuple.Create(m, NewExpression.Arguments[i]))
				.ToList();
		}

		public UnifiedNewExpression([JetBrains.Annotations.NotNull] MemberInitExpression memberInitExpression)
		{
			Init(memberInitExpression);
		}

		private void Init(MemberInitExpression memberInitExpression)
		{
			MemberInitExpression = memberInitExpression ?? throw new ArgumentNullException(nameof(memberInitExpression));
			_type = MemberInitExpression.Type;

			_members = MemberInitExpression.NewExpression.Members
				.Select((m, i) => Tuple.Create(m, NewExpression.Arguments[i]))
				.ToList();
		}

		public UnifiedNewExpression([JetBrains.Annotations.NotNull] Expression objectExpression, [JetBrains.Annotations.NotNull] MappingSchema mappingSchema)
		{
			if (objectExpression == null) throw new ArgumentNullException(nameof(objectExpression));
			if (mappingSchema == null) throw new ArgumentNullException(nameof(mappingSchema));
			if (!objectExpression.Type.IsClassEx())
				throw new ArgumentNullException(nameof(objectExpression), "Expression should be class type");

			Init(objectExpression, mappingSchema);
		}

		private void Init(Expression objectExpression, MappingSchema mappingSchema)
		{
			ObjectExpression = objectExpression;
			_type = objectExpression.Type;

			var entityDescriptor = mappingSchema.GetEntityDescriptor(objectExpression.Type);

			if (entityDescriptor.Columns.Count > 0)
				_members = entityDescriptor.Columns
					.Select(c => Tuple.Create(c.MemberInfo, (Expression)MakeMemberAccess(objectExpression, c.MemberInfo)))
					.ToList();
			else
				_members = _type.GetProperties()
					.Select(p => Tuple.Create<MemberInfo, Expression>(p, MakeMemberAccess(objectExpression, p)))
					.ToList();
		}

		public UnifiedNewExpression([JetBrains.Annotations.NotNull] ConstructorInfo constructor, IEnumerable<Tuple<MemberInfo, Expression>> members)
		{
			Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));

			_type = Constructor.DeclaringType;
			_members = members.ToList();
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

			if (Constructor != null)
			{
				NewExpression newExpression;

				var pms = Constructor.GetParameters();

				Tuple<MemberInfo, Expression>[] toPass = _members
					.Select(m =>
						Tuple.Create(m.Item1, m.Item2.Transform(e => e is UnifiedNewExpression ne ? ne.Reduce() : e)))
					.ToArray();

				if (pms.Length == 0)
					newExpression = Expression.New(Constructor);
				else
				{
					var args = pms.Join(_members, p => p.Name, m => m.Item1.Name, (p, m) => m).ToArray();
					newExpression = Expression.New(Constructor, args.Select(m => m.Item2));
					toPass = toPass.Where(m => args.All(a => m.Item1 != a.Item1)).ToArray();
				}

				return Expression.MemberInit(newExpression,
					toPass.Select(m => Expression.Bind(m.Item1, m.Item2.Reduce())));
			}

			throw new InvalidOperationException();
		}

		public NewExpression NewExpression { get; private set; }
		public MemberInitExpression MemberInitExpression { get; private set; }

		public override Type Type => _type;
		public override ExpressionType NodeType => ExpressionType.Extension;
		public override bool CanReduce => true;

		public void AddMember(MemberInfo memberInfo, Expression expression)
		{
			_modified = true;
			_members.Add(Tuple.Create(memberInfo, expression));
		}

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

		public override bool CustomEquals(Expression other)
		{
			throw new NotImplementedException();
		}

		public IReadOnlyList<Tuple<MemberInfo, Expression>> Members => _members;
	}
}
