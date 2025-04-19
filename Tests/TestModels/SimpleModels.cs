namespace SimpleAutoMapping.Tests.TestModels
{
    public class SimpleSource
    {
        public int Id { get; set; }
        public required string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class SimpleDestination
    {
        public int Id { get; set; }
        public required string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
    
    public class SimpleDestinationWithDifferentNames
    {
        public int Identifier { get; set; }
        public required string FullName { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public bool Status { get; set; }
    }
} 