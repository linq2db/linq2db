using System;
using System.Globalization;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Declares that a mapped <see cref="TimeSpan"/> column represents a duration and specifies its storage unit.
	/// The attribute does not change the column's database type or value converter.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
	public sealed class DurationAttribute : MappingAttribute
	{
		/// <summary>
		/// Creates a duration mapping.
		/// </summary>
		/// <param name="unit">Unit used to store the duration.</param>
		public DurationAttribute(DurationUnit unit)
		{
			Unit = unit;
		}

		/// <summary>
		/// Gets the unit used to store the duration.
		/// </summary>
		public DurationUnit Unit { get; }

		/// <inheritdoc />
		public override string GetObjectID()
		{
			return string.Create(CultureInfo.InvariantCulture, $".{Configuration}.{(int)Unit}.");
		}
	}
}
