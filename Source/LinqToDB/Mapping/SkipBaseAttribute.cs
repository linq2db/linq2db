﻿using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Flags for specifying skip modifications used for Attributes based on <see cref="SkipBaseAttribute"/>.
	/// </summary>
	[Flags]
	public enum SkipModification
	{
		/// <summary> 
		/// No value should be skipped.
		/// </summary>
		None = 0x0,
		/// <summary> 
		/// A value should be skipped on insert.
		/// </summary>
		Insert = 0x1,
		/// <summary> 
		/// A value should be skipped on update.
		/// </summary>
		Update = 0x2
	}

	/// <summary>
	/// Abstract Attribute to be used for skipping values
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
	public abstract class SkipBaseAttribute : Attribute
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

		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string Configuration { get; set; }
	}
}
