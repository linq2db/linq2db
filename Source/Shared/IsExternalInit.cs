﻿#if !NET5_0_OR_GREATER
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Rationale: required for init-only properties and record types
// Source: https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/Runtime/CompilerServices/IsExternalInit.cs
using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
	/// <summary>
	/// Reserved to be used by the compiler for tracking metadata.
	/// This class should not be used by developers in source code.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static class IsExternalInit
	{
	}
}
#endif
