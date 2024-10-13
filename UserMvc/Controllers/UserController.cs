using Microsoft.AspNetCore.Mvc;
using UserMvc.Jobs;
using UserMvc.Models;

namespace UserMvc.Controllers
{
    public class UserController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UserController> _logger;
        public UserController(IHttpClientFactory httpClientFactory, ILogger<UserController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        public async Task<IActionResult> Index()
        {
            List<User> users = new List<User>();

            // Check if the Quartz job has completed
            if (JobStatus.IsJobCompleted)
            {
                try
                {
                    // Fetch user data from the API
                    var client = _httpClientFactory.CreateClient();
                    var response = await client.GetAsync("https://localhost:7013/api/User");

                    if (response.IsSuccessStatusCode)
                    {
                        users = await response.Content.ReadAsAsync<List<User>>();
                        _logger.LogInformation("User data retrieved successfully.");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to retrieve users. Status Code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error occurred while fetching users: {ex.Message}");
                }
            }

            return View(users); 
                  
        }
    }
}
