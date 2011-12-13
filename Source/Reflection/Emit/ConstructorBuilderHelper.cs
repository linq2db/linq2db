using System;
using System.Reflection.Emit;

namespace LinqToDB.Reflection.Emit
{
	/// <summary>
	/// A wrapper around the <see cref="ConstructorBuilder"/> class.
	/// </summary>
	public class ConstructorBuilderHelper : MethodBuilderBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ConstructorBuilder"/> class
		/// with the specified parameters.
		/// </summary>
		/// <param name="typeBuilder">Associated <see cref="TypeBuilderHelper"/>.</param>
		/// <param name="constructorBuilder">A <see cref="ConstructorBuilder"/></param>
		public ConstructorBuilderHelper(TypeBuilderHelper typeBuilder, ConstructorBuilder constructorBuilder)
			: base(typeBuilder)
		{
			if (constructorBuilder == null) throw new ArgumentNullException("constructorBuilder");

			_constructorBuilder = constructorBuilder;
			_constructorBuilder.SetCustomAttribute(Type.Assembly.LinqToDBAttribute);
		}

		private readonly ConstructorBuilder _constructorBuilder;
		/// <summary>
		/// Gets ConstructorBuilder.
		/// </summary>
		public           ConstructorBuilder  ConstructorBuilder
		{
			get { return _constructorBuilder; }
		}

		/// <summary>
		/// Converts the supplied <see cref="ConstructorBuilderHelper"/> to a <see cref="MethodBuilder"/>.
		/// </summary>
		/// <param name="constructorBuilder">The <see cref="ConstructorBuilder"/>.</param>
		/// <returns>A <see cref="ConstructorBuilder"/>.</returns>
		public static implicit operator ConstructorBuilder(ConstructorBuilderHelper constructorBuilder)
		{
			if (constructorBuilder == null) throw new ArgumentNullException("constructorBuilder");

			return constructorBuilder.ConstructorBuilder;
		}

		private EmitHelper _emitter;
		/// <summary>
		/// Gets <see cref="EmitHelper"/>.
		/// </summary>
		public override EmitHelper Emitter
		{
			get
			{
				if (_emitter == null)
					_emitter = new EmitHelper(this, _constructorBuilder.GetILGenerator());

				return _emitter;
			}
		}
	}
}
