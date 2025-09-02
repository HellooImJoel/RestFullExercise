# REST Full API con C# y Docker

## Introducción

Este proyecto implementa un ejercicio práctico de comunicación en sistemas distribuidos mediante REST Full API utilizando C# y .NET 8. El sistema consta de dos componentes principales:

- **RestApiService**: Una API REST que maneja operaciones de inventario
- **RestApiClient**: Un cliente de consola que consume los servicios de la API

Ambos componentes se ejecutan en contenedores Docker para simular un entorno distribuido real.

## Estructura del Proyecto

```
RestFullExercise/
├── RestFullExercise.sln
├── docker-compose.yml
├── RestApiService/
│   ├── RestApiService.csproj
│   ├── Program.cs
│   ├── Dockerfile
│   ├── Controllers/
│   │   └── InventoryController.cs
│   └── Properties/
│       └── launchSettings.json
├── RestApiClient/
│   ├── RestApiClient.csproj
│   ├── Program.cs
│   └── Dockerfile
└── README.md
```

## Configuración Inicial del Proyecto

### Paso 1: Crear la solución base

Abre una terminal en VS Code y ejecuta los siguientes comandos:

```bash
# Crear directorio principal
mkdir RestFullExercise && cd RestFullExercise

# Crear solución de .NET
dotnet new sln -n RestFullExercise

# Crear proyecto API REST
dotnet new webapi -n RestApiService

# Crear proyecto cliente consola
dotnet new console -n RestApiClient

# Agregar proyectos a la solución
dotnet sln add RestApiService/RestApiService.csproj
dotnet sln add RestApiClient/RestApiClient.csproj
```

## Implementación del Servicio API REST

### Paso 2: Crear el controlador de inventario

Crear el archivo `RestApiService/Controllers/InventoryController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace RestApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        // Almacén en memoria para simular una base de datos
        private static readonly Dictionary<string, int> Stock = new()
        {
            ["P001"] = 100,
            ["P002"] = 50
        };

        // GET: api/inventory/check/{productId}/{quantity}
        [HttpGet("check/{productId}/{quantity}")]
        public IActionResult CheckStock(string productId, int quantity)
        {
            var available = Stock.TryGetValue(productId, out var stockQty) && stockQty >= quantity;
            return Ok(new { ProductId = productId, Available = available });
        }

        // POST: api/inventory/order
        [HttpPost("order")]
        public IActionResult CreateOrder([FromBody] OrderRequest request)
        {
            if (Stock.TryGetValue(request.ProductId, out var stockQty) && stockQty >= request.Quantity)
            {
                Stock[request.ProductId] -= request.Quantity;
                return Ok(new { Success = true, Message = "Order created." });
            }
            return BadRequest(new { Success = false, Message = "Insufficient stock." });
        }
    }

    // Modelo para las solicitudes de pedido
    public class OrderRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
```

**Explicación del código:**
- `[ApiController]`: Atributo que habilita características específicas de API
- `[Route("api/[controller]")]`: Define la ruta base del controlador
- `Stock`: Diccionario estático que simula una base de datos en memoria
- `CheckStock`: Endpoint GET que verifica disponibilidad de stock
- `CreateOrder`: Endpoint POST que procesa pedidos y actualiza el inventario

### Paso 3: Configurar el Dockerfile de la API

Crear `RestApiService/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RestApiService.dll"]
```

## Implementación del Cliente

### Paso 4: Crear el cliente HTTP

Editar `RestApiClient/Program.cs`:

