using Dapper;
using nextjs_dashboardAPI.DTOs;
using System.Data;
using System.Data.Common;

namespace nextjs_dashboardAPI.Services
{
    public class InvoiceService
    {
        private readonly IDbConnection _db;

        public InvoiceService(IDbConnection db)
        {
            _db = db;
        }

        // GET single invoice
        public async Task<dynamic?> GetInvoiceAsync(Guid id)
        {
            var sql = @"SELECT * FROM invoices WHERE id = @Id";
            return await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
        }

        // CREATE invoice
        public async Task<Guid> CreateInvoiceAsync(InvoiceCreateDto dto)
        {
            var newId = Guid.NewGuid();
            var date = DateTime.UtcNow.Date;

            var sql = @"
                INSERT INTO invoices (id, customer_id, amount, status, date)
                VALUES (@Id, @CustomerId, @Amount, @Status, @Date);
            ";

            await _db.ExecuteAsync(sql, new
            {
                Id = newId,
                dto.CustomerId,
                dto.Amount,
                dto.Status,
                Date = date
            });

            return newId;
        }

        // UPDATE invoice
        public async Task<bool> UpdateInvoiceAsync(Guid id, InvoiceUpdateDto dto)
        {
            var sql = @"
                UPDATE invoices
                SET customer_id = @CustomerId,
                    amount = @Amount,
                    status = @Status
                WHERE id = @Id;
            ";

            //var amountInCents = dto.Amount * 100; // 將 dollars 轉回 cents

            var rows = await _db.ExecuteAsync(sql, new
            {
                Id = id,
                dto.CustomerId,
                dto.Amount,
                dto.Status
            });

            return rows > 0; // false = not found
        }

        // DELETE invoice
        public async Task<bool> DeleteInvoiceAsync(Guid id)
        {
            var sql = @"DELETE FROM invoices WHERE id = @Id";

            var rows = await _db.ExecuteAsync(sql, new { Id = id });

            return rows > 0; // false = not found
        }

        public async Task<IEnumerable<FetchFilteredInvoicesDto>> FetchFilteredInvoicesAsync(
    string query, int currentPage, int perPage)
        {
            //var itemsPerPage = 6;

            var keyword = $"%{query}%";
            int offset = (currentPage - 1) * perPage;
            int limit = perPage;

            const string sql = @"
            SELECT 
                i.id,
                i.amount,
                i.date,
                i.status,
                c.name,
                c.email,
                c.image_url
            FROM invoices i
            INNER JOIN customers c ON i.customer_id = c.id
            WHERE 
                LOWER(c.name) LIKE LOWER('%' + @keyword + '%') OR
                LOWER(c.email) LIKE LOWER('%' + @keyword + '%') OR
                CAST(i.amount AS NVARCHAR(50)) LIKE '%' + @keyword + '%' OR
                CONVERT(NVARCHAR(10), i.date, 23) LIKE '%' + @keyword + '%' OR
                LOWER(i.status) LIKE LOWER('%' + @keyword + '%')
            ORDER BY i.date DESC
            OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
        ";

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("keyword", keyword);
                parameters.Add("offset", offset, DbType.Int32);
                parameters.Add("limit", limit, DbType.Int32);

                var result = await _db.QueryAsync<FetchFilteredInvoicesDto>(sql, parameters);

                //var mappedResult = result.Select(x =>
                //{
                //    x.Amount /= 100;
                //    return x;
                //});

                //return mappedResult.ToList();

                return result;

            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL Error: " + ex.Message);
                throw; // 把真正錯誤往上拋
            }
        }
        public async Task<FetchInvoiceByIdDto?> FetchInvoiceByIdAsync(Guid id)
        {
            const string sql = @"
            SELECT
                id,
                customer_id AS CustomerId,
                amount,
                status
            FROM invoices
            WHERE id = @Id;
        ";

            try
            {
                var invoice = await _db.QueryFirstOrDefaultAsync<FetchInvoiceByIdDto>(sql, new { Id = id });

                //if (invoice != null)
                //{
                //    // Convert amount from cents to dollars
                //    invoice.Amount /= 100;
                //}

                return invoice;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new Exception("Failed to fetch invoice.", ex);
            }
        }
        public async Task<int> FetchInvoicesCountAsync(
    string query)
        {

            var keyword = $"%{query}%";


            const string sql = @"
            SELECT COUNT(*)
            FROM invoices i
            INNER JOIN customers c ON i.customer_id = c.id
            WHERE 
                LOWER(c.name) LIKE LOWER('%' + @keyword + '%') OR
                LOWER(c.email) LIKE LOWER('%' + @keyword + '%') OR
                CAST(i.amount AS NVARCHAR(50)) LIKE '%' + @keyword + '%' OR
                CONVERT(NVARCHAR(10), i.date, 23) LIKE '%' + @keyword + '%' OR
                LOWER(i.status) LIKE LOWER('%' + @keyword + '%')
        ";

            try
            {
                var parameters = new DynamicParameters();

                int count = await _db.ExecuteScalarAsync<int>(sql, new { keyword });


                return count;

                //var mappedResult = result.Select(x =>
                //{
                //    x.Amount /= 100;
                //    return x;
                //});

                //return mappedResult.ToList();

                //return result;

            }
            catch (Exception ex)
            {
                Console.WriteLine("SQL Error: " + ex.Message);
                throw; // 把真正錯誤往上拋
            }
        }


    }

    }
