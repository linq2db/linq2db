namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	internal static class WritableContext
	{
		public static WritableContext<TWriteable, TStatic> Create<TWriteable, TStatic>(TWriteable init, TStatic staticValue)
		{
			return new WritableContext<TWriteable, TStatic>(staticValue)
			{
				WriteableValue = init
			};
		}
	}
}
