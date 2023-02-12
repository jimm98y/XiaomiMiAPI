namespace XiaomiMiAPI.Model
{
    /// <summary>
    /// Yeelight device state.
    /// </summary>
    public struct YeelightState
    {
        /// <summary>
        /// on: smart LED is turned on / off: smart LED is turned off.
        /// </summary>
        public bool? Power { get; set; }

        /// <summary>
        /// Brightness percentage. Range 1 ~ 100.
        /// </summary>
        public int? Brightness { get; set; }

        /// <summary>
        /// Color temperature. Range 1700 ~ 6500(k).
        /// </summary>
        public int? ColorTemperature { get; set; }

        /// <summary>
        /// Color. Range 1 ~ 16777215.
        /// </summary>
        public int? Color { get; set; }

        /// <summary>
        /// Hue. Range 0 ~ 359.
        /// </summary>
        public int? Hue { get; set; }

        /// <summary>
        /// Saturation. Range 0 ~ 100.
        /// </summary>
        public int? Saturation { get; set; }

        /// <summary>
        /// 1: rgb mode / 2: color temperature mode / 3: hsv mode.
        /// </summary>
        public int? ColorMode { get; set; }

        /// <summary>
        /// 0: no flow is running / 1:color flow is running.
        /// </summary>
        public bool? Flowing { get; set; }

        /// <summary>
        /// The remaining time of a sleep timer. Range 1 ~ 60 (minutes).
        /// </summary>
        public int? DelayOff { get; set; }

        /// <summary>
        /// Current flow parameters (only meaningful when 'flowing' is 1).
        /// </summary>
        public int? FlowParameters { get; set; }

        /// <summary>
        /// 1: Music mode is on / 0: Music mode is off.
        /// </summary>
        public bool? Music { get; set; }

        /// <summary>
        /// The name of the device set by "set_name" command.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Background light power status.
        /// </summary>
        public bool? BgPower { get; set; }

        /// <summary>
        /// Background light is flowing.
        /// </summary>
        public bool? BgFlowing { get; set; }

        /// <summary>
        /// Current flow parameters of background light.
        /// </summary>
        public int? BgFlowParameters { get; set; }

        /// <summary>
        /// Color temperature of background light.
        /// </summary>
        public int? BgColorTemperature { get; set; }

        /// <summary>
        /// 1: rgb mode / 2: color temperature mode / 3: hsv mode.
        /// </summary>
        public int? BgLightMode { get; set; }

        /// <summary>
        /// Brightness percentage of background light.
        /// </summary>
        public int? BgBrightness { get; set; }

        /// <summary>
        /// Color of background light.
        /// </summary>
        public int? BgColor { get; set; }

        /// <summary>
        /// Hue of background light.
        /// </summary>
        public int? BgHue { get; set; }

        /// <summary>
        /// Saturation of background light.
        /// </summary>
        public int? BgSaturation { get; set; }

        /// <summary>
        /// Brightness of night mode light.
        /// </summary>
        public int? NightLightBrightness { get; set; }

        /// <summary>
        /// 0: daylight mode / 1: moonlight mode (ceiling light only).
        /// </summary>
        public int? ActiveMode { get; set; }
    }
}
