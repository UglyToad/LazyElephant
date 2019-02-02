using System;

namespace PostPig.DataAccess.Florp
{
    public class Task
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime Created { get; set; }

        public Guid UserId { get; set; }
    }
}