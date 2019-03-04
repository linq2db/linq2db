namespace LinqToDB.Mapping
{
	/// <summary>
	/// Attribute for skipping specific values on update.
	/// </summary>
	public class SkipValuesOnUpdateAttribute : SkipValuesByListAttribute
	{
		/// <summary>  
		/// Constructor. 
		/// </summary>
		/// <param name="values"> 
		/// Values to skip on update operations.
		/// </param>
		public SkipValuesOnUpdateAttribute(params object[] values) : base(values){ }

		/// <summary>
		/// Operations that affects skipping a value.
		/// </summary>
		public override SkipModification Affects => SkipModification.Update;
	}
}
