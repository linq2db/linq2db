using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	public sealed class CodeClass : TypeBase, IGroupElement
	{
		private readonly List<CodeTypeToken> _implements;
		private readonly List<IMemberGroup>  _members;

		public CodeClass(
			IEnumerable<CodeAttribute>? customAttributes,
			Modifiers                   attributes,
			CodeXmlComment?             xmlDoc,
			IType                       type,
			CodeIdentifier              name,
			CodeClass?                  parent,
			CodeTypeToken?              inherits,
			IEnumerable<CodeTypeToken>? implements,
			IEnumerable<IMemberGroup>?  members,
			CodeTypeInitializer?        typeInitializer)
			: base(customAttributes, attributes, xmlDoc, type, name)
		{
			Parent          = parent;
			Inherits        = inherits;
			_implements     = [.. implements ?? []];
			_members        = [.. members    ?? []];
			TypeInitializer = typeInitializer;
			This            = new CodeThis(this);
		}

		/// <summary>
		/// Create top-level or namespace-scoped class.
		/// </summary>
		/// <param name="namespace">Optional namespace.</param>
		/// <param name="name">Class name.</param>
		public CodeClass(IReadOnlyList<CodeIdentifier>? @namespace, CodeIdentifier name)
			: this(null, default, null, new RegularType(@namespace, name, false, false, false), name, null, null, null, null, null)
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
		public CodeClass?                   Parent          { get; }
		/// <summary>
		/// Base class.
		/// </summary>
		public CodeTypeToken?               Inherits        { get; internal set; }
		/// <summary>
		/// Implemented interfaces.
		/// </summary>
		public IReadOnlyList<CodeTypeToken> Implements      => _implements;
		/// <summary>
		/// Class members (in groups).
		/// </summary>
		public IReadOnlyList<IMemberGroup>  Members         => _members;
		/// <summary>
		/// Static constructor.
		/// </summary>
		public CodeTypeInitializer?         TypeInitializer { get; internal set; }

		/// <summary>
		/// <c>this</c> expression.
		/// </summary>
		public CodeThis                     This            { get; }

		public override CodeElementType ElementType => CodeElementType.Class;

		internal void AddInterface(CodeTypeToken interfaceType)
		{
			_implements.Add(interfaceType);
		}

		internal void AddMemberGroup(IMemberGroup memberGroup)
		{
			_members.Add(memberGroup);
		}
	}
}
