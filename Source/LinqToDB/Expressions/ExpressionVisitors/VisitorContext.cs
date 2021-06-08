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
}
