using System.ComponentModel.DataAnnotations;

namespace nextjs_dashboardAPI.DTOs
{
    public class InvoiceCreateDto
    {
        public Guid CustomerId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public int Amount { get; set; }

        [Required]
        [RegularExpression("^(pending|paid)$", ErrorMessage = "Status must be 'pending' or 'paid'.")]
        public string Status { get; set; } = string.Empty;

        public DateOnly Date { get; set; }
    }

    public class InvoiceUpdateDto
    {
        [Required(ErrorMessage = "Amount is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public int Amount { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("pending|paid", ErrorMessage = "Status must be 'pending' or 'paid'.")]
        public string Status { get; set; } = string.Empty;

        // 如果想允許更新客戶，可以加這個欄位
        [Required(ErrorMessage = "CustomerId is required.")]
        public Guid CustomerId { get; set; }
    }

    public class LatestInvoiceDto
    {
        public Guid Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerImageUrl { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }


    public class FetchFilteredInvoicesDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public DateOnly Date { get; set; }
        public string Status { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string image_url { get; set; } = "";
    }

    public class FetchInvoiceByIdDto
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";

    }

    public class FetchInvoicesCountdto
    {
        public int TotalCount { get; set; }  // ✅ 正確屬性
    }


}
