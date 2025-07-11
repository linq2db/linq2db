using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace LinqToDB.Internal.Mapping
{
	/// <inheritdoc />
	/// <summary>
	/// Represents a dynamic column, which doesn't have a backing field in it's declaring type.
	/// </summary>
	public class SpecialPropertyInfo : VirtualPropertyInfoBase, IEquatable<SpecialPropertyInfo>
	{
		private static readonly MethodInfo _dummyGetter = typeof(SpecialPropertyInfo).GetMethod(nameof(DummyGetter), BindingFlags.Instance | BindingFlags.NonPublic)!;
		private static readonly MethodInfo _dummySetter = typeof(SpecialPropertyInfo).GetMethod(nameof(DummySetter), BindingFlags.Instance | BindingFlags.NonPublic)!;
		private readonly MethodInfo _typedDummyGetter;
		private readonly MethodInfo _typedDummySetter;

		public override string Name { get; }

		public override Type DeclaringType { get; }

		public override Type ReflectedType => DeclaringType;

		public override Type PropertyType { get; }

		public override PropertyAttributes Attributes => PropertyAttributes.None;

		public override bool CanRead => true;

		public override bool CanWrite => true;

		/// <summary>
		/// Initializes a new instance of the <see cref="SpecialPropertyInfo" /> class.
		/// </summary>
		/// <param name="declaringType">Type of the declaring.</param>
		/// <param name="columnType">Type of the column.</param>
		/// <param name="memberName">Name of the member.</param>
		public SpecialPropertyInfo(Type declaringType, Type columnType, string memberName)
		{
			DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
			PropertyType  = columnType    ?? throw new ArgumentNullException(nameof(columnType));

			Name = !string.IsNullOrEmpty(memberName) ? memberName : throw new ArgumentNullException(nameof(memberName));

			_typedDummyGetter = _dummyGetter.MakeGenericMethod(declaringType);
			_typedDummySetter = _dummySetter.MakeGenericMethod(declaringType);
		}

		public bool Equals(SpecialPropertyInfo? other)
		{
			if (other == null)
				return false;

			return ReferenceEquals(this, other) || Name.Equals(other.Name, StringComparison.Ordinal) && other.DeclaringType == DeclaringType;
		}

		public override bool Equals(object? obj)
		{
			if (obj is SpecialPropertyInfo dynamicColumnInfo)
				return Equals(dynamicColumnInfo);

			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, DeclaringType);
		}

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="a">a.</param>
		/// <param name="b">The b.</param>
		/// <returns>
		/// The result of the operator.
		/// </returns>
		public static bool operator ==(SpecialPropertyInfo? a, SpecialPropertyInfo? b)
			=> a?.Equals(b) ?? ReferenceEquals(b, null);

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="a">a.</param>
		/// <param name="b">The b.</param>
		/// <returns>
		/// The result of the operator.
		/// </returns>
		public static bool operator !=(SpecialPropertyInfo? a, SpecialPropertyInfo? b)
			=> !a?.Equals(b) ?? !ReferenceEquals(b, null);

#pragma warning disable RS0030 // Do not used banned APIs
		public override object[] GetCustomAttributes(bool inherit) => [];
#pragma warning restore RS0030 // Do not used banned APIs

#pragma warning disable RS0030 // Do not used banned APIs
		// must return Attibute[] as some runtimes doesn't follow their own contract
		public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<Attribute>();
#pragma warning restore RS0030 // Do not used banned APIs

		public override IList<CustomAttributeData> GetCustomAttributesData() => [];

		public override bool IsDefined(Type attributeType, bool inherit)
			=> false;

		public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
			=> throw new InvalidOperationException("SetValue on special column is not to be called.");

		public override MethodInfo[] GetAccessors(bool nonPublic)
			=> new[] {_typedDummyGetter, _typedDummyGetter};

		public override MethodInfo GetGetMethod(bool nonPublic)
			// we're returning dummy method, so the rest of the stack can inspect it and correctly build expressions
			=> _typedDummyGetter;

		public override MethodInfo GetSetMethod(bool nonPublic)
			// we're returning dummy method, so the rest of the stack can inspect it and correctly build expressions
			=> _typedDummySetter;

		public override ParameterInfo[] GetIndexParameters() => [];

		public override object GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
			=> throw new InvalidOperationException("SetValue on special column is not to be called.");

		private T DummyGetter<T>()
			=> throw new InvalidOperationException("Special column getter is not to be called.");

		private void DummySetter<T>(T value)
			=> throw new InvalidOperationException("Special column setter is not to be called.");
	}
}
