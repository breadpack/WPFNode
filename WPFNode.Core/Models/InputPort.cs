using System;
using System.Collections.Generic;
using System.ComponentModel;
using WPFNode.Abstractions;
using System.Reflection;

namespace WPFNode.Core.Models;

public class InputPort<T> : IInputPort, INotifyPropertyChanged
{
    private readonly List<IConnection> _connections = new();
    private readonly Dictionary<Type, Func<object, T>> _converters = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public InputPort(string name, INode node)
    {
        Id = Guid.NewGuid();
        Name = name;
        Node = node;
    }

    public Guid Id { get; }
    public string Name { get; set; }
    public Type DataType => typeof(T);
    public bool IsInput => true;
    public bool IsConnected => _connections.Count > 0;
    public IReadOnlyList<IConnection> Connections => _connections;
    public INode? Node { get; private set; }

    public void RegisterConverter<TSource>(Func<TSource, T> converter)
    {
        if (converter == null)
            throw new ArgumentNullException(nameof(converter));
            
        _converters[typeof(TSource)] = obj => converter((TSource)obj);
    }

    public bool CanAcceptType(Type sourceType)
    {
        // 1. 컨버터가 등록되어 있으면 변환 가능
        if (_converters.ContainsKey(sourceType))
            return true;

        // 2. 동일한 타입이면 호환됨
        if (sourceType == typeof(T) || typeof(T).IsAssignableFrom(sourceType))
            return true;

        // 3. 암시적 변환 연산자 확인
        var implicitOperator = sourceType.GetMethod("op_Implicit", 
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { sourceType },
            null);

        if (implicitOperator != null && implicitOperator.ReturnType == typeof(T))
            return true;

        // 4. 숫자 타입 간의 안전한 변환이 가능한 경우
        if (IsNumericType(sourceType) && IsNumericType(typeof(T)))
        {
            return IsImplicitNumericConversion(sourceType, typeof(T));
        }

        // 5. string으로의 변환 지원
        if (typeof(T) == typeof(string))
            return true;

        return false;
    }

    private bool IsNumericType(Type type)
    {
        if (type == null) return false;

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }

    private bool IsImplicitNumericConversion(Type source, Type target)
    {
        var sourceCode = Type.GetTypeCode(source);
        var targetCode = Type.GetTypeCode(target);

        switch (sourceCode)
        {
            case TypeCode.SByte:
                return targetCode == TypeCode.Int16 || targetCode == TypeCode.Int32 || 
                       targetCode == TypeCode.Int64 || targetCode == TypeCode.Single || 
                       targetCode == TypeCode.Double || targetCode == TypeCode.Decimal;
            case TypeCode.Byte:
                return targetCode == TypeCode.Int16 || targetCode == TypeCode.UInt16 || 
                       targetCode == TypeCode.Int32 || targetCode == TypeCode.UInt32 ||
                       targetCode == TypeCode.Int64 || targetCode == TypeCode.UInt64 || 
                       targetCode == TypeCode.Single || targetCode == TypeCode.Double || 
                       targetCode == TypeCode.Decimal;
            case TypeCode.Int16:
                return targetCode == TypeCode.Int32 || targetCode == TypeCode.Int64 || 
                       targetCode == TypeCode.Single || targetCode == TypeCode.Double || 
                       targetCode == TypeCode.Decimal;
            case TypeCode.UInt16:
                return targetCode == TypeCode.Int32 || targetCode == TypeCode.UInt32 ||
                       targetCode == TypeCode.Int64 || targetCode == TypeCode.UInt64 ||
                       targetCode == TypeCode.Single || targetCode == TypeCode.Double ||
                       targetCode == TypeCode.Decimal;
            case TypeCode.Int32:
                return targetCode == TypeCode.Int64 || targetCode == TypeCode.Single ||
                       targetCode == TypeCode.Double || targetCode == TypeCode.Decimal;
            case TypeCode.UInt32:
                return targetCode == TypeCode.Int64 || targetCode == TypeCode.UInt64 ||
                       targetCode == TypeCode.Single || targetCode == TypeCode.Double ||
                       targetCode == TypeCode.Decimal;
            case TypeCode.Int64:
                return targetCode == TypeCode.Single || targetCode == TypeCode.Double ||
                       targetCode == TypeCode.Decimal;
            case TypeCode.UInt64:
                return targetCode == TypeCode.Single || targetCode == TypeCode.Double ||
                       targetCode == TypeCode.Decimal;
            case TypeCode.Single:
                return targetCode == TypeCode.Double;
            default:
                return false;
        }
    }

    public object? Value
    {
        get
        {
            // 연결된 OutputPort로부터 값을 가져옴
            if (_connections.Count > 0 && _connections[0].Source is IOutputPort outputPort)
            {
                var sourceValue = outputPort.Value;
                if (sourceValue == null) return default(T);

                var sourceType = sourceValue.GetType();
                
                // 1. 컨버터를 통한 변환 시도
                if (_converters.TryGetValue(sourceType, out var converter))
                {
                    return converter(sourceValue);
                }

                // 2. 직접적인 타입 체크
                if (sourceValue is T typedValue)
                {
                    return typedValue;
                }

                // 3. 암시적 변환 연산자 확인
                var implicitOperator = sourceType.GetMethod("op_Implicit", 
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { sourceType },
                    null);

                if (implicitOperator != null && implicitOperator.ReturnType == typeof(T))
                {
                    try
                    {
                        return implicitOperator.Invoke(null, new[] { sourceValue });
                    }
                    catch
                    {
                        return default(T);
                    }
                }

                // 4. 숫자 타입 간의 안전한 변환 시도
                if (IsNumericType(sourceType) && IsNumericType(typeof(T)))
                {
                    try
                    {
                        if (IsImplicitNumericConversion(sourceType, typeof(T)))
                        {
                            return Convert.ChangeType(sourceValue, typeof(T));
                        }
                    }
                    catch
                    {
                        return default(T);
                    }
                }

                // 5. string 타입으로의 변환
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)(sourceValue.ToString() ?? string.Empty);
                }
            }
            return default(T);
        }
    }

    public T? GetValueOrDefault(T? defaultValue = default)
    {
        var value = Value;
        return value is T typedValue ? typedValue : defaultValue;
    }

    public void AddConnection(IConnection connection)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));
        _connections.Add(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
    }

    public void RemoveConnection(IConnection connection)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));
        _connections.Remove(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 