# EntApp.Modules.ModuleName

**ModuleName** modülü — EntApp Framework.

## Yapı

```
EntApp.Modules.ModuleName.Domain/
  └── Entities/, Ids/, Enums/

EntApp.Modules.ModuleName.Application/
  ├── Commands/           → IRequest<T> command tanımları
  ├── Queries/            → IRequest<T> query tanımları
  ├── Validators/         → FluentValidation kuralları (pipeline otomatik)
  └── IntegrationEvents/  → Modüller arası event'ler

EntApp.Modules.ModuleName.Infrastructure/
  ├── Handlers/           → IRequestHandler<T,TResult> implementasyonları
  ├── Endpoints/          → Thin proxy (ISender → MediatR)
  ├── Persistence/        → DbContext
  └── ModuleInstaller     → DI kaydı
```

## Mimari Kurallar

1. **Endpoint'ler thin proxy'dir** — sadece HTTP → IRequest eşlemesi yapar
2. **İş mantığı Handler'larda yaşar** — DbContext erişimi sadece handler'larda
3. **Validation otomatiktir** — `AbstractValidator<T>` tanımlayın, `ValidationBehavior` pipeline çalıştırır
4. **CancellationToken** — tüm handler'lar desteklemelidir

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
- [ ] Commands/Queries/Validators/Handlers dosyalarını güncelleyin
- [ ] ID dosyasında yeni `IEntityId` struct'lar tanımlayın
- [ ] DbContext'e yeni entity mapping'ler ekleyin
- [ ] Integration Event'leri tanımlayın
- [ ] Dynamic UI attribute'larını ayarlayın
