namespace Fb2Kindle
{
    public class DataItem
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public DataItem()
        {
        }

        public DataItem(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}