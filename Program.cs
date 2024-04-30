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



        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;

            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Request info
                Console.WriteLine("Request #: {0}", ++requestCount);
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine(req.Url.AbsolutePath);

                // Bei /shutdown stoppen
                byte[] data;
                string endpoint = req.Url.AbsolutePath;
                string windowsPath = endpoint.Replace('/', '\\');
                if (windowsPath.StartsWith("\\"))
                {
                    windowsPath = windowsPath.Substring(1);
                }
                string path = Path.Combine(Directory.GetCurrentDirectory(), windowsPath, "index.html");
                Console.WriteLine("Accesing {0}", path);
                Console.WriteLine();
                if (File.Exists(path))
                {
                    Console.WriteLine(path);
                    pageData = File.ReadAllText(path);

                    int headindex = pageData.IndexOf("<head>");
                    Console.WriteLine(headindex);
                    pageData = pageData.Insert(headindex, cssData);
                    File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "log.txt"), pageData);
                    //CSS einfügen
                    data = Encoding.UTF8.GetBytes(pageData);
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;
                    await resp.OutputStream.WriteAsync(data, 0, data.Length);
                }
                else
                {
                    if (req.Url.AbsolutePath == "/shutdown")
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
                // Aufrufe nicht bei zusatzdateien erhöhen
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
            try
            {
                cssData = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "style.css"));
                Console.WriteLine("CSS loaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load css file!");
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }

            cssData = cssData.Insert(0, "<style>\r\n");
            cssData += "</style>\r\n";
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            Console.WriteLine("Starting server...");
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }
    }
}