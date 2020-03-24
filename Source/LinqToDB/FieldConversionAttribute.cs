using System;
using System.Linq.Expressions;

namespace LinqToDB
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	internal class FieldConversionAttribute : Attribute
	{
		public LambdaExpression ToProviderQuery { get; set; }
		public Func<object, object> ToProvider { get; set; }
		public Func<object, object> ToModel    { get; set; }
	}
}
