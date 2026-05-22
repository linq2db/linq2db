namespace LinqToDB
{
	public enum MergeOperationType
	{
		None = 0,
		Insert,
		Update,
		Delete,
		UpdateWithDelete,
		UpdateBySource,
		DeleteBySource,
	}
}
