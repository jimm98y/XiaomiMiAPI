namespace XiaomiMiAPI.Model
{
    /// <summary>
    /// Mode of the effect transtion.
    /// </summary>
    public enum YeelightMode : int
    {
        /// <summary>
        ///  If effect is "sudden", then the color temperature will be changed directly to target value,
        ///  under this case, the third parameter "duration" is ignored.
        /// </summary>
        Sudden = 0,

        /// <summary>
        /// If effect is "smooth", then the color temperature will be changed to target value in a gradual
        /// fashion, under this case, the total time of gradual change is specified in third parameter "duration".
        /// </summary>
        Smooth = 1
    }
}
