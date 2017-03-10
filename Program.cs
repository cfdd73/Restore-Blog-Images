using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Http;

namespace ConsoleApplication
{
    public class Program
    {
        
        public IDictionary<String, FileInfo> scan(DirectoryInfo root, string[] missingImages, StreamWriter writer, IDictionary<String, FileInfo> map)
        {
            if (map.Count>=missingImages.Length)
                return map;
            foreach(DirectoryInfo dir in root.EnumerateDirectories())
            {
                scan(dir, missingImages, writer, map);
            }
            
            foreach(FileInfo file in root.EnumerateFiles())
            {
                if (file.Name.ToLower().EndsWith(".jpg") &&
                    !map.ContainsKey(file.Name) && 
                    Array.IndexOf(missingImages, file.Name) >= 0)
                    {
                        map.Add(file.Name,file);
                        writer.WriteLine(file.FullName);
                        writer.Flush();
                    }
            }
            return map;
        }

        public static void SearchMissingImages(string[] args)
        {
            Program prog = new Program();
            string[] missingImages = File.ReadAllLines("Resources/image-list.txt");
            string outputFile = "Resources/images-found.txt";
            FileStream fs = File.Create(outputFile);
            var writer = new System.IO.StreamWriter(fs);
            IDictionary<String, FileInfo> map = prog.scan(Directory.CreateDirectory("/Volumes/photo/sorted"), missingImages, writer, new Dictionary<String, FileInfo>());
            Console.WriteLine($"found images:{+map.Count}/{missingImages.Length}");    
        }

        public static void CopyFoundImages(string[] args)
        {
            string foundImagesFile = "Resources/images-found.txt";
            String destDir="/Users/ericlouvard/Pictures/Google+/Blog";
            string[] foundImages = File.ReadAllLines(foundImagesFile);
            DirectoryInfo di = new DirectoryInfo(destDir);
            foreach(String filePath in foundImages)
            {
                FileInfo fi = new FileInfo(filePath);
                string destPath = destDir+"/"+fi.Name;
                Console.WriteLine($"Copy {filePath} to {destPath}");
                if (!File.Exists(destPath))
                    File.Copy(filePath,destPath);
            }
        }

        public static void listImageLinksFromBlogBackup(string[] args)
        {
            string blogBackupPath = "Resources/blog-03-05-2017.xml";
            string blogContent = File.ReadAllText(blogBackupPath); 
            string patternHref = "href=\"(http[s]{0,1}:[a-zA-Z0-9\\-/\\._:]*jpg)\"";
            // string patternUrl = "http[s]{0,1}:[a-zA-Z0-9\\-/\\._:]*jpg";
            
            List<string> visitedUrl = new List<string>();
            IDictionary<String, HttpStatusCode> visitedUrlMap = new Dictionary<String, HttpStatusCode>();
            string outputFile = "Resources/missing-from-backup-2.txt";
            FileStream fs = File.Create(outputFile);
            var writer = new System.IO.StreamWriter(fs);
            using (HttpClient client = new HttpClient())
            {
                int countVisited=0;
                int countReached=0;
                int countMissing=0;
                using(writer=new System.IO.StreamWriter(fs))
                {
                    writer.WriteLine($"[{outputFile}");
                    foreach (Match match in Regex.Matches(blogContent, patternHref, RegexOptions.IgnoreCase))
                    {
                        countVisited++;
                        string url=match.Groups[1].Value;
                        // Console.WriteLine($"Query {url}");
                        if (visitedUrlMap.ContainsKey(url))
                            continue;
                        countReached++;
                        using (HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult())
                        {
                            HttpStatusCode respCode = (response).StatusCode;
                            visitedUrlMap.Add(url,respCode); 
                            if (respCode!=HttpStatusCode.OK)
                            {
                                Console.WriteLine($"[{countMissing}] {url} {respCode}");
                                writer.WriteLine($"[{countMissing}] {url} {respCode}");
                                countMissing++;
                            }
                        } 
                    }
                }
                Console.WriteLine($" visited:{countVisited} reached:{countReached} missing:{countMissing}");
            } 
        }

