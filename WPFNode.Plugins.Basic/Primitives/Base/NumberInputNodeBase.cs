using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WPFNode.Core.Models;

namespace WPFNode.Plugins.Basic.Primitives.Base;

public abstract class NumberInputNodeBase<T> : InputNodeBase<T> where T : struct
{
    public ICommand IncrementCommand { get; }
    public ICommand DecrementCommand { get; }

    protected NumberInputNodeBase()
    {
        IncrementCommand = new RelayCommand(OnIncrement);
        DecrementCommand = new RelayCommand(OnDecrement);
    }

    protected abstract void OnIncrement();
    protected abstract void OnDecrement();
} 