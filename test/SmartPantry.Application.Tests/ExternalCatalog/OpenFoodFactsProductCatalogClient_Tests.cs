using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SmartPantry.ProductLookup;
using Xunit;

namespace SmartPantry.ExternalCatalog;

public class OpenFoodFactsProductCatalogClient_Tests
{
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public HttpRequestMessage? LastRequest { get; private set; }

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }

    private static (OpenFoodFactsProductCatalogClient Client, StubHttpMessageHandler Handler) CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new StubHttpMessageHandler(responder);
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://world.openfoodfacts.org/")
        };
        return (new OpenFoodFactsProductCatalogClient(http), handler);
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    [Fact]
    public async Task Debe_consultar_la_ruta_v3_con_el_codigo_y_solo_los_campos_necesarios()
    {
        var (client, handler) = CreateClient(_ =>
            Json("""{"code":"3017620422003","product":{"product_name":"Nutella"}}"""));

        await client.GetByBarcodeAsync("3017620422003");

        var request = handler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/v3/product/3017620422003", request.RequestUri!.AbsolutePath);
        Assert.Equal(
            "?fields=code,product_name,product_name_es,brands,image_front_url",
            Uri.UnescapeDataString(request.RequestUri.Query));
    }

    [Fact]
    public async Task Debe_mapear_los_campos_y_preferir_el_nombre_en_espanol()
    {
        var (client, _) = CreateClient(_ => Json("""
            {
              "code": "3017620422003",
              "product": {
                "product_name": "Nutella",
                "product_name_es": "Nutella crema",
                "brands": "Nutella, Ferrero",
                "image_front_url": "https://example.com/front.jpg"
              }
            }
            """));

        var result = await client.GetByBarcodeAsync("3017620422003");

        Assert.NotNull(result);
        Assert.Equal("3017620422003", result!.Barcode);
        Assert.Equal("Nutella crema", result.Name);
        Assert.Equal("Nutella, Ferrero", result.Brand);
        Assert.Equal("https://example.com/front.jpg", result.ImageUrl);
    }

    [Fact]
    public async Task Debe_usar_product_name_cuando_no_hay_nombre_en_espanol()
    {
        var (client, _) = CreateClient(_ => Json("""
            {"code":"3017620422003","product":{"product_name":"Nutella"}}
            """));

        var result = await client.GetByBarcodeAsync("3017620422003");

        Assert.Equal("Nutella", result!.Name);
    }

    [Fact]
    public async Task Debe_dejar_en_null_nombre_marca_e_imagen_cuando_el_proveedor_no_los_informa()
    {
        var (client, _) = CreateClient(_ => Json("""
            {"code":"7790000000017","product":{}}
            """));

        var result = await client.GetByBarcodeAsync("7790000000017");

        Assert.NotNull(result);
        Assert.Equal("7790000000017", result!.Barcode);
        Assert.Null(result.Name);
        Assert.Null(result.Brand);
        Assert.Null(result.ImageUrl);
    }

    [Fact]
    public async Task Debe_tratar_los_textos_vacios_como_datos_ausentes()
    {
        var (client, _) = CreateClient(_ => Json("""
            {
              "code": "7790000000017",
              "product": { "product_name": "  ", "product_name_es": "", "brands": " ", "image_front_url": "" }
            }
            """));

        var result = await client.GetByBarcodeAsync("7790000000017");

        Assert.NotNull(result);
        Assert.Null(result!.Name);
        Assert.Null(result.Brand);
        Assert.Null(result.ImageUrl);
    }

    [Fact]
    public async Task Debe_devolver_null_cuando_la_respuesta_no_trae_producto()
    {
        var (client, _) = CreateClient(_ => Json("""{"code":"0000000000000"}"""));

        var result = await client.GetByBarcodeAsync("0000000000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task Debe_devolver_null_cuando_el_proveedor_responde_404()
    {
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await client.GetByBarcodeAsync("0000000000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task Debe_lanzar_RateLimit_cuando_el_proveedor_responde_429()
    {
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        await Assert.ThrowsAsync<ExternalCatalogRateLimitException>(
            () => client.GetByBarcodeAsync("3017620422003"));
    }

    [Fact]
    public async Task Debe_lanzar_Unavailable_cuando_el_proveedor_responde_un_error_del_servidor()
    {
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<ExternalCatalogUnavailableException>(
            () => client.GetByBarcodeAsync("3017620422003"));
    }

    [Fact]
    public async Task Debe_lanzar_Unavailable_cuando_falla_la_conexion()
    {
        var (client, _) = CreateClient(_ => throw new HttpRequestException("sin conexión"));

        await Assert.ThrowsAsync<ExternalCatalogUnavailableException>(
            () => client.GetByBarcodeAsync("3017620422003"));
    }

    [Fact]
    public async Task Debe_lanzar_Unavailable_cuando_se_agota_el_tiempo_de_espera()
    {
        var (client, _) = CreateClient(_ => throw new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ExternalCatalogUnavailableException>(
            () => client.GetByBarcodeAsync("3017620422003"));
    }

    [Fact]
    public async Task Debe_lanzar_Unavailable_cuando_la_respuesta_no_es_un_JSON_valido()
    {
        var (client, _) = CreateClient(_ => Json("esto no es json"));

        await Assert.ThrowsAsync<ExternalCatalogUnavailableException>(
            () => client.GetByBarcodeAsync("3017620422003"));
    }
}