using Azure.Search.Documents.Indexes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchMultiSrcIndex.Models
{
    public class Product
    {
        [JsonProperty(PropertyName = "productId")]
        [SimpleField(IsFilterable = true, IsKey = true)]
        public string ProductId { get; set; }

        [JsonProperty(PropertyName = "productName")]
        [SimpleField(IsFilterable = true)]
        public string ProductName { get; set; }

        [JsonProperty(PropertyName = "price")]
        [SimpleField(IsFilterable = true)]
        public int Price { get; set; }

        // Data from other data source
        [JsonProperty(PropertyName = "description")]
        [SimpleField(IsFilterable = true)]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "available")]
        [SimpleField(IsFilterable = true)]
        public bool Available { get; set; }

        [JsonProperty(PropertyName = "shopLocation")]
        [SimpleField(IsFilterable = true)]
        public string ShopLocation { get; set; }
    }
}
