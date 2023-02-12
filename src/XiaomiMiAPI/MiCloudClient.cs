using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using XiaomiMiAPI.Model;

namespace XiaomiMiAPI
{
    /// <summary>
    /// Xiaomi Mi cloud client. 
    /// Token extractor ported form here: https://github.com/PiotrMachowski/Xiaomi-cloud-tokens-extractor
    /// </summary>
    public class MiCloudClient : IDisposable
    {
        private struct MiLoginInfo
        {
            public string Ssecurity { get; set; }
            public string UserId { get; set; }
        }

        private static readonly Random Rand = new Random();

        private const int MiCloudDropLength = 1024;

        private HttpClient _client;
        private CookieContainer _cookieContainer;
        private HttpClientHandler _handler;

        public MiCloudClient()
        {
            _cookieContainer = new CookieContainer();
            _handler = new HttpClientHandler() { CookieContainer = _cookieContainer };
            _client = new HttpClient(_handler);

            InitializeHttpClient(GenerateDeviceID(), GenerateAgent());
        }

        private void InitializeHttpClient(string deviceID, string agent)
        {
            _cookieContainer.Add(new Cookie("sdkVersion", "accountsdk-18.8.15", "/", "mi.com"));
            _cookieContainer.Add(new Cookie("sdkVersion", "accountsdk-18.8.15", "/", "xiaomi.com"));
            _cookieContainer.Add(new Cookie("deviceId", deviceID, "/", "mi.com"));
            _cookieContainer.Add(new Cookie("deviceId", deviceID, "/", "xiaomi.com"));
            _client.DefaultRequestHeaders.Add("User-Agent", agent);
        }

        private async Task<MiLoginInfo> LoginAsync(string username, string password)
        {
            const string serviceLoginUri = "https://account.xiaomi.com/pass/serviceLogin?sid=xiaomiio&_json=true";
            const string loginAuthUri = "https://account.xiaomi.com/pass/serviceLoginAuth2";
            const string callbackUri = "https://sts.api.io.mi.com/sts";

            _cookieContainer.Add(new Cookie("userId", username, "/", "xiaomi.com"));

            // get sign
            string sign = null;

            var serviceLoginResult = await _client.GetAsync(serviceLoginUri);
            serviceLoginResult.EnsureSuccessStatusCode();

            string serviceLoginJson = await serviceLoginResult.Content.ReadAsStringAsync();
            if (serviceLoginJson.Contains("_sign"))
            {
                sign = JsonNode.Parse(GetValidJson(serviceLoginJson))["_sign"]?.AsValue().ToString();
            }

            if (string.IsNullOrEmpty(sign))
                throw new Exception("Cannot get _sign.");

            // sign in
            string location = null;
            string ssecurity = null;
            string userId = null;
            string serviceToken;

            var loginAuthForm = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("sid", "xiaomiio"),
                new KeyValuePair<string, string>("hash", GetPasswordHash(password)),
                new KeyValuePair<string, string>("callback", callbackUri),
                new KeyValuePair<string, string>("qs", "%3Fsid%3Dxiaomiio%26_json%3Dtrue"),
                new KeyValuePair<string, string>("user", username),
                new KeyValuePair<string, string>("_sign", sign),
                new KeyValuePair<string, string>("_json", "true"),
            };

            using (FormUrlEncodedContent loginAuthContent = new FormUrlEncodedContent(loginAuthForm))
            {
                var loginAuthResult = await _client.PostAsync(loginAuthUri, loginAuthContent);
                loginAuthResult.EnsureSuccessStatusCode();

                string loginAuthJson = await loginAuthResult.Content.ReadAsStringAsync();

                if (loginAuthJson.Contains("notificationUrl"))
                    throw new Exception("Two factor authentication required.");

                if (!loginAuthJson.Contains("ssecurity"))
                    throw new Exception("Invalid user name or password.");

                var loginAuthParsed = JsonNode.Parse(GetValidJson(loginAuthJson));

                ssecurity = loginAuthParsed["ssecurity"]?.AsValue().ToString();

                if (ssecurity.Length <= 4)
                    throw new Exception("Invalid ssecurity");

                userId = loginAuthParsed["userId"].AsValue().ToString();
                location = loginAuthParsed["location"].AsValue().ToString();

                // not needed:
                //_cUserId = loginAuthParsed["cUserId"].AsValue().ToString();
                //_passToken = loginAuthParsed["passToken"].AsValue().ToString();
                //_code = loginAuthParsed["code"].AsValue().ToString();
            }

