param([string]$ProjectRoot = 'C:\Users\angel\UnityProjects\PG', [string]$Stage = "$env:TEMP\PG-FullExperience\Assets")
$ErrorActionPreference = 'Stop'
$assets = Join-Path $ProjectRoot 'Assets'
$utf8 = New-Object Text.UTF8Encoding($false)
$manifest = [Collections.Generic.List[object]]::new()
function Save-Staged([string]$relative, [string]$text) {
    $target = Join-Path $assets $relative
    $staged = Join-Path $Stage $relative
    New-Item -ItemType Directory -Path (Split-Path $staged) -Force | Out-Null
    [IO.File]::WriteAllText($staged,$text,$utf8)
    $hash = if(Test-Path -LiteralPath $target) { (Get-FileHash -LiteralPath $target).Hash } else { $null }
    $manifest.Add([pscustomobject]@{Target=$target;Staged=$staged;OriginalHash=$hash})
}
function Asset([string]$name,[string]$script,[string]$fields) {
    return "%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n--- !u!114 &11400000`nMonoBehaviour:`n  m_ObjectHideFlags: 0`n  m_CorrespondingSourceObject: {fileID: 0}`n  m_PrefabInstance: {fileID: 0}`n  m_PrefabAsset: {fileID: 0}`n  m_GameObject: {fileID: 0}`n  m_Enabled: 1`n  m_EditorHideFlags: 0`n  m_Script: {fileID: 11500000, guid: $script, type: 3}`n  m_Name: $name`n  m_EditorClassIdentifier: `n$fields"
}
function New-Asset([string]$relative,[string]$guid,[string]$content) {
    Save-Staged $relative $content
    Save-Staged ($relative+'.meta') "fileFormatVersion: 2`nguid: $guid`nNativeFormatImporter:`n  externalObjects: {}`n  mainObjectFileID: 11400000`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
}
$activation='8f976cf0f1d94405a66e9b963c299e56'
$fade='01a98b27a1e64d09afbd7b686d842243'
$sequence='e67c34a6df844960ad1b025f8eaeae8a'
$finalSequence='ff94d256832249d1bb340d3f0c211fc9'
New-Asset 'ScriptableObjects\EventChannels\Application\ExperienceSceneActivation.asset' $activation (Asset 'ExperienceSceneActivation' 'ea1b768896e34bdb926c271a44f6a3e7' '')
New-Asset 'ScriptableObjects\Experiences\CameraFade.asset' $fade (Asset 'CameraFade' '09f5b0b335174bf58bdde87827cad2c9' "  fadeOutSeconds: 0.25`n  fadeInSeconds: 0.35`n  color: {r: 0, g: 0, b: 0, a: 1}`n  overlayShader: {fileID: 4800000, guid: f2c8234077a84edab73726488031d31a, type: 3}`n")
$sceneGuids=@{}
foreach($name in @('Alex','Joaquin','Shipi','JuanAndres','Danniel','Jeremy')) {
    $meta=[IO.File]::ReadAllText((Join-Path $assets "ScriptableObjects\ExperienceScenes\SCN_$name.asset.meta"))
    $sceneGuids[$name]=[regex]::Match($meta,'guid: (\w+)').Groups[1].Value
}
function Sequence-Fields($entries) {
    $result="  segments:`n"
    foreach($e in $entries) {
        $guid=$sceneGuids[$e[0]]
        $result+="  - Scene: {fileID: 11400000, guid: $guid, type: 2}`n    StartTime: $($e[1])`n    EndTime: $($e[2])`n    Transition: {fileID: 11400000, guid: $fade, type: 2}`n"
    }
    return $result+"  preloadLeadSeconds: 10`n  holdLastSceneUntilSongEnds: 1`n"
}
New-Asset 'ScriptableObjects\Experiences\SEQ_Punko_Test.asset' $sequence (Asset 'SEQ_Punko_Test' '1f3c886fd40440e7bd956ec1f82fcb66' (Sequence-Fields @(@('Alex',0,31),@('Joaquin',31,125),@('Danniel',125,156),@('Jeremy',156,187))))
New-Asset 'ScriptableObjects\Experiences\SEQ_Punko_Final.asset' $finalSequence (Asset 'SEQ_Punko_Final' '1f3c886fd40440e7bd956ec1f82fcb66' (Sequence-Fields @(@('Alex',0,31),@('Joaquin',31,62),@('Shipi',62,94),@('JuanAndres',94,125),@('Danniel',125,156),@('Jeremy',156,187))))
$exp=[IO.File]::ReadAllText((Join-Path $assets 'ScriptableObjects\Experiences\EXP_Punko.asset'))
Save-Staged 'ScriptableObjects\Experiences\EXP_Punko.asset' ($exp+"  fullSequence: {fileID: 11400000, guid: $sequence, type: 2}`n")

