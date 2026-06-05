$BaseUrl = "http://localhost:8765"
$ErrorActionPreference = "Continue"

Write-Host "=== Stage 4 Preview / Template / Workflow Test ===" -ForegroundColor Cyan
Write-Host ""

function Test-Step {
    param($Number, $Description, $ScriptBlock)
    Write-Host "$Number. $Description..." -ForegroundColor Yellow
    try {
        $result = & $ScriptBlock
        $json = $result | ConvertTo-Json -Compress -Depth 5
        Write-Host "   Response: $json" -ForegroundColor Gray
        if (-not $result.success) { throw "FAILED: $($result.message)" }
        Write-Host "   OK" -ForegroundColor Green
    } catch {
        Write-Host "   FAILED: $_" -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}

# 1. Ping
Test-Step -Number 1 -Description "Ping Unity" -ScriptBlock {
    Invoke-RestMethod -Uri "$BaseUrl/ping" -Method Get
}

# 2. Create Magic Portal for testing
Test-Step -Number 2 -Description "Create Magic Portal AI_Stage4_Portal" -ScriptBlock {
    $body = @{
        effectName = "AI_Stage4_Portal"
        mainColor = "#33AAFF"
        radius = 2.0
        duration = 5.0
        loop = $true
        saveAsPrefab = $true
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/create-magic-portal" -Method Post -Body $body -ContentType "application/json"
}

# 3. Focus Scene Object
Test-Step -Number 3 -Description "Focus scene object AI_Stage4_Portal" -ScriptBlock {
    $body = @{ objectName = "AI_Stage4_Portal" } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/focus-scene-object" -Method Post -Body $body -ContentType "application/json"
}

# 4. Play Effect
Test-Step -Number 4 -Description "Play effect AI_Stage4_Portal" -ScriptBlock {
    $body = @{ objectName = "AI_Stage4_Portal"; includeChildren = $true } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/play-effect" -Method Post -Body $body -ContentType "application/json"
}

# 5. Capture View (Scene)
Test-Step -Number 5 -Description "Capture SceneView to AI_Stage4_Portal_Capture" -ScriptBlock {
    $body = @{
        fileName = "AI_Stage4_Portal_Capture"
        viewType = "scene"
        width = 1280
        height = 720
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/capture-view" -Method Post -Body $body -ContentType "application/json"
}

# 6. Stop Effect
Test-Step -Number 6 -Description "Stop effect AI_Stage4_Portal" -ScriptBlock {
    $body = @{ objectName = "AI_Stage4_Portal"; includeChildren = $true; clearParticles = $true } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/stop-effect" -Method Post -Body $body -ContentType "application/json"
}

# 7. List Generated Assets
Test-Step -Number 7 -Description "List generated assets (all)" -ScriptBlock {
    $body = @{ assetType = "all" } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/list-generated-assets" -Method Post -Body $body -ContentType "application/json"
}

# 8. Instantiate Prefab
Test-Step -Number 8 -Description "Instantiate prefab -> AI_Stage4_Portal_Instance" -ScriptBlock {
    $body = @{
        prefabPath = "Assets/AI_Generated/Prefabs/AI_Stage4_Portal.prefab"
        objectName = "AI_Stage4_Portal_Instance"
        x = 5
        y = 0
        z = 0
        scale = 1.0
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/instantiate-prefab" -Method Post -Body $body -ContentType "application/json"
}

# 9. Get Object Info
Test-Step -Number 9 -Description "Get object info for AI_Stage4_Portal_Instance" -ScriptBlock {
    $body = @{ objectName = "AI_Stage4_Portal_Instance"; includeChildren = $true } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/get-object-info" -Method Post -Body $body -ContentType "application/json"
}

# 10. Create VFX from Template
Test-Step -Number 10 -Description "Create VFX from template (reuse prefab as template)" -ScriptBlock {
    $body = @{
        templatePath = "Assets/AI_Generated/Prefabs/AI_Stage4_Portal.prefab"
        outputName = "AI_Stage4_Template_Copy"
        x = -5
        y = 0
        z = 0
        scale = 1.0
        mainColor = "#FF33AA"
        saveAsPrefab = $true
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/create-vfx-from-template" -Method Post -Body $body -ContentType "application/json"
}

# 11. List Scene Objects
Test-Step -Number 11 -Description "List scene objects" -ScriptBlock {
    Invoke-RestMethod -Uri "$BaseUrl/list-scene-objects" -Method Get
}

Write-Host ""
Write-Host "=== All Stage 4 tests passed! ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "To clean up AI_* objects from scene, run:" -ForegroundColor Yellow
Write-Host '  $body = @{ prefix = "AI_" } | ConvertTo-Json' -ForegroundColor Gray
Write-Host '  Invoke-RestMethod -Uri "http://localhost:8765/clear-ai-generated-scene-objects" -Method Post -Body $body -ContentType "application/json"' -ForegroundColor Gray
