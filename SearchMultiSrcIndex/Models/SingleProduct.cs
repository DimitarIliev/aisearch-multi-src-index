using Azure.Search.Documents.Indexes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchMultiSrcIndex.Models
{
    public class SingleProduct
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
    }
}
