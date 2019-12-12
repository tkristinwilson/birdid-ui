namespace BirdId.Ui.Models
{
    public class ClassifyViewModel
    {
        public string DefaultImageUrl { get { return "~/img/home-bird.jpg"; }  }
        public string DefaultImageCaption { get { return "Prediction: bellbird (0.9999935626983643)"; }  }
        
        public string ImageUrl { get; set; }
        public string ImageCaption { get; set; }

        public string PredictedImageUrl { get; set; }

        public string Message { get; set; }
        public string Mode { get; set; }

        public string AppVersion { get; set; }
        public bool PredictionModeOn { get; set; }

    }
}
