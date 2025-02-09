using System.Collections.ObjectModel;
using WPFNode.Core.Models;

namespace WPFNode.Core.Services;

public class NodeTemplateService
{
    private readonly ObservableCollection<NodeTemplate> _templates = new();
    public IReadOnlyCollection<NodeTemplate> Templates => _templates;

    public NodeTemplateService()
    {
        RegisterDefaultTemplates();
    }

    public void RegisterTemplate(NodeTemplate template)
    {
        if (_templates.Any(t => t.Name == template.Name))
            throw new ArgumentException($"Template with name '{template.Name}' already exists");
        
        _templates.Add(template);
    }

    public NodeTemplate? GetTemplate(string name)
    {
        return _templates.FirstOrDefault(t => t.Name == name);
    }

    public IEnumerable<NodeTemplate> GetTemplatesByCategory(string category)
    {
        return _templates.Where(t => t.Category == category);
    }

    public IEnumerable<string> GetCategories()
    {
        return _templates.Select(t => t.Category).Distinct();
    }

    private void RegisterDefaultTemplates()
    {
        // 수학 연산
        var mathCategory = "수학";
        
        var addTemplate = new NodeTemplate("덧셈", mathCategory, "두 수를 더합니다.");
        addTemplate.Ports.Add(new PortTemplate("A", typeof(double), true));
        addTemplate.Ports.Add(new PortTemplate("B", typeof(double), true));
        addTemplate.Ports.Add(new PortTemplate("결과", typeof(double), false));
        RegisterTemplate(addTemplate);

        var subtractTemplate = new NodeTemplate("뺄셈", mathCategory, "A에서 B를 뺍니다.");
        subtractTemplate.Ports.Add(new PortTemplate("A", typeof(double), true));
        subtractTemplate.Ports.Add(new PortTemplate("B", typeof(double), true));
        subtractTemplate.Ports.Add(new PortTemplate("결과", typeof(double), false));
        RegisterTemplate(subtractTemplate);

        var multiplyTemplate = new NodeTemplate("곱셈", mathCategory, "두 수를 곱합니다.");
        multiplyTemplate.Ports.Add(new PortTemplate("A", typeof(double), true));
        multiplyTemplate.Ports.Add(new PortTemplate("B", typeof(double), true));
        multiplyTemplate.Ports.Add(new PortTemplate("결과", typeof(double), false));
        RegisterTemplate(multiplyTemplate);

        var divideTemplate = new NodeTemplate("나눗셈", mathCategory, "A를 B로 나눕니다.");
        divideTemplate.Ports.Add(new PortTemplate("A", typeof(double), true));
        divideTemplate.Ports.Add(new PortTemplate("B", typeof(double), true));
        divideTemplate.Ports.Add(new PortTemplate("결과", typeof(double), false));
        RegisterTemplate(divideTemplate);

        // 논리 연산
        var logicCategory = "논리";
        
        var andTemplate = new NodeTemplate("AND", logicCategory, "두 불리언 값의 AND 연산을 수행합니다.");
        andTemplate.Ports.Add(new PortTemplate("A", typeof(bool), true));
        andTemplate.Ports.Add(new PortTemplate("B", typeof(bool), true));
        andTemplate.Ports.Add(new PortTemplate("결과", typeof(bool), false));
        RegisterTemplate(andTemplate);

        var orTemplate = new NodeTemplate("OR", logicCategory, "두 불리언 값의 OR 연산을 수행합니다.");
        orTemplate.Ports.Add(new PortTemplate("A", typeof(bool), true));
        orTemplate.Ports.Add(new PortTemplate("B", typeof(bool), true));
        orTemplate.Ports.Add(new PortTemplate("결과", typeof(bool), false));
        RegisterTemplate(orTemplate);

        var notTemplate = new NodeTemplate("NOT", logicCategory, "불리언 값의 NOT 연산을 수행합니다.");
        notTemplate.Ports.Add(new PortTemplate("입력", typeof(bool), true));
        notTemplate.Ports.Add(new PortTemplate("결과", typeof(bool), false));
        RegisterTemplate(notTemplate);
    }
} 