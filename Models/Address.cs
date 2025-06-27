using System.ComponentModel;

namespace bankrupt_piterjust.Models
{
    public class Address : INotifyPropertyChanged
    {
        public int AddressId { get; set; }
        public int PersonId { get; set; }

        private string? _postalCode;
        public string? PostalCode
        {
            get => _postalCode;
            set { _postalCode = value; OnPropertyChanged(nameof(PostalCode)); }
        }

        private string _country = "Россия";
        public string Country
        {
            get => _country;
            set { _country = value; OnPropertyChanged(nameof(Country)); }
        }

        private string? _region;
        public string? Region
        {
            get => _region;
            set { _region = value; OnPropertyChanged(nameof(Region)); }
        }

        private string? _district;
        public string? District
        {
            get => _district;
            set { _district = value; OnPropertyChanged(nameof(District)); }
        }

        private string? _city;
        public string? City
        {
            get => _city;
            set { _city = value; OnPropertyChanged(nameof(City)); }
        }

        private string? _locality;
        public string? Locality
        {
            get => _locality;
            set { _locality = value; OnPropertyChanged(nameof(Locality)); }
        }

        private string? _street;
        public string? Street
        {
            get => _street;
            set { _street = value; OnPropertyChanged(nameof(Street)); }
        }

        private string? _houseNumber;
        public string? HouseNumber
        {
            get => _houseNumber;
            set { _houseNumber = value; OnPropertyChanged(nameof(HouseNumber)); }
        }

        private string? _building;
        public string? Building
        {
            get => _building;
            set { _building = value; OnPropertyChanged(nameof(Building)); }
        }

        private string? _apartment;
        public string? Apartment
        {
            get => _apartment;
            set { _apartment = value; OnPropertyChanged(nameof(Apartment)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public bool IsEmpty()
        {
            return string.IsNullOrWhiteSpace(PostalCode) &&
                   string.IsNullOrWhiteSpace(Country) &&
                   string.IsNullOrWhiteSpace(Region) &&
                   string.IsNullOrWhiteSpace(District) &&
                   string.IsNullOrWhiteSpace(City) &&
                   string.IsNullOrWhiteSpace(Locality) &&
                   string.IsNullOrWhiteSpace(Street) &&
                   string.IsNullOrWhiteSpace(HouseNumber) &&
                   string.IsNullOrWhiteSpace(Building) &&
                   string.IsNullOrWhiteSpace(Apartment);
        }
    }
}
