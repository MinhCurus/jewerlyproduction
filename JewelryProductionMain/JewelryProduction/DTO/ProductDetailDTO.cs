﻿namespace JewelryProduction.DTO
{
    public class ProductDetailDTO
    {
        public string ProductSampleId { get; set; } = null!;

        public string ProductName { get; set; } = null!;
        public string Description { get; set; } = null!;

        public string Type { get; set; } = null!;

        public string Style { get; set; } = null!;

        public double? Size { get; set; }

        public decimal Price { get; set; }

        public string GoldId { get; set; } = null!;

        public string GoldType { get; set; } = null!;

        public ICollection<string> Image { get; set; }
    }
}
