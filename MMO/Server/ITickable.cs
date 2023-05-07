namespace VaporMMO
{
    public interface ITickable
    {
        public bool IsRegistered { get; }
        public uint NetID { get; }
        public void Register(uint netID);
        public void Tick();
    }
}
