using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Linq.Relinq.Extensions
{
	public class UnifiedNewExpression : Expression
	{
		public ConstructorInfo Constructor { get; }

		private List<MemberMappingInfo> _members;
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
				.Select((m, i) => new MemberMappingInfo(m, NewExpression.Arguments[i]))
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
				.Select((m, i) => new MemberMappingInfo(m, NewExpression.Arguments[i]))
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
					.Select(c => new MemberMappingInfo(c.MemberInfo, MakeMemberAccess(objectExpression, c.MemberInfo)))
					.ToList();
			else
				_members = _type.GetProperties()
					.Select(p => new MemberMappingInfo(p, MakeMemberAccess(objectExpression, p)))
					.ToList();
		}

		public UnifiedNewExpression([JetBrains.Annotations.NotNull] ConstructorInfo constructor, IEnumerable<MemberMappingInfo> members)
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
				var newExpr = Expression.New(ctor);

				var memberInit = Expression.MemberInit(newExpr,
					Members.Select(m => Expression.Bind(m.MemberInfo, m.Expression)));

				return memberInit;
			}

			if (Constructor != null)
			{
				NewExpression newExpression;

				var pms = Constructor.GetParameters();

				Tuple<MemberInfo, Expression>[] toPass = _members
					.Select(m =>
						Tuple.Create(m.MemberInfo, m.Expression.Transform(e => e is UnifiedNewExpression ne ? ne.Reduce() : e)))
					.ToArray();

				if (pms.Length == 0)
					newExpression = Expression.New(Constructor);
				else
				{
					var args = pms.Join(_members, p => p.Name, m => m.MemberInfo.Name, (p, m) => m).ToArray();
					newExpression = Expression.New(Constructor, args.Select(m => m.Expression));
					toPass = toPass.Where(m => args.All(a => m.Item1 != a.MemberInfo)).ToArray();
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
			_members.Add(new MemberMappingInfo(memberInfo, expression));
		}

		public IReadOnlyList<MemberMappingInfo> Members => _members;
	}
}