        public static IDictionary<string, string> getMissingLinksTable(string[] args)
        {
            IDictionary<string, string> ret = new Dictionary<string, string>();
            string missingFilePath = "Resources/missing-from-backup.txt";
            string[] missingFile = File.ReadAllLines(missingFilePath);
            // [236] http://1.bp.blogspot.com/-MXPlbpmoflo/UzXYDWmJS6I/AAAAAAAABJY/1wk6gWJ_D9Y/s1600/DSC04442.JPG Forbidden
            string patternUrl = "(http[s]{0,1}:[a-zA-Z0-9\\-/\\._:]*.jpg)";
            string patternFilename = "http[s]{0,1}:[a-zA-Z0-9\\-/\\._:]*/([a-zA-Z0-9\\-/\\._]*.jpg)";

            int i=0;
            foreach(string line in missingFile)
            {
                string filename=Regex.Match(line, patternFilename, RegexOptions.IgnoreCase).Groups[1].Value;
                string url = Regex.Match(line, patternUrl, RegexOptions.IgnoreCase).Groups[1].Value;
                if (!ret.ContainsKey(filename))
                {
                    // Console.WriteLine($"[{i++}] {filename} {url}");
                    ret.Add(filename,url);
                }
            }
            return ret;
        }

        public static void listAllMissingImagesFromBackup(string[] args)
        {
            string blogBackupPath = "Resources/blog-03-08-2017.xml";
            string blogContent = File.ReadAllText(blogBackupPath); 
            string blogContentFixed = blogContent; 
            string patternUrl = "(http[s]{0,1}:[a-zA-Z0-9\\-/\\._:% ]*\\.[jpegpng]{3,4})";
            int countVisited=0;
            int countMissing=0;
            int countReached=0;
            string outputFile = "Resources/listAllMissingImagesFromBackup.txt";
            FileStream fs = File.Create(outputFile);
            var writer = new System.IO.StreamWriter(fs);
            IDictionary<String, HttpStatusCode> visitedUrlMap = new Dictionary<String, HttpStatusCode>();
            using (HttpClient client = new HttpClient())
            {
                foreach (Match match in Regex.Matches(blogContent, patternUrl, RegexOptions.IgnoreCase))
                {
                    string url=match.Groups[1].Value;
                    countVisited++;
                    if (visitedUrlMap.ContainsKey(url))
                        continue;
                    writer.WriteLine($"{url}");
                    using (HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult())
                    {
                        HttpStatusCode respCode = (response).StatusCode;
                        visitedUrlMap.Add(url,respCode); 
                        if (respCode!=HttpStatusCode.OK)
                        {
                            Console.WriteLine($"[{countMissing}] {url} {respCode}");
                            writer.WriteLine($"[{countMissing}] {url} {respCode}");
                            countMissing++;
                        }
                        else
                        {
                            countReached++;
                        }
                    } 
                }
            }
            Console.WriteLine($" visited:{countVisited} reached:{countReached} missing:{countMissing}");
        }

        public static void replaceMissingLinksTable(IDictionary<string, string> validTable)
        {
            string blogBackupPath = "Resources/blog-03-08-2017.xml";
            string blogContent = File.ReadAllText(blogBackupPath); 
            string blogContentFixed = blogContent; 
            string patternUrl = "(http[s]{0,1}:[a-zA-Z0-9\\-/\\._:]*.jpg)";
            string patternFilename = "http[s]{0,1}:[a-zA-Z0-9\\-/\\._:]*/(s[0-9]{1,4})/([a-zA-Z0-9\\-/\\._]*.jpg)";
            
            int iFixCount=0;
            int iSkipCount=0;
            foreach (Match match in Regex.Matches(blogContent, patternUrl, RegexOptions.IgnoreCase))
            {
                string url=match.Groups[1].Value;
                Match m = Regex.Match(url, patternFilename, RegexOptions.IgnoreCase);
                string size=m.Groups[1].Value;
                string filename=m.Groups[2].Value;
                string validUrl;
                if(!validTable.TryGetValue(filename,out validUrl))
                {
                    iSkipCount++;
                    Console.WriteLine($"can't fix {filename}");
                }
                else
                {
                    iFixCount++;
                    // need to fix the size
                    string sizeInValidUrl=Regex.Match(validUrl, patternFilename, RegexOptions.IgnoreCase).Groups[1].Value;
                    string validUrlWithSize=validUrl.Replace(sizeInValidUrl,size);
                    // Console.WriteLine($"fix {filename} {size} {validUrlWithSize}");  
                    Console.WriteLine($"fix {url} -> {validUrlWithSize}");  
                    blogContentFixed=blogContentFixed.Replace(url,validUrlWithSize);                  
                }
            }
            Console.WriteLine($"URL fixed:{iFixCount} URL skiped:{iSkipCount}");                    
            string blogBackupFixedPath = "Resources/blog-03-08-2017.xml";
            File.WriteAllText(blogBackupFixedPath,blogContentFixed);
        }

