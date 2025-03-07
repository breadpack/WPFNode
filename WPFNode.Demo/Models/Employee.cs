using System;

namespace WPFNode.Demo.Models
{
    public class Employee
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        
        public override string ToString()
        {
            return $"{Name} (나이: {Age})";
        }
    }
}
