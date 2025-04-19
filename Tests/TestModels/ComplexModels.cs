using System.Collections.Generic;

namespace SimpleAutoMapping.Tests.TestModels
{
    public class Address
    {
        public int Id { get; set; }
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
    }

    public class AddressDto
    {
        public int Id { get; set; }
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
    }

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Address Address { get; set; } = new Address();
        public List<string> PhoneNumbers { get; set; } = new List<string>();
    }

    public class PersonDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AddressDto Address { get; set; } = new AddressDto();
        public List<string> PhoneNumbers { get; set; } = new List<string>();
    }
} 