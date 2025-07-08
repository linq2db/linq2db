namespace Shouldly
{
	public sealed class Exactly(int times) : ITimesConstraint
	{
		private static readonly ITimesConstraint _once = new Exactly(1);
		private static readonly ITimesConstraint _twice = new Exactly(2);
		private static readonly ITimesConstraint _thrice = new Exactly(3);

		public static ITimesConstraint Once() => _once;
		public static ITimesConstraint Twice() => _twice;
		public static ITimesConstraint Thrice() => _thrice;
		public static ITimesConstraint Times(int times) => new Exactly(times);

		TimesType ITimesConstraint.Type => TimesType.Exactly;
		int ITimesConstraint.Times => times;
	}
}
