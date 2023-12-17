using System;
using System.Linq.Expressions;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class CompositeAssociationAttribute : AssociationAttribute
	{
		public Expression? CurrentExpression { get; set; }
	}
}
