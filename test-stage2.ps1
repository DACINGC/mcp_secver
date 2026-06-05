$BaseUrl = "http://localhost:8765"
$ErrorActionPreference = "Continue"

Write-Host "=== Stage 2 Material System Test ===" -ForegroundColor Cyan
Write-Host ""

# 1. Ping
Write-Host "1. Ping Unity..." -ForegroundColor Yellow
$result = Invoke-RestMethod -Uri "$BaseUrl/ping" -Method Get
Write-Host "   Response: $($result | ConvertTo-Json -Compress)" -ForegroundColor Gray
if (-not $result.success) { Write-Host "   FAILED!" -ForegroundColor Red; exit 1 }
Write-Host "   OK" -ForegroundColor Green
Write-Host ""

# 2. Create Empty
Write-Host "2. Creating empty object AI_Material_Test_Object..." -ForegroundColor Yellow
$body = @{ name = "AI_Material_Test_Object"; x = 0; y = 0; z = 0 } | ConvertTo-Json
$result = Invoke-RestMethod -Uri "$BaseUrl/create-empty" -Method Post -Body $body -ContentType "application/json"
Write-Host "   Response: $($result | ConvertTo-Json -Compress)" -ForegroundColor Gray
if (-not $result.success) { Write-Host "   FAILED!" -ForegroundColor Red; exit 1 }
Write-Host "   OK" -ForegroundColor Green
Write-Host ""

# 3. Create Material
Write-Host "3. Creating material AI_Test_Glow_Blue..." -ForegroundColor Yellow
$body = @{
    materialName = "AI_Test_Glow_Blue"
    color = "#3355FF"
    shaderName = "Universal Render Pipeline/Particles/Unlit"
    emissionColor = "#33AAFF"
    emissionIntensity = 3.0
} | ConvertTo-Json
$result = Invoke-RestMethod -Uri "$BaseUrl/create-material" -Method Post -Body $body -ContentType "application/json"
Write-Host "   Response: $($result | ConvertTo-Json -Compress)" -ForegroundColor Gray
if (-not $result.success) { Write-Host "   FAILED!" -ForegroundColor Red; exit 1 }
Write-Host "   OK" -ForegroundColor Green
Write-Host ""

# 4. Assign Material
Write-Host "4. Assigning material to AI_Material_Test_Object..." -ForegroundColor Yellow
$body = @{
    objectName = "AI_Material_Test_Object"
    materialPath = $result.assetPath
} | ConvertTo-Json
$result = Invoke-RestMethod -Uri "$BaseUrl/assign-material" -Method Post -Body $body -ContentType "application/json"
Write-Host "   Response: $($result | ConvertTo-Json -Compress)" -ForegroundColor Gray
if (-not $result.success) { Write-Host "   FAILED!" -ForegroundColor Red; exit 1 }
Write-Host "   OK" -ForegroundColor Green
Write-Host ""

# 5. Set Material Color
Write-Host "5. Setting material color to #FF33AA..." -ForegroundColor Yellow
$materialPath = $result.assetPath
$body = @{
    materialPath = $materialPath
    color = "#FF33AA"
} | ConvertTo-Json
$result = Invoke-RestMethod -Uri "$BaseUrl/set-material-color" -Method Post -Body $body -ContentType "application/json"
Write-Host "   Response: $($result | ConvertTo-Json -Compress)" -ForegroundColor Gray
if (-not $result.success) { Write-Host "   FAILED!" -ForegroundColor Red; exit 1 }
Write-Host "   OK" -ForegroundColor Green
Write-Host ""

# 6. Set Material Emission
Write-Host "6. Setting emission to #33AAFF / 3.0..." -ForegroundColor Yellow
$body = @{
    materialPath = $materialPath
    emissionColor = "#33AAFF"
    emissionIntensity = 3.0
} | ConvertTo-Json
$result = Invoke-RestMethod -Uri "$BaseUrl/set-material-emission" -Method Post -Body $body -ContentType "application/json"
Write-Host "   Response: $($result | ConvertTo-Json -Compress)" -ForegroundColor Gray
if (-not $result.success) { Write-Host "   FAILED!" -ForegroundColor Red; exit 1 }
Write-Host "   OK" -ForegroundColor Green
Write-Host ""

# 7. Create Particle Effect
Write-Host "7. Creating particle effect AI_Material_Test_Particle..." -ForegroundColor Yellow
$body = @{
    effectName = "AI_Material_Test_Particle"
    color = "#33AAFF"
    duration = 5.0
    emissionRate = 50.0
    startLifetime = 2.0
    startSpeed = 1.0
    startSize = 0.3
    radius = 0.5
    loop = $true
} | ConvertTo-Json
$result = Invoke-RestMethod -Uri "$BaseUrl/create-particle-effect" -Method Post -Body $body -ContentType "application/json"
Write-Host "   Response: $($result | ConvertTo-Json -Compress)" -ForegroundColor Gray
if (-not $result.success) { Write-Host "   FAILED!" -ForegroundColor Red; exit 1 }
Write-Host "   OK" -ForegroundColor Green
Write-Host ""

# 8. Create Additive Particle Material
Write-Host "8. Creating additive particle material AI_Test_Additive_Particle..." -ForegroundColor Yellow
$body = @{
    materialName = "AI_Test_Additive_Particle"
    color = "#33AAFF"
    emissionIntensity = 2.0
} | ConvertTo-Json
$result = Invoke-RestMethod -Uri "$BaseUrl/create-additive-particle-material" -Method Post -Body $body -ContentType "application/json"
Write-Host "   Response: $($result | ConvertTo-Json -Compress)" -ForegroundColor Gray
if (-not $result.success) { Write-Host "   FAILED!" -ForegroundColor Red; exit 1 }
$particleMaterialPath = $result.assetPath
Write-Host "   OK" -ForegroundColor Green
Write-Host ""

# 9. Assign Particle Material
Write-Host "9. Assigning particle material to AI_Material_Test_Particle..." -ForegroundColor Yellow
$body = @{
    objectName = "AI_Material_Test_Particle"
    materialPath = $particleMaterialPath
} | ConvertTo-Json
$result = Invoke-RestMethod -Uri "$BaseUrl/assign-material" -Method Post -Body $body -ContentType "application/json"
Write-Host "   Response: $($result | ConvertTo-Json -Compress)" -ForegroundColor Gray
if (-not $result.success) { Write-Host "   FAILED!" -ForegroundColor Red; exit 1 }
Write-Host "   OK" -ForegroundColor Green
Write-Host ""

# 10. Save Prefab
Write-Host "10. Saving AI_Material_Test_Particle as prefab..." -ForegroundColor Yellow
$body = @{
    objectName = "AI_Material_Test_Particle"
    prefabPath = ""
} | ConvertTo-Json
$result = Invoke-RestMethod -Uri "$BaseUrl/save-prefab" -Method Post -Body $body -ContentType "application/json"
Write-Host "   Response: $($result | ConvertTo-Json -Compress)" -ForegroundColor Gray
if (-not $result.success) { Write-Host "   FAILED!" -ForegroundColor Red; exit 1 }
Write-Host "   OK" -ForegroundColor Green
Write-Host ""

Write-Host "=== All tests passed! ===" -ForegroundColor Cyan
