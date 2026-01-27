# Phase 7: User Story 5 - Export Metrics for External Analysis

## Summary

Implement two new functions (`Export-Metrics` and `Generate-CumulativeReport`) in the existing `agent-metrics.ps1` script, and wire them into the action switch. Tests (T058-T062) are already written and should fail (RED). Implementation makes them pass (GREEN).

## Files to Modify

1. **`.specify/scripts/powershell/agent-metrics.ps1`** - Add `Export-Metrics` and `Generate-CumulativeReport` functions; update action switch
2. **`.specify/scripts/powershell/agent-metrics.tests.ps1`** - Already modified (tests T058-T062 added)
3. **`specs/001-agent-metrics-enhancement/tasks.md`** - Mark T058-T069 as complete

## Implementation Steps

### Step 1: Verify tests fail (RED)

Run the new tests to confirm they fail before implementation:
```powershell
Invoke-Pester -Path .\.specify\scripts\powershell\agent-metrics.tests.ps1 -TestName "*Export*","*Cumulative*"
```

### Step 2: Implement `Export-Metrics` function (T063-T066)

Add function in `agent-metrics.ps1` (before the action switch block, around line 1485):

**Parameters**: `-Format` (CSV/JSON), `-OutputPath` (optional), `-IncludeArchives` (switch), `-Branch` (optional)

**Logic**:
1. Load active session invocations from `$MetricsFile`
2. If `-IncludeArchives`: also load all archive files from `archive/` dir, filter by `-Branch` if specified, filter by v2.0.0 schema using `Test-SchemaVersion`
3. Collect all invocations into a flat array
4. If no invocations found, write error message and return
5. **CSV format**: Write header row with 14 columns per contract (`Timestamp,Phase,FeatureBranch,Agent,Model,TaskId,Category,Status,Tokens,DurationMs,DurationSec,IsParallel,ParallelGroupId,Description`), then one row per invocation. `DurationSec` = `DurationMs / 1000`. Use `Set-Content` to write.
6. **JSON format**: Convert invocations array to JSON using `ConvertTo-Json -Depth 5`. Write with `Set-Content`.
7. If `-OutputPath` not provided, auto-generate: `metrics-export-YYYYMMDD-HHMMSS.csv/json` (or `metrics-export-all-*` with archives)
8. Output success message with file path and invocation count

### Step 3: Implement `Generate-CumulativeReport` function (T067-T069)

Add function in `agent-metrics.ps1`:

**Parameters**: `-Branch` (string, feature branch to aggregate)

**Logic**:
1. If `-Branch` not provided, detect from git (`git rev-parse --abbrev-ref HEAD`)
2. Load all archive files, filter by `featureBranch` match and v2.0.0 schema
3. Also load active session if it matches the branch
4. If no matching sessions, write error and return
5. Aggregate across all sessions:
   - Total invocations, tokens, successes, failures
   - Group by phase: count sessions, invocations, tokens, duration, success rate per phase
   - Combined model distribution (sum tokens per model, recalculate cost%)
   - Combined category distribution
6. Display formatted report:
   - Header: `CUMULATIVE FEATURE REPORT`
   - Feature branch name, period (earliest to latest), session count
   - `PHASE BREAKDOWN` table: Phase, Sessions, Invocations, Tokens, Duration, Success%
   - TOTAL row
   - `CUMULATIVE MODEL DISTRIBUTION` (if model data available)
   - `CUMULATIVE CATEGORY BREAKDOWN` (if category data available)
   - `FEATURE SUMMARY` with totals

### Step 4: Wire functions into action switch

Update the action switch (lines 1506-1513) to replace the placeholder stubs:

```powershell
"Cumulative" {
    Generate-CumulativeReport -Branch $FeatureBranch
}
"Export" {
    Export-Metrics -Format $Format -OutputPath $OutputPath -IncludeArchives:$IncludeArchives.IsPresent -Branch $FeatureBranch
}
```

Also add `-OutputPath` parameter to the `param()` block at the top of the script (currently missing).

### Step 5: Run tests (GREEN)

```powershell
# Run US5 tests
Invoke-Pester -Path .\.specify\scripts\powershell\agent-metrics.tests.ps1 -TestName "*Export*","*Cumulative*"

# Run full test suite to verify no regressions
Invoke-Pester -Path .\.specify\scripts\powershell\agent-metrics.tests.ps1
```

### Step 6: Update tasks.md

Mark tasks T058-T069 as `[X]` in `specs/001-agent-metrics-enhancement/tasks.md`.

## Verification

1. All 5 new tests (T058-T062) pass
2. All existing tests continue to pass (no regressions)
3. Manual smoke test:
   ```powershell
   .\agent-metrics.ps1 -Action Export -Format CSV
   .\agent-metrics.ps1 -Action Export -Format JSON -IncludeArchives
   .\agent-metrics.ps1 -Action Cumulative -FeatureBranch "001-agent-metrics-enhancement"
   ```

## Notes

- Tests are already written (RED phase complete)
- The `Export-Metrics` function needs to handle the CSV escaping of descriptions that may contain commas (wrap in quotes)
- The `Generate-CumulativeReport` function reuses patterns from existing `Generate-TrendsReport` (archive loading, schema validation, formatting)
- No new parameters needed in `param()` except `-OutputPath` (string)
