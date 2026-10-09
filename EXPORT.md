# linq2db Expert knowledge pack (export)

This branch holds the linq2db Expert knowledge pack (a pack for hosted assistants such as a
Custom GPT) as it existed on linq2db PR #5376 (branch `llm-architecture-support`), with every file
it needs, so that it can be moved to its own repository later. Paths are the same as in the linq2db
repository.

| Path | What |
|---|---|
| `Source/Knowledge/**` | The generated pack (`linq2db-expert/NN-*.md`, manifests, Custom GPT instructions) and its README. |
| `.agents/Build-LinqToDBExpert.ps1` | Generator of the pack. Inputs: `Source/Skills/linq2db/**` (the skill guides of the linq2db repo), `Source/LinqToDB/README.md`, and `linq2db.xml` from a Release build of linq2db. |
| `.agents/knowledge-pack-maintenance.md` | Maintenance procedure for the pack. |
| `.gitattributes` | The pack's `linguist-generated` lines, as they were in the linq2db `.gitattributes`. |
| `linq2db.slnx.fragment.xml` | The `Source/Knowledge` folder entries that were in `linq2db.slnx`. |
| `Source/Skills/linq2db/docs/api.md` | Skill input removed from the PR: the generated API extract (pack chapter 04). |
| `Source/Skills/linq2db/docs/ai-tags.md` | Skill input removed from the PR: the AI-tags vocabulary (part of pack chapter 06). |
| `Build/GenerateApiDocs.ps1` | Generator of `api.md`, removed from the PR together with it. |

The remaining generator inputs (`Source/Skills/linq2db/**` guides, `Source/LinqToDB/README.md`) stay
in the linq2db repository. The `[AiTags]` attributes that `GenerateApiDocs.ps1` and the pack
generator read were removed from linq2db; a regenerated pack carries no AI metadata.
