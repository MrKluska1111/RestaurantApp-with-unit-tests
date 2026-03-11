using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Entities;
using RestaurantAPI.Models;
using Newtonsoft.Json;
using System.Text;
using Microsoft.AspNetCore.Authorization.Policy;
using RestaurantAPI.IntegrationTests.Helpers;


namespace RestaurantAPI.IntegrationTests
{
    public class RestaurantControllerTests : IClassFixture<WebApplicationFactory<Program>>  //shared context
    {
        private HttpClient _client;
        private WebApplicationFactory<Program> _factory;

        public RestaurantControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory
               .WithWebHostBuilder(builder =>
               {
                   builder.ConfigureServices(services =>
                   {
                       var dbContextOptions = services
                       .SingleOrDefault(service => service.ServiceType == typeof(DbContextOptions<RestaurantDbContext>));

                       services.Remove(dbContextOptions);

                       services.AddSingleton<IPolicyEvaluator, FakePolicyEvaluator>();

                       services.AddMvc(option => option.Filters.Add(new FakeUserFilter()));

                       services.AddDbContext<RestaurantDbContext>(options => options.UseInMemoryDatabase("RestaurantDb"));
                       //the parameter name is arbitrary

                   });
               });

            _client = _factory.CreateClient();
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

        [Fact]
        public async Task CreateRestaurant_WithValidModel_ReturnsCreatedStatus()
        {
            //arrange
            var model = new CreateRestaurantDto()
            {
                Name = "TestRestaurant",
                Description = "test",
                Category = "test",
                ContactEmail = "test",
                ContactNumber = "123456789",
                City = "Kraków",
                Street = "Długa 5",
                PostalCode = "12345"
            };

            var httpContent = model.ToJsonHttpContent();

            //act
            var response = await _client.PostAsync("/api/restaurant", httpContent);

            //Checking error information
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine(body);

            //assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();

        }

        [Fact]
        public async Task CreateRestaurant_WithInvalidModel_ReturnsBadRequest()
        {
            //arrange
            var model = new CreateRestaurantDto()
            {
                Description = "test desc",
                ContactEmail = "test@test.com",
                ContactNumber = "999 888 777"
            };

            var httpContent = model.ToJsonHttpContent();

            //act
            var response = await _client.PostAsync("/api/restaurant", httpContent);

            //assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Delete_ForNonExistingRestaurant_ReturnsNotFound()
        {
            //act
            var response = await _client.DeleteAsync("/api/restaurant/987");


            //assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

        }

        private void SeedRestaurant(Restaurant restaurant)
        {
            //seed
            var scopeFactory = _factory.Services.GetService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            var _dbContext = scope.ServiceProvider.GetService<RestaurantDbContext>();

            _dbContext.Restaurants.Add(restaurant);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Delete_ForRestaurantOwner_ReturnsNoContent()
        {
            //arrange

            var restaurant = new Restaurant()
            {
                Description = "test",
                Category = "test",
                ContactEmail = "test@test.com",
                ContactNumber = "999 999 999",
                CreatedById = 1,  //must be equal to NameIndetifier in FakeUserFilter
                Name = "test"
            };

            //seed
            SeedRestaurant(restaurant);

            //act
            var response = await _client.DeleteAsync("/api/restaurant/" + restaurant.Id);

            //assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Delete_ForNonRestaurantOwner_ReturnsForbidden()
        {
            //arrange

            var restaurant = new Restaurant()
            {
                Description = "test",
                Category = "test",
                ContactEmail = "test@test.com",
                ContactNumber = "999 999 999",
                CreatedById = 900,  //must be equal to NameIndetifier in FakeUserFilter
                Name = "test"
            };

            //seed
            SeedRestaurant(restaurant);

            //act
            var response = await _client.DeleteAsync("/api/restaurant/" + restaurant.Id);

            //assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
        }
    }
}
