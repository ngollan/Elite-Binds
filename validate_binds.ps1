$status = @()
$preset_names = @{}

function New-Status()
{
  param ($PresetName, $FileName, $FileStatus, $CorrectName)

  $status = New-Object PSObject

  $status | Add-Member -type NoteProperty -Name Preset -Value $PresetName
  $status | Add-Member -type NoteProperty -Name FileName -Value $FileName
  $status | Add-Member -type NoteProperty -Name Status -Value $FileStatus
  $status | Add-Member -type NoteProperty -Name CorrectName -Value $CorrectName

  return $status
}

function Check-Profiles() {
    Get-ChildItem "$(Get-ChildItem env:localappdata | Get-Content)\Frontier Developments\Elite Dangerous\Options\Bindings" -Filter "*.binds" |
    Foreach-Object {
        $fname = $_.Name
        Select-Xml -Path $_.FullName -XPath "/Root" |
        foreach {
            $pn = $_.node.GetAttribute('PresetName')
            $major = $_.node.GetAttribute('MajorVersion')
            $minor = $_.node.GetAttribute('MinorVersion')

            $gen_name = "$($pn).$($major).$($minor).binds"

            if ($preset_names.ContainsKey($pn)) {
                $preset_names[$pn] += 1
            } else {
                $preset_names.Add($pn, 1)
            }

            $status
            if ($gen_name -ne $fname) {
                $status += New-Status -PresetName $pn -FileName $fname -CorrectName $gen_name -FileStatus 'bad'
            } else {
                $status += New-Status -PresetName $pn -FileName $fname -FileStatus 'OK'
            }
        }
    }

    $status | foreach {
        $_ | Add-Member -type NoteProperty -Name Conflicts -value ($preset_names[$_.Preset] -gt 1)
    }

    $status | Out-GridView -Wait
}

Check-Profiles