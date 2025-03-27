using Microsoft.AspNetCore.Mvc;
using Fatiha__app.Extension;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Xml;
using System.Resources;
using System.Diagnostics;
using Fatiha__app.Models;
namespace Fatiha__app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LanguageController : ControllerBase
    {
        private readonly string _resxFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
        private readonly string _jsonFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "i18n");
        [HttpPost("SetLanguage")]
        public IActionResult SetLanguage([FromQuery] string culture, [FromQuery] string returnUrl = "/")
        {
            if (string.IsNullOrWhiteSpace(culture))
            {
                return BadRequest(new { Message = "Culture parameter is required." });
            }

            // تحديد الاتجاه بناءً على اللغة
            bool isRTL = culture == "ar-SA" || culture == "ur-PK";
            string direction = isRTL ? "rtl" : "ltr";

            // حفظ اللغة في الكوكيز
            Response.Cookies.Append("Culture", culture, new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddYears(1),
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            });

            // حفظ الاتجاه في الكوكيز
            Response.Cookies.Append("Direction", direction, new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddYears(1),
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            });

            return Ok(new
            {
                Message = "Language set successfully.",
                Culture = culture,
                Direction = direction,
                ReturnUrl = returnUrl
            });
        }



        [HttpPost("ConvertResxToJsonManual")]
        public IActionResult ConvertResxToJsonManual()
        {
            try
            {
                if (!Directory.Exists(_jsonFolderPath))
                {
                    Directory.CreateDirectory(_jsonFolderPath);
                }

                var resxFolders = new string[]
                {
                    "Resources/Shared",
                    "Resources/Home",
                    "Resources/FatihaRequest",
                    "Resources/FatihaExam",
                    "Resources/AuthorizedUsers"
                };

                var fileMappings = new List<object>();

                foreach (var folder in resxFolders)
                {
                    if (!Directory.Exists(folder))
                        continue;

                    var resxFiles = Directory.GetFiles(folder, "*.resx");

                    foreach (var resxFile in resxFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(resxFile);
                        var parentFolder = Path.GetFileName(folder); // استخراج اسم المجلد الأب

                        var targetFolder = Path.Combine(_jsonFolderPath, parentFolder); // تحديد المجلد المستهدف
                        if (!Directory.Exists(targetFolder))
                        {
                            Directory.CreateDirectory(targetFolder);
                        }

                        var jsonFilePath = Path.Combine(targetFolder, $"{fileName}.json"); // حفظ بنفس الاسم داخل المجلد الجديد

                        ConvertResxFileToJson(resxFile, jsonFilePath);

                        fileMappings.Add(new { ResxFile = resxFile, JsonFile = jsonFilePath });
                    }
                }

                return Ok(new
                {
                    Message = "All .resx files have been converted to JSON successfully.",
                    Files = fileMappings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while converting .resx files to JSON.", Error = ex.Message });
            }
        }

        private void ConvertResxFileToJson(string resxFilePath, string jsonFilePath)
        {
            try
            {
                Console.WriteLine($"Processing: {resxFilePath} -> {jsonFilePath}");

                var dictionary = new Dictionary<string, string>();

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(resxFilePath);

                XmlNodeList dataNodes = xmlDoc.SelectNodes("//data");

                foreach (XmlNode node in dataNodes)
                {
                    var key = node.Attributes["name"]?.Value;
                    var value = node.SelectSingleNode("value")?.InnerText;

                    if (!string.IsNullOrEmpty(key))
                    {
                        dictionary[key] = value ?? "";
                    }
                }

                string json = JsonConvert.SerializeObject(dictionary, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(jsonFilePath, json);

                Console.WriteLine($"Successfully written: {jsonFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting {resxFilePath} to JSON: {ex.Message}");
            }
        }


        [HttpGet("GetTranslation/{culture}")]
        public IActionResult GetTranslation(string culture)
        {
            try
            {
                var resxFolders = new string[]
                {
            "Resources/Shared",
            "Resources/Home",
            "Resources/FatihaRequest",
            "Resources/FatihaExam",
            "Resources/AuthorizedUsers"
                };

                var translationData = new Dictionary<string, Dictionary<string, string>>();
                bool filesFound = false; // متغير لتتبع إذا تم العثور على أي ملفات أم لا
                var fileCount = new Dictionary<string, int>(); // تتبع عدد التكرارات لكل ملف

                foreach (var folder in resxFolders)
                {
                    string fullFolderPath = Path.Combine(Directory.GetCurrentDirectory(), folder);
                    if (!Directory.Exists(fullFolderPath))
                        continue;

                    // البحث عن أي ملف يحتوي على .{culture}.resx داخل المجلد
                    var resxFiles = Directory.GetFiles(fullFolderPath, $"*.{culture}.resx");

                    foreach (var resxFile in resxFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(resxFile);

                        // التحقق إذا كان الاسم مكررًا، وإضافة رقم تسلسلي له
                        if (fileCount.ContainsKey(fileName))
                        {
                            fileCount[fileName]++;
                            fileName = $"{fileName}_{fileCount[fileName]}";
                        }
                        else
                        {
                            fileCount[fileName] = 1;
                            fileName = $"{fileName}_1";
                        }

                        translationData[fileName] = LoadResxFile(resxFile);
                        filesFound = true;
                    }
                }

                if (!filesFound)
                {
                    return NotFound(new { Message = "No translation files found for the specified culture." });
                }

                return Ok(translationData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving translations.", Error = ex.Message });
            }
        }
        [HttpGet("GetLanguages")]
        public IActionResult GetLanguages()
        {
            var languages = Enum.GetValues(typeof(EnumCommon.Languagelist))
                                .Cast<EnumCommon.Languagelist>()
                                .Select(lang => new
                                {
                                    Id = (int)lang,
                                    LanguageName = lang.ToString() // تحويل اسم اللغة إلى نص
                                }).ToList();

            return Ok(languages);
        }

        private Dictionary<string, string> LoadResxFile(string resxFilePath)
        {
            var translations = new Dictionary<string, string>();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(resxFilePath);

            XmlNodeList dataNodes = xmlDoc.SelectNodes("//data");

            foreach (XmlNode node in dataNodes)
            {
                var key = node.Attributes["name"]?.Value;
                var value = node.SelectSingleNode("value")?.InnerText;

                if (!string.IsNullOrEmpty(key))
                {
                    translations[key] = value ?? "";
                }
            }

            return translations;
        }
    }
    }