            if (string.IsNullOrEmpty(location))
                throw new Exception("Cannot get location.");

            // get service token
            var locationResult = await _client.GetAsync(location);
            locationResult.EnsureSuccessStatusCode();

            var locationCookies = _cookieContainer.GetCookies(new Uri(location));
            serviceToken = locationCookies["serviceToken"].Value;

            if (string.IsNullOrEmpty(serviceToken))
                throw new Exception("Cannot get service token.");

            // prepare for API calls, set default cookies and headers
            const string miDomain = ".mi.com";
            _cookieContainer.Add(new Cookie("userId", userId, "/", miDomain));
            _cookieContainer.Add(new Cookie("yetAnotherServiceToken", serviceToken, "/", miDomain));
            _cookieContainer.Add(new Cookie("serviceToken", serviceToken, "/", miDomain));
            _cookieContainer.Add(new Cookie("channel", "MI_APP_STORE", "/", miDomain));

            _cookieContainer.Add(new Cookie("locale", "en_GB", "/", miDomain));
            _cookieContainer.Add(new Cookie("timezone", "GMT+02:00", "/", miDomain));
            _cookieContainer.Add(new Cookie("is_daylight", "1", "/", miDomain));
            _cookieContainer.Add(new Cookie("dst_offset", "3600000", "/", miDomain));

            _client.DefaultRequestHeaders.Add("Accept-Encoding", "identity");
            _client.DefaultRequestHeaders.Add("x-xiaomi-protocal-flag-cli", "PROTOCAL-HTTP2");
            _client.DefaultRequestHeaders.Add("MIOT-ENCRYPT-ALGORITHM", "ENCRYPT-RC4");

            return new MiLoginInfo()
            {
                Ssecurity = ssecurity,
                UserId = userId
            };
        }

        private static string GetValidJson(string invalidJson)
        {
            // there is &&&START&&& at the beginning of the response, strip it and get the JSON
            if (!invalidJson.StartsWith("{"))
            {
                int beginJson = invalidJson.IndexOf('{');
                invalidJson = invalidJson.Substring(beginJson);
            }

            return invalidJson;
        }

