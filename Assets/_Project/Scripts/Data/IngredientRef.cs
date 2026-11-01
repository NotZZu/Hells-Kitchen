using System;
using System.Collections.Generic;
using HellsKitchen.Core.Ingredients;
using UnityEngine;

namespace HellsKitchen.Data
{
    /// <summary>
    /// 인스펙터에서 편집하기 위한 재료 표기.
    /// Core 의 IngredientStack 은 readonly struct 라 유니티 직렬화가 되지 않으므로,
    /// 데이터 레이어에서 이 클래스를 거쳐 변환한다.
    /// </summary>
    [Serializable]
    public class IngredientRef
    {
        public IngredientType Type = IngredientType.None;
        public ProcessState State = ProcessState.Raw;

        public IngredientRef() { }

        public IngredientRef(IngredientType type, ProcessState state)
        {
            Type = type;
            State = state;
        }

        public IngredientStack ToStack() => new IngredientStack(Type, State);

        public static IngredientStack[] ToStacks(List<IngredientRef> refs)
        {
            if (refs == null || refs.Count == 0) return Array.Empty<IngredientStack>();

            var result = new IngredientStack[refs.Count];
            for (int i = 0; i < refs.Count; i++)
            {
                result[i] = refs[i] != null ? refs[i].ToStack() : default;
            }
            return result;
        }
    }
}
