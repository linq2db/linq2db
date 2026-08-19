// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LinqToDB.Internal.Infrastructure
{
	/// <summary>
	///     Extension methods for <see cref="IReadOnlyAnnotatable" /> and <see cref="IMutableAnnotatable" />.
	/// </summary>
	public static class AnnotatableExtensions
	{
		/// <summary>
		///     Gets the annotation with the given name, throwing if it does not exist.
		/// </summary>
		/// <param name="annotatable">The object to find the annotation on.</param>
		/// <param name="annotationName">The key of the annotation to find.</param>
		/// <returns>The annotation with the specified name.</returns>
		public static IAnnotation GetAnnotation(this IReadOnlyAnnotatable annotatable, string annotationName)
		{
			ArgumentNullException.ThrowIfNull(annotatable);

			if (string.IsNullOrEmpty(annotationName))
				throw new ArgumentException("Value cannot be null or empty.", nameof(annotationName));

			var annotation = annotatable.FindAnnotation(annotationName);
			if (annotation == null)
			{
				throw new InvalidOperationException($"Annotation '{annotationName}' was not found on '{annotatable}'.");
			}

			return annotation;
		}

		/// <summary>
		///     Gets the debug string for all annotations declared on the object.
		/// </summary>
		/// <param name="annotatable">The object whose annotations should be rendered.</param>
		/// <param name="indent">The number of indent spaces to use before each new line.</param>
		/// <returns>Debug string representation of all annotations.</returns>
		public static string AnnotationsToDebugString(this IReadOnlyAnnotatable annotatable, int indent = 0)
		{
			ArgumentNullException.ThrowIfNull(annotatable);

			var annotations = annotatable.GetAnnotations().ToList();
			if (annotations.Count == 0)
			{
				return "";
			}

			var builder      = new StringBuilder();
			var indentString = new string(' ', indent);

			builder.AppendLine().Append(indentString).Append("Annotations: ");
			foreach (var annotation in annotations)
			{
				builder
					.AppendLine()
					.Append(indentString)
					.Append("  ")
					.Append(annotation.Name)
					.Append(": ")
					.Append(CultureInfo.InvariantCulture, $"{annotation.Value}");
			}

			return builder.ToString();
		}

		/// <summary>
		///     Adds annotations to an object. If <paramref name="annotatable"/> is an <see cref="AnnotatableBase"/>,
		///     routes through its <see cref="AnnotatableBase.AddAnnotations(IEnumerable{IAnnotation})"/> override
		///     so subclass customizations are respected.
		/// </summary>
		/// <param name="annotatable">The object to add annotations to.</param>
		/// <param name="annotations">The annotations to be added.</param>
		public static void AddAnnotations(this IMutableAnnotatable annotatable, IEnumerable<IAnnotation> annotations)
		{
			ArgumentNullException.ThrowIfNull(annotatable);
			ArgumentNullException.ThrowIfNull(annotations);

			if (annotatable is AnnotatableBase annotatableBase)
			{
				annotatableBase.AddAnnotations(annotations);
				return;
			}

			foreach (var annotation in annotations)
			{
				annotatable.AddAnnotation(annotation.Name, annotation.Value);
			}
		}
	}
}
