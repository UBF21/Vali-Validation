# Instalación

## Requisitos previos

- .NET SDK 7.0, 8.0 o 9.0
- Cualquier IDE compatible: Visual Studio 2022+, Rider, VS Code con C# DevKit

## Paquetes disponibles

| Paquete | Descripción | Cuándo instalarlo |
|---|---|---|
| `Vali-Validation` | Core de validación | Siempre |
| `Vali-Validation.MediatR` | Integración con MediatR | Si usas MediatR |
| `Vali-Validation.ValiMediator` | Integración con Vali-Mediator | Si usas Vali-Mediator |
| `Vali-Validation.AspNetCore` | Middleware y filtros ASP.NET Core | Si usas ASP.NET Core |

---

## Paquete core: `Vali-Validation`

Instala el paquete core en el proyecto donde defines los validadores (normalmente el proyecto de Application o el API).

### .NET CLI

```bash
dotnet add package Vali-Validation
```

### Package Manager Console (Visual Studio)

```powershell
Install-Package Vali-Validation
```

### PackageReference en `.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Vali-Validation" Version="1.0.0" />
</ItemGroup>
```

El paquete core tiene **una sola dependencia transitiva**: `Microsoft.Extensions.DependencyInjection.Abstractions`. Esto significa que puedes usarlo en proyectos de biblioteca (class libraries) sin arrastrar el contenedor DI completo.

---

## Integración con MediatR: `Vali-Validation.MediatR`

Instala este paquete **además del core** en el proyecto donde configuras MediatR (normalmente el proyecto de API o Infrastructure).

```bash
dotnet add package Vali-Validation.MediatR
```

Este paquete ya incluye `Vali-Validation` como dependencia. Si tienes los validadores en un proyecto separado, instala `Vali-Validation` allí y `Vali-Validation.MediatR` en el proyecto de arranque.

### Estructura típica con MediatR

```
MyApp.sln
├── MyApp.Api/                    ← Instala Vali-Validation.MediatR + MediatR
│   └── Program.cs
├── MyApp.Application/            ← Instala Vali-Validation
│   ├── Commands/
│   │   └── CreateOrderCommand.cs
│   └── Validators/
│       └── CreateOrderValidator.cs
└── MyApp.Domain/                 ← Sin dependencias de validación
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

## Integración con Vali-Mediator: `Vali-Validation.ValiMediator`

Instala este paquete si usas Vali-Mediator en lugar de MediatR.

```bash
dotnet add package Vali-Validation.ValiMediator
```

> **Importante:** No instales `Vali-Validation.MediatR` y `Vali-Validation.ValiMediator` en el mismo proyecto. Son mutuamente excluyentes porque implementan el mismo pipeline behavior para mediators distintos.

### Estructura típica con Vali-Mediator

```
MyApp.sln
├── MyApp.Api/                    ← Instala Vali-Validation.ValiMediator
│   └── Program.cs
├── MyApp.Application/            ← Instala Vali-Validation
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

## Integración con ASP.NET Core: `Vali-Validation.AspNetCore`

Instala este paquete en el proyecto de API.

```bash
dotnet add package Vali-Validation.AspNetCore
```

Este paquete puede usarse en combinación con `Vali-Validation.MediatR` o `Vali-Validation.ValiMediator`. También puede usarse de forma standalone si no usas ningún mediator.

### Combinaciones habituales

**API Minimal + Vali-Mediator + AspNetCore:**

```xml
<ItemGroup>
  <PackageReference Include="Vali-Validation.ValiMediator" Version="1.0.0" />
  <PackageReference Include="Vali-Validation.AspNetCore" Version="1.0.0" />
</ItemGroup>
```

**API MVC + MediatR + AspNetCore:**

```xml
<ItemGroup>
  <PackageReference Include="Vali-Validation.MediatR" Version="1.0.0" />
  <PackageReference Include="Vali-Validation.AspNetCore" Version="1.0.0" />
</ItemGroup>
```

**Solo validación manual (sin mediator):**

```xml
<ItemGroup>
  <PackageReference Include="Vali-Validation" Version="1.0.0" />
  <PackageReference Include="Vali-Validation.AspNetCore" Version="1.0.0" />
</ItemGroup>
```

---

## Verificación de la instalación

Después de instalar, compila el proyecto para asegurarte de que no hay conflictos:

```bash
dotnet build
```

Puedes verificar que los namespaces están disponibles con un archivo de prueba:

```csharp
using Vali_Validation.Core.Validators;
using Vali_Validation.Core.Results;

// Si compila, la instalación es correcta.
public class SampleValidator : AbstractValidator<string>
{
    public SampleValidator()
    {
        RuleFor(x => x).NotEmpty();
    }
}
```

---

## Configuración de versiones

### Versión fija (recomendado para producción)

```xml
<PackageReference Include="Vali-Validation" Version="1.0.0" />
```

### Última versión minor compatible

```xml
<PackageReference Include="Vali-Validation" Version="1.*" />
```

### Última versión (solo desarrollo)

```xml
<PackageReference Include="Vali-Validation" Version="*" />
```

---

## Fuentes de paquetes

Los paquetes están disponibles en **NuGet.org**. Si trabajas en un entorno corporativo con un feed privado, asegúrate de que el feed incluya NuGet.org como fuente upstream, o publica los paquetes en tu feed privado.

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

## Siguiente paso

Con los paquetes instalados, sigue con el **[Inicio rápido](03-inicio-rapido.md)** para ver un ejemplo completo funcionando en minutos.
