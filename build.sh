dotnet run --project ./CakeBuild/CakeBuild.csproj -- "$@"
cd ./traitacquirer/bin/Release/Mods/mod/publish/
zip ../../../../../../traitacquirermoddedclasses-0.9.8.zip -r * 
cd ../../../../../../