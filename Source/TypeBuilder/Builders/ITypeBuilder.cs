using System;

using LinqToDB.Reflection.Emit;

namespace LinqToDB.TypeBuilder.Builders
{
	public interface ITypeBuilder
	{
		string AssemblyNameSuffix { get; }
		Type   Build          (AssemblyBuilderHelper assemblyBuilder);
		string GetTypeName    ();
	}
}
