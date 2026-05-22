namespace LinqToDB
{
	public static partial class Sql
	{
		public enum AggregateModifier
		{
			None,
			Distinct,
			All,
		}

		public enum From
		{
			None,
			First,
			Last,
		}

		public enum Nulls
		{
			None,
			Respect,
			Ignore,
		}

		public enum NullsPosition
		{
			None,
			First,
			Last,
		}
	}
}
