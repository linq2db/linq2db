using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Base class for CLI commands.
	/// </summary>
	internal abstract class CliCommand
	{
		private readonly Dictionary<OptionCategory, List<CliOption>>           _optionsByCategory      = new ();
		private readonly List<CliOption>                                       _optionsWithoutCategory = new ();
		// TODO: replace with HashSet if not used as it also ensures command name/shortname is unique
		private readonly Dictionary<string, CliOption>                         _optionsByName          = new ();
		private readonly Dictionary<char, CliOption>                           _optionsByShortName     = new ();
		private readonly Dictionary<CliOption, IReadOnlyCollection<CliOption>> _incompatibleOptions    = new ();

		/// <summary>
		/// Base CLI command constructor.
		/// </summary>
		/// <param name="name">Command name.</param>
		/// <param name="hasOptions">Indicate wether command could have options or not.</param>
		/// <param name="supportsJson">Indicate wether command supports options import from JSON file.</param>
		/// <param name="template">Command template.</param>
		/// <param name="help">Command help text.</param>
		/// <param name="examples">Command examples.</param>
		protected CliCommand(string name, bool hasOptions, bool supportsJson, string template, string help, IReadOnlyCollection<CommandExample> examples)
		{
			Name         = name;
			HasOptions   = hasOptions;
			SupportsJSON = supportsJson;
			Template     = template;
			Help         = help;
			Examples     = examples;
		}

		/// <summary>
		/// Gets command name.
		/// </summary>
		public string                              Name         { get; }
		/// <summary>
		/// Gets command template (not parsed, for help rendering only).
		/// </summary>
		public string                              Template     { get; }
		/// <summary>
		/// Gets command help text.
		/// </summary>
		public string                              Help         { get; }
		/// <summary>
		/// Gets command examples.
		/// </summary>
		public IReadOnlyCollection<CommandExample> Examples     { get; }

		/// <summary>
		/// Gets flag, indicating wether command supports options or not.
		/// </summary>
		public bool                                HasOptions   { get; }

		/// <summary>
		/// Gets flag, indicating wether command supports options import from JSON.
		/// </summary>
		public bool                                SupportsJSON { get; }

		/// <summary>
		/// Gets command option categories in rendering (in help) order.
		/// </summary>
		public IEnumerable<OptionCategory>         Categories => _optionsByCategory.Keys.OrderBy(static _ => _.Order);

		/// <summary>
		/// Gets options for specific options category.
		/// </summary>
		/// <param name="category">Options category.</param>
		/// <returns>Category options.</returns>
		public IReadOnlyCollection<CliOption> GetCategoryOptions(OptionCategory category) => _optionsByCategory[category];

		/// <summary>
		/// Gets options without category.
		/// </summary>
		/// <returns>Options.</returns>
		public IReadOnlyCollection<CliOption> GetOptionsWithoutCategory() => _optionsWithoutCategory;

		/// <summary>
		/// Returns option descriptor by name of option or <c>null</c> if option not found.
		/// </summary>
		/// <param name="name">Option name.</param>
		/// <returns>Option or <c>null</c>.</returns>
		public CliOption? GetOptionByName(string name)
		{
			return _optionsByName.TryGetValue(name, out var option) ? option : null;
		}

		/// <summary>
		/// Returns option descriptor by short name of option or <c>null</c> if option not found.
		/// </summary>
		/// <param name="name">Option short name.</param>
		/// <returns>Option or <c>null</c>.</returns>
		public CliOption? GetOptionByShortName(char name)
		{
			return _optionsByShortName.TryGetValue(name, out var option) ? option : null;
		}

		/// <summary>
		/// Returns list of options, not compatible with <paramref name="forOption"/> option.
		/// </summary>
		/// <param name="forOption">For which option return incompatible options.</param>
		/// <returns>List of incompatible options or <c>null</c>, if <paramref name="forOption"/> option has no conflicts with other options.</returns>
		public IEnumerable<CliOption>? GetIncompatibleOptions(CliOption forOption)
		{
			if (_incompatibleOptions.TryGetValue(forOption, out var options))
				return options;

			return null;
		}

		/// <summary>
		/// Gets all command options.
		/// </summary>
		public IEnumerable<CliOption> AllOptions => _optionsByName.Values;

		/// <summary>
		/// Register command option without category.
		/// </summary>
		/// <param name="option">Command's option to register.</param>
		protected void AddOption(CliOption option)
		{
			if (!HasOptions)
				throw new InvalidOperationException($"Command {Name} doesn't support options");

			_optionsWithoutCategory.Add(option);
			_optionsByName         .Add(option.Name, option);

			if (option.ShortName != null)
				_optionsByShortName.Add(option.ShortName.Value, option);
		}

		/// <summary>
		/// Register command option with specific category.
		/// </summary>
		/// <param name="category">Option's category.</param>
		/// <param name="option">Command's option to register.</param>
		protected void AddOption(OptionCategory category, CliOption option)
		{
			if (!HasOptions)
				throw new InvalidOperationException($"Command {Name} doesn't support options");

			if (!_optionsByCategory.TryGetValue(category, out var options))
				_optionsByCategory.Add(category, options = new ());

			options.Add(option);

			_optionsByName.Add(option.Name, option);

			if (option.ShortName != null)
				_optionsByShortName.Add(option.ShortName.Value, option);
		}

		/// <summary>
		/// Register several command options that cannot be used together with specific category.
		/// </summary>
		/// <param name="category">Options category (same category for all provided options).</param>
		/// <param name="options">Command options to register. At least two options required.</param>
		protected void AddMutuallyExclusiveOptions(OptionCategory category, params CliOption[] options)
		{
			if (options.Length < 2)
				throw new InvalidOperationException($"{nameof(AddMutuallyExclusiveOptions)} requires at least two options, but {options.Length} were provided");

			foreach (var option in options)
			{
				AddOption(category, option);
				_incompatibleOptions.Add(option, options.Where(_ => _ != option).ToArray());
			}
		}

		/// <summary>
		/// Execute command with provided parameters.
		/// </summary>
		/// <param name="controller">CLI controller instance.</param>
		/// <param name="rawArgs">Raw list of CLI arguments.</param>
		/// <param name="options">Parsed command options with values. Command allowed to modify dictionary (e.g. remove processed options to detect options without handler).</param>
		/// <param name="unknownArgs">List of unrecognized arguments.</param>
		/// <returns>Command execution status code.</returns>
		public abstract ValueTask<int> Execute(
			CliController                  controller,
			string[]                       rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string>    unknownArgs);
	}
}
