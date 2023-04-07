# XiaomiMiAPI
A client to control the Xiaomi Mi Smart LED Lamp Pro. 

## Disclaimer
It might work with other Xiaomi Mi devices, but I have no way to test them so they are not officially supported.

## Yeelight Client
To control the smart lamp use the YeelightClient. The client was implemented using the official documentation here: https://www.yeelight.com/download/Yeelight_Inter-Operation_Spec.pdf. 

Create the Yeelight client:
```cs
var yeelightClient = new YeelightClient();
```

Connect to the lamp using a device token:
```cs
await yeelightClient.ConnectAsync("192.168.1.12", "abcd123456789");
```
In the hotspot mode when the lamp creates a Wi-Fi hotspot used for configuration, you can pass `null` instead of the token. The code can get the token from the lamp in this mode and you can also control the lamp. 
```cs
await yeelightClient.ConnectAsync("192.168.1.12", null);
```
However, after you configure the lamp to connect to the Wi-Fi network, the lamp re-generates the initial token and it is no longer possible to retrieve it from the lamp. The new token can be only retrieved from the Mi cloud, which means you have to use the official app to link the lamp with your Mi account. To retrieve the token from the cloud, use `MiCloudClient`.

Control the lamp:
```cs
await yeelightClient.ToggleAsync();
```
## Mi Cloud Client
To get the token to pass into the `YeelightClient` you can use the extractor as follows:
```cs
var miCloudClient = new MiCloudClient();
var homes = await miCloudClient.DiscoverDevicesAsync("+444123456789", "mySecretPassword");
string token = homes.First().Devices.First().Token;
```
## Token
Once you retrieve the token, you can store it somewhere safe and block the lamp from accessing the Internet. The `YeelightClient` communicates with the lamp locally and the token remains valid until you reset/disconnect/reconnect the lamp to the Wi-Fi.

## Credits
The `MiCloudClient` is just a C# port of the great work from Piotr Machowski: https://github.com/PiotrMachowski/Xiaomi-cloud-tokens-extractor