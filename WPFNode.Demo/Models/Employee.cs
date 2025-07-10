using System;
using Newtonsoft.Json;

namespace WPFNode.Demo.Models
{
    public interface IEmployee {
        int Id { get; set; }
        string Name { get; set; }
        string Department { get; set; }
        decimal Salary { get; set; }
    }
    
    public class Employee : IEmployee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
        public decimal Salary { get; set; }
        public string[] Addresses { get; set; }

        // 기본 생성자
        public Employee()
        {
            Id = 0;
            Name = "Unknown";
            Department = "None";
            Salary = 0;
        }

        // ID로 초기화하는 생성자 (생성자를 통한 변환 테스트용)
        public Employee(int id)
        {
            Id = id;
            Name = $"Employee-{id}";
            Department = "Default";
            Salary = 3000;
        }

        // JSON 문자열로 초기화하는 생성자 (생성자를 통한 변환 테스트용)
        public Employee(string json)
        {
            try
            {
                var emp = JsonConvert.DeserializeObject<Employee>(json);
                if (emp != null)
                {
                    Id = emp.Id;
                    Name = emp.Name;
                    Department = emp.Department;
                    Salary = emp.Salary;
                }
            }
            catch
            {
                Id = 0;
                Name = $"Error-Parsing: {json}";
                Department = "None";
                Salary = 0;
            }
        }

        // 암시적 변환 연산자: int -> Employee
        public static implicit operator Employee(int id)
        {
            return new Employee(id);
        }

        // 암시적 변환 연산자: string -> Employee
        public static implicit operator Employee(string json)
        {
            return new Employee(json);
        }

        // 명시적 변환 연산자: Employee -> string
        public static explicit operator string(Employee employee)
        {
            return JsonConvert.SerializeObject(employee);
        }

        public override string ToString()
        {
            return $"{Id}: {Name} ({Department}) - ${Salary}";
        }
    }
}
