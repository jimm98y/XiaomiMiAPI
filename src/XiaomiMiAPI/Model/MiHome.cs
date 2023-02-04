using System.Collections.Generic;

namespace XiaomiMiAPI.Model
{
    public class MiHome
    {
        public string HomeId { get; internal set; }

        public string HomeOwner { get; internal set; }

        public string Name { get; internal set; }

        public string ServerLocation { get; internal set; }

        public List<MiDevice> Devices { get; set; } = new List<MiDevice>();
    }
}
