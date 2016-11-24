using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LinqToDB.Extensions;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Tests.NetCore
{
	[TestFixture]
    public class ReflectionTests
    {
		/// <summary>
		/// https://github.com/dotnet/corefx/issues/12921
		/// </summary>
		[Test]
	    public void GetRuntimeMethodTest()
	    {
		    var type = typeof(Int64?);
		    var paremeterType = typeof(int);

		    var method = type.GetRuntimeMethod("op_Implicit", new[] {paremeterType});

		    Assert.AreEqual(paremeterType, method.GetParameters()[0].ParameterType);
	    }

	    [Test]
	    public void GetRuntimeMethodTest2()
	    {
		    var type = typeof(Int64?);
		    var paremeterType = typeof(int);

		    var method = type.GetMethodEx("op_Implicit", new[] {paremeterType});

		    if (method != null)
			    Assert.AreEqual(paremeterType, method.GetParameters()[0].ParameterType);
	    }
    }
}
