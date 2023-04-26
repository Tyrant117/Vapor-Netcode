using System.Collections.Generic;

namespace VaporNetcode
{
    /// <summary>
    /// This class is used to cleanup unused dictionary keys while still being an O(n) removal operation.
    /// The idea is to use this for cleanup, when cleanup needs to be done conistently on an update schedule.
    /// T is the key to the dictionary.
    /// </summary>
    public class DictionaryCleanupList<T,V>
    {
        private int pointer;
        public T[] content;

        public DictionaryCleanupList(int capacity)
        {
            pointer = 0;
            content = new T[capacity];
        }

        public void Add(T key)
        {
            if (pointer == content.Length)
            {
                var buffer = new T[content.Length * 2];
                content.CopyTo(buffer, 0);
                content = buffer;
                content[pointer] = key;
            }
            else
            {
                content[pointer] = key;
            }
            pointer++;
        }

        public void RemoveAll(Dictionary<T, V> db)
        {
            while (pointer > 0)
            {
                pointer--;
                db.Remove(content[pointer]);
            }
        }
    }
}