using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using Microsoft.AspNetCore.Mvc;
using Task_1.Models;

namespace Task_1.Controllers
{
    public class NotificationController : Controller
    {
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(ILogger<NotificationController> logger)
        {
            _logger = logger;
        }
        IFirebaseConfig firebaseConfig = new FirebaseConfig
        {
            //Your database secret here
            AuthSecret = "6OUnOyJ6HeTrfJ8dyJHehREe00BJIAINALcbOXlZ",
            //Your database url
            BasePath = "https://demopush-18ac7-default-rtdb.firebaseio.com/"
        };
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult StorePayload([FromBody] dynamic notification)
        {

            // Deserialize dynamic notification object to MyNotification class
            MyNotification myNotification = new MyNotification
            {
                Title = notification.GetProperty("title").GetString(),
                Body = notification.GetProperty("body").GetString()
            };


            IFirebaseClient client = new FirebaseClient(firebaseConfig);
            if (client != null && !string.IsNullOrEmpty(firebaseConfig.BasePath) && !string.IsNullOrEmpty(firebaseConfig.AuthSecret))
            {
                _ = client.Push("notifications/", myNotification);
                ViewBag.PushResponse = "Data pushed successfully";
            }
            else
            {
                ViewBag.PushResponse = "Failed to connect to Firebase";
            }
            return Json(new { success = true });
        }
        public ActionResult GetAllNotifications()
        {
            try
            {

                FirebaseClient client = new FireSharp.FirebaseClient(firebaseConfig);

                if (client != null)
                {
                    FirebaseResponse response = client.Get("notifications/");

                    // Check if there are notifications in the database
                    if (response.Body != "null")
                    {
                        Dictionary<string, MyNotification> notifications = response.ResultAs<Dictionary<string, MyNotification>>();


                        // Now 'notifications' dictionary contains all notifications from the database
                        // You can iterate over it or process it as needed

                        ViewBag.Notifications = notifications;
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "No notifications found in the database.";
                    }
                }
                else
                {
                    ViewBag.ErrorMessage = "Failed to connect to Firebase.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error: {ex.Message}";
            }

            return View();
        }


    }
}
