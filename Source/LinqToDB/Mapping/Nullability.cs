using System;
using System.Reflection;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Internal API.
	/// </summary>
	public static class Nullability
	{
		private static Lazy<bool> _isSupported = new(() => 
		{
			try
			{
				var fieldInfo = typeof(Nullability).GetField(nameof(_isSupported), BindingFlags.NonPublic | BindingFlags.Static)!;
				var context  = new NullabilityInfoContext();
				// Create() throws InvalidOperationException if feature flag NullabilityInfoContextSupport is false.
				// This happens when build is configured to aggressively trim C# nullability attributes,
				// which is the default for MAUI and Blazor targets.
				// Linq2db still works and users have two choices:
				// 1. Rely on good old [Column] or [NotNullable] attributes instead;
				// 2. Prevent aggressive trimming by adding the following property to csproj:
				//    <NullabilityInfoContextSupport>true</NullabilityInfoContextSupport>
				context.Create(fieldInfo);
				return true;
			}
			catch (InvalidOperationException)
			{ /* return false below */ }
			return false;
		});

		internal static void EnsureSupport()
		{
			if (!_isSupported.Value)
			{
				throw new NotSupportedException(
					"Nullable metadata is not available. " +
					"Check that you run on .NET Standard 2.0 or greater, " + 
					"and that you don't trim nullable metadata during build (see NullabilityInfoContextSupport).");
			}
		}

		public static bool TryAnalyzeMember(MemberInfo member, out bool isNullable)
		{
			// Extract info from C# Nullable Reference Types if available.
			// Note that this also handles Nullable Value Types.
			var context = new NullabilityInfoContext();
			var nullability = member switch 
			{
				PropertyInfo p => context.Create(p).ReadState,
				FieldInfo    f => context.Create(f).ReadState,
				MethodInfo   m => context.Create(m.ReturnParameter).ReadState,
				_ => NullabilityState.Unknown,
			};
			
			isNullable = nullability == NullabilityState.Nullable;
			return nullability != NullabilityState.Unknown;
		}
	}
}
