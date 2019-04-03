namespace LinqToDB.Mapping
{
	/// <summary>
	/// Attribute for skipping specific values on insert.
	/// </summary>
	public class SkipValuesOnInsertAttribute : SkipValuesByListAttribute
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="values">
		/// Values to skip on insert operations.
		/// </param>
		public SkipValuesOnInsertAttribute(params object[] values) : base(values ?? new object[] { null }) { }

		/// <summary>
		/// Operations, affected by value skipping.
		/// </summary>
		public override SkipModification Affects => SkipModification.Insert;
	}
}
