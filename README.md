find . -type f -name "*.cs" -exec cat {} + > output.txt


dotnet ef migrations add "InitialMigration" --project src/Portfolio.Infrastructure --startup-project src/Portfolio.Api --output-dir Data\Migrations

dotnet ef database update --project src/Portfolio.Infrastructure --startup-project src/Portfolio.Api 