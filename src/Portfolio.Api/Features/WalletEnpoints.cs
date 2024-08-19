namespace Portfolio.Api.Features
{
    public static class WalletEndpoints
    {
        public static void MapWalletEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/wallets");

            group.MapGet("/", async (context) =>
            {
                // Your code to handle the GET request
                await context.Response.WriteAsync("Get all products");
            });

            group.MapGet("/{id:int}", async (HttpContext context, int id) =>
            {
                // Your code to handle the GET request with id
                await context.Response.WriteAsync($"Get product with ID {id}");
            });

            group.MapPost("/", async (context) =>
            {
                // Your code to handle the POST request
                await context.Response.WriteAsync("Create a new product");
            });

            group.MapPut("/{id:int}", async (HttpContext context, int id) =>
            {
                // Your code to handle the PUT request
                await context.Response.WriteAsync($"Update product with ID {id}");
            });

            group.MapDelete("/{id:int}", async (HttpContext context, int id) =>
            {
                // Your code to handle the DELETE request
                await context.Response.WriteAsync($"Delete product with ID {id}");
            });
        }
    }
}
