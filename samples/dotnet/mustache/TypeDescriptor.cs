﻿using System.Reflection;
using System.Linq.Expressions;

namespace mustache
{
	#region Documentation
	
	/// <summary>
	/// Cache for type metadata
	/// Converts FieldInfo and PropertyInfo to delegates to speed up the reflection
	/// </summary>
	
	#endregion Documentation
	
	internal static class TypeDescriptor
    {
		#region Fields

		private static readonly Dictionary<Type, Dictionary<string, Func<object, object>>> types = new();

		#endregion Fields

		#region Methods

		public static object? Get(object instance, string name)
		{
            var type = instance.GetType();

            if (!types.TryGetValue(type, out Dictionary<string, Func<object, object>>? delegates))
            {
                lock (types)
                {
                    if (!types.TryGetValue(type, out delegates))
                    {
                        delegates = GetDelegates(type);
                        types.Add(type, delegates);
                    }
				}
            }

			if (delegates!.TryGetValue(name, out Func<object, object>? get))
			{
				return get?.Invoke(instance);
			}

			return null;
		}

		private static Dictionary<string, Func<object, object>> GetDelegates(Type type)
		{
			var delegates = new Dictionary<string, Func<object, object>>();

			foreach (var field in type.GetFields())
			{
				var get = CreateAcessor(type, field);
				if (get == null) continue;

				delegates.Add(field.Name, get);
			}

			foreach (var field in type.GetProperties())
			{
				var get = CreateAcessor(type, field);
				if (get == null) continue;

				delegates.Add(field.Name, get);
			}

            return delegates;
		}

		private static Func<object, object>? CreateAcessor(Type type, MemberInfo memberInfo)
		{
			var instance = Expression.Parameter(typeof(object));

			if (memberInfo is FieldInfo fieldInfo)
			{
				var getBody = Expression.Convert(Expression.Field(Expression.Convert(instance, type), fieldInfo), typeof(object));
				var getParameters = new ParameterExpression[] { instance };
				return Expression.Lambda<Func<object, object>>(getBody, getParameters).Compile();
			}
			else if (memberInfo is PropertyInfo propertyInfo)
			{
				var getMethod = propertyInfo.GetGetMethod(nonPublic: true);
				if (getMethod != null)
				{
					var getBody = Expression.Convert(Expression.Call(Expression.Convert(instance, type), getMethod), typeof(object));
					var getParameters = new ParameterExpression[] { instance };
					return Expression.Lambda<Func<object, object>>(getBody, getParameters).Compile();
				}
			}

			return null;
		}

		#endregion Methods
	}
}