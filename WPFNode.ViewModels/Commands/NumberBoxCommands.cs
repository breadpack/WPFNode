using System.Windows.Input;

namespace WPFNode.Commands;

/// <summary>
/// 숫자 입력을 위한 일반적인 명령들을 정의합니다.
/// 실제 구현은 각 플러그인에서 담당합니다.
/// </summary>
public static class NumberBoxCommands
{
    public static readonly RoutedCommand Increment = new(
        nameof(Increment),
        typeof(NumberBoxCommands));

    public static readonly RoutedCommand Decrement = new(
        nameof(Decrement),
        typeof(NumberBoxCommands));
} 