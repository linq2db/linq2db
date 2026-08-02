# CLAUDE.md

<!-- Trampoline: do not edit. The instruction corpus is the .claude/ submodule
     (github.com/linq2db/agents) - edit the files there, not this pointer.

     The import list below is the always-loaded set declared by .claude/CLAUDE.md. It lives
     here, at the project root, because a nested import's resolution root (importing file vs.
     project root) is unspecified; at the root both readings coincide. Adding an always-loaded
     corpus file is therefore the one corpus change that also needs a linq2db commit.

     If these imports don't load, .claude/ is an empty submodule directory:
       git submodule update --init
       git -C .claude switch master          # init leaves a detached HEAD
       git config core.hooksPath .githooks   # corpus auto-refresh + gitlink/trampoline guards
     ...and then start a NEW session: these imports resolve once, at session start, and are not
     re-read when .claude/ appears, so populating the submodule does not repair the session that
     found it empty (skill discovery may refresh on its own; the instruction set does not). -->

@.claude/AGENTS.md
@.claude/CLAUDE.md
@.claude/docs/agent-rules.md
