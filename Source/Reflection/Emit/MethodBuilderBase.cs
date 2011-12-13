using System;

namespace LinqToDB.Reflection.Emit
{
	/// <summary>
	/// Base class for wrappers around methods and constructors.
	/// </summary>
	public abstract class MethodBuilderBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MethodBuilderHelper"/> class
		/// with the specified parameters.
		/// </summary>
		/// <param name="typeBuilder">Associated <see cref="TypeBuilderHelper"/>.</param>
		protected MethodBuilderBase(TypeBuilderHelper typeBuilder)
		{
			if (typeBuilder == null) throw new ArgumentNullException("typeBuilder");

			_type = typeBuilder;
		}

		private readonly TypeBuilderHelper _type;
		/// <summary>
		/// Gets associated <see cref="TypeBuilderHelper"/>.
		/// </summary>
		public           TypeBuilderHelper  Type
		{
			get { return _type; }
		}

		/// <summary>
		/// Gets <see cref="EmitHelper"/>.
		/// </summary>
		public abstract EmitHelper Emitter { get; }
	}
}
