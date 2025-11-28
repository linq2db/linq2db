using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using LINQPad.Extensibility.DataContext;

using LinqToDB.Extensions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

namespace LinqToDB.LINQPad;

/// <summary>
/// Generates schema tree structure for static context using reflection.
/// </summary>
internal static class StaticSchemaGenerator
{
	private sealed class TableInfo
	{
		public TableInfo(PropertyInfo propertyInfo)
		{
			Name         = propertyInfo.Name;
			Type         = propertyInfo.PropertyType.GetItemType()!;
			TypeAccessor = TypeAccessor.GetAccessor(Type);

			var tableAttr = Type.GetAttribute<TableAttribute>();

			if (tableAttr != null)
			{
				IsColumnAttributeRequired = tableAttr.IsColumnAttributeRequired;
				IsView                    = tableAttr.IsView;
			}
		}

		/// <summary>
		/// Table accessor property name.
		/// </summary>
		public readonly string       Name;
		/// <summary>
		/// Table mapping class type.
		/// </summary>
		public readonly Type         Type;
		/// <summary>
		/// Value of <see cref="TableAttribute.IsColumnAttributeRequired"/> mapping property for mapping.
		/// </summary>
		public readonly bool         IsColumnAttributeRequired;
		/// <summary>
		/// Table mapping <see cref="TypeAccessor"/> instance.
		/// </summary>
		public readonly TypeAccessor TypeAccessor;
		/// <summary>
		/// <see cref="TableAttribute.IsView"/> value for mapping.
		/// </summary>
		public readonly bool         IsView;
	}

#pragma warning disable CA1002 // Do not expose generic lists
	public static List<ExplorerItem> GetSchema(Type customContextType)
#pragma warning restore CA1002 // Do not expose generic lists
	{
		var items = new List<ExplorerItem>();

		List<ExplorerItem>? tableItems = null;
		List<ExplorerItem>? viewItems  = null;

		// tables discovered using table access properties in context:
		// ITable<TableRecord> Prop or // IQueryable<TableRecord> Prop
		var tables = customContextType.GetProperties()
			.Where(static p => !p.HasAttribute<ObsoleteAttribute>() && typeof(IQueryable<>).IsSameOrParentOf(p.PropertyType))
			.OrderBy(static p => p.Name)
			.Select(static p => new TableInfo(p));

		var lookup = new Dictionary<Type, ExplorerItem>();

		foreach (var table in tables)
		{
			var list = table.IsView ? (viewItems ??= new()) : (tableItems ??= new());

			var item = GetTable(table.IsView ? ExplorerIcon.View : ExplorerIcon.Table, table);
			list.Add(item);
			lookup.Add(table.Type, item);

			// add association nodes
			foreach (var ma in table.TypeAccessor.Members)
			{
				if (ma.MemberInfo.HasAttribute<AssociationAttribute>())
				{
					var isToMany   = ma.Type is IEnumerable;
					// TODO: try to infer this information?
					var backToMany = true;

					var otherType  = isToMany ? ma.Type.GetItemType()! : ma.Type;
					lookup.TryGetValue(otherType, out var otherItem);

					item.Children.Add(
						new ExplorerItem(
							ma.Name,
							isToMany
								? ExplorerItemKind.CollectionLink
								: ExplorerItemKind.ReferenceLink,
							isToMany
								? ExplorerIcon.OneToMany
								: backToMany
									? ExplorerIcon.ManyToOne
									: ExplorerIcon.OneToOne)
						{
							DragText        = CSharpUtils.EscapeIdentifier(ma.Name),
							ToolTipText     = GetTypeName(ma.Type),
							IsEnumerable    = isToMany,
							HyperlinkTarget = otherItem
						});
				}
			}
		}

		if (tableItems != null)
			items.Add(new ExplorerItem("Tables", ExplorerItemKind.Category, ExplorerIcon.Table)
			{
				Children = tableItems
			});

		if (viewItems != null)
			items.Add(new ExplorerItem("Views", ExplorerItemKind.Category, ExplorerIcon.View)
			{
				Children = viewItems
			});

		return items;
	}

	static ExplorerItem GetTable(ExplorerIcon icon, TableInfo table)
	{
		var columns =
		(
			from ma in table.TypeAccessor.Members
			where !ma.MemberInfo.HasAttribute<AssociationAttribute>()
			let ca = ma.MemberInfo.GetAttribute<ColumnAttribute>()
			let id = ma.MemberInfo.GetAttribute<IdentityAttribute>()
			let pk = ma.MemberInfo.GetAttribute<PrimaryKeyAttribute>()
			orderby
				ca == null ? 1 : ca.Order >= 0 ? 0 : 2,
				ca?.Order,
				ma.Name
			where
				ca != null && ca.IsColumn ||
				pk != null ||
				id != null ||
				ca == null && !table.IsColumnAttributeRequired && MappingSchema.Default.IsScalarType(ma.Type)
			select new ExplorerItem(
				ma.Name,
				ExplorerItemKind.Property,
				pk != null || ca != null && ca.IsPrimaryKey ? ExplorerIcon.Key : ExplorerIcon.Column)
			{
				Text     = $"{ma.Name} : {GetTypeName(ma.Type)}",
				DragText = CSharpUtils.EscapeIdentifier(ma.Name),
			}
		).ToList();

		var ret = new ExplorerItem(table.Name, ExplorerItemKind.QueryableObject, icon)
		{
			DragText     = CSharpUtils.EscapeIdentifier(table.Name),
			IsEnumerable = true,
			Children     = columns
		};

		return ret;
	}

	static string GetTypeName(Type type)
	{
		switch (type.FullName)
		{
			case "System.Boolean" : return "bool";
			case "System.Byte"    : return "byte";
			case "System.SByte"   : return "sbyte";
			case "System.Int16"   : return "short";
			case "System.Int32"   : return "int";
			case "System.Int64"   : return "long";
			case "System.UInt16"  : return "ushort";
			case "System.UInt32"  : return "uint";
			case "System.UInt64"  : return "ulong";
			case "System.Decimal" : return "decimal";
			case "System.Single"  : return "float";
			case "System.Double"  : return "double";
			case "System.String"  : return "string";
			case "System.Char"    : return "char";
			case "System.Object"  : return "object";
		}

		if (type.IsArray)
			return GetTypeName(type.GetElementType()!) + "[]";

		if (type.IsNullableType)
			return GetTypeName(type.UnwrapNullableType()) + '?';

		if (type.IsGenericType)
		{
			var typeName = new StringBuilder();
			typeName
				.Append(type.Name)
				.Append('<');

			var first = true;
			foreach (var param in type.GetGenericArguments())
			{
				if (first)
					first = false;
				else
					typeName.Append(", ");

				typeName.Append(GetTypeName(param));
			}

			return typeName.Append('>').ToString();
		}

		return type.Name;
	}
}
