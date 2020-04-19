using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Attribute for skipping specific values on update.
	/// </summary>
	[CLSCompliant(false)]
	public class SkipValuesOnUpdateAttribute : SkipValuesByListAttribute
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="values">
		/// Values to skip on update operations.
		/// </param>
		public SkipValuesOnUpdateAttribute(params object?[]? values) : base(values ?? new object?[] { null }) { }

		/// <summary>
		/// Operations, affected by value skipping.
		/// </summary>
		public override SkipModification Affects => SkipModification.Update;
	}
}
