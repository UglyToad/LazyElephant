using System;

namespace PostPig.DataAccess.Florp
{
    public class Customer
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? Age { get; set; }
    }
}