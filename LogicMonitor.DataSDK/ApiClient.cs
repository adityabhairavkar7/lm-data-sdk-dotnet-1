﻿/*
 * Copyright, 2021, LogicMonitor, Inc.
 * This Source Code Form is subject to the terms of the 
 * Mozilla Public License, v. 2.0. If a copy of the MPL 
 * was not distributed with this file, You can obtain 
 * one at https://mozilla.org/MPL/2.0/.
 */
using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using RestSharp;

namespace LogicMonitor.DataSDK
{

    /// <summary>
    /// This class is Controller.
    /// </summary>
    public class ApiClient 
    {
        public Configuration configuration;
        private Rest rest_client;
        private readonly Dictionary<string, string> default_headers = new Dictionary<string, string>();
        public ApiClient() { }
        public ApiClient(Configuration configuration)
        {

            if (configuration == null)
                configuration = new Configuration();
            this.configuration = configuration;
            this.rest_client = new Rest();
            // Set default API version
            default_headers.Add("X-version", "1");
        }

        public RestResponse Callapi(
              string path,
              string method,
              TimeSpan _request_timeout,
              Dictionary<string, string> queryParams = default,
              Dictionary<string, string> headerParams = default,
              string body = null,
              string authSetting = default

              )
        {

            var url = this.configuration.host + path;
            this.Update_params_for_auth(headerParams, queryParams, authSetting, path, method, body);
            var response_data = this.Request(method: method, url: url, queryParams: queryParams, _request_timeout: _request_timeout, headers: headerParams, body: body);


            Console.WriteLine("Response: {0}", response_data.Content.ToString());
            return response_data;
        }

        public RestResponse CallApi(
            string path,string method,TimeSpan _request_timeout,Dictionary<string, string> queryParams = default,
            Dictionary<string, string> headerParams = default,string body = default,string authSetting = null,bool asyncRequest = true)
        {
            if (!asyncRequest)
            {
                return this.Callapi(path: path, method: method, _request_timeout: _request_timeout, headerParams: headerParams, queryParams: queryParams, body: body, authSetting: authSetting);
            }
            else
            {
                return CallAsync(path, method, _request_timeout, queryParams, headerParams, body, authSetting)
                    .Result;
            }
        }

        public async Task<RestResponse> CallAsync(
            string path,
            string method,
            TimeSpan _request_timeout,
            Dictionary<string, string> queryParams = null,
            Dictionary<string, string> headerParams = null,
            string body = null,
            string authSetting = null
  
            )
        {
            var thread = await Task.FromResult(this.Callapi(path: path, method: method, _request_timeout: _request_timeout, headerParams: headerParams, queryParams: queryParams, body: body, authSetting: authSetting));
            return thread;
        }

        public RestResponse Request(
            string method,
            string url,
            TimeSpan _request_timeout,
            string body,
            Dictionary<string, string> queryParams = default,
            Dictionary<string, string> headers = default,
            Dictionary<string, string> post_params = default

             )
        {
            if (rest_client == null)
                rest_client = new Rest();
            
            if (method == "GET")
            {
                return rest_client.Get("GET", url, queryParams: queryParams, requestTimeout: _request_timeout, headers: headers, body: body);
            }
            else if (method == "POST")
            {

                return rest_client.Post("POST", url, queryParams: queryParams, headers: headers, postParams: post_params, requestTimeout: _request_timeout, body: body);

            }
            else if (method == "DELETE")
            {
                return this.rest_client.Delete("DELETE", url, headers: headers, requestTimeout: _request_timeout, body: body, queryParams: queryParams);
            }
            else
            {
                throw new ArgumentException("http method must be `GET`, `POST`, `PATCH`, `PUT` or `DELETE`.");
            }
        }

        public string SelectHeaderAccept(string accepts)
        {
            if (accepts == "")
            {
                return null;
            }

            if (accepts.Contains("application/json"))
            {
                return "application/json";
            }
            else
            {
                return (accepts);
            }
        }
        public string SelectHeaderContentType(string content_types)
        {
            if (content_types == "")
            {
                return "application/json";
            }

            if (content_types.Contains("application/json") || content_types.Contains("*/*"))
            {
                return "application/json";
            }
            else
            {
                return content_types;
            }
        }

        public bool Update_params_for_auth(
                Dictionary<string, string> headers,
                Dictionary<string, string> querys,
                string auth_settings,
                string resource_path,
                string method,
                string body = null)
        {
            string msg;
            if (auth_settings == null)
            {
                return false;
            }

            var auth_setting = this.configuration.authentication;
            if (auth_setting.Key != null && auth_setting.Id !=null)
            {
                if (auth_setting.Type == "Bearer" )
                {
                    headers.Add("Authorization", string.Format("Bearer={0}", auth_setting.Key));
                    return true;
                }
                else
                {
                    DateTimeOffset n = DateTimeOffset.UtcNow;
                    long epoch = n.ToUnixTimeMilliseconds();
                    if (body != null)
                    {
                        //body is serialized
                        msg = method + epoch + body + resource_path;
                    }
                    else
                    {
                        msg = method + epoch + resource_path;
                    }
                    // Construct signature
                    string hmac = HmacSHA256(auth_setting.Key, msg);
                    var a = System.Text.Encoding.UTF8.GetBytes(hmac);
                    string signature = Convert.ToBase64String(a);
                    var auth_hash = "LMv1 " + auth_setting.Id + ":" + signature + ":" + epoch;

                    headers.Add("Authorization", auth_hash);
                    return true;

                }
            }
            else
            {
                throw new ArgumentException("Authentication token must be in `header`");
            }

        }

        public static string HmacSHA256(string key, string data)
        {
            string hash;
            ASCIIEncoding encoder = new ASCIIEncoding();
            var code = System.Text.Encoding.UTF8.GetBytes(key);
            using (HMACSHA256 hmac = new HMACSHA256(code))
            {
                Byte[] hmBytes = hmac.ComputeHash(encoder.GetBytes(data));
                hash = ToHexString(hmBytes);
            }
            return hash;
        }

        public static string ToHexString(byte[] array)
        {
            StringBuilder hex = new StringBuilder(array.Length * 2);
            foreach (byte b in array)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
    }
}






