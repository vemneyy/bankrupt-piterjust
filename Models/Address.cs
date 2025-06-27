namespace bankrupt_piterjust.Models
{
    public class Address
    {
        public int AddressId { get; set; }
        public int PersonId { get; set; }
        public AddressType AddressType { get; set; }
        public string AddressText { get; set; } = string.Empty;
    }
}
