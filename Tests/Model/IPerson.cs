namespace Tests.Model
{
	public interface IPerson
	{
		int    ID         { get; set; }
		Gender Gender     { get; set; }
		string FirstName  { get; set; }
		string MiddleName { get; set; }
		string LastName   { get; set; }
	}
}
