using RegexCrafter.Helpers.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegexCrafter;

internal abstract class CraftingBase
{
    protected readonly CraftPlaceType _placeType;
    protected bool _isCraftingEnabled = true;
    protected CraftingBase(CraftPlaceType placeType)
    {
        _placeType = placeType;
    }

}
