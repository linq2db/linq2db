using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Specifies value generation sequence for mapped property of field.
	/// Currently it supported only for:
	/// <list type="bullet">
	/// <item>Firebird generators</item>
	/// <item>Oracle sequences</item>
	/// <item>PostgreSQL serial pseudotypes/sequences</item>
	/// </list>
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public class SequenceNameAttribute : MappingAttribute
	{
		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="configuration">Mapping schema configuration name. See <see cref="Configuration"/>.</param>
		/// <param name="sequenceName">Sequence generator name.</param>
		public SequenceNameAttribute(string? configuration, string sequenceName)
		{
			Configuration = configuration;
			SequenceName  = sequenceName;
		}

		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="sequenceName">Sequence generator name.</param>
		public SequenceNameAttribute(string sequenceName)
		{
			SequenceName = sequenceName;
		}

		/// <summary>
		/// Gets or sets sequence generator name.
		/// </summary>
		public string SequenceName  { get; set; }

		/// <summary>
		/// Gets or sets sequence generator schema name.
		/// </summary>
		public string? Schema { get; set; }

		public override string GetObjectID()
		{
			return $".{Configuration}.{Schema}.{SequenceName}.";
		}
	}
}
