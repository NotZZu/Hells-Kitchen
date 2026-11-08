using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HellsKitchen.Tests.Generated
{
    /// <summary>
    /// 생성된 시험 코드가 쓰는 보조 함수. 객체 만들기, 비공개 멤버 접근, deltaTime 측정, 뒷정리를 맡는다.
    /// 모델이 쓴 코드가 아니라 사람이 검토한 고정 코드다. 생성 파일은 이 함수들만 호출한다.
    /// </summary>
    public static class GenHelpers
    {
        private const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly List<Object> Created = new List<Object>();

        // ------------------------------------------------------------ 만들기와 정리

        public static T Component<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            Created.Add(go);
            return go.AddComponent<T>();
        }

        public static T Scriptable<T>() where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            Created.Add(so);
            return so;
        }

        public static void Cleanup()
        {
            for (var i = Created.Count - 1; i >= 0; i--)
                if (Created[i] != null) Object.DestroyImmediate(Created[i]);
            Created.Clear();
        }

        // ------------------------------------------------------------ 시간

        /// <summary>
        /// 지금 Time.deltaTime 을 읽는다. EditMode 에서는 호출마다 값이 달라서 기대값을 상수로 쓸 수 없으므로,
        /// 시험이 같은 시점의 값을 읽어 기대값에 쓴다. 0 이하이면 판정할 수 없으므로 Inconclusive 처리한다.
        /// </summary>
        public static float MeasureDt()
        {
            var dt = Time.deltaTime;
            Assume.That(dt > 0f, "Time.deltaTime 이 0 이하라 측정값으로 기대값을 계산할 수 없다");
            return dt;
        }

        // ------------------------------------------------------------ 멤버 쓰기

        /// <summary>공개 필드 또는 프로퍼티에 값을 넣는다.</summary>
        public static void SetMember(object target, string name, object value)
        {
            var type = target.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public;

            var field = type.GetField(name, flags);
            if (field != null)
            {
                field.SetValue(target, Coerce(value, field.FieldType, name));
                return;
            }

            var prop = type.GetProperty(name, flags);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(target, Coerce(value, prop.PropertyType, name));
                return;
            }

            throw new MissingMemberException(type.Name, name + " (공개 필드/프로퍼티를 찾지 못했다)");
        }

        /// <summary>비공개 포함 필드에 값을 넣는다. 자동 구현 프로퍼티의 백킹 필드(&lt;X&gt;k__BackingField)도 이 함수로 쓴다.</summary>
        public static void SetField(object target, string name, object value)
        {
            for (var t = target.GetType(); t != null; t = t.BaseType)
            {
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f != null)
                {
                    f.SetValue(target, Coerce(value, f.FieldType, name));
                    return;
                }
            }

            throw new MissingFieldException(target.GetType().Name, name);
        }

        // ------------------------------------------------------------ 메서드 호출

        public static object Invoke(object target, string method, params object[] args)
        {
            return Call(target.GetType(), target, method, args);
        }

        public static object InvokeStatic(Type type, string method, params object[] args)
        {
            return Call(type, null, method, args);
        }

        private static object Call(Type type, object target, string method, object[] args)
        {
            args = args ?? new object[0];
            var triedCandidates = 0;

            for (var t = type; t != null; t = t.BaseType)
            {
                foreach (var m in t.GetMethods(All | BindingFlags.DeclaredOnly))
                {
                    if (m.Name != method) continue;
                    if (target == null && !m.IsStatic) continue;
                    triedCandidates++;

                    if (!TryBind(m, args, out var bound)) continue;

                    try
                    {
                        return m.Invoke(m.IsStatic ? null : target, bound);
                    }
                    catch (TargetInvocationException e)
                    {
                        // 원래 예외(Assert 실패, 게임 코드 예외)가 그대로 보이도록 풀어서 다시 던진다.
                        ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
                    }
                }
            }

            throw new MissingMethodException(type.Name, method + " (인자 " + args.Length + "개에 맞는 메서드를 찾지 못했다, 같은 이름 후보 " + triedCandidates + "개)");
        }

        private static bool TryBind(MethodInfo m, object[] args, out object[] bound)
        {
            var ps = m.GetParameters();
            bound = null;
            if (args.Length > ps.Length) return false;

            var result = new object[ps.Length];
            for (var i = 0; i < ps.Length; i++)
            {
                if (i < args.Length)
                {
                    if (!TryCoerce(args[i], ps[i].ParameterType, out result[i])) return false;
                }
                else if (ps[i].HasDefaultValue)
                {
                    result[i] = ps[i].DefaultValue;
                }
                else
                {
                    return false;
                }
            }

            bound = result;
            return true;
        }

        // ------------------------------------------------------------ 값 변환

        private static object Coerce(object value, Type to, string what)
        {
            if (TryCoerce(value, to, out var result)) return result;
            throw new InvalidCastException(what + ": " + (value == null ? "null" : value.GetType().Name) + " 을(를) " + to.Name + " 으로 바꿀 수 없다");
        }

        private static bool TryCoerce(object value, Type to, out object result)
        {
            if (value == null)
            {
                result = null;
                return !to.IsValueType || Nullable.GetUnderlyingType(to) != null;
            }

            if (to.IsInstanceOfType(value))
            {
                result = value;
                return true;
            }

            try
            {
                if (to.IsEnum)
                {
                    result = value is string s ? Enum.Parse(to, s) : Enum.ToObject(to, value);
                    return true;
                }

                if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(to))
                {
                    result = Convert.ChangeType(value, to, CultureInfo.InvariantCulture);
                    return true;
                }
            }
            catch (Exception e) when (e is FormatException || e is InvalidCastException || e is OverflowException || e is ArgumentException)
            {
                // 아래에서 실패로 처리한다.
            }

            result = null;
            return false;
        }
    }
}
