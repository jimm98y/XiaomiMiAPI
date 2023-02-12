using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using XiaomiMiAPI.API;
using System.Security.Cryptography;
using System.Net;
using System.Net.Sockets;
using XiaomiMiAPI.Model;
using System.Text.Json.Nodes;
using System.Collections;
using System.Threading.Tasks;

namespace XiaomiMiAPI
{
    /// <summary>
    /// Yeelight client. 
    /// Documentation of the supported methods is here: https://www.yeelight.com/download/Yeelight_Inter-Operation_Spec.pdf
    /// </summary>
    public class YeelightClient : IDisposable
    {
        private const int YEELIGHT_PORT = 54321;

        private UdpClient _udpClient;
        private IPEndPoint _remoteEndpoint;

        private byte[] _token;
        private byte[] _key;
        private byte[] _iv;
        private uint _deviceID;
        private uint _stamp;

        private int _messageId = 1;

        /// <summary>
        /// Connect to the light.
        /// </summary>
        /// <param name="ipAddress">IP address of the light.</param>
        /// <param name="token">Token encoded in HEX string. Can be retrieved using <see cref="MiCloudClient"/>.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task ConnectAsync(string ipAddress, string token)
        {
            if (string.IsNullOrEmpty(ipAddress))
                throw new ArgumentNullException(nameof(ipAddress));

            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            _token = StringToByteArray(token);
            _remoteEndpoint = new IPEndPoint(IPAddress.Parse(ipAddress), YEELIGHT_PORT);
            await ConnectInternal();
        }

        /// <summary>
        /// Toggle the light.
        /// </summary>
        /// <returns>true if successful, false otherwise.</returns>
        public async Task<bool> ToggleAsync()
        {
            return (await SendMessageAsync("toggle"))[0] == "ok";
        }

        /// <summary>
        /// Turn on/off the light.
        /// </summary>
        /// <param name="mode"><see cref="YeelightMode"/>.</param>
        /// <param name="duration">Duration of the effect.</param>
        /// <returns>true if successful, false otherwise.</returns>
        public async Task<bool> SetPowerAsync(bool turnOn, YeelightMode mode = YeelightMode.Smooth, int duration = 500)
        {
            if (duration < 30)
                throw new ArgumentOutOfRangeException("Minimal supported duration is 30 ms");

            return (await SendMessageAsync("set_power", turnOn ? "on" : "off", GetMode(mode), duration))[0] == "ok";
        }

        /// <summary>
        /// Get the current power state.
        /// </summary>
        /// <returns>true if turned on, false if turned off.</returns>
        public async Task<bool> GetPowerAsync()
        {
            string result = (await SendMessageAsync("get_prop", "power"))[0];

            if (string.IsNullOrEmpty(result))
                throw new NotSupportedException();

            return result == "on";
        }

        /// <summary>
        /// Set brightness of the light.
        /// </summary>
        /// <param name="brightness">Brightness in percents. Supported range is 1 to 100.</param>
        /// <param name="mode"><see cref="YeelightMode"/>.</param>
        /// <param name="duration">Duration of the effect.</param>
        /// <returns>true if successful, false otherwise.</returns>
        public async Task<bool> SetBrightnessAsync(int brightness, YeelightMode mode = YeelightMode.Smooth, int duration = 500)
        {
            if (brightness < 1 || brightness > 100)
                throw new ArgumentOutOfRangeException("Brightness must be within the range of 1 and 100.");

            if (duration < 30)
                throw new ArgumentOutOfRangeException("Minimal supported duration is 30 ms");

            return (await SendMessageAsync("set_bright", brightness, GetMode(mode), duration))[0] == "ok";
        }

        /// <summary>
        /// Adjust brightness of the light.
        /// </summary>
        /// <param name="action"><see cref="YeelightAdjust"/>.</param>
        /// <returns>true if successful, false otherwise.</returns>
        public async Task<bool> AdjustBrightnessAsync(YeelightAdjust action = YeelightAdjust.Circle)
        {
            return (await SendMessageAsync("set_adjust", GetAction(action), "bright"))[0] == "ok";
        }

        /// <summary>
        /// Adjust brightness of the light.
        /// </summary>
        /// <param name="percentage">The percentage to be adjusted. The range is -100 to 100.</param>
        /// <param name="duration">Duration of the effect.</param>
        /// <returns>true if successful, false otherwise.</returns>
        public async Task<bool> AdjustBrightnessAsync(int percentage, int duration = 500)
        {
            if (percentage < -100 || percentage > 100)
                throw new ArgumentOutOfRangeException("Percentage must be in between -100 and 100.");

            if (duration < 30)
                throw new ArgumentOutOfRangeException("Minimal supported duration is 30 ms");

            return (await SendMessageAsync("adjust_bright", percentage, duration))[0] == "ok";
        }

