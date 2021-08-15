namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// <see cref="CodeNamespace"/> object builder.
	/// </summary>
	public class NamespaceBuilder
	{
		public NamespaceBuilder(CodeNamespace @namespace)
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
			Namespace.Members.Add(group);
			return group;
		}
	}
}
