using System;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Linq.Parser.Clauses;
using LinqToDB.Mapping;

namespace LinqToDB.Linq.Parser.Builders
{
	public class AssociationBuilder : BaseBuilder
	{
		public override bool CanBuild(ModelTranslator builder, Expression expression)
		{
			return GetAssociationAttribute(builder, expression) != null;
		}

		private static AssociationAttribute GetAssociationAttribute(ModelTranslator builder, Expression expression)
		{
			MemberInfo memberInfo;
			Type type;

			switch (expression.NodeType)
			{
				case ExpressionType.MemberAccess:
					memberInfo = ((MemberExpression)expression).Member;
					type = ((MemberExpression)expression).Expression.Type;
					break;
				case ExpressionType.Call:
					memberInfo = ((MethodCallExpression)expression).Method;
					type = memberInfo.DeclaringType;
					break;
				default:
					return null;
			}

			var attr = builder.MappingSchema.GetAttribute<AssociationAttribute>(type, memberInfo);
			return attr;
		}

		public override Sequence BuildSequence(ModelTranslator builder, ParseBuildInfo parseBuildInfo, Expression expression)
		{
			var attr = GetAssociationAttribute(builder, expression);
			if (expression is MemberExpression me)
			{
				var registry = builder.GenerateAssociation(parseBuildInfo.Sequence, attr, expression, me.Member);

				parseBuildInfo.Sequence.AddClause(registry.Clause);

				return parseBuildInfo.Sequence;
			}

			//TODO
			throw new NotImplementedException();
		}
	}
}
