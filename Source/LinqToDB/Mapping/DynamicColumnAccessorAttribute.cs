#nullable disable
using System;
using System.Linq.Expressions;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Configure setter and getter methods for dynamic columns.
	/// </summary>
	/// <remarks>
	/// Expected signatures for getter and setter:
	/// <code>
	/// // should return true and value of property, if property value found in storage
	/// // should return false if property value not found in storage
	/// static object Getter(Entity object, string propertyName, object defaultValue);
	/// // or
	/// object this.Getter(string propertyName, object defaultValue);
	/// // where defaultValue is default value for property type for current MappingSchema
	/// 
	/// static void Setter(Entity object, string propertyName, object value)
	/// or
	/// void this.Setter(string propertyName, object value)
	/// </code>
	/// </remarks>
	/// <seealso cref="Attribute" />
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class DynamicColumnAccessorAttribute : Attribute, IConfigurationProvider
	{
		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string Configuration              { get; set; }

		/// <summary>
		/// Gets or sets name of dynamic properties property setter method.
		/// </summary>
		public string SetterMethod               { get; set; }

		/// <summary>
		/// Gets or sets name of dynamic properties property getter method.
		/// </summary>
		public string GetterMethod               { get; set; }

		/// <summary>
		/// Gets or sets name of dynamic properties property setter expression method or property. Method or property
		/// must be static.
		/// </summary>
		public string SetterExpressionMethod     { get; set; }

		/// <summary>
		/// Gets or sets name of dynamic properties property getter expression method or property. Method or property
		/// must be static.
		/// </summary>
		public string GetterExpressionMethod     { get; set; }

		/// <summary>
		/// Gets or sets name of dynamic properties property set expression.
		/// </summary>
		public LambdaExpression SetterExpression { get; set; }

		/// <summary>
		/// Gets or sets name of dynamic properties property get expression.
		/// </summary>
		public LambdaExpression GetterExpression { get; set; }

		protected internal void Validate()
		{
			var setters = 0;
			var getters = 0;
			if (SetterMethod           != null) setters++;
			if (SetterExpressionMethod != null) setters++;
			if (SetterExpression       != null) setters++;
			if (GetterMethod           != null) getters++;
			if (GetterExpressionMethod != null) getters++;
			if (GetterExpression       != null) getters++;

			if (setters != 1 || getters != 1)
				throw new LinqToDBException($"{nameof(DynamicColumnAccessorAttribute)} should have exactly one setter and getter configured.");
		}
	}
}
