using System;
using System.Collections.Generic;

public class BuySkillsComparer : IComparer<SkillData>
{
    public int Compare(SkillData x, SkillData y)
    {
        int num = x.StatValue.CompareTo(y.StatValue);
        if (num == 0)
        {
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }
        return num;
    }
}