        /// <summary>
        /// Get the current brightness.
        /// </summary>
        /// <returns>Brightness in percents. Supported range is 1 to 100.</returns>
        public async Task<int> GetBrightnessAsync()
        {
            string result = (await SendMessageAsync("get_prop", "bright"))[0];

            if (string.IsNullOrEmpty(result))
                throw new NotSupportedException();

            return int.Parse(result);
        }

        /// <summary>
        /// Set color temperature of the light.
        /// </summary>
        /// <param name="colorTemperature">Color temperature in Kelvins. Supported range is 1700 - 6500 (k).</param>
        /// <param name="mode"><see cref="YeelightMode"/>.</param>
        /// <param name="duration">Duration of the effect.</param>
        /// <returns>true if successful, false otherwise.</returns>
        public async Task<bool> SetColorTemperatureAsync(int colorTemperature, YeelightMode mode = YeelightMode.Smooth, int duration = 500)
        {
            if (colorTemperature < 1700 || colorTemperature > 6500)
                throw new ArgumentOutOfRangeException("Color temperature must be within the range of 1700 and 6500.");

            if (duration < 30)
                throw new ArgumentOutOfRangeException("Minimal supported duration is 30 ms");

            return (await SendMessageAsync("set_ct_abx", colorTemperature, GetMode(mode), duration))[0] == "ok";
        }

        /// <summary>
        /// Adjust color temperature of the light.
        /// </summary>
        /// <param name="action"><see cref="YeelightAdjust"/>.</param>
        /// <returns>true if successful, false otherwise.</returns>
        public async Task<bool> AdjustColorTemperatureAsync(YeelightAdjust action = YeelightAdjust.Circle)
        {
            return (await SendMessageAsync("set_adjust", GetAction(action), "ct"))[0] == "ok";
        }

        /// <summary>
        /// Adjust color temperature of the light.
        /// </summary>
        /// <param name="percentage">The percentage to be adjusted. The range is -100 to 100.</param>
        /// <param name="duration">Duration of the effect.</param>
        /// <returns>true if successful, false otherwise.</returns>
        public async Task<bool> AdjustColorTemperatureAsync(int percentage, int duration = 500)
        {
            if (percentage < -100 || percentage > 100)
                throw new ArgumentOutOfRangeException("Percentage must be in between -100 and 100.");

            if (duration < 30)
                throw new ArgumentOutOfRangeException("Minimal supported duration is 30 ms");

            return (await SendMessageAsync("adjust_ct", percentage, duration))[0] == "ok";
        }

        /// <summary>
        /// Get the color temperature.
        /// </summary>
        /// <returns>Color temperature in Kelvins. Supported range is 1700 - 6500 (k).</returns>
        public async Task<int> GetColorTemperatureAsync()
        {
            string result = (await SendMessageAsync("get_prop", "ct"))[0];

            if (string.IsNullOrEmpty(result))
                throw new NotSupportedException();

            return int.Parse(result);
        }

        /// <summary>
        /// Sets the current lamp setting as default and writes it into the persistent memory. 
        /// </summary>
        /// <returns>true if successful, false otherwise.</returns>
        public async Task<bool> SetDefaultAsync()
        {
            return (await SendMessageAsync("set_default"))[0] == "ok";
        }

