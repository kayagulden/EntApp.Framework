# EntApp.Modules.ModuleName

**ModuleName** modülü — EntApp Framework.

## Yapı

```
EntApp.Modules.ModuleName.Domain/           → Entity'ler, ID'ler, Enum'lar
EntApp.Modules.ModuleName.Application/      → Integration Event'ler, Validator'lar
EntApp.Modules.ModuleName.Infrastructure/   → DbContext, Endpoint'ler, ModuleInstaller
```

## Solution'a Ekleme

1. Solution'a projeleri ekleyin:

```bash
dotnet sln EntApp.sln add src/Modules/ModuleName/EntApp.Modules.ModuleName.Domain/EntApp.Modules.ModuleName.Domain.csproj
dotnet sln EntApp.sln add src/Modules/ModuleName/EntApp.Modules.ModuleName.Application/EntApp.Modules.ModuleName.Application.csproj
dotnet sln EntApp.sln add src/Modules/ModuleName/EntApp.Modules.ModuleName.Infrastructure/EntApp.Modules.ModuleName.Infrastructure.csproj
```

2. Host projesine Infrastructure referansı ekleyin:

```bash
dotnet add src/Host/EntApp.WebAPI/EntApp.WebAPI.csproj reference src/Modules/ModuleName/EntApp.Modules.ModuleName.Infrastructure/EntApp.Modules.ModuleName.Infrastructure.csproj
```

3. `Program.cs`'de Dynamic UI kaydı (opsiyonel — DynamicEntity attribute varsa):

```csharp
builder.Services.AddDynamicDbContext<ModuleNameDbContext>();
```

4. `Program.cs`'de endpoint mapping:

```csharp
app.MapModuleNameEndpoints();
```

5. Build ve test:

```bash
dotnet build EntApp.sln
dotnet run --project src/Host/EntApp.WebAPI
```

## Schema

PostgreSQL schema: `moduleschema`

## Sonraki Adımlar

- [ ] `SampleEntity`'yi kendi entity'nizle değiştirin
- [ ] ID dosyasında yeni `IEntityId` struct'lar tanımlayın
- [ ] DbContext'e yeni entity mapping'ler ekleyin
- [ ] Endpoint'leri iş kurallarınıza göre güncelleyin
- [ ] Integration Event'leri tanımlayın
- [ ] Dynamic UI attribute'larını ayarlayın
