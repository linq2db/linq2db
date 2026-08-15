using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tests.LinqToDB.CLI
{
	public abstract partial class McpTestBase
	{
		static readonly JsonSerializerOptions _toolResultJsonOptions = new()
		{
			PropertyNameCaseInsensitive = true,
		};

		protected static T ReadToolResult<T>(JsonObject response)
		{
			return JsonSerializer.Deserialize<T>(ReadToolText(response), _toolResultJsonOptions)
				?? throw new InvalidOperationException($"Cannot deserialize MCP tool content as {typeof(T).Name}.");
		}

		protected static T ReadResponseResult<T>(JsonObject response)
			where T : class
		{
			return response["result"].Deserialize<T>(_toolResultJsonOptions)
				?? throw new InvalidOperationException($"Cannot deserialize MCP response result as {typeof(T).Name}.");
		}

		protected static string ReadToolText(JsonObject response)
		{
			var result = ReadResponseResult<McpTestCallToolResult>(response);

			return result.Content.Count > 0
				? result.Content[0].Text
				: throw new InvalidOperationException("MCP tool response doesn't contain primary text content.");
		}

		protected static string ReadToolErrorText(JsonObject response)
		{
			var result = ReadResponseResult<McpTestCallToolResult>(response);

			if (result.IsError != true)
				throw new InvalidOperationException("MCP tool response is not an error.");

			return result.Content.Count > 0
				? result.Content[0].Text
				: throw new InvalidOperationException("MCP tool error response doesn't contain primary text content.");
		}

		protected static McpTestProfile FindProfile(McpTestInfoResult info, string name)
		{
			return info.Profiles.Find(profile => string.Equals(profile.Name, name, StringComparison.Ordinal))
				?? throw new InvalidOperationException($"Profile '{name}' not found.");
		}

		protected static McpTestSupportedProvider FindSupportedProvider(McpTestInfoResult info, string name)
		{
			return info.SupportedProviders.Find(provider => string.Equals(provider.Name, name, StringComparison.Ordinal))
				?? throw new InvalidOperationException($"Supported provider '{name}' not found.");
		}

		protected static McpTestSchemaTable FindSchemaTable(McpTestSchemaResult schema, string name)
		{
			return schema.Tables.Find(table => string.Equals(table.Name, name, StringComparison.Ordinal))
				?? throw new InvalidOperationException($"Schema table '{name}' not found.");
		}

		protected static McpTestTool FindTool(McpTestToolsResult tools, string name)
		{
			return tools.Tools.Find(tool => string.Equals(tool.Name, name, StringComparison.Ordinal))
				?? throw new InvalidOperationException($"Tool '{name}' not found.");
		}

		protected sealed record McpTestInfoResult
		{
			public required McpTestServerInfo              Server                    { get; init; }
			public required string                         DefaultProfile            { get; init; }
			public required bool                           DefaultProfileUsable      { get; init; }
			public required List<McpTestProfile>           Profiles                  { get; init; }
			public required List<McpTestSupportedProvider> SupportedProviders        { get; init; }
			public required List<string>                   SupportedOutputFormats    { get; init; }
			public required List<string>                   QueryCommandOutputFormats { get; init; }
			public required McpTestRules                   Rules                     { get; init; }
		}

		protected sealed record McpTestServerInfo
		{
			public required string Name               { get; init; }
			public required string Command            { get; init; }
			public required bool   ExecuteToolEnabled { get; init; }
			public required int    MaxResponseBytes   { get; init; }
		}

		protected sealed record McpTestProfile
		{
			public required string  Name                        { get; init; }
			public          string? Description                 { get; init; }
			public required string  Provider                    { get; init; }
			public required string  Dialect                     { get; init; }
			public required string  DefaultOutput               { get; init; }
			public required bool    DefaultOutputSupportedByMcp { get; init; }
			public required int     MaxRows                     { get; init; }
			public required bool    EnableExecute               { get; init; }
			public required bool    ImpersonationEnabled        { get; init; }
			public          string? ConnectionString            { get; init; }
			public          string? ConnectionStringEnv         { get; init; }
			public          string? Password                    { get; init; }
			public          string? ProviderLocation            { get; init; }
		}

		protected sealed record McpTestSupportedProvider
		{
			public required string       Name          { get; init; }
			public required List<string> ProviderNames { get; init; }
			public required bool         Bundled       { get; init; }
			public          string?      Notes         { get; init; }
		}

		protected sealed record McpTestRules
		{
			public required bool   SingleStatementOnly                    { get; init; }
			public required bool   SqlGuardIsSecurityBoundary             { get; init; }
			public required string SqlGuardWarning                        { get; init; }
			public required bool   ConnectionStringPlaceholdersEscaped    { get; init; }
			public required string ConnectionStringPlaceholderWarning     { get; init; }
			public required bool   ProviderInputAllowedInToolCall         { get; init; }
			public required bool   ConnectionStringInputAllowedInToolCall { get; init; }
			public required bool   CredentialsInputAllowedInToolCall      { get; init; }
		}

		protected sealed record McpTestJsonTableResult
		{
			public required List<McpTestOutputColumn> Columns          { get; init; }
			public required List<List<string?>>      Rows             { get; init; }
			public required int                      RowCount         { get; init; }
			public required bool                     Truncated        { get; init; }
			public          string?                  TruncationReason { get; init; }
			public          int?                     MaxOutputBytes   { get; init; }
			public          int?                     RecordsAffected  { get; init; }
		}

		protected sealed record McpTestOutputColumn
		{
			public required string Name { get; init; }
		}

		protected sealed record McpTestSchemaResult
		{
			public required string                   Provider { get; init; }
			public required string                   Dialect  { get; init; }
			public required McpTestSchemaOptions     Options  { get; init; }
			public required List<McpTestSchemaTable> Tables   { get; init; }
		}

		protected sealed record McpTestSchemaOptions
		{
			public required string       DetailLevel    { get; init; }
			public required bool         GetProcedures  { get; init; }
			public required bool         GetForeignKeys { get; init; }
			public required List<string> FilterTables   { get; init; }
		}

		protected sealed record McpTestSchemaNamesResult
		{
			public required string                        Provider { get; init; }
			public required string                        Dialect  { get; init; }
			public required McpTestSchemaOptions          Options  { get; init; }
			public required List<McpTestSchemaObjectName> Objects  { get; init; }
		}

		protected sealed record McpTestSchemaObjectName
		{
			public          string? Catalog { get; init; }
			public          string? Schema  { get; init; }
			public required string  Name    { get; init; }
			public required string  Kind    { get; init; }
		}

		protected sealed record McpTestSchemaTable
		{
			public required string                        Name        { get; init; }
			public required List<McpTestSchemaColumn>     Columns     { get; init; }
			public required List<McpTestSchemaForeignKey> ForeignKeys { get; init; }
		}

		protected sealed record McpTestSchemaColumn;

		protected sealed record McpTestSchemaForeignKey;

		protected sealed record McpTestCallToolResult
		{
			public          bool?                IsError { get; init; }
			public required List<McpTestContent> Content { get; init; }
		}

		protected sealed record McpTestContent
		{
			public required string Text { get; init; }
		}

		protected sealed record McpTestValueRow
		{
			public required string Value { get; init; }
		}

		protected sealed record McpTestInitializeResult
		{
			public required McpTestInitializeServerInfo ServerInfo   { get; init; }
			public required string                      Instructions { get; init; }
		}

		protected sealed record McpTestInitializeServerInfo
		{
			public required string Name        { get; init; }
			public required string Title       { get; init; }
			public required string Description { get; init; }
		}

		protected sealed record McpTestToolsResult
		{
			public required List<McpTestTool> Tools { get; init; }
		}

		protected sealed record McpTestTool
		{
			public required string                 Name        { get; init; }
			public required string                 Description { get; init; }
			public required McpTestToolAnnotations Annotations { get; init; }
			public required McpTestInputSchema     InputSchema { get; init; }
		}

		protected sealed record McpTestToolAnnotations
		{
			public required bool ReadOnlyHint    { get; init; }
			public required bool IdempotentHint  { get; init; }
			public required bool OpenWorldHint   { get; init; }
			public required bool DestructiveHint { get; init; }
		}

		protected sealed record McpTestInputSchema
		{
			public required McpTestInputProperties Properties { get; init; }
			public          List<string>?          Required   { get; init; }
		}

		protected sealed record McpTestInputProperties
		{
			public JsonElement? Sql                { get; init; }
			public JsonElement? Provider           { get; init; }
			public JsonElement? ConnectionString   { get; init; }
			public JsonElement? Password           { get; init; }
			public JsonElement? Credentials { get; init; }
			public JsonElement? ProviderLocation   { get; init; }
			public JsonElement? DetailLevel        { get; init; }
			public JsonElement? FilterTables       { get; init; }
			public JsonElement? ExcludeTables      { get; init; }
			public JsonElement? IncludeTables      { get; init; }
			public JsonElement? GetProcedures      { get; init; }
			public JsonElement? UseSchemaOnly      { get; init; }
			public JsonElement? OutputFile         { get; init; }
			public JsonElement? AllowUnsafeSql     { get; init; }
			public JsonElement? AllowExecute       { get; init; }
		}
	}
}
