# Installation

## Prerequisites

- .NET SDK 7.0, 8.0 or 9.0
- Any compatible IDE: Visual Studio 2022+, Rider, VS Code with C# DevKit

## Available Packages

| Package | Description | When to install |
|---|---|---|
| `Vali-Validation` | Validation core | Always |
| `Vali-Validation.MediatR` | Integration with MediatR | If you use MediatR |
| `Vali-Validation.ValiMediator` | Integration with Vali-Mediator | If you use Vali-Mediator |
| `Vali-Validation.AspNetCore` | ASP.NET Core middleware and filters | If you use ASP.NET Core |

---

## Core Package: `Vali-Validation`

Install the core package in the project where you define validators (usually the Application or API project).

### .NET CLI

```bash
dotnet add package Vali-Validation
```

### Package Manager Console (Visual Studio)

```powershell
Install-Package Vali-Validation
```

### PackageReference in `.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Vali-Validation" Version="1.0.0" />
</ItemGroup>
```

The core package has **a single transitive dependency**: `Microsoft.Extensions.DependencyInjection.Abstractions`. This means you can use it in class library projects without dragging in the full DI container.

---

## MediatR Integration: `Vali-Validation.MediatR`

Install this package **in addition to the core** in the project where you configure MediatR (usually the API or Infrastructure project).

```bash
dotnet add package Vali-Validation.MediatR
```

This package already includes `Vali-Validation` as a dependency. If you have validators in a separate project, install `Vali-Validation` there and `Vali-Validation.MediatR` in the startup project.

### Typical Structure with MediatR

```
MyApp.sln
├── MyApp.Api/                    ← Install Vali-Validation.MediatR + MediatR
│   └── Program.cs
├── MyApp.Application/            ← Install Vali-Validation
│   ├── Commands/
│   │   └── CreateOrderCommand.cs
│   └── Validators/
│       └── CreateOrderValidator.cs
└── MyApp.Domain/                 ← No validation dependencies
```

```xml
<!-- MyApp.Application.csproj -->
<ItemGroup>
  <PackageReference Include="Vali-Validation" Version="1.0.0" />
</ItemGroup>

<!-- MyApp.Api.csproj -->
<ItemGroup>
  <PackageReference Include="Vali-Validation.MediatR" Version="1.0.0" />
  <PackageReference Include="MediatR" Version="12.0.0" />
  <ProjectReference Include="..\MyApp.Application\MyApp.Application.csproj" />
</ItemGroup>
```

---

## Vali-Mediator Integration: `Vali-Validation.ValiMediator`

Install this package if you use Vali-Mediator instead of MediatR.

```bash
dotnet add package Vali-Validation.ValiMediator
```

> **Important:** Do not install `Vali-Validation.MediatR` and `Vali-Validation.ValiMediator` in the same project. They are mutually exclusive because they implement the same pipeline behavior for different mediators.

### Typical Structure with Vali-Mediator

```
MyApp.sln
├── MyApp.Api/                    ← Install Vali-Validation.ValiMediator
│   └── Program.cs
├── MyApp.Application/            ← Install Vali-Validation
│   ├── Commands/
│   └── Validators/
```

```xml
<!-- MyApp.Api.csproj -->
<ItemGroup>
  <PackageReference Include="Vali-Validation.ValiMediator" Version="1.0.0" />
  <PackageReference Include="Vali-Mediator" Version="*" />
  <ProjectReference Include="..\MyApp.Application\MyApp.Application.csproj" />
</ItemGroup>
```

---

## ASP.NET Core Integration: `Vali-Validation.AspNetCore`

Install this package in the API project.

```bash
dotnet add package Vali-Validation.AspNetCore
```

This package can be used in combination with `Vali-Validation.MediatR` or `Vali-Validation.ValiMediator`. It can also be used standalone if you are not using any mediator.

### Common Combinations

**Minimal API + Vali-Mediator + AspNetCore:**

```xml
<ItemGroup>
  <PackageReference Include="Vali-Validation.ValiMediator" Version="1.0.0" />
  <PackageReference Include="Vali-Validation.AspNetCore" Version="1.0.0" />
</ItemGroup>
```

**MVC API + MediatR + AspNetCore:**

```xml
<ItemGroup>
  <PackageReference Include="Vali-Validation.MediatR" Version="1.0.0" />
  <PackageReference Include="Vali-Validation.AspNetCore" Version="1.0.0" />
</ItemGroup>
```

**Manual validation only (without mediator):**

```xml
<ItemGroup>
  <PackageReference Include="Vali-Validation" Version="1.0.0" />
  <PackageReference Include="Vali-Validation.AspNetCore" Version="1.0.0" />
</ItemGroup>
```

---

## Verifying the Installation

After installing, build the project to ensure there are no conflicts:

```bash
dotnet build
```

You can verify the namespaces are available with a test file:

```csharp
using Vali_Validation.Core.Validators;
using Vali_Validation.Core.Results;

// If it compiles, the installation is correct.
public class SampleValidator : AbstractValidator<string>
{
    public SampleValidator()
    {
        RuleFor(x => x).NotEmpty();
    }
}
```

---

## Version Configuration

### Fixed version (recommended for production)

```xml
<PackageReference Include="Vali-Validation" Version="1.0.0" />
```

### Latest compatible minor version

```xml
<PackageReference Include="Vali-Validation" Version="1.*" />
```

### Latest version (development only)

```xml
<PackageReference Include="Vali-Validation" Version="*" />
```

---

## Package Sources

The packages are available on **NuGet.org**. If you work in a corporate environment with a private feed, make sure the feed includes NuGet.org as an upstream source, or publish the packages to your private feed.

```xml
<!-- NuGet.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

---

## Next Step

With the packages installed, continue with the **[Quick Start](03-quick-start.md)** to see a complete working example in minutes.
