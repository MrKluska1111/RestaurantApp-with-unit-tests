using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;


namespace RestaurantAPI.IntegrationTests
{
    public class RestaurantControllerTests
    {
        [Theory]
        [InlineData("PageSize=5&PageNumber=1&SortBy=Name")]
        [InlineData("PageSize=15&PageNumber=2&SortBy=Name")]
        //[InlineData("PageSize=155&PageNumber=3&SortBy=Name")]
        public async Task GetAll_WithQueryParameters_ReturnsOkResult(string queryParams)
        {
            //arrange
            var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();          

            //act
            var response = await client.GetAsync("/api/restaurant?" + queryParams);

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
            //arrange
            var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();

            //act
            var response = await client.GetAsync("/api/restaurant?" + queryParams);

            //assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);


        }
    }
}
