# OneWaySyncV2

OneWaySyncV2 is a command-line tool for one-way directory synchronization.

After each synchronization cycle, the replica directory is updated to match the source directory as closely as possible.

## Features

- One-way synchronization from `source` to `replica`
- Periodic synchronization
- File create, update and delete operations
- Empty directory synchronization
- SHA-256 content comparison when metadata differs
- Console logging
- File logging
- Graceful shutdown via `Ctrl+C`
- CLI argument validation

## Synchronization behavior

The synchronization is one-way:

```text
source -> replica

The replica directory is modified to match the source directory.

This means:

files missing in replica are copied from source
files changed in source are updated in replica
files existing only in replica are deleted
directories missing in replica are created
directories existing only in replica are deleted

Changes made directly in replica are not preserved.

CLI usage
OneWaySyncV2.Cli --source <path> --replica <path> --interval-seconds <seconds> --log-file <path>
Arguments
Argument	Description
--source	Source directory. Must exist.
--replica	Replica directory. Created if it does not exist.
--interval-seconds	Synchronization interval in seconds. Must be greater than 0.
--log-file	Path to the log file. Parent directory is created if needed.
--help, -h	Shows help output.
Example
Windows
dotnet run --project src/OneWaySyncV2.Cli -- ^
  --source "C:\Data\Source" ^
  --replica "C:\Data\Replica" ^
  --interval-seconds 30 ^
  --log-file "C:\Logs\one-way-sync.log"
Linux / macOS
dotnet run --project src/OneWaySyncV2.Cli -- \
  --source "/data/source" \
  --replica "/data/replica" \
  --interval-seconds 30 \
  --log-file "/var/log/one-way-sync.log"
Help
dotnet run --project src/OneWaySyncV2.Cli -- --help
Validation rules

The application validates CLI arguments before synchronization starts.

Rules:

source must exist
replica is created if it does not exist
source and replica must be different directories
source cannot be inside replica
replica cannot be inside source
interval-seconds must be greater than 0
the parent directory for log-file is created automatically
Logging

The application logs:

application start and stop
each synchronization cycle
every create, update and delete operation
recoverable file system errors

Logs are written to:

console
configured log file

Example log output:

[2026-04-30T09:42:34.6542297+02:00] [Information] OneWaySync started. Press Ctrl+C to stop.
[2026-04-30T09:42:34.7000000+02:00] [Information] Sync cycle started.
[2026-04-30T09:42:34.8000000+02:00] [Information] Create: documents/report.txt
[2026-04-30T09:42:34.9000000+02:00] [Information] Sync cycle completed.

If a file is locked or cannot be accessed, the error is logged and synchronization continues where possible.

Example:

[2026-04-30T09:42:34.6542297+02:00] [Error] Skipped Create for 'locked-file.txt'. Reason: The process cannot access the file because it is being used by another process.
Architecture

The solution follows a layered Clean Architecture style.

src/
  OneWaySyncV2.Domain/
  OneWaySyncV2.Application/
  OneWaySyncV2.Infrastructure/
  OneWaySyncV2.Cli/
Domain

Contains synchronization domain models:

SyncPlan
SyncOperation
SyncOperationType
Application

Contains application logic and abstractions:

sync planning
sync execution
sync runner
file system abstraction
logging abstraction
hashing abstraction
Infrastructure

Contains concrete implementations:

local file system access
SHA-256 file hashing
console and file logging
CLI

Contains presentation concerns:

CLI argument parsing
validation
help output
dependency injection composition
graceful shutdown
Build
dotnet build
Run tests
dotnet test
Test types

The solution contains:

unit tests for sync planning, execution and CLI validation
integration tests for real file system synchronization
CLI smoke test
Known limitations
File locking is handled as a recoverable error. Locked files are skipped and retried in the next cycle.
The tool does not currently support ignore patterns.
The tool does not currently support dry-run mode.
The tool does not currently perform log rotation.
The replica directory is intentionally destructive: files not present in source are deleted from replica.
Exit codes
Code	Meaning
0	Success
1	Invalid CLI arguments
2	Runtime error
Example workflow
dotnet build

dotnet test

dotnet run --project src/OneWaySyncV2.Cli -- \
  --source "./source" \
  --replica "./replica" \
  --interval-seconds 10 \
  --log-file "./logs/sync.log"

Stop the application with:

Ctrl+C


```