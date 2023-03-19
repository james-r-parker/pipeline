cd ./src/DotNetHelp.Pipelines.Tests
dotnet test /p:Threshold=50 /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/
reportgenerator "-reports:./TestResults/coverage.cobertura.xml" "-targetdir:./CoverageReport" "-reporttypes:HTML;Badges"
dotnet stryker
