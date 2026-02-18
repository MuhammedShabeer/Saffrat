
namespace Saffrat.Helpers
{
    public class Uploader
    {
        // Upload Image In Public Folder
        public static string UploadImage(IFormFile file)
        {
            if (file != null)
            {
                var allowedExtensions = new[] {
                    ".Jpg", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".ico"
                };
                var ext = Path.GetExtension(file.FileName);
                if (allowedExtensions.Contains(ext))
                {
                    string filename = Path.GetFileName(file.FileName);
                    string name = Path.GetFileNameWithoutExtension(filename);

                    string myfile = name + ext;

                    string paths = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/"));

                    string fullpath = Path.Combine(paths, myfile);

                    if (!Directory.Exists(paths))
                        Directory.CreateDirectory(paths);

                    if (File.Exists(fullpath))
                        myfile = Guid.NewGuid().ToString() + ext;

                    fullpath = Path.Combine(paths, myfile);

                    string res = "/uploads/" + myfile;
                    using FileStream stream = new(fullpath, FileMode.Create);
                    file.CopyTo(stream);
                    stream.Close();
                    return res;
                }
                return null;
            }
            return null;
        }

        // Delete File From Public Folder
        public static void DeleteFile(string path)
        {
            try
            {
                if (String.IsNullOrEmpty(path))
                    return;

                string paths = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + path));
                FileInfo file = new(paths);
                if (file.Exists)//check file exsit or not  
                {
                    file.Delete();
                }
                else
                {
                    return;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // Upload Image In Private Folder
        public static Dictionary<string, string> UploadImageMedia(int company, string category, IFormFile file)
        {
            var res = new Dictionary<string, string>();
            if (file != null)
            {
                var allowedExtensions = new[] {
                    ".Jpg", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".ico"
                };
                var ext = Path.GetExtension(file.FileName);
                if (file.Length > 2097152)
                {
                    res.Add("status", "error");
                    res.Add("message", "Maximum file upload size is 2MB.");
                }
                else if (allowedExtensions.Contains(ext))
                {
                    string myfile = Guid.NewGuid().ToString() + ext;

                    string paths = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "AppFiles/Company/"));
                    if (!Directory.Exists(paths))
                        Directory.CreateDirectory(paths);

                    paths = Path.GetFullPath(Path.Combine(paths, company.ToString()));
                    if (!Directory.Exists(paths))
                        Directory.CreateDirectory(paths);

                    paths = Path.GetFullPath(Path.Combine(paths, category));
                    if (!Directory.Exists(paths))
                        Directory.CreateDirectory(paths);

                    string fullpath = Path.Combine(paths, myfile);

                    using FileStream stream = new(fullpath, FileMode.Create);
                    file.CopyTo(stream);
                    stream.Close();

                    res.Add("status", "success");
                    res.Add("message", myfile);
                }
                else
                {
                    res.Add("status", "error");
                    res.Add("message", "File format not supported.");
                }
                return res;
            }
            return null;
        }

        // Upload Document In Private Folder
        public static Dictionary<string, string> UploadDocumentMedia(int company, string category, IFormFile file)
        {
            var res = new Dictionary<string, string>();
            if (file != null)
            {
                var allowedExtensions = new[] {
                    ".Jpg", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp", ".ico", ".pdf"
                };
                var ext = Path.GetExtension(file.FileName);
                if (file.Length > 2097152)
                {
                    res.Add("status", "error");
                    res.Add("message", "Maximum file upload size is 2MB.");
                }
                else if (allowedExtensions.Contains(ext))
                {
                    string myfile = Guid.NewGuid().ToString() + ext;

                    string paths = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "AppFiles/Company/"));
                    if (!Directory.Exists(paths))
                        Directory.CreateDirectory(paths);

                    paths = Path.GetFullPath(Path.Combine(paths, company.ToString()));
                    if (!Directory.Exists(paths))
                        Directory.CreateDirectory(paths);

                    paths = Path.GetFullPath(Path.Combine(paths, category));
                    if (!Directory.Exists(paths))
                        Directory.CreateDirectory(paths);

                    string fullpath = Path.Combine(paths, myfile);

                    using FileStream stream = new(fullpath, FileMode.Create);
                    file.CopyTo(stream);
                    stream.Close();

                    res.Add("status", "success");
                    res.Add("message", myfile);
                }
                else
                {
                    res.Add("status", "error");
                    res.Add("message", "File format not supported.");
                }
                return res;
            }
            return null;
        }

        // Delete File From Private Folder
        public static void DeleteMedia(int company, string category, string filename)
        {
            try
            {
                string path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), String.Format("AppFiles/{0}/{1}/{2}", company, category, filename)));
                FileInfo file = new(path);
                if (file.Exists)//check file exsit or not  
                {
                    file.Delete();
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
