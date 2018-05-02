Import-Module -Name NetNat
Get-NetNat | Remove-NetNat -Confirm:$false # https://github.com/docker/for-win/issues/598
New-NetFirewallRule -DisplayName "Allow Access to MSSQL from Docker" -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow

Push-Location -Path 'tests'
Write-Output "Starting docker-compose. This may take a while..."
Start-Process -FilePath 'docker-compose' -ArgumentList 'up -d' -Wait -NoNewWindow

# Wait for Octopus to be ready
$octopusReady = $false
$retryCount

do {
    try {
        Write-Output "Trying to connect to Octopus..."
        $result = Invoke-WebRequest -UseBasicParsing -Uri "http://172.25.16.1:81" -TimeoutSec 5
        if ($result.StatusCode -eq 200) {
            $octopusReady = $true
        }
    }
    catch {
        Write-Output "Next attempt in 5 seconds"
        Start-sleep -Seconds 5
    }

    $retryCount++

}until($retryCount -eq 5 -or $octopusReady)

if ($retryCount -eq 5)
{
    Write-Error "Failed to bring up Octopus Deploy container correctly."
}
else
{
    Invoke-Expression ".\get-api-key.ps1"
}

