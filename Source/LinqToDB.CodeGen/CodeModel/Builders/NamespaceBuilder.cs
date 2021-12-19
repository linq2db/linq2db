namespace LinqToDB.CodeModel
{
	/// <summary>
	/// <see cref="CodeNamespace"/> object builder.
	/// </summary>
	public sealed class NamespaceBuilder
	{
		internal NamespaceBuilder(CodeNamespace @namespace)
		{
			Namespace = @namespace;
		}

		/// <summary>
		/// Current namespace AST node.
		/// </summary>
		public CodeNamespace Namespace { get; }

		/// <summary>
		/// Adds classes group to namespace.
		/// </summary>
		/// <returns>Classes group instance.</returns>
		public ClassGroup Classes()
		{
			var group = new ClassGroup(Namespace);
			Namespace.AddGroup(group);
			return group;
		}
	}
}
