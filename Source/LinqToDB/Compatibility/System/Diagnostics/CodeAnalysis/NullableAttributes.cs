using System;

namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
	sealed class AllowNullAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
	sealed class DisallowNullAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
	public sealed class MaybeNullAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
	public sealed class NotNullAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class MaybeNullWhenAttribute : Attribute
	{
		/// <summary>Initializes the attribute with the specified return value condition.</summary>
		/// <param name="returnValue">The return value condition. If the method returns this value, the associated parameter may be <see langword="null" />.</param>
		public MaybeNullWhenAttribute(bool returnValue)
		{
			ReturnValue = returnValue;
		}

		/// <summary>Gets the return value condition.</summary>
		/// <returns>The return value condition. If the method returns this value, the associated parameter may be <see langword="null" />.</returns>
		public bool ReturnValue { get; }
	}

	/// <summary>Specifies that when a method returns <see cref="P:System.Diagnostics.CodeAnalysis.NotNullWhenAttribute.ReturnValue" />, the parameter will not be <see langword="null" /> even if the corresponding type allows it.</summary>
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	public sealed class NotNullWhenAttribute : Attribute
	{
		/// <summary>Initializes the attribute with the specified return value condition.</summary>
		/// <param name="returnValue">The return value condition. If the method returns this value, the associated parameter will not be <see langword="null" />.</param>
		public NotNullWhenAttribute(bool returnValue)
		{
			ReturnValue = returnValue;
		}

		/// <summary>Gets the return value condition.</summary>
		/// <returns>The return value condition. If the method returns this value, the associated parameter will not be <see langword="null" />.</returns>
		public bool ReturnValue { get; }
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
	sealed class NotNullIfNotNullAttribute : Attribute
	{
		public NotNullIfNotNullAttribute(string parameterName)
		{
			ParameterName = parameterName;
		}

		public string ParameterName { get; }
	}
}

