$BaseUrl = "http://localhost:8765"
$ErrorActionPreference = "Continue"

Write-Host "=== Stage 3 Advanced VFX Test ===" -ForegroundColor Cyan
Write-Host ""

function Test-Step {
    param($Number, $Description, $ScriptBlock)
    Write-Host "$Number. $Description..." -ForegroundColor Yellow
    try {
        $result = & $ScriptBlock
        $json = $result | ConvertTo-Json -Compress
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

# 2. Create Magic Portal
Test-Step -Number 2 -Description "Create Magic Portal" -ScriptBlock {
    $body = @{
        effectName = "AI_Test_Magic_Portal"
        mainColor = "#33AAFF"
        radius = 2.0
        duration = 5.0
        loop = $true
        saveAsPrefab = $true
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/create-magic-portal" -Method Post -Body $body -ContentType "application/json"
}

# 3. Create Fire Explosion
Test-Step -Number 3 -Description "Create Fire Explosion" -ScriptBlock {
    $body = @{
        effectName = "AI_Test_Fire_Explosion"
        radius = 2.0
        intensity = 1.0
        duration = 1.2
        saveAsPrefab = $true
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/create-fire-explosion" -Method Post -Body $body -ContentType "application/json"
}

# 4. Create Lightning Hit
Test-Step -Number 4 -Description "Create Lightning Hit" -ScriptBlock {
    $body = @{
        effectName = "AI_Test_Lightning_Hit"
        mainColor = "#AA33FF"
        height = 4.0
        radius = 1.0
        duration = 0.8
        branchCount = 5
        saveAsPrefab = $true
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/create-lightning-hit" -Method Post -Body $body -ContentType "application/json"
}

# 5. Create Heal Aura
Test-Step -Number 5 -Description "Create Heal Aura" -ScriptBlock {
    $body = @{
        effectName = "AI_Test_Heal_Aura"
        mainColor = "#55FF88"
        radius = 2.0
        duration = 4.0
        loop = $true
        saveAsPrefab = $true
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/create-heal-aura" -Method Post -Body $body -ContentType "application/json"
}

# 6. Create Smoke Burst
Test-Step -Number 6 -Description "Create Smoke Burst" -ScriptBlock {
    $body = @{
        effectName = "AI_Test_Smoke_Burst"
        color = "#777777"
        radius = 2.0
        duration = 2.5
        density = 1.0
        saveAsPrefab = $true
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/create-smoke-burst" -Method Post -Body $body -ContentType "application/json"
}

# 7. Create Slash Trail
Test-Step -Number 7 -Description "Create Slash Trail" -ScriptBlock {
    $body = @{
        effectName = "AI_Test_Slash_Trail"
        mainColor = "#66CCFF"
        length = 3.0
        width = 0.3
        duration = 0.5
        saveAsPrefab = $true
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$BaseUrl/create-slash-trail" -Method Post -Body $body -ContentType "application/json"
}

# 8. List Scene Objects
Test-Step -Number 8 -Description "List Scene Objects" -ScriptBlock {
    Invoke-RestMethod -Uri "$BaseUrl/list-scene-objects" -Method Get
}

Write-Host "=== All Stage 3 tests passed! ===" -ForegroundColor Cyan