        /// <summary>
        /// Get the current state
        /// </summary>
        /// <returns>Current state of all supported properties.</returns>
        public async Task <YeelightState> GetCurrentStateAsync()
        {
            string[] result = await SendMessageAsync("get_prop", 
                "power", 
                "bright",
                "ct", 
                "rgb",
                "hue",
                "sat",
                "color_mode",
                "flowing",
                "delayoff",
                "flow_params",
                "music_on",
                "name",
                "bg_power",
                "bg_flowing",
                "bg_flow_params",
                "bg_ct",
                "bg_lmode",
                "bg_bright",
                "bg_rgb",
                "bg_hue",
                "bg_sat",
                "nl_br",
                "active_mode");

            var state = new YeelightState();
            if (!string.IsNullOrEmpty(result[0]))
                state.Power = result[0] == "on";

            if (!string.IsNullOrEmpty(result[1]))
                state.Brightness = int.Parse(result[1]);

            if (!string.IsNullOrEmpty(result[2]))
                state.ColorTemperature = int.Parse(result[2]);

            if (!string.IsNullOrEmpty(result[3]))
                state.Color = int.Parse(result[3]);

            if (!string.IsNullOrEmpty(result[4]))
                state.Hue = int.Parse(result[4]);

            if (!string.IsNullOrEmpty(result[5]))
                state.Saturation = int.Parse(result[5]);

            if (!string.IsNullOrEmpty(result[6]))
                state.ColorMode = int.Parse(result[6]);

            if (!string.IsNullOrEmpty(result[7]))
                state.Flowing = int.Parse(result[7]) == 1;

            if (!string.IsNullOrEmpty(result[8]))
                state.DelayOff = int.Parse(result[8]);

            if (!string.IsNullOrEmpty(result[9]))
                state.FlowParameters = int.Parse(result[9]);

            if (!string.IsNullOrEmpty(result[10]))
                state.Music = int.Parse(result[10]) == 1;

            if (!string.IsNullOrEmpty(result[11]))
                state.Name = result[11];

            if (!string.IsNullOrEmpty(result[12]))
                state.BgPower = result[12] == "on";

            if (!string.IsNullOrEmpty(result[13]))
                state.BgFlowing = int.Parse(result[13]) == 1;

            if (!string.IsNullOrEmpty(result[14]))
                state.BgFlowParameters = int.Parse(result[14]);

            if (!string.IsNullOrEmpty(result[15]))
                state.BgColorTemperature = int.Parse(result[15]);

            if (!string.IsNullOrEmpty(result[16]))
                state.BgLightMode = int.Parse(result[16]);

            if (!string.IsNullOrEmpty(result[17]))
                state.BgBrightness = int.Parse(result[17]);

            if (!string.IsNullOrEmpty(result[18]))
                state.BgColor = int.Parse(result[18]);

            if (!string.IsNullOrEmpty(result[19]))
                state.BgHue = int.Parse(result[19]);

            if (!string.IsNullOrEmpty(result[20]))
                state.BgSaturation = int.Parse(result[20]);

            if (!string.IsNullOrEmpty(result[21]))
                state.BgBrightness = int.Parse(result[21]);

            if (!string.IsNullOrEmpty(result[22]))
                state.ActiveMode = int.Parse(result[22]);

            return state;
        }

        private string GetMode(YeelightMode mode)
        {
            switch (mode)
            {
                case YeelightMode.Sudden:
                    return "sudden";

                case YeelightMode.Smooth:
                    return "smooth";

                default:
                    throw new NotSupportedException(mode.ToString());
            }
        }

        private string GetAction(YeelightAdjust action)
        {
            switch (action)
            {
                case YeelightAdjust.Increase:
                    return "increase";

                case YeelightAdjust.Decrease:
                    return "decrease";

                case YeelightAdjust.Circle:
                    return "circle";

                default:
                    throw new NotSupportedException(action.ToString());
            }
        }

        private async Task ConnectInternal()
        {
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, YEELIGHT_PORT));
            await _udpClient.Client.ConnectAsync(_remoteEndpoint);

            MiMessage helloMessage = CreateHelloMessage();

            byte[] helloBytes = helloMessage.ToBytes();
            await _udpClient.SendAsync(helloBytes, helloBytes.Length, _remoteEndpoint);

            UdpReceiveResult receivedResult = await _udpClient.ReceiveAsync();
            helloBytes = receivedResult.Buffer;

            var helloResponse = MiMessage.FromBytes(helloBytes);
            _deviceID = helloResponse.DeviceId;
            _stamp = helloResponse.Stamp;

