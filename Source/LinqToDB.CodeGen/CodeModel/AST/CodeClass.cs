using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	public class CodeClass : TypeBase, IGroupElement
	{
		/// <summary>
		/// Create top-level or namespace-scoped class.
		/// </summary>
		/// <param name="namespace">Optional namespace.</param>
		/// <param name="name">Class name.</param>
		public CodeClass(CodeIdentifier[]? @namespace, CodeIdentifier name)
		{
			Name = name;
			Type = new RegularType(@namespace, name, null, false, false, false);
		}

		/// <summary>
		/// Create nested class.
		/// </summary>
		/// <param name="parent">Parent class.</param>
		/// <param name="name">Class name.</param>
		public CodeClass(CodeClass parent, CodeIdentifier name)
		{
			Name   = name;
			Parent = parent;
			// regular type as we don't generate generic types for now
			Type   = new RegularType(parent.Type, name, false, false, false);
		}

		/// <summary>
		/// Parent class.
		/// </summary>
		public CodeClass?           Parent          { get; }
		/// <summary>
		/// Base class.
		/// </summary>
		public CodeTypeToken?       Inherits        { get; set; }
		/// <summary>
		/// Implemented interfaces.
		/// </summary>
		public List<CodeTypeToken>  Implements      { get; set; } = new();
		/// <summary>
		/// Class members (in groups).
		/// </summary>
		public List<IMemberGroup>   Members         { get; set; } = new();
		/// <summary>
		/// Static constructor.
		/// </summary>
		public CodeTypeInitializer? TypeInitializer { get; set; }

		public override CodeElementType ElementType => CodeElementType.Class;
	}
}
