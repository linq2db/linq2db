// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace LinqToDB.Internal.Infrastructure
{
	/// <summary>
	///     <para>
	///         An arbitrary piece of metadata that can be stored on an object that implements <see cref="IReadOnlyAnnotatable" />.
	///     </para>
	///     <para>
	///         This interface is typically used by linq2db providers and extensions. It is generally
	///         not used in application code.
	///     </para>
	/// </summary>
	public interface IAnnotation
	{
		/// <summary>
		///     Gets the key of this annotation.
		/// </summary>
		string Name { get; }

		/// <summary>
		///     Gets the value assigned to this annotation.
		/// </summary>
		object? Value { get; }
	}
}
