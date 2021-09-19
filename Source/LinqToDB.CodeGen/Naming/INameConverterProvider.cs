using System;

namespace LinqToDB.CodeGen.Naming
{
	public interface INameConverterProvider
	{
		Func<string, string> GetConverter(Pluralization conversion);
	}
}
