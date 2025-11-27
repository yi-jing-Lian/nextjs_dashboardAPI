using Microsoft.AspNetCore.Mvc;
using nextjs_dashboardAPI.DTOs;
using nextjs_dashboardAPI.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace nextjs_dashboardAPI.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IDatabase _redisDb;
        private readonly InvoiceService _service;



        public InvoiceController(InvoiceService service, IConnectionMultiplexer redis)
        {
            _service = service;
            _redisDb = redis.GetDatabase();
        }
        private const string CachePrefix = "filtered_invoices:";
        /// <summary>
        /// 儲存快取並記錄 key 到索引
        /// </summary>
        private async Task SetCacheAsync(string cacheKey, string value)
        {
            await _redisDb.StringSetAsync(cacheKey, value, TimeSpan.FromMinutes(10));
            // 將 key 存到索引集合
            await _redisDb.SetAddAsync($"{CachePrefix}keys", cacheKey);
        }

        /// <summary>
        /// 刪除所有 filtered_invoices 快取
        /// </summary>
        private async Task ClearFilteredInvoicesCacheAsync()
        {
            var redisValues = await _redisDb.SetMembersAsync($"{CachePrefix}keys"); // RedisValue[]

            // 過濾空值，轉成 RedisKey[]
            RedisKey[] keys = redisValues
                .Where(x => !x.IsNullOrEmpty)
                .Select(x => new RedisKey(x.ToString()))
                .ToArray();

            if (keys.Length > 0)
            {
                await _redisDb.KeyDeleteAsync(keys);
            }

            // 刪除索引集合本身
            await _redisDb.KeyDeleteAsync($"{CachePrefix}keys");
        }



        //// GET api/invoice/{id}
        //[HttpGet("{id}")]
        //public async Task<IActionResult> Get(Guid id)
        //{
        //    var invoice = await _service.GetInvoiceAsync(id);

        //    if (invoice == null)
        //        return NotFound(new { message = "Invoice not found." });

        //    return Ok(invoice);
        //}

        // POST api/invoice
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] InvoiceCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdId = await _service.CreateInvoiceAsync(dto);
                await ClearFilteredInvoicesCacheAsync();
                return Ok(new
                {
                    message = "Invoice created successfully.",
                    id = createdId
                });
            }
            catch (Exception ex)
            {
                // 印出錯誤訊息到 console/log
                Console.Error.WriteLine($"Error creating invoice: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);

                // 回傳前端錯誤訊息
                return StatusCode(500, new
                {
                    message = "Failed to create invoice.",
                    error = ex.Message
                });
            }
        }


        // PUT api/invoice/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] InvoiceUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState); // 會回傳缺少 amount 或 status 的錯誤

            var updated = await _service.UpdateInvoiceAsync(id, dto);

            if (!updated)
                return NotFound(new { message = "Invoice not found." });
            // 清除快取
            await ClearFilteredInvoicesCacheAsync();
            return Ok(new { message = "Invoice updated successfully." });
        }


        // DELETE api/invoice/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _service.DeleteInvoiceAsync(id);

            if (!deleted)
                return NotFound(new { message = "Invoice not found." });
            // 清除快取
            await ClearFilteredInvoicesCacheAsync();

            return Ok(new { message = "Invoice deleted successfully." });
        }

        [HttpGet("filtered")]
        public async Task<IActionResult> GetFiltered(
        [FromQuery] string? query = "",
        [FromQuery] int page = 1,
        [FromQuery] int perPage =6)

        {
            var cacheKey = $"filtered_invoices:query={query}:page={page}:perPage={perPage}";
            

            // 嘗試從 Redis 拿快取
            var cachedData = await _redisDb.StringGetAsync(cacheKey);
            if (cachedData.HasValue)
            {
                var cachedInvoices = JsonSerializer.Deserialize<List<FetchFilteredInvoicesDto>>(cachedData!);
                return Ok(cachedInvoices);
            }


            // Redis 沒有快取，從 DB 讀

            try
            {
                var invoices = await _service.FetchFilteredInvoicesAsync(
                    query ?? "",
                    page,
                    perPage
                );
                var serialized = JsonSerializer.Serialize(invoices);
                await SetCacheAsync(cacheKey, serialized);

                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to fetch filtered invoices.",
                    error = ex.Message,
                    stack = ex.StackTrace
                });
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var invoice = await _service.FetchInvoiceByIdAsync(id);

                if (invoice == null)
                    return NotFound(new { message = "Invoice not found." });

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch invoice.", error = ex.Message });
            }
        }

        [HttpGet("pages")]
        public async Task<IActionResult> GetTotalInvoice([FromQuery] string query = "")
        {
            try
            {
                int count = await _service.FetchInvoicesCountAsync(query);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch total pages.", error = ex.Message });
            }
        }
    }





}

