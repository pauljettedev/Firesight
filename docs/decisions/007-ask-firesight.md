# ADR 007: Ask Firesight: the AI picks the query, the code gives the answer

**Status:** Accepted

## Decision

Ask Firesight uses Claude (Messages API with tool use) to turn a question into calls to five
Firesight tools: the three MCP tools, plus place-name lookup and sync state. Claude only chooses which tool to call and what kind of
answer is wanted (count, yes/no, status, records). Firesight's own code works out the answer
from the tool results, and fire details on screen always come from the Firesight API.

## Why

Language models can miscount or misstate facts. Earlier versions let the model write the
answer, and it was sometimes wrong. Letting code produce the answer keeps it accurate.

## What this means

- The model never touches the database. Its tools call the Application services (ADR 005).
- At most 4 tool rounds per question, with a bounded response length.
- Per-IP rate limits protect the Claude bill. The API key stays on the server.
- `AnthropicClient` is registered once for the whole app (a singleton), since it's stateless
  and safe to share.
