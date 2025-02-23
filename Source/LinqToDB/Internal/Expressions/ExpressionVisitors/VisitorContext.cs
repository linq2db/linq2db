namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	internal sealed class WritableContext<T>
	{
		public T WriteableValue = default!;
	}

	internal sealed class WritableContext<TWriteable, TStatic>
	{
		public WritableContext(TStatic staticValue)
		{
			StaticValue = staticValue;
		}

		public readonly TStatic StaticValue;

		public TWriteable WriteableValue = default!;
	}

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
