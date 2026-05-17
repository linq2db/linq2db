// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace LinqToDB.Internal.Infrastructure
{
	/// <summary>
	///     <para>
	///         Base class for types that support reading and writing annotations.
	///     </para>
	///     <para>
	///         This type is typically used by linq2db providers and extensions. It is generally
	///         not used in application code.
	///     </para>
	/// </summary>
	public class Annotatable : AnnotatableBase, IMutableAnnotatable
	{
		/// <summary>
		///     Throws if the object is read-only.
		/// </summary>
		protected override void EnsureMutable()
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException("The object is read-only and cannot be modified.");
			}
		}

		/// <inheritdoc />
		[DebuggerStepThrough]
		IAnnotation IMutableAnnotatable.AddAnnotation(string name, object? value)
			=> AddAnnotation(name, value);

		/// <inheritdoc />
		[DebuggerStepThrough]
		IAnnotation? IMutableAnnotatable.RemoveAnnotation(string name)
			=> RemoveAnnotation(name);

		/// <inheritdoc />
		void IMutableAnnotatable.SetOrRemoveAnnotation(string name, object? value)
			=> this[name] = value;
	}
}
