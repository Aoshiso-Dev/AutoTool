using System;
using System.Linq.Expressions;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Desktop.Panels.ViewModel.Helpers;

/// <summary>
/// 式ツリーから getter/setter を生成し、型安全にプロパティへアクセスできるようにします。
/// </summary>
/// <typeparam name="TInterface">アクセス対象のインターフェース型</typeparam>
/// <typeparam name="TProperty">プロパティの型</typeparam>
public class PropertyAccessor<TInterface, TProperty> where TInterface : class
{
    private readonly Func<TInterface, TProperty> _getter;
    private readonly Action<TInterface, TProperty> _setter;
    private readonly TProperty _defaultValue;

    public PropertyAccessor(
        Expression<Func<TInterface, TProperty>> propertyExpression, 
        TProperty defaultValue = default!)
    {
        _getter = propertyExpression.Compile();
        _setter = CreateSetter(propertyExpression);
        _defaultValue = defaultValue;
    }

    public TProperty GetValue(ICommandListItem? item)
    {
        return item is TInterface target ? _getter(target) : _defaultValue;
    }

    public void SetValue(ICommandListItem? item, TProperty value)
    {
        if (item is TInterface target)
        {
            _setter(target, value);
        }
    }

    private static Action<TInterface, TProperty> CreateSetter(Expression<Func<TInterface, TProperty>> propertyExpression)
    {
        if (propertyExpression.Body is not MemberExpression memberExpr)
        throw new ArgumentException("式はプロパティアクセスである必要があります。");

        var parameter = propertyExpression.Parameters[0];
        var valueParameter = Expression.Parameter(typeof(TProperty), "value");
        var assignExpression = Expression.Assign(memberExpr, valueParameter);
        
        return Expression.Lambda<Action<TInterface, TProperty>>(assignExpression, parameter, valueParameter).Compile();
    }
}

/// <summary>
/// 複数インターフェースに対するプロパティアクセス定義を保持し、対象型に応じて getter/setter を切り替えます。
/// </summary>
/// <typeparam name="TProperty">プロパティの型</typeparam>
public class MultiInterfacePropertyAccessor<TProperty>
{
    private readonly Dictionary<Type, (Func<object, TProperty> getter, Action<object, TProperty> setter)> _accessors = [];
    private readonly TProperty _defaultValue;

    public MultiInterfacePropertyAccessor(TProperty defaultValue = default!)
    {
        _defaultValue = defaultValue;
    }

    public MultiInterfacePropertyAccessor<TProperty> AddInterface<TInterface>(Expression<Func<TInterface, TProperty>> propertyExpression)
        where TInterface : class
    {
        var getter = propertyExpression.Compile();
        var setter = CreateSetter(propertyExpression);
        
        _accessors[typeof(TInterface)] = (
            obj => getter((TInterface)obj),
            (obj, val) => setter((TInterface)obj, val)
        );
        
        return this;
    }

    public TProperty GetValue(ICommandListItem? item)
    {
        if (item is null) return _defaultValue;

        foreach (var kvp in _accessors)
        {
            if (kvp.Key.IsInstanceOfType(item))
            {
                return kvp.Value.getter(item);
            }
        }
        
        return _defaultValue;
    }

    public void SetValue(ICommandListItem? item, TProperty value)
    {
        if (item is null) return;

        foreach (var kvp in _accessors)
        {
            if (kvp.Key.IsInstanceOfType(item))
            {
                kvp.Value.setter(item, value);
                return;
            }
        }
    }

    private static Action<TInterface, TProperty> CreateSetter<TInterface>(Expression<Func<TInterface, TProperty>> propertyExpression)
    {
        if (propertyExpression.Body is not MemberExpression memberExpr)
        throw new ArgumentException("式はプロパティアクセスである必要があります。");

        var parameter = propertyExpression.Parameters[0];
        var valueParameter = Expression.Parameter(typeof(TProperty), "value");
        var assignExpression = Expression.Assign(memberExpr, valueParameter);
        
        return Expression.Lambda<Action<TInterface, TProperty>>(assignExpression, parameter, valueParameter).Compile();
    }
}
