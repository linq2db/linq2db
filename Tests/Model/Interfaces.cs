using LinqToDB.Mapping;

namespace Tests.Model
{
	public interface IMappedId
	{
		public int Id { get; set; }
	}

	public interface IMappedId<T>
	{
		public T Id { get; set; }
	}

	// must implement IMappedId without specifying IMappedId in implementation list
	[Table("Person")]
	public class PersonBaseExternal
	{
		[Column("PersonID")] public int Id { get; set; }
	}
}
