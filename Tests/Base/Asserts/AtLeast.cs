namespace Shouldly
{
	public sealed class AtLeast(int times) : ITimesConstraint
	{
		private static readonly ITimesConstraint _once = new AtLeast(1);
		private static readonly ITimesConstraint _twice = new AtLeast(2);
		private static readonly ITimesConstraint _thrice = new AtLeast(3);

		public static ITimesConstraint Once() => _once;
		public static ITimesConstraint Twice() => _twice;
		public static ITimesConstraint Thrice() => _thrice;

		public static ITimesConstraint Times(int times) => new AtLeast(times);

		TimesType ITimesConstraint.Type => TimesType.AtLeast;
		int ITimesConstraint.Times => times;
	}
}
