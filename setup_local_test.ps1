
$hostsPath = "$env:systemroot\System32\drivers\etc\hosts"
$entry = "127.0.0.1 tenant1.localhost"

Write-Host "Checking hosts file at $hostsPath..."

try {
    $content = Get-Content $hostsPath -Raw
    if ($content -match "tenant1.localhost") {
        Write-Host "Entry already exists. Good to go!" -ForegroundColor Green
    }
    else {
        Write-Host "Adding entry to hosts file..."
        Add-Content -Path $hostsPath -Value "`r`n$entry" -ErrorAction Stop
        Write-Host "Added '$entry' to hosts file." -ForegroundColor Green
    }
}
catch {
    Write-Error "Failed to modify hosts file. Please run this script as Administrator."
}

Write-Host "`r`n--- TESTING INSTRUCTIONS ---"
Write-Host "1. Ensure your database is updated by running 'UpdateDB_MultiTenancy.sql' in SSMS."
Write-Host "2. Insert a tenant into the Tenants table:"
Write-Host "   INSERT INTO Tenants (Id, Name) VALUES ('tenant1', 'Test Tenant');"
Write-Host "3. Run the application: dotnet run"
Write-Host "4. Open Browser to: http://tenant1.localhost:5156 (or the port shown in your console)"
