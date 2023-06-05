﻿using System;
using System.Reflection;

namespace BuildTools.Reflection
{
    static class ReflectionUtilities
    {
        public static FieldInfo GetInternalFieldInfo(this Type type, string name)
        {
            var fieldInfo = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

            if (fieldInfo == null)
                throw new MissingMemberException(type.Name, name);

            return fieldInfo;
        }

        public static PropertyInfo GetInternalPropertyInfo(this object obj, string name)
        {
            var propertyInfo = obj.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);

            if (propertyInfo == null)
                throw new MissingMemberException(obj.GetType().Name, name);

            return propertyInfo;
        }

        public static object GetInternalProperty(this object obj, string name)
        {
            var propertyInfo = GetInternalPropertyInfo(obj, name);

            return propertyInfo.GetValue(obj);
        }

        public static MethodInfo GetInternalMethod(this object obj, string name) => GetInternalMethod(obj.GetType(), name);

        public static MethodInfo GetInternalMethod(this Type type, string name)
        {
            var methodInfo = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);

            if (methodInfo == null)
                throw new MissingMemberException(type.Name, name);

            return methodInfo;
        }

        public static MethodInfo GetStaticInternalMethod(this Type type, string name)
        {
            var methodInfo = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);

            if (methodInfo == null)
                throw new MissingMemberException(type.Name, name);

            return methodInfo;
        }
    }
}