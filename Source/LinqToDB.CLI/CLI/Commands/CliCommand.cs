using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CLI
{
	/// <summary>
	/// Base class for CLI commands.
	/// </summary>
	internal abstract class CliCommand
	{
		private readonly Dictionary<OptionCategory, List<CliOption>> _optionsByCategory  = new ();
		// TODO: replace with HashSet if not used as it also ensures command name/shortname is unique
		private readonly Dictionary<string, CliOption>               _optionsByName      = new ();
		private readonly Dictionary<char, CliOption>                 _optionsByShortName = new ();

		/// <summary>
		/// Base CLI command constructor.
		/// </summary>
		/// <param name="name">Command name.</param>
		/// <param name="hasOptions">Indicate wether command could have options or not.</param>
		/// <param name="template">Command template.</param>
		/// <param name="help">Command help text.</param>
		/// <param name="examples">Command examples.</param>
		protected CliCommand(string name, bool hasOptions, string template, string help, IReadOnlyCollection<CommandExample> examples)
		{
			Name       = name;
			HasOptions = hasOptions;
			Template   = template;
			Help       = help;
			Examples   = examples;
		}

		/// <summary>
		/// Gets command name.
		/// </summary>
		public string                              Name       { get; }
		/// <summary>
		/// Gets command template (not parsed, for help rendering only).
		/// </summary>
		public string                              Template   { get; }
		/// <summary>
		/// Gets command help text.
		/// </summary>
		public string                              Help       { get; }
		/// <summary>
		/// Gets command examples.
		/// </summary>
		public IReadOnlyCollection<CommandExample> Examples   { get; }

		/// <summary>
		/// Gets flag, indicating wether command supports options or not.
		/// </summary>
		public bool                                HasOptions { get; }

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
		/// Gets all command options.
		/// </summary>
		public IEnumerable<CliOption> AllOptions => _optionsByName.Values;

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
		/// Execute command with provided parameters.
		/// </summary>
		/// <param name="controller">CLI controller instance.</param>
		/// <param name="rawArgs">Raw list of CLI arguments.</param>
		/// <param name="options">Parsed command options with values.</param>
		/// <param name="unknownArgs">List of unrecognized arguments.</param>
		/// <returns>Command execution status code.</returns>
		public abstract int Execute(
			CLIController                           controller,
			string[]                                rawArgs,
			IReadOnlyDictionary<CliOption, object?> options,
			IReadOnlyCollection<string>             unknownArgs);
	}
}
