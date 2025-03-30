using System;

namespace WPFNode.Models;

public record struct NodeMetadata(Type NodeType, string Name, string? Category, string? Description); 