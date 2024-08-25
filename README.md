find . -type f -name "*.cs" -exec cat {} + > output.txt

without comments
find . -type f -name "*.cs" -exec cat {} + | grep -v '^\s*//' > output.txt

cat */* > output.txt


dotnet ef migrations add "InitialMigration" --project src/Portfolio.Infrastructure --startup-project src/Portfolio.Api --output-dir Data\Migrations

dotnet ef database update --project src/Portfolio.Infrastructure --startup-project src/Portfolio.Api 

# Node
If you need to have node@20 first in your PATH, run:
  echo 'export PATH="/opt/homebrew/opt/node@20/bin:$PATH"' >> ~/.zshrc

For compilers to find node@20 you may need to set:
  export LDFLAGS="-L/opt/homebrew/opt/node@20/lib"
  export CPPFLAGS="-I/opt/homebrew/opt/node@20/include"