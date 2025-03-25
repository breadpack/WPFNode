using System;
using System.Collections.Generic;

namespace WPFNode.Tests.Models
{
    // 객체 생성 테스트를 위한 기본 모델 클래스
    public class TestPerson
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive;  // 필드
    }

    // 복잡한 객체 테스트를 위한 모델 클래스
    public class TestAddress
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
    }

    // 중첩 객체를 가진 모델 클래스
    public class TestEmployee
    {
        public string Name { get; set; } = string.Empty;
        public int Id { get; set; }
        public TestAddress? Address { get; set; }
    }

    // 컬렉션을 포함한 모델 클래스
    public class TestTeam
    {
        public string TeamName { get; set; } = string.Empty;
        public List<TestPerson> Members { get; set; } = new List<TestPerson>();
    }
}