            // derive AES IV and key
            GetKeyAndIV(_token, out _key, out _iv);
        }

        private void VerifyConnected()
        {
            if (_udpClient == null)
                throw new Exception("Not connected");
        }

        private Task<string[]> SendMessageAsync(string method, params object[] parameters)
        {
            VerifyConnected();

            if(parameters == null)
                parameters = new object[0];

            YeelightMessage toggleMessage = new YeelightMessage();
            toggleMessage.Id = _messageId++; // id should be used to corellate message requests and responses
            toggleMessage.Method = method;
            toggleMessage.Params = parameters;

            return SendMessageAsync(toggleMessage);
        }

        private async Task<string[]> SendMessageAsync(YeelightMessage message)
        {
            VerifyConnected();

            byte[] messageInBytes = CreateMessage(_token, _key, _iv, _deviceID, _stamp, message);
            int sentBytes = await _udpClient.SendAsync(messageInBytes, messageInBytes.Length, _remoteEndpoint);

            if (sentBytes > 0)
                _stamp++; // increment the current stamp only if something has been sent

            UdpReceiveResult receivedResult = await _udpClient.ReceiveAsync();
            messageInBytes = receivedResult.Buffer;

            var recv = MiMessage.FromBytes(messageInBytes);
            if (recv.Data.All(x => x == 0x00))
            {
                // this happens when the token is not valid
                throw new Exception("Token is invalid.");
            }

            byte[] response = AesDecrypt(_key, _iv, recv.Data);
            string jsonResponse = Encoding.UTF8.GetString(response).TrimEnd('\0');
            
            string[] result = ResponseFromJson(jsonResponse);
            if(result == null)
            {
                string[] error = ErrorFromJson(jsonResponse);
                throw new Exception(error[0]);
            }

            return result;
        }

        private static MiMessage CreateHelloMessage()
        {
            // get token - TODO: convert to message
            MiMessage hello = new MiMessage();
            hello.MagicNumber = MiMessage.MAGIC;
            hello.Unknown1 = 0xffffffff;
            hello.DeviceId = 0xffffffff;
            hello.Stamp = 0xffffffff;
            hello.PacketLength = 32;

            // when in pairing mode (wifi hotspot) the checksum in the response will contain the Token to authenticate
            hello.Checksum = new byte[16] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
            hello.Data = new byte[] { };
            return hello;
        }

        private static byte[] CreateMessage(byte[] token, byte[] key, byte[] iv, uint deviceId, uint stamp, YeelightMessage message)
        {
            var miMessage = new MiMessage();
            miMessage.MagicNumber = 0x2131;
            miMessage.Unknown1 = 0;
            miMessage.DeviceId = deviceId;
            miMessage.Stamp = stamp + 1;
            miMessage.Checksum = token; // must be initialized to Token, not zeros

            string serializedMessage = RequestToJson(message);
            miMessage.Data = AesEncrypt(key, iv, Encoding.UTF8.GetBytes(serializedMessage));
            miMessage.PacketLength = (ushort)(32 + miMessage.Data.Length);

            var messageInBytes = miMessage.ToBytes();
            using (MD5 md5 = MD5.Create())
            {
                miMessage.Checksum = md5.ComputeHash(messageInBytes);
            }

            messageInBytes = miMessage.ToBytes();
            return messageInBytes;
        }

        private static string RequestToJson(YeelightMessage msg)
        {
            string json = JsonSerializer.Serialize(
                msg, 
                typeof(YeelightMessage),
                new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            return json;
        }

        private static string[] ResponseFromJson(string response)
        {
            return JsonNode.Parse(response)?["result"]?.AsArray().Select(x => x.AsValue().ToString()).ToArray();
        }

        private static string[] ErrorFromJson(string response)
        {
            return JsonNode.Parse(response)?["error"]?.AsArray().Select(x => x.AsValue().ToString()).ToArray();
        }

        private static byte[] AesEncrypt(byte[] key, byte[] iv, byte[] inputBytes)
        {
            return ApplyAesWithPkcs7(true, key, iv, inputBytes);
        }

        private static byte[] AesDecrypt(byte[] key, byte[] iv, byte[] inputBytes)
        {
            return ApplyAesWithPkcs7(false, key, iv, inputBytes);
        }

        private static byte[] ApplyAesWithPkcs7(bool encrypt, byte[] key, byte[] iv, byte[] inputBytes)
        {
            AesEngine engine = new AesEngine();
            CbcBlockCipher blockCipher = new CbcBlockCipher(engine);
            PaddedBufferedBlockCipher cipher1 = new PaddedBufferedBlockCipher(blockCipher, new Pkcs7Padding());
            KeyParameter keyParam = new KeyParameter(key);
            ParametersWithIV keyParamWithIv = new ParametersWithIV(keyParam, iv);
            cipher1.Init(encrypt, keyParamWithIv);

            byte[] outputBytes = new byte[cipher1.GetOutputSize(inputBytes.Length)];
            int length = cipher1.ProcessBytes(inputBytes, outputBytes, 0);
            cipher1.DoFinal(outputBytes, length); 
            return outputBytes;
        }

        private static void GetKeyAndIV(byte[] token, out byte[] key, out byte[] iv)
        {
            using (MD5 md5 = MD5.Create())
            {
                key = md5.ComputeHash(token);
                iv = md5.ComputeHash(key.Concat(token).ToArray());
            }
        }

        private static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        #region IDisposable

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if(_udpClient != null)
                    {
                        _udpClient.Dispose();
                        _udpClient = null;
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
