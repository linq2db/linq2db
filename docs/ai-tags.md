# AI Tags for API Documentation

`AI-Tags` are compact metadata annotations embedded in XML documentation (`<remarks>`) for public APIs.

They are intended for:
- LLM/agent tooling,
- semantic indexing,
- quick API behavior classification.

## Canonical format

Use a single-line key-value list:

`AI-Tags: Key=Value; Key=Value; ...;`

Example:

`AI-Tags: Group=DML; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;`

## Standard keys

- `Group` - primary API category.
- `Execution` - when execution happens.
- `Composability` - whether API returns composable query shape or is terminal.
- `Affects` - main semantic artifact affected by the call.
- `Pipeline` - affected translation/execution stages.
- `Provider` - provider dependency level.

## Controlled values (current baseline)

### `Group`
- `QueryDirectives`
- `NavigationLoading`
- `Hints`
- `DML`
- `Merge`
- `Helpers`

### `Execution`
- `Deferred`
- `Immediate`

### `Composability`
- `Composable`
- `Terminal`

### `Provider`
- `ProviderDefined`
- `ProviderAgnostic`

## Authoring rules

1. Keep one `AI-Tags` block per API member.
2. Keep vocabulary stable; avoid introducing synonyms.
3. Prefer extending controlled values in this document before using new values in code.
4. If API semantics are multi-modal (e.g., provider-dependent execution shape), encode the dominant behavior and explain details in regular XML remarks.
5. Keep tags behavior-focused (execution/composability/semantic impact), not implementation-detail-focused.

## Scope guidance

Prioritize tagging for:
- high-level public APIs (`DataExtensions`, `LinqExtensions`),
- APIs that switch query semantics,
- APIs where deferred vs immediate execution is easy to misinterpret,
- provider-sensitive APIs.

## Notes

`AI-Tags` complement XML documentation; they do not replace human-readable API docs.
