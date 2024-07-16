namespace AdvisoryMap
{
    internal class IsoMapper
    {
        int NameIndex = 0;
        int CodeIndex = 1;

        Dictionary<string, string> map = new Dictionary<string, string>();

        internal IsoMapper()
        {
            foreach (string line in File.ReadAllLines("iso-codes.csv"))
            {
                string[] entry = line.Split(',', StringSplitOptions.TrimEntries);

                map[entry[NameIndex]] = entry[CodeIndex];
            }
        }

        internal string NameToCode(string name)
        {
            return map.TryGetValue(name, out string? code) ? code : string.Empty;
        }
    }
}
