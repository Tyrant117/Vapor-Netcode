namespace VaporMMO
{
    public interface ITickable
    {
        public bool IsRegistered { get; set; }
        public uint NetID { get; set; }
        public void Register(uint netID)
        {
            NetID = netID;
            IsRegistered = true;
        }
        public void Tick();
    }
}
