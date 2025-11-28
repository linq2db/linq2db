using System.Linq.Expressions;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public class CombinedMemberConverter : IMemberConverter
	{
		private readonly IMemberConverter[] _converters;

		public CombinedMemberConverter(IMemberConverter[] converters)
		{
			_converters = converters;
		}

		public Expression Convert(Expression expression, out bool handled)
		{
			foreach (var converter in _converters)
			{
				var result = converter.Convert(expression, out handled);
				if (handled)
					return result;
			}

			handled = false;
			return expression;
		}
	}
}
