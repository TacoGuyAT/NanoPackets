; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
NP0001  | codegen  | Error    | Multiple server definitions
NP0002  | codegen  | Error    | Multiple client definitions
NP0003  | codegen  | Error    | Couldn't find server
NP0004  | codegen  | Error    | Couldn't find client
NP0005  | codegen  | Warning  | Packet has no serializable fields
NP0006  | codegen  | Error    | Packet must be partial
