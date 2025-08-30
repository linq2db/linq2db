using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Abstract Attribute to be used for skipping values
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public abstract class SkipBaseAttribute : MappingAttribute
	{
		/// <summary>
		/// Check if object contains values that should be skipped.
		/// </summary>
		/// <param name="obj">The object to check.</param>
		/// <param name="entityDescriptor">The entity descriptor.</param>
		/// <param name="columnDescriptor">The column descriptor.</param>
		/// <returns><c>true</c> if object should be skipped for the operation.</returns>
		public abstract bool ShouldSkip(object obj, EntityDescriptor entityDescriptor, ColumnDescriptor columnDescriptor);

	  /// <summary>
	  /// Defines on which method a value should be skipped.
	  /// </summary>
		public abstract SkipModification Affects { get; }

		public override string GetObjectID()
		{
			return FormattableString.Invariant($".{Configuration}.{(int)Affects}.");
		}
	}
}