        public static void replaceMissingLinksTable00(IDictionary<string, string> validTable)
        {
            string missingFilePath = "Resources/missing-from-backup.txt";
            string[] missingFile = File.ReadAllLines(missingFilePath);
            // [236] http://1.bp.blogspot.com/-MXPlbpmoflo/UzXYDWmJS6I/AAAAAAAABJY/1wk6gWJ_D9Y/s1600/DSC04442.JPG Forbidden
            string patternUrl = "(http[s]{0,1}:[a-zA-Z0-9\\-/\\._:]*.jpg)";
            string patternFilename = "http[s]{0,1}:[a-zA-Z0-9\\-/\\._:]*/(s[0-9]{1,4})/([a-zA-Z0-9\\-/\\._]*.jpg)";

            int iFixCount=0;
            int iSkipCount=0;
            foreach(string line in missingFile)
            {
                string size=Regex.Match(line, patternFilename, RegexOptions.IgnoreCase).Groups[1].Value;
                string filename=Regex.Match(line, patternFilename, RegexOptions.IgnoreCase).Groups[2].Value;
                string url = Regex.Match(line, patternUrl, RegexOptions.IgnoreCase).Groups[1].Value;
                string validUrl;
                if(!validTable.TryGetValue(filename,out validUrl))
                {
                    iSkipCount++;
                    Console.WriteLine($"can't fix {filename}");
                }
                else
                {
                    iFixCount++;
                    // need to fix the size
                    string patternSize = "(http[s]{0,1}:[a-zA-Z0-9\\-/\\._:]*.jpg)";
                    Console.WriteLine($"fix {filename} {size} {validUrl}");                    
                }
            }
            Console.WriteLine($"URL fixed:{iFixCount} URL skiped:{iSkipCount}");                    
            
        }

        public static IDictionary<string, string> getValidLinksTable(string[] args)
        {
            IDictionary<string, string> ret = new Dictionary<string, string>();
            string validFilePath = "Resources/all-valid-links.html";
            string validFileContent = File.ReadAllText(validFilePath);
            // <a href="https://1.bp.blogspot.com/-vvIvCVl3mUA/WLnU64cG9oI/AAAAAAABhoc/z3Vx4W47luEjLeFKnrxPX0cOX5qPDQlkwCPcB/s1600/DSC04376.JPG" imageanchor="1"><img border="0" height="150" src="https://1.bp.blogspot.com/-vvIvCVl3mUA/WLnU64cG9oI/AAAAAAABhoc/z3Vx4W47luEjLeFKnrxPX0cOX5qPDQlkwCPcB/s200/DSC04376.JPG" width="200" />            string patternUrl = "(http[s]{0,1}:[a-zA-Z0-9\\-/\\._:]*.jpg)";
            // TODO
            
            string patternUrl = "(http[s]{0,1}:[a-zA-Z0-9\\-/\\._:]{80,150}/[a-zA-Z0-9\\-/\\._]*.jpg)";
            string patternFilename = "http[s]{0,1}:[a-zA-Z0-9\\-/\\._:]*/([a-zA-Z0-9\\-/\\._]*.jpg)";

            int i=0;
            foreach(Match match in Regex.Matches(validFileContent, patternUrl, RegexOptions.IgnoreCase))
            {
                string url = match.Groups[1].Value;
                string filename=Regex.Match(url, patternFilename, RegexOptions.IgnoreCase).Groups[1].Value;
                if (!ret.ContainsKey(filename))
                {
                    // Console.WriteLine($"[{i++}] {filename} {url}");
                    ret.Add(filename,url);
                }
            }
            return ret;
        }

        public static void joinMissingToValid(string[] args)
        {
            IDictionary<string, string> validDict = getValidLinksTable(args);
            replaceMissingLinksTable(validDict);   
        }

        public static void Main(string[] args)
        {
            // SearchMissingImages(args);
            // CopyFoundImages(args);
            // listImageLinksFromBlogBackup(args);
            // joinMissingToValid(args);
            listAllMissingImagesFromBackup(args);
        }
    }
}
