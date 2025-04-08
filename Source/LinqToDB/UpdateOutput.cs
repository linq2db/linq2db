namespace LinqToDB
{
	public class UpdateOutput<T>
	{
		public T Deleted { get; set; } = default!;
		public T Inserted { get; set; } = default!;
	}
}
