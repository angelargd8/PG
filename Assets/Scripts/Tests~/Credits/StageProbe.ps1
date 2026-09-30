param([string]$ProbePath = (Join-Path $env:TEMP 'PG-Credits-Validation'))
$ErrorActionPreference = 'Stop'
$scriptsRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$projectRoot = [IO.Path]::GetFullPath((Join-Path $scriptsRoot '../..'))
if (Test-Path -LiteralPath $ProbePath) { throw 'Use a new temporary project path.' }
foreach ($folder in @('Assets/Runtime','Assets/Editor','Packages','ProjectSettings')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $ProbePath $folder) | Out-Null
}
$files = @(
    'Application/AppState.cs','Application/AppStateMachine.cs','SceneManagement/SceneFlowManager.cs',
    'Events/ExperienceDenitionSO.cs','Events/SequenceDirector.cs',
    'Events/Channels/VoidEventChannelSO.cs','Events/Channels/BoolEventChannel.cs',
    'Events/Channels/ExperienceEventChannelSO.cs','Events/Channels/ExperienceSceneActivationEventChannelSO.cs',
    'Events/Channels/InteractionResultEventChannelSO.cs',
    'Beat System/ExperienceMusicClock.cs',
    'Experience/ExperienceSequenceSO.cs','Experience/ExperienceSceneDefinitionSO.cs','Experience/ExperienceRequest.cs',
    'Experience/ExperienceSceneBoostrap.cs','Experience/ExperiencePreloadOperation.cs',
    'Experience/IExperiencePreloadable.cs','Experience/IExperienceRuntime.cs','Experience/FullExperienceDirector.cs',
    'Experience/ExperienceTransitionSO.cs','Experience/CameraFadeTransitionSO.cs',
    'Experience/ExperienceTransitionView.cs','Experience/ExperienceCameraFade.shader',
    'Metrics & DDA/InteractionResult.cs','Metrics & DDA/InteractionType.cs',
    'Metrics & DDA/InteractionOutcome.cs','Metrics & DDA/DifficultyLevel.cs',
    'UI/ResultsScreenController.cs','UI/VRLoadingCanvasBinder.cs',
    'Score/ScoreSystem.cs','Score/ScoreProfileSO.cs','Score/ScoreProfileEventChannelSO.cs',
    'Score/ScoreChangedEventChannelSO.cs','Score/ScoreChange.cs','Score/ScoreEvaluation.cs',
    'Score/ScoreBonus.cs','Score/ScoreBonusEventChannelSO.cs','Score/TimingJudgement.cs',
    'Score/FinalScoreEventChannelSO.cs','Score/RunResult.cs','Score/RunResultEventChannelSO.cs',
    'Score/ScoreRunContext.cs','Score/ResultsScreenFollower.cs',
    'Tests~/FullExperience/FullProbeRuntime.cs'
)
foreach ($file in $files) {
    Copy-Item -LiteralPath (Join-Path $scriptsRoot $file) -Destination (Join-Path $ProbePath 'Assets/Runtime')
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'CreditsProbeSetup.cs'),(Join-Path $PSScriptRoot 'CreditsProbeRunner.cs') -Destination (Join-Path $ProbePath 'Assets/Runtime')
Copy-Item -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $ProbePath 'ProjectSettings')
$dependencies = [ordered]@{'com.unity.ugui'='2.0.0';'com.unity.timeline'='1.8.10';'com.unity.test-framework'='1.6.0'}
$sourceManifest = Get-Content (Join-Path $projectRoot 'Packages/manifest.json') -Raw | ConvertFrom-Json
foreach ($entry in $sourceManifest.dependencies.PSObject.Properties) {
    if ($entry.Name.StartsWith('com.unity.modules.')) { $dependencies[$entry.Name] = $entry.Value }
}
[IO.File]::WriteAllText((Join-Path $ProbePath 'Packages/manifest.json'), (@{dependencies=$dependencies} | ConvertTo-Json -Depth 4), [Text.UTF8Encoding]::new($false))
# Embed cached packages so the probe can run without fetching these sources again.
foreach ($package in @('com.unity.ugui','com.unity.timeline')) {
    $cached = Get-ChildItem (Join-Path $projectRoot 'Library/PackageCache') -Directory -Filter ($package+'@*') | Select-Object -First 1
    if ($null -eq $cached) { throw "Missing cached package: $package" }
    Copy-Item -LiteralPath $cached.FullName -Destination (Join-Path $ProbePath ('Packages/'+$package)) -Recurse
}
Write-Output $ProbePath
