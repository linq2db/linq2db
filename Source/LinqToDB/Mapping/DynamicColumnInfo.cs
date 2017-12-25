using System;
using System.Reflection;

namespace LinqToDB.Mapping
{
#if !NETSTANDARD1_6

	/// <summary>
	/// Represents a dynamic column, which doesn't have a backing field in it's declaring type.
	/// </summary>
	/// <seealso cref="System.Reflection.MemberInfo" />
	public class DynamicColumnInfo : MemberInfo, IEquatable<DynamicColumnInfo>
	{
		/// <inheritdoc cref="MemberInfo.MemberType"/>
		public override MemberTypes MemberType => MemberTypes.Custom;
		
		/// <inheritdoc cref="MemberInfo.Name"/>
		public override string Name { get; }

		/// <inheritdoc cref="MemberInfo.DeclaringType"/>
		public override Type DeclaringType { get; }

		/// <inheritdoc cref="MemberInfo.ReflectedType"/>
		public override Type ReflectedType => DeclaringType;

		/// <inheritdoc cref="MemberInfo.GetCustomAttributes(bool)"/>
		public override object[] GetCustomAttributes(bool inherit)
			=> new object[0];

		/// <inheritdoc cref="MemberInfo.GetCustomAttributes(Type, bool)"/>
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
			=> new object[0];

		/// <inheritdoc cref="MemberInfo.IsDefined"/>
		public override bool IsDefined(Type attributeType, bool inherit)
			=> false;

		/// <summary>
		/// Gets the type of the column.
		/// </summary>
		/// <value>
		/// The type of the column.
		/// </value>
		public Type ColumnType { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DynamicColumnInfo" /> class.
		/// </summary>
		/// <param name="declaringType">Type of the declaring.</param>
		/// <param name="columnType">Type of the column.</param>
		/// <param name="memberName">Name of the member.</param>
		public DynamicColumnInfo(Type declaringType, Type columnType, string memberName)
		{
			DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
			ColumnType = columnType ?? throw new ArgumentNullException(nameof(columnType));
			Name = !string.IsNullOrEmpty(memberName) ? memberName : throw new ArgumentNullException(nameof(memberName));
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
	}
	
#endif
}
