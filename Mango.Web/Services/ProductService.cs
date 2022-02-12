using Mango.Web.Models;
using Mango.Web.Services.IServices;

namespace Mango.Web.Services
{
    public class ProductService : BaseService, IProductService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductService(IHttpClientFactory httpClientFactory): base(httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public async Task<T> CreateProductAsync<T>(ProductDto productDto, string token)
        {
            return await SendAsync<T>(new ApiRequest()
            {
                apiType = SD.ApiType.POST,
                Data = productDto,
                Url = $"{SD.ProductAPIBase}api/products",
                Token = token
            });
        }

        public async Task<T> DeleteProductAsync<T>(int id, string token)
        {
            return await SendAsync<T>(new ApiRequest()
            {
                apiType = SD.ApiType.DELETE,
                Url = $"{SD.ProductAPIBase}api/products/{id}",
                Token = token
            });
        }

        public async Task<T> GetAllProductsAsync<T>(string token)
        {
            return await SendAsync<T>(new ApiRequest()
            {
                apiType = SD.ApiType.GET,
                Url = $"{SD.ProductAPIBase}api/products",
                Token = token
            });
        }

        public async Task<T> GetProductByIdAsync<T>(int id, string token)
        {
            return await SendAsync<T>(new ApiRequest()
            {
                apiType = SD.ApiType.GET,
                Url = $"{SD.ProductAPIBase}api/products/{id}",
                Token = token
            });
        }

        public async Task<T> UpdateProductAsync<T>(ProductDto productDto, string token)
        {
            return await SendAsync<T>(new ApiRequest()
            {
                apiType = SD.ApiType.PUT,
                Data = productDto,
                Url = $"{SD.ProductAPIBase}api/products",
                Token = token
            });
        }
    }
}
