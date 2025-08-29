using System;
using System.Linq.Expressions;
using MacroPanels.Model.List.Interface;

namespace MacroPanels.ViewModel.Helpers
{
    /// <summary>
    /// 型安全なプロパティアクセサ
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
                throw new ArgumentException("Expression must be a property access");

            var parameter = propertyExpression.Parameters[0];
            var valueParameter = Expression.Parameter(typeof(TProperty), "value");
            var assignExpression = Expression.Assign(memberExpr, valueParameter);
            
            return Expression.Lambda<Action<TInterface, TProperty>>(assignExpression, parameter, valueParameter).Compile();
        }
    }

    /// <summary>
    /// 複数のインターフェースに対応するプロパティアクセサ
    /// </summary>
    /// <typeparam name="TProperty">プロパティの型</typeparam>
    public class MultiInterfacePropertyAccessor<TProperty>
    {
        private readonly Dictionary<Type, (Func<object, TProperty> getter, Action<object, TProperty> setter)> _accessors = new();
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
            if (item == null) return _defaultValue;

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
            if (item == null) return;

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
                throw new ArgumentException("Expression must be a property access");

            var parameter = propertyExpression.Parameters[0];
            var valueParameter = Expression.Parameter(typeof(TProperty), "value");
            var assignExpression = Expression.Assign(memberExpr, valueParameter);
            
            return Expression.Lambda<Action<TInterface, TProperty>>(assignExpression, parameter, valueParameter).Compile();
        }
    }
}