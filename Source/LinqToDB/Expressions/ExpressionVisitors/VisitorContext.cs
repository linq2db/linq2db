namespace LinqToDB.Expressions
{
	internal class WritableContext<T>
	{
		public T WriteableValue = default!;
	}

	internal class WritableContext<TWriteable, TStatic>
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
