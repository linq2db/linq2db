using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Reflection;

namespace LinqToDB.Expressions
{
	class SqlGenericConstructorExpression : Expression
	{
		public class Assignment
		{
			public static ReadOnlyCollection<Assignment> EmptyCollection = new (new List<Assignment>());

			public Assignment(MemberInfo memberInfo, Expression expression, bool isMandatory)
			{
				MemberInfo  = memberInfo;
				Expression  = expression;
				IsMandatory = isMandatory;
			}

			public MemberInfo MemberInfo  { get;  }
			public Expression Expression  { get; }
			public bool       IsMandatory { get; }

			public Assignment WithExpression(Expression expression)
			{
				if (expression == Expression)
					return this;
				return new Assignment(MemberInfo, expression, IsMandatory);
			}

			public override string ToString()
			{
				return $"{MemberInfo.Name} = {Expression}";
			}
		}

		public class Parameter
		{
			public static ReadOnlyCollection<Parameter> EmptyCollection = new (new List<Parameter>());

			public Parameter(Expression expression, MemberInfo? memberInfo)
			{
				MemberInfo  = memberInfo;
				Expression  = expression;
			}

			public Parameter(Expression expression)
			{
				Expression = expression;
			}

			public MemberInfo? MemberInfo { get; }
			public Expression  Expression { get; }

			public Parameter WithExpression(Expression expression)
			{
				if (expression == Expression)
					return this;
				return new Parameter(expression, MemberInfo);
			}

			public override string ToString()
			{
				return $"{Expression}";
			}
		}

		public SqlGenericConstructorExpression(bool isFull, Type objectType, ReadOnlyCollection<Parameter>? parameters, ReadOnlyCollection<Assignment>? assignments)
		{
			ObjectType    = objectType;
			ConstructType = isFull ? CreateType.Full : CreateType.Unknown;
			Parameters    = parameters  ?? Parameter.EmptyCollection;
			Assignments   = assignments ?? Assignment.EmptyCollection;
		}

		public SqlGenericConstructorExpression(NewExpression newExpression)
		{
			var items = new List<Assignment>(newExpression.Members?.Count ?? 0);

			if (newExpression.Members != null)
			{
				for (var i = 0; i < newExpression.Members.Count; i++)
				{
					var member = newExpression.Members[i];
					items.Add(new Assignment(member, Parse(newExpression.Arguments[i]), true));
				}
			}

			Constructor   = newExpression.Constructor;
			ConstructType = CreateType.New;
			ObjectType    = newExpression.Type;
			Parameters    = Parameter.EmptyCollection;
			Assignments   = new ReadOnlyCollection<Assignment>(items);
		}

		public static MemberInfo? FindMember(Type inType, ParameterInfo parameter)
		{
			var found = FindMember(TypeAccessor.GetAccessor(inType).Members, parameter);
			return found;
		}

		public static MemberInfo? FindMember(IReadOnlyCollection<MemberAccessor> members, ParameterInfo parameter)
		{
			MemberInfo? exactMatch   = null;
			MemberInfo? nonCaseMatch = null;

			foreach (var member in members)
			{
				if (member.Type == parameter.ParameterType)
				{
					if (member.Name == parameter.Name)
					{
						exactMatch = member.MemberInfo;
						break;
					}

					if (member.Name.Equals(parameter.Name, StringComparison.InvariantCultureIgnoreCase))
					{
						nonCaseMatch = member.MemberInfo;
						break;
					}

				}
			}

			return exactMatch ?? nonCaseMatch;
		}

		public SqlGenericConstructorExpression(MemberInitExpression memberInitExpression)
		{
			if (memberInitExpression.NewExpression.Members != null)
				throw new NotImplementedException();

			var arguments = memberInitExpression.NewExpression.Arguments;
			if (arguments.Count == 0)
			{
				Parameters = Parameter.EmptyCollection;
			}
			else
			{
				var constructorParameters = memberInitExpression.NewExpression.Constructor.GetParameters();
				var parameters = new List<Parameter>(arguments.Count);
				for (int i = 0; i < arguments.Count; i++)
				{
					parameters.Add(new Parameter(arguments[0],
						FindMember(memberInitExpression.Type, constructorParameters[i])));
				}

				Parameters = new ReadOnlyCollection<Parameter>(parameters);
			}
			
			var items = new List<Assignment>(memberInitExpression.Bindings.Count);

			for (var i = 0; i < memberInitExpression.Bindings.Count; i++)
			{
				var binding = memberInitExpression.Bindings[i];
				switch (binding.BindingType)
				{
					case MemberBindingType.Assignment:
					{
						var a = (MemberAssignment)binding;
						items.Add(new Assignment(a.Member, Parse(a.Expression), true));
						break;
					}

					default:
						throw new NotImplementedException();
				}
			}

			NewExpression = memberInitExpression.NewExpression;
			ConstructType = CreateType.MemberInit;
			ObjectType    = memberInitExpression.Type;
			Assignments   = new ReadOnlyCollection<Assignment>(items);
		}

		public Expression?      NewExpression  { get; private set; }
		public ConstructorInfo? Constructor    { get; private set; }
		public CreateType       ConstructType  { get; private set; }
		public Type             ObjectType     { get; private set; }

		public ReadOnlyCollection<Parameter>  Parameters  { get; private set; }
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
			Parameters  = null!;
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
				Parameters    = Parameters,
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
