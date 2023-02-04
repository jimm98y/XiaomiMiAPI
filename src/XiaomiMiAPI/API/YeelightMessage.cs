namespace XiaomiMiAPI.API
{
    internal struct YeelightMessage
    {
        public int Id { get; set; }
        public string Method { get; set; }
        public object Params { get; set; }
    }
}
