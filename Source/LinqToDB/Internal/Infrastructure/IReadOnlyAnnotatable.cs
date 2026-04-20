// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace LinqToDB.Internal.Infrastructure
{
	/// <summary>
	///     <para>
	///         A class that supports annotations. Annotations allow for arbitrary metadata to be stored on an object.
	///     </para>
	///     <para>
	///         This interface is typically used by linq2db providers and extensions. It is generally
	///         not used in application code.
	///     </para>
	/// </summary>
	public interface IReadOnlyAnnotatable
	{
		/// <summary>
		///     Gets the value of the annotation with the given name, returning <see langword="null" /> if it does not exist.
		/// </summary>
		/// <param name="name">The name of the annotation to find.</param>
		/// <returns>
		///     The value of the existing annotation if an annotation with the specified name already exists. Otherwise, <see langword="null" />.
		/// </returns>
		object? this[string name] { get; }

		/// <summary>
		///     Gets the annotation with the given name, returning <see langword="null" /> if it does not exist.
		/// </summary>
		/// <param name="name">The name of the annotation to find.</param>
		/// <returns>
		///     The existing annotation if an annotation with the specified name already exists. Otherwise, <see langword="null" />.
		/// </returns>
		IAnnotation? FindAnnotation(string name);

		/// <summary>
		///     Gets all annotations on the current object.
		/// </summary>
		IEnumerable<IAnnotation> GetAnnotations();
	}
}
