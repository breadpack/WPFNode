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
        get => base.Value == null ? default : (T)base.Value;
        set => base.Value = value;
    }

    protected override bool TrySetValue(object? value)
    {
        if (value == null)
        {
            base.Value = default(T);
            return true;
        }

        var valueType = value.GetType();
        if(typeof(T) == typeof(string)) 
        {
            base.Value = value.ToString();
            return true;
        }

        // 등록된 타입 변환기를 찾아서 실행
        if (_typeConverters.TryGetValue(valueType, out var converter))
        {
            var result = converter(value);
            if (result != null)
            {
                base.Value = result;
                return true;
            }
            throw new InvalidOperationException($"[{Name}] 타입 변환 실패: {valueType.Name} -> {typeof(T).Name}, 변환 결과가 null");
        }

        // 암시적 캐스팅 시도
        try
        {
            if (CanImplicitlyCast(valueType))
            {
                if (typeof(T).IsAssignableFrom(valueType))
                {
                    base.Value = value;
                    return true;
                }

                var convertedValue = Convert.ChangeType(value, typeof(T));
                base.Value = convertedValue;
                return true;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"[{Name}] 암시적 변환 실패: {valueType.Name} -> {typeof(T).Name}", ex);
        }

        throw new InvalidOperationException($"[{Name}] 타입 변환기 없음: {valueType.Name} -> {typeof(T).Name}");
    }

    public T GetValueOrDefault(T defaultValue)
    {
        return Value ?? defaultValue;
    }

    public T GetValueOrDefault()
    {
        return Value ?? default(T)!;
    }

    public bool TryGetValue(out T? value)
    {
        value = default;
        if (!IsConnected || base.Value == null)
            return false;

        value = (T)base.Value;
        return true;
    }
} 