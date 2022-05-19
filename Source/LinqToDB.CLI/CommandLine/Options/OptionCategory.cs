namespace LinqToDB.CommandLine;

/// <summary>
/// Defines CLI command options category.
/// </summary>
/// <param name="Order">Category rendering order in command help.</param>
/// <param name="Name">Options category name.</param>
/// <param name="Description">Short description text for options category.</param>
/// <param name="JsonProperty">Property name for category options child object in JSON options file.</param>
internal sealed record OptionCategory(int Order, string Name, string Description, string JsonProperty);
