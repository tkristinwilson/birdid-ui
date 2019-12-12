using Newtonsoft.Json;
using System.Collections.Generic;

namespace BirdId.Ui.Models
{
    public class PredictionsJson
    {
        [JsonProperty(PropertyName = "predictions")]
        public IList<string[]> Predictions { get; set; }
    }
    public class Prediction    
    {   
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "value")]
        public float Value { get; set; }
    }
}
