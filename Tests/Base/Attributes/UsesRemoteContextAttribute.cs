using System;

namespace Tests
{
	/// <summary>
	/// Marks a test that builds a remote (LinqService) context from a *non-remote* parameter value, by
	/// appending <c>TestBase.LinqServiceSuffix</c> inside the test body. The lane classifier only sees the
	/// parameter value, so such a test would otherwise be treated as non-remote and run without the
	/// process-wide secondary mutex - while still writing the shared LinqService host state that mutex
	/// protects. <see cref="DatabaseLaneStrategy"/> honours this attribute by requiring the mutex anyway.
	/// </summary>
	/// <remarks>
	/// Prefer a remote parameter value (<c>[DataSources(true)]</c> and friends) over this attribute. It
	/// exists for tests that need both the direct and the remote context for the same parameter.
	/// Forgetting it is not silent: <c>ServerContainerBase.CreateContext</c> asserts that every remote
	/// context is created by a test the classifier can see as remote.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public sealed class UsesRemoteContextAttribute : Attribute
	{
	}
}
