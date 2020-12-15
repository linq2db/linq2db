using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace LinqToDB
{
	[Generator]
	public class ExpressionMethodGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			// do nothing for now.
			// TODO: Add SyntaxReceiver support
		}

		public void Execute(GeneratorExecutionContext context)
		{
			// r/restofthef***ingowl
			context.AddSource("myGeneratedFile.cs", SourceText.From(@"
namespace GeneratedNamespace
{
    public class GeneratedClass
    {
        public static void GeneratedMethod()
        {
            // generated code
        }
    }
}", Encoding.UTF8));
		}
	}
}
