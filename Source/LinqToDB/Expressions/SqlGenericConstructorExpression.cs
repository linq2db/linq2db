using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	class SqlGenericConstructorExpression : Expression
	{
		public class Assignment
		{
			public Assignment(MemberInfo memberInfo, Expression expression)
			{
				MemberInfo = memberInfo;
				Expression = expression;
			}

			public MemberInfo MemberInfo { get;  }
			public Expression Expression { get; }

			public Assignment WithExpression(Expression expression)
			{
				if (expression == Expression)
					return this;
				return new Assignment(MemberInfo, expression);
			}

			public override string ToString()
			{
				return $"{MemberInfo.Name} = {Expression}";
			}
		}

		public SqlGenericConstructorExpression(bool isFull, Type objectType, IList<Assignment> assignments)
		{
			ObjectType    = objectType;
			ConstructType = isFull ? CreateType.Full : CreateType.Unknown;
			Assignments   = new ReadOnlyCollection<Assignment>(assignments);
		}

		public SqlGenericConstructorExpression(NewExpression newExpression)
		{
			var items = new List<Assignment>(newExpression.Members?.Count ?? 0);

			if (newExpression.Members != null)
			{
				for (var i = 0; i < newExpression.Members.Count; i++)
				{
					var member = newExpression.Members[i];
					items.Add(new Assignment(member, Parse(newExpression.Arguments[i])));
				}
			}

			Constructor   = newExpression.Constructor;
			ConstructType = CreateType.New;
			ObjectType    = newExpression.Type;
			Assignments   = new ReadOnlyCollection<Assignment>(items);
		}

		public SqlGenericConstructorExpression(MemberInitExpression memberInitExpression)
		{
			var items = new List<Assignment>(memberInitExpression.Bindings.Count);

			for (var i = 0; i < memberInitExpression.Bindings.Count; i++)
			{
				var binding = memberInitExpression.Bindings[i];
				switch (binding.BindingType)
				{
					case MemberBindingType.Assignment:
					{
						var a = (MemberAssignment)binding;
						items.Add(new Assignment(a.Member, Parse(a.Expression)));
						break;
					}

					default:
						throw new NotImplementedException();
				}
			}

			NewExpression = Parse(memberInitExpression.NewExpression);
			ConstructType = CreateType.MemberInit;
			ObjectType    = memberInitExpression.Type;
			Assignments   = new ReadOnlyCollection<Assignment>(items);
		}

		public Expression?      NewExpression { get; private set; }
		public ConstructorInfo? Constructor   { get; private set; }
		public CreateType       ConstructType { get; private set; }
		public Type             ObjectType    { get; private set; }

		public ReadOnlyCollection<Assignment> Assignments { get; private set; }

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => ObjectType;

		public override string ToString()
		{
			var assignments = string.Join(",\n", Assignments.Select(a => $"\t{a.MemberInfo.Name} = {a.Expression}"));

			var result = $"new {Type.Name}({ConstructType})\n{{\n{assignments}\n}}";

			return result;
		}

		public enum CreateType
		{
			Unknown,
			Full,
			New,
			MemberInit,
		}

		SqlGenericConstructorExpression()
		{
			Assignments = null!;
			ObjectType  = null!;
		}

		public SqlGenericConstructorExpression AppendAssignment(Assignment assignment)
		{
			var newAssignments = new List<Assignment>(Assignments.Count + 1);
			newAssignments.AddRange(Assignments);
			newAssignments.Add(assignment);

			return ReplaceAssignments(newAssignments);
		}

		public SqlGenericConstructorExpression ReplaceAssignments(List<Assignment> assignment)
		{
			var createNew = assignment.Count != Assignments.Count;

			if (!createNew)
			{
				createNew = !Assignments.SequenceEqual(assignment);
			}

			if (!createNew)
				return this;

			var result = new SqlGenericConstructorExpression
			{
				Assignments   = new ReadOnlyCollection<Assignment>(assignment),
				ObjectType    = ObjectType,
				Constructor   = Constructor,
				ConstructType = ConstructType,
				NewExpression = NewExpression,
			};

			return result;
		}

		public static Expression Parse(Expression createExpression)
		{
			switch (createExpression.NodeType)
			{
				case ExpressionType.New:
				{
					return new SqlGenericConstructorExpression((NewExpression)createExpression);
				}

				case ExpressionType.MemberInit:
				{
					return new SqlGenericConstructorExpression((MemberInitExpression)createExpression);
				}
			}

			return createExpression;
		}
	}
}
