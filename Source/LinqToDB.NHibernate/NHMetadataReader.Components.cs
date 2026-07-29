using System;
using System.Collections.Generic;
using System.Globalization;

using LinqToDB.Mapping;

using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.Type;

namespace LinqToDB.NHibernate
{
	// Maps an NHibernate <component> — a value object whose properties are columns of the owning entity's own
	// table — by describing each sub-property as a column of the entity, named relative to the component member.
	// linq2db combines that with the member it was asked about, giving it the flattened "Component.Property" path
	// it uses for nested members.
	partial class NHMetadataReader
	{
		/// <summary>
		/// Builds one <see cref="ColumnAttribute"/> per single-column sub-property of a component. A sub-property
		/// that is itself an association or spans several columns (a nested component) has no single column to map,
		/// and is refused rather than dropped — leaving it out would read as null and write nothing.
		/// </summary>
		T[] BuildComponentColumns<T>(ComponentType componentType, PropInfo prop, Type owner) where T : Attribute
		{
			// GetColumnSpan needs the mapping, which the session factory itself provides.
			if (_sessionFactory is not ISessionFactoryImplementor mapping)
				return Array.Empty<T>();

			var names    = componentType.PropertyNames;
			var subtypes = componentType.Subtypes;
			var columns  = prop.ColumnNames;

			var result = new List<T>(names.Length);
			var offset = 0;

			for (var i = 0; i < names.Length && offset < columns.Length; i++)
			{
				var subtype = subtypes[i];
				var span    = subtype.GetColumnSpan(mapping);

				if (span != 1 || subtype.IsAssociationType)
				{
					var what = subtype.IsAssociationType ? "an association" : $"{span.ToString(CultureInfo.InvariantCulture)} columns";

					throw new LinqToDBForNHibernateToolsException(
						$"Component '{owner.Name}.{prop.MemberName}' maps '{names[i]}' to {what}, which has no single column to map to.");
				}

				var column = BuildColumnAttribute(columns[offset], subtype, componentType.PropertyNullability[i], false, 0, false, names[i]);

				result.Add((T)(Attribute)column);

				// Advance by the sub-property's own width so the remaining ones stay aligned with their columns.
				offset += span;
			}

			return result.ToArray();
		}

		/// <summary>
		/// Describes a single database column, taking type, length and precision from the NHibernate type when it
		/// exposes them. <paramref name="memberName"/> names the member relative to the type the attribute is
		/// returned for, which is how a component's sub-properties are addressed.
		/// </summary>
		static ColumnAttribute BuildColumnAttribute(
			string  columnName,
			IType?  propType,
			bool    canBeNull,
			bool    isPrimaryKey,
			int     pkOrder,
			bool    isIdentity,
			string? memberName = null)
		{
			SqlType? sqlType = null;

			if (propType is NullableType nullableType)
			{
				sqlType = nullableType.SqlType;
			}
			else if (propType is CustomType customType && customType.UserType.SqlTypes.Length == 1)
			{
				// A user type is not a NullableType, but a single-column one still describes its column.
				sqlType = customType.UserType.SqlTypes[0];
			}

			var column = new ColumnAttribute
			{
				Name            = columnName,
				MemberName      = memberName,
				CanBeNull       = canBeNull,
				DataType        = sqlType != null ? DbTypeToDataType(sqlType.DbType) : DataType.Undefined,
				IsPrimaryKey    = isPrimaryKey,
				PrimaryKeyOrder = pkOrder,
				IsIdentity      = isIdentity,
			};

			if (sqlType != null)
			{
				if (sqlType.Length > 0)
					column.Length = sqlType.Length;
				if (sqlType.Precision > 0)
					column.Precision = sqlType.Precision;
				if (sqlType.Scale > 0)
					column.Scale = sqlType.Scale;
			}

			return column;
		}
	}
}
