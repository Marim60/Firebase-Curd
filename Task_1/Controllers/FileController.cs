using Firebase.Auth;
using Firebase.Storage;
using FireSharp;
using FireSharp.Interfaces;
using FireSharp.Response;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Task_1.Models;

namespace Task_1.Controllers
{
    public class FileController : Controller
    {
        private static string ApiKey = "AIzaSyCjiRbBCPHmytALV0VBY1BB00VM4gObUmQ";
        private static string Bucket = "demopush-18ac7.appspot.com";
        private static string AuthEmail = "Test@test.com";
        private static string AuthPassword = "Test@123";
        private static string FirebaseUrl = "https://demopush-18ac7-default-rtdb.firebaseio.com/";
        private Microsoft.AspNetCore.Hosting.IHostingEnvironment _env;
        public FileController(Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            _env = env;
        }
        IFirebaseConfig firebaseConfig = new FireSharp.Config.FirebaseConfig
        {
            //Your database secret here
            AuthSecret = "6OUnOyJ6HeTrfJ8dyJHehREe00BJIAINALcbOXlZ",
            //Your database url
            BasePath = "https://demopush-18ac7-default-rtdb.firebaseio.com/"
        };
        private async Task<FirebaseStorage> stroageConfigAsync()
        {
            // Upload file to Firebase storage
            var auth = new Firebase.Auth.FirebaseAuthProvider(new Firebase.Auth.FirebaseConfig(ApiKey));
            // Sign in with email and password
            var authResult = await auth.SignInWithEmailAndPasswordAsync(AuthEmail, AuthPassword);

            // Create a new instance of FirebaseStorage,
            // with the FirebaseStorageOptions object that contains the authentication token.
            var storage = new FirebaseStorage(
                Bucket,
                new FirebaseStorageOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(authResult.FirebaseToken),
                    ThrowOnCancel = true
                });
            return storage;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> UploadFile(FileUploadViewModel fileUploadView)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var file = fileUploadView.File;

                if (file.Length == 0)
                {
                    return BadRequest("Empty file");
                }

                string folderName = "Upload";
                string path = Path.Combine(_env.WebRootPath, "images", folderName);

                // Create the directory if it doesn't exist
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // Open file stream with file 
                using (var fileStream = new FileStream(Path.Combine(path, file.FileName), FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                var storage = await stroageConfigAsync();
                var cancellation = new CancellationTokenSource();
                // Upload file to Firebase Storage
                await storage
                    .Child("Uploads")
                    .Child(fileUploadView.UserID)
                    .Child(file.FileName)
                    .PutAsync(file.OpenReadStream(), cancellation.Token);
                string link = await storage
                    .Child("Uploads")
                    .Child(fileUploadView.UserID)
                    .Child(file.FileName)
                    .GetDownloadUrlAsync();

                // Save URL to the database
                bool urlSaved = await SaveUrlToDatabase(fileUploadView.UserID, link);
                if (!urlSaved)
                {
                    // Handle failure to save URL to the database
                    ViewBag.ErrorMessage = "Failed to save URL to the database.";
                }

                // Return success response
                return Ok("File uploaded successfully");

            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                return BadRequest("Upload failed: " + ex.Message);
            }
        }

        private async Task<bool> SaveUrlToDatabase(string userId, string url)
        {
            try
            {
                IFirebaseClient client = new FireSharp.FirebaseClient(firebaseConfig);
                if (client != null && !string.IsNullOrEmpty(firebaseConfig.BasePath) && !string.IsNullOrEmpty(firebaseConfig.AuthSecret))
                {
                    client.Push($"files/{userId}/", url);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                Console.WriteLine($"Error saving URL to database: {ex.Message}");
                return false;
            }
        }
        public async Task<IActionResult> Upload(FileUploadViewModel fileUploadView)
        {
            // Call the UploadFile action
            var result = await UploadFile(fileUploadView);
            if (result is OkObjectResult)
            {
                ViewBag.Message = "File uploaded successfully";
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Error");

            }

        }

        [HttpPost]
        public ActionResult GetFiles(string userId)
        {
            try
            {
                IFirebaseClient client = new FireSharp.FirebaseClient(firebaseConfig);
                if (client != null)
                {

                    FirebaseResponse response = client.Get($"files/{userId}");

                    if (response.Body != "null") // Check if data exists for the user
                    {
                        Dictionary<string, string> data = response.ResultAs<Dictionary<string, string>>();
                        ViewBag.files = data.Values.ToList(); // Return list of image URLs
                        ViewBag.UserId = userId;
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "No files found in the database.";
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
        [HttpGet]
        public ActionResult GetFiles()
        {
            return View();
        }

        private async Task<IActionResult> DeleteFileFromStore(string UserId, string imageUrl)
        {
            try
            {
                var storage = await stroageConfigAsync();

                Uri uri = new Uri(imageUrl);
                string fileName = Path.GetFileName(uri.LocalPath);
                // Delete the file from Firebase Storage
                await storage
                    .Child("Uploads")
                    .Child(UserId)
                    .Child(fileName)
                    .DeleteAsync();

                return Ok("File deleted successfully");
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                return BadRequest("Deletion failed: " + ex.Message);
            }
        }

        private async Task<IActionResult> DeleteFileFromDatabase(string UserId, string imageUrl)
        {
            try
            {
                IFirebaseClient client = new FirebaseClient(firebaseConfig);
                if (client != null)
                {
                    FirebaseResponse response = client.Get($"files/{UserId}");

                    if (response.Body != "null") // Check if data exists for the user
                    {
                        Dictionary<string, string> data = response.ResultAs<Dictionary<string, string>>();
                        string key = data.FirstOrDefault(x => x.Value == imageUrl).Key;
                        if (key != null)
                        {
                            client.Delete($"files/{UserId}/{key}");
                            return Ok("File deleted successfully");
                        }
                        else
                        {
                            return BadRequest("File not found in the database");
                        }
                    }
                    else
                    {
                        return BadRequest("No files found in the database.");
                    }
                }
                else
                {
                    return BadRequest("Failed to connect to Firebase.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                return BadRequest("Deletion failed: " + ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteFile(string UserId, string imageUrl)
        {
            // Call the DeleteFileFromStore action
            var result = await DeleteFileFromStore(UserId, imageUrl);
            if (result is OkObjectResult)
            {
                // Call the DeleteFileFromDatabase action
                var result2 = await DeleteFileFromDatabase(UserId, imageUrl);
                if (result2 is OkObjectResult)
                {
                    ViewBag.Message = "File deleted successfully";
                    return RedirectToAction("GetFiles", new { userId = UserId });
                }
                else
                {
                    return RedirectToAction("Error");
                }
            }
            else
            {
                return RedirectToAction("Error");
            }
        }
        public IActionResult Error()
        {
            return View();
        }



    }
}
