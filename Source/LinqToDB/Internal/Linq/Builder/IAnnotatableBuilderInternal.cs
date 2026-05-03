namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Internal extension surface for fluent builders that carry an annotation bag.
	/// Provider-specific extension methods under <see cref="LinqToDB"/> reach the
	/// annotations through this interface instead of downcasting to the concrete
	/// builder implementation, keeping third-party extension authoring symmetric
	/// with in-box extensions.
	/// </summary>
	public interface IAnnotatableBuilderInternal
	{
		/// <summary>
		/// Sets an annotation on the object being built.
		/// Keys should come from a well-known names class (e.g. <see cref="SqlQuery.CteAnnotationNames"/>);
		/// values should be primitives that serialize cleanly and hash stably.
		/// </summary>
		void SetAnnotation(string name, object? value);
	}
}
