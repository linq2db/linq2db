namespace LinqToDB.Mapping
{
	/// <summary>
	/// Specifies the storage unit used by a database duration column.
	/// </summary>
	public enum DurationUnit
	{
		/// <summary>
		/// The database type carries duration semantics and its own precision, for example an SQL interval type.
		/// </summary>
		Native,

		/// <summary>100-nanosecond ticks.</summary>
		Tick,

		/// <summary>Nanoseconds.</summary>
		Nanosecond,

		/// <summary>Microseconds.</summary>
		Microsecond,

		/// <summary>Milliseconds.</summary>
		Millisecond,

		/// <summary>Seconds.</summary>
		Second,

		/// <summary>Minutes.</summary>
		Minute,

		/// <summary>Hours.</summary>
		Hour,

		/// <summary>Days.</summary>
		Day,
	}
}
