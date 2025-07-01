namespace Shouldly
{
	public interface ITimesConstraint
	{
		TimesType Type { get; }
		int Times { get; }
	}
}
