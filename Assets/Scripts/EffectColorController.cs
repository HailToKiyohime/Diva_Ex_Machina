using System.Collections.Generic;
using UnityEngine;

[System.Serializable] // 重要：讓 Inspector 能顯示
public class LinkedParticleSystem
{
    public ParticleSystem[] particleSystem;
}

public class EffectColorController : MonoBehaviour
{
    [SerializeField] private LinkedParticleSystem[] particleSystems;

    // 用來存每組的顏色資訊：
    // - 如果該組是單色：存 1 個 color
    // - 如果該組是 TwoColors：存 2 個 color（min, max）
    public List<Color> colors = new List<Color>();

    private void Start()
    {
        CacheColorsFromGroups();
    }

    // -----------------------------
    // 讀：把每組的第一個 PS 的 StartColor 存進 colors
    // -----------------------------
    public void CacheColorsFromGroups()
    {
        colors.Clear();

        if (particleSystems == null) return;

        foreach (var group in particleSystems)
        {
            if (!IsValidGroup(group)) continue;

            AddColorsToList(group); // 你原本的函式（我保留並強化）
        }
    }

    // -----------------------------
    // 寫：改某一組全部 ParticleSystem 的 StartColor
    // -----------------------------
    public void ApplyGroupColor(int groupIndex, Color singleColor)
    {
        if (!IsValidGroupIndex(groupIndex)) return;

        var group = particleSystems[groupIndex];
        foreach (var ps in group.particleSystem)
        {
            if (ps == null) continue;
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(singleColor);
        }
    }

    public void ApplyGroupTwoColors(int groupIndex, Color minColor, Color maxColor)
    {
        if (!IsValidGroupIndex(groupIndex)) return;

        var group = particleSystems[groupIndex];
        foreach (var ps in group.particleSystem)
        {
            if (ps == null) continue;
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(minColor, maxColor);
        }
    }

    // -----------------------------
    // 寫：用 colors 清單，依序套用回每組（單色/雙色）
    // 規則：以該組第一個 PS 的 mode 決定吃 1 或 2 個顏色
    // -----------------------------
    public void ApplyFromColorsList()
    {
        if (particleSystems == null) return;

        int cursor = 0;

        for (int gi = 0; gi < particleSystems.Length; gi++)
        {
            var group = particleSystems[gi];
            if (!IsValidGroup(group)) continue;

            var first = group.particleSystem[0];
            var sc = first.main.startColor;

            if (sc.mode == ParticleSystemGradientMode.Color)
            {
                if (cursor >= colors.Count) return;
                ApplyGroupColor(gi, colors[cursor]);
                cursor += 1;
            }
            else if (sc.mode == ParticleSystemGradientMode.TwoColors)
            {
                if (cursor + 1 >= colors.Count) return;
                ApplyGroupTwoColors(gi, colors[cursor], colors[cursor + 1]);
                cursor += 2;
            }
            else
            {
                // 其他模式（Gradient / TwoGradients / RandomColor…）
                // 這裡選擇：當作單色吃 1 個（你也可以改成 TwoColors）
                if (cursor >= colors.Count) return;
                ApplyGroupColor(gi, colors[cursor]);
                cursor += 1;
            }
        }
    }

    // -----------------------------
    // 你原本的函式：我加了 null / length 防呆
    // -----------------------------
    void AddColorsToList(LinkedParticleSystem lps)
    {
        if (!IsValidGroup(lps)) return;

        var sc = lps.particleSystem[0].main.startColor;

        if (sc.mode == ParticleSystemGradientMode.Color)
        {
            colors.Add(sc.color);
        }
        else if (sc.mode == ParticleSystemGradientMode.TwoColors)
        {
            colors.Add(sc.colorMin);
            colors.Add(sc.colorMax);
        }
        else
        {
            // 其他模式：給一個合理 fallback（取 colorMax 或 white）
            // 也可以改成從 gradient.Evaluate(0/1) 取值
            colors.Add(Color.white);
        }
    }

    private bool IsValidGroup(LinkedParticleSystem group)
    {
        return group != null &&
               group.particleSystem != null &&
               group.particleSystem.Length > 0 &&
               group.particleSystem[0] != null;
    }

    private bool IsValidGroupIndex(int i)
    {
        return particleSystems != null &&
               i >= 0 && i < particleSystems.Length &&
               IsValidGroup(particleSystems[i]);
    }
}
