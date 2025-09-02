using System.Net.Http.Json;

var client = new HttpClient();
client.BaseAddress = new Uri("http://restapi:80/");

// Check stock
var checkResponse = await client.GetFromJsonAsync<dynamic>("api/inventory/check/P001/3");
Console.WriteLine($"Check Stock: {checkResponse}");

// Crear pedido
var orderResponse = await client.PostAsJsonAsync("api/inventory/order", new { ProductId = "P001", Quantity = 3 });
var result = await orderResponse.Content.ReadFromJsonAsync<dynamic>();
Console.WriteLine($"Create Order: {result}");