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


        public static void Main(string[] args)
        {
            // SearchMissingImages(args);
            // CopyFoundImages(args);
            listImageLinksFromBlogBackup(args);
            
        }
    }
}
