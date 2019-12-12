using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using BirdId.Ui.Models;
using BirdId.Ui;

namespace Bird.Ui.Controllers
{
    public class HomeController : Controller
    {
        readonly ClassifyViewModel _defaultModel = new ClassifyViewModel();
        private readonly string[] _extensions= new string[] { ".jpg", ".jpeg", ".png" };
        private readonly IOptions<AppConfig> _config;
        static readonly HttpClient _client = new HttpClient();

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IOptions<AppConfig> config)
        {
            _logger = logger;
            _config = config;           
        }

       public IActionResult Index()
        {
            ViewData["Version"] = _config.Value.Version;
            _defaultModel.PredictionModeOn = _config.Value.PredictionModeOn;
            return View(_defaultModel);
        }
        public IActionResult About()
        {
            ViewData["Version"] = _config.Value.Version;
            return View();
        }

        [HttpGet()]
        public IActionResult Classify()
        {
            return RedirectToAction("Index");
        }

        [HttpPost()]
        public async Task<IActionResult> Classify(ClassifyViewModel viewModel, IFormFile file)
        {
            if (!_config.Value.PredictionModeOn) 
            {
                return RedirectToAction("Index");
            }

            ViewData["Version"] = _config.Value.Version;
            ClassifyViewModel model = new ClassifyViewModel();
            model.PredictionModeOn = _config.Value.PredictionModeOn;

            string errorMessage = IsValid(viewModel, file);

            if (string.IsNullOrEmpty(errorMessage))
            {
                //no validation errors
                byte[] imageBytes = null;
                string imageFileName = Guid.NewGuid().ToString();

                //Get the image
                if ("url".Equals(viewModel.Mode))
                {
                    imageBytes = await GetImageBytesAsync(viewModel.ImageUrl);                    
                }
                else if ("file".Equals(viewModel.Mode)) 
                {
                    using (var ms = new MemoryStream())
                    {
                        file.CopyTo(ms);
                        imageBytes = ms.ToArray();                        
                    }
                }

                if (imageBytes != null && imageBytes.Length > 0)
                {
                    string prediction = await PredictAsync(imageBytes, imageFileName);
                    model.ImageCaption = prediction;
                    model.PredictedImageUrl = "data:image;base64," + Convert.ToBase64String(imageBytes);
                }
                else 
                {
                    model.ImageUrl = string.Empty;
                    model.ImageCaption = _defaultModel.DefaultImageCaption;
                    model.Message = "Image could not be loaded";
                }
               
            }
            else 
            {
                model.ImageUrl = string.Empty;
                model.ImageCaption = _defaultModel.DefaultImageCaption;                  
                model.Message = errorMessage;
            }
                        
            return View("Index", model);        
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            ViewData["Version"] = _config.Value.Version;
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<string> PredictAsync(byte[] fileBytes, string fileName)
        {
            StringBuilder prediction = new StringBuilder();
                       
            try
            {
                using (var formData = new MultipartFormDataContent())
                {
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        Name = "file",
                        FileName = fileName
                    };

                    formData.Add(fileContent);                   
                    var response = await _client.PostAsync(_config.Value.PredictionAppUrl, formData);    
                    if (response.IsSuccessStatusCode)
                    {                       
                        var jsonResult = await response.Content.ReadAsStringAsync();
                        PredictionsJson pj = JsonConvert.DeserializeObject<PredictionsJson>(jsonResult);
                        string[] topPrediction = pj.Predictions[0];
                        Prediction p = new Prediction() { Name = topPrediction[0], Value = float.Parse(topPrediction[1]) };                        
                        prediction.Append("Prediction: " + p.Name + " (" + p.Value + ")");
                    }
                }           
            }
            catch (HttpRequestException ex)
            {
                prediction.Append("There was a problem getting the predition: " + ex.Message);
            }            
            return prediction.ToString();           
        }

        private string IsValid(ClassifyViewModel viewModel, IFormFile file)
        {
            if ("url".Equals(viewModel.Mode))
            {
                if (string.IsNullOrEmpty(viewModel.ImageUrl))
                {
                    return "Enter an Image URL";
                }

                Uri uriResult;
                bool result = Uri.TryCreate(viewModel.ImageUrl, UriKind.Absolute, out uriResult);

                if (!result)
                {
                    return "Image URL is not a valid URL";
                }
                
                if (!string.IsNullOrEmpty(Path.GetExtension(viewModel.ImageUrl))
                    && !_extensions.Contains(Path.GetExtension(viewModel.ImageUrl).ToLower()))
                {
                    return "This file is not a supported image type, expected: jpg/jpeg or png";
                }
            }
            else if ("file".Equals(viewModel.Mode)) 
            {                         
                if (file == null)
                {
                    return "Select a file to upload";
                }

                if (file.Length > 1 * 1024 * 1024)
                {
                    return "A file cannot be larger than 1MB";
                }

                if (!string.IsNullOrEmpty(Path.GetExtension(file.FileName))
                    && !_extensions.Contains(Path.GetExtension(file.FileName).ToLower()))
                {
                    return "This file is not a supported image type, expected: jpg/jpeg or png";
                }
            }
            else 
            {
                return "Paste an image URL or upload an image";
            }

            return string.Empty;
        }

        private async Task<byte[]> GetImageBytesAsync(string imageUrl) 
        {
            try
            {
                return await _client.GetByteArrayAsync(imageUrl);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex.Message);
            }
            return null;
        }
    }
}
