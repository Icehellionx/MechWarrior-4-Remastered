# Auxiliary model catalog

Updated: 2026-09-15. Names come from the ignored local `.env`; no credential values are recorded here.

## Featherless routes

| Role | Model |
|---|---|
| adviser | `zai-org/GLM-5.2` |
| adversary | `deepseek-ai/DeepSeek-V4-Pro` |
| coder | `Qwen/Qwen3-Coder-Next` |
| balanced | `Qwen/Qwen3.8-27B` |
| fast | `deepseek-ai/DeepSeek-V4-Flash-0731` |
| long-context | `MiniMaxAI/MiniMax-M3` |

## Local Ollama routes

| Role | Model |
|---|---|
| adviser | `qwen3:8b` |
| adversary | `mistral:7b-instruct` |
| balanced | `gemma2:9b` |
| fast | `llama3.1:8b` |
| coder | `qwen3-coder:30b` |
| fast-coder | `qwen2.5-coder:7b-instruct` |
| vision | `qwen2.5vl:7b` |
| fast-vision | `gemma3:4b` |
| embedding | `nomic-embed-text:latest` |

## Health record

Run `node tools/agent/aux-model.mjs --check` for local availability and configuration. Use `--live-remote` only for a bounded paid probe. Record date, router version, reachable provider, and installed/model availability; never log API keys or full provider responses containing private data.

- 2026-09-15: router `1.0.0`; all four unit tests passed. Ollama was reachable and all nine cataloged local models were installed. Featherless credentials and every remote role were configured, but no paid live completion was sent during this check.
- 2026-09-17: router `1.0.0`; the same nine local models were reachable. Local adversary `mistral:7b-instruct` and adviser `qwen3:8b` reviewed the 0.6.23 polish slice. The bounded Featherless adversary request was attempted without private paths, credentials, binaries, or proprietary data but returned HTTP 401 (`You must be signed in`), so no remote advice was accepted. Local findings were independently classified: archive hash/record-count/source-immutability/rollback and full-PDF render concerns are already covered; multi-monitor, lower-end GPU, real roster visibility, cinematics, gameplay, and Alt-Tab remain valid field gates.
- 2026-09-17 post-`0.6.25` correction: bounded local adversary and adviser calls both timed out without output, and the sanitized Featherless adversary route returned HTTP 401 (`You must be signed in`). No auxiliary advice was accepted. Primary evidence remains the official dgVoodoo contract, exact executable disassembly/transforms, focused tests, reproducible private stages, and the still-open interactive gates.
