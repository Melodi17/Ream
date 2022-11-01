using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Libraries
{
    public class Internet
    {
        public static InternetResponse Get(string url, Dictionary<object, object> headers, Dictionary<object, object> cookies, string contentType)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            if (contentType != null)
            {
                request.ContentType = contentType;
            }
            if (headers != null)
            {
                request.Headers = new WebHeaderCollection();
                foreach (KeyValuePair<object, object> header in headers)
                {
                    request.Headers.Add(header.Key.ToString(), header.Value.ToString());
                }
            }
            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                foreach (KeyValuePair<object, object> cookie in cookies)
                {
                    request.CookieContainer.Add(new Cookie(cookie.Key.ToString(), cookie.Value.ToString()));
                }
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return new InternetResponse(response);
        }

        public static InternetResponse Post(string url, Dictionary<object, object> headers, Dictionary<object, object> cookies, string contentType, string postData)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            if (contentType != null)
            {
                request.ContentType = contentType;
            }
            if (headers != null)
            {
                request.Headers = new WebHeaderCollection();
                foreach (KeyValuePair<object, object> header in headers)
                {
                    request.Headers.Add(header.Key.ToString(), header.Value.ToString());
                }
            }
            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                foreach (KeyValuePair<object, object> cookie in cookies)
                {
                    request.CookieContainer.Add(new Cookie(cookie.Key.ToString(), cookie.Value.ToString()));
                }
            }
            if (postData != null)
            {
                byte[] data = Encoding.UTF8.GetBytes(postData);
                request.ContentLength = data.Length;
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return new InternetResponse(response);
        }
    }

    public class InternetResponse
    {
        public int Status { get; private set; }
        public string Body { get; private set; }
        public List<object> Bytes { get; private set; }
        public Dictionary<object, object> Headers { get; private set; }

        public InternetResponse(HttpWebResponse response)
        {
            Status = (int)response.StatusCode;
            Body = new StreamReader(response.GetResponseStream()).ReadToEnd();
            Bytes = new List<object>();
            foreach (byte b in Body)
                Bytes.Add(b);
            Headers = new Dictionary<object, object>();
            foreach (string header in response.Headers.AllKeys)
            {
                Headers.Add(header, response.Headers[header]);
            }
        }
    }
}
