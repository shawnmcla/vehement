namespace Vehement.Assembler
{
    public partial class Assembler
    {
        private class StaticData
        {
            private class StaticDataItem
            {
                public string Identifier;
                public List<byte> Bytes;
                public int Offset;
                public StaticDataItem(string identifier, List<byte> bytes, int offset)
                {
                    Identifier = identifier;
                    Bytes = bytes;
                    Offset = offset;
                }
            }

            private int size = 0;
            private SortedDictionary<string, StaticDataItem> data = new();
            public List<byte> AllBytes => data.SelectMany(d => d.Value.Bytes).ToList();

            public int GetOffset(string identifier)
            {
                return data[identifier].Offset;
            }

            public void Add(string identifier, List<byte> bytes)
            {
                data.Add(identifier, new StaticDataItem(identifier, bytes, size));
                size += bytes.Count;
            }
        }
    }
}