        private string GetPasswordHash(string password)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(password);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
            }
        }

        private static string GenerateAgent()
        {  
            return $"Android-7.1.1-1.0.0-ONEPLUS A3010-136-{GenerateRandomID(65, 69, 13)} APP/xiaomi.smarthome APPV/62830";
        }

        private static string GenerateDeviceID()
        {
            return GenerateRandomID(97, 122, 6);
        }

        private static string GenerateRandomID(int startAsciiCode, int endAsciiCode, int length)
        {
            char[] c = new char[length];
            for (int i = 0; i < c.Length; i++)
            {
                c[i] = (char)(startAsciiCode + Rand.Next(endAsciiCode - startAsciiCode));
            }
            return string.Concat(c);
        }

        private async Task<string> ExecuteCallAsync(string url, string data, string ssecurity)
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string nonce = GenerateNonce(timestamp);
            string signedNonce = SignNonce(nonce, ssecurity);
            var content = new Dictionary<string, string>
            {
                { "data", data },
            };
            var fields = GenerateParams(url, "POST", signedNonce, nonce, content, ssecurity);

            using (var formContent = new FormUrlEncodedContent(fields))
            {
                var response = await _client.PostAsync(url, formContent);
                response.EnsureSuccessStatusCode();
                response.Headers.TryGetValues("X-Xiaomi-Status-Code", out var xiaomiStatusCodeHeaders);

                string xiaomiStatusCode = xiaomiStatusCodeHeaders?.FirstOrDefault();
                
                // most likely cause of this error is a malformed JSON in the request, or error in the RC4 encryption
                if (!string.IsNullOrEmpty(xiaomiStatusCode))
                    throw new Exception($"Request error: {xiaomiStatusCode}");

                string responseString = await response.Content.ReadAsStringAsync();
                return DecryptRC4(responseString, signedNonce);
            }
        }

        private static Dictionary<string, string> GenerateParams(string url, string method, string signedNonce, string nonce, Dictionary<string, string> content, string ssecurity)
        {
            var fields = new Dictionary<string, string>();

            foreach (var item in content)
            {
                fields[item.Key] = EncryptRC4(item.Value, signedNonce);
            }

            fields["rc4_hash__"] = GenerateSignature(url, method, signedNonce, content);
            fields["signature"] = GenerateSignature(url, method, signedNonce, fields);
            fields["ssecurity"] = ssecurity;
            fields["_nonce"] = nonce;

            return fields;
        }

        private static string EncryptRC4(string payload, string key)
        {
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
            byte[] keyBytes = Convert.FromBase64String(key);
            return Convert.ToBase64String(ApplyRC4Drop(payloadBytes, keyBytes, MiCloudDropLength));
        }

        private static string DecryptRC4(string payload, string key)
        {
            byte[] payloadBytes = Convert.FromBase64String(payload);
            byte[] keyBytes = Convert.FromBase64String(key);
            return Encoding.UTF8.GetString(ApplyRC4Drop(payloadBytes, keyBytes, MiCloudDropLength));
        }

        private static byte[] ApplyRC4Drop(byte[] payload, byte[] key, int dropLength = 0)
        {
            RC4Engine cipher = new RC4Engine();
            KeyParameter keyParam = new KeyParameter(key);
            cipher.Init(true, keyParam); // RC4 is symmetric, it does not matter if we encrypt or decrypt, the first parameter is being ignored

            if (dropLength > 0)
            {
                // drop first N bytes, all initialized to 0
                byte[] dropBytes = new byte[dropLength];
                byte[] dropBytesOut = new byte[dropLength];
                cipher.ProcessBytes(dropBytes, 0, dropLength, dropBytesOut, 0);
            }

            byte[] output = new byte[payload.Length];
            cipher.ProcessBytes(payload, 0, payload.Length, output, 0);
            return output;
        }

        private static string GenerateSignature(string url, string method, string signedNonce, Dictionary<string, string> content)
        {
            var signatureParams = new List<string>
            {
                method.ToUpperInvariant(),
                url.Split(new string[] { "com" }, StringSplitOptions.None)[1].Replace("/app/", "/")
            };

            foreach (var item in content)
            {
                signatureParams.Add($"{item.Key}={item.Value}");
            }

            signatureParams.Add(signedNonce);

            string signatureString = string.Join("&", signatureParams);
            using (SHA1 sha = SHA1.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
                return Convert.ToBase64String(hash);
            }
        }

        private static string SignNonce(string nonce, string ssecurity)
        {
            byte[] securityBytes = Convert.FromBase64String(ssecurity);
            byte[] nonceBytes = Convert.FromBase64String(nonce);

            using (SHA256Managed sha256 = new SHA256Managed())
            { 
                byte[] hash = sha256.ComputeHash(securityBytes.Concat(nonceBytes).ToArray());
                return Convert.ToBase64String(hash);
            }
        }

        private static string GenerateNonce(long timestamp)
        {
            byte[] bytes = new byte[8 + 4];
            Rand.NextBytes(bytes);

            int minutes = (int)(timestamp / 60);

            // Big Endian
            bytes[8] = (byte)(minutes >> 24);
            bytes[9] = (byte)(minutes >> 16);
            bytes[10] = (byte)(minutes >> 8);
            bytes[11] = (byte)minutes;

            return Convert.ToBase64String(bytes);
        }

        private static string GetUri(string country)
        {
            string uriCountry = country.ToLowerInvariant() == "cn" ? "" : (country + ".");
            return $"https://{uriCountry}api.io.mi.com/app";
        }

        private Task<string> GetHomesAsync(string country, string ssecurity)
        {
            string url = $"{GetUri(country)}/v2/homeroom/gethome";
            string data = $"{{\"fg\": true, \"fetch_share\": true, \"fetch_share_dev\": true, \"limit\": 300, \"app_ver\": 7}}";
            return ExecuteCallAsync(url, data, ssecurity);
        }

        private Task<string> GetDevicesAsync(string country, string homeId, string ownerId, string ssecurity)
        {
            string url = $"{GetUri(country)}/v2/home/home_device_list";
            string data = $"{{\"home_owner\": {ownerId}, \"home_id\": {homeId}, \"limit\": 200, \"get_split_device\": true, \"support_smart_home\": true}}";
            return ExecuteCallAsync(url, data, ssecurity);
        }

        private Task<string> GetDeviceCountAsync(string country, string ssecurity)
        {
            string url = $"{GetUri(country)}/v2/user/get_device_cnt";
            string data = $"{{\"fetch_own\": true, \"fetch_share\": true}}";
            return ExecuteCallAsync(url, data, ssecurity);
        }

        private Task<string> GetBeaconKeyAsync(string country, string deviceId, string ssecurity)
        {
            string url = $"{GetUri(country)}/v2/device/blt_get_beaconkey";
            string data = $"{{\"did\":\"{deviceId}\", \"pdid\":1}}";
            return ExecuteCallAsync(url, data, ssecurity);
        }

        /// <summary>
        /// Retrieves all devices from the Mi cloud including the tokens.
        /// </summary>
        /// <param name="username">User name. Phone number in an international format or an email.</param>
        /// <param name="password">Xiaomi Mi cloud password.</param>
        /// <param name="serverLocation">Server location. When null, all servers will be searched. Available locations are: us, cn, de, ru, tw, sg, in, i2.</param>
        /// <returns><see cref="MiHome"/>.</returns>
        public async Task<MiHome[]> DiscoverDevicesAsync(string username, string password, string serverLocation = "us")
        {
            var availableServerLocations = new string[] { "us", "cn", "de", "ru", "tw", "sg", "in", "i2" };
            var result = new List<MiHome>();
            string[] serverLocations;

            // first log in
            var login = await LoginAsync(username, password);

            if (serverLocation != null && availableServerLocations.Contains(serverLocation.ToLowerInvariant()))
            {
                serverLocations = new string[] { serverLocation };
            }
            else
            {
                // try to search all available servers
                serverLocations = availableServerLocations;
            }

            foreach(string location in serverLocations)
            {
                var miHomes = new List<MiHome>();

                // get homes
                string homesResponse = await GetHomesAsync(location, login.Ssecurity);
                var homesParsed = JsonNode.Parse(homesResponse)["result"]?["homelist"]?.AsArray();

                if (homesParsed == null || homesParsed.Count == 0)
                {
                    continue;
                }

                foreach(var home in homesParsed)
                {
                    miHomes.Add(new MiHome() {
                        HomeId = home["id"]?.AsValue().ToString(), 
                        HomeOwner = login.UserId,
                        Name = home["name"]?.AsValue().ToString(),
                        ServerLocation = location
                    });
                }

                // TODO: not tested
                string deviceCountResponse = await GetDeviceCountAsync(location, login.Ssecurity);
                var deviceCountParsed = JsonNode.Parse(deviceCountResponse)["result"]?["share"]?["share_family"]?.AsArray();

                if (deviceCountParsed != null && deviceCountParsed.Count > 0)
                {
                    foreach (var home in deviceCountParsed)
                    {
                        miHomes.Add(new MiHome()
                        {
                            HomeId = home["home_id"]?.AsValue().ToString(),
                            HomeOwner = home["home_owner"]?.AsValue().ToString()
                        });
                    }
                }

                if (miHomes.Count == 0)
                {
                    continue; // no homes found on this server
                }
                
                foreach(var miHome in miHomes)
                {
                    var devicesResponse = await GetDevicesAsync(location, miHome.HomeId, miHome.HomeOwner, login.Ssecurity);
                    var devicesParsed = JsonNode.Parse(devicesResponse)["result"]?["device_info"]?.AsArray();
                    
                    if(devicesParsed == null || devicesParsed.Count == 0)
                    {
                        continue; // no devices found
                    }

                    foreach(var device in devicesParsed)
                    {
                        var miDevice = new MiDevice();
                        miDevice.Name = device["name"].AsValue().ToString();
                        miDevice.DeviceID = device["did"].AsValue().ToString();

                        // TODO not tested
                        if(!string.IsNullOrEmpty(miDevice.DeviceID) && miDevice.DeviceID.Contains("blt"))
                        {
                            var beaconKeyResponse = await GetBeaconKeyAsync(location, miDevice.DeviceID, login.Ssecurity);
                            if(beaconKeyResponse.Contains("result") && beaconKeyResponse.Contains("beaconkey"))
                            {
                                miDevice.BeaconKey = JsonNode.Parse(beaconKeyResponse)["result"]?["beaconkey"]?.AsValue().ToString();
                            }
                        }

                        miDevice.Mac = device["mac"]?.AsValue().ToString();
                        miDevice.LocalIP = device["localip"]?.AsValue().ToString();
                        miDevice.Token = device["token"]?.AsValue().ToString();
                        miDevice.Model = device["model"]?.AsValue().ToString();

                        miHome.Devices.Add(miDevice);
                    }

                    result.Add(miHome);
                }
            }

            return result.ToArray();
        }

        #region IDisposable

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_client != null)
                    {
                        _client.Dispose();
                        _client = null;
                    }

                    if(_handler != null)
                    {
                        _handler.Dispose();
                        _handler = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion // IDisposable
    }
}
