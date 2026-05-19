namespace KspWebMap
{
    public interface IKspWebMapService
    {
        string Name { get; }

        void Start();

        void Stop();
    }
}
