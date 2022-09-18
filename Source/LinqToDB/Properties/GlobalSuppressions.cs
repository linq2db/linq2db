using System.Diagnostics.CodeAnalysis;

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER || NET5_0_OR_GREATER

[assembly: SuppressMessage(
  "Usage", "CA2201:Exception type System.Exception is not sufficiently specific",
  Justification = "Source code imported from 3rd party package Nullability.Source",
  Scope = "type", 
  Target = "T:System.Reflection.NullabilityInfoExtensions")]

#endif
