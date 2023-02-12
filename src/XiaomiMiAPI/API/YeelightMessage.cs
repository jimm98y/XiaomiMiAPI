namespace XiaomiMiAPI.API
{
    /// <summary>
    /// Yeelight message - will be serialized into JSON.
    /// </summary>
    internal struct YeelightMessage
    {
        /// <summary>
        /// Message ID that is being used to pair request/response.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Method to be called.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Method parameters.
        /// </summary>
        public object Params { get; set; }
    }
}
