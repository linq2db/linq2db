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

	//internal class StaticContext<TStatic1, TStatic2>
	//{
	//	public StaticContext(TStatic1 staticValue1, TStatic2 staticValue2)
	//	{
	//		StaticValue1 = staticValue1;
	//		StaticValue2 = staticValue2;
	//	}

	//	public readonly TStatic1 StaticValue1;
	//	public readonly TStatic2 StaticValue2;
	//}
}
