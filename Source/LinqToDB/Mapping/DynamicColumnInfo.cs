﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using LinqToDB.Common;

namespace LinqToDB.Mapping
{
	/// <inheritdoc />
	/// <summary>
	/// Represents a dynamic column, which doesn't have a backing field in it's declaring type.
	/// </summary>
	public class DynamicColumnInfo : PropertyInfo, IEquatable<DynamicColumnInfo>
	{
		private static readonly MethodInfo _dummyGetter = typeof(DynamicColumnInfo).GetMethod(nameof(DummyGetter), BindingFlags.Instance | BindingFlags.NonPublic)!;
		private static readonly MethodInfo _dummySetter = typeof(DynamicColumnInfo).GetMethod(nameof(DummySetter), BindingFlags.Instance | BindingFlags.NonPublic)!;
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
		/// Initializes a new instance of the <see cref="DynamicColumnInfo" /> class.
		/// </summary>
		/// <param name="declaringType">Type of the declaring.</param>
		/// <param name="columnType">Type of the column.</param>
		/// <param name="memberName">Name of the member.</param>
		public DynamicColumnInfo(Type declaringType, Type columnType, string memberName)
		{
			DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
			PropertyType  = columnType    ?? throw new ArgumentNullException(nameof(columnType));

			Name = !string.IsNullOrEmpty(memberName) ? memberName : throw new ArgumentNullException(nameof(memberName));

			_typedDummyGetter = _dummyGetter.MakeGenericMethod(declaringType);
			_typedDummySetter = _dummySetter.MakeGenericMethod(declaringType);
		}

		public bool Equals(DynamicColumnInfo? other)
		{
			if (other == null)
				return false;

			return ReferenceEquals(this, other) || Name.Equals(other.Name, StringComparison.Ordinal) && other.DeclaringType == DeclaringType;
		}

		public override bool Equals(object? obj)
		{
			if (obj is DynamicColumnInfo dynamicColumnInfo)
				return Equals(dynamicColumnInfo);

			return false;
		}

		public override int GetHashCode()
			=> unchecked ((Name.GetHashCode() * 397) ^ DeclaringType.GetHashCode());

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="a">a.</param>
		/// <param name="b">The b.</param>
		/// <returns>
		/// The result of the operator.
		/// </returns>
		public static bool operator ==(DynamicColumnInfo? a, DynamicColumnInfo? b)
			=> a?.Equals(b) ?? ReferenceEquals(b, null);

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="a">a.</param>
		/// <param name="b">The b.</param>
		/// <returns>
		/// The result of the operator.
		/// </returns>
		public static bool operator !=(DynamicColumnInfo? a, DynamicColumnInfo? b)
			=> !a?.Equals(b) ?? !ReferenceEquals(b, null);

#pragma warning disable RS0030 // Do not used banned APIs
		public override object[] GetCustomAttributes(bool inherit)
#pragma warning restore RS0030 // Do not used banned APIs
			=> Array<object>.Empty;

#pragma warning disable RS0030 // Do not used banned APIs
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
#pragma warning restore RS0030 // Do not used banned APIs
			=> Array<Attribute>.Empty;

		public override IList<CustomAttributeData> GetCustomAttributesData()
			=> Array<CustomAttributeData>.Empty;

		public override bool IsDefined(Type attributeType, bool inherit)
			=> false;

		public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
			=> throw new InvalidOperationException("SetValue on dynamic column is not to be called.");

		public override MethodInfo[] GetAccessors(bool nonPublic)
			=> new[] {_typedDummyGetter, _typedDummyGetter};

		public override MethodInfo GetGetMethod(bool nonPublic)
			// we're returning dummy method, so the rest of the stack can inspect it and correctly build expressions
			=> _typedDummyGetter;

		public override MethodInfo GetSetMethod(bool nonPublic)
			// we're returning dummy method, so the rest of the stack can inspect it and correctly build expressions
			=> _typedDummySetter;

		public override ParameterInfo[] GetIndexParameters()
			=> Array<ParameterInfo>.Empty;

		public override object GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
			=> throw new InvalidOperationException("SetValue on dynamic column is not to be called.");
		
		private T DummyGetter<T>()
			=> throw new InvalidOperationException("Dynamic column getter is not to be called.");

		private void DummySetter<T>(T value)
			=> throw new InvalidOperationException("Dynamic column setter is not to be called.");
	}
}
