using System.Text;

namespace LinqToDB
{
	/// <summary>
	/// Base class for T4 template code.
	/// </summary>
#pragma warning disable CA1515 // Consider making public types internal
	public abstract class LinqToDBHost
#pragma warning restore CA1515 // Consider making public types internal
	{
		/// <summary>
		/// Main entry point. We call it to execute template logic.
		/// </summary>
		/// <returns>Return type dictated by mono.t4.</returns>
		public abstract string TransformText();

		// various members we don't use, invoked by mono.t4 generated template code
		// there are more such members, but it doesn't look like they called currently (we can add them later if needed)
		public virtual void Initialize() { }
		public void Write(string code) => _code.Append(code);

		private readonly StringBuilder _code = new();

		protected string GenerationEnvironment
		{
			get => _code.ToString();
			set { }
		}
	}
}
