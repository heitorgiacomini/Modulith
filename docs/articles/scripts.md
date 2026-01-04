# ef-core
> Add-Migration InitialCreate -OutputDir Data/Migrations -Project Basket -StartupProject Api -Context BasketDbContext
> Update-Database -Context BasketDbContext

# generate docs
  > docfx

# generate logs
  > cd "root" \
  >  git-cliff --config docs\cliff.toml -o docs\articles\changelog.md