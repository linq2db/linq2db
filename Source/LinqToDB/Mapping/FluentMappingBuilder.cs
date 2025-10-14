using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Metadata;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Fluent mapping builder.
	/// </summary>
	public class FluentMappingBuilder
	{
		private Dictionary<Type, List<MappingAttribute>>       _typeAttributes   = new();
		private Dictionary<MemberInfo, List<MappingAttribute>> _memberAttributes = new();
		private List<MemberInfo>                               _orderedMembers   = new();

		#region Init

		/// <summary>
		/// Creates new MappingSchema and fluent mapping builder for it.
		/// </summary>
		public FluentMappingBuilder()
		{
			MappingSchema = new ();
		}

		/// <summary>
		/// Creates fluent mapping builder for specified mapping schema.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema.</param>
		public FluentMappingBuilder(MappingSchema mappingSchema)
		{
			MappingSchema = mappingSchema ?? throw new ArgumentNullException(nameof(mappingSchema));
		}

		/// <summary>
		/// Gets builder's mapping schema.
		/// </summary>
		public MappingSchema MappingSchema { get; }

		#endregion

		/// <summary>
		/// Adds configured mappings to builder's mapping schema.
		/// </summary>
		public FluentMappingBuilder Build()
		{
			if (_typeAttributes.Count > 0 || _memberAttributes.Count > 0)
			{
				MappingSchema.AddMetadataReader(new FluentMetadataReader(_typeAttributes, _memberAttributes, _orderedMembers));
				_typeAttributes.Clear();
				_memberAttributes.Clear();
				_orderedMembers.Clear();
			}

			return this;
		}

		#region GetAtributes

		/// <summary>
		/// Gets attributes of type <typeparamref name="T"/>, applied to specified type.
		/// </summary>
		/// <typeparam name="T">Mapping attribute type.</typeparam>
		/// <param name="type">Type with attributes.</param>
		/// <returns>Returns attributes of specified type, applied to <paramref name="type"/>.</returns>
		internal IEnumerable<T> GetAttributes<T>(Type type)
			where T : MappingAttribute
		{
			return _typeAttributes.TryGetValue(type, out var attributes) ? attributes.OfType<T>() : [];
		}

		/// <summary>
		/// Gets attributes of type <typeparamref name="T"/>, applied to specified member. Search for member in specified
		/// type or it's parents.
		/// </summary>
		/// <typeparam name="T">Mapping attribute type.</typeparam>
		/// <param name="memberInfo">Member descriptor.</param>
		/// <returns>Returns attributes of specified type, applied to <paramref name="memberInfo"/>.</returns>
		internal IEnumerable<T> GetAttributes<T>(MemberInfo memberInfo)
			where T : MappingAttribute
		{
			return _memberAttributes.TryGetValue(memberInfo, out var attributes) ? attributes.OfType<T>() : [];
		}

		#endregion

		#region HasAtribute

		/// <summary>
		/// Adds mapping attribute to specified type.
		/// </summary>
		/// <param name="type">Target type.</param>
		/// <param name="attribute">Mapping attribute to add to specified type.</param>
		/// <returns>Returns current fluent mapping builder.</returns>
		public FluentMappingBuilder HasAttribute(Type type, MappingAttribute attribute)
		{
			AddAttribute(type, attribute);
			return this;
		}

		/// <summary>
		/// Adds mapping attribute to specified type.
		/// </summary>
		/// <typeparam name="T">Target type.</typeparam>
		/// <param name="attribute">Mapping attribute to add to specified type.</param>
		/// <returns>Returns current fluent mapping builder.</returns>
		public FluentMappingBuilder HasAttribute<T>(MappingAttribute attribute)
		{
			AddAttribute(typeof(T), attribute);
			return this;
		}

		/// <summary>
		/// Adds mapping attribute to specified member.
		/// </summary>
		/// <param name="memberInfo">Target member.</param>
		/// <param name="attribute">Mapping attribute to add to specified member.</param>
		/// <returns>Returns current fluent mapping builder.</returns>
		public FluentMappingBuilder HasAttribute(MemberInfo memberInfo, MappingAttribute attribute)
		{
			AddAttribute(memberInfo, attribute);
			return this;
		}

		/// <summary>
		/// Adds mapping attribute to a member, specified using lambda expression.
		/// </summary>
		/// <param name="func">Target member, specified using lambda expression.</param>
		/// <param name="attribute">Mapping attribute to add to specified member.</param>
		/// <returns>Returns current fluent mapping builder.</returns>
		public FluentMappingBuilder HasAttribute(LambdaExpression func, MappingAttribute attribute)
		{
			var memberInfo = MemberHelper.GetMemberInfo(func);
			AddAttribute(memberInfo, attribute);
			return this;
		}

		/// <summary>
		/// Adds mapping attribute to a member, specified using lambda expression.
		/// </summary>
		/// <typeparam name="T">Type of labmda expression parameter.</typeparam>
		/// <param name="func">Target member, specified using lambda expression.</param>
		/// <param name="attribute">Mapping attribute to add to specified member.</param>
		/// <returns>Returns current fluent mapping builder.</returns>
		public FluentMappingBuilder HasAttribute<T>(Expression<Func<T,object?>> func, MappingAttribute attribute)
		{
			var memberInfo = MemberHelper.MemberOf(func);
			AddAttribute(memberInfo, attribute);
			return this;
		}

		#endregion

		/// <summary>
		/// Creates entity builder for specified mapping type.
		/// </summary>
		/// <typeparam name="T">Mapping type.</typeparam>
		/// <param name="configuration">Optional mapping schema configuration name, for which this entity builder should be taken into account.
		/// <see cref="ProviderName"/> for standard configuration names.</param>
		/// <returns>Returns entity fluent mapping builder.</returns>
		public EntityMappingBuilder<T> Entity<T>(string? configuration = null)
		{
			return new (this, configuration);
		}

		private void AddAttribute(Type owner, MappingAttribute attribute)
		{
			if (!_typeAttributes.TryGetValue(owner, out var attributes))
				_typeAttributes.Add(owner, attributes = new());

			attributes.Add(attribute);
		}

		private void AddAttribute(MemberInfo memberInfo, MappingAttribute attribute)
		{
			if (!_memberAttributes.TryGetValue(memberInfo, out var attributes))
			{
				_memberAttributes.Add(memberInfo, attributes = new());
				_orderedMembers.Add(memberInfo);
			}

			attributes.Add(attribute);
		}
	}
}
