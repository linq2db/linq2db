// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
	public class AnnotatableBase : IReadOnlyAnnotatable
	{
		private Dictionary<string, Annotation>? _annotations;

		/// <summary>
		///     Indicates whether the current object is read-only.
		/// </summary>
		/// <remarks>
		///     Annotations cannot be changed when the object is read-only.
		/// </remarks>
		public virtual bool IsReadOnly
			=> false;

		/// <summary>
		///     Throws if the model is read-only.
		/// </summary>
		protected virtual void EnsureMutable()
		{
			if (IsReadOnly)
				throw new InvalidOperationException("The object is read-only.");
		}

		/// <summary>
		///     Gets all annotations on the current object.
		/// </summary>
		public virtual IEnumerable<Annotation> GetAnnotations()
			=> _annotations?.Values.OrderBy(a => a.Name, StringComparer.Ordinal) ?? Enumerable.Empty<Annotation>();

		/// <summary>
		///     Adds an annotation to this object. Throws if an annotation with the specified name already exists.
		/// </summary>
		/// <param name="name">The key of the annotation to be added.</param>
		/// <param name="value">The value to be stored in the annotation.</param>
		/// <returns>The newly added annotation.</returns>
		public virtual Annotation AddAnnotation(string name, object? value)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("Value cannot be null or empty.", nameof(name));

			var annotation = CreateAnnotation(name, value);

			return AddAnnotation(name, annotation);
		}

		/// <summary>
		///     Adds an annotation to this object. Throws if an annotation with the specified name already exists.
		/// </summary>
		/// <param name="name">The key of the annotation to be added.</param>
		/// <param name="annotation">The annotation to be added.</param>
		/// <returns>The added annotation.</returns>
		protected virtual Annotation AddAnnotation(string name, Annotation annotation)
		{
			if (FindAnnotation(name) != null)
			{
				throw new InvalidOperationException($"Annotation '{name}' already exists on '{ToString()}'.");
			}

			SetAnnotation(name, annotation, oldAnnotation: null);

			return annotation;
		}

		/// <summary>
		///     Adds annotations to this object.
		/// </summary>
		/// <param name="annotations">The annotations to be added.</param>
		public virtual void AddAnnotations(IEnumerable<IAnnotation> annotations)
		{
			ArgumentNullException.ThrowIfNull(annotations);

			foreach (var annotation in annotations)
			{
				AddAnnotation(annotation.Name, annotation.Value);
			}
		}

		/// <summary>
		///     Adds annotations to this object.
		/// </summary>
		/// <param name="annotations">The annotations to be added.</param>
		public virtual void AddAnnotations(IReadOnlyDictionary<string, object?> annotations)
		{
			ArgumentNullException.ThrowIfNull(annotations);

			foreach (var kvp in annotations)
			{
				AddAnnotation(kvp.Key, kvp.Value);
			}
		}

		/// <summary>
		///     Sets the annotation stored under the given key. Overwrites the existing annotation if an
		///     annotation with the specified name already exists.
		/// </summary>
		/// <param name="name">The key of the annotation to be added.</param>
		/// <param name="value">The value to be stored in the annotation.</param>
		public virtual void SetAnnotation(string name, object? value)
		{
			var oldAnnotation = FindAnnotation(name);
			if (oldAnnotation != null
				&& Equals(oldAnnotation.Value, value))
			{
				return;
			}

			SetAnnotation(name, CreateAnnotation(name, value), oldAnnotation);
		}

		/// <summary>
		///     Sets the annotation stored under the given key. Overwrites the existing annotation if an
		///     annotation with the specified name already exists.
		/// </summary>
		/// <param name="name">The key of the annotation to be added.</param>
		/// <param name="annotation">The annotation to be set.</param>
		/// <param name="oldAnnotation">The annotation being replaced.</param>
		/// <returns>The annotation that was set.</returns>
		protected virtual Annotation? SetAnnotation(
			string      name,
			Annotation  annotation,
			Annotation? oldAnnotation)
		{
			EnsureMutable();

			_annotations ??= new Dictionary<string, Annotation>(StringComparer.Ordinal);
			_annotations[name] = annotation;

			return OnAnnotationSet(name, annotation, oldAnnotation);
		}

		/// <summary>
		///     Called when an annotation was set or removed.
		/// </summary>
		/// <param name="name">The key of the set annotation.</param>
		/// <param name="annotation">The annotation set.</param>
		/// <param name="oldAnnotation">The old annotation.</param>
		/// <returns>The annotation that was set.</returns>
		protected virtual Annotation? OnAnnotationSet(
			string      name,
			Annotation? annotation,
			Annotation? oldAnnotation)
			=> annotation;

		/// <summary>
		///     Gets the annotation with the given name, returning <see langword="null" /> if it does not exist.
		/// </summary>
		/// <param name="name">The key of the annotation to find.</param>
		/// <returns>
		///     The existing annotation if an annotation with the specified name already exists. Otherwise, <see langword="null" />.
		/// </returns>
		public virtual Annotation? FindAnnotation(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("Value cannot be null or empty.", nameof(name));

			return _annotations != null && _annotations.TryGetValue(name, out var annotation) ? annotation : null;
		}

		/// <summary>
		///     Gets the annotation with the given name, throwing if it does not exist.
		/// </summary>
		/// <param name="annotationName">The key of the annotation to find.</param>
		/// <returns>The annotation with the specified name.</returns>
		public virtual Annotation GetAnnotation(string annotationName)
			=> (Annotation)GetAnnotation(this, annotationName);

		internal static IAnnotation GetAnnotation(IReadOnlyAnnotatable annotatable, string annotationName)
		{
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
		///     Removes the given annotation from this object.
		/// </summary>
		/// <param name="name">The annotation to remove.</param>
		/// <returns>The annotation that was removed.</returns>
		public virtual Annotation? RemoveAnnotation(string name)
		{
			ArgumentNullException.ThrowIfNull(name);
			EnsureMutable();

			var annotation = FindAnnotation(name);
			if (annotation == null)
			{
				return null;
			}

			_annotations!.Remove(name);

			if (_annotations.Count == 0)
			{
				_annotations = null;
			}

			OnAnnotationSet(name, null, annotation);

			return annotation;
		}

		/// <summary>
		///     Gets the value annotation with the given name, returning <see langword="null" /> if it does not exist.
		/// </summary>
		/// <param name="name">The key of the annotation to find.</param>
		/// <returns>
		///     The value of the existing annotation if an annotation with the specified name already exists.
		///     Otherwise, <see langword="null" />.
		/// </returns>
		public virtual object? this[string name]
		{
			get => FindAnnotation(name)?.Value;

			set
			{
				if (string.IsNullOrEmpty(name))
					throw new ArgumentException("Value cannot be null or empty.", nameof(name));

				if (value == null)
				{
					RemoveAnnotation(name);
				}
				else
				{
					SetAnnotation(name, value);
				}
			}
		}

		/// <summary>
		///     Creates a new annotation.
		/// </summary>
		/// <param name="name">The key of the annotation.</param>
		/// <param name="value">The value to be stored in the annotation.</param>
		/// <returns>The newly created annotation.</returns>
		protected virtual Annotation CreateAnnotation(string name, object? value)
			=> new(name, value);

		/// <inheritdoc />
		object? IReadOnlyAnnotatable.this[string name]
		{
			[DebuggerStepThrough]
			get => this[name];
		}

		/// <inheritdoc />
		[DebuggerStepThrough]
		IEnumerable<IAnnotation> IReadOnlyAnnotatable.GetAnnotations()
			=> GetAnnotations();

		/// <inheritdoc />
		[DebuggerStepThrough]
		IAnnotation? IReadOnlyAnnotatable.FindAnnotation(string name)
			=> FindAnnotation(name);
	}
}
