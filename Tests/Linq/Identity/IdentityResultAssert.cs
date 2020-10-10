// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Tests.Identity
{
	public static class IdentityResultAssert
	{
		public static void IsSuccess(IdentityResult result)
		{
			Assert.NotNull(result);
			Assert.True(result.Succeeded);
		}

		public static void IsFailure(IdentityResult result)
		{
			Assert.NotNull(result);
			Assert.False(result.Succeeded);
		}

		public static void IsFailure(IdentityResult result, string error)
		{
			Assert.NotNull(result);
			Assert.False(result.Succeeded);
			Assert.AreEqual(error, result.Errors.First().Description);
		}

		public static void IsFailure(IdentityResult result, IdentityError error)
		{
			Assert.NotNull(result);
			Assert.False(result.Succeeded);
			Assert.AreEqual(error.Description, result.Errors.First().Description);
			Assert.AreEqual(error.Code, result.Errors.First().Code);
		}

		public static void VerifyLogMessage(ILogger logger, string expectedLog)
		{
			if (logger is ITestLogger testlogger)
				Assert.Contains(expectedLog, (ICollection)testlogger.LogMessages);
			else
				Assert.False(true, "No logger registered");
		}
	}
}
