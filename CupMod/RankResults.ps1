param(
    [string]$EventId,
    [string]$Results = (Join-Path (Split-Path -Parent $PSScriptRoot) 'UserData\AnnihilatedCup\results')
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Results)) { throw "Result directory does not exist: $Results" }
$rows = foreach ($file in Get-ChildItem -LiteralPath $Results -File -Filter '*.json') {
    $row = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    if ($row.Schema -ne 1 -or $row.Status -notin @('clear', 'defeat', 'time_limit', 'wrong_ending', 'quit_to_title', 'required_choice_locked', 'wrong_story_branch')) { continue }
    if ($EventId -and $row.Settings.EventId -ne $EventId) { continue }
    $row
}
foreach ($match in $rows | Group-Object MatchHash) {
    $rank = 0
    $previous = ''
    $position = 0
    $ordered = $match.Group | Sort-Object -Property @(
        @{ Expression = { if ($_.Status -eq 'clear') { 0 } else { 1 } } }
        @{ Expression = { if ($_.Status -eq 'clear') { [long]$_.ElapsedMilliseconds } else { -[int]$_.Progress } } }
        'RunId'
    )
    foreach ($run in $ordered) {
        $position++
        $key = if ($run.Status -eq 'clear') { 'C' + $run.ElapsedMilliseconds } else { 'P' + $run.Progress }
        if ($key -ne $previous) { $rank = $position; $previous = $key }
        [pscustomobject]@{
            EventId = $run.Settings.EventId
            Match = $match.Name.Substring(0, 12)
            Rank = $rank
            RunCode = $run.RunCode
            RunId = $run.RunId
            Status = $run.Status
            Progress = $run.ProgressName
            Time = if ($run.Status -eq 'clear') { [TimeSpan]::FromMilliseconds($run.ElapsedMilliseconds).ToString('c') } else { '' }
        }
    }
}
