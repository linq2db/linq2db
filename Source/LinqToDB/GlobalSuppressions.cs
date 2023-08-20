// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// temporary workaround https://github.com/SimonCropp/NullabilityInfo/issues/52
#pragma warning disable IDE0076
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>", Scope = "member", Target = "~M:System.Reflection.NullabilityInfoContext.TryPopulateNullabilityInfo(System.Reflection.NullabilityInfo,System.Reflection.NullabilityInfoContext.NullableAttributeStateParser,System.Int32@)~System.Boolean")]
