using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Entities;


namespace RestaurantAPI.IntegrationTests
{
    public class RestaurantControllerTests : IClassFixture<WebApplicationFactory<Program>>  //shared context
    {
        private HttpClient _client;

        public RestaurantControllerTests(WebApplicationFactory<Program> factory)
        {
             _client = factory
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        var dbContextOptions = services
                        .SingleOrDefault(service => service.ServiceType == typeof(DbContextOptions<RestaurantDbContext>));

                        services.Remove(dbContextOptions);

                        services.AddDbContext<RestaurantDbContext>(optionms => optionms.UseInMemoryDatabase("RestaurantDb"));
                        //the parameter name is arbitrary

                    });
                })
                .CreateClient();
        }

        [Theory]
        [InlineData("PageSize=5&PageNumber=1&SortBy=Name")]
        [InlineData("PageSize=15&PageNumber=2&SortBy=Name")]
        //[InlineData("PageSize=155&PageNumber=3&SortBy=Name")]
        public async Task GetAll_WithQueryParameters_ReturnsOkResult(string queryParams)
        {
            //act
            var response = await _client.GetAsync("/api/restaurant?" + queryParams);

            //Checking error information
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine(body);

            //assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        [Theory]
        [InlineData("PageSize=100&PageNumber=3&SortBy=Name")]
        [InlineData("PageSize=11&PageNumber=1&SortBy=Name")]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetAll_WithInvalidQueryParams_ReturnsBadRequest(string queryParams)
        {
            //act
            var response = await _client.GetAsync("/api/restaurant?" + queryParams);

            //assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);


        }
    }
}
