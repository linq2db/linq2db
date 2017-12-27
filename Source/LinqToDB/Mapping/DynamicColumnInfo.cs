using System;
using System.Globalization;
using System.Reflection;

namespace LinqToDB.Mapping
{
#if !NETSTANDARD1_6

	/// <summary>
	/// Represents a dynamic column, which doesn't have a backing field in it's declaring type.
	/// </summary>
	/// <seealso cref="System.Reflection.MemberInfo" />
	public class DynamicColumnInfo : PropertyInfo, IEquatable<DynamicColumnInfo>
	{
		private static readonly MethodInfo _dummyGetter = typeof(DynamicColumnInfo).GetMethod(nameof(DummyGetter), BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly MethodInfo _dummySetter = typeof(DynamicColumnInfo).GetMethod(nameof(DummySetter), BindingFlags.Instance | BindingFlags.NonPublic);
		private readonly MethodInfo _typedDummyGetter;
		private readonly MethodInfo _typedDummySetter;

		/// <inheritdoc cref="MemberInfo.Name"/>
		public override string Name { get; }

		/// <inheritdoc cref="MemberInfo.DeclaringType"/>
		public override Type DeclaringType { get; }

		/// <inheritdoc cref="MemberInfo.ReflectedType"/>
		public override Type ReflectedType => DeclaringType;

		/// <inheritdoc cref="PropertyInfo.PropertyType"/>
		public override Type PropertyType { get; }

		/// <inheritdoc cref="PropertyInfo.Attributes"/>
		public override PropertyAttributes Attributes => PropertyAttributes.None;

		/// <inheritdoc cref="PropertyInfo.CanRead"/>
		public override bool CanRead => true;

		/// <inheritdoc cref="PropertyInfo.CanWrite"/>
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
			PropertyType = columnType ?? throw new ArgumentNullException(nameof(columnType));
			Name = !string.IsNullOrEmpty(memberName) ? memberName : throw new ArgumentNullException(nameof(memberName));

			_typedDummyGetter = _dummyGetter.MakeGenericMethod(declaringType);
			_typedDummySetter = _dummySetter.MakeGenericMethod(declaringType);
		}

		/// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
		public bool Equals(DynamicColumnInfo other)
		{
			if (other == null)
				return false;

			return ReferenceEquals(this, other) || Name.Equals(other.Name, StringComparison.Ordinal) && other.DeclaringType == DeclaringType;
		}

		/// <inheritdoc cref="object.Equals(object)"/>
		public override bool Equals(object obj)
		{
			if (obj is DynamicColumnInfo dynamicColumnInfo)
				return Equals(dynamicColumnInfo);

			return false;
		}

		/// <inheritdoc cref="object.GetHashCode"/>
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
		public static bool operator ==(DynamicColumnInfo a, DynamicColumnInfo b)
			=> a?.Equals(b) ?? ReferenceEquals(b, null);

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="a">a.</param>
		/// <param name="b">The b.</param>
		/// <returns>
		/// The result of the operator.
		/// </returns>
		public static bool operator !=(DynamicColumnInfo a, DynamicColumnInfo b)
			=> !a?.Equals(b) ?? !ReferenceEquals(b, null);

		/// <inheritdoc cref="MemberInfo.GetCustomAttributes(bool)"/>
		public override object[] GetCustomAttributes(bool inherit)
			=> new object[0];

		/// <inheritdoc cref="MemberInfo.GetCustomAttributes(Type, bool)"/>
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
			=> new object[0];

		/// <inheritdoc cref="MemberInfo.IsDefined"/>
		public override bool IsDefined(Type attributeType, bool inherit)
			=> false;

		/// <inheritdoc cref="PropertyInfo.SetValue(Object, Object, BindingFlags, Binder, object[], CultureInfo)"/>
		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
			=> throw new InvalidOperationException("SetValue on dynamic column is not to be called.");

		/// <inheritdoc cref="PropertyInfo.GetAccessors(bool)"/>
		public override MethodInfo[] GetAccessors(bool nonPublic)
			=> new[] {_typedDummyGetter, _typedDummyGetter};

		/// <inheritdoc cref="PropertyInfo.GetGetMethod(bool)"/>
		public override MethodInfo GetGetMethod(bool nonPublic)
			// we're returning dummy method, so the rest of the stack can inspect it and correctly build expressions
			=> _typedDummyGetter;

		/// <inheritdoc cref="PropertyInfo.GetSetMethod(bool)"/>
		public override MethodInfo GetSetMethod(bool nonPublic)
			// we're returning dummy method, so the rest of the stack can inspect it and correctly build expressions
			=> _typedDummySetter;

		/// <inheritdoc cref="PropertyInfo.GetIndexParameters"/>
		public override ParameterInfo[] GetIndexParameters()
			=> new ParameterInfo[0];

		/// <inheritdoc cref="PropertyInfo.GetValue(object, BindingFlags, Binder, object[], CultureInfo)"/>
		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
			=> throw new InvalidOperationException("SetValue on dynamic column is not to be called.");
		
		private T DummyGetter<T>()
			=> throw new InvalidOperationException("Dynamic column getter is not to be called.");

		private void DummySetter<T>(T value)
			=> throw new InvalidOperationException("Dynamic column setter is not to be called.");
	}
	
#endif
}
