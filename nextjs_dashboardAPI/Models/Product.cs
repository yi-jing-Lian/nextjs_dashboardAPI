namespace nextjs_dashboardAPI.Models
{
    public class Product
    {
        /// <summary>
        /// 產品編號
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 產品名稱
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 產品價格
        /// </summary>
        public decimal Price { get; set; }
        /// <summary>
        /// 產品描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 產品分類
        /// </summary>
        public string Category { get; set; }
    }
}
