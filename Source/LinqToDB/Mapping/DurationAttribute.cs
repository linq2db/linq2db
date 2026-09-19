using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Declares that a <see cref="TimeSpan"/> column stores a duration, and in which unit.
	/// </summary>
	/// <remarks>
	/// Without this attribute (or the equivalent fluent or mapping schema configuration) a <see cref="TimeSpan"/>
	/// column keeps its existing meaning, which is provider-defined and is a time of day rather than a duration on
	/// providers that map <see cref="TimeSpan"/> to a <c>TIME</c> column. Duration semantics are opt-in precisely so
	/// that existing mappings are not silently reinterpreted.
	/// <para>
	/// The unit is what makes translation possible: a <c>BIGINT</c> column alone does not say whether it holds ticks
	/// or seconds, and guessing wrong is a silent factor-of-10000000 error.
	/// </para>
	/// Applying this attribute to a class or interface has no effect.
	/// </remarks>
	/// <example>
	/// <code>
	/// [Column(DataType = DataType.Int64), Duration(DurationUnit.Second)]
	/// public TimeSpan Elapsed { get; set; }
	/// </code>
	/// </example>
	[AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property,
		AllowMultiple = true, Inherited = true)]
	public class DurationAttribute : MappingAttribute
	{
		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="unit">Unit in which the duration is stored.</param>
		public DurationAttribute(DurationUnit unit)
		{
			Unit = unit;
		}

		/// <summary>
		/// Gets or sets the unit in which the duration is stored in the column.
		/// </summary>
		public DurationUnit Unit { get; set; }

		public override string GetObjectID()
		{
			return $".{Configuration}.{Unit}.";
		}
	}
}
