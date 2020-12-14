using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// <para>
	/// When applied to method, tells linq2db to create an <see cref="ExpressionMethodAttribute"/> method
	/// version of the function. This is only supported in C# 9 or later, using Source Generators.
	/// </para>
	/// 
	/// <para>
	/// Requirements:
	/// </para>
	/// 
	/// <list type="bullet">
	/// 
	/// <item>
	/// <term><c>class</c></term>
	/// <description>The containing class must be marked <c>partial</c>, so that the generator can add new methods to it.</description>
	/// </item> 
	/// <item>
	/// <term><c>method</c></term>
	/// <description>
	/// <list type="bullet">
	/// <item>Must have exactly one parameter.</item>
	/// <item>Must not have <c>void</c> return type.</item>
	/// <item>Must have a single statement returning a mapped object.</item>
	/// </list>
	/// </description>
	/// </item>
	/// </list>
	/// </summary>
	/// <remarks>
	/// See <see href="https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/"/> for additional information.
	/// </remarks>
	[PublicAPI]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class GenerateExpressionMethodAttribute : Attribute { }
}
