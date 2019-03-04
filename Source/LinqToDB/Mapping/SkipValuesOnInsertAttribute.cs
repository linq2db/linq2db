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
		public SkipValuesOnInsertAttribute(params object[] values) : base(values){ }

		/// <summary>
		/// Operations that affects skipping a value.
		/// </summary>
		public override SkipModification Affects => SkipModification.Insert;
	}
}
