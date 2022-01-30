using LinqToDB.Scaffold;

namespace LinqToDB
{
	/// <summary>
	/// Base class for T4 template code.
	/// </summary>
	public abstract class LinqToDBHost
	{
		/// <summary>
		/// Main entry point. We call it to execute template logic.
		/// </summary>
		/// <returns>Return type dictated by mono.t4.</returns>
		public abstract string TransformText();

		/// <summary>
		/// Gets current scaffold options. Template code could modify them and add custom customization handlers.
		/// </summary>
		internal protected ScaffoldOptions Options { get; internal set; } = null!;

		// various members we don't use, invoked by mono.t4 generated template code
		// there are more such members, but it doesn't look like they called currently (we can add them later if needed)
		public virtual void Initialize() { }
		public void Write(string _) { }
		protected string GenerationEnvironment
		{
			get => string.Empty;
			set { }
		}
	}
}
