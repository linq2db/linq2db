namespace LinqToDB.CodeModel
{
	partial class TableLayoutBuilder
	{
		#region Fluent API
		/// <summary>
		/// Column group layout definition builder.
		/// </summary>
		/// <typeparam name="TConfigurator">Current column group builder type.</typeparam>
		internal interface IColumnGroupConfiguratorBase<TConfigurator>
			where TConfigurator : IColumnGroupConfiguratorBase<TConfigurator>
		{
			/// <summary>
			/// Define child column group.
			/// </summary>
			/// <param name="name">Group name. Must be unique within current group.</param>
			/// <param name="prefix">Optional group prefix. Used only for non-empty groups.</param>
			/// <param name="separator">Optional sub-groups separator. Used only between non-empty sub-groups.</param>
			/// <param name="suffix">Optional group suffix. Used only for non-empty groups.</param>
			/// <returns>Child group builder.</returns>
			IColumnGroupConfigurator<TConfigurator> Group(string name, string? prefix, string? separator, string? suffix);

			/// <summary>
			/// Define simple column.
			/// </summary>
			/// <param name="name">Column name. Must be unique within current group.</param>
			/// <returns>Current group builder instance.</returns>
			TConfigurator Column(string name);

			/// <summary>
			/// Column with fixed value.
			/// </summary>
			/// <param name="value">Column value.</param>
			/// <param name="requireNonEmptyBefore">When is not 0, specify number of prevoius columns (or column groups) in current group that should have value for this column to generate it's value. When both <paramref name="requireNonEmptyAfter"/> and <paramref name="requireNonEmptyBefore"/> are not 0, this column value will be generated only if both pre- and post-columns are not empty.</param>
			/// <param name="requireNonEmptyAfter">When is not 0, specify number of next columns (or column groups) in current group that should have value for this column to generate it's value. When both <paramref name="requireNonEmptyAfter"/> and <paramref name="requireNonEmptyBefore"/> are not 0, this column value will be generated only if both pre- and post-columns are not empty.</param>
			/// <returns>Current group builder instance.</returns>
			TConfigurator Fixed(string value, int requireNonEmptyBefore, int requireNonEmptyAfter);
		}

		/// <summary>
		/// Column group layout definition builder.
		/// </summary>
		/// <typeparam name="TParent">Parent column group type.</typeparam>
		internal interface IColumnGroupConfigurator<TParent> : IColumnGroupConfiguratorBase<IColumnGroupConfigurator<TParent>>
			where TParent : IColumnGroupConfiguratorBase<TParent>
		{
			/// <summary>
			/// Finalize column group definition.
			/// </summary>
			/// <returns>Parent column group builder instance.</returns>
			TParent EndGroup();
		}

		/// <summary>
		/// Top-level table layout definition builder.
		/// </summary>
		internal interface IHeaderConfigurator : IColumnGroupConfiguratorBase<IHeaderConfigurator>
		{
			/// <summary>
			/// Finalize table layout definition.
			/// </summary>
			void End();
		}
		#endregion

		#region implementation
		/// <summary>
		/// Top level column group builder implementation.
		/// </summary>
		private sealed class HeaderConfigurator : GroupConfiguratorBase<IHeaderConfigurator>, IHeaderConfigurator
		{
			public HeaderConfigurator(ColumnGroup group)
				: base(group)
			{
			}

			void IHeaderConfigurator.End()
			{
				// freeze definition changes for whole column structure
				Group.FinalizeConfiguration();
			}
		}

		/// <summary>
		/// Child column group builder implementation.
		/// </summary>
		/// <typeparam name="TParent"></typeparam>
		private sealed class GroupConfigurator<TParent> : GroupConfiguratorBase<IColumnGroupConfigurator<TParent>>, IColumnGroupConfigurator<TParent>
			where TParent : IColumnGroupConfiguratorBase<TParent>
		{
			private readonly TParent _parent;

			public GroupConfigurator(ColumnGroup group, TParent parent)
				: base(group)
			{
				_parent = parent;
			}

			TParent IColumnGroupConfigurator<TParent>.EndGroup() => _parent;
		}

		/// <summary>
		/// Base class for column group builders.
		/// </summary>
		/// <typeparam name="TConfigurator">Column group builder type.</typeparam>
		private abstract class GroupConfiguratorBase<TConfigurator>
			: IColumnGroupConfiguratorBase<TConfigurator>
			where TConfigurator : IColumnGroupConfiguratorBase<TConfigurator>
		{
			/// <summary>
			/// Column group definition.
			/// </summary>
			protected ColumnGroup Group { get; }

			protected GroupConfiguratorBase(ColumnGroup group)
			{
				Group = group;
			}

			TConfigurator IColumnGroupConfiguratorBase<TConfigurator>.Column(string name)
			{
				Group.AddColumn(new SimpleColumn(name));
				return (TConfigurator)(object)this;
			}

			TConfigurator IColumnGroupConfiguratorBase<TConfigurator>.Fixed(string value, int requireNonEmptyBefore, int requireNonEmptyAfter)
			{
				Group.AddColumn(new FixedColumn(value, requireNonEmptyBefore, requireNonEmptyAfter));
				return (TConfigurator)(object)this;
			}

			IColumnGroupConfigurator<TConfigurator> IColumnGroupConfiguratorBase<TConfigurator>.Group(string name, string? prefix, string? separator, string? suffix)
			{
				var group = new ColumnGroup(name, prefix, separator, suffix);
				Group.AddColumn(group);
				return new GroupConfigurator<TConfigurator>(group, (TConfigurator)(object)this);
			}
		}
		#endregion
	}
}