# Wrap scene roots without unpacking prefabs or changing existing local transforms/references.
foreach($name in @('Alex','Joaquin','Danniel','Jeremy')) {
    $relative="Scenes\Skeepers\$name.unity"
    $text=[IO.File]::ReadAllText((Join-Path $assets $relative)).Replace("`r`n","`n")
    $docs=[regex]::Matches($text,'(?ms)^--- !u!(\d+) &(\d+)([^\n]*)\n.*?(?=^--- !u!|\z)')
    $byId=@{}; foreach($doc in $docs) {$byId[$doc.Groups[2].Value]=$doc.Value}
    $boot=@($docs | Where-Object {$_.Value -match 'guid: eaa6b2cacddd6674481841d251c26da6'})
    if($boot.Count -ne 1) {throw "$name must have one bootstrap"}
    $bootGo=[regex]::Match($boot[0].Value,'m_GameObject: \{fileID: (\d+)\}').Groups[1].Value
    $bootTransform=@($docs | Where-Object { $_.Groups[1].Value -eq '4' -and $_.Value -match "m_GameObject: \{fileID: $bootGo\}" })[0].Groups[2].Value
    $rootsDoc=@($docs | Where-Object {$_.Groups[1].Value -eq '1660057539'})[0]
    $roots=[regex]::Matches($rootsDoc.Value,'- \{fileID: (\d+)\}')
    $children=[Collections.Generic.List[string]]::new(); $extra=''; $id=9000000010L
    foreach($root in $roots) {
        $rootId=$root.Groups[1].Value
        if($rootId -eq $bootTransform){continue}
        $doc=$byId[$rootId]
        if($doc -match '^--- !u!1001') {
            $prefabGuid=[regex]::Match($doc,'m_SourcePrefab: \{fileID: \d+, guid: (\w+)').Groups[1].Value
            $metaPath=@(& rg -l --fixed-strings "guid: $prefabGuid" $assets -g '*.meta')[0]
            if(-not $metaPath){throw "Missing prefab $prefabGuid"}
            $prefab=[IO.File]::ReadAllText($metaPath.Substring(0,$metaPath.Length-5))
            $rootTransform=@([regex]::Matches($prefab,'(?ms)^--- !u!4 &(\d+)\n.*?(?=^--- !u!|\z)') | Where-Object {$_.Value -match 'm_Father: \{fileID: 0\}'})
            if($rootTransform.Count -ne 1){throw "Ambiguous prefab root $prefabGuid"}
            $sourceTransform=$rootTransform[0].Groups[1].Value
            $stripped=@($docs | Where-Object { $_.Groups[1].Value -eq '4' -and $_.Value -match "m_PrefabInstance: \{fileID: $rootId\}" -and $_.Value -match "m_CorrespondingSourceObject: \{fileID: $sourceTransform," })
            if($stripped.Count -gt 0){$childId=$stripped[0].Groups[2].Value}
            else {
                $childId=($id++).ToString()
                $extra+="--- !u!4 &$childId stripped`nTransform:`n  m_CorrespondingSourceObject: {fileID: $sourceTransform, guid: $prefabGuid, type: 3}`n  m_PrefabInstance: {fileID: $rootId}`n  m_PrefabAsset: {fileID: 0}`n"
            }
            $text=$text.Replace($doc,$doc.Replace('m_TransformParent: {fileID: 0}','m_TransformParent: {fileID: 9000000002}'))
            $children.Add($childId)
        } else {
            if($doc -notmatch 'm_Father: \{fileID: 0\}') {throw "Unexpected root $rootId"}
            $text=$text.Replace($doc,$doc.Replace('m_Father: {fileID: 0}','m_Father: {fileID: 9000000002}'))
            $children.Add($rootId)
        }
    }
    $bootText=$boot[0].Value
    $equip=@($docs | Where-Object {$_.Value -match 'guid: 1c0764f1c69c95742b9110ecd02180b6'})
    $runtimeExtra=($equip | ForEach-Object {"  - {fileID: $($_.Groups[2].Value)}`n"}) -join ''
    $bootText=$bootText.Replace('  experienceReady:',($runtimeExtra+'  experienceReady:'))
    $bullets=@($docs | Where-Object {$_.Value -match 'guid: e692a127c92adf0419d93b5547073ad2'})
    $preloadExtra=($bullets | ForEach-Object {"  - {fileID: $($_.Groups[2].Value)}`n"}) -join ''
    $bootText=$bootText.Replace('  runtimeSystems:',($preloadExtra+'  runtimeSystems:'))
    $bootText+="  gameplayRoot: {fileID: 9000000001}`n  sceneActivation: {fileID: 11400000, guid: $activation, type: 2}`n"
    $text=$text.Replace($boot[0].Value,$bootText)
    $childList=($children | ForEach-Object {"  - {fileID: $_}`n"}) -join ''
    $wrapper=@"
--- !u!1 &9000000001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 9000000002}
  m_Layer: 0
  m_Name: Experience Content
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0
--- !u!4 &9000000002
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9000000001}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
${childList}  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
"@
    $newRoots="--- !u!1660057539 &9223372036854775807`nSceneRoots:`n  m_ObjectHideFlags: 0`n  m_Roots:`n  - {fileID: $bootTransform}`n  - {fileID: 9000000002}`n"
    $text=$text.Replace($rootsDoc.Value,($extra+$wrapper+"`n"+$newRoots))
    Save-Staged $relative $text
    Write-Output "$name wrapped $($children.Count) roots; $($equip.Count) weapon runtimes; $($bullets.Count) bullet preloaders."
}

