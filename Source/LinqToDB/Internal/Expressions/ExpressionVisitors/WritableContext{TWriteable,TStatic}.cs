namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	internal sealed class WritableContext<TWriteable, TStatic>
	{
		public WritableContext(TStatic staticValue)
		{
			StaticValue = staticValue;
		}

		public readonly TStatic StaticValue;

		public TWriteable WriteableValue = default!;
	}
}
