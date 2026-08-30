<#
.SYNOPSIS
Dump type/member names from a Unity managed assembly using pure metadata reads
(System.Reflection.Metadata, built into PowerShell 7+). No code execution.

.EXAMPLE
./dump-studio-types.ps1 -Assembly "D:\...\StudioNEOV2_Data\Managed\Assembly-CSharp.dll" -Namespace Studio -OutFile studio-types.txt

.EXAMPLE
./dump-studio-types.ps1 -Assembly "...\Assembly-CSharp.dll" -TypeNames OCIChar,SceneInfo -Members
#>
param(
    [Parameter(Mandatory = $true)] [string]$Assembly,
    [string]$Namespace,
    [string[]]$TypeNames,
    [switch]$Members,
    [string]$OutFile
)

$fs = [System.IO.File]::OpenRead($Assembly)
$pe = [System.Reflection.PortableExecutable.PEReader]::new($fs)
$md = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
$sb = [System.Text.StringBuilder]::new()

foreach ($h in $md.TypeDefinitions) {
    $t = $md.GetTypeDefinition($h)
    $ns = $md.GetString($t.Namespace)
    $name = $md.GetString($t.Name)
    if ($Namespace -and $ns -ne $Namespace) { continue }
    if ($TypeNames -and $TypeNames -notcontains $name) { continue }
    [void]$sb.AppendLine("$ns.$name")
    if ($Members -or $TypeNames) {
        $methods = foreach ($mh in $t.GetMethods()) { $md.GetString($md.GetMethodDefinition($mh).Name) }
        $fields = foreach ($fh in $t.GetFields()) { $md.GetString($md.GetFieldDefinition($fh).Name) }
        [void]$sb.AppendLine("  methods: " + (($methods | Select-Object -Unique) -join ', '))
        [void]$sb.AppendLine("  fields:  " + ($fields -join ', '))
    }
}

$pe.Dispose(); $fs.Dispose()

if ($OutFile) { [System.IO.File]::WriteAllText($OutFile, $sb.ToString()); Write-Host "written to $OutFile" }
else { $sb.ToString() }