```csharp
using System.Net.Http.Json;

// Configurar cliente HTTP
var client = new HttpClient();
client.BaseAddress = new Uri("http://restapi:80/");

try
{
    Console.WriteLine("=== Cliente REST API ===");
    Console.WriteLine();

    // Verificar stock disponible
    Console.WriteLine("1. Verificando stock para producto P001 (cantidad: 3)");
    var checkResponse = await client.GetFromJsonAsync<dynamic>("api/inventory/check/P001/3");
    Console.WriteLine($"Respuesta: {checkResponse}");
    Console.WriteLine();

    // Crear pedido
    Console.WriteLine("2. Creando pedido para producto P001 (cantidad: 3)");
    var orderResponse = await client.PostAsJsonAsync("api/inventory/order", 
        new { ProductId = "P001", Quantity = 3 });
    var result = await orderResponse.Content.ReadFromJsonAsync<dynamic>();
    Console.WriteLine($"Respuesta: {result}");
    Console.WriteLine();

    // Verificar stock después del pedido
    Console.WriteLine("3. Verificando stock después del pedido");
    var checkResponse2 = await client.GetFromJsonAsync<dynamic>("api/inventory/check/P001/3");
    Console.WriteLine($"Respuesta: {checkResponse2}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    client.Dispose();
}

Console.WriteLine("Presiona cualquier tecla para salir...");
Console.ReadKey();
```

**Explicación del código:**
- `HttpClient`: Cliente HTTP para realizar peticiones REST
- `BaseAddress`: URL base de la API (usando el nombre del servicio Docker)
- `GetFromJsonAsync`: Método para realizar peticiones GET y deserializar JSON
- `PostAsJsonAsync`: Método para realizar peticiones POST con cuerpo JSON

### Paso 5: Configurar el Dockerfile del cliente

Crear `RestApiClient/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RestApiClient.dll"]
```

## Configuración de Docker Compose

### Paso 6: Crear docker-compose.yml

Crear el archivo `docker-compose.yml` en la raíz del proyecto:

```yaml
version: "3.9"
services:
  restapi:
    build: ./RestApiService
    container_name: restapi
    ports:
      - "5000:80"
    networks:
      - app-network

  client:
    build: ./RestApiClient
    container_name: restapi-client
    depends_on:
      - restapi
    networks:
      - app-network

networks:
  app-network:
    driver: bridge
```

**Explicación de la configuración:**
- `restapi`: Servicio que ejecuta la API REST
- `client`: Servicio que ejecuta el cliente de consola
- `depends_on`: Asegura que la API se inicie antes que el cliente
- `networks`: Red personalizada para comunicación entre contenedores

## Ejecución del Proyecto

### Desde VS Code

1. **Instalar extensiones recomendadas:**
   - C# Dev Kit
   - Docker
   - REST Client (opcional)

2. **Ejecutar con Docker Compose:**
   ```bash
   # Construir y ejecutar todos los servicios
   docker-compose up --build
   
   # Ejecutar en segundo plano
   docker-compose up --build -d
   
   # Ver logs
   docker-compose logs -f
   
   # Detener servicios
   docker-compose down
   ```

3. **Ejecutar individualmente para desarrollo:**
   ```bash
   # Terminal 1: Ejecutar API
   cd RestApiService
   dotnet run
   
   # Terminal 2: Ejecutar cliente (ajustar URL a localhost:5000)
   cd RestApiClient
   dotnet run
   ```

### Verificación del funcionamiento

1. **API disponible en:** `http://localhost:5000`
2. **Endpoints disponibles:**
   - GET: `http://localhost:5000/api/inventory/check/P001/3`
   - POST: `http://localhost:5000/api/inventory/order`

3. **Probar con curl:**
   ```bash
   # Verificar stock
   curl http://localhost:5000/api/inventory/check/P001/3
   
   # Crear pedido
   curl -X POST http://localhost:5000/api/inventory/order \
        -H "Content-Type: application/json" \
        -d '{"ProductId":"P001","Quantity":3}'
   ```

## Extensiones Sugeridas

- **Swagger/OpenAPI**: Documentación automática de la API
- **Persistencia**: Integración con SQLite o SQL Server
- **Validación**: Validación de modelos con Data Annotations
- **Logging**: Implementar logging estructurado
- **Testing**: Pruebas unitarias e integración
- **Health Checks**: Endpoints para monitoreo de salud

## Tecnologías Utilizadas

- **.NET 8**: Framework de desarrollo
- **ASP.NET Core**: Framework web para APIs
- **Docker**: Containerización
- **Docker Compose**: Orquestación de contenedores
- **HTTP Client**: Cliente HTTP nativo de .NET
