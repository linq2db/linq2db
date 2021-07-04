using System;

namespace LinqToDB.CodeGen.ContextModel
{
	public interface INameConverterProvider
	{
		Func<string, string> GetConverter(Pluralization conversion);
	}
}
