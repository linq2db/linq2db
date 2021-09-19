using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	public sealed class CodeClass : TypeBase, IGroupElement
	{
		public CodeClass(
			List<CodeAttribute>? customAttributes,
			Modifiers            attributes,
			CodeXmlComment?      xmlDoc,
			IType                type,
			CodeIdentifier       name,
			CodeClass?           parent,
			CodeTypeToken?       inherits,
			List<CodeTypeToken>? implements,
			List<IMemberGroup>?  members,
			CodeTypeInitializer? typeInitializer)
			: base(customAttributes, attributes, xmlDoc, type, name)
		{
			Parent          = parent;
			Inherits        = inherits;
			Implements      = implements ?? new ();
			Members         = members ?? new ();
			TypeInitializer = typeInitializer;

			This            = new CodeThis(this);
		}

		/// <summary>
		/// Create top-level or namespace-scoped class.
		/// </summary>
		/// <param name="namespace">Optional namespace.</param>
		/// <param name="name">Class name.</param>
		public CodeClass(IReadOnlyList<CodeIdentifier>? @namespace, CodeIdentifier name)
			: this(null, default, null, new RegularType(@namespace, name, null, false, false, false), name, null, null, null, null, null)
		{
		}

		/// <summary>
		/// Create nested class.
		/// </summary>
		/// <param name="parent">Parent class.</param>
		/// <param name="name">Class name.</param>
		public CodeClass(CodeClass parent, CodeIdentifier name)
			// regular type as we don't generate generic types for now
			: this(null, default, null, new RegularType(parent.Type, name, false, false, false), name, parent, null, null, null, null)
		{
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
		public List<CodeTypeToken>  Implements      { get; }
		/// <summary>
		/// Class members (in groups).
		/// </summary>
		public List<IMemberGroup>   Members         { get; }
		/// <summary>
		/// Static constructor.
		/// </summary>
		public CodeTypeInitializer? TypeInitializer { get; set; }

		/// <summary>
		/// <c>this</c> expression.
		/// </summary>
		public CodeThis             This            { get; }

		public override CodeElementType ElementType => CodeElementType.Class;
	}
}
