using System.Linq.Expressions;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Linq.Parser
{
	public static class ExpressionGeneratorHelper
	{
		/// <summary>
		/// Gets Expression.Equal if <paramref name="left"/> and <paramref name="right"/> expression types are not same
		/// <paramref name="right"/> would be converted to <paramref name="left"/>
		/// </summary>
		/// <param name="mappringSchema"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static BinaryExpression Equal(MappingSchema mappringSchema, Expression left, Expression right)
		{
			if (left.Type != right.Type)
			{
				if (right.Type.CanConvertTo(left.Type))
					right = Expression.Convert(right, left.Type);
				else if (left.Type.CanConvertTo(right.Type))
					left = Expression.Convert(left, right.Type);
				else
				{
					var rightConvert = ConvertBuilder.GetConverter(mappringSchema, right.Type, left. Type);
					var leftConvert  = ConvertBuilder.GetConverter(mappringSchema, left. Type, right.Type);

					var leftIsPrimitive  = left. Type.IsPrimitiveEx();
					var rightIsPrimitive = right.Type.IsPrimitiveEx();

					if (leftIsPrimitive == true && rightIsPrimitive == false && rightConvert.Item2 != null)
						right = rightConvert.Item2.GetBody(right);
					else if (leftIsPrimitive == false && rightIsPrimitive == true && leftConvert.Item2 != null)
						left = leftConvert.Item2.GetBody(left);
					else if (rightConvert.Item2 != null)
						right = rightConvert.Item2.GetBody(right);
					else if (leftConvert.Item2 != null)
						left = leftConvert.Item2.GetBody(left);
				}
			}

			return Expression.Equal(left, right);
		}		
	}
}
