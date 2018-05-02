Import-Module -Name NetNat
Get-NetNat | Remove-NetNat -Confirm:$false # https://github.com/docker/for-win/issues/598
New-NetFirewallRule -DisplayName "Allow Access to MSSQL from Docker" -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow

Push-Location -Path 'tests'
Write-Output "Starting docker-compose. This may take a while..."
Start-Process -FilePath 'docker-compose' -ArgumentList 'up -d' -Wait -NoNewWindow
Invoke-Expression ".\get-api-key.ps1"
