﻿using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading.Tasks;

namespace HttpListenerExample
{
    class HttpServer
    {
        public static HttpListener listener;
        public static string url = "http://localhost:8000/";
        public static int pageViews = 0;
        public static int requestCount = 0;
        public static string pageData;
        public static string cssData;
        public static DateTime start;
        public static List<string> publics = new List<string>();



        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;

            while (runServer)
            {
                // Create new Task for individual connections
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Request info
                Console.WriteLine();
                Console.WriteLine("Request #: {0}", ++requestCount);
                Console.WriteLine("URL      : " + req.Url.ToString());
                Console.WriteLine("Method   : " + req.HttpMethod);
                Console.WriteLine("Host     : " + req.UserHostName);
                Console.WriteLine("UserAgent: " + req.UserAgent);
                Console.WriteLine("Endpoint : " + req.Url.AbsolutePath);
                Console.WriteLine("Query    : " + req.Url.Query);
                // Bei /shutdown stoppen
                byte[] data;
                string endpoint = req.Url.AbsolutePath;
                string windowsPath = endpoint.Replace('/', '\\');
                if (windowsPath.StartsWith("\\"))
                {
                    windowsPath = windowsPath.Substring(1);
                }
                string path = Path.Combine(Directory.GetCurrentDirectory(), windowsPath);
                if(endpoint == "/") {
                    path = Path.Combine(path, "main\\index.html");
                }
                Console.WriteLine("Accesing : {0}", path);
                Console.WriteLine();
                if (File.Exists(path))
                {
                    if (true)
                    {
                        pageData = File.ReadAllText(path, Encoding.UTF8);

                        int headindex = pageData.IndexOf("<head>");
                        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "log.txt"), pageData);
                        //CSS einfügen
                        data = Encoding.UTF8.GetBytes(pageData);
                        resp.ContentType = "text/html; charset=utf-8";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    }
                    else
                    {
                        resp.StatusCode = 403;
                        resp.StatusDescription = "Forbidden";
                        byte[] notFoundData = Encoding.UTF8.GetBytes("403 - Forbidden");
                        resp.ContentType = "text/plain";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = notFoundData.LongLength;
                        await resp.OutputStream.WriteAsync(notFoundData, 0, notFoundData.Length);
                    }
                }
                else
                {
                    if (req.Url.AbsolutePath == "/shutdown" && req.Url.Query == "?pwd=537")
                    {
                        data = Encoding.UTF8.GetBytes("Shutting down...");
                        resp.ContentType = "text/plain";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Server shutdown initiatet by {req.RemoteEndPoint.Address} !!");
                        Console.ResetColor();
                        runServer = false;
                    }
                    else if(req.Url.AbsolutePath == "/shutdown")
                    {
                        resp.StatusCode = 403;
                        resp.StatusDescription = "Forbidden";
                        byte[] notFoundData = Encoding.UTF8.GetBytes("403 - Forbidden");
                        resp.ContentType = "text/plain";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = notFoundData.LongLength;
                        await resp.OutputStream.WriteAsync(notFoundData, 0, notFoundData.Length);
                    }
                    else if (req.Url.AbsolutePath == "/count")
                    {
                        data = Encoding.UTF8.GetBytes($"Requests: {requestCount} since {start.ToString("dddd, dd.MM yyyy HH:mm:ss.ff")}({start - DateTime.Now})");
                        resp.ContentType = "text/plain";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
                    }
                    else
                    {
                        // Handle unknown endpoints (send 404)
                        resp.StatusCode = 404;
                        resp.StatusDescription = "Not Found";
                        byte[] notFoundData = Encoding.UTF8.GetBytes("404 - Page not found");
                        resp.ContentType = "text/plain";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = notFoundData.LongLength;
                        await resp.OutputStream.WriteAsync(notFoundData, 0, notFoundData.Length);
                    }
                }
                // Specific requests dont increast the count.
                if (req.Url.AbsolutePath != "/favicon.ico")
                    pageViews += 1;
                resp.Close();
            }
            Console.WriteLine("Close this window to exit!");
            while (true)
                Console.ReadLine();
        }


        public static void Main(string[] args)
        {
            // Create a Http server and start listening for incoming connections

            listener = new HttpListener();
            listener.Prefixes.Add(url);
            Console.WriteLine("Starting server...");
            listener.Start();
            start = DateTime.Now;
            Console.WriteLine("Listening for connections on {0}", url);

            // Start handling requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Exit
            listener.Close();
        }
    }
}
