using System;
using System.Collections.Generic;
using WPFNode.Abstractions;

namespace WPFNode.Core.Models;

public class InputPort<T>(string name, INode node) : PortBase(name, typeof(T), true, node), IInputPort {
    private readonly Dictionary<Type, Func<object, T?>> _typeConverters = new() {
        { typeof(T), o => o is T t ? t : default }
    };

    public void AddTypeConverter<TInput>(Func<TInput, T?> converter)
    {
        _typeConverters[typeof(TInput)] = value => value is TInput input ? converter(input) : default;
    }

    private bool CanImplicitlyCast(Type sourceType)
    {
        // 이미 같은 타입이거나 상속 관계인 경우
        if (typeof(T).IsAssignableFrom(sourceType))
            return true;

        // 숫자 타입 간의 암시적 변환 가능 여부 확인
        try
        {
            var method = sourceType.GetMethod("op_Implicit", new[] { sourceType });
            if (method != null && method.ReturnType == typeof(T))
                return true;

            // Convert.ChangeType를 통한 변환 가능 여부 확인
            if (typeof(IConvertible).IsAssignableFrom(sourceType) && 
                typeof(IConvertible).IsAssignableFrom(typeof(T)))
                return true;
        }
        catch
        {
            return false;
        }

        return false;
    }

    private T? ConvertValue(object? value)
    {
        if (value == null)
            return default;

        var valueType = value.GetType();
        if(typeof(T) == typeof(string)) 
            return (T?)(object?)value.ToString();

        // 등록된 타입 변환기를 찾아서 실행
        if (_typeConverters.TryGetValue(valueType, out var converter))
        {
            var result = converter(value);
            if (result != null)
                return result;
            throw new InvalidOperationException($"[{Name}] 타입 변환 실패: {valueType.Name} -> {typeof(T).Name}, 변환 결과가 null");
        }

        // 암시적 캐스팅 시도
        try
        {
            if (CanImplicitlyCast(valueType))
            {
                if (typeof(T).IsAssignableFrom(valueType))
                    return (T?)value;

                var convertedValue = Convert.ChangeType(value, typeof(T));
                return (T?)convertedValue;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"[{Name}] 암시적 변환 실패: {valueType.Name} -> {typeof(T).Name}", ex);
        }

        throw new InvalidOperationException($"[{Name}] 타입 변환기 없음: {valueType.Name} -> {typeof(T).Name}");
    }

    public bool CanAcceptType(Type type)
    {
        if (typeof(T) == typeof(string)) {
            return true;
        }
        
        return _typeConverters.ContainsKey(type) || CanImplicitlyCast(type);
    }

    public bool CanConnectTo(IOutputPort targetPort)
    {
        // 같은 노드의 포트와는 연결 불가
        if (targetPort.Node == Node) return false;

        if (IsInput == targetPort.IsInput) return false;

        return CanAcceptType(targetPort.DataType);
    }

    public new T? Value
    {
        get
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException($"[{Name}] 연결되지 않은 입력 포트의 값을 읽을 수 없습니다.");
            }

            var outputPort = Connections.FirstOrDefault()?.Source as IOutputPort;
            if (outputPort == null)
            {
                throw new InvalidOperationException($"[{Name}] 연결된 출력 포트를 찾을 수 없습니다.");
            }

            var value = outputPort.GetValue();
            return ConvertValue(value);
        }
    }

    public T GetValueOrDefault(T defaultValue)
    {
        if (!IsConnected)
            return defaultValue;

        return Value ?? defaultValue;
    }

    public T GetValueOrDefault()
    {
        if (!IsConnected)
            return default!;

        return Value ?? default!;
    }

    public bool TryGetValue(out T? value)
    {
        if (!IsConnected)
        {
            value = default;
            return false;
        }

        try
        {
            value = Value;
            return value != null;
        }
        catch
        {
            value = default;
            return false;
        }
    }
} 