$relative='Scenes\Core\Bootstrap.unity'
$text=[IO.File]::ReadAllText((Join-Path $assets $relative)).Replace("`r`n","`n")
$text=$text.Replace('  - component: {fileID: 1101344458}',"  - component: {fileID: 1101344458}`n  - component: {fileID: 9000000101}`n  - component: {fileID: 9000000102}")
$text=$text.Replace('  experienceCoreScene: ExperienceCore',"  experienceCoreScene: ExperienceCore`n  fullExperienceDirector: {fileID: 9000000101}")
foreach($pair in @(@('9000000101','beda4d7c7f95424ca81f1db5a58e37bd'),@('9000000102','93516b0e25584e0b80041f42fcacb719'))) {
    $text+="--- !u!114 &$($pair[0])`nMonoBehaviour:`n  m_ObjectHideFlags: 0`n  m_CorrespondingSourceObject: {fileID: 0}`n  m_PrefabInstance: {fileID: 0}`n  m_PrefabAsset: {fileID: 0}`n  m_GameObject: {fileID: 1101344456}`n  m_Enabled: 1`n  m_EditorHideFlags: 0`n  m_Script: {fileID: 11500000, guid: $($pair[1]), type: 3}`n  m_Name: `n  m_EditorClassIdentifier: `n"
    if($pair[0] -eq '9000000101') {
        $text+="  experienceReady: {fileID: 11400000, guid: f4b8f7b73053533419eb97071978892d, type: 2}`n  experienceCompleted: {fileID: 11400000, guid: d0711fd40f6603a4f8f8bf3ae2315306, type: 2}`n  mainMenuRequested: {fileID: 11400000, guid: 79709713fbe723e48b79c6b3004f8758, type: 2}`n  sceneActivation: {fileID: 11400000, guid: $activation, type: 2}`n"
    }
}
Save-Staged $relative $text
$manifest | ConvertTo-Json | Set-Content (Join-Path (Split-Path $Stage) 'asset-manifest.json') -Encoding UTF8
