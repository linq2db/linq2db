using System.Linq.Expressions;

namespace LinqToDB.Internal.Conversion
{
	public record struct ConverterLambda(LambdaExpression CheckNullLambda, LambdaExpression? Lambda, bool IsSchemaSpecific);
}
