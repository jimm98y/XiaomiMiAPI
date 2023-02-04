namespace XiaomiMiAPI.Model
{
    /// <summary>
    /// Adjustment action.
    /// </summary>
    public enum MiAdjust : int
    {
        /// <summary>
        /// Increase the specified property.
        /// </summary>
        Increase,

        /// <summary>
        /// Decrease the specified property.
        /// </summary>
        Decrease,

        /// <summary>
        /// Increase the specified property, after it reaches the max value, go back to minimum value.
        /// </summary>
        Circle
    }
}
