pushd .\src\Streamnesia.Gui
dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true -o ../deployment --self-contained
popd
pushd .\src\deployment
Remove-Item *.pdb
popd
Copy-Item -Path .\src\deployment\* -Destination "D:\steam\steamapps\common\Amnesia The Dark Descent\streamnesia" -Recurse -force
